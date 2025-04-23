using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine
{
    /// <summary>
    /// Contains the logging parameters related to the text template engine.
    /// </summary>
    public static class TextTemplateEngineLog
    {
        /// <summary>
        /// Log parameter that contains the name of the process that was used to compile an artifact.
        /// </summary>
        public const string CompilerProcess = "TextTemplateEngine.CompilerProcess";
        /// <summary>
        /// Log parameter that contains the cache key used to store an artifact.
        /// </summary>
        public const string ArtifactCacheKey = "TextTemplateEngine.ArtifactCacheKey";
    }
}
