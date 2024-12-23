using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <inheritdoc cref="TextTemplateEngineConstants.Compilation.ExpressionTypes.NamedParameter"/>
    public interface INamedParameterTextTemplateSyntaxExpression : ITextTemplateSyntaxExpression
    {
        /// <summary>
        /// The identifier that makes up the expression.
        /// </summary>
        public IIdentifierTextTemplateSyntaxExpression Name { get; }
        /// <summary>
        /// The expression that forms the argument for the parameter.
        /// </summary>
        public ITextTemplateSyntaxExpression Argument { get; }
    }
}
