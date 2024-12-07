using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Called by a <see cref="ITextTemplateParser"/> or another <see cref="ITextTemplateExpressionParser"/> to see if the parser can create an expression using the current token buffer.
    /// If it is the parser will call <see cref="ITextTemplateExpressionParser.ParseAsync(ITextTemplateParserContext, CancellationToken)"/> to get the token.
    /// </summary>
    public interface ITextTemplateExpressionParser
    {
        /// <summary>
        /// Determines the order in which <see cref="ITextTemplateParserContext"/> will call all expression parsers.
        /// The first parser that returns <see cref="ExpressionParserResponse.CanParse"/> will be used to parse the token.
        /// </summary>
        public byte Priority { get; }
        /// <summary>
        /// Checks if the expression parser is interested in the current buffer and token.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The response from the parser</returns>
        public Task<ExpressionParserResponse> IsInterestedAsync(ITextTemplateParserContext context, CancellationToken cancellationToken);
        /// <summary>
        /// Generates an expression from the current buffer and token in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated expression</returns>
        public Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, CancellationToken cancellationToken);
    }
    /// <summary>
    /// Response from a <see cref="ITextTemplateExpressionParser"/> to indicate if the parser is interested in the current buffer and character.
    /// </summary>
    public enum ExpressionParserResponse
    {
        /// <summary>
        /// Not interested in the current buffer and character.
        /// </summary>
        NotInterested = 0,
        /// <summary>
        /// Interested in the current buffer and character but can't parse it yet.
        /// </summary>
        Interested = 1,
        /// <summary>
        /// Can parse the current buffer and character into an expression. Might read the next tokens in the buffer.
        /// </summary>
        CanParse = 2
    }
}
