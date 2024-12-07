using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Token that represents a whitespace token.
    /// </summary>
    public class WhitespaceTextTemplateToken : BaseSequenceToken
    {
        // Properties
        /// <summary>
        /// The whitespace characters that make up the token.
        /// </summary>
        public override IReadOnlyList<char> Characters { get; }
        /// <inheritdoc/>
        public override string Type => TextTemplateEngineConstants.Compilation.TokenTypes.Text;

        /// <inheritdoc cref="WhitespaceTextTemplateToken"/>
        /// <param name="characters"><inheritdoc cref="Characters"/></param>
        /// <param name="position"><inheritdoc cref="Position"/></param>
        /// <param name="line"><inheritdoc cref="Line"/></param>
        public WhitespaceTextTemplateToken(IEnumerable<char> characters)
        {
            Characters = Guard.IsNotNullOrEmpty(characters).ToList();
        }
    }
}
