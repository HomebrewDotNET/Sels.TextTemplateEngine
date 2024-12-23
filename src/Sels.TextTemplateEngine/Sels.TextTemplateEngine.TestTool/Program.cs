using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Reflection;
using Sels.TextTemplateEngine.Compilation;
using Sels.TextTemplateEngine.Compilation.Lexing;
using System.Runtime.Versioning;

namespace Sels.TextTemplateEngine.TestTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Helper.Console.RunAsync(() => ReadSyntaxTreeFromFile("TestTemplate.txt"));
        }

        private static async Task ReadTokensFromFile(string filePath) 
        { 
            filePath = Guard.IsNotNullOrWhitespace(filePath);

            var provider = new ServiceCollection()
                .AddTextTemplateLexer()
                .AddLogging(x =>
                {
                    x.AddConsole();
                })
                .BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var lexer = provider.GetRequiredService<ITextTemplateLexer>();
            var file = new FileInfo(filePath);
            logger.Log($"Lexing file <{file}> into tokens");

            await foreach(var token in lexer.LexAsync(provider.GetServices<ITextTemplateTokenLexer>(), file.OpenRead(), cancellationToken: CancellationToken.None))
            {
                logger.Log($"Token <{token.Type}> of length <{token.Length}> found at <{token.Position}>");
            }
        }

        private static async Task ReadSyntaxTreeFromFile(string filePath)
        {
            filePath = Guard.IsNotNullOrWhitespace(filePath);
            var provider = new ServiceCollection()
                .AddTextTemplateLexer()
                .AddTextTemplateParser()
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.AddFilter(typeof(TextTemplateLexer).Namespace, LogLevel.Warning);
                })
                .BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<Program>>();
            var lexer = provider.GetRequiredService<ITextTemplateLexer>();
            var parser = provider.GetRequiredService<ITextTemplateParser>();
            var file = new FileInfo(filePath);
            logger.Log($"Lexing file <{file}> into tokens");
            var tokens = lexer.LexAsync(provider.GetServices<ITextTemplateTokenLexer>(), file.OpenRead(), cancellationToken: CancellationToken.None);
            var syntaxTree = await parser.ParseAsync(provider.GetServices<ITextTemplateExpressionParser>(), tokens, CancellationToken.None);
            await Helper.Async.Sleep(1000).ConfigureAwait(false);
            Console.WriteLine(syntaxTree.ToString(x => x.GetType().GetDisplayName(false)));
        }
    }
}
