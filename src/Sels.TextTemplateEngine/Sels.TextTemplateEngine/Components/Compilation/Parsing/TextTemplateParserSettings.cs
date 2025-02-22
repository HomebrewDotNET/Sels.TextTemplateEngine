using Sels.Core;
using Sels.Core.Extensions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Parsing
{
    /// <summary>
    /// The settings to use when parsing tokens into an expression tree.
    /// </summary>
    public class TextTemplateParserSettings : ITextTemplateParserConfigurationBuilder
    {
        /// <inheritdoc cref="ITextTemplateParserConfigurationBuilder.Parsers"/>
        public List<ITextTemplateSyntaxExpressionParser> Parsers { get; } = new List<ITextTemplateSyntaxExpressionParser>();
        /// <inheritdoc/>
        IList<ITextTemplateSyntaxExpressionParser> ITextTemplateParserConfigurationBuilder.Parsers => Parsers;
        public List<Delegates.Async.AsyncFunc<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken, ITextTemplateSyntaxExpression?>> Interceptors { get; } = new List<Delegates.Async.AsyncFunc<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken, ITextTemplateSyntaxExpression?>>();

        /// <inheritdoc cref="TextTemplateParserSettings"/>
        /// <param name="parsers"><see cref="Parsers"/></param>
        /// <param name="configure">Optional delegate to configure the current instance</param>
        public TextTemplateParserSettings(IEnumerable<ITextTemplateSyntaxExpressionParser>? parsers, Action<ITextTemplateParserConfigurationBuilder>? configure)
        {
            if(parsers != null)
            {
                parsers.Where(x => x != null).Execute(x => Parsers.Add(x));
            }

            configure?.Invoke(this);
        }

        /// <inheritdoc/>
        ITextTemplateParserConfigurationBuilder ITextTemplateParserConfigurationBuilder.WithParser(ITextTemplateSyntaxExpressionParser parser)
        {
            Parsers.Add(Guard.IsNotNull(parser));
            return this;
        }
        /// <inheritdoc/>
        ITextTemplateParserConfigurationBuilder ITextTemplateParserConfigurationBuilder.InterceptParsedExpression(Delegates.Async.AsyncFunc<ITextTemplateParserContext, ITextTemplateSyntaxExpression, CancellationToken, ITextTemplateSyntaxExpression?> interceptor)
        {
            Interceptors.Add(Guard.IsNotNull(interceptor));
            return this;
        }
    }
}
