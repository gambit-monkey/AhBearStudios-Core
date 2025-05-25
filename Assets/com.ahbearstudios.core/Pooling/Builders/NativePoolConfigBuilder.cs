using System;
using AhBearStudios.Core.Pooling.Configurations;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for native pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for Unity native containers with a fluent API.
    /// </summary>
    public class NativePoolConfigBuilder : IPoolConfigBuilder<NativePoolConfig, NativePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly NativePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public NativePoolConfigBuilder()
        {
            _config = new NativePoolConfig();
        }
        
        /// <summary>
        /// Creates a new builder initialized with an existing configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public NativePoolConfigBuilder(NativePoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }
    
            _config = config.Clone() as NativePoolConfig 
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder with specified initial capacity and allocator
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native allocator to use</param>
        public NativePoolConfigBuilder(int initialCapacity, Allocator allocator)
        {
            ValidateConstructorParameters(initialCapacity, allocator);
            _config = new NativePoolConfig(initialCapacity, allocator);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public NativePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public NativePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to use safety checks in native containers
        /// </summary>
        public NativePoolConfigBuilder WithSafetyChecks(bool useSafetyChecks)
        {
            _config.UseSafetyChecks = useSafetyChecks;
            return this;
        }

        /// <summary>
        /// Sets the native configuration flags
        /// </summary>
        /// <param name="flags">The configuration flags to set</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithNativeConfigFlags(int flags)
        {
            _config.NativeConfigFlags = flags;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects when released back to the pool
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        /// <param name="autoShrink">Whether to enable auto-shrinking</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithAutoShrink(bool autoShrink)
        {
            _config.EnableAutoShrink = autoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        /// <param name="threshold">The shrink threshold (0.0-1.0)</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds between shrink operations</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        /// <param name="useExponential">Whether to use exponential growth</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithExponentialGrowth(bool useExponential)
        {
            _config.UseExponentialGrowth = useExponential;
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth)
        /// </summary>
        /// <param name="factor">The growth factor (multiplicative)</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, factor);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth)
        /// </summary>
        /// <param name="increment">The number of items to add each time</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        /// <param name="threadingMode">The threading mode to use</param>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets whether to auto-dispose native containers
        /// </summary>
        public NativePoolConfigBuilder WithAutoDispose(bool autoDispose)
        {
            _config.AutoDisposeOnRelease = autoDispose;
            return this;
        }

        /// <summary>
        /// Sets whether to use Burst-compatible collections
        /// </summary>
        public NativePoolConfigBuilder WithBurstCompatibleCollections(bool useBurstCompatible)
        {
            _config.UseBurstCompatibleCollections = useBurstCompatible;
            return this;
        }

        /// <summary>
        /// Sets whether to verify allocator consistency
        /// </summary>
        public NativePoolConfigBuilder WithAllocatorVerification(bool verifyAllocator)
        {
            _config.VerifyAllocator = verifyAllocator;
            return this;
        }
        
        /// <summary>
        /// Sets the native allocator to use for the pool.
        /// </summary>
        /// <param name="allocator">The allocator to use for native memory allocation.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public NativePoolConfigBuilder WithAllocator(Allocator allocator)
        {
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
    
            _config.NativeAllocator = allocator;
            return this;
        }


        /// <summary>
        /// Sets the growth strategy for pool expansion
        /// </summary>
        public NativePoolConfigBuilder WithGrowthStrategy(bool useExponentialGrowth, float growthFactor)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            _config.GrowthFactor = Mathf.Max(1.1f, growthFactor);
            return this;
        }
        
        /// <summary>
        /// Initializes the builder with values from an existing NativePoolConfig.
        /// </summary>
        /// <param name="config">The source configuration to copy settings from.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public NativePoolConfigBuilder FromExisting(NativePoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Source configuration cannot be null");

            // Copy IPoolConfig properties
            WithInitialCapacity(config.InitialCapacity);
            WithMaxSize(config.MaximumCapacity);
            WithPrewarming(config.PrewarmOnInit);
            WithMetricsCollection(config.CollectMetrics);
            WithDetailedLogging(config.DetailedLogging);
            WithWarningLogging(config.LogWarnings);
            WithResetOnRelease(config.ResetOnRelease);
            WithThreadingMode(config.ThreadingMode);
            WithAutoShrink(config.EnableAutoShrink);
            WithShrinkThreshold(config.ShrinkThreshold);
            WithShrinkInterval(config.ShrinkInterval);
            WithAllocator(config.NativeAllocator);
            WithExponentialGrowth(config.UseExponentialGrowth);
            WithGrowthFactor(config.GrowthFactor);
            WithGrowthIncrement(config.GrowthIncrement);
    
            // Copy NativePoolConfig-specific properties
            WithSafetyChecks(config.UseSafetyChecks);
            WithAutoDispose(config.AutoDisposeOnRelease);
            WithAllocatorVerification(config.VerifyAllocator);
            WithNativeConfigFlags(config.NativeConfigFlags);
            WithBurstCompatibleCollections(config.UseBurstCompatibleCollections);
    
            return this;
        }

        /// <summary>
        /// Configures the builder with high-performance settings.
        /// Optimizes for maximum speed by disabling safety checks and diagnostics.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsHighPerformance()
        {
            _config.UseSafetyChecks = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.LogWarnings = false;
            _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
            _config.UseBurstCompatibleCollections = true;
            _config.VerifyAllocator = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.EnableAutoShrink = false;
            _config.PrewarmOnInit = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for job-compatible operations.
        /// Optimizes for use with Unity's Job System in multithreaded scenarios.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsJobCompatible()
        {
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.UseSafetyChecks = true;
            _config.AutoDisposeOnRelease = true;
            _config.VerifyAllocator = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.DetailedLogging = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for Burst-compatible operations.
        /// Optimizes for use with Unity's Burst compiler for maximum performance.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsBurstCompatible()
        {
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.UseSafetyChecks = true;
            _config.AutoDisposeOnRelease = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;

            return this;
        }

        /// <summary>
        /// Configures the builder for debugging scenarios.
        /// Enables all safety checks and extensive logging.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsDebug()
        {
            _config.UseSafetyChecks = true;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.ThrowIfExceedingMaxCount = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient operations.
        /// Optimizes for minimal memory footprint with automatic shrinking.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 10.0f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.UseSafetyChecks = true;
            _config.AutoDisposeOnRelease = true;
            _config.MaximumCapacity = Math.Max(_config.InitialCapacity * 2, 32);

            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings.
        /// Provides good performance while maintaining safety and monitoring.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsBalanced()
        {
            _config.UseSafetyChecks = true;
            _config.AutoDisposeOnRelease = true;
            _config.VerifyAllocator = true;
            _config.UseBurstCompatibleCollections = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.CollectMetrics = true;
            _config.DetailedLogging = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;

            return this;
        }

        /// <summary>
        /// Configures the builder for thread-safe operations outside the Job System.
        /// Optimizes for manual multithreading scenarios.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public NativePoolConfigBuilder AsThreadSafe()
        {
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.UseSafetyChecks = true;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.UseBurstCompatibleCollections = false; // Not needed for standard thread safety
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;

            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public NativePoolConfig Build()
        {
            ValidateConfiguration();
            var clone = _config.Clone() as NativePoolConfig;
            return clone ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        private void ValidateConstructorParameters(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
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

            if (_config.NativeAllocator <= Allocator.None)
                throw new InvalidOperationException("Invalid allocator specified");

            if (_config.UseBurstCompatibleCollections && !_config.UseSafetyChecks)
            {
                Debug.LogWarning("Enabling safety checks as they are required for Burst compatibility");
                _config.UseSafetyChecks = true;
            }

            if (_config.ThreadingMode == PoolThreadingMode.JobCompatible && !_config.UseBurstCompatibleCollections)
            {
                Debug.LogWarning("Enabling Burst-compatible collections for job compatibility");
                _config.UseBurstCompatibleCollections = true;
            }
        }
    }
}