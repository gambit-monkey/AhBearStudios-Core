using System;
using AhBearStudios.Core.Pooling.Builders;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for asynchronous object pools with thread-safe operations and
    /// asynchronous processing capabilities. Designed for high-performance Unity applications
    /// with full Unity Collections v2 compatibility.
    /// </summary>
    public sealed class AsyncObjectPoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation

        /// <summary>
        /// Gets or sets the unique identifier for this pool configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString("N");
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 32;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain, preventing
        /// shrinking below this threshold.
        /// </summary>
        public int MinimumCapacity { get; set; } = 16;
        
        /// <summary>
        /// Gets or sets the maximum size of the pool (0 for unlimited).
        /// </summary>
        public int MaximumCapacity { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets whether to prewarm the pool on initialization.
        /// </summary>
        public bool PrewarmOnInit { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to collect metrics for this pool.
        /// </summary>
        public bool CollectMetrics { get; set; } = false;
        
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
        /// Gets or sets the threading mode for this pool.
        /// For async pools, this is always ThreadSafe.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;
        
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
        public float ShrinkInterval { get; set; } = 60.0f;
        
        /// <summary>
        /// Gets or sets the native allocator to use for native pools.
        /// </summary>
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;
        
        /// <summary>
        /// Gets or sets whether to use exponential growth when expanding the pool.
        /// </summary>
        public bool UseExponentialGrowth { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        public float GrowthFactor { get; set; } = 1.5f;
        
        /// <summary>
        /// Gets or sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        public int GrowthIncrement { get; set; } = 16;
        
        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        public bool ThrowIfExceedingMaxCount { get; set; } = false;

        #endregion

        #region Async-Specific Configuration

        /// <summary>
        /// Gets or sets the timeout in milliseconds for acquire operations.
        /// </summary>
        public int AcquireTimeoutMs { get; set; } = 1000;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent asynchronous operations.
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 4;
        
        /// <summary>
        /// Gets or sets whether to use background threads for pool initialization.
        /// </summary>
        public bool UseBackgroundInitialization { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to use background threads for pool cleanup.
        /// </summary>
        public bool UseBackgroundCleanup { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use an operation queue for managing async requests.
        /// </summary>
        public bool UseOperationQueue { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the capacity of the operation queue.
        /// </summary>
        public int OperationQueueCapacity { get; set; } = 32;
        
        /// <summary>
        /// Gets or sets whether to use batch processing for operations.
        /// </summary>
        public bool UseBatchProcessing { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the size of batches for batch processing.
        /// </summary>
        public int BatchSize { get; set; } = 8;
        
        /// <summary>
        /// Gets or sets whether to cancel pending operations when the pool is disposed.
        /// </summary>
        public bool CancelPendingOnDispose { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the timeout in milliseconds for cancellation operations.
        /// </summary>
        public int CancellationTimeoutMs { get; set; } = 500;
        
        /// <summary>
        /// Gets or sets whether to throw exceptions for asynchronous operation failures.
        /// </summary>
        public bool ThrowOnAsyncFailure { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use priority-based processing for operations.
        /// </summary>
        public bool UsePriorityProcessing { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to report detailed progress of asynchronous operations.
        /// </summary>
        public bool ReportDetailedProgress { get; set; } = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPoolConfig"/> class with default settings.
        /// </summary>
        public AsyncObjectPoolConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPoolConfig"/> class with specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity for the pool.</param>
        public AsyncObjectPoolConfig(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPoolConfig"/> class 
        /// with specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity for the pool.</param>
        /// <param name="maxSize">The maximum size for the pool.</param>
        public AsyncObjectPoolConfig(int initialCapacity, int maxSize) 
            : this(initialCapacity)
        {
            MaximumCapacity = maxSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPoolConfig"/> class with specified configuration ID.
        /// </summary>
        /// <param name="configId">The configuration ID.</param>
        public AsyncObjectPoolConfig(string configId)
        {
            ConfigId = configId ?? throw new ArgumentNullException(nameof(configId));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPoolConfig"/> class from an existing configuration.
        /// </summary>
        /// <param name="source">The source configuration to copy from.</param>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        public AsyncObjectPoolConfig(IPoolConfig source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ConfigId = source.ConfigId;
            InitialCapacity = source.InitialCapacity;
            MinimumCapacity = source.MinimumCapacity;
            MaximumCapacity = source.MaximumCapacity;
            PrewarmOnInit = source.PrewarmOnInit;
            CollectMetrics = source.CollectMetrics;
            DetailedLogging = source.DetailedLogging;
            LogWarnings = source.LogWarnings;
            ResetOnRelease = source.ResetOnRelease;
            ThreadingMode = PoolThreadingMode.ThreadSafe; // Always thread-safe for async pools
            EnableAutoShrink = source.EnableAutoShrink;
            ShrinkThreshold = source.ShrinkThreshold;
            ShrinkInterval = source.ShrinkInterval;
            NativeAllocator = source.NativeAllocator;
            UseExponentialGrowth = source.UseExponentialGrowth;
            GrowthFactor = source.GrowthFactor;
            GrowthIncrement = source.GrowthIncrement;
            ThrowIfExceedingMaxCount = source.ThrowIfExceedingMaxCount;

            // Copy async-specific properties if source is an AsyncObjectPoolConfig
            if (source is AsyncObjectPoolConfig asyncConfig)
            {
                AcquireTimeoutMs = asyncConfig.AcquireTimeoutMs;
                MaxConcurrentOperations = asyncConfig.MaxConcurrentOperations;
                UseBackgroundInitialization = asyncConfig.UseBackgroundInitialization;
                UseBackgroundCleanup = asyncConfig.UseBackgroundCleanup;
                UseOperationQueue = asyncConfig.UseOperationQueue;
                OperationQueueCapacity = asyncConfig.OperationQueueCapacity;
                UseBatchProcessing = asyncConfig.UseBatchProcessing;
                BatchSize = asyncConfig.BatchSize;
                CancelPendingOnDispose = asyncConfig.CancelPendingOnDispose;
                CancellationTimeoutMs = asyncConfig.CancellationTimeoutMs;
                ThrowOnAsyncFailure = asyncConfig.ThrowOnAsyncFailure;
                UsePriorityProcessing = asyncConfig.UsePriorityProcessing;
                ReportDetailedProgress = asyncConfig.ReportDetailedProgress;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the configuration settings and ensures they meet the requirements for an async pool.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool Validate()
        {
            // Always ensure thread-safe mode is set for async pools
            ThreadingMode = PoolThreadingMode.ThreadSafe;

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

            // Validate growth settings
            if (GrowthFactor < 1.1f)
                GrowthFactor = 1.1f;

            if (GrowthIncrement < 1)
                GrowthIncrement = 1;

            // Validate async-specific settings
            if (AcquireTimeoutMs < 0)
                AcquireTimeoutMs = 0;

            if (CancellationTimeoutMs < 0)
                CancellationTimeoutMs = 0;

            if (MaxConcurrentOperations < 1)
                MaxConcurrentOperations = 1;

            if (OperationQueueCapacity < 1)
                OperationQueueCapacity = 1;

            if (BatchSize < 1)
                BatchSize = 1;

            // Validate shrink settings
            if (ShrinkThreshold < 0.1f)
                ShrinkThreshold = 0.1f;
            else if (ShrinkThreshold > 0.9f)
                ShrinkThreshold = 0.9f;

            if (ShrinkInterval < 0)
                ShrinkInterval = 0;

            return true;
        }

        /// <summary>
        /// Creates a deep copy of the configuration.
        /// </summary>
        /// <returns>A new instance of the configuration with the same settings.</returns>
        public IPoolConfig Clone()
        {
            return new AsyncObjectPoolConfig(this);
        }

        /// <summary>
        /// Registers this configuration with a pool configuration registry.
        /// </summary>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown when registry is null.</exception>
        public void Register(IPoolConfigRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            registry.RegisterConfig(ConfigId, this);
        }

        /// <summary>
        /// Creates a builder for configuring this pool configuration using the fluent pattern.
        /// </summary>
        /// <returns>A new builder instance for this configuration.</returns>
        public AsyncObjectPoolConfigBuilder CreateBuilder()
        {
            return new AsyncObjectPoolConfigBuilder().FromExisting(this);
        }

        /// <summary>
        /// Creates a builder initialized with this configuration's settings.
        /// </summary>
        /// <returns>A new builder instance for modifying this configuration.</returns>
        public AsyncObjectPoolConfigBuilder ToBuilder()
        {
            return CreateBuilder();
        }

        #endregion
    }
}