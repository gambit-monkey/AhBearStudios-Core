using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for semaphore pool configurations implementing IPoolConfigBuilder.
    /// Provides thread-safe pool configuration with semaphore-specific settings.
    /// </summary>
    public class SemaphorePoolConfigBuilder : IPoolConfigBuilder<SemaphorePoolConfig, SemaphorePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly SemaphorePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public SemaphorePoolConfigBuilder()
        {
            _config = new SemaphorePoolConfig();
        }

        /// <summary>
        /// Creates a new builder initialized with an existing semaphore configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public SemaphorePoolConfigBuilder(SemaphorePoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = config.Clone() as SemaphorePoolConfig
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder initialized with a generic pool configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public SemaphorePoolConfigBuilder(IPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = new SemaphorePoolConfig(config);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public SemaphorePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        /// <param name="enableAutoShrink">Whether to enable automatic pool shrinking</param>
        /// <returns>This builder instance for chaining</returns>
        public SemaphorePoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public SemaphorePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }



        /// <summary>
        /// Sets whether to use exponential growth when expanding
        /// </summary>
        public SemaphorePoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential expansion
        /// </summary>
        public SemaphorePoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets the initial count of available semaphore resources
        /// </summary>
        public SemaphorePoolConfigBuilder WithInitialCount(int initialCount)
        {
            _config.InitialCount = Mathf.Max(0, initialCount);
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent wait operations
        /// </summary>
        public SemaphorePoolConfigBuilder WithMaxConcurrentWaits(int maxConcurrentWaits)
        {
            _config.MaxConcurrentWaits = Mathf.Max(1, maxConcurrentWaits);
            return this;
        }

        /// <summary>
        /// Sets whether to track thread ownership of semaphore resources
        /// </summary>
        public SemaphorePoolConfigBuilder WithOwnershipTracking(bool trackOwnership)
        {
            _config.TrackOwnership = trackOwnership;
            return this;
        }

        /// <summary>
        /// Sets the default timeout for wait operations
        /// </summary>
        public SemaphorePoolConfigBuilder WithDefaultTimeout(int timeoutMs)
        {
            _config.DefaultTimeoutMs = Mathf.Max(0, timeoutMs);
            return this;
        }

        /// <summary>
        /// Sets whether to allow recursive acquisition
        /// </summary>
        public SemaphorePoolConfigBuilder WithRecursiveAcquisition(bool allowRecursive)
        {
            _config.AllowRecursiveAcquisition = allowRecursive;
            return this;
        }

        /// <summary>
        /// Sets whether to auto-release on thread exit
        /// </summary>
        public SemaphorePoolConfigBuilder WithAutoReleaseOnThreadExit(bool autoRelease)
        {
            _config.AutoReleaseOnThreadExit = autoRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to track operation timing metrics
        /// </summary>
        public SemaphorePoolConfigBuilder WithOperationTimingTracking(bool trackTimings)
        {
            _config.TrackOperationTimings = trackTimings;
            return this;
        }

        /// <summary>
        /// Sets whether to track contention metrics
        /// </summary>
        public SemaphorePoolConfigBuilder WithContentionMetrics(bool trackContention)
        {
            _config.TrackContentionMetrics = trackContention;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        public SemaphorePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for the pool
        /// </summary>
        public SemaphorePoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to enable detailed logging for the pool
        /// </summary>
        public SemaphorePoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings for the pool
        /// </summary>
        public SemaphorePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset semaphores on release
        /// </summary>
        public SemaphorePoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// Note: This will be forced to ThreadSafe for semaphore pools
        /// </summary>
        public SemaphorePoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            // Semaphore pools are always thread-safe, but we'll store the setting anyway
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for any native collections
        /// </summary>
        public SemaphorePoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets the fixed growth increment for linear pool growth
        /// </summary>
        public SemaphorePoolConfigBuilder WithGrowthIncrement(int growthIncrement)
        {
            _config.GrowthIncrement = Mathf.Max(1, growthIncrement);
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when exceeding max count
        /// </summary>
        public SemaphorePoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        /// <summary>
        /// Sets whether to validate semaphore counts on operations
        /// </summary>
        public SemaphorePoolConfigBuilder WithCountValidation(bool validateCounts)
        {
            _config.ValidateCounts = validateCounts;
            return this;
        }

        /// <summary>
        /// Sets whether to support priority-based waiting
        /// </summary>
        public SemaphorePoolConfigBuilder WithPriorityWaiting(bool enablePriorityWaiting)
        {
            _config.EnablePriorityWaiting = enablePriorityWaiting;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        /// <param name="shrinkThreshold">Threshold value between 0.0 and 1.0</param>
        /// <returns>This builder instance for chaining</returns>
        public SemaphorePoolConfigBuilder WithShrinkThreshold(float shrinkThreshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(shrinkThreshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds</param>
        /// <returns>This builder instance for chaining</returns>
        public SemaphorePoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0.1f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing configuration.
        /// Allows for fluent method chaining.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public SemaphorePoolConfigBuilder FromExisting(SemaphorePoolConfig config)
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
            _config.ThreadingMode = config.ThreadingMode;
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // Copy Semaphore-specific properties
            _config.InitialCount = config.InitialCount;
            _config.MaxConcurrentWaits = config.MaxConcurrentWaits;
            _config.TrackOwnership = config.TrackOwnership;
            _config.DefaultTimeoutMs = config.DefaultTimeoutMs;
            _config.AllowRecursiveAcquisition = config.AllowRecursiveAcquisition;
            _config.AutoReleaseOnThreadExit = config.AutoReleaseOnThreadExit;
            _config.ValidateCounts = config.ValidateCounts;
            _config.TrackOperationTimings = config.TrackOperationTimings;
            _config.TrackContentionMetrics = config.TrackContentionMetrics;
            _config.EnablePriorityWaiting = config.EnablePriorityWaiting;

            return this;
        }

        /// <summary>
        /// Initializes this builder with settings from an existing IPoolConfig.
        /// For semaphore specific settings, defaults will be used unless the source
        /// is a SemaphorePoolConfig.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public SemaphorePoolConfigBuilder FromExisting(IPoolConfig config)
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
            _config.ThreadingMode = config.ThreadingMode;
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // If source is a SemaphorePoolConfig, also copy those specific properties
            if (config is SemaphorePoolConfig semaphoreConfig)
            {
                _config.CopySemaphoreSpecificProperties(semaphoreConfig);
            }

            // Force thread-safe mode for semaphore pools regardless of source setting
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;

            return this;
        }

        /// <summary>
        /// Configures the builder with high-performance settings optimized for throughput
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsHighPerformance()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 32);
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.TrackOwnership = false;
            _config.ValidateCounts = false;
            _config.TrackOperationTimings = false;
            _config.DetailedLogging = false;
            _config.MaxConcurrentWaits = 512;
            return this;
        }

        /// <summary>
        /// Configures the builder with debug-friendly settings for development
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.LogWarnings = true;
            _config.CollectMetrics = true;
            _config.TrackOwnership = true;
            _config.ValidateCounts = true;
            _config.TrackOperationTimings = true;
            _config.TrackContentionMetrics = true;
            _config.DefaultTimeoutMs = 10000; // 10 seconds timeout to catch deadlocks
            return this;
        }

        /// <summary>
        /// Configures the builder with full tracking capabilities
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsWithTracking()
        {
            _config.TrackOwnership = true;
            _config.TrackOperationTimings = true;
            _config.TrackContentionMetrics = true;
            _config.CollectMetrics = true;
            _config.DetailedLogging = true;
            return this;
        }

        /// <summary>
        /// Configures the builder for high-contention scenarios
        /// </summary>
        /// <param name="initialCount">Initial count of semaphore resources</param>
        /// <param name="maxConcurrentWaits">Maximum number of concurrent wait operations</param>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsForContention(int initialCount = 4, int maxConcurrentWaits = 512)
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 64);
            _config.InitialCount = initialCount;
            _config.MaxConcurrentWaits = maxConcurrentWaits;
            _config.EnablePriorityWaiting = true;
            _config.TrackContentionMetrics = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            return this;
        }

        /// <summary>
        /// Configures the builder with timeout settings
        /// </summary>
        /// <param name="timeoutMs">Default timeout in milliseconds</param>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsWithTimeout(int timeoutMs = 1000)
        {
            _config.DefaultTimeoutMs = timeoutMs;
            _config.TrackOperationTimings = true;
            _config.TrackContentionMetrics = true;
            return this;
        }

        /// <summary>
        /// Configures the builder to allow recursive acquisition
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsRecursive()
        {
            _config.AllowRecursiveAcquisition = true;
            _config.TrackOwnership = true;
            _config.AutoReleaseOnThreadExit = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings suitable for most use cases
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public SemaphorePoolConfigBuilder AsBalanced()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 16);
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 60.0f;
            _config.TrackOwnership = true;
            _config.TrackContentionMetrics = true;
            _config.ValidateCounts = true;
            _config.MaxConcurrentWaits = 100;
            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public SemaphorePoolConfig Build()
        {
            // Force thread-safe mode for semaphore pools
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;

            ValidateConfiguration();
            var clone = _config.Clone() as SemaphorePoolConfig;
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

            if (_config.InitialCount < 0)
                throw new InvalidOperationException("Initial count cannot be negative");

            if (_config.MaxConcurrentWaits < 1)
                throw new InvalidOperationException("Maximum concurrent waits must be at least 1");

            if (_config.DefaultTimeoutMs < 0)
                throw new InvalidOperationException("Default timeout cannot be negative");
        }
    }
}