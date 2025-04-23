using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Base compilation context used by various compiler components to resolve options and services.
    /// </summary>
    public interface ITextTemplateCompilationContext
    {
        /// <summary>
        /// The compiler process that is being executed. 
        /// </summary>
        public string CompilerProcess { get; }
        /// <summary>
        /// Service provider scope started for the current compilation. Used to resolve lexers, parsers, analysers based on <see cref="CompilerProcess"/> to resolve named service.
        /// Can also be used by said services to resolve other dependencies / their options.
        /// </summary>
        public IServiceProvider CompilationScope { get; }
        /// <summary>
        /// Resolves options of type <typeparamref name="T"/> based on the current <see cref="CompilerProcess"/> and any custom configuration for the current compilation.
        /// Can be used by compiler components to resolve options configured for the current compilation.
        /// </summary>
        /// <typeparam name="T">The type of options to resolve</typeparam>
        /// <returns>The configured instance of <typeparamref name="T"/> for the current compilation</returns>
        public T GetOptions<T>() where T : class;
        /// <summary>
        /// Retrieves all registered compiler services of type <typeparamref name="T"/> based on the current <see cref="CompilerProcess"/> and any custom configuration for the current compilation.
        /// </summary>
        /// <typeparam name="T">The type of services to resolve</typeparam>
        /// <returns>All registered services of type <typeparamref name="T"/> for the current compilation if any</returns>
        public T[] GetCompilerServices<T>() where T : class;
    }
}
