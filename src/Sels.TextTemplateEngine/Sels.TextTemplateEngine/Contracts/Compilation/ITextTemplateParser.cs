using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Parser tokens into an expression tree.
    /// </summary>
    public interface ITextTemplateParser
    {
        /// <summary>
        /// Parsees all tokens returned by <paramref name="tokens"/> into an expression tree.
        /// </summary>
        /// <param name="parsers">The parsers to use</param>
        /// <param name="tokens">Enumerator returning all the token to parse</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The expression that represents the root of the syntax tree</returns>
        public Task<ITextTemplateSyntaxExpression> ParseAsync(IEnumerable<ITextTemplateExpressionParser> parsers, IAsyncEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context that contains the current state of the parser when creating the syntax tree.
    /// </summary>
    public interface ITextTemplateParserContext : IDisposable
    {
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
        public IReadOnlyList<ITextTemplateExpressionParser> Parsers { get; }  
        /// <summary>
        /// The root expression of the syntax tree.
        /// </summary>
        public SyntaxTreeRootExpression Root { get; }
        /// <summary>
        /// The current parent parser that is creating an expression. Will be null if the current parser is the root parser.
        /// </summary>
        public ITextTemplateExpressionParser? ParentParser { get; }
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
        /// <returns>Response: The response from the parsers|Parser the parser if response is either <see cref="ExpressionParserResponse.Interested"/> or <see cref="ExpressionParserResponse.CanParse"/></returns>
        public Task<(ExpressionParserResponse Response, ITextTemplateExpressionParser? Parser)> AreInterestedInAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses the current buffer into an expression(s) using <paramref name="parser"/>.
        /// </summary>
        /// <param name="parser">The parser to user</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Enumerator returning the parsed expressions</returns>
        public IAsyncEnumerable<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateExpressionParser parser, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a sub scope that can be used to use other parsers to create expressions.
        /// </summary>
        /// <param name="current">The instance creating the scope</param>
        /// <param name="scope">Optional scope to give parsers more context</param>
        /// <param name="bufferOffset">Used to skip the first n tokens in <see cref="Buffer"/> so sub context doesn't see them</param>
        /// <param name="bufferLimit">Limit the amount of token that can be read from <paramref name="bufferLimit"/> starting from <paramref name="bufferOffset"/></param>
        /// <param name="canReadNext">Indicates if the sub scope is allowed to read the next character</param>
        /// <returns>A sub scope that can be used to call parsers. Should be disposed once done</returns>
        public ITextTemplateParserContext CreateScope(ITextTemplateExpressionParser current, string? scope = null, int bufferOffset = 0, int? bufferLimit = null, bool canReadNext = true);
    }
}
