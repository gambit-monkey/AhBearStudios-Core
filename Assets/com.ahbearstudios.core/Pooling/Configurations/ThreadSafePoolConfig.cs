using System;
using AhBearStudios.Pooling.Builders;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Configuration for thread-safe object pools that can be accessed from multiple threads.
    /// Provides specialized settings for high-concurrency scenarios with synchronized access.
    /// Implements <see cref="IPoolConfig"/> through composition for better architectural design.
    /// </summary>
    [Serializable]
    public sealed class ThreadSafePoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation - Core Configuration
        
        /// <summary>
        /// Gets or sets the unique identifier for this pool configuration.
        /// </summary>
        public string ConfigId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain, preventing
        /// shrinking below this threshold.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the maximum size of the pool (0 for unlimited).
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
        /// This will always be ThreadSafe for ThreadSafePoolConfig.
        /// </summary>
        public PoolThreadingMode ThreadingMode 
        { 
            get => PoolThreadingMode.ThreadSafe;
            set { /* Always ThreadSafe, ignoring assignment */ } 
        }
        
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
        
        #region Thread-Safe Specific Configuration
        
        /// <summary>
        /// Gets or sets the maximum concurrent operation count before issuing a warning.
        /// </summary>
        public int ConcurrencyWarningThreshold { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets whether to track performance metrics of pool operations.
        /// </summary>
        public bool TrackOperationPerformance { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use concurrent collections for improved throughput.
        /// </summary>
        public bool UseConcurrentCollections { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to track object acquisition times for monitoring.
        /// </summary>
        public bool TrackObjectLifetimes { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to prefer lock-free algorithms where possible.
        /// </summary>
        public bool PreferLockFreeAlgorithms { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the maximum lock wait time in milliseconds before timing out.
        /// </summary>
        public int LockTimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// Gets or sets the number of retry attempts for concurrent operations.
        /// </summary>
        public int OperationRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets whether to use SpinLock instead of regular Monitor locks
        /// for potentially improved performance in high-contention scenarios.
        /// </summary>
        public bool UseSpinLocks { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the spin count (number of spin iterations) before yielding the thread.
        /// Only applicable when UseSpinLocks is true.
        /// </summary>
        public int SpinCount { get; set; } = 35;
        
        /// <summary>
        /// Gets or sets whether to optimize for bursts of concurrent access.
        /// </summary>
        public bool OptimizeForBurstAccess { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use a dedicated lock object per operation type
        /// to reduce contention versus using a single global lock.
        /// </summary>
        public bool UseSeparateLocks { get; set; } = true;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new thread-safe pool configuration with default settings.
        /// </summary>
        public ThreadSafePoolConfig()
        {
            ConfigId = Guid.NewGuid().ToString("N"); // More efficient format for GUIDs
        }
        
        /// <summary>
        /// Creates a new thread-safe pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ThreadSafePoolConfig(int initialCapacity) : this()
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }
        
        /// <summary>
        /// Creates a new thread-safe pool configuration with the specified initial and maximum sizes.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than initialCapacity and not 0</exception>
        public ThreadSafePoolConfig(int initialCapacity, int maxSize) : this(initialCapacity)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be negative");
                
            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize), 
                    "Max size must be greater than or equal to initial capacity, or 0 for unlimited");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new thread-safe pool configuration based on an existing pool configuration.
        /// </summary>
        /// <param name="baseConfig">The base configuration to extend</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null</exception>
        public ThreadSafePoolConfig(IPoolConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
            
            // Copy base IPoolConfig properties
            ConfigId = baseConfig.ConfigId;
            InitialCapacity = baseConfig.InitialCapacity;
            MinimumCapacity = baseConfig.MinimumCapacity;
            MaximumCapacity = baseConfig.MaximumCapacity;
            PrewarmOnInit = baseConfig.PrewarmOnInit;
            CollectMetrics = baseConfig.CollectMetrics;
            DetailedLogging = baseConfig.DetailedLogging;
            LogWarnings = baseConfig.LogWarnings;
            ResetOnRelease = baseConfig.ResetOnRelease;
            // Thread mode is always ThreadSafe regardless of source
            EnableAutoShrink = baseConfig.EnableAutoShrink;
            ShrinkThreshold = baseConfig.ShrinkThreshold;
            ShrinkInterval = baseConfig.ShrinkInterval;
            NativeAllocator = baseConfig.NativeAllocator;
            UseExponentialGrowth = baseConfig.UseExponentialGrowth;
            GrowthFactor = baseConfig.GrowthFactor;
            GrowthIncrement = baseConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = baseConfig.ThrowIfExceedingMaxCount;
            
            // If the source is already a ThreadSafePoolConfig, copy thread-safe specific properties
            if (baseConfig is ThreadSafePoolConfig threadSafeConfig)
            {
                ConcurrencyWarningThreshold = threadSafeConfig.ConcurrencyWarningThreshold;
                TrackOperationPerformance = threadSafeConfig.TrackOperationPerformance;
                UseConcurrentCollections = threadSafeConfig.UseConcurrentCollections;
                TrackObjectLifetimes = threadSafeConfig.TrackObjectLifetimes;
                PreferLockFreeAlgorithms = threadSafeConfig.PreferLockFreeAlgorithms;
                LockTimeoutMs = threadSafeConfig.LockTimeoutMs;
                OperationRetryCount = threadSafeConfig.OperationRetryCount;
                UseSpinLocks = threadSafeConfig.UseSpinLocks;
                SpinCount = threadSafeConfig.SpinCount;
                OptimizeForBurstAccess = threadSafeConfig.OptimizeForBurstAccess;
                UseSeparateLocks = threadSafeConfig.UseSeparateLocks;
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new ThreadSafePoolConfig instance with the same settings</returns>
        public IPoolConfig Clone()
        {
            // Create a new instance
            var clone = new ThreadSafePoolConfig
            {
                // Copy IPoolConfig properties
                ConfigId = ConfigId,
                InitialCapacity = InitialCapacity,
                MinimumCapacity = MinimumCapacity,
                MaximumCapacity = MaximumCapacity,
                PrewarmOnInit = PrewarmOnInit,
                CollectMetrics = CollectMetrics,
                DetailedLogging = DetailedLogging,
                LogWarnings = LogWarnings,
                ResetOnRelease = ResetOnRelease,
                // ThreadingMode is readonly for ThreadSafePoolConfig
                EnableAutoShrink = EnableAutoShrink,
                ShrinkThreshold = ShrinkThreshold,
                ShrinkInterval = ShrinkInterval,
                NativeAllocator = NativeAllocator,
                UseExponentialGrowth = UseExponentialGrowth,
                GrowthFactor = GrowthFactor,
                GrowthIncrement = GrowthIncrement,
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount,
                
                // Copy thread-safe specific properties
                ConcurrencyWarningThreshold = ConcurrencyWarningThreshold,
                TrackOperationPerformance = TrackOperationPerformance,
                UseConcurrentCollections = UseConcurrentCollections,
                TrackObjectLifetimes = TrackObjectLifetimes,
                PreferLockFreeAlgorithms = PreferLockFreeAlgorithms,
                LockTimeoutMs = LockTimeoutMs,
                OperationRetryCount = OperationRetryCount,
                UseSpinLocks = UseSpinLocks,
                SpinCount = SpinCount,
                OptimizeForBurstAccess = OptimizeForBurstAccess,
                UseSeparateLocks = UseSeparateLocks
            };
            
            return clone;
        }
        
        /// <summary>
        /// Creates a builder initialized with this configuration.
        /// </summary>
        /// <returns>A new builder initialized with this configuration</returns>
        public ThreadSafePoolConfigBuilder ToBuilder()
        {
            return new ThreadSafePoolConfigBuilder().FromExisting(this);
        }
        
        /// <summary>
        /// Validates the configuration for correctness.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
        public void Validate()
        {
            if (InitialCapacity < 0)
                throw new InvalidOperationException("Initial capacity cannot be negative");

            if (MaximumCapacity > 0 && InitialCapacity > MaximumCapacity)
                throw new InvalidOperationException("Initial capacity cannot exceed maximum size");
                
            if (MinimumCapacity > InitialCapacity)
                throw new InvalidOperationException("Minimum capacity cannot exceed initial capacity");
                
            if (ConcurrencyWarningThreshold <= 0)
                throw new InvalidOperationException("Concurrency warning threshold must be positive");
                
            if (LockTimeoutMs < 0)
                throw new InvalidOperationException("Lock timeout cannot be negative");
                
            if (OperationRetryCount < 0)
                throw new InvalidOperationException("Operation retry count cannot be negative");
                
            if (SpinCount < 0)
                throw new InvalidOperationException("Spin count cannot be negative");
        }
        
        /// <summary>
        /// Optimizes the configuration for high concurrency scenarios.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ThreadSafePoolConfig OptimizeForHighConcurrency()
        {
            UseConcurrentCollections = true;
            PreferLockFreeAlgorithms = true;
            UseSpinLocks = true;
            SpinCount = 50;
            UseSeparateLocks = true;
            OperationRetryCount = 5;
            ConcurrencyWarningThreshold = 500;
            OptimizeForBurstAccess = true;
            
            return this;
        }
        
        /// <summary>
        /// Optimizes the configuration for debugging and diagnostics.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ThreadSafePoolConfig OptimizeForDebugging()
        {
            CollectMetrics = true;
            DetailedLogging = true;
            LogWarnings = true;
            TrackOperationPerformance = true;
            TrackObjectLifetimes = true;
            ConcurrencyWarningThreshold = 20;
            UseConcurrentCollections = true;
            
            return this;
        }
        
        /// <summary>
        /// Optimizes the configuration for memory efficiency.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ThreadSafePoolConfig OptimizeForMemory()
        {
            EnableAutoShrink = true;
            ShrinkThreshold = 0.4f;
            ShrinkInterval = 30f;
            MinimumCapacity = Math.Max(1, InitialCapacity / 4);
            UseExponentialGrowth = false;
            GrowthIncrement = Math.Max(1, InitialCapacity / 5);
            TrackOperationPerformance = false;
            TrackObjectLifetimes = false;
            CollectMetrics = false;
            
            return this;
        }
        
        /// <summary>
        /// Returns a string representation of this configuration.
        /// </summary>
        /// <returns>A string representation of the configuration</returns>
        public override string ToString()
        {
            return $"ThreadSafePoolConfig[Id={ConfigId}, InitCap={InitialCapacity}, MaxCap={MaximumCapacity}, UseConcurrent={UseConcurrentCollections}]";
        }
        
        #endregion
    }
}