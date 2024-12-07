using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions
{
    /// <summary>
    /// Expression that represents a comment.
    /// </summary>
    public class CommentExpression : ITextTemplateSyntaxExpression
    {
        /// <summary>
        /// The expression that contains the text of the comment.
        /// </summary>
        public TextExpression Text { get; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.Comment;
        /// <inheritdoc/>
        public IReadOnlyCollection<ITextTemplateToken> Tokens { get; }
        /// <inheritdoc/>
        ITextTemplateSyntaxExpression? ITextTemplateSyntaxExpression.Parent => null;
        /// <inheritdoc/>
        IReadOnlyList<ITextTemplateSyntaxExpression> ITextTemplateSyntaxExpression.Children => [];

        /// <inheritdoc cref="CommentExpression"/>
        /// <param name="tokens"><inheritdoc cref="Tokens"/></param>
        /// <param name="text"><inheritdoc cref="Text"/></param>
        public CommentExpression(IEnumerable<ITextTemplateToken> tokens, TextExpression text)
        {
            Tokens = Guard.IsNotNullOrEmpty(tokens).ToList();
            Text = Guard.IsNotNull(text);
        }
    }
}
