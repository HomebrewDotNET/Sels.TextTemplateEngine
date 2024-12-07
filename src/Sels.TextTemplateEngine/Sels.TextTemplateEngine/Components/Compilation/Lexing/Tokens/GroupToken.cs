using Sels.TextTemplateEngine.Templates.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Token that represents the start of a group expression.
    /// </summary>
    public class GroupToken : BaseSequenceToken
    {
        // Statics
        private static char[] _characters = TextTemplateEngineConstants.Compilation.Syntax.ExpressionGroupToken.ToCharArray();

        // Properties
        /// <summary>
        /// The characters that make up the token.
        /// </summary>
        public override IReadOnlyList<char> Characters => _characters;
        /// <inheritdoc/>
        public override string Type => TextTemplateEngineConstants.Compilation.TokenTypes.Group;
    }
}
