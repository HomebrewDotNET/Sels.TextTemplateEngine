using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Equality;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Text;
using Sels.Core.Models;
using Sels.TextTemplateEngine.Compilation.Expressions;
using Sels.TextTemplateEngine.Compilation.Expressions.Syntax;
using Sels.TextTemplateEngine.Compilation.Lexing.Tokens;
using Sels.TextTemplateEngine.Templates.Compilation.Parsing.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TT = Sels.TextTemplateEngine.TextTemplateEngineConstants.Compilation.TokenTypes;

namespace Sels.TextTemplateEngine.Compilation.Parsing.Parsers
{
    /// <summary>
    /// Parses expressions that are embedded in a template using the start/end tag syntax.
    /// </summary>
    public class InvocationExpressionParser : BaseRecurringMatchingSetTextTemplateParser
    {
        /// <inheritdoc/>
        public override Predicate<ITextTemplateToken>[] RecurringSetConditions { get; } =
        [
            x => x.Type.EqualsNoCase(TT.StartExpression),
            x => x.Type.In(StringComparer.OrdinalIgnoreCase, TT.BlockStart, TT.Identifier)
        ];
        /// <inheritdoc/>
        public override int? MatchWhitespaceAfterIndex => 1;

        /// <inheritdoc cref="InvocationExpressionParser"/>
        /// <param name="priority"><inheritdoc cref="BaseRecurringMatchingSetTextTemplateParser.Priority"/></param>
        public InvocationExpressionParser(byte priority) : base(priority)
        {
        }
        /// <inheritdoc/>
        protected override async Task<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateParserContext context, List<ITextTemplateToken> matchingTokens, int firstTokenIndex, int lastTokenIndex, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            matchingTokens = Guard.IsNotNullOrEmpty(matchingTokens);

            var currentIndex = lastTokenIndex;
            var currentToken = context.Buffer.ElementAt(currentIndex);

            // Filter leading whitespace
            currentIndex = await FilterWhitespaceAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
            var hasWhitespace = currentIndex != lastTokenIndex;
            currentToken = context.Buffer.ElementAt(currentIndex);

            var lengthBeforeMatch = firstTokenIndex;
            // Check if we have a body
            bool isBlockExpression = false;
            if(currentToken.Type.EqualsNoCase(TT.BlockStart))
            {
                if (hasWhitespace) throw new UnexpectedTokenException(this, currentToken, TT.Identifier);
                isBlockExpression = true;
                currentToken = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                if (currentToken == null)
                {
                    throw new UnexceptedEndOfParserBufferException(this);
                }
                currentIndex++;
            }
            currentIndex = await FilterWhitespaceAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
            var prefixLength = currentIndex - firstTokenIndex;

            // Get full identifier which should be single identifier token or alternating between . and identifier tokens
            (currentIndex, var identifierExpression) = await ParseIdentifierAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
            var identifierLength = identifierExpression.Tokens.GetCount();
            currentToken = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
            // Filter trailing whitespace
            var lastIndex = currentIndex;
            currentIndex = await FilterWhitespaceAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
            hasWhitespace = currentIndex != lastIndex;

