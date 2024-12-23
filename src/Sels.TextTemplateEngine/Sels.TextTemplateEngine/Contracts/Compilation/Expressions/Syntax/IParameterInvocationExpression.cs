using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// An <see cref="IInvocationSyntaxExpression"/> that is called with parameters of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of parameters the invocation is called with</typeparam>
    public interface IParameterInvocationExpression<T> : IInvocationSyntaxExpression
    {
        /// <summary>
        /// The parameters defined for the invocation.
        /// </summary>
        public IReadOnlyList<T> Parameters { get; }
    }
}
