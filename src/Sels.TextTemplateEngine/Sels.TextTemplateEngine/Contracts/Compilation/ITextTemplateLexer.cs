using Sels.Core;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sels.Core.Delegates.Async;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Lexer that reads a text stream and converts it into tokens for a <see cref="ITextTemplateParser"/> to parse.
    /// </summary>
    public interface ITextTemplateLexer
    {
        /// <summary>
        /// Reads <paramref name="stream"/> and enumerates the tokens found in the stream.
        /// </summary>
        /// <param name="compilationContext">The configured compilation context</param>
        /// <param name="stream">The stream to read from</param>
        /// <param name="configure">Delegate called to get the settings to use for lexing tokens from <paramref name="stream"/></param>
        /// <param name="encoding">The encoding of <paramref name="stream"/> if known</param>
        /// <param name="ownsStream">If the lexers owns the stream and can dispose it when done, set to false if caller does the disposing</param>
        /// <param name="bufferLength">How many characters will be read at a time from <paramref name="stream"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Async enumerator that will return any tokens reads from <paramref name="stream"/></returns>
        public IAsyncEnumerable<ITextTemplateToken> LexAsync(ITextTemplateCompilationContext compilationContext, Action<ITextTemplateLexerConfigurationBuilder> configure, Stream stream, Encoding? encoding = null, bool ownsStream = true, int bufferLength = 1024, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Used to configure the settings to use when parsing a text template.
    /// </summary>
    public interface ITextTemplateLexerConfigurationBuilder
    {
        /// <summary>
        /// The current lexers that will be used to parse the tokens into expressions.
        /// </summary>
        public IList<ITextTemplateTokenLexer> Lexers { get; }

        /// <summary>
        /// Adds <paramref name="lexer"/> to <see cref="Lexers"/>
        /// </summary>
        /// <param name="lexer">The lexer to add to the current configuration</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder WithLexer(ITextTemplateTokenLexer lexer);
        /// <summary>
        /// Clears all lexers from <see cref="Lexers"/>. Includes the default/globally defined lexers.
        /// </summary>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder ClearLexers()
        {
            Lexers.Clear();
            return this;
        }

        /// <summary>
        /// Removes all lexers that don't produce any of the <paramref name="tokenTypes"/> from <see cref="Lexers"/>
        /// </summary>
        /// <param name="tokenTypes">The wanted token types to be lexed</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder OnlyLexTokenTypes(params string[] tokenTypes)
        {
            tokenTypes = Guard.IsNotNull(tokenTypes);
            Lexers.Where(x => x != null).Execute(x =>
            {
                foreach(var producedTokenTypes in x.Produces)
                {
                    if (!tokenTypes.Contains(producedTokenTypes))
                    {
                        Lexers.Remove(x);
                        break;
                    }
                }
            });
            return this;
        }
        /// <summary>
        /// Removes all lexers that produce any of the <paramref name="tokenTypes"/> from <see cref="Lexers"/>
        /// </summary>
        /// <param name="tokenTypes">The unwated token types not to lex</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder LexAllTokenTypesExcept(params string[] tokenTypes)
        {
            tokenTypes = Guard.IsNotNull(tokenTypes);
            Lexers.Where(x => x != null).Execute(x =>
            {
                foreach (var producedTokenTypes in x.Produces)
                {
                    if (tokenTypes.Contains(producedTokenTypes))
                    {
                        Lexers.Remove(x);
                        break;
                    }
                }
            });
            return this;
        }

        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a character is read from the stream.
        /// Can be used to either consume the character or modify it before it is lexed.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptRead(AsyncFunc<ITextTemplateLexerContext, char, CancellationToken, char?[]?> interceptor);
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a character is read from the stream.
        /// Can be used to either consume the character or modify it before it is lexed.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptRead(Func<ITextTemplateLexerContext, char, CancellationToken, char?[]?> interceptor)
            => InterceptRead((context, character, token) => Task.FromResult(interceptor(context, character, token)));
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a character is read from the stream.
        /// Can be used to either consume the character or modify it before it is lexed.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptRead(AsyncFunc<ITextTemplateLexerContext, char, CancellationToken, char?> interceptor)
        {
            interceptor = Guard.IsNotNull(interceptor);

            return InterceptRead(async (context, character, token) =>
            {
                interceptor = Guard.IsNotNull(interceptor);
                return new char?[] { await interceptor(context, character, token).ConfigureAwait(false) };
            });
        }
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a character is read from the stream.
        /// Can be used to either consume the character or modify it before it is lexed.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptRead(Func<ITextTemplateLexerContext, char, CancellationToken, char?> interceptor)
            => InterceptRead((context, character, token) => Task.FromResult(interceptor(context, character, token)));

        /// <summary>
        /// Registers <paramref name="reader"/> that will be called each time a character is read from the stream.
        /// </summary>
        /// <param name="reader">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder OnRead(AsyncAction<ITextTemplateLexerContext, char, CancellationToken> reader)
        {
            reader = Guard.IsNotNull(reader);
            return InterceptRead(async (context, character, token) =>
            {
                await reader(context, character, token).ConfigureAwait(false);
                return character;
            });
        }
        /// <summary>
        /// Registers <paramref name="reader"/> that will be called each time a character is read from the stream.
        /// </summary>
        /// <param name="reader">The delegate that will be called. Arg1: The current context, Arg2: The character that was read, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder OnRead(Action<ITextTemplateLexerContext, char, CancellationToken> reader)
            => OnRead((context, character, token) =>
            {
                reader = Guard.IsNotNull(reader);
                reader(context, character, token);
                return character.ToTaskResult();
            });

        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptTokenGeneration(Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, IAsyncEnumerable<ITextTemplateToken>> interceptor);
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptTokenGeneration(Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, IEnumerable<ITextTemplateToken>> interceptor)
        {
            interceptor = Guard.IsNotNull(interceptor);
            return InterceptTokenGeneration((context, token, cancellationToken) =>
            {
                return Enumerate(interceptor(context, token, cancellationToken));
            });
        }
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptTokenGeneration(AsyncFunc<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, ITextTemplateToken?> interceptor)
        {
            interceptor = Guard.IsNotNull(interceptor);
            return InterceptTokenGeneration((context, token, cancellationToken) =>
            {
                return Enumerate(interceptor(context, token, cancellationToken));
            });
        }
        /// <summary>
        /// Registers <paramref name="interceptor"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="interceptor">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder InterceptTokenGeneration(Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, ITextTemplateToken?> interceptor)
            => InterceptTokenGeneration((context, token, cancellationToken) => Task.FromResult(interceptor(context, token, cancellationToken)));
        /// <summary>
        /// Registers <paramref name="reader"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="reader">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder OnTokenGenerated(AsyncAction<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken> reader)
        {
            reader = Guard.IsNotNull(reader);
            return InterceptTokenGeneration(async (context, token, cancellationToken) =>
            {
                await reader(context, token, cancellationToken).ConfigureAwait(false);
                return token;
            });
        }
        /// <summary>
        /// Registers <paramref name="reader"/> that will be called each time a token is generated.
        /// </summary>
        /// <param name="reader">The delegate that will be called. Arg1: The current context, Arg2: The token that was generated, Arg3: Token that will be cancelled when the lexer is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateLexerConfigurationBuilder OnTokenGenerated(Action<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken> reader)
        {
            reader = Guard.IsNotNull(reader);
            return OnTokenGenerated((context, token, cancellationToken) =>
            {
                reader(context, token, cancellationToken);
                return token.ToTaskResult();
            });
        }
        private async IAsyncEnumerable<T> Enumerate<T>(IEnumerable<T> source)
        {
            source = Guard.IsNotNull(source);
            foreach (var item in source)
            {
                if(item != null)
                    yield return await Task.FromResult(item);
            }
        }

        private async IAsyncEnumerable<T> Enumerate<T>(Task<T?> get)
        {
            get = Guard.IsNotNull(get);
            var item = await get.ConfigureAwait(false);
            if (item != null)
                yield return item;
        }
    }

    /// <summary>
    /// Context that contains the current state of the lexer when reading a stream.
    /// </summary>
    public interface ITextTemplateLexerContext : ITextTemplateCompilationContext
    {
        /// <summary>
        /// The stream being read.
        /// </summary>
        public Stream Source { get; }
        /// <summary>
        /// The current index of <see cref="CurrentCharacter"/>.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// The index of the first character in <see cref="Buffer"/>.
        /// </summary>
        public int BufferIndex => Index - Buffer.Count + 1;
        /// <summary>
        /// The current line in the stream.
        /// </summary>
        public int Line { get; }
        /// <summary>
        /// The current index of <see cref="CurrentCharacter"/> in <see cref="Line"/>.
        /// </summary>
        public int LineIndex { get; }
        /// <summary>
        /// The index of the first character in <see cref="Buffer"/> in <see cref="Line"/>.
        /// </summary>
        public int BufferLineIndex => LineIndex - Buffer.Count + 1;
        /// <summary>
        /// The current character buffer that hasn't been lexed yet.
        /// </summary>
        public IReadOnlyList<char> Buffer { get; }
        /// <summary>
        /// The current character being read.
        /// </summary>
        public char CurrentCharacter => Buffer.Last();
        /// <summary>
        /// The character read before <see cref="CurrentCharacter"/>. Can be null if the buffer only contains <see cref="CurrentCharacter"/>.
        /// </summary>
        public char? PreviousCharacter => Buffer.Skip(Buffer.Count - 2).FirstOrDefault();
        /// <summary>
        /// Indicates if <see cref="CurrentCharacter"/> is the last character in the stream.
        /// </summary>
        public bool IsLastCharacter { get; }
        /// <summary>
        /// All the token lexers that are available to lex tokens in the current context.
        /// </summary>
        public IReadOnlyList<ITextTemplateTokenLexer> TokenLexers { get; }
    }
}
