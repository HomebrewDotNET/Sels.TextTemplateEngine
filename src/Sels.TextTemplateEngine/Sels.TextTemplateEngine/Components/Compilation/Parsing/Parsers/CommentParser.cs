using Sels.Core;
using Sels.Core.Extensions.Text;
using Sels.TextTemplateEngine.Compilation.Expressions;
using Sels.TextTemplateEngine.Templates.Compilation.Parsing.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Parsing.Parsers
{
    /// <summary>
    /// Parses comments from tokens.
    /// </summary>
    public class CommentParser : BaseRecurringMatchingSetTextTemplateParser
    {
        /// <inheritdoc/>
        public override Predicate<ITextTemplateToken>[] RecurringSetConditions { get; } =
        [
            x => x.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.StartExpression),
            x => x.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.Comment)
        ];

        /// <inheritdoc/>
        public override IEnumerable<string> Parses
        {
            get
            {
                yield return TextTemplateEngineConstants.Compilation.ExpressionTypes.Comment;
            }
        }

        /// <inheritdoc cref="CommentParser"/>
        /// <param name="priority"><inheritdoc cref="Priority"/></param>
        public CommentParser(byte priority) : base (priority)
        {
        }
        /// <inheritdoc/>
        protected override async Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, List<ITextTemplateToken> matchingTokens, int firstTokenIndex, int lastTokenIndex, CancellationToken cancellationToken)
        {
            var currentIndex = firstTokenIndex;

            await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false);
            var currentTokens = new List<ITextTemplateToken>();
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentToken = context.Buffer.ElementAt(currentIndex);

                if (currentToken.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.EndExpression))
                {
                    if (currentTokens.Count <= 0)
                    {
                        var startToken = context.Buffer.ElementAt(firstTokenIndex);
                        throw new TextTemplateCompilationException($"Found empty comment expression at position <{startToken.Position}>");
                    }
                    return new CommentExpression(context.Buffer.Skip(firstTokenIndex).Take(currentTokens.Count + matchingTokens.Count +1), new TextExpression(currentTokens));
                }
                else
                {
                    currentTokens.Add(currentToken);
                    if (currentIndex + 1 >= context.Buffer.Count)
                    {
                        if (await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false) == null)
                        {
                            break;
                        }
                    }
                }

                currentIndex++;
            }

            cancellationToken.ThrowIfCancellationRequested();

            throw new UnexceptedEndOfParserBufferException(this);
        }
    }
}
