using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Expression that forms an identifier consiting of one or more tokens.
    /// </summary>
    public interface IIdentifierTextTemplateSyntaxExpression : ITextTemplateSyntaxExpression
    {
        // Properties
        /// <summary>
        /// The identifier that makes up the expression.
        /// </summary>
        public IReadOnlyCollection<char> Identifier { get; }
    }
}
