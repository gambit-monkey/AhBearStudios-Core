using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Builder for complex object pool configurations implementing IPoolConfigBuilder.
    /// Provides a fluent API for configuring complex object pools with advanced features.
    /// </summary>
    public class
        ComplexObjectPoolConfigBuilder : IPoolConfigBuilder<ComplexObjectPoolConfig, ComplexObjectPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ComplexObjectPoolConfig _config;

        /// <summary>
        /// Creates a new complex object pool configuration builder with default settings
        /// </summary>
        public ComplexObjectPoolConfigBuilder()
        {
            _config = new ComplexObjectPoolConfig();
        }

        /// <summary>
        /// Creates a new complex object pool configuration builder with an existing configuration
        /// </summary>
        /// <param name="sourceConfig">Source configuration to copy</param>
        public ComplexObjectPoolConfigBuilder(ComplexObjectPoolConfig sourceConfig)
        {
            _config = sourceConfig ?? throw new ArgumentNullException(nameof(sourceConfig));
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ComplexObjectPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ComplexObjectPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for threading mode
        /// </summary>
        public ComplexObjectPoolConfigBuilder WithThreadingMode(PoolThreadingMode mode)
        {
            _config.ThreadingMode = mode;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for auto-shrink
        /// </summary>
        public ComplexObjectPoolConfigBuilder WithAutoShrink(bool enable)
        {
            _config.EnableAutoShrink = enable;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for metrics collection
        /// </summary>
        public ComplexObjectPoolConfigBuilder WithMetrics(bool enable)
        {
            _config.CollectMetrics = enable;
            return this;
        }

        /// <summary>
        /// Configures whether this pool should collect metrics.
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Configures whether this pool should use detailed logging.
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Configures whether to prewarm the pool on initialization.
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Configures whether to log warnings.
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Configures whether objects should be reset when released back to the pool.
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Configures the shrink threshold for auto-shrinking.
        /// </summary>
        /// <param name="threshold">The threshold ratio (0.0-1.0) that triggers shrinking</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Configures the interval between automatic shrinking operations.
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds between shrink operations</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Configures whether to use exponential growth when expanding the pool.
        /// </summary>
        /// <param name="useExponential">Whether to use exponential growth</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithExponentialGrowth(bool useExponential)
        {
            _config.UseExponentialGrowth = useExponential;
            return this;
        }

        /// <summary>
        /// Configures the growth factor when using exponential growth.
        /// </summary>
        /// <param name="factor">The growth factor (multiplicative)</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Mathf.Max(1.0f, factor);
            return this;
        }

        /// <summary>
        /// Configures the growth increment when using linear growth.
        /// </summary>
        /// <param name="increment">The number of items to add each time</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Configures whether to throw an exception when trying to exceed the maximum pool size.
        /// </summary>
        /// <param name="throwException">Whether to throw an exception</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Configures the native allocator to use.
        /// </summary>
        /// <param name="allocator">The allocator to use</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Configures whether to validate objects when retrieved from the pool.
        /// </summary>
        /// <param name="validate">Whether to validate objects on acquire</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithValidation(bool validate)
        {
            _config.ValidateOnAcquire = validate;
            return this;
        }

        /// <summary>
        /// Configures whether to create new objects if validation fails.
        /// </summary>
        /// <param name="recreate">Whether to recreate objects that fail validation</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithRecreateOnValidationFailure(bool recreate)
        {
            _config.RecreateOnValidationFailure = recreate;
            return this;
        }

        /// <summary>
        /// Configures whether to automatically destroy objects that fail validation.
        /// </summary>
        /// <param name="destroy">Whether to destroy invalid objects</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithDestroyInvalidObjects(bool destroy)
        {
            _config.DestroyInvalidObjects = destroy;
            return this;
        }

        /// <summary>
        /// Configures whether to clear properties when an object is released to the pool.
        /// </summary>
        /// <param name="clear">Whether to clear properties on release</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithClearPropertiesOnRelease(bool clear)
        {
            _config.ClearPropertiesOnRelease = clear;
            return this;
        }

        /// <summary>
        /// Configures the initial capacity for property dictionaries.
        /// </summary>
        /// <param name="capacity">Initial capacity for property storage</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithInitialPropertyCapacity(int capacity)
        {
            _config.InitialPropertyCapacity = Mathf.Max(1, capacity);
            return this;
        }

        /// <summary>
        /// Configures the initial capacity for dependency lists.
        /// </summary>
        /// <param name="capacity">Initial capacity for dependency storage</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithInitialDependencyCapacity(int capacity)
        {
            _config.InitialDependencyCapacity = Mathf.Max(1, capacity);
            return this;
        }

        /// <summary>
        /// Configures whether to track timing metrics for pool operations.
        /// </summary>
        /// <param name="track">Whether to track operation timings</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithOperationTimingTracking(bool track)
        {
            _config.TrackOperationTimings = track;
            return this;
        }

        /// <summary>
        /// Configures whether to dispose objects asynchronously.
        /// </summary>
        /// <param name="useAsync">Whether to use async disposal</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithAsyncDisposal(bool useAsync)
        {
            _config.UseAsyncDisposal = useAsync;
            return this;
        }

        /// <summary>
        /// Enables property storage for complex objects in the pool.
        /// </summary>
        /// <param name="enabled">Whether to enable property storage</param>
        /// <param name="initialCapacity">Initial capacity for property storage per object</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithPropertyStorage(bool enabled, int initialCapacity = 8)
        {
            _config.EnablePropertyStorage = enabled;
            _config.InitialPropertyCapacity = Mathf.Max(1, initialCapacity);
            return this;
        }

        /// <summary>
        /// Enables dependency tracking for complex objects in the pool.
        /// </summary>
        /// <param name="enabled">Whether to enable dependency tracking</param>
        /// <param name="initialCapacity">Initial capacity for dependency storage per object</param>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder WithDependencyTracking(bool enabled, int initialCapacity = 4)
        {
            _config.EnableDependencyTracking = enabled;
            _config.InitialDependencyCapacity = Mathf.Max(1, initialCapacity);
            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public ComplexObjectPoolConfigBuilder FromExisting(ComplexObjectPoolConfig source)
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

            // Copy complex object pool specific properties
            _config.EnableDependencyTracking = source.EnableDependencyTracking;
            _config.EnablePropertyStorage = source.EnablePropertyStorage;
            _config.ValidateOnAcquire = source.ValidateOnAcquire;
            _config.RecreateOnValidationFailure = source.RecreateOnValidationFailure;
            _config.DestroyInvalidObjects = source.DestroyInvalidObjects;
            _config.ClearPropertiesOnRelease = source.ClearPropertiesOnRelease;
            _config.InitialPropertyCapacity = source.InitialPropertyCapacity;
            _config.InitialDependencyCapacity = source.InitialDependencyCapacity;
            _config.TrackOperationTimings = source.TrackOperationTimings;
            _config.UseAsyncDisposal = source.UseAsyncDisposal;
            _config.UseSIMDAlignment = source.UseSIMDAlignment;
            _config.UseSafetyHandles = source.UseSafetyHandles;
            _config.TrackAllocationSizes = source.TrackAllocationSizes;
            _config.DisposeSentinel = source.DisposeSentinel;
            _config.UseParallelJobFriendlyLayout = source.UseParallelJobFriendlyLayout;
            _config.UseFixedStringBuffers = source.UseFixedStringBuffers;
            _config.FixedStringCapacity = source.FixedStringCapacity;
            _config.EnableMemoryFences = source.EnableMemoryFences;
            _config.UseBurstFunctions = source.UseBurstFunctions;

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance settings.
        /// Optimizes for speed with minimal overhead by disabling features that may impact performance.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsHighPerformance()
        {
            _config.EnableDependencyTracking = false;
            _config.EnablePropertyStorage = false;
            _config.ValidateOnAcquire = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.EnableAutoShrink = false;
            _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
            _config.TrackOperationTimings = false;
            _config.UseAsyncDisposal = false;
            _config.ClearPropertiesOnRelease = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug settings.
        /// Enables comprehensive logging, validation, and tracking features for development.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsDebug()
        {
            _config.EnableDependencyTracking = true;
            _config.EnablePropertyStorage = true;
            _config.ValidateOnAcquire = true;
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.ResetOnRelease = true;
            _config.RecreateOnValidationFailure = true;
            _config.DestroyInvalidObjects = true;
            _config.TrackOperationTimings = true;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 5;

            return this;
        }

        /// <summary>
        /// Configures the builder for balanced settings.
        /// Provides a compromise between performance and safety features.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsBalanced()
        {
            _config.EnableDependencyTracking = true;
            _config.EnablePropertyStorage = true;
            _config.ValidateOnAcquire = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.ResetOnRelease = true;
            _config.DestroyInvalidObjects = true;
            _config.TrackOperationTimings = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 120f;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient settings.
        /// Optimizes for minimal memory usage with more aggressive shrinking.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 30f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.InitialPropertyCapacity = 2;
            _config.InitialDependencyCapacity = 1;
            _config.ClearPropertiesOnRelease = true;
            _config.MaximumCapacity = _config.InitialCapacity * 2;

            return this;
        }

        /// <summary>
        /// Configures the builder for dependency tracking optimization.
        /// Focuses on efficient tracking of object relationships and dependencies.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsDependencyTracking()
        {
            _config.EnableDependencyTracking = true;
            _config.EnablePropertyStorage = true;
            _config.InitialDependencyCapacity = 4;
            _config.InitialPropertyCapacity = 8;
            _config.ValidateOnAcquire = true;
            _config.RecreateOnValidationFailure = true;
            _config.DestroyInvalidObjects = true;
            _config.CollectMetrics = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for async operations.
        /// Optimizes for asynchronous usage with appropriate disposal settings.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComplexObjectPoolConfigBuilder AsAsyncOptimized()
        {
            _config.UseAsyncDisposal = true;
            _config.TrackOperationTimings = true;
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.EnablePropertyStorage = true;
            _config.ValidateOnAcquire = false;
            _config.ClearPropertiesOnRelease = true;
            _config.EnableAutoShrink = true;
            _config.ShrinkInterval = 60f;

            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ComplexObjectPoolConfig Build()
        {
            ValidateConfiguration();
            return _config;
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

            if (_config.UseExponentialGrowth && _config.GrowthFactor <= 1.0f)
                throw new InvalidOperationException(
                    "Growth factor must be greater than 1.0 when using exponential growth");

            if (_config.EnablePropertyStorage && _config.InitialPropertyCapacity <= 0)
                throw new InvalidOperationException(
                    "Initial property capacity must be positive when property storage is enabled");

            if (_config.EnableDependencyTracking && _config.InitialDependencyCapacity <= 0)
                throw new InvalidOperationException(
                    "Initial dependency capacity must be positive when dependency tracking is enabled");

            // Thread safety validation
            if (_config.ThreadingMode != PoolThreadingMode.SingleThreaded &&
                (_config.EnablePropertyStorage || _config.EnableDependencyTracking))
            {
                _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
                Debug.LogWarning(
                    "Forcing single-threaded mode due to property storage or dependency tracking being enabled");
            }
        }
    }
}