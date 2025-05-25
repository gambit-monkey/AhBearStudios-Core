using System;
using AhBearStudios.Core.Pooling.Configurations;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for Managed object pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for managed object pools with a fluent API.
    /// </summary>
    public class
        ManagedObjectPoolConfigBuilder : IPoolConfigBuilder<ManagedObjectPoolConfig, ManagedObjectPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ManagedObjectPoolConfig _config;

        /// <summary>
        /// Creates a new Managed object pool configuration builder with default settings
        /// </summary>
        public ManagedObjectPoolConfigBuilder()
        {
            _config = new ManagedObjectPoolConfig();
        }

        /// <summary>
        /// Creates a new builder with an existing configuration
        /// </summary>
        /// <param name="sourceConfig">Source configuration to copy</param>
        /// <exception cref="ArgumentNullException">Thrown if sourceConfig is null</exception>
        public ManagedObjectPoolConfigBuilder(ManagedObjectPoolConfig sourceConfig)
        {
            _config = sourceConfig ?? throw new ArgumentNullException(nameof(sourceConfig));
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with specified initial and maximum capacities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new managed object pool configuration builder</returns>
        public static ManagedObjectPoolConfigBuilder Managed(int initialCapacity, int maxSize)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="sourceConfig">The source configuration to copy settings from</param>
        /// <returns>A new managed object pool configuration builder initialized with the source configuration</returns>
        public static ManagedObjectPoolConfigBuilder ManagedFrom(ManagedObjectPoolConfig sourceConfig)
        {
            return new ManagedObjectPoolConfigBuilder(sourceConfig);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder from an existing pool configuration
        /// </summary>
        /// <param name="baseConfig">The base configuration to copy settings from</param>
        /// <returns>A new managed object pool configuration builder with copied settings</returns>
        public static ManagedObjectPoolConfigBuilder ManagedFrom(IPoolConfig baseConfig)
        {
            var config = new ManagedObjectPoolConfig(baseConfig);
            return new ManagedObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to call object lifecycle methods
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithLifecycleMethods(bool callLifecycleMethods)
        {
            _config.CallLifecycleMethods = callLifecycleMethods;
            return this;
        }

        /// <summary>
        /// Sets whether to track stack traces for debugging
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithStackTraceTracking(bool trackStackTraces)
        {
            _config.TrackStackTraces = trackStackTraces;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects when released back to the pool
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        /// <param name="threadingMode">The threading mode to use</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        /// <param name="autoShrink">Whether to enable auto-shrinking</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithAutoShrink(bool autoShrink)
        {
            _config.EnableAutoShrink = autoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        /// <param name="threshold">The shrink threshold (0.0-1.0)</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds between shrink operations</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for native collections
        /// </summary>
        /// <param name="allocator">The allocator to use</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        /// <param name="useExponential">Whether to use exponential growth</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithExponentialGrowth(bool useExponential)
        {
            _config.UseExponentialGrowth = useExponential;
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth)
        /// </summary>
        /// <param name="factor">The growth factor (multiplicative)</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, factor);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth)
        /// </summary>
        /// <param name="increment">The number of items to add each time</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when attempting to get an object
        /// that would exceed the maximum pool size
        /// </summary>
        /// <param name="throwException">Whether to throw an exception</param>
        /// <returns>This builder for method chaining</returns>
        public ManagedObjectPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Sets whether to validate items on release
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithValidation(bool validateOnRelease)
        {
            _config.ValidateOnRelease = validateOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the maximum time an item can remain active
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithMaxActiveTime(float maxActiveTime)
        {
            _config.MaxActiveTime = Mathf.Max(0f, maxActiveTime);
            return this;
        }

        /// <summary>
        /// Sets whether to maintain FIFO order
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithFifoOrder(bool maintainFifoOrder)
        {
            _config.MaintainFifoOrder = maintainFifoOrder;
            return this;
        }

        /// <summary>
        /// Sets whether to force GC after shrinking
        /// </summary>
        public ManagedObjectPoolConfigBuilder WithGcAfterShrink(bool forceGcAfterShrink)
        {
            _config.ForceGcAfterShrink = forceGcAfterShrink;
            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public ManagedObjectPoolConfigBuilder FromExisting(ManagedObjectPoolConfig source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source configuration cannot be null");

            // Copy standard IPoolConfig properties
            _config.ConfigId = source.ConfigId;
            _config.InitialCapacity = source.InitialCapacity;
            _config.MinimumCapacity = source.MinimumCapacity;
            _config.MaximumCapacity = source.MaximumCapacity;
            _config.PrewarmOnInit = source.PrewarmOnInit;
            _config.CollectMetrics = source.CollectMetrics;
            _config.DetailedLogging = source.DetailedLogging;
            _config.LogWarnings = source.LogWarnings;
            _config.ResetOnRelease = source.ResetOnRelease;
            _config.ThreadingMode = source.ThreadingMode;
            _config.EnableAutoShrink = source.EnableAutoShrink;
            _config.ShrinkThreshold = source.ShrinkThreshold;
            _config.ShrinkInterval = source.ShrinkInterval;
            _config.NativeAllocator = source.NativeAllocator;
            _config.UseExponentialGrowth = source.UseExponentialGrowth;
            _config.GrowthFactor = source.GrowthFactor;
            _config.GrowthIncrement = source.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = source.ThrowIfExceedingMaxCount;

            // Copy Managed-specific properties
            _config.CallLifecycleMethods = source.CallLifecycleMethods;
            _config.TrackStackTraces = source.TrackStackTraces;
            _config.ValidateOnRelease = source.ValidateOnRelease;
            _config.MaxActiveTime = source.MaxActiveTime;
            _config.MaintainFifoOrder = source.MaintainFifoOrder;
            _config.ForceGcAfterShrink = source.ForceGcAfterShrink;
            _config.UseCustomAllocators = source.UseCustomAllocators;
            _config.UseNativeCollections = source.UseNativeCollections;
            _config.UseSafetyHandles = source.UseSafetyHandles;
            _config.TrackAllocationSizes = source.TrackAllocationSizes;
            _config.ClearReferencesOnRelease = source.ClearReferencesOnRelease;
            _config.DisposeSentinel = source.DisposeSentinel;

            return this;
        }

        /// <summary>
        /// Configures for balanced usage with moderate monitoring and good performance
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsBalanced()
        {
            _config.TrackStackTraces = false;
            _config.ValidateOnRelease = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.MaintainFifoOrder = true;
            _config.EnableAutoShrink = true;
            _config.MaxActiveTime = 300f; // 5 minutes warning timeout
            return this;
        }

        /// <summary>
        /// Configures for memory-efficient operation with automatic shrinking
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 10.0f;
            _config.ForceGcAfterShrink = true;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.MaintainFifoOrder = true;
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            return this;
        }

        /// <summary>
        /// Configures for monitored operation with detailed metrics and logging
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsMonitored()
        {
            _config.TrackStackTraces = true;
            _config.ValidateOnRelease = true;
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.MaxActiveTime = 180f; // 3 minutes warning timeout
            return this;
        }

        /// <summary>
        /// Configures for thread-safe operation in multi-threaded environments
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsThreadSafe()
        {
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.MaintainFifoOrder = false; // For better performance in thread-safe mode
            _config.ValidateOnRelease = true; // More important in thread-safe scenarios
            _config.TrackStackTraces = false; // Performance trade-off
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            return this;
        }

        /// <summary>
        /// Configures for long-lived objects with appropriate object tracking
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsLongLived()
        {
            _config.MaxActiveTime = 0f; // No maximum time limit
            _config.EnableAutoShrink = false;
            _config.MaintainFifoOrder = true;
            _config.ValidateOnRelease = true;
            _config.ResetOnRelease = true;
            return this;
        }

        /// <summary>
        /// Configures for short-lived objects with high turnover rate
        /// </summary>
        public ManagedObjectPoolConfigBuilder AsShortLived()
        {
            _config.MaxActiveTime = 60f; // 1 minute warning
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 30.0f;
            _config.MaintainFifoOrder = false; // Better performance for high-churn objects
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ManagedObjectPoolConfig Build()
        {
            ValidateConfiguration();
            var clone = _config.Clone() as ManagedObjectPoolConfig;
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

            if (_config.MaxActiveTime < 0)
                throw new InvalidOperationException("Maximum active time cannot be negative");

            if (_config.ThreadingMode == PoolThreadingMode.JobCompatible)
            {
                Debug.LogWarning(
                    "JobCompatible threading mode is not supported for managed objects. Using ThreadSafe instead.");
                _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            }
        }
    }
}