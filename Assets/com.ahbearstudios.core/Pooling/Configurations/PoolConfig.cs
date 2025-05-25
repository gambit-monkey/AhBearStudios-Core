using System;
using AhBearStudios.Core.Pooling.Builders;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Standard implementation of configuration options for object pools.
    /// Provides a robust implementation of IPoolConfig with support for registry integration.
    /// </summary>
    public class PoolConfig : IPoolConfig
    {
        /// <inheritdoc />
        public int InitialCapacity { get; set; } = 10;
        
        /// <inheritdoc />
        public int MinimumCapacity { get; set; }

        /// <inheritdoc />
        public int MaximumCapacity { get; set; } = 0;
        
        /// <inheritdoc />
        public bool PrewarmOnInit { get; set; } = true;
        
        /// <inheritdoc />
        public bool UseExponentialGrowth { get; set; } = true;
        
        /// <inheritdoc />
        public float GrowthFactor { get; set; } = 2.0f;
        
        /// <inheritdoc />
        public int GrowthIncrement { get; set; } = 10;
        
        /// <inheritdoc />
        public bool EnableAutoShrink { get; set; } = false;
        
        /// <inheritdoc />
        public float ShrinkThreshold { get; set; } = 0.25f;
        
        /// <inheritdoc />
        public float ShrinkInterval { get; set; } = 60.0f;
        
        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;
        
        /// <inheritdoc />
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;
        
        /// <inheritdoc />
        public bool LogWarnings { get; set; } = true;
        
        /// <inheritdoc />
        public bool CollectMetrics { get; set; } = true;
        
        /// <inheritdoc />
        public bool DetailedLogging { get; set; } = false;
        
        /// <inheritdoc />
        public bool ResetOnRelease { get; set; } = true;
        
        /// <inheritdoc />
        public bool ThrowIfExceedingMaxCount { get; set; } = false;
        
        /// <inheritdoc />
        public string ConfigId { get; set; } = string.Empty;
        
        /// <inheritdoc />
        public Type PoolType => typeof(IPool);
        
        /// <summary>
        /// Creates a default pool configuration
        /// </summary>
        public PoolConfig() 
        {
            ConfigId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Creates a pool configuration with the specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create</param>
        public PoolConfig(int initialCapacity) : this()
        {
            InitialCapacity = initialCapacity;
        }
        
        /// <summary>
        /// Creates a pool configuration with the specified initial and maximum sizes
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create</param>
        /// <param name="maximumCapacity">Maximum pool size (0 for unlimited)</param>
        public PoolConfig(int initialCapacity, int maximumCapacity) : this(initialCapacity)
        {
            MaximumCapacity = maximumCapacity;
        }
        
        /// <summary>
        /// Creates a pool configuration with a custom config ID
        /// </summary>
        /// <param name="configId">The configuration ID to use</param>
        public PoolConfig(string configId) : this()
        {
            if (!string.IsNullOrEmpty(configId))
            {
                ConfigId = configId;
            }
        }
        
        /// <summary>
        /// Creates a pool configuration by copying settings from another IPoolConfig
        /// </summary>
        /// <param name="source">The source configuration to copy settings from</param>
        public PoolConfig(IPoolConfig source) : this()
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "Source configuration cannot be null");
            }
            
            InitialCapacity = source.InitialCapacity;
            MinimumCapacity = source.MinimumCapacity;
            MaximumCapacity = source.MaximumCapacity;
            PrewarmOnInit = source.PrewarmOnInit;
            UseExponentialGrowth = source.UseExponentialGrowth;
            GrowthFactor = source.GrowthFactor;
            GrowthIncrement = source.GrowthIncrement;
            EnableAutoShrink = source.EnableAutoShrink;
            ShrinkThreshold = source.ShrinkThreshold;
            ShrinkInterval = source.ShrinkInterval;
            ThreadingMode = source.ThreadingMode;
            NativeAllocator = source.NativeAllocator;
            LogWarnings = source.LogWarnings;
            CollectMetrics = source.CollectMetrics;
            DetailedLogging = source.DetailedLogging;
            ResetOnRelease = source.ResetOnRelease;
            ThrowIfExceedingMaxCount = source.ThrowIfExceedingMaxCount;
            
            // Copy config ID if available
            if (source.ConfigId != null)
            {
                ConfigId = source.ConfigId;
            }
        }
        
        /// <inheritdoc />
        public virtual IPoolConfig Clone()
        {
            return new PoolConfig
            {
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
                ConfigId = this.ConfigId
            };
        }
        
        /// <inheritdoc />
        public void Register(IPoolConfigRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            registry.RegisterConfig(ConfigId, this);
        }
        
        /// <summary>
        /// Creates a builder for this configuration using the factory
        /// </summary>
        /// <returns>A new builder instance from the factory</returns>
        public static PoolConfigBuilder CreateBuilder()
        {
            return PoolConfigBuilderFactory.Standard();
        }
        
        /// <summary>
        /// Creates a builder initialized with this instance's values
        /// </summary>
        /// <returns>A builder initialized with this configuration's values</returns>
        public PoolConfigBuilder ToBuilder()
        {
            return PoolConfigBuilderFactory.FromExistingConfig(this);
        }
    }
}