            // Parse parameters if defined
            IInvocationSyntaxExpression? invocationExpression = null;
            currentToken = context.Buffer.ElementAt(currentIndex);
            switch (currentToken.Type)
            {
                // Invocation without parameters
                case TT.EndExpression:
                    var suffixLength = context.Buffer.Count - firstTokenIndex - prefixLength - identifierLength;
                    var prefixtTokens = context.Buffer.Skip(lengthBeforeMatch).Take(prefixLength);
                    var suffixTokens = context.Buffer.Skip(firstTokenIndex + prefixLength + identifierLength).Take(suffixLength);
                    invocationExpression = new InvocationSyntaxExpression(identifierExpression, prefixtTokens, suffixTokens);
                    break;
                // Invocation using named parameters
                case TT.InvocationNamedParameterStart:
                    if(hasWhitespace) throw new UnexpectedTokenException(this, currentToken, TT.EndExpression);
                    var lastIndexBeforeNamedParameters = currentIndex;
                    (currentIndex, var namedParameters) = await ParseNamedParametersAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    if (currentIndex <= lastIndexBeforeNamedParameters) throw new InvalidOperationException("Expected buffer to have moved forward after parsing named parameters but did not");
                    // Should close the current expression
                    currentToken = context.Buffer.ElementAt(currentIndex);
                    if (!currentToken.Type.EqualsNoCase(TT.EndExpression)) throw new UnexpectedTokenException(this, currentToken, TT.EndExpression);
                    invocationExpression = new NamedParameterInvocationSyntaxExpression(identifierExpression, namedParameters, context.Buffer.Skip(firstTokenIndex).Take(currentIndex - firstTokenIndex+1));
                    break;
                // Invocation using positional parameters like a method
                case TT.InvocationParameterOrGroupStart:
                    if (hasWhitespace) throw new UnexpectedTokenException(this, currentToken, TT.EndExpression);
                    var lastIndexBeforeParameters = currentIndex;
                    (currentIndex, var parameters) = await ParsePositionalParametersAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    if (currentIndex <= lastIndexBeforeParameters) throw new InvalidOperationException("Expected buffer to have moved forward after parsing positional parameters but did not");
                    _ = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    currentIndex++;
                    // Filter whitespace before the end token
                    currentIndex = await FilterWhitespaceAsync(currentIndex, context, cancellationToken, true).ConfigureAwait(false);
                    // Should close the current expression
                    currentToken = context.Buffer.ElementAt(currentIndex);
                    if(!currentToken.Type.EqualsNoCase(TT.EndExpression)) throw new UnexpectedTokenException(this, currentToken, TT.EndExpression);
                    invocationExpression = new PositionalParameterInvocationSyntaxExpression(identifierExpression, parameters, context.Buffer.Skip(firstTokenIndex).Take(currentIndex - firstTokenIndex+1));
                    break;
                default: throw new UnexpectedTokenException(this, currentToken);
            }


            if(invocationExpression != null)
            {
                if (isBlockExpression)
                {
                    var indexBeforeBody = currentIndex;
                    (currentIndex, var bodyExpressions) = await ParseBodyAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    if (currentIndex <= indexBeforeBody) throw new InvalidOperationException("Expected buffer to have moved forward after parsing body but did not");
                    currentToken = context.Buffer.ElementAt(currentIndex);
                    if (!currentToken.Type.EqualsNoCase(TT.BlockEnd)) throw new UnexpectedTokenException(this, currentToken, TT.BlockEnd);
                    var expressionEndTagStartIndex = currentIndex - 1;
                    // Filter whitepsace after block end
                    _ = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    currentIndex++;
                    currentIndex = await FilterWhitespaceAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    // Should be identifier
                    (currentIndex, var endIndentifier) = await ParseIdentifierAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                    // Validate identifiers match
                    if (!endIndentifier.Identifier.SequenceEqual(identifierExpression.Identifier)) throw new TextTemplateCompilationException($"Expected current block end expression to close body for <{new string(identifierExpression.Identifier.ToArray())}> but closes <{new string(endIndentifier.Identifier.ToArray())}>");
                    // Should close the current expression
                    currentToken = context.Buffer.ElementAt(currentIndex);
                    if (!currentToken.Type.EqualsNoCase(TT.EndExpression)) throw new UnexpectedTokenException(this, currentToken, TT.EndExpression);

                    var endTagTokens = context.Buffer.Skip(expressionEndTagStartIndex).Take(currentIndex - expressionEndTagStartIndex + 1).ToList();
                    var bodyExpression = new TemplateBodySyntaxExpression(bodyExpressions, suffixTokens: endTagTokens);

                    invocationExpression.Body = bodyExpression;
                }
                    
                return invocationExpression;
            }

            throw new UnexceptedEndOfParserBufferException(this, matchingTokens.First().Position);
        }

