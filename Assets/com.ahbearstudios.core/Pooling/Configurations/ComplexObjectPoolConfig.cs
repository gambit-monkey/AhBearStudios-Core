using System;
using AhBearStudios.Pooling.Builders;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Configuration for complex object pools that support advanced features like dependencies tracking,
    /// property storage, validation, and enhanced lifecycle management.
    /// Fully compatible with Unity Collections v2, Burst, and Unity Jobs system.
    /// Uses composition rather than inheritance for better modularity.
    /// </summary>
    public sealed class ComplexObjectPoolConfig : IPoolConfig, IDisposable
    {
        #region IPoolConfig Implementation

        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum size of the pool. Set to 0 for unlimited.
        /// </summary>
        public int MaximumCapacity { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to prewarm the pool on initialization.
        /// </summary>
        public bool PrewarmOnInit { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect metrics for this pool.
        /// </summary>
        public bool CollectMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log detailed pool operations.
        /// </summary>
        public bool DetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log warnings when the pool grows.
        /// </summary>
        public bool LogWarnings { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to call Reset() on objects when they are released.
        /// </summary>
        public bool ResetOnRelease { get; set; } = true;

        /// <summary>
        /// Gets or sets the threading mode for the pool.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.SingleThreaded;

        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.25f;

        /// <summary>
        /// Gets or sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        public float ShrinkInterval { get; set; } = 60f;

        /// <summary>
        /// Gets or sets the allocator to use for native containers.
        /// Uses Unity.Collections v2 Allocator enum.
        /// </summary>
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;

        /// <summary>
        /// Gets or sets whether to use exponential growth when expanding the pool.
        /// </summary>
        public bool UseExponentialGrowth { get; set; } = true;

        /// <summary>
        /// Gets or sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        public float GrowthFactor { get; set; } = 2.0f;

        /// <summary>
        /// Gets or sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        public int GrowthIncrement { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        public bool ThrowIfExceedingMaxCount { get; set; } = false;

        #endregion

        #region Complex Pool Specific Properties

        /// <summary>
        /// Gets or sets whether to enable dependency tracking for pooled objects.
        /// </summary>
        public bool EnableDependencyTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable property storage for pooled objects.
        /// </summary>
        public bool EnablePropertyStorage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate objects when retrieved from the pool.
        /// </summary>
        public bool ValidateOnAcquire { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to create new objects if validation fails.
        /// </summary>
        public bool RecreateOnValidationFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically destroy objects that fail validation.
        /// </summary>
        public bool DestroyInvalidObjects { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to clear properties when an object is released to the pool.
        /// </summary>
        public bool ClearPropertiesOnRelease { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial capacity for property dictionaries.
        /// </summary>
        public int InitialPropertyCapacity { get; set; } = 4;

        /// <summary>
        /// Gets or sets the initial capacity for dependency lists.
        /// </summary>
        public int InitialDependencyCapacity { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to track timing metrics for pool operations.
        /// </summary>
        public bool TrackOperationTimings { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to dispose objects asynchronously.
        /// </summary>
        public bool UseAsyncDisposal { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to ensure memory is aligned for SIMD operations.
        /// </summary>
        public bool UseSIMDAlignment { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use safety handles for native collections.
        /// </summary>
        public bool UseSafetyHandles { get; set; } = true;

        /// <summary>
        /// Gets or sets whether objects should track memory usage.
        /// </summary>
        public bool TrackAllocationSizes { get; set; } = false;

        /// <summary>
        /// Gets or sets the disposal sentinel value for native containers.
        /// </summary>
        public byte DisposeSentinel { get; set; } = 0xFF;

        /// <summary>
        /// Gets or sets whether to use parallel job friendly data layouts.
        /// </summary>
        public bool UseParallelJobFriendlyLayout { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use fixed string buffer sizes for deterministic memory layout.
        /// </summary>
        public bool UseFixedStringBuffers { get; set; } = true;

        /// <summary>
        /// Gets or sets the fixed string buffer capacity when UseFixedStringBuffers is true.
        /// </summary>
        public int FixedStringCapacity { get; set; } = 128;

        /// <summary>
        /// Gets or sets whether to enable memory fences for cross-thread synchronization.
        /// </summary>
        public bool EnableMemoryFences { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use bursted functions where available.
        /// </summary>
        public bool UseBurstFunctions { get; set; } = true;

        #endregion

        #region Private Fields

        private readonly IPoolLogger _logger;
        private readonly IPoolingServiceLocator _serviceLocator;
        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the complex object pool configuration.
        /// </summary>
        public ComplexObjectPoolConfig()
        {
            _logger = null;
            _serviceLocator = null;
        }

        /// <summary>
        /// Creates a new instance of the complex object pool configuration with dependency injection.
        /// </summary>
        /// <param name="logger">Logger service for pool logging</param>
        /// <param name="serviceLocator">Service locator for additional pool services</param>
        public ComplexObjectPoolConfig(IPoolLogger logger, IPoolingServiceLocator serviceLocator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceLocator = serviceLocator;
        }

        /// <summary>
        /// Creates a new complex object pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ComplexObjectPoolConfig(int initialCapacity) : this()
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }

        /// <summary>
        /// Creates a new complex object pool configuration with the specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than initialCapacity and not 0</exception>
        public ComplexObjectPoolConfig(int initialCapacity, int maxSize) : this(initialCapacity)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be negative");

            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize),
                    "Max size must be greater than or equal to initial capacity, or 0 for unlimited");

            MaximumCapacity = maxSize;
        }

        #endregion

        #region IPoolConfig Implementation

        /// <summary>
        /// Creates a clone of this configuration.
        /// </summary>
        /// <returns>A new IPoolConfig instance with the same settings</returns>
        public IPoolConfig Clone()
        {
            var clone = new ComplexObjectPoolConfig(_logger, _serviceLocator)
            {
                // Copy standard properties
                ConfigId = Guid.NewGuid().ToString(), // Generate a new ID for the clone
                InitialCapacity = this.InitialCapacity,
                MinimumCapacity = this.MinimumCapacity,
                MaximumCapacity = this.MaximumCapacity,
                PrewarmOnInit = this.PrewarmOnInit,
                CollectMetrics = this.CollectMetrics,
                DetailedLogging = this.DetailedLogging,
                LogWarnings = this.LogWarnings,
                ResetOnRelease = this.ResetOnRelease,
                ThreadingMode = this.ThreadingMode,
                EnableAutoShrink = this.EnableAutoShrink,
                ShrinkThreshold = this.ShrinkThreshold,
                ShrinkInterval = this.ShrinkInterval,
                NativeAllocator = this.NativeAllocator,
                UseExponentialGrowth = this.UseExponentialGrowth,
                GrowthFactor = this.GrowthFactor,
                GrowthIncrement = this.GrowthIncrement,
                ThrowIfExceedingMaxCount = this.ThrowIfExceedingMaxCount,

                // Copy complex-specific properties
                EnableDependencyTracking = this.EnableDependencyTracking,
                EnablePropertyStorage = this.EnablePropertyStorage,
                ValidateOnAcquire = this.ValidateOnAcquire,
                RecreateOnValidationFailure = this.RecreateOnValidationFailure,
                DestroyInvalidObjects = this.DestroyInvalidObjects,
                ClearPropertiesOnRelease = this.ClearPropertiesOnRelease,
                InitialPropertyCapacity = this.InitialPropertyCapacity,
                InitialDependencyCapacity = this.InitialDependencyCapacity,
                TrackOperationTimings = this.TrackOperationTimings,
                UseAsyncDisposal = this.UseAsyncDisposal,
                UseSIMDAlignment = this.UseSIMDAlignment,
                UseSafetyHandles = this.UseSafetyHandles,
                TrackAllocationSizes = this.TrackAllocationSizes,
                DisposeSentinel = this.DisposeSentinel,
                UseParallelJobFriendlyLayout = this.UseParallelJobFriendlyLayout,
                UseFixedStringBuffers = this.UseFixedStringBuffers,
                FixedStringCapacity = this.FixedStringCapacity,
                EnableMemoryFences = this.EnableMemoryFences,
                UseBurstFunctions = this.UseBurstFunctions
            };

            // Log cloning operation if logger is available
            _logger?.LogInfoInstance($"Cloned complex pool configuration with ID: {ConfigId}");

            return clone;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates an appropriate SIMD-aligned capacity for native collections if SIMD alignment is enabled.
        /// </summary>
        /// <param name="requestedCapacity">The original requested capacity</param>
        /// <returns>The SIMD-aligned capacity</returns>
        public int GetAlignedCapacity(int requestedCapacity)
        {
            if (!UseSIMDAlignment)
                return requestedCapacity;

            // Align to typical SIMD register size (128 bits / 16 bytes)
            const int simdAlignment = 4; // 4 integers or floats per SIMD register
            return (requestedCapacity + simdAlignment - 1) & ~(simdAlignment - 1);
        }

        /// <summary>
        /// Creates a configured NativeList with the configuration's settings applied.
        /// Uses Collections v2 APIs.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list</typeparam>
        /// <param name="initialCapacity">The initial capacity of the list</param>
        /// <returns>A new NativeList configured according to settings</returns>
        public NativeList<T> CreateConfiguredList<T>(int initialCapacity = 0)
            where T : unmanaged
        {
            int capacity = initialCapacity > 0 ? initialCapacity : InitialCapacity;
            int alignedCapacity = GetAlignedCapacity(capacity);

            var options = NativeArrayOptions.ClearMemory;

            // Create the appropriate NativeList based on configuration
            var list = new NativeList<T>(alignedCapacity, NativeAllocator);

            // Configure safety handles based on settings
            if (!UseSafetyHandles)
            {
                DisposeSafetyForList(list);
            }

            // Track allocation size if enabled
            if (TrackAllocationSizes && _logger != null)
            {
                int byteSize = UnsafeUtility.SizeOf<T>() * alignedCapacity;
                _logger.LogInfoInstance(
                    $"Created NativeList of {typeof(T).Name} with capacity {alignedCapacity} using {byteSize} bytes");
            }

            return list;
        }

        /// <summary>
        /// Creates a configured NativeParallelHashMap with the configuration's settings applied.
        /// Uses Collections v2 APIs.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        /// <param name="initialCapacity">The initial capacity of the map</param>
        /// <returns>A new NativeParallelHashMap configured according to settings</returns>
        public NativeParallelHashMap<TKey, TValue> CreateConfiguredHashMap<TKey, TValue>(int initialCapacity = 0)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            int capacity = initialCapacity > 0 ? initialCapacity : InitialPropertyCapacity;

            // Create the hash map
            var hashMap = new NativeParallelHashMap<TKey, TValue>(capacity, NativeAllocator);

            // Configure safety handles based on settings
            if (!UseSafetyHandles)
            {
                DisposeSafetyForHashMap(hashMap);
            }

            // Track allocation if enabled
            if (TrackAllocationSizes && _logger != null)
            {
                int byteSize = UnsafeUtility.SizeOf<TKey>() * capacity + UnsafeUtility.SizeOf<TValue>() * capacity;
                _logger.LogInfoInstance(
                    $"Created NativeParallelHashMap<{typeof(TKey).Name}, {typeof(TValue).Name}> with capacity {capacity} using approximately {byteSize} bytes");
            }

            return hashMap;
        }

        /// <summary>
        /// Calculates the optimal growth size for the pool.
        /// </summary>
        /// <param name="currentCapacity">The current capacity of the pool</param>
        /// <returns>The new capacity after growth</returns>
        public int CalculateGrowthSize(int currentCapacity)
        {
            if (UseExponentialGrowth)
            {
                int newCapacity = Mathf.CeilToInt(currentCapacity * GrowthFactor);

                // Ensure we at least add one element
                if (newCapacity <= currentCapacity)
                    newCapacity = currentCapacity + 1;

                // Respect maximum capacity if set
                if (MaximumCapacity > 0)
                    newCapacity = Mathf.Min(newCapacity, MaximumCapacity);

                return newCapacity;
            }
            else
            {
                // Linear growth
                int newCapacity = currentCapacity + GrowthIncrement;

                // Respect maximum capacity if set
                if (MaximumCapacity > 0)
                    newCapacity = Mathf.Min(newCapacity, MaximumCapacity);

                return newCapacity;
            }
        }

        /// <summary>
        /// Determines if the pool should shrink based on current usage.
        /// </summary>
        /// <param name="usedCount">The number of items currently in use</param>
        /// <param name="totalCapacity">The total capacity of the pool</param>
        /// <returns>True if the pool should shrink, false otherwise</returns>
        public bool ShouldShrink(int usedCount, int totalCapacity)
        {
            if (!EnableAutoShrink || totalCapacity <= MinimumCapacity)
                return false;

            float usageRatio = totalCapacity > 0 ? (float)usedCount / totalCapacity : 0f;
            return usageRatio <= ShrinkThreshold;
        }

        /// <summary>
        /// Calculates the size to shrink to based on current usage.
        /// </summary>
        /// <param name="usedCount">The number of items currently in use</param>
        /// <returns>The new capacity after shrinking</returns>
        public int CalculateShrinkSize(int usedCount)
        {
            // Never shrink below minimum capacity
            int targetCapacity = Mathf.Max(MinimumCapacity, usedCount);

            // If using exponential growth, shrink to neat power of GrowthFactor
            if (UseExponentialGrowth && targetCapacity > InitialCapacity)
            {
                float logFactor = Mathf.Log(targetCapacity / (float)InitialCapacity) / Mathf.Log(GrowthFactor);
                int nearestPower = Mathf.CeilToInt(logFactor);
                targetCapacity = Mathf.CeilToInt(InitialCapacity * Mathf.Pow(GrowthFactor, nearestPower));
            }

            return targetCapacity;
        }

        /// <summary>
        /// Disables safety checks for a NativeList.
        /// Only used when UseSafetyHandles is false.
        /// </summary>
        private unsafe void DisposeSafetyForList<T>(NativeList<T> list) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(((AtomicSafetyHandle*)UnsafeUtility.AddressOf(ref list))[0]);
#endif
        }

        /// <summary>
        /// Disables safety checks for a NativeParallelHashMap.
        /// Only used when UseSafetyHandles is false.
        /// </summary>
        private unsafe void DisposeSafetyForHashMap<TKey, TValue>(NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(((AtomicSafetyHandle*)UnsafeUtility.AddressOf(ref hashMap))[0]);
#endif
        }

        /// <summary>
        /// Validates this configuration and adjusts values to ensure they are within acceptable ranges.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool Validate()
        {
            // Validate capacity settings
            if (InitialCapacity < 0)
                InitialCapacity = 0;

            if (MinimumCapacity < 0)
                MinimumCapacity = 0;

            if (MaximumCapacity < 0)
                MaximumCapacity = 0;

            if (MinimumCapacity > InitialCapacity)
                MinimumCapacity = InitialCapacity;

            if (MaximumCapacity > 0 && MaximumCapacity < InitialCapacity)
                InitialCapacity = MaximumCapacity;

            // Validate shrink settings
            if (ShrinkThreshold < 0.05f)
                ShrinkThreshold = 0.05f;
            else if (ShrinkThreshold > 0.95f)
                ShrinkThreshold = 0.95f;

            if (ShrinkInterval < 0)
                ShrinkInterval = 0;

            // Validate growth settings
            if (GrowthFactor < 1.1f)
                GrowthFactor = 1.1f;

            if (GrowthIncrement < 1)
                GrowthIncrement = 1;

            // Validate property capacity settings
            if (InitialPropertyCapacity < 1)
                InitialPropertyCapacity = 1;

            if (InitialDependencyCapacity < 1)
                InitialDependencyCapacity = 1;

            // Validate v2-specific settings
            if (FixedStringCapacity < 16)
                FixedStringCapacity = 16;

            return true;
        }

        /// <summary>
        /// Converts this configuration to a builder for further modification.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public ComplexObjectPoolConfigBuilder ToBuilder()
        {
            return new ComplexObjectPoolConfigBuilder().FromExisting(this);
        }

        /// <summary>
        /// Registers this configuration with the specified registry.
        /// </summary>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        public void Register(IPoolConfigRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            registry.RegisterConfig(ConfigId, this);
        }

        /// <summary>
        /// Registers this configuration for a specific item type.
        /// </summary>
        /// <typeparam name="T">Type of the items this configuration is for.</typeparam>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        public void RegisterForType<T>(IPoolConfigRegistry registry) where T : class
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            registry.RegisterConfigForType<T>(this);
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new builder for complex pool configurations.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        public static ComplexObjectPoolConfigBuilder CreateBuilder()
        {
            return new ComplexObjectPoolConfigBuilder();
        }

        /// <summary>
        /// Creates a new builder for complex pool configurations with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A new builder instance.</returns>
        public static ComplexObjectPoolConfigBuilder CreateBuilder(int initialCapacity)
        {
            return new ComplexObjectPoolConfigBuilder().WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a new builder for complex pool configurations with the specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="maximumCapacity">Maximum pool size (0 for unlimited).</param>
        /// <returns>A new builder instance.</returns>
        public static ComplexObjectPoolConfigBuilder CreateBuilder(int initialCapacity, int maximumCapacity)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maximumCapacity);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs cleanup of any resources used by this configuration.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Log disposal if logger is available
            _logger?.LogInfoInstance($"Disposing complex pool configuration with ID: {ConfigId}");

            _isDisposed = true;
        }

        #endregion
    }
}