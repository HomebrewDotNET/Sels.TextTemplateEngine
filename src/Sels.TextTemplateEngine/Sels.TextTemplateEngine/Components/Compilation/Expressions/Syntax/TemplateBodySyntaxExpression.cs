using Sels.Core.Extensions;
using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sels.Core.Extensions.Linq;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <inheritdoc cref="TextTemplateEngineConstants.Compilation.ExpressionTypes.TemplateBody"/>
    public class TemplateBodySyntaxExpression : ITemplateBodySyntaxExpression
    {
        // Fields
        private ITextTemplateToken[]? _prefixTokens;
        private ITextTemplateToken[]? _suffixTokens;

        // Properties
        /// <inheritdoc/>
        public virtual string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.TemplateBody;
        /// <summary>
        /// Expressions that are part of the template body.
        /// </summary>
        public List<ITextTemplateSyntaxExpression> Expressions { get; } = new List<ITextTemplateSyntaxExpression>();
        /// <inheritdoc/>
        IEnumerable<ITextTemplateToken> ITextTemplateSyntaxExpression.Tokens
        {
            get
            {
                if(_prefixTokens.HasValue())
                {
                    foreach (var token in _prefixTokens!)
                    {
                        yield return token;
                    }
                }

                foreach (var expression in Expressions)
                {
                    foreach (var token in expression.Tokens)
                    {
                        yield return token;
                    }
                }

                if (_suffixTokens.HasValue())
                {
                    foreach (var token in _suffixTokens!)
                    {
                        yield return token;
                    }
                }
            }
        }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression> ITextTemplateSyntaxExpression.Children => Expressions;

        /// <inheritdoc cref="TemplateBodySyntaxExpression"/>
        /// <param name="expressions">Expressions that are part of the template body</param>
        /// <param name="prefixTokens">Any tokens defined before but not part of <see cref="Expressions"/></param>
        /// <param name="suffixTokens">Any tokens defined after but not part of <see cref="Expressions"/></param>
        public TemplateBodySyntaxExpression(IEnumerable<ITextTemplateSyntaxExpression>? expressions = null, IEnumerable<ITextTemplateToken>? prefixTokens = null, IEnumerable<ITextTemplateToken>? suffixTokens = null)
        {
            if(expressions != null) Expressions.AddRange(expressions.Execute(x => x.Parent = this));
            _prefixTokens = prefixTokens?.ToArray();
            _suffixTokens = suffixTokens?.ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(x => x.ToString()!);
        }
        /// <summary>
        /// Converts the expression to a string representation.
        /// </summary>
        /// <param name="toString">The delegate used to convert expression in the tree to their string representation</param>
        /// <returns>Text representation of the current syntax tree</returns>
        public virtual string ToString(Func<ITextTemplateSyntaxExpression, string> toString)
        {
            toString = Guard.IsNotNull(toString);
            var builder = new StringBuilder();
            WriteExpression(builder, this, x =>
            {
                if (x == this) return "SyntaxTree";
                return toString(x);
            });
            return builder.ToString();
        }

        private static void WriteExpression(StringBuilder builder, ITextTemplateSyntaxExpression expression, Func<ITextTemplateSyntaxExpression, string> toString, int depth = 0)
        {
            builder = Guard.IsNotNull(builder);
            expression = Guard.IsNotNull(expression);
            toString = Guard.IsNotNull(toString);
            builder.Append(' ', depth);
            builder.AppendLine(toString(expression));
            if (expression.Children.HasValue())
            {
                foreach (var child in expression.Children!)
                {
                    WriteExpression(builder, child, toString, depth + 1);
                }
            }
        }
    }
}
