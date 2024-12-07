using Sels.Core;
using Sels.Core.Extensions.Text;
using Sels.TextTemplateEngine.Compilation.Expressions;
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
    public class CommentParser : ITextTemplateExpressionParser
    {
        /// <inheritdoc/>
        public byte Priority { get; }

        /// <inheritdoc cref="CommentParser"/>
        /// <param name="priority"><inheritdoc cref="Priority"/></param>
        public CommentParser(byte priority)
        {
            Priority = priority;
        }

        /// <inheritdoc/>
        public async Task<ExpressionParserResponse> IsInterestedAsync(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            return (await SearchForStartToken(context, cancellationToken).ConfigureAwait(false)).Response;
        }

        private Task<(ExpressionParserResponse Response, int? StartIndex)> SearchForStartToken(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            foreach (var token in context.Buffer)
            {
                if (token.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.StartExpression))
                {
                    var index = context.Buffer.IndexOf(token);
                    if (index + 1 < context.Buffer.Count)
                    {
                        var nextToken = context.Buffer[index + 1];
                        if (nextToken.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.Comment))
                        {
                            return Task.FromResult<(ExpressionParserResponse, int? StartIndex)>((ExpressionParserResponse.CanParse, index));
                        }
                    }
                    else if (index + 1 == context.Buffer.Count && !context.IsLastToken)
                    {
                        return Task.FromResult<(ExpressionParserResponse, int? StartIndex)>((ExpressionParserResponse.Interested, index));
                    }
                }
            }

            return Task.FromResult<(ExpressionParserResponse, int? StartIndex)>((ExpressionParserResponse.NotInterested, null));
        }

        /// <inheritdoc/>
        public async Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            var (response, startIndex) = await SearchForStartToken(context, cancellationToken);
            _ = Guard.Is(response, x => x == ExpressionParserResponse.CanParse);

            var currentTokens = context.Buffer.Skip(startIndex!.Value).ToList();
            var currentIndex = startIndex.Value;

            await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentToken = context.Buffer.ElementAt(currentIndex);

                if (currentToken.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.EndExpression))
                {
                    if (currentTokens.Count <= 0)
                    {
                        var startToken = context.Buffer.ElementAt(startIndex.Value);
                        throw new TextTemplateCompilationException($"Found empty comment expression at position <{startToken.Position}> on line <{startToken.Line}>");
                    }
                    return new CommentExpression(context.Buffer.Skip(startIndex.Value).Take(currentTokens.Count + 3), new TextExpression(currentTokens));
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
