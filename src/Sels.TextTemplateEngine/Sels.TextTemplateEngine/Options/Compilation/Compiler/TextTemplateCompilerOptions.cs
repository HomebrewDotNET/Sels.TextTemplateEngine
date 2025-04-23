using Microsoft.Extensions.Caching.Memory;
using Sels.ObjectValidationFramework.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Compiler
{
    /// <summary>
    /// Contains the options for the <see cref="ITextTemplateCompiler"/> to use when compiling a template.
    /// </summary>
    public class TextTemplateCompilerOptions
    {
        /// <summary>
        /// Indicates if the compiler should gather statistics when compiling artifacts.
        /// </summary>
        public bool EnableStatistics { get; set; } = false;

        // Caching
        /// <summary>
        /// Indicates if the compiler should cache the artifacts it compiles.
        /// </summary>
        public bool EnableCaching { get; set; } = true;
        /// <summary>
        /// The maximum size in bytes of streams whoes artifact should be cached.
        /// If a stream is larger than the maximum caching will be disabled even if otherwise enabled.
        /// When set to null everything can be cached.
        /// </summary>
        public long? MaxCachableLength { get; set; } = 32768; // Only cache up to 32 KiB by default
        /// <summary>
        /// Indicates if the compiler should treat unknown stream sizes as cachable.
        /// Only used when <see cref="MaxCachableLength"/> is used.
        /// By default they are assumed to be above <see cref="MaxCachableLength"/>.
        /// </summary>
        public bool TreatUnknownStreamSizeAsCachable { get; set; } = false;
        /// <summary>
        /// The default cache options to use when caching artifacts.
        /// Can be overridden by the configuration delegates.
        /// </summary>
        public MemoryCacheEntryOptions DefaultCacheOptions { get; set; } = new MemoryCacheEntryOptions()
        {
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Low
        };

        /// <summary>
        /// The default delegate that will be used to configure the cache options for the artifact.
        /// </summary>
        public Action<string, MemoryCacheEntryOptions>? DefaultConfigureCacheOptions { get; set; }
        /// <summary>
        /// The default delegate that will be used to configure the lexing of the artifact.
        /// </summary>
        public Action<ITextTemplateLexerConfigurationBuilder>? DefaultConfigureLexing { get; private set; }
        /// <summary>
        /// The default delegate that will be used to configure the parsing of the artifact.
        /// </summary>
        public Action<ITextTemplateParserConfigurationBuilder>? DefaultConfigureParsing { get; private set; }
    }
    /// <summary>
    /// Contains the validation rules for <see cref="TextTemplateCompilerOptions"/>.
    /// </summary>
    public class TextTemplateCompilerOptionsValidationProfile : ValidationProfile<string>
    {
        /// <inheritdoc />
        public TextTemplateCompilerOptionsValidationProfile()
        {
            CreateValidationFor<TextTemplateCompilerOptions>()
                .ForProperty(x => x.MaxCachableLength, x => x!.Value)
                    .MustBeLargerThan(0L)
                .ForProperty(x => x.DefaultCacheOptions)
                    .CannotBeNull();
        }
    }
}
