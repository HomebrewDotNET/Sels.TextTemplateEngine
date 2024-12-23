using Sels.Core;
using Sels.Core.Extensions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Expression that contains a group of expressions and operator expressions that produce a single result.
    /// </summary>
    public class OperatorGroupSyntaxExpression : ITextTemplateSyntaxExpression
    {
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.TokenTypes.OperationGroup;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens { 
            get
            {
                foreach(var expression in Children!)
                {
                    foreach (var token in expression.Tokens)
                    {
                        yield return token;
                    }
                }
            } 
        }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression>? Children { get; }

        /// <inheritdoc cref="OperatorGroupSyntaxExpression"/>
        /// <param name="expressions">The expressions that make up this group</param>
        public OperatorGroupSyntaxExpression(IEnumerable<ITextTemplateSyntaxExpression> expressions)
        {
            Children = Guard.IsNotNullOrEmpty(Guard.IsNotNull(expressions).ToList());
            Children.Execute(x => x.Parent = this);
        }
    }
}
