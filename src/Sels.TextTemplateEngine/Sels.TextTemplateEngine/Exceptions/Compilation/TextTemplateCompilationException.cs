using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Exception thrown when something goes wrong while compiling a text template.
    /// </summary>
    public class TextTemplateCompilationException : Exception
    {
        /// <inheritdoc cref="TextTemplateCompilationException"/>
        /// <param name="message"><inheritdoc cref="Exception.Message"/></param>
        public TextTemplateCompilationException(string? message) : base(message)
        {
        }

        /// <inheritdoc cref="TextTemplateCompilationException"/>
        /// <param name="message"><inheritdoc cref="Exception.Message"/></param>
        /// <param name="innerException"><inheritdoc cref="Exception.InnerException"/></param>
        public TextTemplateCompilationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
