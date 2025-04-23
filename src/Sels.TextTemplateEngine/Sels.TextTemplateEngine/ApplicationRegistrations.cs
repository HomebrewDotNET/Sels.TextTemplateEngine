using Sels.Core;
using Sels.TextTemplateEngine;
using Sels.TextTemplateEngine.Compilation.Compiler;
using Sels.TextTemplateEngine.Compilation.Lexing;
using Sels.TextTemplateEngine.Compilation.Lexing.Tokens;
using Sels.TextTemplateEngine.Compilation.Parsing;
using Sels.TextTemplateEngine.Compilation.Parsing.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods for adding text engine related services to <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ApplicationRegistrations
    {
        /// <summary>
        /// Adds the text template engine compiler to the service collection.
        /// </summary>
        /// <param name="services">Collection to add the service registrations to</param>
        /// <param name="configure">Optional delegate to configure the compiler</param>
        /// <returns>
        /// <returns><paramref name="services"/> for method chaining</returns></returns>
        public static IServiceCollection AddTextTemplateCompiler(this IServiceCollection services, Action<TextTemplateCompilerOptions>? configure = null)
        {
            services = Guard.IsNotNull(services);
            // Parser
            services.AddTextTemplateParser();
            // Lexer
            services.AddTextTemplateLexer();
            // Compiler
            services.New<TextTemplateCompiler>()
                    .TryRegister();
            services.New<ITextTemplateCompiler, TextTemplateCompiler>()
                    .Trace(x => x.Duration.OfAll)
                    .AsSingleton()
                    .AsForwardedService()
                    .TryRegister();
            // Options
            services.AddOptions<TextTemplateCompilerOptions>()
                    .Configure(configure ?? (x => { }));
            services.BindOptionsFromConfig<TextTemplateCompilerOptions>(nameof(TextTemplateCompilerOptions), Sels.Core.Options.ConfigurationProviderNamedOptionBehaviour.Prefix, true);
            services.AddValidationProfile<TextTemplateCompilerOptionsValidationProfile, string>();
            services.AddOptionProfileValidator<TextTemplateCompilerOptions, TextTemplateCompilerOptionsValidationProfile>();

            return services;
        }

        /// <summary>
        /// Adds the text template engine parser and expression parsers to the service collection.
        /// </summary>
        /// <param name="services">Collection to add the service registrations to</param>
        /// <returns><paramref name="services"/> for method chaining</returns>
        public static IServiceCollection AddTextTemplateParser(this IServiceCollection services)
        {
            services = Guard.IsNotNull(services);
            // Parser
            services.New<TextTemplateParser>()
                    .TryRegister();
            services.New<ITextTemplateParser, TextTemplateParser>()
                    .Trace(x => x.Duration.OfAll)
                    .AsSingleton()
                    .AsForwardedService()
                    .TryRegister();

            // Expression parsers
            services.New<CommentParser>()
                    .ConstructWith(x => new CommentParser(1))
                    .TryRegister();
            services.New<ITextTemplateSyntaxExpressionParser, CommentParser>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .TryRegisterImplementation();

            services.New<AccessorExpressionParser>()
                    .ConstructWith(x => new AccessorExpressionParser(90))
                    .TryRegister();
            services.New<ITextTemplateSyntaxExpressionParser, AccessorExpressionParser>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .TryRegisterImplementation();

            services.New<InvocationExpressionParser>()
                    .ConstructWith(x => new InvocationExpressionParser(100))
                    .TryRegister();
            services.New<ITextTemplateSyntaxExpressionParser, InvocationExpressionParser>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .TryRegisterImplementation();

            return services;
        }

        /// <summary>
        /// Adds the text template engine lexer and token lexers to the service collection.
        /// </summary>
        /// <param name="services">Collection to add the service registrations to</param>
        /// <returns><paramref name="services"/> for method chaining</returns>
        public static IServiceCollection AddTextTemplateLexer(this IServiceCollection services)
        {
            services = Guard.IsNotNull(services);

            // Lexer
            services.New<TextTemplateLexer>()
                    .TryRegister();
            services.New<ITextTemplateLexer, TextTemplateLexer>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .AsSingleton()
                    .TryRegister();

            // Token lexers
            services.New<SimpleRecurringMatchingSetLexer<WhitespaceTextTemplateToken>>()
                    .ConstructWith(x => new SimpleRecurringMatchingSetLexer<WhitespaceTextTemplateToken>(char.IsWhiteSpace, (c, p, s) => new WhitespaceTextTemplateToken(s) { Position = new TokenPosition() { Index = p, Line = c.Line, LineIndex = c.BufferLineIndex } }, (byte)(NewLineTextTemplateTokenLexer.LexerPriority+1)))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleRecurringMatchingSetLexer<WhitespaceTextTemplateToken>>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .TryRegisterImplementation();

            services.New<NewLineTextTemplateTokenLexer>()
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, NewLineTextTemplateTokenLexer>()
                    .Trace(x => x.Duration.OfAll)
                    .AsForwardedService()
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<ExpressionStartToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ExpressionStartToken>(TextTemplateEngineConstants.Compilation.Syntax.StartToken, 10))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ExpressionStartToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<AccessorStartToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<AccessorStartToken>(TextTemplateEngineConstants.Compilation.Syntax.AccessorStartToken, 10))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<AccessorStartToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<CommentToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<CommentToken>(TextTemplateEngineConstants.Compilation.Syntax.CommentStartToken, 10))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<CommentToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<ExpressionEndToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ExpressionEndToken>(TextTemplateEngineConstants.Compilation.Syntax.EndToken, 10))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ExpressionEndToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleRecurringMatchingSetLexer<IdentifierToken>>()
                    .ConstructWith(x => new SimpleRecurringMatchingSetLexer<IdentifierToken>(char.IsLetterOrDigit, (c, p, s) => new IdentifierToken(s) { Position = new TokenPosition() { Index = p, Line = c.Line, LineIndex = c.BufferLineIndex } }, 25))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleRecurringMatchingSetLexer<IdentifierToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<ExpressionBlockToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ExpressionBlockToken>(TextTemplateEngineConstants.Compilation.Syntax.BlockStartToken, 50))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ExpressionBlockToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<ExpressionEndBlockToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ExpressionEndBlockToken>(TextTemplateEngineConstants.Compilation.Syntax.BlockEndToken, 50))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ExpressionEndBlockToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<InvocationNamedParameterStartToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<InvocationNamedParameterStartToken>(TextTemplateEngineConstants.Compilation.Syntax.InvocationNamedParameterStart, 50))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<InvocationNamedParameterStartToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<InvocationParameterOrGroupStartToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<InvocationParameterOrGroupStartToken>(TextTemplateEngineConstants.Compilation.Syntax.InvocationParameterOrGroupStart, 50))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<InvocationParameterOrGroupStartToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<InvocationParameterOrGroupEndToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<InvocationParameterOrGroupEndToken>(TextTemplateEngineConstants.Compilation.Syntax.InvocationParameterOrGroupEnd, 50))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<InvocationParameterOrGroupEndToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<PositionalParameterSplitToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<PositionalParameterSplitToken>(TextTemplateEngineConstants.Compilation.Syntax.PositionalParameterSplit, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<PositionalParameterSplitToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<NamedParameterSplitToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<NamedParameterSplitToken>(TextTemplateEngineConstants.Compilation.Syntax.NamedParameterSplit, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<NamedParameterSplitToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<AssignmentOperatorToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<AssignmentOperatorToken>(TextTemplateEngineConstants.Compilation.Syntax.AssignmentOperator, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<AssignmentOperatorToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<ElvisOperatorToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ElvisOperatorToken>(TextTemplateEngineConstants.Compilation.Syntax.ElvisOperator, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ElvisOperatorToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<SubPropertyOrIdentifierDivisorToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<SubPropertyOrIdentifierDivisorToken>(TextTemplateEngineConstants.Compilation.Syntax.SubPropertyOrIdentifierDivisor, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<SubPropertyOrIdentifierDivisorToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            return services;
        }
    }
}
