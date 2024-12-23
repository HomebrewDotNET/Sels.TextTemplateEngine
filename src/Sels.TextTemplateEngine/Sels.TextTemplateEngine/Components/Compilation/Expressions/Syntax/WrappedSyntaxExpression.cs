using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Expression that wraps another expression.
    /// </summary>
    public class WrappedSyntaxExpression : ITextTemplateSyntaxExpression
    {
        // Fields
        private ITextTemplateToken[]? _prefixTokens;
        private ITextTemplateToken[]? _suffixTokens;

        // Properties
        /// <summary>
        /// The expression that's wrapped.
        /// </summary>
        public ITextTemplateSyntaxExpression Expression { get; }
        /// <inheritdoc/>
        string ITextTemplateSyntaxExpression.Type => TextTemplateEngineConstants.Compilation.SyntaxExpressionTypes.Wrapped;
        /// <inheritdoc/>
        IEnumerable<ITextTemplateToken> ITextTemplateSyntaxExpression.Tokens
        {
            get
            {
                if (_prefixTokens.HasValue())
                {
                    foreach (var token in _prefixTokens!)
                    {
                        yield return token;
                    }
                }

                foreach (var token in Expression.Tokens)
                {
                    yield return token;
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
        IReadOnlyList<ITextTemplateSyntaxExpression>? ITextTemplateSyntaxExpression.Children => new List<ITextTemplateSyntaxExpression>() { Expression };

        /// <inheritdoc cref="WrappedSyntaxExpression"/>
        /// <param name="expression">The expression that's wrapped</param>
        /// <param name="prefixTokens">Any tokens defined before <paramref name="expression"/></param>
        /// <param name="suffixTokens">Any tokens defined after <paramref name="suffixTokens"/></param>
        public WrappedSyntaxExpression(ITextTemplateSyntaxExpression expression, IEnumerable<ITextTemplateToken>? prefixTokens, IEnumerable<ITextTemplateToken>? suffixTokens)
        {
            Expression = Guard.IsNotNull(expression);
            expression.Parent = this;
            _prefixTokens = prefixTokens.HasValue() ? prefixTokens!.ToArray() : null;
            _suffixTokens = suffixTokens.HasValue() ? suffixTokens!.ToArray() : null;
        }
    }
}
