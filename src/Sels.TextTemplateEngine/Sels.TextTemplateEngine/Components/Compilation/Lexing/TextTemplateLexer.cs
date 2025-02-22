using Microsoft.Extensions.Logging;
using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Text;
using Sels.TextTemplateEngine.Compilation.Lexing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing
{
    /// <inheritdoc cref="ITextTemplateLexer"/>
    public class TextTemplateLexer : ITextTemplateLexer
    {
        // Fields
        private readonly ILogger? _logger;
        private readonly ITextTemplateTokenLexer[] _tokenLexers;

        /// <inheritdoc cref="TextTemplateLexer"/>
        /// <param name="lexers">The token lexers that will be used to read tokens</param>
        /// <param name="logger">Optional logger for tracing</param>
        public TextTemplateLexer(IEnumerable<ITextTemplateTokenLexer> lexers, ILogger<TextTemplateLexer>? logger = null)
        {
            _tokenLexers = Guard.IsNotNull(lexers).ToArray();
            _logger = logger;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ITextTemplateToken> LexAsync(string compilerProcess, Action<ITextTemplateLexerConfigurationBuilder> configure, Stream stream, Encoding? encoding = null, bool ownsStream = true, int bufferLength = 1024, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            compilerProcess = Guard.IsNotNull(compilerProcess);
            stream = Guard.IsNotNull(stream);
            bufferLength = Guard.IsLarger(bufferLength, 0);
            configure = Guard.IsNotNull(configure);

            _logger.Log($"Preparing to read stream <{stream}> using a buffer length of <{bufferLength}> to lex it into tokens");

            try
            {
                var settings = new TextTemplateLexerSettings(_tokenLexers, configure);
                var context = new TextTemplateLexerContext(compilerProcess, stream, settings);
                var interfaceContext = context.CastTo<ITextTemplateLexerContext>();
                using (var streamReader = encoding != null ? new StreamReader(stream, encoding, leaveOpen: true) : new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
                {
                    char[] buffer = new char[bufferLength];
                    var streamPosition = 0;
                    var charactersRead = await streamReader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    var atEndOfStream = charactersRead < buffer.Length;
                    bool lexCurrentBuffer = true;

                    while (charactersRead > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        streamPosition += charactersRead;
                        _logger.Debug($"Read <{charactersRead}> characters from stream at position <{streamPosition}>");
                        context.IsLastCharacter = false;
                        await foreach (var (i, character) in context.InterceptAsync(buffer.Take(charactersRead).ToArray(), cancellationToken).ConfigureAwait(false))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            lexCurrentBuffer = true;
                            context.Buffer.Add(character);
                            var lastCharacterInCurrentBuffer = i == charactersRead - 1;
                            context.IsLastCharacter = atEndOfStream && lastCharacterInCurrentBuffer;

                            while (lexCurrentBuffer && context.Buffer.HasValue())
                            {
                                lexCurrentBuffer = false;
                                cancellationToken.ThrowIfCancellationRequested();

                                await foreach (var token in TryLexAsync(context, cancellationToken).ConfigureAwait(false))
                                {
                                    lexCurrentBuffer = true;
                                    _logger.Log($"Token <{token}> of length <{token.Length}> found at <{token.Position}>");
                                    yield return token;
                                }
                            }
                            context.Index++;
                            context.LineIndex++;
                        }

                        if (!atEndOfStream) charactersRead = await streamReader.ReadAsync(buffer, streamPosition, buffer.Length).ConfigureAwait(false);
                        else charactersRead = 0;
                    }

                    if (context.Buffer.Count > 0)
                    {
                        _logger.Log($"End of stream reached with a buffer of length <{context.Buffer.Count}>. Generating text token at position <{interfaceContext.BufferIndex}> on line <{context.Line}>");
                        yield return new TextTemplateTextToken(context.Buffer) { Position = new TokenPosition() { Index = interfaceContext.BufferIndex, Line = context.Line, LineIndex = interfaceContext.BufferLineIndex } };
                    }
                }
            }
            finally
            {
                if (ownsStream)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        private async IAsyncEnumerable<ITextTemplateToken> TryLexAsync(TextTemplateLexerContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            var interfaceContext = context.CastTo<ITextTemplateLexerContext>();
            _logger.Debug($"Asking <{context.TokenLexers}> lexers if they can lex character <{interfaceContext.CurrentCharacter}> with a current buffer of length <{context.Buffer.Count}>");
            foreach (var tokenLexer in context.TokenLexers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.Debug($"Calling token lexer <{tokenLexer}> for character <{interfaceContext.CurrentCharacter}> with a current buffer of length <{context.Buffer.Count}>");
                var response = await tokenLexer.IsInterestedAsync(context, cancellationToken).ConfigureAwait(false);
                _logger.Debug($"Token lexer <{tokenLexer}> responded with <{response}> for character <{interfaceContext.CurrentCharacter}> with a current buffer of length <{context.Buffer.Count}>");
                if (response == TokenLexerResponse.CanLex)
                {
                    _logger.Debug($"Token lexer <{tokenLexer}> can lex character <{interfaceContext.CurrentCharacter}> with a current buffer of length <{context.Buffer.Count}>. Generating token");
                    var token = Guard.IsNotNull(await tokenLexer.GenerateAsync(context, cancellationToken).ConfigureAwait(false));
                    _logger.Debug($"Token lexer <{tokenLexer}> generated token <{token}> from <{token.Length}> characters from position <{token.Position}>");

                    // Validate that token is not longer than the current position
                    if (token.Position.Index + token.Length > context.Index + 1) throw new InvalidOperationException($"Token <{token}> is longer than the current position <{context.Index}>");
                    // Validate token is not created before the buffer
                    if (token.Position.Index < interfaceContext.BufferIndex) throw new InvalidOperationException($"Token <{token}> is created before the buffer position <{interfaceContext.BufferIndex}>");

                    // Check if we need to trim buffer before token
                    var bufferBeforeToken = token.Position.Index - interfaceContext.BufferIndex;
                    if (bufferBeforeToken > 0)
                    {
                        _logger.Debug($"Token was generated at <{token.Position}> while buffer is at position <{interfaceContext.BufferIndex}>. Trying to lex remaining buffer before token");
                        var remainingBuffer = context.Buffer.Take(bufferBeforeToken).ToList();
                        var remainingBufferContext = new TextTemplateLexerContext(context.CompilerProcess, context, remainingBuffer) { IsLastCharacter = true };
                        context.Buffer.RemoveRange(0, bufferBeforeToken);
                        await foreach (var bufferToken in TryLexAsync(remainingBufferContext, cancellationToken).ConfigureAwait(false))
                        {
                            yield return bufferToken;
                        }
                        
                        if (remainingBufferContext.Buffer.HasValue())
                        {
                            var remainingToken = new TextTemplateTextToken(remainingBufferContext.Buffer) { Position = new TokenPosition() { Index = ((ITextTemplateLexerContext)remainingBufferContext).BufferIndex, Line = remainingBufferContext.Line, LineIndex = ((ITextTemplateLexerContext)remainingBufferContext).BufferLineIndex } };
                            await foreach(var returnToken in context.InterceptAsync(remainingToken, cancellationToken).ConfigureAwait(false))
                            {
                                yield return returnToken;
                            }
                        }
                    }
                    // Remove token from remaining buffer
                    context.Buffer.RemoveRange(0, token.Length);

                    await foreach (var returnToken in context.InterceptAsync(token, cancellationToken).ConfigureAwait(false))
                    {
                        yield return returnToken;
                    }
                    break;
                }
                else if (!context.IsLastCharacter && response == TokenLexerResponse.Interested)
                {
                    _logger.Debug($"Token lexer <{tokenLexer}> is interested in character <{interfaceContext.CurrentCharacter}> with a current buffer of length <{context.Buffer.Count}>. Reading next");
                    break;
                }
            }
        }

        private class TextTemplateLexerContext : ITextTemplateLexerContext
        {
            // Properties
            public string CompilerProcess { get; }
            public Stream Source { get; }
            public int Index { get; set; } = 0;
            public int Line { get; set; } = 1;
            public List<char> Buffer { get; } = new List<char>();
            IReadOnlyList<char> ITextTemplateLexerContext.Buffer => Buffer;
            public IReadOnlyList<ITextTemplateTokenLexer> TokenLexers { get; }
            public bool IsLastCharacter { get; set; }
            public int LineIndex { get; set; } = 0;
            public TextTemplateLexerSettings Settings { get; }

            public TextTemplateLexerContext(string compilerProcess, Stream source, TextTemplateLexerSettings settings)
            {
                CompilerProcess = Guard.IsNotNull(compilerProcess);
                Source = Guard.IsNotNull(source);
                TokenLexers = settings.Lexers.OrderBy(x => x.Priority).ToList();
                Settings = Guard.IsNotNull(settings);
            }

            public TextTemplateLexerContext(string compilerProcess, TextTemplateLexerContext context, List<char> newBuffer)
            {
                CompilerProcess = Guard.IsNotNull(compilerProcess);
                context = Guard.IsNotNull(context);
                newBuffer = Guard.IsNotNull(newBuffer);
                Source = context.Source;
                Index = context.Index;
                Line = context.Line;
                LineIndex = context.LineIndex;
                Buffer = newBuffer;
                TokenLexers = context.TokenLexers;
                IsLastCharacter = context.IsLastCharacter;
                Settings = context.Settings;
            }

            public async IAsyncEnumerable<(int BufferPosition, char Character)> InterceptAsync(char[] charachtersRead, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < charachtersRead.Length; i++)
                {
                    char character = charachtersRead[i];
                    bool consumed = false;
                    foreach (var interceptor in Settings.Interceptors)
                    {
                        var result = await interceptor(this, character, cancellationToken).ConfigureAwait(false);
                        if (result.HasValue())
                        {
                            if(result!.Length == 1 && result[0].Equals(character)) continue;
                            foreach (var newCharacter in result!.Where(x => x.HasValue))
                            {
                                yield return (i, newCharacter!.Value);
                            }
                        }
                        consumed = true;
                        break;
                    }

                    if(!consumed)
                    {
                        yield return (i, character);
                    }
                }
            }

            public async IAsyncEnumerable<ITextTemplateToken> InterceptAsync(ITextTemplateToken token, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                void IncreaseIfLine(ITextTemplateToken token)
                {
                    token = Guard.IsNotNull(token);
                    if (token.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.NewLine) && !token.Position.IsVirtual)
                    {
                        Line++;
                        LineIndex = 0;
                    }
                }

                bool consumed = false;
                foreach (var interceptor in Settings.TokenInterceptors)
                {
                    bool interceptorConsumed = false;
                    int tokenCount = 0;
                    await foreach (var newToken in interceptor(this, token, cancellationToken).ConfigureAwait(false))
                    {
                        if (newToken != null)
                        {
                            tokenCount++;
                            var tokenPosition = new TokenPosition() { Index = ((ITextTemplateLexerContext)this).Index, Line = Line, LineIndex = ((ITextTemplateLexerContext)this).BufferLineIndex, IsVirtual = token != newToken };

                            newToken.Position = tokenPosition;

                            if (!interceptorConsumed) interceptorConsumed = token.Position.IsVirtual;

                            if(!token.Position.IsVirtual || !consumed) yield return newToken;
                            IncreaseIfLine(token);
                        }
                        else if(tokenCount == 0)
                        {
                            consumed = true;
                        }
                    }
                    consumed = interceptorConsumed && tokenCount > 1;
                    if(consumed) break;
                }

                if (!consumed)
                {
                    token.Position = new TokenPosition() { Index = ((ITextTemplateLexerContext)this).Index, Line = Line, LineIndex = ((ITextTemplateLexerContext)this).BufferLineIndex, IsVirtual = false };
                    yield return token;
                    IncreaseIfLine(token);
                }
            }
        }
    }
}