        /// <summary>
        /// Parses the tokens to use for the identifier.
        /// </summary>
        /// <param name="currentIndex">The current index of the token being parsed in the buffer</param>
        /// <param name="context"><inheritdoc cref="ITextTemplateParserContext"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>NewIndex: <paramref name="currentIndex"/> if it was modified| Identifier:The identifier for the invocation</returns>
        /// <exception cref="UnexpectedTokenException"></exception>
        protected virtual async Task<(int NewIndex, IIdentifierTextTemplateSyntaxExpression Identifier)> ParseIdentifierAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            var currentToken = context.Buffer.ElementAt(currentIndex);
            var identifierTokens = new List<ITextTemplateToken>();
            var lastType = currentToken.Type;
            if (lastType.EqualsNoCase(TT.Identifier))
            {
                identifierTokens.Add(currentToken);
            }
            else
            {
                throw new UnexpectedTokenException(this, currentToken, TT.Identifier);
            }
            while (currentToken.Type.In(TT.Identifier, TT.SubPropertyOrIdentifierDivisor))
            {
                currentToken = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                currentIndex++;
                switch (currentToken.Type)
                {
                    case TT.Identifier:
                        if (lastType.EqualsNoCase(TT.Identifier))
                        {
                            throw new UnexpectedTokenException(this, currentToken, TT.SubPropertyOrIdentifierDivisor);
                        }
                        identifierTokens.Add(currentToken);
                        break;
                    case TT.SubPropertyOrIdentifierDivisor:
                        if (!lastType.EqualsNoCase(TT.Identifier))
                        {
                            throw new UnexpectedTokenException(this, currentToken, TT.Identifier);
                        }
                        identifierTokens.Add(currentToken);
                        break;
                }
                lastType = currentToken.Type;
            }
            if (!identifierTokens.HasValue()) throw new UnexpectedTokenException(this, currentToken, TT.Identifier);

