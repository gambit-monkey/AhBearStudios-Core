using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for job-compatible pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for Unity Jobs System compatibility.
    /// </summary>
    public class
        JobCompatiblePoolConfigBuilder : IPoolConfigBuilder<JobCompatiblePoolConfig, JobCompatiblePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly JobCompatiblePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings optimized for Jobs
        /// </summary>
        public JobCompatiblePoolConfigBuilder()
        {
            _config = new JobCompatiblePoolConfig();
        }

        /// <summary>
        /// Creates a new builder initialized with values from an existing configuration
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public JobCompatiblePoolConfigBuilder(JobCompatiblePoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _config = config.Clone() as JobCompatiblePoolConfig;
        }

        /// <summary>
        /// Creates a new builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        public JobCompatiblePoolConfigBuilder(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");

            _config = new JobCompatiblePoolConfig(initialCapacity);
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor when expanding the pool (for exponential growth)
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Math.Max(1.0f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool (for linear growth)
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithGrowthIncrement(int growthIncrement)
        {
            _config.GrowthIncrement = Math.Max(1, growthIncrement);
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage drops
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithShrinkThreshold(float shrinkThreshold)
        {
            _config.ShrinkThreshold = Math.Clamp(shrinkThreshold, 0.0f, 1.0f);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithShrinkInterval(float shrinkInterval)
        {
            _config.ShrinkInterval = Math.Max(0.0f, shrinkInterval);
            return this;
        }

        /// <summary>
        /// Sets the threading mode for this pool
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for the pool (alias for WithAllocator)
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            return WithAllocator(allocator);
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to call Reset() on objects when they are released
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when attempting to get an object that would exceed the maximum pool size
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically dispose native collections on release
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithAutoDisposeOnRelease(bool autoDispose)
        {
            _config.AutoDisposeOnRelease = autoDispose;
            return this;
        }

        /// <summary>
        /// Sets whether to verify the allocator is valid before operations
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithAllocatorVerification(bool verifyAllocator)
        {
            _config.VerifyAllocator = verifyAllocator;
            return this;
        }

        /// <summary>
        /// Sets whether to synchronize job completion after operations
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithSyncAfterOperations(bool syncAfterOps)
        {
            _config.SyncAfterOps = syncAfterOps;
            return this;
        }

        /// <summary>
        /// Sets whether to use job safety handles in native containers
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithJobSafetyHandles(bool useJobSafetyHandles)
        {
            _config.UseJobSafetyHandles = useJobSafetyHandles;
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for collections
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether to use safety checks in native containers
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithSafetyChecks(bool useSafetyChecks)
        {
            _config.UseSafetyChecks = useSafetyChecks;
            return this;
        }

        /// <summary>
        /// Sets whether to use Burst-compatible collections
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithBurstCompatibleCollections(bool useBurstCompatible)
        {
            _config.UseBurstCompatibleCollections = useBurstCompatible;
            return this;
        }

        /// <summary>
        /// Sets whether to enable parallel access from multiple jobs
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithParallelJobAccess(bool enableParallelAccess)
        {
            _config.EnableParallelJobAccess = enableParallelAccess;
            return this;
        }

        /// <summary>
        /// Sets the job scheduling mode for operations
        /// </summary>
        public JobCompatiblePoolConfigBuilder WithSchedulingMode(JobSchedulingMode mode)
        {
            _config.SchedulingMode = mode;
            return this;
        }
        
        /// <summary>
        /// Sets the job batch size for processing items in parallel jobs.
        /// </summary>
        /// <param name="batchSize">The batch size (must be at least 1)</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithJobBatchSize(int batchSize)
        {
            _config.JobBatchSize = Math.Max(1, batchSize);
            return this;
        }

        /// <summary>
        /// Sets whether to use SIMD alignment for better vectorized math performance.
        /// </summary>
        /// <param name="useSIMDAlignment">Whether to align memory for SIMD operations</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithSIMDAlignment(bool useSIMDAlignment)
        {
            _config.UseSIMDAlignment = useSIMDAlignment;
            return this;
        }

        /// <summary>
        /// Sets the float precision for job operations.
        /// </summary>
        /// <param name="precision">The float precision level</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithFloatPrecision(FloatPrecision precision)
        {
            _config.FloatPrecision = precision;
            return this;
        }

        /// <summary>
        /// Sets the float mode for job operations.
        /// </summary>
        /// <param name="mode">The float mode</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithFloatMode(FloatMode mode)
        {
            _config.FloatMode = mode;
            return this;
        }

        /// <summary>
        /// Sets whether to use the temporary allocator for short-lived job resources.
        /// </summary>
        /// <param name="useTempAllocator">Whether to use the temp allocator</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithTempAllocator(bool useTempAllocator)
        {
            _config.UseTempAllocator = useTempAllocator;
            return this;
        }

        /// <summary>
        /// Sets whether to enable exception support in jobs.
        /// </summary>
        /// <param name="supportExceptions">Whether to support exceptions</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithExceptionSupport(bool supportExceptions)
        {
            _config.SupportExceptions = supportExceptions;
            return this;
        }

        /// <summary>
        /// Sets whether to enable job dependency tracking.
        /// </summary>
        /// <param name="enableJobDependencies">Whether to enable job dependencies</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithJobDependencies(bool enableJobDependencies)
        {
            _config.EnableJobDependencies = enableJobDependencies;
            return this;
        }

        /// <summary>
        /// Sets whether to allow managed references in jobs (unsafe but sometimes necessary).
        /// </summary>
        /// <param name="allowManagedReferences">Whether to allow managed references</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithManagedReferences(bool allowManagedReferences)
        {
            _config.AllowManagedReferences = allowManagedReferences;
            return this;
        }

        /// <summary>
        /// Sets the minimum capacity the pool should maintain.
        /// </summary>
        /// <param name="minimumCapacity">The minimum capacity</param>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder WithMinimumCapacity(int minimumCapacity)
        {
            _config.MinimumCapacity = Math.Max(0, minimumCapacity);
            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public JobCompatiblePoolConfigBuilder FromExisting(IPoolConfig source)
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

            // Copy NativePoolConfig-specific properties if available
            if (source is NativePoolConfig nativeConfig)
            {
                _config.UseSafetyChecks = nativeConfig.UseSafetyChecks;
                _config.AutoDisposeOnRelease = nativeConfig.AutoDisposeOnRelease;
                _config.VerifyAllocator = nativeConfig.VerifyAllocator;
                _config.NativeConfigFlags = nativeConfig.NativeConfigFlags;
                _config.UseBurstCompatibleCollections = nativeConfig.UseBurstCompatibleCollections;
                _config.PoolType = nativeConfig.PoolType;
            }

            // Copy JobCompatiblePoolConfig-specific properties if available
            if (source is JobCompatiblePoolConfig jobConfig)
            {
                _config.SyncAfterOps = jobConfig.SyncAfterOps;
                _config.UseJobSafetyHandles = jobConfig.UseJobSafetyHandles;
                _config.EnableParallelJobAccess = jobConfig.EnableParallelJobAccess;
                _config.SchedulingMode = jobConfig.SchedulingMode;
                _config.AllowManagedReferences = jobConfig.AllowManagedReferences;
                _config.UseSIMDAlignment = jobConfig.UseSIMDAlignment;
                _config.JobBatchSize = jobConfig.JobBatchSize;
                _config.FloatPrecision = jobConfig.FloatPrecision;
                _config.FloatMode = jobConfig.FloatMode;
                _config.UseTempAllocator = jobConfig.UseTempAllocator;
                _config.SupportExceptions = jobConfig.SupportExceptions;
                _config.EnableJobDependencies = jobConfig.EnableJobDependencies;
            }

            return this;
        }

        /// <summary>
        /// Initializes this builder with values from an existing JobCompatiblePoolConfig.
        /// More specific than the general FromExisting method.
        /// </summary>
        /// <param name="source">The source configuration to copy properties from</param>
        /// <returns>This builder for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public JobCompatiblePoolConfigBuilder FromExisting(JobCompatiblePoolConfig source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source configuration cannot be null");

            // Copy JobCompatiblePoolConfig properties directly instead of replacing _config
            var sourceClone = source.Clone() as JobCompatiblePoolConfig;

            // Copy all properties individually
            _config.InitialCapacity = sourceClone.InitialCapacity;
            _config.MinimumCapacity = sourceClone.MinimumCapacity;
            _config.MaximumCapacity = sourceClone.MaximumCapacity;
            _config.PrewarmOnInit = sourceClone.PrewarmOnInit;
            _config.CollectMetrics = sourceClone.CollectMetrics;
            _config.DetailedLogging = sourceClone.DetailedLogging;
            _config.LogWarnings = sourceClone.LogWarnings;
            _config.ResetOnRelease = sourceClone.ResetOnRelease;
            _config.ThreadingMode = sourceClone.ThreadingMode;
            _config.EnableAutoShrink = sourceClone.EnableAutoShrink;
            _config.ShrinkThreshold = sourceClone.ShrinkThreshold;
            _config.ShrinkInterval = sourceClone.ShrinkInterval;
            _config.NativeAllocator = sourceClone.NativeAllocator;
            _config.UseExponentialGrowth = sourceClone.UseExponentialGrowth;
            _config.GrowthFactor = sourceClone.GrowthFactor;
            _config.GrowthIncrement = sourceClone.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = sourceClone.ThrowIfExceedingMaxCount;
            _config.UseSafetyChecks = sourceClone.UseSafetyChecks;
            _config.AutoDisposeOnRelease = sourceClone.AutoDisposeOnRelease;
            _config.VerifyAllocator = sourceClone.VerifyAllocator;
            _config.NativeConfigFlags = sourceClone.NativeConfigFlags;
            _config.UseBurstCompatibleCollections = sourceClone.UseBurstCompatibleCollections;
            _config.PoolType = sourceClone.PoolType;
            _config.SyncAfterOps = sourceClone.SyncAfterOps;
            _config.UseJobSafetyHandles = sourceClone.UseJobSafetyHandles;
            _config.EnableParallelJobAccess = sourceClone.EnableParallelJobAccess;
            _config.SchedulingMode = sourceClone.SchedulingMode;
            _config.AllowManagedReferences = sourceClone.AllowManagedReferences;
            _config.UseSIMDAlignment = sourceClone.UseSIMDAlignment;
            _config.JobBatchSize = sourceClone.JobBatchSize;
            _config.FloatPrecision = sourceClone.FloatPrecision;
            _config.FloatMode = sourceClone.FloatMode;
            _config.UseTempAllocator = sourceClone.UseTempAllocator;
            _config.SupportExceptions = sourceClone.SupportExceptions;
            _config.EnableJobDependencies = sourceClone.EnableJobDependencies;

            // Ensure we have a new ConfigId
            _config.ConfigId = Guid.NewGuid().ToString();

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance settings optimized for parallel job execution.
        /// Maximizes throughput for compute-intensive parallel operations with the Job System.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsParallelOptimized()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.UseJobSafetyHandles = true;
            _config.EnableParallelJobAccess = true;
            _config.SchedulingMode = JobSchedulingMode.Parallel;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug settings with comprehensive safety checks.
        /// Enables extensive diagnostics and validation for development environments.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsDebug()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = true;
            _config.LogWarnings = true;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.UseJobSafetyHandles = true;
            _config.EnableParallelJobAccess = false;
            _config.SchedulingMode = JobSchedulingMode.Sequential;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 5;

            return this;
        }

        /// <summary>
        /// Configures the builder for balanced settings between performance and safety.
        /// Provides a reasonable compromise suitable for most general-purpose job scenarios.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsBalanced()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.UseJobSafetyHandles = true;
            _config.EnableParallelJobAccess = false;
            _config.SchedulingMode = JobSchedulingMode.Batched;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 120f;

            return this;
        }

        /// <summary>
        /// Configures the builder for optimal Burst compiler compatibility.
        /// Optimizes settings for maximum performance with Unity's Burst compiler.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsBurstCompatible()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.VerifyAllocator = false;
            _config.AutoDisposeOnRelease = true;
            _config.UseJobSafetyHandles = true;
            _config.EnableParallelJobAccess = true;
            _config.SchedulingMode = JobSchedulingMode.Parallel;
            _config.EnableAutoShrink = false;
            _config.LogWarnings = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient settings.
        /// Optimizes for minimal memory usage with controlled growth and active shrinking.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsMemoryEfficient()
        {
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 30f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = true;
            _config.VerifyAllocator = true;
            _config.AutoDisposeOnRelease = true;
            _config.EnableParallelJobAccess = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for optimal sequential job execution.
        /// Optimized for cases where jobs must run in sequence rather than in parallel.
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public JobCompatiblePoolConfigBuilder AsSequentialOptimized()
        {
            _config.UseSafetyChecks = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseBurstCompatibleCollections = true;
            _config.SyncAfterOps = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.VerifyAllocator = false;
            _config.AutoDisposeOnRelease = true;
            _config.UseJobSafetyHandles = true;
            _config.EnableParallelJobAccess = false;
            _config.SchedulingMode = JobSchedulingMode.Sequential;
            _config.EnableAutoShrink = false;

            return this;
        }

        /// <summary>
        /// Builds and returns the configured pool configuration.
        /// </summary>
        /// <returns>The configured JobCompatiblePoolConfig</returns>
        public JobCompatiblePoolConfig Build()
        {
            // Validate the configuration before returning
            _config.Validate();
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

            if (_config.NativeAllocator <= Allocator.None)
                throw new InvalidOperationException("Invalid allocator specified");

            if (_config.EnableParallelJobAccess && !_config.UseSafetyChecks)
            {
                Debug.LogWarning("Enabling safety checks as they are required for parallel job access");
                _config.UseSafetyChecks = true;
            }

            if (_config.ThreadingMode != PoolThreadingMode.JobCompatible)
            {
                Debug.LogWarning("Forcing JobCompatible threading mode for job-compatible pool");
                _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            }
        }
    }
}