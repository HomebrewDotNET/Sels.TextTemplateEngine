using Sels.TextTemplateEngine.Expressions.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Represents an artifact of type <typeparamref name="T"/> that was compiled using a <see cref="ITextTemplateCompiler"/>.
    /// </summary>
    /// <typeparam name="T">The type of the compiled artifact</typeparam>
    public interface ITextTemplateArtifact<T>
    {
        // Compilation information
        /// <summary>
        /// Indicates if the compilation for the artifact is available.
        /// Can be false if the artifact was retrieved from a cache, ...
        /// </summary>
        public bool CompilationInfoAvailable { get; }
        /// <summary>
        /// The compiler process that was used to compile the artifact.
        /// </summary>
        public string CompilerProcess { get; }
        /// <summary>
        /// The syntax tree that was used to compile the artifact.
        /// Only set if <see cref="CompilationInfoAvailable"/> is true.
        /// </summary>
        public AbstractSyntaxTreeExpression? SyntaxTree { get; }
        /// <summary>
        /// The stream that was used to compile the artifact.
        /// Only set if <see cref="CompilationInfoAvailable"/> is true.
        /// </summary>
        public Stream? Stream { get; }
        /// <summary>
        /// The compilation statistics for the artifact.
        /// Only set if <see cref="CompilationInfoAvailable"/> is true.
        /// </summary>
        public ITextTemplateCompilationStatistics? Statistics { get; }

        // Artifact
        /// <summary>
        /// The artifact that was compiled.
        /// </summary>
        public T Artifact { get; }
        /// <summary>
        /// Indicates if the artifact is available in the cache.
        /// </summary>
        public bool Cached { get; }
    }
}
