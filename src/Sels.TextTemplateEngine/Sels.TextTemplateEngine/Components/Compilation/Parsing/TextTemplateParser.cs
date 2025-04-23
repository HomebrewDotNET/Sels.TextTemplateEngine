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
using static Sels.Core.Delegates.Async;

namespace Sels.TextTemplateEngine.Compilation.Parsing
{
    /// <inheritdoc cref="ITextTemplateParser"/>
    public class TextTemplateParser : ITextTemplateParser
    {
        // Fields
        private readonly ILogger? _logger;

        /// <inheritdoc cref="TextTemplateParser"/>
        /// <param name="parsers">The parsers that will parse tokens into expressions</param>
        /// <param name="logger">Optional logger for tracing</param>
        public TextTemplateParser(ILogger<TextTemplateParser>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<AbstractSyntaxTreeExpression> ParseAsync(ITextTemplateCompilationContext compilationContext, Action<ITextTemplateParserConfigurationBuilder> configure, IAsyncEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default)
        {
            compilationContext = Guard.IsNotNull(compilationContext);
            tokens = Guard.IsNotNull(tokens);

            var expressionParsers = compilationContext.GetCompilerServices<ITextTemplateSyntaxExpressionParser>();
            if (expressionParsers == null || expressionParsers.Length == 0)
            {
                throw new InvalidOperationException($"No expression parsers found in the current compilation context <{compilationContext.CompilerProcess}>. Please register at least one expression parser in the compilation context");
            }

            var settings = new TextTemplateParserSettings(expressionParsers, configure);
            _logger.Log($"Preparing to parse token into syntax tree using <{settings.Parsers.Count}> expression parsers");

            await using var context = new RootContext(compilationContext, settings, tokens.GetAsyncEnumerator(cancellationToken), _logger);
            var interfactContext = (ITextTemplateParserContext)context;
            _logger.Debug($"Created root context. Starting parse using <{settings.Parsers.Count}> expression parsers");
            using var tracer = _logger.TraceAction($"Create syntax tree with <{context.Parsers.Count}> parsers");
            while (await context.TryReadNextAsync(cancellationToken).ConfigureAwait(false) != null)
            {
                _logger.Debug($"Root parser read token <{interfactContext.CurrentToken}> from stream");

                var (response, parser) = await context.AreInterestedInAsync(cancellationToken).ConfigureAwait(false);

                if (response == SyntaxExpressionParserResponse.CanParse)
                {
                    _logger.Debug($"Parser <{parser}> can parse token <{interfactContext.CurrentToken}>");
                    await foreach (var expression in context.ParseAsync(parser!, cancellationToken))
                    {
                        _logger.Log($"Adding expression <{expression}> created by <{parser}> to root syntax tree");
                        context.Ast.Expressions.Add(expression);
                    }
                }
                else if (response == SyntaxExpressionParserResponse.Interested)
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
                    context.Ast.Expressions.Add(expression);
                }

                context.Buffer.Clear();
            }

            return context.Ast;
        }

        private class RootContext : SubContext, IAsyncDisposable
        {
            // Fields
            private readonly IAsyncEnumerator<ITextTemplateToken> _tokenEnumerator;

            // Properties
            protected override AsyncFunc<CancellationToken, ITextTemplateToken?> TryReadNext => TryReadNextAsync;

            public RootContext(ITextTemplateCompilationContext compilationContext, TextTemplateParserSettings settings, IAsyncEnumerator<ITextTemplateToken> tokenEnumerator, ILogger? logger) : base(compilationContext, settings, logger)
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
                    if(!_subScopeActive) Buffer.Add(_tokenEnumerator.Current);
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

        private class SubContext : ITextTemplateParserContext, ITextTemplateCompilationContext
        {
            // Fields
            protected readonly ITextTemplateCompilationContext _compilationContext;
            protected readonly ILogger? _logger;
            private readonly int? _readLimit;
            private readonly int _offset;
            private readonly List<ITextTemplateToken>? _readTokens;
            private readonly Action? _disposeAction;
            private readonly AsyncFunc<CancellationToken, ITextTemplateToken?>? _tryReadNext;

            // State
            private int _tokensRead = 0;
            protected bool _subScopeActive = false;

