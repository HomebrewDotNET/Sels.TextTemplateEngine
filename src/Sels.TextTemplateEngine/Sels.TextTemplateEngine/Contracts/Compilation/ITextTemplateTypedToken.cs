using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// A <see cref="ITextTemplateLexer"/> that statically exposes it's type.
    /// </summary>
    public interface ITextTemplateTypedToken : ITextTemplateToken
    {
        /// <inheritdoc cref="ITextTemplateToken.Type"/>
        public static abstract string TokenType { get; }
    }
}
