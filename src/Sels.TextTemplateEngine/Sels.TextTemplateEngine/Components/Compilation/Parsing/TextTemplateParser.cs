using Microsoft.Extensions.Logging;
using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Logging;
using Sels.TextTemplateEngine.Compilation.Expressions;
using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Parsing
{
    /// <inheritdoc cref="ITextTemplateParser"/>
    public class TextTemplateParser : ITextTemplateParser
    {
        // Fields
        private readonly ILogger? _logger;

        /// <inheritdoc cref="TextTemplateParser"/>
        /// <param name="logger">Optional logger for tracing</param>
        public TextTemplateParser(ILogger<TextTemplateParser>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ITextTemplateSyntaxExpression> ParseAsync(IEnumerable<ITextTemplateExpressionParser> parsers, IAsyncEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default)
        {
            tokens = Guard.IsNotNull(tokens);
            var expressionParsers = Guard.IsNotNull(parsers).Where(x => x != null).OrderBy(x => x.Priority).ToArray();

            _logger.Log($"Preparing to parse token into syntax tree using <{expressionParsers.Length}> expression parsers");

            await using var context = new RootContext(expressionParsers, tokens.GetAsyncEnumerator(cancellationToken), _logger);
            var interfactContext = (ITextTemplateParserContext)context;
            _logger.Debug($"Created root context. Starting parse using <{expressionParsers.Length}> expression parsers");
            using var tracer = _logger.TraceAction($"Create syntax tree with <{context.Parsers.Count}> parsers");
            while (await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false) != null)
            {
                _logger.Debug($"Root parser read token <{interfactContext.CurrentToken}> from stream");

                var (response, parser) = await context.AreInterestedInAsync(cancellationToken).ConfigureAwait(false);

                if (response == ExpressionParserResponse.CanParse)
                {
                    _logger.Debug($"Parser <{parser}> can parse token <{interfactContext.CurrentToken}>");
                    await foreach (var expression in context.ParseAsync(parser!, cancellationToken))
                    {
                        _logger.Log($"Adding expression <{expression}> created by <{parser}> to root syntax tree");
                        context.Root.Expressions.Add(expression);
                    }
                }
                else if (response == ExpressionParserResponse.Interested)
                {
                    _logger.Debug($"Parser <{parser}> is interested in token <{interfactContext.CurrentToken}> but can't parse it yet");
                }
                else
                {
                    _logger.Debug($"No parser is interested in token <{interfactContext.CurrentToken}>");
                }
            }

            var expressions = new List<ITextTemplateSyntaxExpression>();
            if (context.Buffer.Count > 0)
            {
                _logger.Log($"Parsing remaining <{context.Buffer.Count}> token(s) in buffer and adding to end of the syntax tree");
                await context.FlushAsync(expressions, context.Buffer, cancellationToken).ConfigureAwait(false);

                foreach (var expression in expressions)
                {
                    _logger.Log($"Adding expression <{expression}> to the end of the root syntax tree");
                    context.Root.Expressions.Add(expression);
                }

                context.Buffer.Clear();
            }

            return context.Root;
        }

        private class RootContext : SubContext, IAsyncDisposable
        {
            // Fields
            private readonly IAsyncEnumerator<ITextTemplateToken> _tokenEnumerator;

            // Properties

            public RootContext(IEnumerable<ITextTemplateExpressionParser> parsers, IAsyncEnumerator<ITextTemplateToken> tokenEnumerator, ILogger? logger) : base(parsers, logger)
            {
                _tokenEnumerator = Guard.IsNotNull(tokenEnumerator);
                ParserScope = TextTemplateEngineConstants.Compilation.ParserScopes.TemplateBody;
                ParentContext = this;
            }

            /// <inheritdoc/>
            public override async Task<ITextTemplateToken?> TryReadNextAsync(CancellationToken cancellationToken = default)
            {
                _logger.Debug($"Reading next token into buffer");
                if (await _tokenEnumerator.MoveNextAsync())
                {
                    Buffer.Add(_tokenEnumerator.Current);
                    _logger.Debug($"Added token <{_tokenEnumerator.Current}> to buffer");
                    IsLastToken = false;
                    return _tokenEnumerator.Current;
                }
                else
                {
                    _logger.Debug($"All tokens read");
                    IsLastToken = true;
                    return null;
                }
            }
            /// <inheritdoc/>
            public override void Dispose()
            {
            }

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            => _tokenEnumerator.DisposeAsync();
        }

        private class SubContext : ITextTemplateParserContext
        {
            // Fields
            protected readonly ILogger? _logger;
            private readonly int? _readLimit;
            private readonly int _offset;

            // State
            private int _tokensRead = 0;

            // Properties
            /// <inheritdoc/>
            public List<ITextTemplateToken> Buffer { get; }
            /// <inheritdoc/>
            public bool IsLastToken { get; protected set; }
            /// <inheritdoc/>
            public IReadOnlyList<ITextTemplateExpressionParser> Parsers { get; }
            /// <inheritdoc/>
            public SyntaxTreeRootExpression Root { get; }
            /// <inheritdoc/>
            public ITextTemplateExpressionParser? ParentParser { get; init; }
            /// <inheritdoc/>
            public string? ParserScope { get; init; }
            /// <inheritdoc/>
            public ITextTemplateParserContext? ParentContext { get; protected set; }

            public SubContext(ITextTemplateParserContext parentContext, int offset, int? readLimit, bool canRead, ILogger? logger)
            {
                ParentContext = Guard.IsNotNull(parentContext);
                Root = ParentContext.Root;
                Parsers = ParentContext.Parsers;
                if (!canRead) IsLastToken = true;
                _offset = Guard.IsLargerOrEqual(offset, 0);
                Buffer = ParentContext.Buffer.Skip(offset).Take(readLimit.HasValue ? readLimit.Value : ParentContext.Buffer.Count).ToList();
                if (readLimit.HasValue) _tokensRead = readLimit.Value - Buffer.Count;
                _readLimit = readLimit;
                _logger = logger;
            }

            protected SubContext(IEnumerable<ITextTemplateExpressionParser> parsers, ILogger? logger)
            {
                Parsers = Guard.IsNotNull(parsers).ToList();
                Buffer = new List<ITextTemplateToken>();
                Root = new SyntaxTreeRootExpression();
                ParentContext = this;
                _logger = logger;
            }

            /// <inheritdoc/>
            public async Task<(ExpressionParserResponse Response, ITextTemplateExpressionParser? Parser)> AreInterestedInAsync(CancellationToken cancellationToken = default)
            {
                _logger.Debug($"Checking if any of the <{Parsers.Count}> parsers is interested in the current buffer of size <{Buffer.Count}>");
                foreach (var parser in Parsers)
                {
                    var response = await parser.IsInterestedAsync(this, cancellationToken).ConfigureAwait(false);
                    switch (response)
                    {
                        case ExpressionParserResponse.CanParse:
                            _logger.Debug($"Parser <{parser}> can parse the current buffer");
                            return (response, parser);
                        case ExpressionParserResponse.Interested:
                            _logger.Debug($"Parser <{parser}> is interested in the current buffer");
                            return (response, parser);
                        case ExpressionParserResponse.NotInterested:
                            _logger.Debug($"Parser <{parser}> is not interested in the current buffer");
                            break;
                        default: throw new NotSupportedException($"Response <{response}> from parser <{parser}> is not supported");
                    }
                }

                return (ExpressionParserResponse.NotInterested, null);
            }
            /// <inheritdoc/>
            public async IAsyncEnumerable<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateExpressionParser parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                parser = Guard.IsNotNull(parser);
                _logger.Debug($"Using parser <{parser}> to parse current buffer of size <{Buffer.Count}>");

                var expression = Guard.IsNotNull(await parser.ParseAsync(this, cancellationToken));
                _logger.Debug($"Parsed expression <{expression}> using <{parser}>");
                // Validate that tokens were used
                _ = Guard.IsNotNull(expression.Tokens);
                // Check if tokens are from buffer
                expression.Tokens.Execute(x => Guard.IsNotNullAnd(x, x => Buffer.Contains(x)));
                // Get index of first token in buffer
                var firstTokenIndex = Guard.IsLargerOrEqual(Buffer.IndexOf(expression.Tokens.First()), 0);
                // Check that tokens length is not longer than buffer after first token
                var remainingBuffer = Buffer.Take(firstTokenIndex).ToList();
                _ = Guard.Is(expression.Tokens, x => x.Count <= remainingBuffer.Count);

                // Try parse tokens before expression
                var bufferBeforeExpression = Buffer.Take(firstTokenIndex).ToList();
                var expressions = new List<ITextTemplateSyntaxExpression>();
                if (firstTokenIndex > 0)
                {
                    _logger.Debug($"Parsing <{firstTokenIndex}> token(s) before expression <{expression}>");
                    using var subContext = CreateScope(parser, ParserScope, 0, bufferBeforeExpression.Count, false);
                    var (response, subParser) = await subContext.AreInterestedInAsync(cancellationToken);
                    if (response == ExpressionParserResponse.CanParse)
                    {
                        await foreach (var subExpression in subContext.ParseAsync(subParser!, cancellationToken))
                        {
                            expressions.Add(subExpression);
                        }

                        bufferBeforeExpression = subContext.Buffer;
                    }
                }


                await FlushAsync(expressions, bufferBeforeExpression, cancellationToken).ConfigureAwait(false);

                foreach (var subExpression in expressions)
                {
                    subExpression.Tokens.Execute(x => Guard.Is(Buffer.Remove(x), x => true));
                    yield return subExpression;
                }
                expression.Tokens.Execute(x => Guard.Is(Buffer.Remove(x), x => true));
                yield return expression;
            }

            public async Task FlushAsync(List<ITextTemplateSyntaxExpression> expressions, IEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default)
            {
                tokens = Guard.IsNotNull(tokens);

                var currentTextTokens = new List<ITextTemplateToken>();
                foreach (var token in tokens)
                {
                    if (token is ITextTemplateSelfParseableToken selfParseableToken)
                    {
                        if (currentTextTokens.HasValue())
                        {
                            expressions.Add(new TextExpression(currentTextTokens));
                            currentTextTokens.Clear();
                        }
                        expressions.Insert(tokens.IndexOf(token), await selfParseableToken.ParseAsync(cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        currentTextTokens.Add(token);
                    }
                }

                if (currentTextTokens.HasValue())
                {
                    expressions.Add(new TextExpression(currentTextTokens));
                    currentTextTokens.Clear();
                }
            }

            /// <inheritdoc/>
            public virtual ITextTemplateParserContext CreateScope(ITextTemplateExpressionParser current, string? scope = null, int bufferOffset = 0, int? bufferLimit = null, bool canReadNext = true)
            {
                current = Guard.IsNotNull(current);
                return new SubContext(this, bufferOffset, bufferLimit, canReadNext, _logger);
            }

            /// <inheritdoc/>
            public virtual async Task<ITextTemplateToken?> TryReadNextAsync(CancellationToken cancellationToken = default)
            {
                _logger.Debug($"Scope reading next token into buffer");

                if (IsLastToken)
                {
                    _logger.Debug($"Scope reached last token");
                    return null;
                }
                else if (_readLimit.HasValue && _tokensRead >= _readLimit.Value)
                {
                    _logger.Debug($"Scope reached read limit");
                    return null;
                }

                var expression = await ParentContext!.TryReadNextAsync(cancellationToken);
                if (expression == null)
                {
                    _logger.Debug($"Scope reached last token");
                    IsLastToken = true;
                    return null;
                }
                else
                {
                    Buffer.Add(expression);
                    _tokensRead++;
                    _logger.Debug($"Scope added token <{expression}> to it's buffer");
                    return expression;
                }
            }

            /// <inheritdoc/>
            public virtual void Dispose()
            {
                if (_offset > 0)
                {
                    if (Buffer.Count > 0 && !Buffer.All(x => ParentContext!.Buffer.Contains(x))) throw new InvalidOperationException($"Scope was created with offset of {_offset} but there are still <{Buffer.Count}> tokens in the scope causing a gap. Consume token first before disposing scope");

                    // Scope buffer fully consumed or all tokens are in parent buffer so nothing to do
                }
                else
                {
                    // Scope buffer is now parent buffer since parent shouldn't have any tokens
                    ParentContext!.Buffer.Clear();
                    ParentContext!.Buffer.AddRange(Buffer);
                }
            }
        }
    }
}