            // Properties
            /// <inheritdoc/>
            public List<ITextTemplateToken> Buffer { get; }
            /// <inheritdoc/>
            public bool IsLastToken { get; protected set; }
            /// <inheritdoc/>
            public IReadOnlyList<ITextTemplateSyntaxExpressionParser> Parsers => Settings.Parsers;
            /// <inheritdoc/>
            public AbstractSyntaxTreeExpression Ast { get; }
            /// <inheritdoc/>
            public ITextTemplateSyntaxExpressionParser? ParentParser { get; init; }
            /// <inheritdoc/>
            public string? ParserScope { get; init; }
            /// <inheritdoc/>
            public ITextTemplateParserContext? ParentContext { get; protected set; }
            protected virtual AsyncFunc<CancellationToken, ITextTemplateToken?> TryReadNext => Guard.IsNotNull(_tryReadNext);
            protected TextTemplateParserSettings Settings { get; }

            public string CompilerProcess => _compilationContext.CompilerProcess;

            public IServiceProvider CompilationScope => _compilationContext.CompilationScope;

            public SubContext(AsyncFunc<CancellationToken, ITextTemplateToken?> tryReadNext, ITextTemplateCompilationContext compilationContext, TextTemplateParserSettings settings, ITextTemplateParserContext parentContext, int offset, int? readLimit, bool canRead, bool consumeTokens, Action? disposeAction, ILogger? logger)
            {
                _compilationContext = Guard.IsNotNull(compilationContext);
                _tryReadNext = Guard.IsNotNull(tryReadNext);
                Settings = Guard.IsNotNull(settings);
                ParentContext = Guard.IsNotNull(parentContext);
                Ast = ParentContext.Ast;
                if (!canRead) IsLastToken = true;
                _offset = Guard.IsLargerOrEqual(offset, 0);
                Buffer = ParentContext.Buffer.Skip(offset).Take(readLimit.HasValue ? readLimit.Value : ParentContext.Buffer.Count).ToList();
                if (readLimit.HasValue) _tokensRead = readLimit.Value - Buffer.Count;
                _readLimit = readLimit;
                if (!consumeTokens) _readTokens = new List<ITextTemplateToken>();
                _disposeAction = disposeAction;
                _logger = logger;
            }

            protected SubContext(ITextTemplateCompilationContext compilationContext, TextTemplateParserSettings settings, ILogger? logger)
            {
                _compilationContext = Guard.IsNotNull(compilationContext);
                Settings = Guard.IsNotNull(settings);
                Buffer = new List<ITextTemplateToken>();
                Ast = new AbstractSyntaxTreeExpression();
                ParentContext = this;
                _logger = logger;
            }

            /// <inheritdoc/>
            public async Task<(SyntaxExpressionParserResponse Response, ITextTemplateSyntaxExpressionParser? Parser)> AreInterestedInAsync(CancellationToken cancellationToken = default)
            {
                _logger.Debug($"Checking if any of the <{Parsers.Count}> parsers is interested in the current buffer of size <{Buffer.Count}>");
                foreach (var parser in Parsers)
                {
                    var response = await parser.IsInterestedAsync(this, cancellationToken).ConfigureAwait(false);
                    switch (response)
                    {
                        case SyntaxExpressionParserResponse.CanParse:
                            _logger.Debug($"Parser <{parser}> can parse the current buffer");
                            return (response, parser);
                        case SyntaxExpressionParserResponse.Interested:
                            _logger.Debug($"Parser <{parser}> is interested in the current buffer");
                            return (response, parser);
                        case SyntaxExpressionParserResponse.NotInterested:
                            _logger.Debug($"Parser <{parser}> is not interested in the current buffer");
                            break;
                        default: throw new NotSupportedException($"Response <{response}> from parser <{parser}> is not supported");
                    }
                }

                return (SyntaxExpressionParserResponse.NotInterested, null);
            }
            /// <inheritdoc/>
            public async IAsyncEnumerable<ITextTemplateSyntaxExpression> ParseAsync(ITextTemplateSyntaxExpressionParser parser, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                parser = Guard.IsNotNull(parser);
                if (_subScopeActive) throw new InvalidOperationException("Sub scope active. Parse not allowed");
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
                var remainingBuffer = Buffer.Skip(firstTokenIndex).ToList();
                _ = Guard.Is(expression.Tokens, x => x.GetCount() <= remainingBuffer.Count);

                // Try parse tokens before expression
                var bufferBeforeExpression = Buffer.Take(firstTokenIndex).ToList();
                var expressions = new List<ITextTemplateSyntaxExpression>();
                if (firstTokenIndex > 0)
                {
                    _logger.Debug($"Parsing <{firstTokenIndex}> token(s) before expression <{expression}>");
                    using var subContext = CreateScope(parser, ParserScope, 0, bufferBeforeExpression.Count, false);
                    var (response, subParser) = await subContext.AreInterestedInAsync(cancellationToken);
                    if (response == SyntaxExpressionParserResponse.CanParse)
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
                    var interceptedSubExpression = await InterceptAsync(subExpression, cancellationToken).ConfigureAwait(false);
                    if (interceptedSubExpression != null) yield return subExpression;
                }
                expression.Tokens.Execute(x => Guard.Is(Buffer.Remove(x), x => true));
                var interceptedExpression = await InterceptAsync(expression, cancellationToken).ConfigureAwait(false);
                if (interceptedExpression != null) yield return expression;
            }

