using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions
{
    /// <summary>
    /// Expression that represents a section of static characters in a template.
    /// </summary>
    public class TextExpression : ITextTemplateSyntaxExpression
    {
        /// <inheritdoc/>
        string ITextTemplateSyntaxExpression.Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.Text;
        /// <inheritdoc/>
        public IReadOnlyCollection<ITextTemplateToken> Tokens { get; }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression> Children { get; } = [];

        /// <inheritdoc cref="TextExpression"/>
        /// <param name="tokens"><inheritdoc cref="Tokens"/></param>
        /// <param name="parent"><inheritdoc cref="Parent"/></param>
        public TextExpression(IEnumerable<ITextTemplateToken> tokens, ITextTemplateSyntaxExpression? parent = null)
        {
            Tokens = Guard.IsNotNullOrEmpty(tokens).ToList();
            Parent = parent;
        }
    }
}
