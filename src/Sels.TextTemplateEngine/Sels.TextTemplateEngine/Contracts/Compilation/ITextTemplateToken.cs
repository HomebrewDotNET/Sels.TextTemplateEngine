using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Represents a token lexed by a <see cref="ITextTemplateLexer"/> that is used by a <see cref="ITextTemplateParser"/> to produce <see cref="ITextTemplateExpression"/>(s).
    /// </summary>
    public interface ITextTemplateToken
    {
        /// <summary>
        /// The type of the token.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Contains information about the position of the token in the source stream.
        /// </summary>
        public TokenPosition Position { get; }
        /// <summary>
        /// Enumerates the characters that represents the token in text form. Doesn't have to match the characters in the source stream for example when dealing with escape characters.
        /// </summary>
        public IEnumerable<char> TextValue { get; }
        /// <summary>
        /// The length of the characters from the source string that was used to create the token.
        /// </summary>
        public int Length { get; }
    }
}
