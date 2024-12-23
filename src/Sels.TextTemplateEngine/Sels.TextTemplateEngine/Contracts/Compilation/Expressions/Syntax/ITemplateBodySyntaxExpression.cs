using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <inheritdoc cref="TextTemplateEngineConstants.Compilation.ExpressionTypes.TemplateBody"/>
    public interface ITemplateBodySyntaxExpression : ITextTemplateSyntaxExpression
    {
        /// <summary>
        /// Expressions that are part of the template body.
        /// </summary>
        public List<ITextTemplateSyntaxExpression> Expressions { get; }
    }
}
