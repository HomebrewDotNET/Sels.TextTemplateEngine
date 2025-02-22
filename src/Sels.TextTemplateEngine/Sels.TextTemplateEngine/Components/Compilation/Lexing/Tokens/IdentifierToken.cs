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
    public class IdentifierToken : ITextTemplateToken, ITextTemplateTypedToken
    {
        // Properties
        /// <summary>
        /// The characters that make up the identifier.
        /// </summary>
        public char[] Identifier { get; }
        /// <inheritdoc/>
        public int Length => Identifier.Length;
        /// <inheritdoc/>
        public static string TokenType => TextTemplateEngineConstants.Compilation.TokenTypes.Identifier;
        /// <inheritdoc/>
        public string Type => TokenType;

        /// <inheritdoc/>
        public TokenPosition Position { get; set; }
        /// <inheritdoc/>
        IEnumerable<char> ITextTemplateToken.TextValue => Identifier;

        /// <inheritdoc cref="IdentifierToken"/>
        /// <param name="identifier"><inheritdoc cref="Identifier"/></param>
        public IdentifierToken(IEnumerable<char> identifier)
        {
            Identifier = Guard.IsNotNullOrEmpty(identifier).ToArray();
        }
    }
}
