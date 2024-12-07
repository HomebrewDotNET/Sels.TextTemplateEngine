using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Called by a <see cref="ITextTemplateLexer"/> to see if the token lexer is interested in the current buffer and character. 
    /// If it is the lexer will call <see cref="ITextTemplateTokenLexer.GenerateAsync(ITextTemplateLexerContext, bool, CancellationToken)"/> to get the token.
    /// </summary>
    public interface ITextTemplateTokenLexer
    {
        /// <summary>
        /// Determines the order in which <see cref="ITextTemplateTokenLexer"/> will call all token lexers.
        /// The first lexer that returns <see cref="TokenLexerResponse.CanParse"/> will be used to parse the token.
        /// </summary>
        public byte Priority { get; }

        /// <summary>
        /// Checks if the token lexer is interested in the current buffer and character.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="isLastCharacter">Indicates if we are currently checking the last character in the stream</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The response of the token lexer</returns>
        public Task<TokenLexerResponse> IsInterestedAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken);
        /// <summary>
        /// Generates a token from the current buffer and character in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="isLastCharacter">Indicates if we are currently checking the last character in the stream</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated token</returns>
        public Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Response from a <see cref="ITextTemplateTokenLexer"/> to indicate if the lexer is interested in the current buffer and character.
    /// </summary>
    public enum TokenLexerResponse
    {
        /// <summary>
        /// Not interested in the current buffer and character.
        /// </summary>
        NotInterested = 0,
        /// <summary>
        /// Interested in the current buffer and character but can't parse it yet.
        /// </summary>
        Interested = 1,
        /// <summary>
        /// Can lext the current buffer and character into a token.
        /// </summary>
        CanLex = 2
    }
}
