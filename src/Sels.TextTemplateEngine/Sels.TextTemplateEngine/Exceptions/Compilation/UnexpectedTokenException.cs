using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Thrown when a parser encounters a token that it did not expect.
    /// </summary>
    public class UnexpectedTokenException : TextTemplateCompilationException
    {
        // Properties
        /// <summary>
        /// The parser that read the token.
        /// </summary>
        public ITextTemplateSyntaxExpressionParser Parser { get; }
        /// <summary>
        /// The token that was unexpected.
        /// </summary>
        public ITextTemplateToken UnexpectedToken { get; }
        /// <summary>
        /// Optional array of token types that were expected instead of <see cref="UnexpectedToken"/>
        /// </summary>
        public string[]? ExpectedTokenTypes { get; }

        /// <inheritdoc cref="UnexpectedTokenException"/>
        /// <param name="parser"><inheritdoc cref="Parser"/></param>
        /// <param name="unexpectedToken"><inheritdoc cref="UnexpectedToken"/></param>
        /// <param name="expectedTokenTypes"><inheritdoc cref="ExpectedTokenTypes"/></param>
        public UnexpectedTokenException(ITextTemplateSyntaxExpressionParser parser, ITextTemplateToken unexpectedToken, IEnumerable<string>? expectedTokenTypes) : base($"Unexpected token <{unexpectedToken.Type}> at position <{unexpectedToken.Position}>{(expectedTokenTypes.HasValue() ? $". Expected token of type(s) <{expectedTokenTypes.JoinString(',')}>" : String.Empty)}")
        {
            Parser = Guard.IsNotNull(parser);
            UnexpectedToken = Guard.IsNotNull(unexpectedToken);
            ExpectedTokenTypes = expectedTokenTypes?.ToArray();
        }

        /// <inheritdoc cref="UnexpectedTokenException"/>
        /// <param name="parser"><inheritdoc cref="Parser"/></param>
        /// <param name="unexpectedToken"><inheritdoc cref="UnexpectedToken"/></param>
        /// <param name="expectedTokenTypes"><inheritdoc cref="ExpectedTokenTypes"/></param>
        public UnexpectedTokenException(ITextTemplateSyntaxExpressionParser parser, ITextTemplateToken unexpectedToken, params string[] expectedTokenTypes) : this(parser, unexpectedToken, (IEnumerable<string>?) expectedTokenTypes)
        {
        }
    }
}
