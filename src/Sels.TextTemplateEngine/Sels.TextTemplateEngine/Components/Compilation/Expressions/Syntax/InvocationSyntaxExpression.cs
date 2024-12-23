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
    /// Expression that consists of a <see cref="IdentifierSyntaxExpression"/> wrapped between a <see cref="TextTemplateEngineConstants.Compilation.TokenTypes.StartExpression"/> and <see cref="TextTemplateEngineConstants.Compilation.TokenTypes.EndExpression"/>  with optionally <see cref="TextTemplateEngineConstants.Compilation.TokenTypes.Whitespace"/> tokens in between.
    /// </summary>
    public class InvocationSyntaxExpression : IInvocationSyntaxExpression
    {
        // Fields
        private readonly ITextTemplateToken[] _prefixTokens;
        private readonly ITextTemplateToken[] _suffixTokens;

        // Properties
        /// <inheritdoc/>
        public IIdentifierTextTemplateSyntaxExpression Identifier { get; }
        /// <inheritdoc/>
        string ITextTemplateSyntaxExpression.Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.Invocation;
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

                foreach (var token in Identifier.Tokens)
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

                if (Body != null)
                {
                    foreach (var token in Body.Tokens)
                    {
                        yield return token;
                    }
                }
            }
        }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression>? ITextTemplateSyntaxExpression.Children => Helper.Collection.Enumerate<ITextTemplateSyntaxExpression>(Identifier, Body!).Where(x => x != null).ToList();
        /// <inheritdoc/>
        public ITemplateBodySyntaxExpression? Body { get; set; }

        /// <inheritdoc cref="InvocationSyntaxExpression"/>
        /// <param name="expression"><see cref="Identifier"/></param>
        /// <param name="prefixTokens">Any tokens defined before <paramref name="expression"/></param>
        /// <param name="suffixTokens">Any tokens defined after <paramref name="expression"/></param>
        public InvocationSyntaxExpression(IIdentifierTextTemplateSyntaxExpression expression, IEnumerable<ITextTemplateToken>? prefixTokens, IEnumerable<ITextTemplateToken>? suffixTokens)
        {
            Identifier = Guard.IsNotNull(expression);
            expression.Parent = this;
            _prefixTokens = Guard.IsNotNull(prefixTokens).ToArray();
            _suffixTokens = Guard.IsNotNull(suffixTokens).ToArray();
        }
    }
}
