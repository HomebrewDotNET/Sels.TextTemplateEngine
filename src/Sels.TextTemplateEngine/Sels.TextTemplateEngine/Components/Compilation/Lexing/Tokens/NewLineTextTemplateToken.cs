using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation;
using Sels.TextTemplateEngine.Templates.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing.Tokens
{
    /// <summary>
    /// Token that represents a new line.
    /// </summary>
    public class NewLineTextTemplateToken : BaseSequenceToken
    {
        // Properties
        /// <summary>
        /// The characters that make up the token.
        /// </summary>
        public override IReadOnlyList<char> Characters { get; }
        /// <inheritdoc/>
        public override string Type => TextTemplateEngineConstants.Compilation.TokenTypes.NewLine;

        /// <inheritdoc cref="NewLineTextTemplateToken"/>
        /// <param name="characters"><inheritdoc cref="Characters"/></param>
        /// <param name="position"><inheritdoc cref="Position"/></param>
        /// <param name="line"><inheritdoc cref="Line"/></param>
        public NewLineTextTemplateToken(IEnumerable<char> characters)
        {
            Characters = Guard.IsNotNullOrEmpty(characters).ToList();
        }
    }
    /// <summary>
    /// Lexer that creates <see cref="NewLineTextTemplateToken"/> from <see cref="Environment.NewLine"/>.
    /// </summary>
    public class NewLineTextTemplateTokenLexer : BaseSequenceTextTemplateTokenLexer
    {
        // Statics
        private static readonly char[] _newLineCharacters = Environment.NewLine.ToCharArray();
        /// <inheritdoc cref="Priority"/>
        public static byte LexerPriority => 10;

        // Properties
        /// <inheritdoc/>
        public override string Sequence => Environment.NewLine;
        /// <inheritdoc/>
        public override byte Priority => LexerPriority;
        /// <inheritdoc/>
        protected override Task<ITextTemplateToken> GenerateAsync(ITextTemplateLexerContext context, int bufferMatchPosition, CancellationToken cancellationToken)
        => Task.FromResult<ITextTemplateToken>(new NewLineTextTemplateToken(_newLineCharacters) { Position = bufferMatchPosition, Line = context.Line });
    }
}
