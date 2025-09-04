using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Configuration for health check result caching
    /// </summary>
    public sealed record CachingConfig : ICachingConfig
    {
        /// <summary>
        /// Whether caching is enabled for health check results
        /// </summary>
        public bool EnableCaching { get; init; } = false;

        /// <summary>
        /// Duration to cache health check results
        /// </summary>
        public TimeSpan CacheDuration { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of cached results to keep in memory
        /// </summary>
        public int MaxCachedResults { get; init; } = 1000;

        /// <summary>
        /// Whether to use sliding expiration (refresh cache on access)
        /// </summary>
        public bool UseSlidingExpiration { get; init; } = true;

        /// <summary>
        /// Cache key prefix for health check results
        /// </summary>
        public string CacheKeyPrefix { get; init; } = "HealthCheck";

        /// <summary>
        /// Whether to compress cached data to save memory
        /// </summary>
        public bool EnableCompression { get; init; } = false;

        /// <summary>
        /// Compression level (0-9, higher = better compression but slower)
        /// </summary>
        public int CompressionLevel { get; init; } = 6;

        /// <summary>
        /// Whether to cache only successful results or all results
        /// </summary>
        public bool CacheOnlySuccessfulResults { get; init; } = false;

        /// <summary>
        /// Custom cache tags for categorizing cached results
        /// </summary>
        public HashSet<string> CacheTags { get; init; } = new();

        /// <summary>
        /// Maximum memory usage for cache (in MB)
        /// </summary>
        public int MaxCacheMemoryMB { get; init; } = 50;

        /// <summary>
        /// Interval for cache cleanup operations
        /// </summary>
        public TimeSpan CacheCleanupInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable cache statistics collection
        /// </summary>
        public bool EnableCacheStatistics { get; init; } = true;

        /// <summary>
        /// Validates the caching configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (CacheDuration <= TimeSpan.Zero)
                errors.Add("CacheDuration must be greater than zero");

            if (CacheDuration > TimeSpan.FromHours(24))
                errors.Add("CacheDuration should not exceed 24 hours for practical purposes");

            if (MaxCachedResults < 0)
                errors.Add("MaxCachedResults must be non-negative");

            if (MaxCachedResults > 100000)
                errors.Add("MaxCachedResults should not exceed 100,000 for memory efficiency");

            if (string.IsNullOrWhiteSpace(CacheKeyPrefix))
                errors.Add("CacheKeyPrefix cannot be null or empty");

            if (CompressionLevel < 0 || CompressionLevel > 9)
                errors.Add("CompressionLevel must be between 0 and 9");

            if (MaxCacheMemoryMB <= 0)
                errors.Add("MaxCacheMemoryMB must be greater than zero");

            if (MaxCacheMemoryMB > 1024)
                errors.Add("MaxCacheMemoryMB should not exceed 1024 MB for practical purposes");

            if (CacheCleanupInterval <= TimeSpan.Zero)
                errors.Add("CacheCleanupInterval must be greater than zero");

            return errors;
        }

        /// <summary>
        /// Creates a caching configuration for production environments
        /// </summary>
        /// <returns>Production-optimized caching configuration</returns>
        public static CachingConfig ForProduction()
        {
            return new CachingConfig
            {
                EnableCaching = true,
                CacheDuration = TimeSpan.FromMinutes(5),
                MaxCachedResults = 5000,
                UseSlidingExpiration = true,
                CacheKeyPrefix = "HealthCheck_Prod",
                EnableCompression = true,
                CompressionLevel = 6,
                CacheOnlySuccessfulResults = false,
                MaxCacheMemoryMB = 100,
                CacheCleanupInterval = TimeSpan.FromMinutes(10),
                EnableCacheStatistics = true
            };
        }

        /// <summary>
        /// Creates a caching configuration for development environments
        /// </summary>
        /// <returns>Development-optimized caching configuration</returns>
        public static CachingConfig ForDevelopment()
        {
            return new CachingConfig
            {
                EnableCaching = false,
                CacheDuration = TimeSpan.FromSeconds(30),
                MaxCachedResults = 100,
                UseSlidingExpiration = false,
                CacheKeyPrefix = "HealthCheck_Dev",
                EnableCompression = false,
                CacheOnlySuccessfulResults = false,
                MaxCacheMemoryMB = 10,
                CacheCleanupInterval = TimeSpan.FromMinutes(2),
                EnableCacheStatistics = true
            };
        }

        /// <summary>
        /// Creates a minimal caching configuration
        /// </summary>
        /// <returns>Minimal caching configuration</returns>
        public static CachingConfig Minimal()
        {
            return new CachingConfig
            {
                EnableCaching = false,
                CacheDuration = TimeSpan.FromSeconds(10),
                MaxCachedResults = 50,
                UseSlidingExpiration = false,
                CacheKeyPrefix = "HealthCheck_Min",
                EnableCompression = false,
                CacheOnlySuccessfulResults = true,
                MaxCacheMemoryMB = 5,
                CacheCleanupInterval = TimeSpan.FromMinutes(1),
                EnableCacheStatistics = false
            };
        }
    }
}