using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Expressions.Syntax
{
    /// <summary>
    /// Expression that represents the root of an expression tree.
    /// </summary>
    public class SyntaxTreeRootExpression : ITextTemplateSyntaxExpression
    {
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.SyntaxExpressionTypes.Root;
        /// <summary>
        /// Expressions that are part of the root expression.
        /// </summary>
        public List<ITextTemplateSyntaxExpression> Expressions { get; } = new List<ITextTemplateSyntaxExpression>();
        /// <inheritdoc/>
        IReadOnlyCollection<ITextTemplateToken> ITextTemplateSyntaxExpression.Tokens { get; } = new List<ITextTemplateToken>();
        /// <inheritdoc/>
        ITextTemplateSyntaxExpression? ITextTemplateSyntaxExpression.Parent => null;
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression> ITextTemplateSyntaxExpression.Children => Expressions;
    }
}
