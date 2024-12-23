using Newtonsoft.Json.Linq;
using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions
{
    /// <summary>
    /// Expression that acesses a variable from the current context and returns it's value.
    /// </summary>
    public class VariableExpression : ITextTemplateSyntaxExpression
    {
        // Properties
        /// <summary>
        /// Token that contains the name of the variable.
        /// </summary>
        public ITextTemplateToken IdentifierToken { get; }
        /// <inheritdoc/>
        string ITextTemplateSyntaxExpression.Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.Variable;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens { get; }
        /// <inheritdoc/>
        ITextTemplateSyntaxExpression? ITextTemplateSyntaxExpression.Parent { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression>? ITextTemplateSyntaxExpression.Children => null;

        /// <inheritdoc cref="VariableExpression"/>
        /// <param name="identifierToken"><see cref="IdentifierToken"/></param>
        /// <param name="fullTokens"><see cref="ITextTemplateSyntaxExpression.Tokens"/></param>
        public VariableExpression(ITextTemplateToken identifierToken, IEnumerable<ITextTemplateToken>? fullTokens = null)
        {
            IdentifierToken = Guard.IsNotNull(identifierToken);
            Tokens = fullTokens.HasValue() ? fullTokens! : new List<ITextTemplateToken> { identifierToken };
        }
    }
}
