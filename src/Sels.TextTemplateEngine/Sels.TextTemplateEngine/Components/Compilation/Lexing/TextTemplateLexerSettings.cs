using Sels.Core;
using Sels.Core.Extensions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Lexing
{
    /// <summary>
    /// The settings to use when lexing a text template.
    /// </summary>
    public class TextTemplateLexerSettings : ITextTemplateLexerConfigurationBuilder
    {
        /// <inheritdoc/>
        public IList<ITextTemplateTokenLexer> Lexers { get; } = new List<ITextTemplateTokenLexer>();
        /// <summary>
        /// Interceptors that will be called each time a character is read from the stream.
        /// </summary>
        public IList<Delegates.Async.AsyncFunc<ITextTemplateLexerContext, char, CancellationToken, char?[]?>> Interceptors { get; } = new List<Delegates.Async.AsyncFunc<ITextTemplateLexerContext, char, CancellationToken, char?[]?>>();
        /// <summary>
        /// Interceptors that will be called each time a token is generated.
        /// </summary>
        public IList<Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, IAsyncEnumerable<ITextTemplateToken>>> TokenInterceptors { get; } = new List<Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, IAsyncEnumerable<ITextTemplateToken>>>();

        /// <inheritdoc cref="TextTemplateLexerSettings"/>
        /// <param name="lexers"><see cref="Lexers"/></param>
        /// <param name="configure">Optional delegate to configure the current instance</param>
        public TextTemplateLexerSettings(IEnumerable<ITextTemplateTokenLexer>? lexers, Action<ITextTemplateLexerConfigurationBuilder>? configure)
        {
            if (lexers != null)
            {
                lexers.Where(x => x != null).Execute(x => Lexers.Add(x));
            }
            configure?.Invoke(this);
        }

        /// <inheritdoc/>
        ITextTemplateLexerConfigurationBuilder ITextTemplateLexerConfigurationBuilder.InterceptRead(Delegates.Async.AsyncFunc<ITextTemplateLexerContext, char, CancellationToken, char?[]?> interceptor)
        {
            Interceptors.Add(Guard.IsNotNull(interceptor));
            return this;
        }
        /// <inheritdoc/>
        ITextTemplateLexerConfigurationBuilder ITextTemplateLexerConfigurationBuilder.WithLexer(ITextTemplateTokenLexer lexer)
        {
            Lexers.Add(Guard.IsNotNull(lexer));
            return this;
        }
        /// <inheritdoc/>
        ITextTemplateLexerConfigurationBuilder ITextTemplateLexerConfigurationBuilder.InterceptTokenGeneration(Func<ITextTemplateLexerContext, ITextTemplateToken, CancellationToken, IAsyncEnumerable<ITextTemplateToken>> interceptor)
        {
            TokenInterceptors.Add(Guard.IsNotNull(interceptor));
            return this;
        }
    }
}
