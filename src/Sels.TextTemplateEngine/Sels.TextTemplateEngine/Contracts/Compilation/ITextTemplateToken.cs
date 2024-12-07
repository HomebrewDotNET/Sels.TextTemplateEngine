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
        /// The index of the token in source stream.
        /// </summary>
        public int Position { get; }
        /// <summary>
        /// The length of the characters from the source string that was used to create the token.
        /// </summary>
        public int Length { get; }
        /// <summary>
        /// The line of the token in the source stream.
        /// </summary>
        public int Line { get; }
    }
}
