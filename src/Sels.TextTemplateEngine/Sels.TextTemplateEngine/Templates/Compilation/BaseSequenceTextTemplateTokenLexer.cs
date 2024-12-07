using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Templates.Compilation
{
    /// <summary>
    /// Base class for creating a <see cref="ITextTemplateTokenLexer"/> that creates token from a sequence of characters.
    /// </summary>
    public abstract class BaseSequenceTextTemplateTokenLexer : BaseMultiSequencesTextTemplateTokenLexer
    {
        // Properties
        /// <summary>
        /// The string that represents the sequence of characters that this lexer tokenizes.
        /// </summary>
        public abstract string Sequence { get; }
        
        /// <inheritdoc/>
        public override string[] Sequences => new string[]{ Sequence };
        /// <inheritdoc/>
        protected override Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, string matchingSequence, CancellationToken cancellationToken)
         => GenerateAsync(context, bufferMatchPosition, cancellationToken);

        /// <summary>
        /// Generates a token from the current buffer and character in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="bufferMatchPosition">The index in <see cref="ITextTemplateLexerContext.Buffer"/> where <see cref="Sequence"/> starts matching</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated token</returns>
        protected abstract Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, CancellationToken cancellationToken);
    }
}
