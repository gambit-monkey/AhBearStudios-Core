using System;
using AhBearStudios.Core.Pooling.Configurations;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for thread-local pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for thread-local pooling with a fluent API.
    /// </summary>
    public class ThreadLocalPoolConfigBuilder : IPoolConfigBuilder<ThreadLocalPoolConfig, ThreadLocalPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ThreadLocalPoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public ThreadLocalPoolConfigBuilder()
        {
            _config = new ThreadLocalPoolConfig();
        }

        /// <summary>
        /// Creates a new builder initialized with an existing thread-local configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadLocalPoolConfigBuilder(ThreadLocalPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = config.Clone() as ThreadLocalPoolConfig
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder initialized with a generic pool configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadLocalPoolConfigBuilder(IPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = new ThreadLocalPoolConfig(config);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential expansion
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets the growth increment for linear expansion
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets the maximum number of threads that can have pools
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithMaxThreadCount(int maxThreadCount)
        {
            _config.MaxThreadCount = Mathf.Max(1, maxThreadCount);
            return this;
        }

        /// <summary>
        /// Sets whether thread pools are created on demand
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithCreateThreadPoolsOnDemand(bool createOnDemand)
        {
            _config.CreateThreadPoolsOnDemand = createOnDemand;
            return this;
        }

        /// <summary>
        /// Sets whether to maintain a global fallback pool
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithGlobalFallbackPool(bool maintainGlobalPool)
        {
            _config.MaintainGlobalFallbackPool = maintainGlobalPool;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically clean up unused thread pools
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithAutoCleanupUnusedThreadPools(bool autoCleanup)
        {
            _config.AutoCleanupUnusedThreadPools = autoCleanup;
            return this;
        }

        /// <summary>
        /// Sets the cleanup threshold for unused thread pools
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithThreadPoolCleanupThreshold(float cleanupThreshold)
        {
            _config.ThreadPoolCleanupThreshold = Mathf.Max(0f, cleanupThreshold);
            return this;
        }

        /// <summary>
        /// Sets whether to track object thread affinity
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithObjectThreadAffinity(bool trackAffinity)
        {
            _config.TrackObjectThreadAffinity = trackAffinity;
            return this;
        }

        /// <summary>
        /// Sets whether objects return to their creator thread's pool
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithReturnToCreatorThreadPool(bool returnToCreatorPool)
        {
            _config.ReturnToCreatorThreadPool = returnToCreatorPool;
            if (returnToCreatorPool)
            {
                _config.TrackObjectThreadAffinity = true;
            }

            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for the pool
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to enable detailed logging for the pool
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings for the pool
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects on release
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithShrinkThreshold(float shrinkThreshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(shrinkThreshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for any native collections
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when exceeding max count
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// Note: This will be forced to ThreadLocal for thread-local pools
        /// </summary>
        public ThreadLocalPoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            // Thread-local pools are always in ThreadLocal mode, but we'll store the setting anyway
            return this;
        }

        /// <summary>
        /// Configures the builder with high-performance settings optimized for throughput
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadLocalPoolConfigBuilder AsHighPerformance()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 32);
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.MaxThreadCount = 32;
            _config.CreateThreadPoolsOnDemand = true;
            _config.AutoCleanupUnusedThreadPools = true;
            _config.ThreadPoolCleanupThreshold = 60f;
            _config.TrackObjectThreadAffinity = false;
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;
            _config.EnableAutoShrink = false;
            return this;
        }

        /// <summary>
        /// Configures the builder with debug-friendly settings for development
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadLocalPoolConfigBuilder AsDebug()
        {
            _config.MaximumCapacity = _config.InitialCapacity * 4;
            _config.MaxThreadCount = 16;
            _config.CreateThreadPoolsOnDemand = true;
            _config.MaintainGlobalFallbackPool = true;
            _config.AutoCleanupUnusedThreadPools = false;
            _config.TrackObjectThreadAffinity = true;
            _config.CollectMetrics = true;
            _config.DetailedLogging = true;
            _config.LogWarnings = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with settings optimized for thread affinity tracking
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadLocalPoolConfigBuilder AsWithAffinity()
        {
            _config.MaxThreadCount = 16;
            _config.TrackObjectThreadAffinity = true;
            _config.ReturnToCreatorThreadPool = true;
            _config.CreateThreadPoolsOnDemand = true;
            _config.AutoCleanupUnusedThreadPools = true;
            _config.ThreadPoolCleanupThreshold = 300f;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            return this;
        }

        /// <summary>
        /// Configures the builder with memory-efficient settings to minimize overhead
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadLocalPoolConfigBuilder AsMemoryEfficient()
        {
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 10.0f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.MaxThreadCount = 8;
            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings suitable for most use cases
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadLocalPoolConfigBuilder AsBalanced()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 16);
            _config.MaximumCapacity = _config.InitialCapacity * 4;
            _config.MaxThreadCount = 16;
            _config.CreateThreadPoolsOnDemand = true;
            _config.MaintainGlobalFallbackPool = true;
            _config.AutoCleanupUnusedThreadPools = true;
            _config.ThreadPoolCleanupThreshold = 120f;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.TrackObjectThreadAffinity = false;
            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing thread-local pool configuration.
        /// Allows for fluent method chaining.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadLocalPoolConfigBuilder FromExisting(ThreadLocalPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            // Copy common IPoolConfig properties
            _config.ConfigId = config.ConfigId;
            _config.InitialCapacity = config.InitialCapacity;
            _config.MinimumCapacity = config.MinimumCapacity;
            _config.MaximumCapacity = config.MaximumCapacity;
            _config.PrewarmOnInit = config.PrewarmOnInit;
            _config.CollectMetrics = config.CollectMetrics;
            _config.DetailedLogging = config.DetailedLogging;
            _config.LogWarnings = config.LogWarnings;
            _config.ResetOnRelease = config.ResetOnRelease;
            // ThreadingMode is always ThreadLocal
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // Copy thread-local specific properties
            _config.MaxThreadCount = config.MaxThreadCount;
            _config.CreateThreadPoolsOnDemand = config.CreateThreadPoolsOnDemand;
            _config.MaintainGlobalFallbackPool = config.MaintainGlobalFallbackPool;
            _config.AutoCleanupUnusedThreadPools = config.AutoCleanupUnusedThreadPools;
            _config.ThreadPoolCleanupThreshold = config.ThreadPoolCleanupThreshold;
            _config.TrackObjectThreadAffinity = config.TrackObjectThreadAffinity;
            _config.ReturnToCreatorThreadPool = config.ReturnToCreatorThreadPool;

            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing IPoolConfig.
        /// Thread-local specific settings will be set to defaults unless the source
        /// is a ThreadLocalPoolConfig.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadLocalPoolConfigBuilder FromExisting(IPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            // Copy common IPoolConfig properties
            _config.ConfigId = config.ConfigId;
            _config.InitialCapacity = config.InitialCapacity;
            _config.MinimumCapacity = config.MinimumCapacity;
            _config.MaximumCapacity = config.MaximumCapacity;
            _config.PrewarmOnInit = config.PrewarmOnInit;
            _config.CollectMetrics = config.CollectMetrics;
            _config.DetailedLogging = config.DetailedLogging;
            _config.LogWarnings = config.LogWarnings;
            _config.ResetOnRelease = config.ResetOnRelease;
            // ThreadingMode is always ThreadLocal
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // If source is a ThreadLocalPoolConfig, also copy thread-local specific properties
            if (config is ThreadLocalPoolConfig threadLocalConfig)
            {
                _config.MaxThreadCount = threadLocalConfig.MaxThreadCount;
                _config.CreateThreadPoolsOnDemand = threadLocalConfig.CreateThreadPoolsOnDemand;
                _config.MaintainGlobalFallbackPool = threadLocalConfig.MaintainGlobalFallbackPool;
                _config.AutoCleanupUnusedThreadPools = threadLocalConfig.AutoCleanupUnusedThreadPools;
                _config.ThreadPoolCleanupThreshold = threadLocalConfig.ThreadPoolCleanupThreshold;
                _config.TrackObjectThreadAffinity = threadLocalConfig.TrackObjectThreadAffinity;
                _config.ReturnToCreatorThreadPool = threadLocalConfig.ReturnToCreatorThreadPool;
            }

            // Force thread-local mode regardless of source setting
            _config.ThreadingMode = PoolThreadingMode.ThreadLocal;

            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ThreadLocalPoolConfig Build()
        {
            // Force thread-local mode
            _config.ThreadingMode = PoolThreadingMode.ThreadLocal;

            ValidateConfiguration();
            var clone = _config.Clone() as ThreadLocalPoolConfig;
            return clone ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Validates the configuration before building
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            if (_config.InitialCapacity < 0)
                throw new InvalidOperationException("Initial capacity cannot be negative");

            if (_config.MaximumCapacity > 0 && _config.InitialCapacity > _config.MaximumCapacity)
                throw new InvalidOperationException("Initial capacity cannot exceed maximum size");

            if (_config.MaxThreadCount < 1)
                throw new InvalidOperationException("Maximum thread count must be at least 1");

            if (_config.ThreadPoolCleanupThreshold < 0)
                throw new InvalidOperationException("Thread pool cleanup threshold cannot be negative");

            if (_config.ReturnToCreatorThreadPool && !_config.TrackObjectThreadAffinity)
                throw new InvalidOperationException("Return to creator thread pool requires thread affinity tracking");
        }
    }
}