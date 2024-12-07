using Sels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Thrown when the parser reaches the end of the buffer while parsing.
    /// </summary>
    public class UnexceptedEndOfParserBufferException : TextTemplateCompilationException
    {
        // Properties
        /// <summary>
        /// The parser that reached the end of the buffer.
        /// </summary>
        public ITextTemplateExpressionParser Parser { get; }

        /// <inheritdoc cref="UnexceptedEndOfParserBufferException"/>
        /// <param name="parser"><inheritdoc cref="Parser"/></param>
        public UnexceptedEndOfParserBufferException(ITextTemplateExpressionParser parser) : base($"Unexpected end of buffer reached when parser <{parser}> was creating expression")
        {
            Parser = Guard.IsNotNull(parser);
        }
    }
}
