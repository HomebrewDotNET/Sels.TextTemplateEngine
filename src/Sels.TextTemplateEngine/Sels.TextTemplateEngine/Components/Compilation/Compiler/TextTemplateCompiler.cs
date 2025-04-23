using Castle.Components.DictionaryAdapter;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.Logging;
using Sels.Core.Tracing;
using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Compiler
{
    /// <inheritdoc cref="ITextTemplateCompiler"/>
    public class TextTemplateCompiler : ITextTemplateCompiler
    {
        // Fields
        private readonly ILogger? _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITextTemplateLexer _lexer;
        private readonly ITextTemplateParser _parser;

        /// <inheritdoc cref="TextTemplateCompiler"/>
        /// <param name="serviceProvider">Service provider used to create scopes for the compilations</param>
        /// <param name="logger">Optional logger for tracing</param>
        public TextTemplateCompiler(IServiceProvider serviceProvider, ITextTemplateLexer lexer, ITextTemplateParser parser, ILogger<TextTemplateCompiler>? logger = null)
        {
            _serviceProvider = Guard.IsNotNull(serviceProvider);
            _lexer = Guard.IsNotNull(lexer);
            _parser = Guard.IsNotNull(parser);
            _logger = logger;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ITextTemplateToken> LexAsync(Stream stream, Action<ITextTemplateCompilationBuilder>? configure = null, Encoding? encoding = null, bool ownsStream = false, int bufferLength = 1024, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            stream = Guard.IsNotNull(stream);
            bufferLength = Guard.IsLarger(bufferLength, 0);
            var length = stream.CanSeek ? stream.Length.CastTo<long?>() : null;

            _logger.Log($"Compiler preparing to lex stream <{stream}> of length <{(length.HasValue ? length.Value.ToString() : "Unknown")}>");
            await using var context = new CompilationContext(_serviceProvider, configure);
            var tokenStream = LexAsync(context, stream, encoding, ownsStream, bufferLength, cancellationToken);
            _logger.Log($"Compiler opened lex stream <{stream}> of length <{(length.HasValue ? length.Value.ToString() : "Unknown")}> to stream tokens");
            await foreach (var token in tokenStream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return token;
            }
        }
        /// <inheritdoc />
        public async Task<AbstractSyntaxTreeExpression> ParseAsync(Stream stream, Action<ITextTemplateCompilationBuilder>? configure = null, Encoding? encoding = null, bool ownsStream = false, int bufferLength = 1024, CancellationToken cancellationToken = default)
        {
            stream = Guard.IsNotNull(stream);
            bufferLength = Guard.IsLarger(bufferLength, 0);
            var length = stream.CanSeek ? stream.Length.CastTo<long?>() : null;

            _logger.Log($"Compiler preparing to parse stream <{stream}> of length <{(length.HasValue ? length.Value.ToString() : "Unknown")}>");
            await using var context = new CompilationContext(_serviceProvider, configure);
            var tokenStream = LexAsync(context, stream, encoding, ownsStream, bufferLength, cancellationToken);
            var syntaxTree = await ParseAsync(context, tokenStream, cancellationToken).ConfigureAwait(false);
            _logger.Log($"Compiler parsed stream <{stream}> of length <{(length.HasValue ? length.Value.ToString() : "Unknown")}> into syntax tree");
            return syntaxTree;
        }
        /// <inheritdoc />
        public async Task<AbstractSyntaxTreeExpression> ParseAsync(IAsyncEnumerable<ITextTemplateToken> tokenStream, Action<ITextTemplateCompilationBuilder>? configure = null, CancellationToken cancellationToken = default)
        {
            tokenStream = Guard.IsNotNull(tokenStream);
            _logger.Log($"Compiler preparing to parse token stream");
            await using var context = new CompilationContext(_serviceProvider, configure);
            var syntaxTree = await ParseAsync(context, tokenStream, cancellationToken).ConfigureAwait(false);
            _logger.Log($"Compiler parsed token stream into syntax tree");
            return syntaxTree;
        }

        private IAsyncEnumerable<ITextTemplateToken> LexAsync(CompilationContext context, Stream stream, Encoding? encoding = null, bool ownsStream = false, int bufferLength = 1024, CancellationToken cancellationToken = default)
        {
            context = Guard.IsNotNull(context);
            stream = Guard.IsNotNull(stream);

            var lexer = _lexer;
            if (context.CompilerProcess.HasValue())
            {
                lexer = context.GetCompilerServices<ITextTemplateLexer>().LastOrDefault() ?? lexer;
            }
            return lexer.LexAsync(context, context.ConfigureLexing ?? new Action<ITextTemplateLexerConfigurationBuilder>(x => { }), stream, encoding, ownsStream, bufferLength, cancellationToken);
        }

        private Task<AbstractSyntaxTreeExpression> ParseAsync(CompilationContext context, IAsyncEnumerable<ITextTemplateToken> tokenStream, CancellationToken cancellationToken = default)
        {
            context = Guard.IsNotNull(context);
            tokenStream = Guard.IsNotNull(tokenStream);
            var parser = _parser;
            if (context.CompilerProcess.HasValue())
            {
                parser = context.GetCompilerServices<ITextTemplateParser>().LastOrDefault() ?? parser;
            }
            return parser.ParseAsync(context, context.ConfigureParsing ?? new Action<ITextTemplateParserConfigurationBuilder>(x => { }), tokenStream, cancellationToken);
        }

        private class CompilationContext : ITextTemplateCompilationContext, ITextTemplateCompilationBuilder, IAsyncDisposable
        {
            // Fields
            private readonly AsyncServiceScope _scope;

            // State
            private Dictionary<Type, Action<string, object>>? _optionsConfigurations;
            private List<Func<IServiceProvider, Type, IEnumerable<object?>>>? _serviceConfigurations;

            // Properties
            [Traceable(TextTemplateEngineLog.CompilerProcess)]
            public string CompilerProcess { get; private set; }
            public IServiceProvider CompilationScope { get; private set; }
            public Action<ITextTemplateLexerConfigurationBuilder>? ConfigureLexing { get; private set; }
            public Action<ITextTemplateParserConfigurationBuilder>? ConfigureParsing { get; private set; }
            [Traceable(TextTemplateEngineLog.ArtifactCacheKey)]
            public object? StaticCacheKey { get; private set; }
            public Action<string, MemoryCacheEntryOptions>? ConfigureCacheOptions { get; private set; }
            public bool CanGatherStatistics { get; private set; } = true;
            public bool CanCache { get; private set; } = true;
            public TextTemplateCompilerOptions Options { get; }

            public CompilationContext(IServiceProvider serviceProvider, Action<ITextTemplateCompilationBuilder>? configure)
            {
                serviceProvider = Guard.IsNotNull(serviceProvider);
                configure?.Invoke(this);
                if(CompilationScope == null)
                {
                    _scope = serviceProvider.CreateAsyncScope();
                    CompilationScope = _scope.ServiceProvider;
                }
                CompilerProcess = CompilerProcess ?? string.Empty;

                Options = GetOptions<TextTemplateCompilerOptions>();
                if(Options.DefaultConfigureCacheOptions != null)
                {
                    if(ConfigureCacheOptions == null)
                    {
                        ConfigureCacheOptions = Options.DefaultConfigureCacheOptions;
                    }
                    else
                    {
                        ConfigureCacheOptions = Options.DefaultConfigureCacheOptions + ConfigureCacheOptions;
                    }
                }
                if (Options.DefaultConfigureLexing != null)
                {
                    if (ConfigureLexing == null)
                    {
                        ConfigureLexing = Options.DefaultConfigureLexing;
                    }
                    else
                    {
                        ConfigureLexing = Options.DefaultConfigureLexing + ConfigureLexing;
                    }
                }
                if (Options.DefaultConfigureParsing != null)
                {
                    if (ConfigureParsing == null)
                    {
                        ConfigureParsing = Options.DefaultConfigureParsing;
                    }
                    else
                    {
                        ConfigureParsing = Options.DefaultConfigureParsing + ConfigureParsing;
                    }
                }
            }
            /// <inheritdoc/>
            public T GetOptions<T>()
                where T : class
            {
                var options = CompilationScope.GetRequiredService<IOptions<T>>().Value;

                if(_optionsConfigurations != null && _optionsConfigurations.TryGetValue(typeof(T), out var configure))
                {
                    configure(CompilerProcess ?? string.Empty, options);

                    var errorMessageBuilder = new StringBuilder();
                    bool failed = false;
                    foreach (var validator in CompilationScope.GetServices <IValidateOptions<T>>())
                    {
                        var result = validator.Validate(CompilerProcess ?? string.Empty, options);
                        if (result != null && result.Failed)
                        {
                            failed = true;
                            errorMessageBuilder.AppendLine($"Validation failed \"{result.FailureMessage}\":");
                            if (result.Failures != null)
                            {
                                foreach (var failure in result.Failures)
                                {
                                    errorMessageBuilder.AppendLine($"- {failure}");
                                }
                            }
                        }
                    }

                    if (failed)
                    {
                        throw new InvalidOperationException($"Validation failed for options <{typeof(T).Name}>: {errorMessageBuilder.ToString()}");
                    }
                }

                return options;
            }

            /// <inheritdoc/>
            public T[] GetCompilerServices<T>() where T : class => ResolveGetCompilerServices<T>().Where(x => x != null).Distinct().ToArray();

            private IEnumerable<T> ResolveGetCompilerServices<T>()
                where T : class
            {
                if (CompilerProcess.HasValue())
                {

                    var keyedProvider = Guard.IsNotNull(CompilerProcess.CastTo<IKeyedServiceProvider>());
                    foreach (var service in CompilationScope.GetKeyedServices<T>(CompilerProcess))
                    {
                        yield return service;
                    }
                }
                else
                {
                    foreach (var service in CompilationScope.GetServices<T>())
                    {
                        yield return service;
                    }
                }

                if (_serviceConfigurations != null)
                {
                    foreach (var serviceConfiguration in _serviceConfigurations)
                    {
                        foreach (var service in serviceConfiguration(CompilationScope, typeof(T)))
                        {
                            yield return service.CastTo<T>();
                        }
                    }
                }
            }

            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.Configure<T>(Action<string, T> configure)
            {
                configure = Guard.IsNotNull(configure);
                _optionsConfigurations ??= new Dictionary<Type, Action<string, object>>();

                if (_optionsConfigurations.ContainsKey(typeof(T)))
                {
                    _optionsConfigurations[typeof(T)] = (process, options) => configure(process, (T)options);
                }
                else
                {
                    _optionsConfigurations.Add(typeof(T), (process, options) => configure(process, (T)options));
                }
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.ConfigureLexing(Action<ITextTemplateLexerConfigurationBuilder> configure)
            {
                configure = Guard.IsNotNull(configure);
                if(ConfigureLexing == null)
                {
                    ConfigureLexing = configure;
                }
                else
                {
                    ConfigureLexing += configure;
                }
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.ConfigureParsing(Action<ITextTemplateParserConfigurationBuilder> configure)
            {
                ConfigureParsing = Guard.IsNotNull(configure);
                if (ConfigureParsing == null)
                {
                    ConfigureParsing = configure;
                }
                else
                {
                    ConfigureParsing += configure;
                }
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.IncludeFromProcess(string? compilerProcess)
            {
                compilerProcess = compilerProcess.HasValue() ? compilerProcess : string.Empty;

                _serviceConfigurations ??= new List<Func<IServiceProvider, Type, IEnumerable<object?>>>();

                _serviceConfigurations.Add((p, t) =>
                {
                    if (compilerProcess.HasValue())
                    {
                        var keyedProvider = Guard.IsNotNull(p.CastTo<IKeyedServiceProvider>());
                        return keyedProvider.GetKeyedServices(t, compilerProcess);
                    }
                    else
                    {
                        return p.GetServices(t);
                    }
                });
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.IncludeFromProcess<T>(string? compilerProcess)
            {
                compilerProcess = compilerProcess.HasValue() ? compilerProcess : string.Empty;
                _serviceConfigurations ??= new List<Func<IServiceProvider, Type, IEnumerable<object?>>>();
                _serviceConfigurations.Add((p, t) =>
                {
                    if (compilerProcess.HasValue())
                    {
                        var keyedProvider = Guard.IsNotNull(p.CastTo<IKeyedServiceProvider>());
                        return keyedProvider.GetKeyedServices<T>(compilerProcess).OfType<object>();
                    }
                    else
                    {
                        return p.GetServices<T>().OfType<object>();
                    }
                });
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.UsingProcess(string compilerProcess)
            {
                compilerProcess = Guard.IsNotNullOrWhitespace(compilerProcess);
                CompilerProcess = compilerProcess;
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.WithCacheKey<T>(T key)
            {
                key = Guard.IsNotNull(key);
                StaticCacheKey = key;
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.WithCacheOptions(Action<string, MemoryCacheEntryOptions> configure)
            {
                configure = Guard.IsNotNull(configure);
                
                if(ConfigureCacheOptions == null)
                {
                    ConfigureCacheOptions = configure;
                }
                else
                {
                    ConfigureCacheOptions += configure;
                }

                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.WithNoCaching()
            {
                CanCache = false;
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.WithNoStatistics()
            {
                CanGatherStatistics = false;
                return this;
            }
            /// <inheritdoc/>
            ITextTemplateCompilationBuilder ITextTemplateCompilationBuilder.WithScope(IServiceProvider scope)
            {
                scope = Guard.IsNotNull(scope);
                CompilationScope = scope;
                return this;
            }

            public ValueTask DisposeAsync() => _scope.DisposeAsync();
        }
    }
}
