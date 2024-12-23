using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Expression that represents an invocation.
    /// </summary>
    public interface IInvocationSyntaxExpression : ITextTemplateSyntaxExpression
    {
        /// <summary>
        /// Identifier pointing to what to invoke.
        /// </summary>
        public IIdentifierTextTemplateSyntaxExpression Identifier { get; }
        /// <summary>
        /// The body for the invocation if defined.
        /// </summary>
        public ITemplateBodySyntaxExpression? Body { get; set; }
    }
}
