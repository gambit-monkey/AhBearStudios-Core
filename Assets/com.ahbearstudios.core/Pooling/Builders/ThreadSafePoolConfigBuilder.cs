using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Builder for thread-safe pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for thread-safe pooling with a fluent API.
    /// </summary>
    public class ThreadSafePoolConfigBuilder : IPoolConfigBuilder<ThreadSafePoolConfig, ThreadSafePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ThreadSafePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public ThreadSafePoolConfigBuilder()
        {
            _config = new ThreadSafePoolConfig();
        }

        /// <summary>
        /// Creates a new builder initialized with an existing thread-safe configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadSafePoolConfigBuilder(ThreadSafePoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = config.Clone() as ThreadSafePoolConfig
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder initialized with a generic pool configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadSafePoolConfigBuilder(IPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = new ThreadSafePoolConfig(config);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ThreadSafePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ThreadSafePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding
        /// </summary>
        public ThreadSafePoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential expansion
        /// </summary>
        public ThreadSafePoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets the growth increment for linear expansion
        /// </summary>
        public ThreadSafePoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets the concurrency warning threshold
        /// </summary>
        public ThreadSafePoolConfigBuilder WithConcurrencyWarningThreshold(int threshold)
        {
            _config.ConcurrencyWarningThreshold = Mathf.Max(1, threshold);
            return this;
        }

        /// <summary>
        /// Sets whether to track operation performance
        /// </summary>
        public ThreadSafePoolConfigBuilder WithOperationPerformanceTracking(bool trackPerformance)
        {
            _config.TrackOperationPerformance = trackPerformance;
            return this;
        }

        /// <summary>
        /// Sets whether to use concurrent collections
        /// </summary>
        public ThreadSafePoolConfigBuilder WithConcurrentCollections(bool useConcurrentCollections)
        {
            _config.UseConcurrentCollections = useConcurrentCollections;
            return this;
        }

        /// <summary>
        /// Sets whether to track object lifetimes
        /// </summary>
        public ThreadSafePoolConfigBuilder WithObjectLifetimeTracking(bool trackLifetimes)
        {
            _config.TrackObjectLifetimes = trackLifetimes;
            return this;
        }

        /// <summary>
        /// Sets whether to prefer lock-free algorithms
        /// </summary>
        public ThreadSafePoolConfigBuilder WithLockFreeAlgorithms(bool preferLockFree)
        {
            _config.PreferLockFreeAlgorithms = preferLockFree;
            return this;
        }

        /// <summary>
        /// Sets the lock timeout in milliseconds
        /// </summary>
        public ThreadSafePoolConfigBuilder WithLockTimeout(int timeoutMs)
        {
            _config.LockTimeoutMs = Mathf.Max(0, timeoutMs);
            return this;
        }

        /// <summary>
        /// Sets the operation retry count
        /// </summary>
        public ThreadSafePoolConfigBuilder WithOperationRetryCount(int retryCount)
        {
            _config.OperationRetryCount = Mathf.Max(0, retryCount);
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        public ThreadSafePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for the pool
        /// </summary>
        public ThreadSafePoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to enable detailed logging for the pool
        /// </summary>
        public ThreadSafePoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings for the pool
        /// </summary>
        public ThreadSafePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects on release
        /// </summary>
        public ThreadSafePoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        public ThreadSafePoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        public ThreadSafePoolConfigBuilder WithShrinkThreshold(float shrinkThreshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(shrinkThreshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        public ThreadSafePoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for any native collections
        /// </summary>
        public ThreadSafePoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when exceeding max count
        /// </summary>
        public ThreadSafePoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing thread-safe pool configuration.
        /// Allows for fluent method chaining.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadSafePoolConfigBuilder FromExisting(ThreadSafePoolConfig config)
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
            // ThreadingMode is always ThreadSafe
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // Copy thread-safe specific properties
            _config.ConcurrencyWarningThreshold = config.ConcurrencyWarningThreshold;
            _config.TrackOperationPerformance = config.TrackOperationPerformance;
            _config.UseConcurrentCollections = config.UseConcurrentCollections;
            _config.TrackObjectLifetimes = config.TrackObjectLifetimes;
            _config.PreferLockFreeAlgorithms = config.PreferLockFreeAlgorithms;
            _config.LockTimeoutMs = config.LockTimeoutMs;
            _config.OperationRetryCount = config.OperationRetryCount;

            // Copy additional properties that may exist in an updated ThreadSafePoolConfig
            if (config.GetType().GetProperty("UseSpinLocks") != null)
            {
                var spinLocksProp = _config.GetType().GetProperty("UseSpinLocks");
                if (spinLocksProp != null)
                {
                    spinLocksProp.SetValue(_config, config.GetType().GetProperty("UseSpinLocks").GetValue(config));
                }
            }

            if (config.GetType().GetProperty("SpinCount") != null)
            {
                var spinCountProp = _config.GetType().GetProperty("SpinCount");
                if (spinCountProp != null)
                {
                    spinCountProp.SetValue(_config, config.GetType().GetProperty("SpinCount").GetValue(config));
                }
            }

            if (config.GetType().GetProperty("OptimizeForBurstAccess") != null)
            {
                var burstAccessProp = _config.GetType().GetProperty("OptimizeForBurstAccess");
                if (burstAccessProp != null)
                {
                    burstAccessProp.SetValue(_config,
                        config.GetType().GetProperty("OptimizeForBurstAccess").GetValue(config));
                }
            }

            if (config.GetType().GetProperty("UseSeparateLocks") != null)
            {
                var separateLocksProp = _config.GetType().GetProperty("UseSeparateLocks");
                if (separateLocksProp != null)
                {
                    separateLocksProp.SetValue(_config,
                        config.GetType().GetProperty("UseSeparateLocks").GetValue(config));
                }
            }

            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing IPoolConfig.
        /// Thread-safe specific settings will be set to defaults unless the source
        /// is a ThreadSafePoolConfig.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ThreadSafePoolConfigBuilder FromExisting(IPoolConfig config)
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
            // ThreadingMode is always ThreadSafe
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // If source is a ThreadSafePoolConfig, also copy thread-safe specific properties
            if (config is ThreadSafePoolConfig threadSafeConfig)
            {
                return FromExisting(threadSafeConfig);
            }

            // Force thread-safe mode regardless of source setting
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;

            return this;
        }

        /// <summary>
        /// Configures for high-performance settings
        /// </summary>
        public ThreadSafePoolConfigBuilder AsHighPerformance()
        {
            _config.UseConcurrentCollections = true;
            _config.PreferLockFreeAlgorithms = true;
            _config.TrackOperationPerformance = false;
            _config.TrackObjectLifetimes = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            return this;
        }

        /// <summary>
        /// Configures for debugging with extensive tracking
        /// </summary>
        public ThreadSafePoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.TrackOperationPerformance = true;
            _config.TrackObjectLifetimes = true;
            _config.LogWarnings = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with settings optimized for high concurrency scenarios
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadSafePoolConfigBuilder AsHighConcurrency()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 64);
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.UseConcurrentCollections = true;
            _config.PreferLockFreeAlgorithms = true;
            _config.OperationRetryCount = 5;
            _config.LockTimeoutMs = 2000;
            _config.ConcurrencyWarningThreshold = 200;
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;
            return this;
        }

        /// <summary>
        /// Configures the builder with optimized lock settings for reducing contention
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadSafePoolConfigBuilder AsWithOptimizedLocks()
        {
            _config.PreferLockFreeAlgorithms = true;
            _config.LockTimeoutMs = 1000;
            _config.OperationRetryCount = 3;
            _config.UseConcurrentCollections = true;
            _config.ConcurrencyWarningThreshold = 150;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.CollectMetrics = false;
            return this;
        }

        /// <summary>
        /// Configures the builder with monitoring settings for diagnostics
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadSafePoolConfigBuilder AsWithMonitoring()
        {
            _config.CollectMetrics = true;
            _config.DetailedLogging = true;
            _config.TrackOperationPerformance = true;
            _config.TrackObjectLifetimes = true;
            _config.LogWarnings = true;
            _config.ConcurrencyWarningThreshold = 100;
            _config.LockTimeoutMs = 3000;
            _config.OperationRetryCount = 2;
            return this;
        }

        /// <summary>
        /// Configures the builder with memory-efficient settings to minimize overhead
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadSafePoolConfigBuilder AsMemoryEfficient()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 16);
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 10.0f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.UseConcurrentCollections = false;
            _config.TrackObjectLifetimes = false;
            _config.TrackOperationPerformance = false;
            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings suitable for most use cases
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ThreadSafePoolConfigBuilder AsBalanced()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 32);
            _config.MaximumCapacity = _config.InitialCapacity * 4;
            _config.UseConcurrentCollections = true;
            _config.PreferLockFreeAlgorithms = true;
            _config.ConcurrencyWarningThreshold = 120;
            _config.LockTimeoutMs = 1500;
            _config.OperationRetryCount = 3;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.CollectMetrics = true;
            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ThreadSafePoolConfig Build()
        {
            // Force thread-safe mode
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;

            ValidateConfiguration();
            var clone = _config.Clone() as ThreadSafePoolConfig;
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

            if (_config.ConcurrencyWarningThreshold < 1)
                throw new InvalidOperationException("Concurrency warning threshold must be at least 1");

            if (_config.LockTimeoutMs < 0)
                throw new InvalidOperationException("Lock timeout cannot be negative");

            if (_config.OperationRetryCount < 0)
                throw new InvalidOperationException("Operation retry count cannot be negative");
        }
    }
}