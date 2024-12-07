using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Token that represents an identifier.
    /// </summary>
    public class IdentifierToken : ITextTemplateToken
    {
        // Properties
        /// <summary>
        /// The characters that make up the identifier.
        /// </summary>
        public char[] Identifier { get; }
        /// <inheritdoc/>
        public int Position { get; init; }
        /// <inheritdoc/>
        public int Length => Identifier.Length;
        /// <inheritdoc/>
        public int Line { get; init; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.TokenTypes.Identifier;

        /// <inheritdoc cref="IdentifierToken"/>
        /// <param name="identifier"><inheritdoc cref="Identifier"/></param>
        public IdentifierToken(IEnumerable<char> identifier)
        {
            Identifier = Guard.IsNotNullOrEmpty(identifier).ToArray();
        }
    }
}
