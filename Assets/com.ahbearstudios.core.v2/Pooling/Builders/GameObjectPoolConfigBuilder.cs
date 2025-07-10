using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for GameObject pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for Unity GameObject pools with a fluent API.
    /// </summary>
    public class GameObjectPoolConfigBuilder : IPoolConfigBuilder<GameObjectPoolConfig, GameObjectPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly GameObjectPoolConfig _config;

        /// <summary>
        /// Creates a new GameObject pool configuration builder with default settings
        /// </summary>
        public GameObjectPoolConfigBuilder()
        {
            _config = new GameObjectPoolConfig();
        }

        /// <summary>
        /// Creates a new GameObject pool configuration builder with an existing configuration
        /// </summary>
        /// <param name="sourceConfig">Source configuration to copy</param>
        /// <exception cref="ArgumentNullException">Thrown if sourceConfig is null</exception>
        public GameObjectPoolConfigBuilder(GameObjectPoolConfig sourceConfig)
        {
            _config = sourceConfig ?? throw new ArgumentNullException(nameof(sourceConfig));
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public GameObjectPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public GameObjectPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to disable GameObjects when released back to the pool
        /// </summary>
        public GameObjectPoolConfigBuilder WithDisableOnRelease(bool disableOnRelease)
        {
            _config.DisableOnRelease = disableOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to reparent GameObjects when released
        /// </summary>
        public GameObjectPoolConfigBuilder WithReparentOnRelease(bool reparentOnRelease)
        {
            _config.ReparentOnRelease = reparentOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to toggle GameObject active state
        /// </summary>
        public GameObjectPoolConfigBuilder WithToggleActive(bool toggleActive)
        {
            _config.ToggleActive = toggleActive;
            return this;
        }

        /// <summary>
        /// Sets whether to call pool lifecycle events
        /// </summary>
        public GameObjectPoolConfigBuilder WithCallPoolEvents(bool callPoolEvents)
        {
            _config.CallPoolEvents = callPoolEvents;
            return this;
        }

        /// <summary>
        /// Sets whether to validate GameObjects before acquisition
        /// </summary>
        public GameObjectPoolConfigBuilder WithValidateOnAcquire(bool validateOnAcquire)
        {
            _config.ValidateOnAcquire = validateOnAcquire;
            return this;
        }

        /// <summary>
        /// Sets the layers for pooled GameObjects
        /// </summary>
        public GameObjectPoolConfigBuilder WithActiveLayers(int activeLayer, int inactiveLayer)
        {
            _config.ActiveLayer = activeLayer;
            _config.InactiveLayer = inactiveLayer;
            return this;
        }

        /// <summary>
        /// Sets the parent transform for pooled GameObjects
        /// </summary>
        public GameObjectPoolConfigBuilder WithParentTransform(Transform parentTransform)
        {
            _config.ParentTransform = parentTransform;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset objects when released back to the pool
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        /// <param name="threadingMode">The threading mode to use</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        /// <param name="autoShrink">Whether to enable auto-shrinking</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithAutoShrink(bool autoShrink)
        {
            _config.EnableAutoShrink = autoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        /// <param name="threshold">The shrink threshold (0.0-1.0)</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds between shrink operations</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = Mathf.Max(0f, intervalSeconds);
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for native pools
        /// </summary>
        /// <param name="allocator">The allocator to use</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        /// <param name="useExponential">Whether to use exponential growth</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithExponentialGrowth(bool useExponential)
        {
            _config.UseExponentialGrowth = useExponential;
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth)
        /// </summary>
        /// <param name="factor">The growth factor (multiplicative)</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, factor);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth)
        /// </summary>
        /// <param name="increment">The number of items to add each time</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithGrowthIncrement(int increment)
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
        public GameObjectPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Sets whether to reset object position when released back to the pool
        /// </summary>
        /// <param name="resetPositionOnRelease">Whether to reset position on release</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithResetPositionOnRelease(bool resetPositionOnRelease)
        {
            _config.ResetPositionOnRelease = resetPositionOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to reset object rotation when released back to the pool
        /// </summary>
        /// <param name="resetRotationOnRelease">Whether to reset rotation on release</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithResetRotationOnRelease(bool resetRotationOnRelease)
        {
            _config.ResetRotationOnRelease = resetRotationOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to reset object scale when released back to the pool
        /// </summary>
        /// <param name="resetScaleOnRelease">Whether to reset scale on release</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithResetScaleOnRelease(bool resetScaleOnRelease)
        {
            _config.ResetScaleOnRelease = resetScaleOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to use batch operations for GameObjects.
        /// </summary>
        /// <param name="useBatchOperations">Whether to batch operations</param>
        /// <param name="batchSize">The batch size if enabled</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithBatchOperations(bool useBatchOperations, int batchSize = 16)
        {
            _config.UseBatchOperations = useBatchOperations;
            if (useBatchOperations && batchSize > 0)
            {
                _config.BatchSize = batchSize;
            }

            return this;
        }

        /// <summary>
        /// Sets whether to maintain local positions when reparenting GameObjects.
        /// </summary>
        /// <param name="maintainLocalPositions">Whether to maintain local positions</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithMaintainLocalPositions(bool maintainLocalPositions)
        {
            _config.MaintainLocalPositions = maintainLocalPositions;
            return this;
        }

        /// <summary>
        /// Sets whether to use native collections for internal storage.
        /// </summary>
        /// <param name="useNativeCollections">Whether to use native collections</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithNativeCollections(bool useNativeCollections)
        {
            _config.UseNativeCollections = useNativeCollections;
            return this;
        }

        /// <summary>
        /// Sets whether to pool child GameObjects.
        /// </summary>
        /// <param name="poolChildObjects">Whether to pool child objects</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithPoolChildObjects(bool poolChildObjects)
        {
            _config.PoolChildObjects = poolChildObjects;
            return this;
        }

        /// <summary>
        /// Sets whether to use scene handles for more efficient object tracking.
        /// </summary>
        /// <param name="useSceneHandles">Whether to use scene handles</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithSceneHandles(bool useSceneHandles)
        {
            _config.UseSceneHandles = useSceneHandles;
            return this;
        }

        /// <summary>
        /// Sets whether to persist through scene loads.
        /// </summary>
        /// <param name="persistThroughSceneLoads">Whether to persist through scene loads</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithScenePersistence(bool persistThroughSceneLoads)
        {
            _config.PersistThroughSceneLoads = persistThroughSceneLoads;
            return this;
        }

        /// <summary>
        /// Sets whether to clear references on release.
        /// </summary>
        /// <param name="clearReferencesOnRelease">Whether to clear references</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithClearReferencesOnRelease(bool clearReferencesOnRelease)
        {
            _config.ClearReferencesOnRelease = clearReferencesOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to use safety handles for native collections.
        /// </summary>
        /// <param name="useSafetyHandles">Whether to use safety handles</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithSafetyHandles(bool useSafetyHandles)
        {
            _config.UseSafetyHandles = useSafetyHandles;
            return this;
        }

        /// <summary>
        /// Sets whether to use SIMD-aligned memory for better performance.
        /// </summary>
        /// <param name="useSIMDAlignment">Whether to use SIMD alignment</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithSIMDAlignment(bool useSIMDAlignment)
        {
            _config.UseSIMDAlignment = useSIMDAlignment;
            return this;
        }

        /// <summary>
        /// Sets whether to track allocation sizes for profiling.
        /// </summary>
        /// <param name="trackAllocationSizes">Whether to track allocation sizes</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithTrackAllocationSizes(bool trackAllocationSizes)
        {
            _config.TrackAllocationSizes = trackAllocationSizes;
            return this;
        }

        /// <summary>
        /// Sets whether to reset transform properties on release.
        /// </summary>
        /// <param name="resetPosition">Whether to reset position</param>
        /// <param name="resetRotation">Whether to reset rotation</param>
        /// <param name="resetScale">Whether to reset scale</param>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder WithResetTransformOnRelease(bool resetPosition, bool resetRotation,
            bool resetScale)
        {
            _config.ResetPositionOnRelease = resetPosition;
            _config.ResetRotationOnRelease = resetRotation;
            _config.ResetScaleOnRelease = resetScale;
            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public GameObjectPoolConfigBuilder FromExisting(GameObjectPoolConfig source)
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

            // Copy GameObject-specific properties
            _config.ResetPositionOnRelease = source.ResetPositionOnRelease;
            _config.ResetRotationOnRelease = source.ResetRotationOnRelease;
            _config.ResetScaleOnRelease = source.ResetScaleOnRelease;
            _config.DisableOnRelease = source.DisableOnRelease;
            _config.ReparentOnRelease = source.ReparentOnRelease;
            _config.ToggleActive = source.ToggleActive;
            _config.CallPoolEvents = source.CallPoolEvents;
            _config.ValidateOnAcquire = source.ValidateOnAcquire;
            _config.ActiveLayer = source.ActiveLayer;
            _config.InactiveLayer = source.InactiveLayer;
            _config.ParentTransform = source.ParentTransform;
            _config.MaintainLocalPositions = source.MaintainLocalPositions;
            _config.UseBatchOperations = source.UseBatchOperations;
            _config.BatchSize = source.BatchSize;
            _config.UseNativeCollections = source.UseNativeCollections;
            _config.PoolChildObjects = source.PoolChildObjects;
            _config.UseSceneHandles = source.UseSceneHandles;
            _config.PersistThroughSceneLoads = source.PersistThroughSceneLoads;
            _config.ClearReferencesOnRelease = source.ClearReferencesOnRelease;
            _config.UseSafetyHandles = source.UseSafetyHandles;
            _config.UseSIMDAlignment = source.UseSIMDAlignment;
            _config.TrackAllocationSizes = source.TrackAllocationSizes;
            _config.DisposeSentinel = source.DisposeSentinel;

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance settings.
        /// Optimizes for speed with minimal overhead for frequently used GameObjects.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsHighPerformance()
        {
            _config.PrewarmOnInit = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.EnableAutoShrink = false;
            _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.CallPoolEvents = false;
            _config.ValidateOnAcquire = false;
            _config.LogWarnings = false;
            _config.ResetPositionOnRelease = false;
            _config.ResetRotationOnRelease = false;
            _config.ResetScaleOnRelease = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug settings.
        /// Enables comprehensive logging, metrics, and validation for development.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 5;
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.CallPoolEvents = true;
            _config.ValidateOnAcquire = true;
            _config.ResetOnRelease = true;
            _config.ResetPositionOnRelease = true;
            _config.ResetRotationOnRelease = true;
            _config.ResetScaleOnRelease = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for balanced settings.
        /// Provides a compromise between performance and safety features.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsBalanced()
        {
            _config.DetailedLogging = false;
            _config.UseSIMDAlignment = true;
            _config.UseBatchOperations = true;
            _config.BatchSize = 16;
            _config.PoolChildObjects = true;
            _config.UseSceneHandles = true;
            _config.PersistThroughSceneLoads = false;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 120f;
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.CallPoolEvents = true;
            _config.ValidateOnAcquire = false;
            _config.ResetPositionOnRelease = true;
            _config.PrewarmOnInit = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient settings.
        /// Optimizes for minimal memory usage with aggressive shrinking.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.UseSIMDAlignment = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 30f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ValidateOnAcquire = true;
            _config.ResetPositionOnRelease = true;
            _config.ResetRotationOnRelease = true;
            _config.ResetScaleOnRelease = true;

            return this;
        }

        /// <summary>
        /// Configures the builder optimized for UI GameObjects.
        /// Settings focus on efficient UI element pooling with appropriate transforms handling.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsUIOptimized()
        {
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.3f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.ShrinkInterval = 60f;
            _config.PrewarmOnInit = true;
            _config.ResetPositionOnRelease = true;
            _config.ResetRotationOnRelease = false;
            _config.ResetScaleOnRelease = true;
            _config.ValidateOnAcquire = false;

            return this;
        }

        /// <summary>
        /// Configures the builder optimized for frequent instantiation/destruction cycles.
        /// Settings focus on reusing GameObjects efficiently in dynamic environments.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsFrequentUse()
        {
            _config.PrewarmOnInit = true;
            _config.EnableAutoShrink = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.CallPoolEvents = false;
            _config.ValidateOnAcquire = false;
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;
            _config.ResetPositionOnRelease = false;
            _config.ResetRotationOnRelease = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for scene transition handling.
        /// Optimizes for GameObjects that need to persist between scene loads.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameObjectPoolConfigBuilder AsSceneTransition()
        {
            _config.DisableOnRelease = true;
            _config.ReparentOnRelease = true;
            _config.ToggleActive = true;
            _config.CallPoolEvents = true;
            _config.ValidateOnAcquire = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.PrewarmOnInit = false;
            _config.ResetPositionOnRelease = true;
            _config.ResetRotationOnRelease = true;
            _config.ResetScaleOnRelease = true;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 30f;

            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public GameObjectPoolConfig Build()
        {
            ValidateConfiguration();
            var clone = _config.Clone() as GameObjectPoolConfig;
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

            if (_config.ParentTransform == null)
                throw new InvalidOperationException("Parent transform is enabled but no transform is set");

            // Layer validation
            if (_config.ActiveLayer < 0 || _config.ActiveLayer > 31)
                throw new InvalidOperationException("Active layer must be between 0 and 31");

            if (_config.InactiveLayer < 0 || _config.InactiveLayer > 31)
                throw new InvalidOperationException("Inactive layer must be between 0 and 31");

            // Thread safety validation for Unity GameObjects
            if (_config.ThreadingMode != PoolThreadingMode.SingleThreaded)
            {
                _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
                Debug.LogWarning(
                    "Forcing single-threaded mode for GameObject pool as Unity GameObjects are not thread-safe");
            }
        }
    }
}