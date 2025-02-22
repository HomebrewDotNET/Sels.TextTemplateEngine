using Sels.Core.Extensions.Text;
using Sels.Core;
using Sels.TextTemplateEngine.Compilation.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Templates.Compilation.Parsing.Parsers
{
    /// <summary>
    /// Base class for creating a parser that can start parsing starting from a recurring set of tokens.
    /// </summary>
    public abstract class BaseRecurringMatchingSetTextTemplateParser : ITextTemplateSyntaxExpressionParser
    {
        // Properties
        /// <inheritdoc/>
        public byte Priority { get; }
        /// <summary>
        /// Predicates that determine if the current token sequence is part of the recurring set.
        /// </summary>
        public abstract Predicate<ITextTemplateToken>[] RecurringSetConditions { get; }
        /// <summary>
        /// If set any whitepsace tokens after this index will be matched automatically. Matched whitespace tokens does not count towards the recurring set index when choosing the predicate.
        /// </summary>
        public virtual int? MatchWhitespaceAfterIndex { get; }
        /// <inheritdoc/>
        public abstract IEnumerable<string> Parses { get; }

        /// <inheritdoc cref="BaseRecurringMatchingSetTextTemplateParser"/>
        /// <param name="priority"><inheritdoc cref="Priority"/></param>
        protected BaseRecurringMatchingSetTextTemplateParser(byte priority)
        {
            Priority = priority;
        }

        /// <inheritdoc/>
        public async Task<SyntaxExpressionParserResponse> IsInterestedAsync(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);

            return (await SearchForStartToken(context, cancellationToken).ConfigureAwait(false)).Response;
        }

        private Task<(SyntaxExpressionParserResponse Response, int? StartIndex, int? EndIndex)> SearchForStartToken(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            var conditions = Guard.IsNotNullOrEmpty(RecurringSetConditions);

            foreach (var token in context.Buffer)
            {
                var index = context.Buffer.IndexOf(token);
                var whitespaceBuffer = MatchWhitespaceAfterIndex.HasValue ? new List<ITextTemplateToken>() : null;

                for (var i = 0; (i - whitespaceBuffer?.Count ?? 0) < conditions.Length && (index + i - whitespaceBuffer?.Count ?? 0) < context.Buffer.Count; i++)
                {
                    var tokenIndex = index + i;
                    var tokenToCheck = context.Buffer.ElementAt(tokenIndex);
                    var conditionIndex = MatchWhitespaceAfterIndex.HasValue ? i - whitespaceBuffer?.Count ?? 0 : i;
                    if (MatchWhitespaceAfterIndex.HasValue && tokenIndex >= MatchWhitespaceAfterIndex.Value && tokenToCheck.Type.EqualsNoCase(TextTemplateEngineConstants.Compilation.TokenTypes.Whitespace))
                    {
                        whitespaceBuffer?.Add(tokenToCheck);
                        continue;
                    }
                    
                    if (!conditions[conditionIndex](tokenToCheck))
                    {
                        break;
                    }

                    if (conditionIndex == conditions.Length - 1)
                    {
                        return Task.FromResult<(SyntaxExpressionParserResponse, int? StartIndex, int? EndIndex)>((SyntaxExpressionParserResponse.CanParse, index, tokenIndex));
                    }
                    else if (tokenIndex == context.Buffer.Count - 1 && !context.IsLastToken)
                    {
                        return Task.FromResult<(SyntaxExpressionParserResponse, int? StartIndex, int? EndIndex)>((SyntaxExpressionParserResponse.Interested, index, tokenIndex));
                    }
                }
            }

            return Task.FromResult<(SyntaxExpressionParserResponse, int? StartIndex, int? EndIndex)>((SyntaxExpressionParserResponse.NotInterested, null, null));
        }

        /// <inheritdoc/>
        public async Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            var (response, startIndex, endIndex) = await SearchForStartToken(context, cancellationToken);
            _ = Guard.Is(response, x => x == SyntaxExpressionParserResponse.CanParse);

            var currentTokens = context.Buffer.Skip(startIndex!.Value).Take(endIndex!.Value - startIndex.Value).ToList();

            return await ParseAsync(context, currentTokens, startIndex.Value, endIndex.Value, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Parses the tokens following the matching start tokens.
        /// </summary>
        /// <param name="context">The current lexer context</param>
        /// <param name="matchingTokens">The tokens that matched <see cref="RecurringSetConditions"/></param>
        /// <param name="firstTokenIndex">The index of the first token in <paramref name="matchingTokens"/> in <see cref="ITextTemplateParserContext.Buffer"/></param>
        /// <param name="lastTokenIndex">The index of the last token in <paramref name="matchingTokens"/> in <see cref="ITextTemplateParserContext.Buffer"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The generated expression</returns>
        protected abstract Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, List<ITextTemplateToken> matchingTokens, int firstTokenIndex, int lastTokenIndex, CancellationToken cancellationToken);
    }
}
