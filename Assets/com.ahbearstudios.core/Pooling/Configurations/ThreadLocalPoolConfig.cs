using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for thread-local object pools that maintain separate pools for each thread.
    /// Provides specialized settings for high-performance multi-threaded scenarios with minimal contention.
    /// Implements IPoolConfig through composition for better architectural design.
    /// </summary>
    [Serializable]
    public sealed class ThreadLocalPoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation - Core Configuration
        
        /// <summary>
        /// Gets or sets the unique identifier for this pool configuration.
        /// </summary>
        public string ConfigId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the initial capacity of each thread-local pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain, preventing
        /// shrinking below this threshold.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the maximum size of each thread-local pool. Set to 0 for unlimited.
        /// </summary>
        public int MaximumCapacity { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets whether to prewarm the thread-local pools on initialization.
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
        /// This will always be ThreadLocal for ThreadLocalPoolConfig.
        /// </summary>
        public PoolThreadingMode ThreadingMode 
        { 
            get => PoolThreadingMode.ThreadLocal;
            set { /* Always ThreadLocal, ignoring assignment */ } 
        }
        
        /// <summary>
        /// Gets or sets whether to automatically shrink the thread-local pools when usage drops.
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
        
        #region Thread-Local Specific Configuration
        
        /// <summary>
        /// Gets or sets the maximum number of threads that can have their own pools.
        /// </summary>
        public int MaxThreadCount { get; set; } = 16;
        
        /// <summary>
        /// Gets or sets whether thread-specific pools should be created on demand.
        /// </summary>
        public bool CreateThreadPoolsOnDemand { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to maintain a global fallback pool for thread-less contexts.
        /// </summary>
        public bool MaintainGlobalFallbackPool { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether the pool automatically detects and removes unused thread pools.
        /// </summary>
        public bool AutoCleanupUnusedThreadPools { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the minimum idle time in seconds before a thread pool is removed.
        /// </summary>
        public float ThreadPoolCleanupThreshold { get; set; } = 300f;
        
        /// <summary>
        /// Gets or sets whether to track which thread created each object.
        /// </summary>
        public bool TrackObjectThreadAffinity { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether released objects should return to their creator thread's pool.
        /// </summary>
        public bool ReturnToCreatorThreadPool { get; set; } = false;
        
        #endregion
        
        #region Constructors and Factory Methods
        
        /// <summary>
        /// Creates a new thread-local pool configuration with default settings.
        /// </summary>
        public ThreadLocalPoolConfig()
        {
            ConfigId = Guid.NewGuid().ToString("N"); // More efficient format for GUIDs
        }
        
        /// <summary>
        /// Creates a new thread-local pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of each thread-local pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ThreadLocalPoolConfig(int initialCapacity) : this()
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }
        
        /// <summary>
        /// Creates a new thread-local pool configuration with the specified initial and maximum sizes.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of each thread-local pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity or maxSize is negative</exception>
        public ThreadLocalPoolConfig(int initialCapacity, int maxSize) : this(initialCapacity)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be negative");
                
            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize), 
                    "Max size must be greater than or equal to initial capacity, or 0 for unlimited");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new thread-local pool configuration based on an existing pool configuration.
        /// </summary>
        /// <param name="baseConfig">The base configuration to extend</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null</exception>
        public ThreadLocalPoolConfig(IPoolConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
            
            // Copy base IPoolConfig properties
            ConfigId = baseConfig.ConfigId;
            InitialCapacity = baseConfig.InitialCapacity;
            MaximumCapacity = baseConfig.MaximumCapacity;
            MinimumCapacity = baseConfig.MinimumCapacity;
            PrewarmOnInit = baseConfig.PrewarmOnInit;
            CollectMetrics = baseConfig.CollectMetrics;
            DetailedLogging = baseConfig.DetailedLogging;
            LogWarnings = baseConfig.LogWarnings;
            ResetOnRelease = baseConfig.ResetOnRelease;
            // Thread mode is always ThreadLocal regardless of source
            EnableAutoShrink = baseConfig.EnableAutoShrink;
            ShrinkThreshold = baseConfig.ShrinkThreshold;
            ShrinkInterval = baseConfig.ShrinkInterval;
            NativeAllocator = baseConfig.NativeAllocator;
            UseExponentialGrowth = baseConfig.UseExponentialGrowth;
            GrowthFactor = baseConfig.GrowthFactor;
            GrowthIncrement = baseConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = baseConfig.ThrowIfExceedingMaxCount;
            
            // If the source is already a ThreadLocalPoolConfig, copy thread-local specific properties
            if (baseConfig is ThreadLocalPoolConfig threadLocalConfig)
            {
                MaxThreadCount = threadLocalConfig.MaxThreadCount;
                CreateThreadPoolsOnDemand = threadLocalConfig.CreateThreadPoolsOnDemand;
                MaintainGlobalFallbackPool = threadLocalConfig.MaintainGlobalFallbackPool;
                AutoCleanupUnusedThreadPools = threadLocalConfig.AutoCleanupUnusedThreadPools;
                ThreadPoolCleanupThreshold = threadLocalConfig.ThreadPoolCleanupThreshold;
                TrackObjectThreadAffinity = threadLocalConfig.TrackObjectThreadAffinity;
                ReturnToCreatorThreadPool = threadLocalConfig.ReturnToCreatorThreadPool;
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new ThreadLocalPoolConfig instance with the same settings</returns>
        public IPoolConfig Clone()
        {
            // Create a new instance
            var clone = new ThreadLocalPoolConfig
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
                // ThreadingMode is readonly for ThreadLocalPoolConfig
                EnableAutoShrink = EnableAutoShrink,
                ShrinkThreshold = ShrinkThreshold,
                ShrinkInterval = ShrinkInterval,
                NativeAllocator = NativeAllocator,
                UseExponentialGrowth = UseExponentialGrowth,
                GrowthFactor = GrowthFactor,
                GrowthIncrement = GrowthIncrement,
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount,
                
                // Copy thread-local specific properties
                MaxThreadCount = MaxThreadCount,
                CreateThreadPoolsOnDemand = CreateThreadPoolsOnDemand,
                MaintainGlobalFallbackPool = MaintainGlobalFallbackPool,
                AutoCleanupUnusedThreadPools = AutoCleanupUnusedThreadPools,
                ThreadPoolCleanupThreshold = ThreadPoolCleanupThreshold,
                TrackObjectThreadAffinity = TrackObjectThreadAffinity,
                ReturnToCreatorThreadPool = ReturnToCreatorThreadPool
            };
            
            return clone;
        }
        
        /// <summary>
        /// Creates a builder initialized with this configuration.
        /// </summary>
        /// <returns>A new builder initialized with this configuration</returns>
        public ThreadLocalPoolConfigBuilder ToBuilder()
        {
            return new ThreadLocalPoolConfigBuilder().FromExisting(this);
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
                
            if (MaxThreadCount <= 0)
                throw new InvalidOperationException("Maximum thread count must be positive");
                
            if (ThreadPoolCleanupThreshold < 0)
                throw new InvalidOperationException("Thread pool cleanup threshold cannot be negative");
                
            if (ReturnToCreatorThreadPool && !TrackObjectThreadAffinity)
                throw new InvalidOperationException("Must enable thread affinity tracking when returning to creator thread pool");
        }
        
        /// <summary>
        /// Returns a string representation of this configuration.
        /// </summary>
        /// <returns>A string representation of the configuration</returns>
        public override string ToString()
        {
            return $"ThreadLocalPoolConfig[Id={ConfigId}, InitCap={InitialCapacity}, MaxCap={MaximumCapacity}, MaxThreads={MaxThreadCount}]";
        }
        
        #endregion
    }
}