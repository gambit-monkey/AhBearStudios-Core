using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for Job-compatible native pools optimized for Unity's Job System.
    /// Provides specialized settings for high-performance parallel processing while maintaining
    /// compatibility with the pooling framework. Designed to work with Burst compiler and Unity Collections v2.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public sealed class JobCompatiblePoolConfig : IPoolConfig, IDisposable
    {
        #region IPoolConfig Implementation

        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 32;

        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain.
        /// </summary>
        public int MinimumCapacity { get; set; } = 8;

        /// <summary>
        /// Gets or sets the maximum size of the pool. Set to 0 for unlimited.
        /// </summary>
        public int MaximumCapacity { get; set; } = 0;

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
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.JobCompatible;

        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = false;

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

        #region Native Pool Options

        /// <summary>
        /// Gets or sets whether to use safety checks for native containers.
        /// Safety checks help detect errors but impact performance.
        /// </summary>
        public bool UseSafetyChecks { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically dispose native resources on release.
        /// </summary>
        public bool AutoDisposeOnRelease { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to verify that the correct allocator is used.
        /// </summary>
        public bool VerifyAllocator { get; set; } = true;

        /// <summary>
        /// Gets or sets flags for native configuration options.
        /// </summary>
        public int NativeConfigFlags { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to use Burst-compatible collections internally.
        /// </summary>
        public bool UseBurstCompatibleCollections { get; set; } = true;

        /// <summary>
        /// Gets or sets the specific pool type this configuration is designed for.
        /// </summary>
        public Type PoolType { get; set; } = null;

        #endregion

        #region Job Specific Options

        /// <summary>
        /// Whether to synchronize job complete status after operations.
        /// When enabled, ensures that jobs complete before returning control.
        /// Disabling can improve performance in scenarios where immediate synchronization isn't needed.
        /// </summary>
        public bool SyncAfterOps { get; set; } = true;

        /// <summary>
        /// Whether to use safety handles in Native containers for job safety.
        /// Should typically be true when using the pool in jobs to prevent race conditions.
        /// </summary>
        public bool UseJobSafetyHandles { get; set; } = true;

        /// <summary>
        /// Whether the pool resources can be accessed from multiple parallel jobs.
        /// When true, uses concurrent containers for better parallel performance.
        /// </summary>
        public bool EnableParallelJobAccess { get; set; } = false;

        /// <summary>
        /// The scheduling mode for internal operations.
        /// Controls how internal jobs are scheduled and synchronized.
        /// </summary>
        public JobSchedulingMode SchedulingMode { get; set; } = JobSchedulingMode.Parallel;

        /// <summary>
        /// Whether to use managed references within jobs (unsafe but sometimes necessary).
        /// This is not recommended for general use as it bypasses safety systems.
        /// </summary>
        public bool AllowManagedReferences { get; set; } = false;

        /// <summary>
        /// Whether to pack/align data structures for SIMD operations.
        /// Improves performance on vectorized operations.
        /// </summary>
        public bool UseSIMDAlignment { get; set; } = false;

        /// <summary>
        /// The batch size for processing items in parallel jobs.
        /// </summary>
        public int JobBatchSize { get; set; } = 64;

        /// <summary>
        /// The float quality of math operations in jobs.
        /// Set to high for precision work, medium for general use, or low for performance.
        /// </summary>
        public FloatPrecision FloatPrecision { get; set; } = FloatPrecision.Standard;

        /// <summary>
        /// The float mode for determinism in job operations.
        /// </summary>
        public FloatMode FloatMode { get; set; } = FloatMode.Fast;

        /// <summary>
        /// Whether to use the Unity Allocators v2 temp allocator for short-lived job resources.
        /// </summary>
        public bool UseTempAllocator { get; set; } = false;

        /// <summary>
        /// Whether jobs should support exception handling.
        /// </summary>
        public bool SupportExceptions { get; set; } = false;

        /// <summary>
        /// Whether to enable job dependencies for automatic dependency tracking.
        /// </summary>
        public bool EnableJobDependencies { get; set; } = true;

        #endregion

        #region Private Fields

        private readonly IPoolLogger _logger;
        private readonly IPoolingServiceLocator _serviceLocator;
        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a default Job-compatible pool configuration with optimized settings.
        /// </summary>
        public JobCompatiblePoolConfig()
        {
            _logger = null;
            _serviceLocator = null;

            // Default optimal settings for Jobs
            UseSafetyChecks = true;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = true;
            VerifyAllocator = true;
            NativeAllocator = Allocator.Persistent;
        }

        /// <summary>
        /// Creates a Job-compatible pool configuration with dependency injection.
        /// </summary>
        /// <param name="logger">Logger service for pool operations</param>
        /// <param name="serviceLocator">Optional service locator for additional services</param>
        public JobCompatiblePoolConfig(IPoolLogger logger, IPoolingServiceLocator serviceLocator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceLocator = serviceLocator;

            // Default optimal settings for Jobs
            UseSafetyChecks = true;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = true;
            VerifyAllocator = true;
            NativeAllocator = Allocator.Persistent;
        }

        /// <summary>
        /// Creates a Job-compatible pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        public JobCompatiblePoolConfig(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 4);

            // Default optimal settings for Jobs
            UseSafetyChecks = true;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = true;
            VerifyAllocator = true;
            NativeAllocator = Allocator.Persistent;
        }

        /// <summary>
        /// Creates a Job-compatible pool configuration with specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid</exception>
        public JobCompatiblePoolConfig(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));

            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 4);
            NativeAllocator = allocator;

            // Default optimal settings for Jobs
            UseSafetyChecks = true;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = true;
            VerifyAllocator = true;
        }

        /// <summary>
        /// Creates a Job-compatible pool configuration from a base NativePoolConfig.
        /// Uses composition to incorporate the native pool configuration settings.
        /// </summary>
        /// <param name="nativeConfig">The native configuration to copy settings from</param>
        /// <exception cref="ArgumentNullException">Thrown if nativeConfig is null</exception>
        public JobCompatiblePoolConfig(NativePoolConfig nativeConfig)
        {
            if (nativeConfig == null)
                throw new ArgumentNullException(nameof(nativeConfig), "Native configuration cannot be null");

            // Copy base IPoolConfig properties
            ConfigId = Guid.NewGuid().ToString(); // Generate new ID
            InitialCapacity = nativeConfig.InitialCapacity;
            MinimumCapacity = nativeConfig.MinimumCapacity;
            MaximumCapacity = nativeConfig.MaximumCapacity;
            PrewarmOnInit = nativeConfig.PrewarmOnInit;
            CollectMetrics = nativeConfig.CollectMetrics;
            DetailedLogging = nativeConfig.DetailedLogging;
            LogWarnings = nativeConfig.LogWarnings;
            ResetOnRelease = nativeConfig.ResetOnRelease;
            ThreadingMode = PoolThreadingMode.JobCompatible; // Override for job compatibility
            EnableAutoShrink = nativeConfig.EnableAutoShrink;
            ShrinkThreshold = nativeConfig.ShrinkThreshold;
            ShrinkInterval = nativeConfig.ShrinkInterval;
            NativeAllocator = nativeConfig.NativeAllocator;
            UseExponentialGrowth = nativeConfig.UseExponentialGrowth;
            GrowthFactor = nativeConfig.GrowthFactor;
            GrowthIncrement = nativeConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = nativeConfig.ThrowIfExceedingMaxCount;

            // Copy NativePoolConfig-specific properties
            UseSafetyChecks = nativeConfig.UseSafetyChecks;
            AutoDisposeOnRelease = nativeConfig.AutoDisposeOnRelease;
            VerifyAllocator = nativeConfig.VerifyAllocator;
            NativeConfigFlags = nativeConfig.NativeConfigFlags;
            UseBurstCompatibleCollections = true; // Ensure Burst compatibility
            PoolType = nativeConfig.PoolType;

            // Set job-specific defaults
            SyncAfterOps = true;
            UseJobSafetyHandles = true;
            EnableParallelJobAccess = false;
            SchedulingMode = JobSchedulingMode.Parallel;
        }

        /// <summary>
        /// Creates a Job-compatible pool configuration with full customization options.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <param name="schedulingMode">Job scheduling mode</param>
        /// <param name="useSafetyChecks">Whether to use safety checks</param>
        /// <param name="enableParallelAccess">Whether to enable parallel job access</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid</exception>
        public JobCompatiblePoolConfig(
            int initialCapacity,
            Allocator allocator,
            JobSchedulingMode schedulingMode,
            bool useSafetyChecks,
            bool enableParallelAccess)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));

            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 4);
            NativeAllocator = allocator;
            SchedulingMode = schedulingMode;
            UseSafetyChecks = useSafetyChecks;
            EnableParallelJobAccess = enableParallelAccess;

            // Default other important settings
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            UseJobSafetyHandles = useSafetyChecks;
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Creates a deep copy of the configuration.
        /// </summary>
        /// <returns>A new instance of JobCompatiblePoolConfig with the same settings</returns>
        public IPoolConfig Clone()
        {
            var clone = new JobCompatiblePoolConfig
            {
                // Copy IPoolConfig properties
                ConfigId = Guid.NewGuid().ToString(), // Generate a new ID for the clone
                InitialCapacity = this.InitialCapacity,
                MinimumCapacity = this.MinimumCapacity,
                MaximumCapacity = this.MaximumCapacity,
                PrewarmOnInit = this.PrewarmOnInit,
                UseExponentialGrowth = this.UseExponentialGrowth,
                GrowthFactor = this.GrowthFactor,
                GrowthIncrement = this.GrowthIncrement,
                EnableAutoShrink = this.EnableAutoShrink,
                ShrinkThreshold = this.ShrinkThreshold,
                ShrinkInterval = this.ShrinkInterval,
                ThreadingMode = this.ThreadingMode,
                NativeAllocator = this.NativeAllocator,
                LogWarnings = this.LogWarnings,
                CollectMetrics = this.CollectMetrics,
                DetailedLogging = this.DetailedLogging,
                ResetOnRelease = this.ResetOnRelease,
                ThrowIfExceedingMaxCount = this.ThrowIfExceedingMaxCount,

                // Copy NativePoolConfig-specific properties
                UseSafetyChecks = this.UseSafetyChecks,
                AutoDisposeOnRelease = this.AutoDisposeOnRelease,
                VerifyAllocator = this.VerifyAllocator,
                NativeConfigFlags = this.NativeConfigFlags,
                UseBurstCompatibleCollections = this.UseBurstCompatibleCollections,
                PoolType = this.PoolType,

                // Copy JobCompatiblePoolConfig-specific properties
                SyncAfterOps = this.SyncAfterOps,
                UseJobSafetyHandles = this.UseJobSafetyHandles,
                EnableParallelJobAccess = this.EnableParallelJobAccess,
                SchedulingMode = this.SchedulingMode,
                AllowManagedReferences = this.AllowManagedReferences,
                UseSIMDAlignment = this.UseSIMDAlignment,
                JobBatchSize = this.JobBatchSize,
                FloatPrecision = this.FloatPrecision,
                FloatMode = this.FloatMode,
                UseTempAllocator = this.UseTempAllocator,
                SupportExceptions = this.SupportExceptions,
                EnableJobDependencies = this.EnableJobDependencies
            };

            // Log cloning operation if logger is available
            _logger?.LogInfoInstance($"Cloned Job-compatible pool configuration with ID: {ConfigId}");

            return clone;
        }

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


        /// Creates a configured NativeList with the configuration's settings applied.
        /// Uses Collections v2 APIs with job safety configuration.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list</typeparam>
        /// <param name="initialCapacity">The initial capacity of the list</param>
        /// <returns>A new NativeList configured according to settings</returns>
        public NativeList<T> CreateConfiguredList<T>(int initialCapacity = 0)
            where T : unmanaged
        {
            int capacity = initialCapacity > 0 ? initialCapacity : InitialCapacity;
            int alignedCapacity = GetAlignedCapacity(capacity);

            // Use temporary allocator if configured
            Allocator allocator = UseTempAllocator && NativeAllocator != Allocator.Persistent
                ? Allocator.Temp
                : NativeAllocator;

            // Create the appropriate NativeList based on configuration
            var list = new NativeList<T>(alignedCapacity, allocator);

            // Configure safety handles based on settings
            if (!UseSafetyChecks || !UseJobSafetyHandles)
            {
                DisposeSafetyForList(ref list);
            }

            // Track allocation size if enabled
            if (CollectMetrics && _logger != null)
            {
                int byteSize = UnsafeUtility.SizeOf<T>() * alignedCapacity;
                _logger.LogInfoInstance(
                    $"Created NativeList of {typeof(T).Name} with capacity {alignedCapacity} using {byteSize} bytes");
            }

            return list;
        }

        /// <summary>
        /// Creates a configured NativeParallelHashMap with the configuration's settings applied.
        /// Uses Collections v2 APIs with job safety configuration.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        /// <param name="initialCapacity">The initial capacity of the map</param>
        /// <returns>A new NativeParallelHashMap configured according to settings</returns>
        public NativeParallelHashMap<TKey, TValue> CreateConfiguredHashMap<TKey, TValue>(int initialCapacity = 0)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            int capacity = initialCapacity > 0 ? initialCapacity : InitialCapacity;

            // Use temporary allocator if configured
            Allocator allocator = UseTempAllocator && NativeAllocator != Allocator.Persistent
                ? Allocator.Temp
                : NativeAllocator;

            // Create the hash map
            var hashMap = new NativeParallelHashMap<TKey, TValue>(capacity, allocator);

            // Configure safety handles based on settings
            if (!UseSafetyChecks || !UseJobSafetyHandles)
            {
                DisposeSafetyForHashMap(ref hashMap);
            }

            // Track allocation if enabled
            if (CollectMetrics && _logger != null)
            {
                int byteSize = UnsafeUtility.SizeOf<TKey>() * capacity + UnsafeUtility.SizeOf<TValue>() * capacity;
                _logger.LogInfoInstance(
                    $"Created NativeParallelHashMap<{typeof(TKey).Name}, {typeof(TValue).Name}> with capacity {capacity} using approximately {byteSize} bytes");
            }

            return hashMap;
        }

        /// <summary>
        /// Gets the safety handle for a NativeList using reflection.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list</typeparam>
        /// <param name="list">The list to get the safety handle for</param>
        /// <returns>The atomic safety handle</returns>
        private AtomicSafetyHandle GetSafetyHandleForList<T>(ref NativeList<T> list) where T : unmanaged
        {
            // Use reflection to access the internal _safety field
            var fieldInfo = typeof(NativeList<T>).GetField("_safety",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                return (AtomicSafetyHandle)fieldInfo.GetValue(list);
            }

            return default;
        }

        /// <summary>
        /// Gets the safety handle for a NativeParallelHashMap using reflection.
        /// </summary>
        /// <typeparam name="TKey">Type of keys in the hash map</typeparam>
        /// <typeparam name="TValue">Type of values in the hash map</typeparam>
        /// <param name="hashMap">The hash map to get the safety handle for</param>
        /// <returns>The atomic safety handle</returns>
        private AtomicSafetyHandle GetSafetyHandleForHashMap<TKey, TValue>(
            ref NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Use reflection to access the internal _safety field
            var fieldInfo = typeof(NativeParallelHashMap<TKey, TValue>).GetField("_safety",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                return (AtomicSafetyHandle)fieldInfo.GetValue(hashMap);
            }

            return default;
        }

        /// <summary>
        /// Gets the safety handle for a NativeArray using reflection.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array</typeparam>
        /// <param name="array">The array to get the safety handle for</param>
        /// <returns>The atomic safety handle</returns>
        private AtomicSafetyHandle GetSafetyHandleForArray<T>(ref NativeArray<T> array) where T : unmanaged
        {
            // Use reflection to access the internal _safety field
            var fieldInfo = typeof(NativeArray<T>).GetField("_safety",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                return (AtomicSafetyHandle)fieldInfo.GetValue(array);
            }

            return default;
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
                int newCapacity = (int)(currentCapacity * GrowthFactor);

                // Ensure we at least add one element
                if (newCapacity <= currentCapacity)
                    newCapacity = currentCapacity + 1;

                // Adjust for SIMD alignment if needed
                if (UseSIMDAlignment)
                {
                    newCapacity = GetAlignedCapacity(newCapacity);
                }

                // Respect maximum capacity if set
                if (MaximumCapacity > 0)
                    newCapacity = Math.Min(newCapacity, MaximumCapacity);

                return newCapacity;
            }
            else
            {
                // Linear growth
                int newCapacity = currentCapacity + GrowthIncrement;

                // Adjust for SIMD alignment if needed
                if (UseSIMDAlignment)
                {
                    newCapacity = GetAlignedCapacity(newCapacity);
                }

                // Respect maximum capacity if set
                if (MaximumCapacity > 0)
                    newCapacity = Math.Min(newCapacity, MaximumCapacity);

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
            int targetCapacity = Math.Max(MinimumCapacity, usedCount);

            // If using exponential growth, shrink to neat power of GrowthFactor
            if (UseExponentialGrowth && targetCapacity > InitialCapacity)
            {
                float logFactor = (float)Math.Log(targetCapacity / (float)InitialCapacity) /
                                  (float)Math.Log(GrowthFactor);
                int nearestPower = (int)Math.Ceiling(logFactor);
                targetCapacity = (int)(InitialCapacity * Math.Pow(GrowthFactor, nearestPower));
            }

            // Adjust for SIMD alignment if needed
            if (UseSIMDAlignment)
            {
                targetCapacity = GetAlignedCapacity(targetCapacity);
            }

            return targetCapacity;
        }

        /// <summary>
        /// Creates a builder initialized with this instance's values.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values</returns>
        public JobCompatiblePoolConfigBuilder ToBuilder()
        {
            return new JobCompatiblePoolConfigBuilder(this);
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

            // Validate job settings
            if (JobBatchSize < 1)
                JobBatchSize = 1;

            if (JobBatchSize > 1024)
                JobBatchSize = 1024;

            // Validate allocator
            if (NativeAllocator <= Allocator.None)
            {
                NativeAllocator = Allocator.Persistent;
                _logger?.LogWarningInstance("Invalid allocator detected and corrected to Allocator.Persistent");
            }

            return true;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a builder for this configuration type.
        /// </summary>
        /// <returns>A new Job-compatible pool configuration builder</returns>
        public static JobCompatiblePoolConfigBuilder CreateBuilder()
        {
            return PoolConfigBuilderFactory.JobCompatible();
        }

        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Job-compatible pool configuration builder</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        public static JobCompatiblePoolConfigBuilder CreateBuilder(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            return PoolConfigBuilderFactory.JobCompatible(initialCapacity);
        }

        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Allocator to use</param>
        /// <returns>A new Job-compatible pool configuration builder</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid</exception>
        public static JobCompatiblePoolConfigBuilder CreateBuilder(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));

            return PoolConfigBuilderFactory.JobCompatible(initialCapacity, allocator);
        }

        /// <summary>
        /// Creates a high-performance configuration optimized for parallel jobs.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new optimized configuration for parallel processing</returns>
        public static JobCompatiblePoolConfig CreateHighPerformanceConfig(int initialCapacity = 64)
        {
            return new JobCompatiblePoolConfig(initialCapacity)
            {
                UseSafetyChecks = false,
                UseJobSafetyHandles = false,
                EnableParallelJobAccess = true,
                SchedulingMode = JobSchedulingMode.Parallel,
                UseSIMDAlignment = true,
                JobBatchSize = 128,
                DetailedLogging = false,
                CollectMetrics = false,
                FloatMode = FloatMode.Fast,
                UseBurstCompatibleCollections = true,
            };
        }

        /// <summary>
        /// Creates a debugging-friendly configuration with safety checks enabled.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new debug-friendly configuration</returns>
        public static JobCompatiblePoolConfig CreateDebugConfig(int initialCapacity = 16)
        {
            return new JobCompatiblePoolConfig(initialCapacity)
            {
                UseSafetyChecks = true,
                UseJobSafetyHandles = true,
                EnableParallelJobAccess = false,
                SchedulingMode = JobSchedulingMode.Sequential,
                UseSIMDAlignment = false,
                DetailedLogging = true,
                CollectMetrics = true,
                LogWarnings = true,
                VerifyAllocator = true,
                SupportExceptions = true,
            };
        }

        /// <summary>
        /// Creates a balanced configuration suitable for most use cases.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new balanced configuration</returns>
        public static JobCompatiblePoolConfig CreateBalancedConfig(int initialCapacity = 32)
        {
            return new JobCompatiblePoolConfig(initialCapacity)
            {
                UseSafetyChecks = true,
                UseJobSafetyHandles = true,
                EnableParallelJobAccess = true,
                SchedulingMode = JobSchedulingMode.Parallel,
                UseSIMDAlignment = false,
                JobBatchSize = 64,
                DetailedLogging = false,
                CollectMetrics = true,
                LogWarnings = true,
                SyncAfterOps = true,
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Private method to dispose safety handles for NativeList.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list</typeparam>
        /// <param name="list">The list to dispose safety handles for</param>
        private unsafe void DisposeSafetyForList<T>(ref NativeList<T> list) where T : unmanaged
        {
            // Use Unity Collections LowLevel Unsafe to access and dispose safety handles
            AtomicSafetyHandle safetyHandle = GetSafetyHandleForList(ref list);

            // Check if handle is valid without accessing internal fields
            bool isValid = false;

            // Use pointer/memory comparison to check if handle is not default
            // We can't directly access 'version', so we need a different approach
            AtomicSafetyHandle defaultHandle = default;
            isValid = UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref safetyHandle),
                UnsafeUtility.AddressOf(ref defaultHandle),
                UnsafeUtility.SizeOf<AtomicSafetyHandle>()) != 0;

            if (isValid)
            {
                try
                {
                    AtomicSafetyHandle.Release(safetyHandle);
                }
                catch (Exception)
                {
                    // Silently fail if the handle can't be released
                    // This might happen if the handle is invalid in a way we can't detect
                }
            }
        }

        /// <summary>
        /// Private method to dispose safety handles for NativeParallelHashMap.
        /// </summary>
        /// <typeparam name="TKey">Type of keys in the hash map</typeparam>
        /// <typeparam name="TValue">Type of values in the hash map</typeparam>
        /// <param name="hashMap">The hash map to dispose safety handles for</param>
        private unsafe void DisposeSafetyForHashMap<TKey, TValue>(ref NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Use Unity Collections LowLevel Unsafe to access and dispose safety handles
            AtomicSafetyHandle safetyHandle = GetSafetyHandleForHashMap(ref hashMap);

            // Check if handle is valid without accessing internal fields
            bool isValid = false;

            // Use pointer/memory comparison to check if handle is not default
            AtomicSafetyHandle defaultHandle = default;
            isValid = UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref safetyHandle),
                UnsafeUtility.AddressOf(ref defaultHandle),
                UnsafeUtility.SizeOf<AtomicSafetyHandle>()) != 0;

            if (isValid)
            {
                try
                {
                    AtomicSafetyHandle.Release(safetyHandle);
                }
                catch (Exception)
                {
                    // Silently fail if the handle can't be released
                }
            }
        }

        /// <summary>
        /// Private method to dispose safety handles for NativeArray.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array</typeparam>
        /// <param name="array">The array to dispose safety handles for</param>
        private unsafe void DisposeSafetyForArray<T>(ref NativeArray<T> array) where T : unmanaged
        {
            // Use Unity Collections LowLevel Unsafe to access and dispose safety handles
            AtomicSafetyHandle safetyHandle = GetSafetyHandleForArray(ref array);

            // Check if handle is valid without accessing internal fields
            bool isValid = false;

            // Use pointer/memory comparison to check if handle is not default
            AtomicSafetyHandle defaultHandle = default;
            isValid = UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref safetyHandle),
                UnsafeUtility.AddressOf(ref defaultHandle),
                UnsafeUtility.SizeOf<AtomicSafetyHandle>()) != 0;

            if (isValid)
            {
                try
                {
                    AtomicSafetyHandle.Release(safetyHandle);
                }
                catch (Exception)
                {
                    // Silently fail if the handle can't be released
                }
            }
        }

        /// <summary>
        /// Disposes any resources allocated by this configuration.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Dispose of any managed resources
            if (_logger != null && DetailedLogging)
            {
                _logger.LogInfoInstance($"Disposing JobCompatiblePoolConfig with ID: {ConfigId}");
            }

            _isDisposed = true;
        }

        #endregion
    }
}