using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Templates.Compilation
{
    /// <summary>
    /// Base class for creating a <see cref="ITextTemplateTokenLexer"/> that creates tokens from multiple sequences of characters.
    /// </summary>
    public abstract class BaseMultiSequencesTextTemplateTokenLexer : ITextTemplateTokenLexer
    {
        // Properties
        /// <summary>
        /// An array that represents the sequences of characters that this lexer tokenizes.
        /// </summary>
        public abstract string[] Sequences { get; }
        /// <inheritdoc/>
        public abstract byte Priority { get; }
        /// <inheritdoc/>
        public Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            var match = IsMatch(context);
            if (match.Response == TokenLexerResponse.CanLex)
            {
                return GenerateAsync(context, match.MatchingPosition, match.Sequence!, cancellationToken);
            }
            else throw new InvalidOperationException($"Can't generate token from current buffer. Response: {match.Response}");
        }
        /// <inheritdoc/>
        public Task<TokenLexerResponse> IsInterestedAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            return Task.FromResult(IsMatch(context).Response);
        }

        private (TokenLexerResponse Response, int MatchingPosition, string? Sequence) IsMatch(ITextTemplateLexerContext context)
        {
            context = Guard.IsNotNull(context);
            var sequences = Guard.IsNotNullOrEmpty(Sequences);
            bool isMatch = false;
            string? lastMatchingSequence = null;
            for (int i = 0; i < context.Buffer.Count; i++)
            {
                foreach(var sequence in sequences)
                {
                    for (int j = 0; j < sequence.Length && (i + j) < context.Buffer.Count; j++)
                    {
                        var bufferCharacter = context.Buffer.ElementAt(i + j);
                        var sequenceCharacter = sequence[j];
                        isMatch = bufferCharacter == sequenceCharacter;
                        if (!isMatch) break;
                        else if (j == sequence.Length - 1)
                        {
                            return (TokenLexerResponse.CanLex, context.BufferPosition + i, sequence); // Last token and still match so full sequence matched
                        }
                        lastMatchingSequence = sequence;
                    }
                }
                
            }
            if (isMatch) return (TokenLexerResponse.Interested, default, lastMatchingSequence);
            return (TokenLexerResponse.NotInterested, default, null);
        }

        /// <summary>
        /// Generates a token from the current buffer and character in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="bufferMatchPosition">The index in <see cref="ITextTemplateLexerContext.Buffer"/> where <paramref name="matchingSequence"/> starts matching</param>
        /// <param name="matchingSequence">The sequence that matched</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated token</returns>
        protected abstract Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, string matchingSequence, CancellationToken cancellationToken);
    }
}
