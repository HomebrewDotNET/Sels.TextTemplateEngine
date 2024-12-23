using Sels.Core.Extensions;
using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Expression that represents an accessor to get properties from an object.
    /// </summary>
    public class AccessorSyntaxExpression : ITextTemplateSyntaxExpression
    {
        // Fields
        private ITextTemplateToken[] _prefixTokens;
        private ITextTemplateToken[] _suffixTokens;

        // Properties
        /// <summary>
        /// The expression that the accessor will used to get the object to access the properties from.
        /// </summary>
        public ITextTemplateSyntaxExpression TargetExpression { get; }
        /// <summary>
        /// The (sub) properties to returns from the object returned by <see cref="TargetExpression"/>.
        /// </summary>
        public IReadOnlyList<PropertyAccessor> PropertyAccessors { get; }
        /// <inheritdoc/>
        string ITextTemplateSyntaxExpression.Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.Accessor;
        /// <inheritdoc/>
        IEnumerable<ITextTemplateToken> ITextTemplateSyntaxExpression.Tokens
        {
            get
            {
                foreach (var token in _prefixTokens)
                {
                    yield return token;
                }

                foreach (var token in TargetExpression.Tokens)
                {
                    yield return token;
                }

                foreach (var token in _suffixTokens)
                {
                    yield return token;
                }
            }
        }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression>? ITextTemplateSyntaxExpression.Children => new List<ITextTemplateSyntaxExpression>() { TargetExpression };

        /// <inheritdoc cref="AccessorSyntaxExpression"/>
        /// <param name="expression">The expression that's wrapped</param>
        /// <param name="prefixTokens">Any tokens defined before <paramref name="expression"/></param>
        /// <param name="suffixTokens">Any tokens defined after <paramref name="suffixTokens"/></param>
        public AccessorSyntaxExpression(ITextTemplateSyntaxExpression expression, IEnumerable<PropertyAccessor> accessors, IEnumerable<ITextTemplateToken> prefixTokens, IEnumerable<ITextTemplateToken> suffixTokens)
        {
            TargetExpression = Guard.IsNotNull(expression);
            expression.Parent = this;
            PropertyAccessors = Guard.IsNotNullOrEmpty(accessors).ToList();
            _prefixTokens = Guard.IsNotNullOrEmpty(prefixTokens).ToArray();
            _suffixTokens = Guard.IsNotNullOrEmpty(suffixTokens).ToArray();
        }
    }
}
