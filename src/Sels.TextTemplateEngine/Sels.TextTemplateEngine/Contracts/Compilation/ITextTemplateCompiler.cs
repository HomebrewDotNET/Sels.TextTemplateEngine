using Microsoft.Extensions.Caching.Memory;
using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Compiles parses syntax tress into artifacts such as invokable templates by managing the various compiler components / compilation steps.
    /// </summary>
    public interface ITextTemplateCompiler
    {
        // Lex only
        /// <summary>
        /// Reads <paramref name="stream"/> and enumerates the tokens found in the stream.
        /// </summary>
        /// <param name="stream">The stream to read tokens from</param>
        /// <param name="configure">Delegate used to configure the current complilation process</param>
        /// <param name="encoding">The encoding of <paramref name="stream"/> if known</param>
        /// <param name="ownsStream">If the lexers owns the stream and can dispose it when done, set to false if caller does the disposing</param>
        /// <param name="bufferLength">How many characters will be read at a time from <paramref name="stream"/></param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Async enumerator that will return any tokens reads from <paramref name="stream"/></returns>
        IAsyncEnumerable<ITextTemplateToken> LexAsync(Stream stream, Action<ITextTemplateCompilationBuilder>? configure = null, Encoding? encoding = null, bool ownsStream = false, int bufferLength = 1024, CancellationToken cancellationToken = default);

        // Parse only
        /// <summary>
        /// Parses <paramref name="stream"/> into an abstract syntax tree.
        /// </summary>
        /// <param name="stream">The stream to read from parse into an abstract syntax tree</param>
        /// <param name="encoding">The encoding of <paramref name="stream"/> if known</param>
        /// <param name="ownsStream">If the lexers owns the stream and can dispose it when done, set to false if caller does the disposing</param>
        /// <param name="bufferLength">How many characters will be read at a time from <paramref name="stream"/></param>
        /// <param name="configure">Delegate used to configure the current complilation process</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The parsed abstract syntax tree</returns>
        public Task<AbstractSyntaxTreeExpression> ParseAsync(Stream stream, Action<ITextTemplateCompilationBuilder>? configure = null, Encoding? encoding = null, bool ownsStream = false, int bufferLength = 1024, CancellationToken cancellationToken = default);
        /// <summary>
        /// Parses <paramref name="tokenStream"/> into an abstract syntax tree.
        /// </summary>
        /// <param name="tokenStream">Async enumerator returning the tokens to parse into a syntax tree</param>
        /// <param name="configure">Delegate used to configure the current complilation process</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>The parsed abstract syntax tree</returns>
        public Task<AbstractSyntaxTreeExpression> ParseAsync(IAsyncEnumerable<ITextTemplateToken> tokenStream, Action<ITextTemplateCompilationBuilder>? configure = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Used to configure the compilation process.
    /// </summary>
    public interface ITextTemplateCompilationBuilder
    {
        // Compiler components
        /// <summary>
        /// Specifies the <see cref="ITextTemplateCompilationContext.CompilerProcess"/> to use for the compilation.
        /// </summary>
        /// <param name="compilerProcess">The name of the process to use</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder UsingProcess(string compilerProcess);
        /// <summary>
        /// Specifies the provider scope to use for the compilation.
        /// By default one is created for the compilation.
        /// </summary>
        /// <param name="scope">The scope to use</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder WithScope(IServiceProvider scope);
        /// <summary>
        /// Configures options of type <typeparamref name="T"/> for the current compilation.
        /// </summary>
        /// <typeparam name="T">The type of the options to configure</typeparam>
        /// <param name="configure">Delegate that configures the option instance. First arg is the <see cref="ITextTemplateCompilationContext.CompilerProcess"/>, second arg is the option instance to configure</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder Configure<T>(Action<string, T> configure);
        /// <summary>
        /// Includes services configured for compiler process <paramref name="compilerProcess"/> in addition to the ones configured for the current process.
        /// </summary>
        /// <param name="compilerProcess">The name of the compiler process to import from. When null, empty or whitespace the default services will be included</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder IncludeFromProcess(string? compilerProcess);
        /// <summary>
        /// Includes services of type <typeparamref name="T"/> configured for compiler process <paramref name="compilerProcess"/> in addition to the ones configured for the current process.
        /// </summary>
        /// <param name="compilerProcess">The name of the compiler process to import from. When null, empty or whitespace the default services will be included</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder IncludeFromProcess<T>(string? compilerProcess);
        /// <summary>
        /// Disables any implicit statistics gathering for the compilation.
        /// </summary>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder WithNoStatistics();

        // Configuration
        /// <summary>
        /// Configures the lexing compilation step.
        /// </summary>
        /// <param name="configure">Delegate used to configure the lexing compilation step</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder ConfigureLexing(Action<ITextTemplateLexerConfigurationBuilder> configure);
        /// <summary>
        /// Configures the lexing compilation step.
        /// </summary>
        /// <param name="configure">Delegate used to configure the parsing compilation step</param>
        /// <returns>Current builder for method chaining</returns>
        ITextTemplateCompilationBuilder ConfigureParsing(Action<ITextTemplateParserConfigurationBuilder> configure);

        // Caching
        /// <summary>
        /// Overwrites any implicit cache key used for the compilation.
        /// </summary>
        /// <param name="key">The cache key to use when storing the compiled artifact</param>
        /// <returns>Current builder for method chainin</returns>
        ITextTemplateCompilationBuilder WithCacheKey(string key) => WithCacheKey<string>(key);
        /// <summary>
        /// Overwrites any implicit cache key used for the compilation.
        /// </summary>
        /// <typeparam name="T">The type of key to use</typeparam>
        /// <param name="key">The cache key to use when storing the compiled artifact</param>
        /// <returns>Current builder for method chainin</returns>
        ITextTemplateCompilationBuilder WithCacheKey<T>(T key);
        /// <summary>
        /// Overwrites any implicit cache options configured for the compilation.
        /// </summary>
        /// <param name="configure">Delegate that configures the option instance. First arg is the <see cref="ITextTemplateCompilationContext.CompilerProcess"/>, second arg is the option instance to configure</param>
        /// <returns>Current builder for method chainin</returns>
        ITextTemplateCompilationBuilder WithCacheOptions(Action<string, MemoryCacheEntryOptions> configure);
        /// <summary>
        /// Disables any implicit caching for the compilation.
        /// </summary>
        /// <returns>Current builder for method chainin</returns>
        ITextTemplateCompilationBuilder WithNoCaching();
    }
}
