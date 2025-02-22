using Sels.Core;
using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sels.Core.Delegates.Async;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Parser tokens into an expression tree.
    /// </summary>
    public interface ITextTemplateParser
    {
        /// <summary>
        /// Parses all tokens returned by <paramref name="tokens"/> into an expression tree.
        /// </summary>
        /// <param name="compilerProcess">Gives an indication on what compiler process is being executed. Handy for resolving named options. Empty string if not provided</param>
        /// <param name="configure">Delegate called to get the settings to use for parsing <paramref name="tokens"/></param>
        /// <param name="tokens">Enumerator returning all the token to parse</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The parsed abstract syntax tree</returns>
        public Task<AbstractSyntaxTreeExpression> ParseAsync(string compilerProcess, Action<ITextTemplateParserConfigurationBuilder> configure, IAsyncEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Used to configure the settings to use when parsing a text template.
    /// </summary>
    public interface ITextTemplateParserConfigurationBuilder
    {
        /// <summary>
        /// The current parsers that will be used to parse the tokens into expressions.
        /// </summary>
        public IList<ITextTemplateSyntaxExpressionParser> Parsers { get; }

        /// <summary>
        /// Adds <paramref name="parser"/> to <see cref="Parsers"/>
        /// </summary>
        /// <param name="parser">The parser to add to the current configuration</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder WithParser(ITextTemplateSyntaxExpressionParser parser);
        /// <summary>
        /// Clears all parsers from <see cref="Parsers"/>. Includes the default/globally defined parsers.
        /// </summary>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder ClearParsers()
        {
            Parsers.Clear();
            return this;
        }
        /// <summary>
        /// Only use parsers that can parse the provided expression <paramref name="types"/>.
        /// </summary>
        /// <param name="types">The wanted types of expressions to parse</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder OnlyParseExpressionTypes(params string[] types)
        {
            types = Guard.IsNotNullOrEmpty(types);

            foreach (var parser in Parsers)
            {
                foreach (var producedExpressionTypes in parser.Parses)
                {
                    if (!types.Contains(producedExpressionTypes))
                    {
                        Parsers.Remove(parser);
                        break;
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Only use parses that don't parse the provided expression <paramref name="types"/>.
        /// </summary>
        /// <param name="types">The unwanted expression types not to parse</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder ParseAllExpressionTypesExcept(params string[] types)
        {
            types = Guard.IsNotNullOrEmpty(types);
            foreach (var parser in Parsers)
            {
                foreach (var producedExpressionTypes in parser.Parses)
                {
                    if (types.Contains(producedExpressionTypes))
                    {
                        Parsers.Remove(parser);
                        break;
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Registers an interceptor that can modify or remove the parsed expression before it is added to the syntax tree.
        /// </summary>
        /// <param name="interceptor">Delegate that will be called each time an expression is parsed. Arg1: The current parser context | Arg2: The parsed expression | Arg3: Token that will be cancelled when interceptor is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder InterceptParsedExpression(AsyncFunc<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken, ITextTemplateSyntaxExpression?> interceptor);
        /// <summary>
        /// Registers an interceptor that can modify or remove the parsed expression before it is added to the syntax tree.
        /// </summary>
        /// <param name="interceptor">Delegate that will be called each time an expression is parsed. Arg1: The current parser context | Arg2: The parsed expression | Arg3: Token that will be cancelled when interceptor is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder InterceptParsedExpression(Func<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken, ITextTemplateSyntaxExpression?> interceptor)
        {
            interceptor = Guard.IsNotNull(interceptor);

            return InterceptParsedExpression((context, expression, token) => Task.FromResult(interceptor(context, expression, token)));
        }
        /// <summary>
        /// Registers a delegate that will be called when an expression is parsed.
        /// </summary>
        /// <param name="onRead">Delegate that will be called each time an expression is parsed. Arg1: The current parser context | Arg2: The parsed expression | Arg3: Token that will be cancelled when interceptor is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder OnRead(AsyncAction<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken> onRead)
        {
            onRead = Guard.IsNotNull(onRead);

            return InterceptParsedExpression(async (context, expression, token) =>
            {
                await onRead(context, expression, token).ConfigureAwait(false);
                return expression;
            });
        }
        /// <summary>
        /// Registers a delegate that will be called when an expression is parsed.
        /// </summary>
        /// <param name="onRead">Delegate that will be called each time an expression is parsed. Arg1: The current parser context | Arg2: The parsed expression | Arg3: Token that will be cancelled when interceptor is requested to stop</param>
        /// <returns>Current builder for method chaining</returns>
        public ITextTemplateParserConfigurationBuilder OnRead(Action<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken> onRead)
        {
            onRead = Guard.IsNotNull(onRead);
            return OnRead((context, expression, token) =>
            {
                onRead(context, expression, token);
                return Task.CompletedTask;
            });
        }
    }

    /// <summary>
    /// Context that contains the current state of the parser when creating the syntax tree.
    /// </summary>
    public interface ITextTemplateParserContext : IDisposable
    {
        /// <summary>
        /// Gives an indication on what compiler process is being executed. Handy for resolving named options. Empty string if not provided.
        /// </summary>
        public string CompilerProcess { get; }
        // Token
        /// <summary>
        /// The current buffer of tokens being parsed.
        /// </summary>
        public List<ITextTemplateToken> Buffer { get; }
        /// <summary>
        /// The current token being parsed.
        /// </summary>
        public ITextTemplateToken CurrentToken => Buffer.Last();
        /// <summary>
        /// The character read before <see cref="CurrentToken"/>. Can be null if the buffer only contains <see cref="CurrentToken"/>.
        /// </summary>
        public ITextTemplateToken? PreviousToken => Buffer.Skip(Buffer.Count - 2).FirstOrDefault();
        /// <summary>
        /// Indicates if <see cref="CurrentToken"/> is the last token in the stream.
        /// </summary>
        public bool IsLastToken { get; }

        /// <summary>
        /// Tries to read the next token from the stream and adds it to <see cref="Buffer"/>.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The token if one was read</returns>
        public Task<ITextTemplateToken?> TryReadNextAsync(CancellationToken cancellationToken = default);

        // Parse
        /// <summary>
        /// The available parsers that can be used to create expressions.
        /// </summary>
        public IReadOnlyList<ITextTemplateSyntaxExpressionParser> Parsers { get; }  
        /// <summary>
        /// The root expression of the syntax tree.
        /// </summary>
        public AbstractSyntaxTreeExpression Ast { get; }
        /// <summary>
        /// The current parent parser that is creating an expression. Will be null if the current parser is the root parser.
        /// </summary>
        public ITextTemplateSyntaxExpressionParser? ParentParser { get; }
        /// <summary>
        /// Gives an indication in what kind of scope the current expression is being created in if any.
        /// </summary>
        public string? ParserScope { get; }
        /// <summary>
        /// The parent context if the current context is a scoped sub context.
        /// </summary>
        public ITextTemplateParserContext? ParentContext { get; }

        /// <summary>
        /// Checks if any of the parsers in <see cref="Parsers"/> are interested in the current buffer.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Response: The response from the parsers|Parser the parser if response is either <see cref="SyntaxExpressionParserResponse.Interested"/> or <see cref="SyntaxExpressionParserResponse.CanParse"/></returns>
        public Task<(SyntaxExpressionParserResponse Response, ITextTemplateSyntaxExpressionParser? Parser)> AreInterestedInAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses the current buffer into an expression(s) using <paramref name="parser"/>.
        /// </summary>
        /// <param name="parser">The parser to user</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Enumerator returning the parsed expressions</returns>
        public IAsyncEnumerable<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateSyntaxExpressionParser parser, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a sub scope that can be used to use other parsers to create expressions.
        /// </summary>
        /// <param name="current">The instance creating the scope</param>
        /// <param name="scope">Optional scope to give parsers more context</param>
        /// <param name="bufferOffset">Used to skip the first n tokens in <see cref="Buffer"/> so sub context doesn't see them</param>
        /// <param name="bufferLimit">Limit the amount of token that can be read from <paramref name="bufferLimit"/> starting from <paramref name="bufferOffset"/></param>
        /// <param name="canReadNext">Indicates if the sub scope is allowed to read the next character</param>
        /// <param name="consumeTokens">When set to true tokens that are parsed by the sub scope will be removed from it's buffer. When disposing the remaining tokens will become the new buffer of the parent. When set to false the tokens read by the sub scope will be added to the buffer of the parent scope</param>
        /// <returns>A sub scope that can be used to call parsers. Should be disposed once done</returns>
        public ITextTemplateParserContext CreateScope(ITextTemplateSyntaxExpressionParser current, string? scope = null, int bufferOffset = 0, int? bufferLimit = null, bool canReadNext = true, bool consumeTokens = true);
    }
}
