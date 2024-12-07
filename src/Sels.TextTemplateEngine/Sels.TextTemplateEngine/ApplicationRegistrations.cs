using Sels.Core;
using Sels.TextTemplateEngine;
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
                    .AsForwardedService()
                    .TryRegister();

            // Expression parsers
            services.New<CommentParser>()
                    .ConstructWith(x => new CommentParser(1))
                    .TryRegister();
            services.New<ITextTemplateExpressionParser, CommentParser>()
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
                    .TryRegister();

            // Token lexers
            services.New<SimpleRecurringMatchingSetLexer<WhitespaceTextTemplateToken>>()
                    .ConstructWith(x => new SimpleRecurringMatchingSetLexer<WhitespaceTextTemplateToken>(char.IsWhiteSpace, (c, p, s) => new WhitespaceTextTemplateToken(s) { Position = p, Line = c.Line}, (byte)(NewLineTextTemplateTokenLexer.LexerPriority+1)))
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
                    .ConstructWith(x => new SimpleRecurringMatchingSetLexer<IdentifierToken>(char.IsLetterOrDigit, (c, p, s) => new IdentifierToken(s) { Position = p, Line = c.Line }, 25))
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

            services.New<SimpleSequenceTokenLexer<ElvisOperatorToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<ElvisOperatorToken>(TextTemplateEngineConstants.Compilation.Syntax.ElvisOperator, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<ElvisOperatorToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            services.New<SimpleSequenceTokenLexer<SubPropertyDivisorToken>>()
                    .ConstructWith(x => new SimpleSequenceTokenLexer<SubPropertyDivisorToken>(TextTemplateEngineConstants.Compilation.Syntax.SubPropertyDivisor, 100))
                    .AsSingleton()
                    .TryRegister();
            services.New<ITextTemplateTokenLexer, SimpleSequenceTokenLexer<SubPropertyDivisorToken>>()
                    .AsForwardedService()
                    .Trace(x => x.Duration.OfAll)
                    .TryRegisterImplementation();

            return services;
        }
    }
}
