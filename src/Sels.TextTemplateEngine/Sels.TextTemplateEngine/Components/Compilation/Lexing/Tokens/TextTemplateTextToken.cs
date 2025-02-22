using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation.Lexing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// A token that contains a series of characters.
    /// </summary>
    public class TextTemplateTextToken : BaseSequenceToken, ITextTemplateTypedToken
    {
        // Properties
        /// <summary>
        /// The characters that make up the token.
        /// </summary>
        public override IReadOnlyList<char> Characters { get; }
        /// <inheritdoc/>
        public static string TokenType => TextTemplateEngineConstants.Compilation.TokenTypes.Text;
        /// <inheritdoc/>
        public override string Type => TokenType;

        /// <inheritdoc cref="TextTemplateTextToken"/>
        /// <param name="characters"><inheritdoc cref="Characters"/></param>
        /// <param name="position"><inheritdoc cref="Position"/></param>
        /// <param name="line"><inheritdoc cref="Line"/></param>
        public TextTemplateTextToken(IEnumerable<char> characters)
        {
            Characters = Guard.IsNotNullOrEmpty(characters).ToList();
        }
    }
}
