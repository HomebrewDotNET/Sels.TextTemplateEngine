using Sels.TextTemplateEngine.Templates.Compilation.Lexing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Token that represents the start of a method named parameter or group.
    /// </summary>
    public class InvocationParameterOrGroupStartToken : BaseSequenceToken, ITextTemplateTypedToken
    {
        // Statics
        private static char[] _characters = TextTemplateEngineConstants.Compilation.Syntax.InvocationParameterOrGroupStart.ToCharArray();

        // Properties
        /// <summary>
        /// The characters that make up the token.
        /// </summary>
        public override IReadOnlyList<char> Characters => _characters;
        /// <inheritdoc/>
        public static string TokenType => TextTemplateEngineConstants.Compilation.TokenTypes.InvocationParameterOrGroupStart;
        /// <inheritdoc/>
        public override string Type => TokenType;
    }
}
