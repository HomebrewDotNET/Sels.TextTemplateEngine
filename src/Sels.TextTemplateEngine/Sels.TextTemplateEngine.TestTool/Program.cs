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
                .AddTextTemplateCompiler()
                .AddLogging(x =>
                {
                    x.AddConsole();
                })
                .BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var compiler = provider.GetRequiredService<ITextTemplateCompiler>();
            var file = new FileInfo(filePath);
            logger.Log($"Lexing file <{file}> into tokens");

            await foreach(var token in compiler.LexAsync(file.OpenRead(), ownsStream: true))
            {
                logger.Log($"Token <{token.Type}> of length <{token.Length}> found at <{token.Position}>");
            }
        }

        private static async Task ReadSyntaxTreeFromFile(string filePath)
        {
            filePath = Guard.IsNotNullOrWhitespace(filePath);
            var provider = new ServiceCollection()
                .AddTextTemplateCompiler()
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.AddFilter(typeof(TextTemplateLexer).Namespace, LogLevel.Warning);
                })
                .BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<Program>>();
            var compiler = provider.GetRequiredService<ITextTemplateCompiler>();
            var file = new FileInfo(filePath);
            logger.Log($"Lexing file <{file}> into tokens");
            var syntaxTree = await compiler.ParseAsync(file.OpenRead(), ownsStream: true);
            await Helper.Async.Sleep(1000).ConfigureAwait(false);
            Console.WriteLine(syntaxTree.ToString(x => x.GetType().GetDisplayName(false)));
        }
    }
}
