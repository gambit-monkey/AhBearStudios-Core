using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for BurstCompatiblePoolConfig using the fluent API pattern.
    /// Implements IPoolConfigBuilder for configuration consistency.
    /// </summary>
    public class
        BurstCompatiblePoolConfigBuilder : IPoolConfigBuilder<BurstCompatiblePoolConfig,
        BurstCompatiblePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly BurstCompatiblePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings optimized for Burst.
        /// </summary>
        public BurstCompatiblePoolConfigBuilder()
        {
            _config = new BurstCompatiblePoolConfig();
        }

        /// <summary>
        /// Creates a new builder using an existing configuration as a template.
        /// </summary>
        /// <param name="sourceConfig">Source configuration to copy settings from</param>
        /// <exception cref="ArgumentNullException">Thrown if sourceConfig is null</exception>
        public BurstCompatiblePoolConfigBuilder(BurstCompatiblePoolConfig sourceConfig)
        {
            if (sourceConfig == null)
                throw new ArgumentNullException(nameof(sourceConfig), "Source configuration cannot be null");

            _config = sourceConfig.Clone() as BurstCompatiblePoolConfig;
        }

        /// <summary>
        /// Creates a new builder with the specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid</exception>
        public BurstCompatiblePoolConfigBuilder(int initialCapacity, Allocator allocator)
        {
            ValidateInitialCapacity(initialCapacity);
            ValidateAllocator(allocator);
            _config = new BurstCompatiblePoolConfig(initialCapacity, allocator);
        }

        private static void ValidateInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
        }

        private static void ValidateAllocator(Allocator allocator)
        {
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public BurstCompatiblePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            ValidateInitialCapacity(capacity);
            _config.InitialCapacity = capacity;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public BurstCompatiblePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for threading mode
        /// </summary>
        public BurstCompatiblePoolConfigBuilder WithThreadingMode(PoolThreadingMode mode)
        {
            _config.ThreadingMode = mode;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for auto-shrink
        /// </summary>
        public BurstCompatiblePoolConfigBuilder WithAutoShrink(bool enable)
        {
            _config.EnableAutoShrink = enable;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio below which the pool will auto-shrink.
        /// </summary>
        /// <param name="threshold">Threshold value between 0 and 1</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Math.Clamp(threshold, 0.0f, 1.0f);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        /// <param name="interval">Interval in seconds</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithShrinkInterval(float interval)
        {
            _config.ShrinkInterval = Math.Max(0.0f, interval);
            return this;
        }

        /// <summary>
        /// Configures whether the pool should use exponential growth.
        /// </summary>
        /// <param name="enable">Whether to use exponential growth</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithExponentialGrowth(bool enable)
        {
            _config.UseExponentialGrowth = enable;
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        /// <param name="increment">Number of items to add during linear growth</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Math.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        /// <param name="factor">Growth factor (must be greater than 1.0)</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = Math.Max(1.01f, factor);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for metrics collection
        /// </summary>
        public BurstCompatiblePoolConfigBuilder WithMetrics(bool enable)
        {
            _config.CollectMetrics = enable;
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for the Burst-compatible pool.
        /// </summary>
        /// <param name="allocator">Allocator to use for native memory</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should use safety checks in Burst code.
        /// </summary>
        /// <param name="useSafetyChecks">Whether to enable safety checks</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithSafetyChecks(bool useSafetyChecks)
        {
            _config.UseSafetyChecks = useSafetyChecks;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should be optimized for use in jobs.
        /// </summary>
        /// <param name="jobOptimized">Whether to optimize for jobs</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithJobOptimization(bool jobOptimized)
        {
            _config.IsJobOptimized = jobOptimized;
            return this;
        }

        /// <summary>
        /// Sets the memory alignment for the native containers.
        /// </summary>
        /// <param name="alignment">Memory alignment in bytes</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithMemoryAlignment(int alignment)
        {
            _config.MemoryAlignment = alignment;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should dispose automatically when finalized.
        /// </summary>
        /// <param name="disposeOnFinalize">Whether to dispose on finalization</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithDisposeOnFinalize(bool disposeOnFinalize)
        {
            _config.DisposeOnFinalize = disposeOnFinalize;
            return this;
        }

        /// <summary>
        /// Configures the cache line padding for optimized memory access in jobs.
        /// </summary>
        /// <param name="usePadding">Whether to use cache line padding</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithCacheLinePadding(bool usePadding)
        {
            _config.UseCacheLinePadding = usePadding;
            return this;
        }

        /// <summary>
        /// Sets a debug name for the pool (used in Profiler and debugging).
        /// </summary>
        /// <param name="debugName">Debug name for the pool</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithDebugName(string debugName)
        {
            _config.DebugName = debugName;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should use parallelism with jobs.
        /// </summary>
        /// <param name="useParallelJobs">Whether to use parallel jobs</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithParallelJobs(bool useParallelJobs)
        {
            _config.UseParallelJobs = useParallelJobs;
            return this;
        }

        /// <summary>
        /// Sets the minimum batch size for parallel jobs.
        /// </summary>
        /// <param name="batchSize">Minimum batch size</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithJobBatchSize(int batchSize)
        {
            _config.JobBatchSize = Mathf.Max(1, batchSize);
            return this;
        }

        /// <summary>
        /// Configures whether the pool should sync after Burst-compiled operations.
        /// </summary>
        /// <param name="syncAfterOps">Whether to synchronize after Burst operations</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithSyncBurstAfterOps(bool syncAfterOps)
        {
            _config.SyncBurstAfterOps = syncAfterOps;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should optimize memory layout for better performance.
        /// </summary>
        /// <param name="optimize">Whether to optimize memory layout</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithOptimizeMemoryLayout(bool optimize)
        {
            _config.OptimizeMemoryLayout = optimize;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should enforce Burst compatibility constraints.
        /// </summary>
        /// <param name="ensure">Whether to ensure Burst compatibility</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithEnsureBurstCompatibility(bool ensure)
        {
            _config.EnsureBurstCompatibility = ensure;
            return this;
        }

        /// <summary>
        /// Sets additional configuration flags for native container behavior.
        /// </summary>
        /// <param name="flags">Configuration flags as bit field</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithNativeConfigFlags(int flags)
        {
            _config.NativeConfigFlags = flags;
            return this;
        }

        /// <summary>
        /// Sets the minimum capacity that the pool will maintain at all times.
        /// </summary>
        /// <param name="capacity">Minimum capacity to maintain (0 or greater)</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithMinimumCapacity(int capacity)
        {
            _config.MinimumCapacity = Math.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Configures whether the pool should prewarm (allocate objects up to initial capacity) during initialization.
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool during initialization</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should throw an exception when attempting to get items beyond the maximum capacity.
        /// </summary>
        /// <param name="throwException">Whether to throw an exception when exceeding max capacity</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Configures whether pooled items should be automatically disposed when released back to the pool.
        /// </summary>
        /// <param name="autoDispose">Whether to auto-dispose items on release</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithAutoDisposeOnRelease(bool autoDispose)
        {
            _config.AutoDisposeOnRelease = autoDispose;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should verify that the allocator is valid before each operation.
        /// </summary>
        /// <param name="verify">Whether to verify the allocator</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithVerifyAllocator(bool verify)
        {
            _config.VerifyAllocator = verify;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should track allocation statistics and stack traces.
        /// </summary>
        /// <param name="track">Whether to track allocations</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithTrackAllocations(bool track)
        {
            _config.TrackAllocations = track;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should use Burst compilation for performance-critical operations.
        /// </summary>
        /// <param name="useBurst">Whether to use Burst compilation</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithBurstCompilation(bool useBurst)
        {
            _config.UseBurstCompilation = useBurst;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should log detailed information about its operations.
        /// </summary>
        /// <param name="detailed">Whether to use detailed logging</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithDetailedLogging(bool detailed)
        {
            _config.DetailedLogging = detailed;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should log warnings when potentially problematic operations occur.
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Configures whether the pool should reset objects to their default state when released back to the pool.
        /// </summary>
        /// <param name="reset">Whether to reset objects on release</param>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder WithResetOnRelease(bool reset)
        {
            _config.ResetOnRelease = reset;
            return this;
        }

        /// <summary>
        /// Initializes the builder with values from an existing BurstCompatiblePoolConfig.
        /// </summary>
        /// <param name="config">The source configuration to copy settings from.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public BurstCompatiblePoolConfigBuilder FromExisting(BurstCompatiblePoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Source configuration cannot be null");

            // Copy IPoolConfig properties
            WithInitialCapacity(config.InitialCapacity);
            WithMaxSize(config.MaximumCapacity);
            WithMinimumCapacity(config.MinimumCapacity);
            WithPrewarming(config.PrewarmOnInit);
            WithMetrics(config.CollectMetrics);
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
            WithExceptionOnExceedingMaxCount(config.ThrowIfExceedingMaxCount);

            // Copy NativePoolConfig properties
            WithSafetyChecks(config.UseSafetyChecks);
            WithAutoDisposeOnRelease(config.AutoDisposeOnRelease);
            WithVerifyAllocator(config.VerifyAllocator);
            WithNativeConfigFlags(config.NativeConfigFlags);

            // Copy BurstCompatiblePoolConfig-specific properties
            WithSyncBurstAfterOps(config.SyncBurstAfterOps);
            WithOptimizeMemoryLayout(config.OptimizeMemoryLayout);
            WithEnsureBurstCompatibility(config.EnsureBurstCompatibility);
            WithJobOptimization(config.IsJobOptimized);
            WithMemoryAlignment(config.MemoryAlignment);
            WithDisposeOnFinalize(config.DisposeOnFinalize);
            WithCacheLinePadding(config.UseCacheLinePadding);
            WithDebugName(config.DebugName);
            WithParallelJobs(config.UseParallelJobs);
            WithJobBatchSize(config.JobBatchSize);
            WithTrackAllocations(config.TrackAllocations);
            WithBurstCompilation(config.UseBurstCompilation);

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance job system usage.
        /// Optimizes for compute-intensive parallel jobs with Burst compilation.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder AsJobOptimized()
        {
            _config.UseSafetyChecks = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.OptimizeMemoryLayout = true;
            _config.SyncBurstAfterOps = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.VerifyAllocator = false;
            _config.AutoDisposeOnRelease = true;
            _config.IsJobOptimized = true;
            _config.UseParallelJobs = true;
            _config.JobBatchSize = 64;
            _config.MemoryAlignment = 16;
            _config.UseCacheLinePadding = true;
            _config.UseBurstCompilation = true;

            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings for general use.
        /// Provides a balance between performance and safety with reasonable defaults.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder AsBalanced()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.OptimizeMemoryLayout = true;
            _config.SyncBurstAfterOps = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.IsJobOptimized = true;
            _config.JobBatchSize = 32;
            _config.MemoryAlignment = 16;
            _config.UseCacheLinePadding = false;
            _config.UseBurstCompilation = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug mode with safety features and diagnostics.
        /// Includes extensive safety checks and logging for development purposes.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder AsDebug()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.UseBurstCompatibleCollections = true;
            _config.OptimizeMemoryLayout = true;
            _config.SyncBurstAfterOps = true;
            _config.LogWarnings = true;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.IsJobOptimized = false;
            _config.JobBatchSize = 16;
            _config.UseParallelJobs = false;
            _config.TrackAllocations = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for maximum performance with minimal overhead.
        /// Optimizes for raw speed with minimal safety checks and diagnostics.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder AsHighPerformance()
        {
            _config.UseSafetyChecks = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.OptimizeMemoryLayout = true;
            _config.SyncBurstAfterOps = false;
            _config.LogWarnings = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.VerifyAllocator = false;
            _config.IsJobOptimized = true;
            _config.UseParallelJobs = true;
            _config.JobBatchSize = 128;
            _config.MemoryAlignment = 64;
            _config.UseCacheLinePadding = true;
            _config.UseBurstCompilation = true;

            return this;
        }

        /// <summary>
        /// Configures the builder for SIMD-optimized operations.
        /// Optimizes for vectorized math operations with explicit SIMD support.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public BurstCompatiblePoolConfigBuilder AsSIMDOptimized()
        {
            _config.UseSafetyChecks = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.OptimizeMemoryLayout = true;
            _config.SyncBurstAfterOps = false;
            _config.VerifyAllocator = false;
            _config.IsJobOptimized = true;
            _config.MemoryAlignment = 32; // Aligned for AVX operations
            _config.UseCacheLinePadding = true;
            _config.UseParallelJobs = true;
            _config.JobBatchSize = 64;
            _config.UseBurstCompilation = true;

            return this;
        }

        /// <summary>
        /// Configures whether the pool should track allocations.
        /// </summary>
        /// <param name="trackAllocations">Whether to track allocations</param>

        /// <summary>
        /// Builds and validates the configuration.
        /// </summary>
        /// <returns>A new BurstCompatiblePoolConfig instance with the configured settings</returns>
        public BurstCompatiblePoolConfig Build()
        {
            ValidateConfiguration();
            return _config.Clone() as BurstCompatiblePoolConfig;
        }

        /// <summary>
        /// Validates the configuration before building
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            // Ensure Burst compatibility requirements are met
            if (_config.EnsureBurstCompatibility)
            {
                _config.ThreadingMode = PoolThreadingMode.JobCompatible;
                _config.OptimizeMemoryLayout = true;
            }

            // Validate threading mode compatibility
            if (_config.ThreadingMode == PoolThreadingMode.JobCompatible && !_config.UseSafetyChecks)
            {
                _config.UseSafetyChecks = true; // Force safety checks for job compatibility
            }

            // Validate allocator if specified
            if (_config.NativeAllocator != Allocator.Invalid)
            {
                ValidateAllocator(_config.NativeAllocator);
            }

            // Validate capacity settings
            if (_config.InitialCapacity < 0)
            {
                throw new InvalidOperationException("Initial capacity cannot be negative");
            }

            if (_config.MaximumCapacity > 0 && _config.InitialCapacity > _config.MaximumCapacity)
            {
                throw new InvalidOperationException("Initial capacity cannot exceed maximum size");
            }

            // Validate growth settings
            if (_config.UseExponentialGrowth && _config.GrowthFactor <= 1.0f)
            {
                throw new InvalidOperationException(
                    "Growth factor must be greater than 1.0 when using exponential growth");
            }
        }
    }
}