using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing
{
    /// <summary>
    /// Lexer that can lex <typeparamref name="T"/> tokens from a recurring sequence of characters.
    /// </summary>
    /// <typeparam name="T">The type of the token to create</typeparam>
    public class SimpleRecurringMatchingSetLexer<T> : BaseRecurringMatchingSetTextTemplateTokenLexer
        where T : ITextTemplateToken
    {
        // Fields
        private readonly Func<char, Task<bool>> _isMatch;
        private readonly Func<ITextTemplateLexerContext, int, char[], CancellationToken, Task<T>> _generate;
        private readonly byte _priority;

        // Properties
        /// <inheritdoc/>
        public override byte Priority => _priority;

        /// <inheritdoc cref="SimpleRecurringMatchingSetLexer{T}"/>
        /// <param name="match">Delegate that matches the signiture of <see cref="BaseRecurringMatchingSetTextTemplateTokenLexer.IsMatch(char)"/></param>
        /// <param name="generate">Delegate that matches the signiture of <see cref="BaseRecurringMatchingSetTextTemplateTokenLexer.GenerateAsync(ITextTemplateLexerContext, int, char[], CancellationToken)"/></param>
        /// <param name="priority"><inheritdoc cref="ITextTemplateTokenLexer.Priority"/></param>
        public SimpleRecurringMatchingSetLexer(Func<char, Task<bool>> match, Func<ITextTemplateLexerContext, int, char[], CancellationToken, Task<T>> generate, byte priority)
        {
            _isMatch = Guard.IsNotNull(match);
            _generate = Guard.IsNotNull(generate);
            _priority = priority;
        }

        /// <inheritdoc cref="SimpleRecurringMatchingSetLexer{T}"/>
        /// <param name="match">Delegate that checks if the character matches the set</param>
        /// <param name="generate">Delegate that creates the token using the <see cref="ITextTemplateLexerContext"/>, the position of the first character in the set in the stream and the matching set</param>
        /// <param name="priority"><inheritdoc cref="ITextTemplateTokenLexer.Priority"/></param>
        public SimpleRecurringMatchingSetLexer(Predicate<char> match, Func<ITextTemplateLexerContext, int, char[], T> generate, byte priority)
        {
            match = Guard.IsNotNull(match);
            generate = Guard.IsNotNull(generate);
            _isMatch = x => Task.FromResult(match(x));
            _generate = (context, bufferMatchPosition, recurringSet, cancellationToken) => Task.FromResult(generate(context, bufferMatchPosition, recurringSet));
            _priority = priority;
        }

        // <inheritdoc/>
        public override Task<bool> IsMatch(char character)
        => _isMatch(character);
        // <inheritdoc/>
        protected override async Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, char[] recurringSet, CancellationToken cancellationToken)
        => await _generate(context, bufferMatchPosition, recurringSet, cancellationToken).ConfigureAwait(false);
    }
}
