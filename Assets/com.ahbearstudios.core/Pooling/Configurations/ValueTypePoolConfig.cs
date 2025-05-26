using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for pools that manage value types (structs) with optimal memory layout and performance.
    /// Specifically designed to minimize boxing, allocations, and cache misses when working with value types.
    /// Implements <see cref="IPoolConfig"/> through composition for better architectural design.
    /// </summary>
    [Serializable]
    public sealed class ValueTypePoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation - Core Configuration
        
        /// <summary>
        /// Gets or sets the unique identifier for this pool configuration.
        /// </summary>
        public string ConfigId { get; set; } = string.Empty;
        
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
        public int MaximumCapacity { get; set; } = 1024;
        
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
        /// For value types, this may have minimal effect if implemented with interfaces.
        /// </summary>
        public bool ResetOnRelease { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the threading mode for the pool.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;
        
        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.5f;
        
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
        public int GrowthIncrement { get; set; } = 32;
        
        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        public bool ThrowIfExceedingMaxCount { get; set; } = false;
        
        #endregion
        
        #region Value Type Specific Configuration
        /// <summary>
        /// Gets or sets whether to align memory for better cache efficiency.
        /// </summary>
        public bool UseMemoryAlignment { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the memory alignment size in bytes (should be a power of 2).
        /// </summary>
        public int AlignmentSizeBytes { get; set; } = 16;
        
        /// <summary>
        /// Gets or sets whether to clear memory on release.
        /// </summary>
        public bool ClearMemoryOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to support blittable types only.
        /// Blittable types are more efficient for native memory operations.
        /// </summary>
        public bool BlittableTypesOnly { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to optimize for SIMD operations.
        /// </summary>
        public bool UseSIMDAlignment { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use direct memory access for performance.
        /// </summary>
        public bool UseDirectMemoryAccess { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use specialized Burst-compatible allocation.
        /// </summary>
        public bool UseBurstAllocation { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to skip default initialization of pooled value types.
        /// </summary>
        public bool SkipDefaultInitialization { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the structural layout type for memory organization.
        /// </summary>
        public StructLayoutType StructLayoutType { get; set; } = StructLayoutType.Sequential;
        
        /// <summary>
        /// Gets or sets whether to use inline handling for elements.
        /// </summary>
        public bool UseInlineHandling { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use validation checks during operations.
        /// </summary>
        public bool UseValidationChecks { get; set; } = true;
        
        /// <summary>
        /// Gets or sets how the pool handles overflow situations.
        /// </summary>
        public OverflowHandlingType OverflowHandling { get; set; } = OverflowHandlingType.ThrowException;
        
        /// <summary>
        /// Gets or sets whether the pool should be automatically disposed.
        /// </summary>
        public bool UseAutomaticDisposal { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the timeout for automatic disposal in seconds.
        /// </summary>
        public float DisposeTimeoutSeconds { get; set; } = 60f;
        
        /// <summary>
        /// Gets or sets whether the pool is compatible with Unity Jobs system.
        /// </summary>
        public bool IsJobCompatible { get; set; } = false;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new value type pool configuration with default settings.
        /// </summary>
        public ValueTypePoolConfig()
        {
            ConfigId = Guid.NewGuid().ToString("N"); // More efficient format for GUIDs
        }
        
        /// <summary>
        /// Creates a new value type pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ValueTypePoolConfig(int initialCapacity) : this()
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
            GrowthIncrement = Math.Max(8, initialCapacity / 4);
        }
        
        /// <summary>
        /// Creates a new value type pool configuration with the specified initial and maximum sizes.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than initialCapacity and not 0</exception>
        public ValueTypePoolConfig(int initialCapacity, int maxSize) : this(initialCapacity)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be negative");
                
            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize), 
                    "Max size must be greater than or equal to initial capacity, or 0 for unlimited");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new value type pool configuration based on an existing pool configuration.
        /// </summary>
        /// <param name="baseConfig">The base configuration to extend</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null</exception>
        public ValueTypePoolConfig(IPoolConfig baseConfig)
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
            ThreadingMode = baseConfig.ThreadingMode;
            EnableAutoShrink = baseConfig.EnableAutoShrink;
            ShrinkThreshold = baseConfig.ShrinkThreshold;
            ShrinkInterval = baseConfig.ShrinkInterval;
            NativeAllocator = baseConfig.NativeAllocator;
            UseExponentialGrowth = baseConfig.UseExponentialGrowth;
            GrowthFactor = baseConfig.GrowthFactor;
            GrowthIncrement = baseConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = baseConfig.ThrowIfExceedingMaxCount;
            
            // If the source is already a ValueTypePoolConfig, copy value-type specific properties
            if (baseConfig is ValueTypePoolConfig valueTypeConfig)
            {
                UseMemoryAlignment = valueTypeConfig.UseMemoryAlignment;
                AlignmentSizeBytes = valueTypeConfig.AlignmentSizeBytes;
                ClearMemoryOnRelease = valueTypeConfig.ClearMemoryOnRelease;
                BlittableTypesOnly = valueTypeConfig.BlittableTypesOnly;
                UseSIMDAlignment = valueTypeConfig.UseSIMDAlignment;
                UseDirectMemoryAccess = valueTypeConfig.UseDirectMemoryAccess;
                UseBurstAllocation = valueTypeConfig.UseBurstAllocation;
                SkipDefaultInitialization = valueTypeConfig.SkipDefaultInitialization;
                StructLayoutType = valueTypeConfig.StructLayoutType;
                UseInlineHandling = valueTypeConfig.UseInlineHandling;
                UseValidationChecks = valueTypeConfig.UseValidationChecks;
                OverflowHandling = valueTypeConfig.OverflowHandling;
                UseAutomaticDisposal = valueTypeConfig.UseAutomaticDisposal;
                DisposeTimeoutSeconds = valueTypeConfig.DisposeTimeoutSeconds;
                IsJobCompatible = valueTypeConfig.IsJobCompatible;
            }
            else
            {
                // Set sensible defaults for value types if source isn't a ValueTypePoolConfig
                UseMemoryAlignment = true;
                AlignmentSizeBytes = 16;
                ClearMemoryOnRelease = true;
                BlittableTypesOnly = true;
                UseSIMDAlignment = false;
                UseDirectMemoryAccess = false;
                UseBurstAllocation = false;
                SkipDefaultInitialization = false;
                StructLayoutType = StructLayoutType.Sequential;
                UseInlineHandling = false;
                UseValidationChecks = true;
                OverflowHandling = OverflowHandlingType.ThrowException;
                UseAutomaticDisposal = false;
                DisposeTimeoutSeconds = 60f;
                IsJobCompatible = false;
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new ValueTypePoolConfig instance with the same settings</returns>
        public IPoolConfig Clone()
        {
            return new ValueTypePoolConfig
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
                ThreadingMode = ThreadingMode,
                EnableAutoShrink = EnableAutoShrink,
                ShrinkThreshold = ShrinkThreshold,
                ShrinkInterval = ShrinkInterval,
                NativeAllocator = NativeAllocator,
                UseExponentialGrowth = UseExponentialGrowth,
                GrowthFactor = GrowthFactor,
                GrowthIncrement = GrowthIncrement,
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount,
                
                // Copy value type specific properties
                UseMemoryAlignment = UseMemoryAlignment,
                AlignmentSizeBytes = AlignmentSizeBytes,
                ClearMemoryOnRelease = ClearMemoryOnRelease,
                BlittableTypesOnly = BlittableTypesOnly,
                UseSIMDAlignment = UseSIMDAlignment,
                UseDirectMemoryAccess = UseDirectMemoryAccess,
                UseBurstAllocation = UseBurstAllocation,
                SkipDefaultInitialization = SkipDefaultInitialization,
                StructLayoutType = StructLayoutType,
                UseInlineHandling = UseInlineHandling,
                UseValidationChecks = UseValidationChecks,
                OverflowHandling = OverflowHandling,
                UseAutomaticDisposal = UseAutomaticDisposal,
                DisposeTimeoutSeconds = DisposeTimeoutSeconds,
                IsJobCompatible = IsJobCompatible
            };
        }
        
        /// <summary>
        /// Creates a builder initialized with this configuration.
        /// </summary>
        /// <returns>A new builder initialized with this configuration</returns>
        public ValueTypePoolConfigBuilder ToBuilder()
        {
            return new ValueTypePoolConfigBuilder(this);
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
                
            if (AlignmentSizeBytes <= 0 || (AlignmentSizeBytes & (AlignmentSizeBytes - 1)) != 0)
                throw new InvalidOperationException("Alignment size must be a positive power of 2");
                
            if (DisposeTimeoutSeconds < 0)
                throw new InvalidOperationException("Dispose timeout cannot be negative");
                
            if (UseDirectMemoryAccess && UseValidationChecks)
                throw new InvalidOperationException("Cannot use both direct memory access and validation checks");
                
            if (UseSIMDAlignment && AlignmentSizeBytes < 16)
                throw new InvalidOperationException("SIMD optimization requires minimum 16-byte alignment");
                
            if (IsJobCompatible && !BlittableTypesOnly)
                throw new InvalidOperationException("Job compatibility requires blittable types only");
        }
        
        /// <summary>
        /// Optimizes the configuration for high-performance scenarios.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ValueTypePoolConfig OptimizeForPerformance()
        {
            InitialCapacity = Math.Max(InitialCapacity, 64);
            UseMemoryAlignment = true;
            AlignmentSizeBytes = 16;
            ClearMemoryOnRelease = false;
            BlittableTypesOnly = true;
            UseDirectMemoryAccess = true;
            UseValidationChecks = false;
            SkipDefaultInitialization = true;
            CollectMetrics = false;
            DetailedLogging = false;
            UseExponentialGrowth = true;
            GrowthFactor = 2.0f;
            
            return this;
        }
        
        /// <summary>
        /// Optimizes the configuration for Burst compilation compatibility.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ValueTypePoolConfig OptimizeForBurst()
        {
            BlittableTypesOnly = true;
            UseMemoryAlignment = true;
            AlignmentSizeBytes = 16;
            UseBurstAllocation = true;
            UseDirectMemoryAccess = true;
            StructLayoutType = StructLayoutType.Sequential;
            IsJobCompatible = true;
            
            return this;
        }
        
        /// <summary>
        /// Optimizes the configuration for SIMD operations.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ValueTypePoolConfig OptimizeForSimd()
        {
            UseMemoryAlignment = true;
            AlignmentSizeBytes = 32; // Optimal for AVX operations
            BlittableTypesOnly = true;
            UseSIMDAlignment = true;
            StructLayoutType = StructLayoutType.Sequential;
            UseDirectMemoryAccess = true;
            UseValidationChecks = false;
            
            return this;
        }
        
        /// <summary>
        /// Optimizes the configuration for debugging and diagnostics.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public ValueTypePoolConfig OptimizeForDebugging()
        {
            UseValidationChecks = true;
            ClearMemoryOnRelease = true;
            UseDirectMemoryAccess = false;
            CollectMetrics = true;
            DetailedLogging = true;
            LogWarnings = true;
            UseExponentialGrowth = false;
            OverflowHandling = OverflowHandlingType.ThrowException;
            
            return this;
        }
        
        /// <summary>
        /// Returns a string representation of this configuration.
        /// </summary>
        /// <returns>A string representation of the configuration</returns>
        public override string ToString()
        {
            return $"ValueTypePoolConfig[Id={ConfigId}, InitCap={InitialCapacity}, " +
                   $"MaxCap={MaximumCapacity}, BlittableOnly={BlittableTypesOnly}, " +
                   $"BurstAlloc={UseBurstAllocation}, JobCompat={IsJobCompatible}]";
        }
        
        #endregion
    }
}