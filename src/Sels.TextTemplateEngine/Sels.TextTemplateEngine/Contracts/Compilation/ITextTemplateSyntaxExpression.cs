using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Represents an expression part of the syntax tree that was created by a <see cref="ITextTemplateParser"/> from <see cref="ITextTemplateToken"/>(s).
    /// </summary>
    public interface ITextTemplateSyntaxExpression
    {
        /// <summary>
        /// The type of expression.
        /// </summary>
        public string Type { get; }
        /// <summary>
        /// The tokens that were used to create the current epxression.
        /// </summary>
        public IReadOnlyCollection<ITextTemplateToken> Tokens { get; }
        /// <summary>
        /// The parent expression if the current expression isn't the root expression.
        /// </summary>
        public ITextTemplateSyntaxExpression? Parent { get; }
        /// <summary>
        /// Any child expressions that are part of the current expression. Can be used to traverse the syntax tree.
        /// </summary>
        public IReadOnlyList<ITextTemplateSyntaxExpression> Children { get; }
    }
}
