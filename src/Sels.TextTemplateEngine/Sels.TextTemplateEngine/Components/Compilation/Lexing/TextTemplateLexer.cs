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

        /// <inheritdoc cref="TextTemplateLexer"/>
        /// <param name="tokenLexers">The token lexers that will be used to read tokens</param>
        /// <param name="logger">Optional logger for tracing</param>
        public TextTemplateLexer(ILogger<TextTemplateLexer>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ITextTemplateToken> LexAsync(IEnumerable<ITextTemplateTokenLexer> tokenLexers, Stream stream, Encoding? encoding = null, bool ownsStream = true, int bufferLength = 1024, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            stream = Guard.IsNotNull(stream);
            bufferLength = Guard.IsLarger(bufferLength, 0);
            tokenLexers = Guard.IsNotNullOrEmpty(Guard.IsNotNull(tokenLexers).Where(x => x != null).OrderBy(x => x.Priority)).ToArray();

            _logger.Log($"Preparing to read stream <{stream}> using a buffer length of <{bufferLength}> to lex it into tokens");

            try
            {
                var context = new TextTemplateLexerContext(stream, tokenLexers);
                var interfaceContext = context.CastTo<ITextTemplateLexerContext>();
                using (var streamReader = encoding != null ? new StreamReader(stream, encoding) : new StreamReader(stream, true))
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
                        for (int i = 0; i < charactersRead; i++)
                        {
                            lexCurrentBuffer = true;
                            context.Buffer.Add(buffer[i]);
                            var lastCharacterInCurrentBuffer = i == charactersRead - 1;
                            context.IsLastCharacter = atEndOfStream && lastCharacterInCurrentBuffer;

                            while (lexCurrentBuffer && context.Buffer.HasValue())
                            {
                                lexCurrentBuffer = false;
                                cancellationToken.ThrowIfCancellationRequested();

                                await foreach (var token in TryLexAsync(context, cancellationToken).ConfigureAwait(false))
                                {
                                    lexCurrentBuffer = true;
                                    _logger.Log($"Token <{token}> of length <{token.Length}> found at position <{token.Position}> on line <{token.Line}>");
                                    yield return token;
                                }
                            }
                            context.Position++;
                        }

                        if (!atEndOfStream) charactersRead = await streamReader.ReadAsync(buffer, streamPosition, buffer.Length).ConfigureAwait(false);
                        else charactersRead = 0;
                    }

                    if (context.Buffer.Count > 0)
                    {
                        _logger.Log($"End of stream reached with a buffer of length <{context.Buffer.Count}>. Generating text token at position <{interfaceContext.BufferPosition}> on line <{context.Line}>");
                        yield return new TextTemplateTextToken(context.Buffer) { Position = interfaceContext.BufferPosition, Line = context.Line };
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
                    if (token.Position + token.Length > context.Position + 1) throw new InvalidOperationException($"Token <{token}> is longer than the current position <{context.Position}>");
                    // Validate token is not created before the buffer
                    if (token.Position < interfaceContext.BufferPosition) throw new InvalidOperationException($"Token <{token}> is created before the buffer position <{interfaceContext.BufferPosition}>");

                    // Check if we need to trim buffer before token
                    var bufferBeforeToken = token.Position - interfaceContext.BufferPosition;
                    if (bufferBeforeToken > 0)
                    {
                        _logger.Debug($"Token was generated starting from position <{token.Position}> while buffer is at position <{interfaceContext.BufferPosition}>. Trying to lex remaining buffer before token");
                        var remainingBuffer = context.Buffer.Take(bufferBeforeToken).ToList();
                        var remainingBufferContext = new TextTemplateLexerContext(context, remainingBuffer) { IsLastCharacter = true };
                        context.Buffer.RemoveRange(0, bufferBeforeToken);
                        await foreach (var bufferToken in TryLexAsync(remainingBufferContext, cancellationToken).ConfigureAwait(false))
                        {
                            yield return bufferToken;
                        }

                        if (remainingBufferContext.Buffer.HasValue()) yield return new TextTemplateTextToken(remainingBufferContext.Buffer) { Position = ((ITextTemplateLexerContext)remainingBufferContext).BufferPosition, Line = remainingBufferContext.Line };
                        // Set line count if increased
                        context.Line = remainingBufferContext.Line > context.Line ? remainingBufferContext.Line : context.Line;
                    }
                    // Remove token from remaining buffer
                    context.Buffer.RemoveRange(0, token.Length);

                    // Increase line count if token is a new line
                    if (TextTemplateEngineConstants.Compilation.TokenTypes.NewLine.EqualsNoCase(token.Type))
                    {
                        _logger.Debug($"Token <{token}> is a new line. Increasing line count");
                        context.Line++;
                    }

                    yield return token;
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
            public Stream Source { get; }
            public int Position { get; set; } = 0;
            public int Line { get; set; } = 1;
            public List<char> Buffer { get; } = new List<char>();
            IReadOnlyList<char> ITextTemplateLexerContext.Buffer => Buffer;
            public IReadOnlyList<ITextTemplateTokenLexer> TokenLexers { get; }
            public bool IsLastCharacter { get; set; }

            public TextTemplateLexerContext(Stream source, IEnumerable<ITextTemplateTokenLexer> lexers)
            {
                Source = Guard.IsNotNull(source);
                TokenLexers = Guard.IsNotNullOrEmpty(lexers).ToList();
            }

            public TextTemplateLexerContext(TextTemplateLexerContext context, List<char> newBuffer)
            {
                context = Guard.IsNotNull(context);
                newBuffer = Guard.IsNotNull(newBuffer);
                Source = context.Source;
                Position = context.Position;
                Line = context.Line;
                Buffer = newBuffer;
                TokenLexers = context.TokenLexers;
                IsLastCharacter = context.IsLastCharacter;
            }
        }
    }
}
