using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Contains statistics about the compilation of an artifact.
    /// </summary>
    public interface ITextTemplateCompilationStatistics
    {
        /// <summary>
        /// Contains the time it took for a compilation process/components to complete.
        /// </summary>
        public IReadOnlyDictionary<string, TimeSpan> Performance { get; }
        /// <summary>
        /// Contains the time it took to lex the input.
        /// </summary>
        public TimeSpan LexDuration => Performance.TryGetValue(TextTemplateEngineConstants.Compilation.Statistics.PerformanceLexing, out TimeSpan duration) ? duration : TimeSpan.Zero;
        /// <summary>
        /// Contains the time it took to parse the tokens from the input into an abstract syntax tree.
        /// </summary>
        public TimeSpan ParseDuration => Performance.TryGetValue(TextTemplateEngineConstants.Compilation.Statistics.PerformanceParsing, out TimeSpan duration) ? duration : TimeSpan.Zero;
        /// <summary>
        /// Contains the time it took to analyze the abstract syntax tree.
        /// </summary>
        public TimeSpan AnalyseDuration => Performance.TryGetValue(TextTemplateEngineConstants.Compilation.Statistics.PerformanceAnalyzing, out TimeSpan duration) ? duration : TimeSpan.Zero;
        /// <summary>
        /// Contains the time it took to analyze the abstract syntax tree.
        /// </summary>
        public TimeSpan ArtifactCompilationDuration => Performance.TryGetValue(TextTemplateEngineConstants.Compilation.Statistics.PerformanceArtifactCompilation, out TimeSpan duration) ? duration : TimeSpan.Zero;
        /// <summary>
        /// Contains the time it took to compile the artifact including all steps + overhead.
        /// </summary>
        public TimeSpan Duration => Performance.TryGetValue(TextTemplateEngineConstants.Compilation.Statistics.PerformanceCompilation, out TimeSpan duration) ? duration : TimeSpan.Zero;
    }
}
