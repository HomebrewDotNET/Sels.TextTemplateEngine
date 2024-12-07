using Sels.Core;
using Sels.TextTemplateEngine.Compilation.Lexing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Templates.Compilation
{
    /// <summary>
    /// Base class for creating a <see cref="ITextTemplateTokenLexer"/> that creates tokens from a recurring sequence of characters.
    /// </summary>
    public abstract class BaseRecurringMatchingSetTextTemplateTokenLexer : ITextTemplateTokenLexer
    {
        // Properties
        /// <inheritdoc/>
        public abstract byte Priority { get; }

        /// <inheritdoc/>
        public async Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            for (int i = 0; i < context.Buffer.Count; i++)
            {
                var character = context.Buffer.ElementAt(i);
                var startPosition = i;
                if (await IsMatch(context.Buffer.ElementAt(i)).ConfigureAwait(false))
                {
                    var whitespaceCharacters = new List<char>() { character };
                    while (startPosition + 1 < context.Buffer.Count)
                    {
                        var nextCharacter = context.Buffer.ElementAt(startPosition + 1);
                        if (await IsMatch(nextCharacter).ConfigureAwait(false))
                        {
                            whitespaceCharacters.Add(nextCharacter);
                            startPosition++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return await GenerateAsync(context, context.BufferPosition + i, whitespaceCharacters.ToArray(), cancellationToken).ConfigureAwait(false);
                }

            }
            throw new InvalidOperationException($"No matching character in buffer");
        }
        /// <inheritdoc/>
        public async Task<TokenLexerResponse> IsInterestedAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            context = Guard.IsNotNull(context);

            for (int i = 0; i < context.Buffer.Count; i++)
            {
                if (await IsMatch(context.Buffer.ElementAt(i)).ConfigureAwait(false))
                {
                    int startPosition = i+1;
                    while(startPosition < context.Buffer.Count)
                    {
                        if (await IsMatch(context.Buffer.ElementAt(startPosition)).ConfigureAwait(false))
                        {
                            startPosition++;
                        }
                        else // Non matching so we can lex
                        {
                            return TokenLexerResponse.CanLex;
                        }
                    }

                    if(context.IsLastCharacter) // If we are at the end of the stream we can lex
                    {
                        return TokenLexerResponse.CanLex;
                    }

                    return TokenLexerResponse.Interested;
                }
            }

            return TokenLexerResponse.NotInterested;
        }
        /// <summary>
        /// Checks if <paramref name="character"/> is a match for the lexer.
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if matched, otherwise false</returns>
        public abstract Task<bool> IsMatch(char character);
        /// <summary>
        /// Generates a token from the current buffer and character in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="isLastCharacter">Indicates if we are currently checking the last character in the stream</param>
        /// <param name="bufferMatchPosition">The index in <see cref="ITextTemplateLexerContext.Buffer"/> where <paramref name="recurringSet"/> starts matching</param>
        /// <param name="recurringSet">The set that matched</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated token</returns>
        protected abstract Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, char[] recurringSet, CancellationToken cancellationToken);
    }
}
