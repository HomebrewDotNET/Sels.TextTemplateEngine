using Sels.Core.Extensions.Equality;
using Sels.Core.Extensions.Text;
using Sels.Core;
using Sels.TextTemplateEngine.Templates.Compilation.Parsing.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TT = Sels.TextTemplateEngine.TextTemplateEngineConstants.Compilation.TokenTypes;
using Sels.TextTemplateEngine.Compilation.Expressions;
using Sels.Core.Extensions;
using Sels.TextTemplateEngine.Compilation.Expressions.Syntax;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.Linq;

namespace Sels.TextTemplateEngine.Compilation.Parsing.Parsers
{
    /// <summary>
    /// Parses accessor expressions.
    /// </summary>
    public class AccessorExpressionParser : BaseRecurringMatchingSetTextTemplateParser
    {
        /// <inheritdoc/>
        public override Predicate<ITextTemplateToken>[] RecurringSetConditions { get; } =
        [
            x => x.Type.EqualsNoCase(TT.AccessorStart),
            x => x.Type.In(TT.Identifier, TT.StartExpression, TT.AccessorStart)
        ];
        /// <inheritdoc/>
        public override int? MatchWhitespaceAfterIndex => 1;
        /// <inheritdoc/>
        public override IEnumerable<string> Parses
        {
            get
            {
                yield return TextTemplateEngineConstants.Compilation.ExpressionTypes.Variable;
                yield return TextTemplateEngineConstants.Compilation.SyntaxExpressionTypes.Wrapped;
                yield return TextTemplateEngineConstants.Compilation.ExpressionTypes.Accessor;
            }
        }

        /// <inheritdoc cref="AccessorExpressionParser"/>
        /// <param name="priority"><inheritdoc cref="BaseRecurringMatchingSetTextTemplateParser.Priority"/></param>
        public AccessorExpressionParser(byte priority) : base(priority)
        {
        }
        /// <inheritdoc/>
        protected override async Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, List<ITextTemplateToken> matchingTokens, int firstTokenIndex, int lastTokenIndex, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            matchingTokens = Guard.IsNotNullOrEmpty(matchingTokens);

            //// Get target expression where to access value from
            ITextTemplateSyntaxExpression? targetExpression = null; // Expression that is being accessed
            var targetStartToken = context.Buffer.ElementAt(lastTokenIndex);
            var isVariable = false;
            if (targetStartToken.Type.EqualsNoCase(TT.Identifier)) // Starts with identifier so variable is being accessed
            {
                isVariable = true;
            }
            else if (targetStartToken.Type.In(StringComparer.OrdinalIgnoreCase, TT.StartExpression, TT.AccessorStart)) // Accessing other embedded expression or accessor
            {
                using var subScope = context.CreateScope(this, TextTemplateEngineConstants.Compilation.ParserScopes.Accessor, context.Buffer.Count, consumeTokens: false);

                while(await subScope.TryReadNextAsync(cancellationToken).ConfigureAwait(false) != null)
                {
                    var (response, parser) = await subScope.AreInterestedInAsync(cancellationToken).ConfigureAwait(false);

                    if (response == SyntaxExpressionParserResponse.CanParse)
                    {
                        var expressions = new List<ITextTemplateSyntaxExpression>();
                        await foreach(var expression in subScope.ParseAsync(parser!, cancellationToken))
                        {
                            expressions.Add(expression);    
                        }
                        targetExpression = expressions.Count == 1 ? expressions.First() : new OperatorGroupSyntaxExpression(expressions);
                        break;
                    }
                }
                
                if (targetExpression == null)
                {
                    var firstToken = subScope.Buffer.First();
                    throw new TextTemplateCompilationException($"Accessor failed to parse target expression between token <{firstToken.Type}> at <{firstToken.Position}> and token <{context.CurrentToken.Type}> at <{context.CurrentToken.Position}>");
                }
            }
            else
            {
                throw new UnexpectedTokenException(this, targetStartToken, TT.Identifier, TT.StartExpression, TT.AccessorStart);
            }
            var lengthBeforeMatch = firstTokenIndex;
            var prefixLength = lastTokenIndex - firstTokenIndex;
            var targetLength = targetExpression != null ? targetExpression.Tokens.GetCount() : 1;
            // See if any sub properties are being accessed
            var subProperties = new List<PropertyAccessor>();
            var lastType = context.CurrentToken.Type;
            var lastIsNullable = false;
            while (await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false) != null)
            {
                switch (context.CurrentToken.Type) 
                {
                    case TT.ElvisOperator:
                        if (!lastType.EqualsNoCase(TT.Identifier))
                        {
                            throw new UnexpectedTokenException(this, context.CurrentToken, TT.Identifier);
                        }
                        lastIsNullable = true;
                        break;
                    case TT.SubPropertyOrIdentifierDivisor:
                        if (!lastType.In(StringComparer.OrdinalIgnoreCase, TT.Identifier, TT.ElvisOperator))
                        {
                            throw new UnexpectedTokenException(this, context.CurrentToken, TT.Identifier, TT.ElvisOperator);
                        }                        
                        break;
                    case TT.Identifier:
                        if (!lastType.EqualsNoCase(TT.SubPropertyOrIdentifierDivisor))
                        {
                            throw new UnexpectedTokenException(this, context.CurrentToken, TT.SubPropertyOrIdentifierDivisor);
                        }
                        subProperties.Add(new PropertyAccessor() { Name = new string(context.CurrentToken.TextValue.ToArray()), IsNullable = lastIsNullable});
                        lastIsNullable = false;
                        break;
                    case TT.Whitespace:
                        if(!lastType.In(StringComparer.OrdinalIgnoreCase, TT.Whitespace, TT.Identifier))
                        {
                            throw new UnexpectedTokenException(this, context.CurrentToken, TT.Whitespace, TT.Identifier);
                        }
                        break;
                    case TT.EndExpression:
                        if (!lastType.In(StringComparer.OrdinalIgnoreCase, TT.Whitespace, TT.Identifier))
                        {
                            throw new UnexpectedTokenException(this, context.CurrentToken, TT.Whitespace, TT.Identifier);
                        }
                        var suffixLength = context.Buffer.Count - firstTokenIndex - prefixLength - targetLength;
                        var prefixTokens = context.Buffer.Skip(lengthBeforeMatch).Take(prefixLength);
                        var suffixTokens = context.Buffer.Skip(firstTokenIndex + prefixLength + targetLength).Take(suffixLength);
                        if (!subProperties.HasValue()) {
                            if (isVariable)
                            {
                                return new VariableExpression(targetStartToken, context.Buffer.Skip(firstTokenIndex).Take(prefixLength + targetLength + suffixLength));
                            }
                            
                            return new WrappedSyntaxExpression(targetExpression!, prefixTokens, suffixTokens);
                        }
                        return new AccessorSyntaxExpression(isVariable ? new VariableExpression(targetStartToken, targetStartToken.AsEnumerable()) : targetExpression!, subProperties,
                            prefixTokens,
                            suffixTokens);
                    default: throw new UnexpectedTokenException(this, context.CurrentToken);

                }

                lastType = context.CurrentToken.Type;
            }

            throw new UnexceptedEndOfParserBufferException(this);
        }
    }
}
