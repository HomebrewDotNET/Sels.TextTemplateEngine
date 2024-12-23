using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Templates.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Base class for all tokens that are lexed from a sequence of characters.
    /// </summary>
    public abstract class BaseSequenceToken : ITextTemplateToken
    {
        // Properties
        /// <summary>
        /// The characters that make up the token.
        /// </summary>
        public abstract IReadOnlyList<char> Characters { get; }
        /// <inheritdoc/>
        public abstract string Type { get; }
        /// <inheritdoc/>
        public int Length => Characters.Count;
        /// <inheritdoc/>
        public TokenPosition Position { get; init; }
        /// <inheritdoc/>
        IEnumerable<char> ITextTemplateToken.TextValue => Characters;
    }
}
