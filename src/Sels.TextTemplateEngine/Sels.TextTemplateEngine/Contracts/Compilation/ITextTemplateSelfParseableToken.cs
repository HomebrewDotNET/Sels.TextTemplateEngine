using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Can be additionally implemented by a token if it can parse itself into an expression.
    /// </summary>
    public interface ITextTemplateSelfParseableToken : ITextTemplateToken
    {
        /// <summary>
        /// Parses the token into an expression.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The expression created from the current token</returns>
        public Task<ITextTemplateSyntaxExpression> ParseAsync(CancellationToken cancellationToken = default);
    }
}