            public async Task FlushAsync(List<ITextTemplateSyntaxExpression> expressions, IEnumerable<ITextTemplateToken> tokens, CancellationToken cancellationToken = default)
            {
                tokens = Guard.IsNotNull(tokens);
                if (_subScopeActive) throw new InvalidOperationException("Sub scope active. Flush not allowed");

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

            /// <summary>
            /// Intercepts <paramref name="expression"/> using all interceptors in <see cref="Settings.Interceptors"/> before it is returned.
            /// </summary>
            /// <param name="expression">The expression being intercepted</param>
            /// <param name="cancellationToken">Optional token to cancel the request</param>
            /// <returns>The (new) expression post interception or null if consumed</returns>
            protected async Task<ITextTemplateSyntaxExpression?> InterceptAsync(ITextTemplateSyntaxExpression expression, CancellationToken cancellationToken)
            {
                expression = Guard.IsNotNull(expression);
                _logger.Debug($"Intercepting expression <{expression}>");
                foreach (var interceptor in Settings.Interceptors)
                {
                    var interceptedExpression = await interceptor(this, expression, cancellationToken).ConfigureAwait(false);

                    if (interceptedExpression == null)
                    {
                        _logger.Debug($"Interceptor <{interceptor}> intercepted expression <{expression}> and returned null");
                        return null;
                    }
                    else if (interceptedExpression != expression)
                    {
                        _logger.Debug($"Interceptor <{interceptor}> intercepted expression <{expression}> and returned <{interceptedExpression}>");
                        expression = interceptedExpression;
                    }
                }
                return expression;
            }

            /// <inheritdoc/>
            public virtual ITextTemplateParserContext CreateScope(ITextTemplateSyntaxExpressionParser current, string? scope = null, int bufferOffset = 0, int? bufferLimit = null, bool canReadNext = true, bool consumeTokens = true)
            {
                current = Guard.IsNotNull(current);
                if(_subScopeActive) throw new InvalidOperationException("Sub scope already active for the current scope. Dispose the active one before starting a new one");
                _subScopeActive = true;
                return new SubContext(_readTokens != null ? async t =>
                {
                    var token = await TryReadNext(t);
                    if(token != null) _readTokens.Add(token);
                    return token;
                } : TryReadNext, this, Settings, this, bufferOffset, bufferLimit, canReadNext, consumeTokens, () =>
                {
                    _subScopeActive = false;
                }, _logger)
                {
                    ParserScope = scope,
                    ParentParser = current
                };
            }

            /// <inheritdoc/>
            public virtual async Task<ITextTemplateToken?> TryReadNextAsync(CancellationToken cancellationToken = default)
            {
                if (_subScopeActive) throw new InvalidOperationException("Sub scope active. Token reading not allowed");
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

                var token = await TryReadNext(cancellationToken);
                if (token == null)
                {
                    _logger.Debug($"Scope reached last token");
                    IsLastToken = true;
                    return null;
                }
                else
                {
                    Buffer.Add(token);
                    _readTokens?.Add(token);
                    _tokensRead++;
                    _logger.Debug($"Scope added token <{token}> to it's buffer");
                    return token;
                }
            }

            /// <inheritdoc/>
            public virtual void Dispose()
            {
                if(_readTokens != null) // Token consumption disabled so add tokens to parent buffer
                {
                    ParentContext!.Buffer.AddRange(_readTokens.Where(x => !ParentContext!.Buffer.Contains(x)));
                }
                else
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

                _disposeAction?.Invoke();
            }

            public T GetOptions<T>()
                where T : class
            {
                return _compilationContext.GetOptions<T>();
            }

            public T[] GetCompilerServices<T>()
                where T : class
            {
                return _compilationContext.GetCompilerServices<T>();
            }
        }
    }
}
