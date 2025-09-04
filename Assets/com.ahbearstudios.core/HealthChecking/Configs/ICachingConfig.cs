using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for health check result caching configuration
    /// </summary>
    public interface ICachingConfig
    {
        /// <summary>
        /// Whether caching is enabled for health check results
        /// </summary>
        bool EnableCaching { get; }

        /// <summary>
        /// Duration to cache health check results
        /// </summary>
        TimeSpan CacheDuration { get; }

        /// <summary>
        /// Maximum number of cached results to keep in memory
        /// </summary>
        int MaxCachedResults { get; }

        /// <summary>
        /// Whether to use sliding expiration (refresh cache on access)
        /// </summary>
        bool UseSlidingExpiration { get; }

        /// <summary>
        /// Cache key prefix for health check results
        /// </summary>
        string CacheKeyPrefix { get; }

        /// <summary>
        /// Whether to compress cached data to save memory
        /// </summary>
        bool EnableCompression { get; }

        /// <summary>
        /// Compression level (0-9, higher = better compression but slower)
        /// </summary>
        int CompressionLevel { get; }

        /// <summary>
        /// Whether to cache only successful results or all results
        /// </summary>
        bool CacheOnlySuccessfulResults { get; }

        /// <summary>
        /// Custom cache tags for categorizing cached results
        /// </summary>
        HashSet<string> CacheTags { get; }

        /// <summary>
        /// Maximum memory usage for cache (in MB)
        /// </summary>
        int MaxCacheMemoryMB { get; }

        /// <summary>
        /// Interval for cache cleanup operations
        /// </summary>
        TimeSpan CacheCleanupInterval { get; }

        /// <summary>
        /// Whether to enable cache statistics collection
        /// </summary>
        bool EnableCacheStatistics { get; }

        /// <summary>
        /// Validates the caching configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        List<string> Validate();
    }
}