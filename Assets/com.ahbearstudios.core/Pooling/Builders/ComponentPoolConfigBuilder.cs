using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Builder for Component pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for Unity Component pools with a fluent API.
    /// </summary>
    public class ComponentPoolConfigBuilder : IPoolConfigBuilder<ComponentPoolConfig, ComponentPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ComponentPoolConfig _config;

        /// <summary>
        /// Creates a new component pool configuration builder with default settings
        /// </summary>
        public ComponentPoolConfigBuilder()
        {
            _config = new ComponentPoolConfig();
        }

        /// <summary>
        /// Creates a new component pool configuration builder with an existing configuration
        /// </summary>
        /// <param name="sourceConfig">Source configuration to copy</param>
        /// <exception cref="ArgumentNullException">Thrown if sourceConfig is null</exception>
        public ComponentPoolConfigBuilder(ComponentPoolConfig sourceConfig)
        {
            _config = sourceConfig ?? throw new ArgumentNullException(nameof(sourceConfig));
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ComponentPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ComponentPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for threading mode
        /// </summary>
        public ComponentPoolConfigBuilder WithThreadingMode(PoolThreadingMode mode)
        {
            _config.ThreadingMode = mode;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        /// <param name="autoShrink">Whether to enable auto-shrinking</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithAutoShrink(bool autoShrink)
        {
            _config.EnableAutoShrink = autoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        /// <param name="threshold">The shrink threshold (0.0-1.0)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds between shrink operations</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for native pools
        /// </summary>
        /// <param name="allocator">The allocator to use</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        /// <param name="useExponential">Whether to use exponential growth</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithExponentialGrowth(bool useExponential)
        {
            _config.UseExponentialGrowth = useExponential;
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth)
        /// </summary>
        /// <param name="factor">The growth factor (multiplicative)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, factor);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth)
        /// </summary>
        /// <param name="increment">The number of items to add each time</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when attempting to get an object
        /// that would exceed the maximum pool size
        /// </summary>
        /// <param name="throwException">Whether to throw an exception</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Sets whether to use a parent transform for pooled components
        /// </summary>
        /// <param name="parentTransform">The parent transform to use</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithParentTransform(Transform parentTransform)
        {
            if (parentTransform != null)
            {
                _config.UseParentTransform = true;
                _config.ParentTransform = parentTransform;
            }

            return this;
        }

        /// <summary>
        /// Sets whether to reset component values when released back to the pool
        /// </summary>
        /// <param name="resetComponent">Whether to reset component values</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithComponentReset(bool resetComponent)
        {
            _config.ResetComponentOnRelease = resetComponent;
            return this;
        }

        /// <summary>
        /// Sets whether to detach components from hierarchy when released
        /// </summary>
        /// <param name="detachFromHierarchy">Whether to detach from hierarchy</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithHierarchyDetachment(bool detachFromHierarchy)
        {
            _config.DetachFromHierarchy = detachFromHierarchy;
            return this;
        }

        /// <summary>
        /// Sets whether to disable components when released back to the pool
        /// </summary>
        /// <param name="disableOnRelease">Whether to disable on release</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithDisableOnRelease(bool disableOnRelease)
        {
            _config.DisableOnRelease = disableOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects when released back to the pool.
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Creates a Component pool configuration builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder</returns>
        public static ComponentPoolConfigBuilder Component(int initialCapacity)
        {
            return new ComponentPoolConfigBuilder().WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a Component pool configuration builder with specified initial capacity and parent transform
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="parent">The parent transform to use for pooled components</param>
        /// <returns>A new component pool configuration builder</returns>
        public static ComponentPoolConfigBuilder Component(int initialCapacity, Transform parent)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithParentTransform(parent);
        }

        /// <summary>
        /// Sets whether to invoke Unity component lifecycle methods
        /// </summary>
        /// <param name="invokeLifecycleMethods">Whether to invoke lifecycle methods</param>
        /// <returns>The builder instance for method chaining</returns>
        public ComponentPoolConfigBuilder WithLifecycleMethods(bool invokeLifecycleMethods)
        {
            _config.InvokeLifecycleMethods = invokeLifecycleMethods;
            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public ComponentPoolConfigBuilder FromExisting(ComponentPoolConfig source)
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

            // Copy component-specific properties
            _config.UseParentTransform = source.UseParentTransform;
            _config.ParentTransform = source.ParentTransform;
            _config.ResetComponentOnRelease = source.ResetComponentOnRelease;
            _config.DetachFromHierarchy = source.DetachFromHierarchy;
            _config.DisableOnRelease = source.DisableOnRelease;
            _config.InvokeLifecycleMethods = source.InvokeLifecycleMethods;
            _config.UseNativeReferences = source.UseNativeReferences;
            _config.OptimizeHierarchyOperations = source.OptimizeHierarchyOperations;
            _config.MaintainLocalPositions = source.MaintainLocalPositions;
            _config.PoolChildObjects = source.PoolChildObjects;
            _config.BatchActivationOperations = source.BatchActivationOperations;
            _config.UseSafetyHandles = source.UseSafetyHandles;
            _config.TrackAllocationSizes = source.TrackAllocationSizes;
            _config.UseSIMDAlignment = source.UseSIMDAlignment;
            _config.DisposeSentinel = source.DisposeSentinel;
            _config.UseParallelJobFriendlyLayout = source.UseParallelJobFriendlyLayout;

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance settings.
        /// Optimizes for speed with minimal overhead for frequently used components.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsHighPerformance()
        {
            _config.PrewarmOnInit = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.EnableAutoShrink = false;
            _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
            _config.ResetComponentOnRelease = false;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = false;
            _config.LogWarnings = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug settings.
        /// Enables comprehensive logging, metrics, and safe component handling.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 5;
            _config.ResetComponentOnRelease = true;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = true;
            _config.PrewarmOnInit = false;
            _config.ResetOnRelease = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for balanced settings.
        /// Provides a compromise between performance and safety features.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsBalanced()
        {
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 120f;
            _config.ResetComponentOnRelease = true;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = false;
            _config.PrewarmOnInit = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient settings.
        /// Optimizes for minimal memory usage with aggressive shrinking.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 30f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = true;
            _config.ResetComponentOnRelease = true;

            return this;
        }

        /// <summary>
        /// Configures the builder optimized for frequent instantiation/destruction cycles.
        /// Settings focus on reusing components efficiently in dynamic environments.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsFrequentUse()
        {
            _config.PrewarmOnInit = true;
            _config.EnableAutoShrink = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = false;
            _config.ResetComponentOnRelease = false;
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for UI components with specialized settings.
        /// Optimizes for UI-specific component handling with appropriate hierarchy management.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public ComponentPoolConfigBuilder AsUIOptimized()
        {
            _config.PrewarmOnInit = true;
            _config.DisableOnRelease = true;
            _config.DetachFromHierarchy = true;
            _config.ResetComponentOnRelease = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.3f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.ShrinkInterval = 60f;

            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ComponentPoolConfig Build()
        {
            ValidateConfiguration();
            return _config;
        }

        /// <summary>
        /// Validates the configuration before building
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        protected virtual void ValidateConfiguration()
        {
            if (_config.InitialCapacity < 0)
                throw new InvalidOperationException("Initial capacity cannot be negative");

            if (_config.MaximumCapacity > 0 && _config.InitialCapacity > _config.MaximumCapacity)
                throw new InvalidOperationException("Initial capacity cannot exceed maximum size");

            if (_config.UseParentTransform && _config.ParentTransform == null)
                throw new InvalidOperationException("Parent transform is enabled but no transform is set");

            if (_config.UseExponentialGrowth && _config.GrowthFactor <= 1.0f)
                throw new InvalidOperationException(
                    "Growth factor must be greater than 1.0 when using exponential growth");

            // Thread safety validation for Unity components
            if (_config.ThreadingMode != PoolThreadingMode.SingleThreaded)
            {
                _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
                Debug.LogWarning(
                    "Forcing single-threaded mode for component pool as Unity components are not thread-safe");
            }
        }
    }
}