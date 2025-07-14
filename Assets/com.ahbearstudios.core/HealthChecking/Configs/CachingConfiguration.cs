using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Comprehensive caching configuration for health check results with advanced features
    /// including distributed caching, cache warming, and intelligent invalidation strategies
    /// </summary>
    public sealed record CachingConfig : IValidatable
    {
        #region Core Caching Settings

        /// <summary>
        /// Whether caching is enabled for health check results
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Cache key prefix for this health check's cached results
        /// </summary>
        public string CacheKeyPrefix { get; init; } = "healthcheck";

        /// <summary>
        /// Time-to-live for cached results (5 seconds to 1 hour)
        /// </summary>
        public TimeSpan TimeToLive { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of cached results to maintain per health check
        /// </summary>
        [Range(1, 1000)]
        public int MaxCacheSize { get; init; } = 100;

        /// <summary>
        /// Cache storage strategy for different scenarios
        /// </summary>
        public CacheStorageStrategy StorageStrategy { get; init; } = CacheStorageStrategy.Memory;

        /// <summary>
        /// Cache serialization format for persistent storage
        /// </summary>
        public CacheSerializationFormat SerializationFormat { get; init; } = CacheSerializationFormat.Json;

        #endregion

        #region Cache Invalidation

        /// <summary>
        /// Cache invalidation strategy to use
        /// </summary>
        public CacheInvalidationStrategy InvalidationStrategy { get; init; } = CacheInvalidationStrategy.TimeBasedOnly;

        /// <summary>
        /// Whether to invalidate cache on health status changes
        /// </summary>
        public bool InvalidateOnStatusChange { get; init; } = true;

        /// <summary>
        /// Whether to invalidate cache on configuration changes
        /// </summary>
        public bool InvalidateOnConfigChange { get; init; } = true;

        /// <summary>
        /// Whether to invalidate cache when dependencies change
        /// </summary>
        public bool InvalidateOnDependencyChange { get; init; } = false;

        /// <summary>
        /// Custom cache invalidation triggers
        /// </summary>
        public HashSet<string> CustomInvalidationTriggers { get; init; } = new();

        /// <summary>
        /// Time window for batch invalidation operations
        /// </summary>
        public TimeSpan BatchInvalidationWindow { get; init; } = TimeSpan.FromSeconds(1);

        #endregion

        #region Cache Warming

        /// <summary>
        /// Whether to enable cache warming for predictive loading
        /// </summary>
        public bool EnableCacheWarming { get; init; } = false;

        /// <summary>
        /// Cache warming strategy to use
        /// </summary>
        public CacheWarmingStrategy WarmingStrategy { get; init; } = CacheWarmingStrategy.Scheduled;

        /// <summary>
        /// Time before cache expiration to trigger warming
        /// </summary>
        public TimeSpan WarmingThreshold { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Maximum concurrent warming operations
        /// </summary>
        [Range(1, 10)]
        public int MaxConcurrentWarming { get; init; } = 3;

        /// <summary>
        /// Whether to allow background warming operations
        /// </summary>
        public bool AllowBackgroundWarming { get; init; } = true;

        /// <summary>
        /// Priority for cache warming operations
        /// </summary>
        [Range(1, 10)]
        public int WarmingPriority { get; init; } = 5;

        #endregion

        #region Distributed Caching

        /// <summary>
        /// Whether to enable distributed caching across multiple instances
        /// </summary>
        public bool EnableDistributedCaching { get; init; } = false;

        /// <summary>
        /// Distributed cache provider configuration
        /// </summary>
        public DistributedCacheProvider DistributedProvider { get; init; } = DistributedCacheProvider.Memory;

        /// <summary>
        /// Connection string for distributed cache (Redis, SQL Server, etc.)
        /// </summary>
        public string DistributedConnectionString { get; init; } = string.Empty;

        /// <summary>
        /// Cache key namespace for distributed scenarios
        /// </summary>
        public string DistributedNamespace { get; init; } = "healthcheck";

        /// <summary>
        /// Whether to use cache replication across instances
        /// </summary>
        public bool EnableCacheReplication { get; init; } = false;

        /// <summary>
        /// Cache consistency level for distributed operations
        /// </summary>
        public CacheConsistencyLevel ConsistencyLevel { get; init; } = CacheConsistencyLevel.Eventual;

        #endregion

        #region Performance Optimization

        /// <summary>
        /// Whether to enable cache compression for large results
        /// </summary>
        public bool EnableCompression { get; init; } = false;

        /// <summary>
        /// Compression algorithm to use
        /// </summary>
        public CompressionAlgorithm CompressionAlgorithm { get; init; } = CompressionAlgorithm.Gzip;

        /// <summary>
        /// Minimum data size threshold for compression (bytes)
        /// </summary>
        [Range(100, 10000)]
        public int CompressionThreshold { get; init; } = 1024;

        /// <summary>
        /// Whether to enable cache partitioning for better performance
        /// </summary>
        public bool EnablePartitioning { get; init; } = false;

        /// <summary>
        /// Number of cache partitions to create
        /// </summary>
        [Range(1, 100)]
        public int PartitionCount { get; init; } = 10;

        /// <summary>
        /// Cache eviction policy when maximum size is reached
        /// </summary>
        public CacheEvictionPolicy EvictionPolicy { get; init; } = CacheEvictionPolicy.LeastRecentlyUsed;

        #endregion

        #region Monitoring and Diagnostics

        /// <summary>
        /// Whether to enable cache hit/miss statistics
        /// </summary>
        public bool EnableStatistics { get; init; } = true;

        /// <summary>
        /// Whether to enable detailed cache operation logging
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;

        /// <summary>
        /// Whether to track cache performance metrics
        /// </summary>
        public bool EnablePerformanceMetrics { get; init; } = true;

        /// <summary>
        /// Cache operation timeout for monitoring
        /// </summary>
        public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Whether to enable cache health monitoring
        /// </summary>
        public bool EnableHealthMonitoring { get; init; } = true;

        /// <summary>
        /// Threshold for cache operation warnings (milliseconds)
        /// </summary>
        [Range(10, 10000)]
        public int SlowOperationThreshold { get; init; } = 100;

        #endregion

        #region Security and Encryption

        /// <summary>
        /// Whether to enable encryption for cached data
        /// </summary>
        public bool EnableEncryption { get; init; } = false;

        /// <summary>
        /// Encryption algorithm to use for cache data
        /// </summary>
        public EncryptionAlgorithm EncryptionAlgorithm { get; init; } = EncryptionAlgorithm.AES256;

        /// <summary>
        /// Whether to hash cache keys for security
        /// </summary>
        public bool HashCacheKeys { get; init; } = false;

        /// <summary>
        /// Hash algorithm for cache key hashing
        /// </summary>
        public HashAlgorithm KeyHashAlgorithm { get; init; } = HashAlgorithm.SHA256;

        /// <summary>
        /// Whether to enable cache access control
        /// </summary>
        public bool EnableAccessControl { get; init; } = false;

        /// <summary>
        /// Allowed cache access patterns
        /// </summary>
        public HashSet<string> AllowedAccessPatterns { get; init; } = new();

        #endregion

        #region Advanced Features

        /// <summary>
        /// Whether to enable multi-level caching (L1, L2, etc.)
        /// </summary>
        public bool EnableMultiLevelCaching { get; init; } = false;

        /// <summary>
        /// Cache levels configuration
        /// </summary>
        public List<CacheLevelConfig> CacheLevels { get; init; } = new();

        /// <summary>
        /// Whether to enable cache versioning for compatibility
        /// </summary>
        public bool EnableVersioning { get; init; } = false;

        /// <summary>
        /// Current cache schema version
        /// </summary>
        public string SchemaVersion { get; init; } = "1.0";

        /// <summary>
        /// Whether to enable cache migration for version changes
        /// </summary>
        public bool EnableMigration { get; init; } = false;

        /// <summary>
        /// Custom cache policies for specific scenarios
        /// </summary>
        public Dictionary<string, CachePolicyConfig> CustomPolicies { get; init; } = new();

        #endregion

        #region Validation

        /// <summary>
        /// Validates the caching configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Basic validation
            if (Enabled)
            {
                if (TimeToLive <= TimeSpan.Zero)
                    errors.Add("TimeToLive must be greater than zero when caching is enabled");

                if (TimeToLive > TimeSpan.FromHours(24))
                    errors.Add("TimeToLive should not exceed 24 hours for stability");

                if (MaxCacheSize < 1)
                    errors.Add("MaxCacheSize must be at least 1 when caching is enabled");

                if (string.IsNullOrWhiteSpace(CacheKeyPrefix))
                    errors.Add("CacheKeyPrefix cannot be empty when caching is enabled");
            }

            // Cache warming validation
            if (EnableCacheWarming)
            {
                if (!Enabled)
                    errors.Add("Cache warming requires caching to be enabled");

                if (WarmingThreshold >= TimeToLive)
                    errors.Add("WarmingThreshold must be less than TimeToLive");

                if (MaxConcurrentWarming < 1)
                    errors.Add("MaxConcurrentWarming must be at least 1");
            }

            // Distributed caching validation
            if (EnableDistributedCaching)
            {
                if (!Enabled)
                    errors.Add("Distributed caching requires basic caching to be enabled");

                if (DistributedProvider != DistributedCacheProvider.Memory &&
                    string.IsNullOrWhiteSpace(DistributedConnectionString))
                    errors.Add("DistributedConnectionString is required for non-memory distributed providers");

                if (string.IsNullOrWhiteSpace(DistributedNamespace))
                    errors.Add("DistributedNamespace cannot be empty for distributed caching");
            }

            // Compression validation
            if (EnableCompression)
            {
                if (CompressionThreshold < 100)
                    errors.Add("CompressionThreshold must be at least 100 bytes");
            }

            // Partitioning validation
            if (EnablePartitioning)
            {
                if (PartitionCount < 1)
                    errors.Add("PartitionCount must be at least 1");

                if (PartitionCount > MaxCacheSize)
                    errors.Add("PartitionCount cannot exceed MaxCacheSize");
            }

            // Multi-level caching validation
            if (EnableMultiLevelCaching)
            {
                if (!CacheLevels.Any())
                    errors.Add("At least one cache level must be configured for multi-level caching");

                foreach (var level in CacheLevels)
                {
                    var levelErrors = level.Validate();
                    errors.AddRange(levelErrors.Select(e => $"Cache Level {level.Level}: {e}"));
                }
            }

            // Security validation
            if (EnableEncryption && EnableDistributedCaching && DistributedProvider == DistributedCacheProvider.Memory)
            {
                errors.Add("Encryption is not necessary for in-memory distributed caching");
            }

            // Performance validation
            if (OperationTimeout <= TimeSpan.Zero)
                errors.Add("OperationTimeout must be greater than zero");

            if (SlowOperationThreshold < 1)
                errors.Add("SlowOperationThreshold must be at least 1 millisecond");

            // Custom policies validation
            foreach (var policy in CustomPolicies)
            {
                var policyErrors = policy.Value.Validate();
                errors.AddRange(policyErrors.Select(e => $"Custom Policy '{policy.Key}': {e}"));
            }

            return errors;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a basic in-memory cache configuration
        /// </summary>
        /// <param name="ttl">Time-to-live for cached results</param>
        /// <param name="maxSize">Maximum cache size</param>
        /// <returns>Basic cache configuration</returns>
        public static CachingConfig CreateBasic(TimeSpan ttl, int maxSize = 100)
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = ttl,
                MaxCacheSize = maxSize,
                StorageStrategy = CacheStorageStrategy.Memory,
                InvalidationStrategy = CacheInvalidationStrategy.TimeBasedOnly,
                EnableStatistics = true
            };
        }

        /// <summary>
        /// Creates a high-performance cache configuration
        /// </summary>
        /// <param name="ttl">Time-to-live for cached results</param>
        /// <returns>High-performance cache configuration</returns>
        public static CachingConfig CreateHighPerformance(TimeSpan ttl)
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = ttl,
                MaxCacheSize = 1000,
                StorageStrategy = CacheStorageStrategy.Memory,
                EnablePartitioning = true,
                PartitionCount = 10,
                EvictionPolicy = CacheEvictionPolicy.LeastRecentlyUsed,
                EnableCacheWarming = true,
                WarmingStrategy = CacheWarmingStrategy.Predictive,
                WarmingThreshold = TimeSpan.FromSeconds(30),
                EnableStatistics = true,
                EnablePerformanceMetrics = true
            };
        }

        /// <summary>
        /// Creates a distributed cache configuration
        /// </summary>
        /// <param name="provider">Distributed cache provider</param>
        /// <param name="connectionString">Connection string for the provider</param>
        /// <param name="ttl">Time-to-live for cached results</param>
        /// <returns>Distributed cache configuration</returns>
        public static CachingConfig CreateDistributed(
            DistributedCacheProvider provider,
            string connectionString,
            TimeSpan ttl)
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = ttl,
                MaxCacheSize = 500,
                StorageStrategy = CacheStorageStrategy.Distributed,
                EnableDistributedCaching = true,
                DistributedProvider = provider,
                DistributedConnectionString = connectionString,
                EnableCacheReplication = true,
                ConsistencyLevel = CacheConsistencyLevel.Strong,
                EnableCompression = true,
                CompressionAlgorithm = CompressionAlgorithm.Gzip,
                EnableStatistics = true,
                EnableHealthMonitoring = true
            };
        }

        /// <summary>
        /// Creates a secure cache configuration with encryption
        /// </summary>
        /// <param name="ttl">Time-to-live for cached results</param>
        /// <returns>Secure cache configuration</returns>
        public static CachingConfig CreateSecure(TimeSpan ttl)
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = ttl,
                MaxCacheSize = 200,
                StorageStrategy = CacheStorageStrategy.Persistent,
                EnableEncryption = true,
                EncryptionAlgorithm = EncryptionAlgorithm.AES256,
                HashCacheKeys = true,
                KeyHashAlgorithm = HashAlgorithm.SHA256,
                EnableAccessControl = true,
                SerializationFormat = CacheSerializationFormat.Binary,
                EnableDetailedLogging = true,
                EnableStatistics = true
            };
        }

        /// <summary>
        /// Creates a development-friendly cache configuration
        /// </summary>
        /// <returns>Development cache configuration</returns>
        public static CachingConfig CreateDevelopment()
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = TimeSpan.FromMinutes(2),
                MaxCacheSize = 50,
                StorageStrategy = CacheStorageStrategy.Memory,
                InvalidationStrategy = CacheInvalidationStrategy.Aggressive,
                InvalidateOnStatusChange = true,
                InvalidateOnConfigChange = true,
                EnableDetailedLogging = true,
                EnableStatistics = true,
                EnablePerformanceMetrics = false
            };
        }

        /// <summary>
        /// Creates a production-ready cache configuration
        /// </summary>
        /// <param name="ttl">Time-to-live for cached results</param>
        /// <returns>Production cache configuration</returns>
        public static CachingConfig CreateProduction(TimeSpan ttl)
        {
            return new CachingConfig
            {
                Enabled = true,
                TimeToLive = ttl,
                MaxCacheSize = 1000,
                StorageStrategy = CacheStorageStrategy.Hybrid,
                EnablePartitioning = true,
                PartitionCount = 20,
                EvictionPolicy = CacheEvictionPolicy.LeastRecentlyUsed,
                EnableCacheWarming = true,
                WarmingStrategy = CacheWarmingStrategy.Scheduled,
                WarmingThreshold = TimeSpan.FromMinutes(1),
                EnableDistributedCaching = true,
                DistributedProvider = DistributedCacheProvider.Redis,
                EnableCacheReplication = true,
                ConsistencyLevel = CacheConsistencyLevel.Eventual,
                EnableCompression = true,
                CompressionThreshold = 1024,
                EnableStatistics = true,
                EnablePerformanceMetrics = true,
                EnableHealthMonitoring = true,
                SlowOperationThreshold = 50
            };
        }

        #endregion
    }
}
