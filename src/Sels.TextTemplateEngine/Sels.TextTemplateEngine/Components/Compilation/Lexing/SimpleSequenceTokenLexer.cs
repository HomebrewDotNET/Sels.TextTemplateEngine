using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation;
using Sels.TextTemplateEngine.Templates.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing
{
    /// <summary>
    /// Lexer that can lex <typeparamref name="T"/> tokens from a sequence of characters.
    /// </summary>
    /// <typeparam name="T">The type of token to lex</typeparam>
    public class SimpleSequenceTokenLexer<T> : BaseSequenceTextTemplateTokenLexer
        where T : BaseSequenceToken, new()
    {
        // Properties
        /// <inheritdoc/>
        public override string Sequence { get; }
        /// <inheritdoc/>
        public override byte Priority { get; }

        /// <inheritdoc cref="SimpleSequenceTokenLexer{T}"/>
        /// <param name="sequence"><inheritdoc cref="Sequence"/></param>
        /// <param name="priority"><inheritdoc cref="Priority"/></param>
        public SimpleSequenceTokenLexer(string sequence, byte priority)
        {
            Sequence = Guard.IsNotNullOrWhitespace(sequence);
            Priority = priority;
        }

        /// <inheritdoc/>
        protected override Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, CancellationToken cancellationToken)
        => Task.FromResult<ITextTemplateToken>(new T() { Position = bufferMatchPosition, Line = context.Line });
    }
}
