using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Contains information about the position of a token in the source stream.
    /// </summary>
    public struct TokenPosition
    {
        /// <summary>
        /// The index of the first character in the token from the source stream.
        /// </summary>
        public int Index { get; init; }
        /// <summary>
        /// The line of the token in the source stream.
        /// </summary>
        public int Line { get; init; }
        /// <summary>
        /// The index of the token in line <see cref="Index"/>
        /// </summary>
        public int LineIndex { get; init; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Index: {Index}|Position {LineIndex} on Line {Line}";
        }
    }
}