            var identifier = new IdentifierSyntaxExpression(identifierTokens);
            return (currentIndex, identifier);
        }

        /// <summary>
        /// Parses the positional parameters for the invocation. Should keep parsing until <see cref="TT.InvocationParameterOrGroupEnd"/> is reached.
        /// </summary>
        /// <param name="currentIndex">The current index of the token being parsed in the buffer</param>
        /// <param name="context"><inheritdoc cref="ITextTemplateParserContext"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>NewIndex: <paramref name="currentIndex"/> if it was modified | Parameters:The parameters for the invocation</returns>
        protected virtual async Task<(int NewIndex, IReadOnlyList<IPositionalParameterTextTemplateSyntaxExpression> Parameters)> ParsePositionalParametersAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            var startToken = context.Buffer.ElementAt(currentIndex);
            using var subScope = context.CreateScope(this, null, currentIndex+1, consumeTokens: false);
            var subScopeIndex = 0;
            var currentParameters = new List<IPositionalParameterTextTemplateSyntaxExpression>();
            var currentToken = subScopeIndex < subScope.Buffer.Count ? subScope.Buffer.ElementAt(subScopeIndex) : await ReadNextAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
            var currentType = currentToken.Type;

            async Task MoveForward()
            {
                currentToken = await ReadNextAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
                currentType = currentToken.Type;
                subScopeIndex++;
            }

            while (!currentType.EqualsNoCase(TT.InvocationParameterOrGroupEnd))
            {
                (currentToken, subScopeIndex, var argumentExpression, var consumedTokens) = await ParseNextParameterValueAsync(subScope, subScopeIndex, TT.PositionalParameterSplit, TT.InvocationParameterOrGroupEnd, cancellationToken).ConfigureAwait(false);
                currentType = currentToken.Type;
                currentParameters.Add(new PositionalParameterSyntaxExpression(argumentExpression, consumedTokens){ Index = currentParameters.Count });

                if (currentType.EqualsNoCase(TT.PositionalParameterSplit)) // Parameter end so new parameter could follow
                {
                    await MoveForward().ConfigureAwait(false);
                }
            }

            return (currentIndex+subScopeIndex+1, currentParameters);
        }

        /// <summary>
        /// Parses the named parameters for the invocation. Should keep parsing until <see cref="TT.EndExpression"/> is reached.
        /// </summary>
        /// <param name="currentIndex">The current index of the token being parsed in the buffer</param>
        /// <param name="context"><inheritdoc cref="ITextTemplateParserContext"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>NewIndex: <paramref name="currentIndex"/> if it was modified | Parameters:The parameters for the invocation</returns>
        protected virtual async Task<(int NewIndex, IReadOnlyList<INamedParameterTextTemplateSyntaxExpression> Parameters)> ParseNamedParametersAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            var startToken = context.Buffer.ElementAt(currentIndex);
            using var subScope = context.CreateScope(this, null, currentIndex + 1, consumeTokens: false);
            var subScopeIndex = 0;
            var currentParameters = new List<INamedParameterTextTemplateSyntaxExpression>();
            var currentToken = subScopeIndex < subScope.Buffer.Count ? subScope.Buffer.ElementAt(subScopeIndex) : await ReadNextAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
            var lastType = currentToken.Type;

            async Task MoveForward()
            {
                currentToken = await ReadNextAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
                lastType = currentToken.Type;
                subScopeIndex++;
            }
            // Keep parsing until we reach the end tag
            while (!lastType.EqualsNoCase(TT.EndExpression))
            {
                // Filter leading whitespace and new lines before the identifier
                subScopeIndex = await FilterWhitespaceAsync(subScopeIndex, subScope, cancellationToken, true).ConfigureAwait(false);
                if(lastType.EqualsNoCase(TT.EndExpression)) continue;

                // New tokens should be the identifier
                var identifierStartIndex = subScopeIndex;
                (subScopeIndex, var identifier) = await ParseIdentifierAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
                // Token after identifier should be the assignment operator
                subScopeIndex = await FilterWhitespaceAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
                var assignmentOperatorToken = subScope.Buffer.ElementAt(subScopeIndex);
                if (!assignmentOperatorToken.Type.EqualsNoCase(TT.AssignmentOperator)) throw new UnexpectedTokenException(this, assignmentOperatorToken, TT.AssignmentOperator);
                var identifierTokens = subScope.Buffer.Skip(identifierStartIndex).Take(subScopeIndex - identifierStartIndex + 1).ToList();

                // Parse the value of the parameter
                await MoveForward().ConfigureAwait(false);
                
                (currentToken, subScopeIndex, var argumentExpression, var consumedTokens) = await ParseNextParameterValueAsync(subScope, subScopeIndex, TT.NamedParameterSplit, TT.EndExpression, cancellationToken).ConfigureAwait(false);
                lastType = currentToken.Type;
                currentParameters.Add(new NamedParameterSyntaxExpression(identifier, argumentExpression, identifierTokens.Concat(consumedTokens)));

                if(lastType.EqualsNoCase(TT.NamedParameterSplit)) // Parameter end so new parameter could follow
                {
                    await MoveForward().ConfigureAwait(false);
                }
            }

            return (currentIndex + subScopeIndex+1, currentParameters);
        }

        /// <summary>
        /// Parses the next value for a parameter based on the current context and index.
        /// </summary>
        /// <param name="context">The index containing the current buffer to parse</param>
        /// <param name="subScopeIndex">The index of the current token to parse</param>
        /// <param name="parameterSplitTokenType">The type of the token that indicates a new parameter should be parsed</param>
        /// <param name="parameterStopTokenType">The type of token that indicates that no more parameters should follow</param>
        /// <param name="cancellationToken">Optional token used to cancel the request</param>
        /// <returns>CurrentToken: The last parsed token|NewSubScopeIndex: The new <paramref name="subScopeIndex"/> after parsing the value|ArgumentExpression: The expression containing the value of the parameter|ConsumedTokens: All tokens that were used to create ArgumentExpression</returns>
        protected async Task<(ITextTemplateToken CurrentToken, int NewSubScopeIndex, ITextTemplateSyntaxExpression ArgumentExpression, IEnumerable<ITextTemplateToken> ConsumedTokens)> ParseNextParameterValueAsync(ITextTemplateParserContext context, int subScopeIndex, string parameterSplitTokenType, string parameterStopTokenType, CancellationToken cancellationToken)
        {
            context = Guard.IsNotNull(context);
            parameterSplitTokenType = Guard.IsNotNullOrEmpty(parameterSplitTokenType);
            parameterStopTokenType = Guard.IsNotNullOrEmpty(parameterStopTokenType);
            var currentExpressions = new List<ITextTemplateSyntaxExpression>();
            var parameterStartIndex = subScopeIndex;
            var countBeforeArgument = context.Buffer.Count;
            var unparsedTokenCount = 0;
            var startToken = context.Buffer.ElementAt(subScopeIndex);
            var currentToken = startToken;
            var currentType = currentToken.Type;
            using (var argumentScope = context.CreateScope(this, TextTemplateEngineConstants.Compilation.ParserScopes.Argument, subScopeIndex, consumeTokens: false))
            {
                bool lastWasParsed = false;
                async Task MoveArgumentForward()
                {
                    currentToken = await argumentScope.TryReadNextAsync(cancellationToken).ConfigureAwait(false);
                    if (currentToken == null) throw new UnexceptedEndOfParserBufferException(this, startToken.Position);
                    currentType = currentToken.Type;
                    if (lastWasParsed) unparsedTokenCount = 0;
                    else unparsedTokenCount++;
                }
                while (!currentType.EqualsNoCase(parameterSplitTokenType) && !currentType.EqualsNoCase(parameterStopTokenType))
                {
                    lastWasParsed = false;
                    var (response, parser) = await argumentScope.AreInterestedInAsync(cancellationToken).ConfigureAwait(false);
                    if (response == ExpressionParserResponse.CanParse)
                    {
                        await foreach (var expression in argumentScope.ParseAsync(parser!, cancellationToken))
                        {
                            currentExpressions.Add(expression);
                        }

                        lastWasParsed = true;
                    }

                    await MoveArgumentForward().ConfigureAwait(false);
                }
            }
            var argumentTokenCount = context.Buffer.Count - countBeforeArgument;
            var consumedTokenCount = argumentTokenCount - unparsedTokenCount;

            var parameterEndIndex = subScopeIndex + consumedTokenCount;
            subScopeIndex = parameterEndIndex + unparsedTokenCount;
            var parameterTokens = context.Buffer.Skip(parameterStartIndex).Take(parameterEndIndex - parameterStartIndex).ToList();
            var remainingTokens = context.Buffer.Skip(parameterEndIndex).Take(unparsedTokenCount).ToList();
            var consumedTokens = parameterTokens.Concat(remainingTokens);

            // Moved forward into buffer last last token are probably just test
            if (remainingTokens.HasValue())
            {
                currentExpressions.Add(new TextExpression(remainingTokens));
            }
            // Previous parameter ended but nothing was parsed so we have a null argument
            if (currentType.EqualsNoCase(TT.NamedParameterSplit) && !currentExpressions.HasValue()) throw new UnexpectedTokenException(this, currentToken, TT.NamedParameterSplit);

            return (currentToken, subScopeIndex, currentExpressions.Count == 1 ? currentExpressions.First() : new OperatorGroupSyntaxExpression(currentExpressions), consumedTokens);
        }
        /// <summary>
        /// Parses the body for the invocation. Should keep parsing until <see cref="TT.StartExpression"/> followed by <see cref="TT.BlockEnd"/> is reached.
        /// </summary>
        /// <param name="currentIndex">The current index of the token being parsed in the buffer</param>
        /// <param name="context"><inheritdoc cref="ITextTemplateParserContext"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>NewIndex: <paramref name="currentIndex"/> if it was modified | Parameters:The parameters for the invocation</returns>
        protected virtual async Task<(int NewIndex, IReadOnlyList<ITextTemplateSyntaxExpression> BodyExpression)> ParseBodyAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            var startToken = context.Buffer.ElementAt(currentIndex);
            using var subScope = context.CreateScope(this, TextTemplateEngineConstants.Compilation.ParserScopes.TemplateBody, currentIndex + 1, consumeTokens: false);
            var subScopeIndex = 0;
            var currentParameters = new List<IPositionalParameterTextTemplateSyntaxExpression>();
            var currentToken = subScopeIndex < subScope.Buffer.Count ? subScope.Buffer.ElementAt(subScopeIndex) : await ReadNextAsync(subScopeIndex, subScope, cancellationToken).ConfigureAwait(false);
            var currentType = currentToken.Type;
            var lastType = currentType;
            var expressions = new List<ITextTemplateSyntaxExpression>();
            int totalConsumedTokens = 0;
            async Task MoveForward()
            {
                lastType = currentType;
                currentToken = await ReadNextAsync(subScopeIndex-totalConsumedTokens, subScope, cancellationToken).ConfigureAwait(false);
                currentType = currentToken.Type;
                subScopeIndex++;
            }

            while(!(currentType.EqualsNoCase(TT.BlockEnd) && lastType.EqualsNoCase(TT.StartExpression)))
            {
                var (response, parser) = await subScope.AreInterestedInAsync(cancellationToken).ConfigureAwait(false);

                if(response == ExpressionParserResponse.CanParse)
                {
                    int consumedTokens = 0;
                    await foreach (var expression in subScope.ParseAsync(parser!, cancellationToken))
                    {
                        expressions.Add(expression);
                        consumedTokens += expression.Tokens.GetCount();
                    }
                    totalConsumedTokens += consumedTokens;
                    subScopeIndex = totalConsumedTokens-1;
                }

                await MoveForward().ConfigureAwait(false);
            }
            var tokensAfterLastExpression = subScope.Buffer.Take(subScopeIndex-1-totalConsumedTokens).ToList();

            if (tokensAfterLastExpression.HasValue()) // Any remaining tokens are just text
            {
                expressions.Add(new TextExpression(tokensAfterLastExpression));
            }

            return (currentIndex + subScopeIndex+1, expressions);
        }

        /// <summary>
        /// Moves the buffer forwards until the current token isn't whitespace and optionally whitespace if <paramref name="includeNewLine"/> is set.
        /// </summary>
        /// <param name="currentIndex">The current index of the current token in the buffer</param>
        /// <param name="context">The parser context</param>
        /// <param name="cancellationToken">Token to cancel the request</param>
        /// <param name="includeNewLine">Set to true to also filter newline tokens</param>
        /// <returns>The new index of the next non-whitespace (or newline if enabled) token</returns>
        protected async Task<int> FilterWhitespaceAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken, bool includeNewLine = false)
        {
            var currentToken = context.Buffer.ElementAt(currentIndex);
            while (currentToken.Type.EqualsNoCase(TT.Whitespace) || (includeNewLine && currentToken.Type.EqualsNoCase(TT.NewLine)))
            {
                currentToken = await ReadNextAsync(currentIndex, context, cancellationToken).ConfigureAwait(false);
                currentIndex++;
            }

            return currentIndex;
        }

        /// <summary>
        /// Reads the next token from the buffer based on index <paramref name="currentIndex"/>. 
        /// If the buffer doesn't contain the token at <paramref name="currentIndex"/> it will try to read the next token from the stream.
        /// </summary>
        /// <param name="currentIndex">The current index of the current token</param>
        /// <param name="context">The parser context</param>
        /// <param name="cancellationToken">Token to cancel the request</param>
        /// <returns>The next token</returns>
        /// <exception cref="UnexceptedEndOfParserBufferException"></exception>
        protected async Task<ITextTemplateToken> ReadNextAsync(int currentIndex, ITextTemplateParserContext context, CancellationToken cancellationToken)
        {
            if(currentIndex+1 < context.Buffer.Count)
            {
                return context.Buffer.ElementAt(currentIndex+1);
            }
            if (currentIndex > context.Buffer.Count) throw new InvalidOperationException($"Current index of <{currentIndex}> is higher than buffer of length <{context.Buffer.Count}>");
            var nextToken = await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false);
            if(nextToken == null) throw new UnexceptedEndOfParserBufferException(this);
            return nextToken;
        }
    }
}
