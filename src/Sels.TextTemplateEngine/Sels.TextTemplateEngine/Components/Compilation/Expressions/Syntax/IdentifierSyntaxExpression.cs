using Sels.Core;
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
    public class IdentifierSyntaxExpression : IIdentifierTextTemplateSyntaxExpression
    {
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.SyntaxExpressionTypes.Identifier;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens { get; }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression>? Children => null;
        /// <inheritdoc/>
        public IReadOnlyCollection<char> Identifier { 
            get
            {
                return Tokens.SelectMany(x => x.TextValue).ToArray();
            } 
        }

        /// <inheritdoc cref="IdentifierSyntaxExpression"/>
        /// <param name="tokens"><inheritdoc cref="Tokens"/></param>
        public IdentifierSyntaxExpression(IEnumerable<ITextTemplateToken> tokens)
        {
            Tokens = Guard.IsNotNullOrEmpty(tokens).ToArray();
        }
    }
}
