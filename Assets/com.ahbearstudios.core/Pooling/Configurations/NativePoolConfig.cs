using System;
using AhBearStudios.Pooling.Builders;
using AhBearStudios.Pooling.Core;
using Unity.Collections;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Configuration for Unity native container pools optimized for high-performance scenarios.
    /// Implements IPoolConfig to provide specialized settings for native memory management.
    /// Fully compatible with Unity Collections v2.
    /// </summary>
    public sealed class NativePoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation
        
        /// <inheritdoc />
        public string ConfigId { get; set; } = Guid.NewGuid().ToString();
        
        /// <inheritdoc />
        public int InitialCapacity { get; set; } = 32;
        
        /// <inheritdoc />
        public int MinimumCapacity { get; set; } = 0;
        
        /// <inheritdoc />
        public int MaximumCapacity { get; set; } = 0; // 0 means unlimited
        
        /// <inheritdoc />
        public bool PrewarmOnInit { get; set; } = true;
        
        /// <inheritdoc />
        public bool CollectMetrics { get; set; } = false;
        
        /// <inheritdoc />
        public bool DetailedLogging { get; set; } = false;
        
        /// <inheritdoc />
        public bool LogWarnings { get; set; } = true;
        
        /// <inheritdoc />
        public bool ResetOnRelease { get; set; } = true;
        
        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.SingleThreaded;
        
        /// <inheritdoc />
        public bool EnableAutoShrink { get; set; } = false;
        
        /// <inheritdoc />
        public float ShrinkThreshold { get; set; } = 0.3f;
        
        /// <inheritdoc />
        public float ShrinkInterval { get; set; } = 60.0f;
        
        /// <inheritdoc />
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;
        
        /// <inheritdoc />
        public bool UseExponentialGrowth { get; set; } = true;
        
        /// <inheritdoc />
        public float GrowthFactor { get; set; } = 2.0f;
        
        /// <inheritdoc />
        public int GrowthIncrement { get; set; } = 10;
        
        /// <inheritdoc />
        public bool ThrowIfExceedingMaxCount { get; set; } = false;
        
        #endregion
        
        #region Native-specific Properties
        
        /// <summary>
        /// Gets or sets whether to use safety checks for native containers.
        /// Safety checks can help detect errors but impact performance.
        /// </summary>
        /// <remarks>
        /// Set to false in performance-critical code once thoroughly tested.
        /// </remarks>
        public bool UseSafetyChecks { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to automatically dispose native resources on release.
        /// </summary>
        /// <remarks>
        /// Should typically be true to prevent memory leaks, but can be false if 
        /// explicit control over disposal timing is needed.
        /// </remarks>
        public bool AutoDisposeOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to verify that the correct allocator is used.
        /// </summary>
        /// <remarks>
        /// Helps catch memory management errors at the cost of performance.
        /// </remarks>
        public bool VerifyAllocator { get; set; } = true;
        
        /// <summary>
        /// Gets or sets flags for native configuration options.
        /// </summary>
        /// <remarks>
        /// Used for specialized native container configurations.
        /// </remarks>
        public int NativeConfigFlags { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets whether to use Burst-compatible collections internally.
        /// </summary>
        /// <remarks>
        /// Set to true when the pool will be used with Burst-compiled code.
        /// </remarks>
        public bool UseBurstCompatibleCollections { get; set; } = false;
        
        /// <summary>
        /// Gets or sets specific pool type this configuration is designed for.
        /// </summary>
        public Type PoolType { get; set; } = null;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a default native pool configuration with safe defaults.
        /// </summary>
        public NativePoolConfig()
        {
            // Base defaults are set through property initializers
        }
        
        /// <summary>
        /// Creates a native pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public NativePoolConfig(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
        }
        
        /// <summary>
        /// Creates a native pool configuration with specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create.</param>
        /// <param name="allocator">Native allocator to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public NativePoolConfig(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
                
            InitialCapacity = initialCapacity;
            NativeAllocator = allocator;
        }
        
        /// <summary>
        /// Creates a native pool configuration with detailed settings.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create.</param>
        /// <param name="allocator">Native allocator to use.</param>
        /// <param name="useSafetyChecks">Whether to use safety checks.</param>
        /// <param name="autoDisposeOnRelease">Whether to auto-dispose resources.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public NativePoolConfig(
            int initialCapacity, 
            Allocator allocator, 
            bool useSafetyChecks, 
            bool autoDisposeOnRelease)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
                
            InitialCapacity = initialCapacity;
            NativeAllocator = allocator;
            UseSafetyChecks = useSafetyChecks;
            AutoDisposeOnRelease = autoDisposeOnRelease;
        }
        
        /// <summary>
        /// Creates a native pool configuration from a base IPoolConfig.
        /// </summary>
        /// <param name="baseConfig">The base configuration to copy settings from.</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null.</exception>
        public NativePoolConfig(IPoolConfig baseConfig)
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
            
            // If source was also a NativePoolConfig, copy specific settings
            if (baseConfig is NativePoolConfig nativeConfig)
            {
                UseSafetyChecks = nativeConfig.UseSafetyChecks;
                AutoDisposeOnRelease = nativeConfig.AutoDisposeOnRelease;
                VerifyAllocator = nativeConfig.VerifyAllocator;
                NativeConfigFlags = nativeConfig.NativeConfigFlags;
                UseBurstCompatibleCollections = nativeConfig.UseBurstCompatibleCollections;
                PoolType = nativeConfig.PoolType;
            }
        }
        
        #endregion
        
        #region IPoolConfig Implementation
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new instance with identical settings.</returns>
        public IPoolConfig Clone()
        {
            return new NativePoolConfig
            {
                // Copy base IPoolConfig properties
                ConfigId = this.ConfigId,
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
                
                // Copy NativePoolConfig-specific properties
                UseSafetyChecks = this.UseSafetyChecks,
                AutoDisposeOnRelease = this.AutoDisposeOnRelease,
                VerifyAllocator = this.VerifyAllocator,
                NativeConfigFlags = this.NativeConfigFlags,
                UseBurstCompatibleCollections = this.UseBurstCompatibleCollections,
                PoolType = this.PoolType
            };
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
        /// <typeparam name="T">The type of items this configuration is for.</typeparam>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        public void RegisterForType<T>(IPoolConfigRegistry registry) where T : class
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
                
            PoolType = typeof(T);
            registry.RegisterConfigForType<T>(this);
        }
        
        #endregion
        
        #region Builder Support
        
        /// <summary>
        /// Creates a builder initialized with this instance's values.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public NativePoolConfigBuilder ToBuilder()
        {
            return new NativePoolConfigBuilder().FromExisting(this);
        }
        
        /// <summary>
        /// Creates a builder for this configuration type.
        /// </summary>
        /// <returns>A new native pool configuration builder.</returns>
        public static NativePoolConfigBuilder CreateBuilder()
        {
            return new NativePoolConfigBuilder();
        }
        
        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A new native pool configuration builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfigBuilder CreateBuilder(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
        
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }
        
        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="allocator">Allocator to use.</param>
        /// <returns>A new native pool configuration builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public static NativePoolConfigBuilder CreateBuilder(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
        
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
        
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithAllocator(allocator);
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Factory method to create a high-performance configuration optimized for speed.
        /// Sacrifices safety checks for maximum performance.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured high-performance instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateHighPerformance(int initialCapacity = 32)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = false,
                DetailedLogging = false,
                CollectMetrics = false,
                EnableAutoShrink = false,
                ResetOnRelease = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                VerifyAllocator = false,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Factory method to create a job-compatible configuration optimized for Unity's Job System.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured job-compatible instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateJobCompatible(int initialCapacity = 32)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = true,
                DetailedLogging = false,
                CollectMetrics = false,
                ThreadingMode = PoolThreadingMode.JobCompatible,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                VerifyAllocator = true,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Factory method to create a Burst-compatible configuration optimized for Burst compiler.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured Burst-compatible instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateBurstCompatible(int initialCapacity = 32)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = false,
                DetailedLogging = false,
                CollectMetrics = false,
                ThreadingMode = PoolThreadingMode.JobCompatible,
                UseBurstCompatibleCollections = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                VerifyAllocator = false,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Factory method to create a debug-friendly configuration for development.
        /// Includes safety features and diagnostics helpful during development.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured debug-friendly instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateDebug(int initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = true,
                DetailedLogging = true,
                CollectMetrics = true,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                LogWarnings = true,
                VerifyAllocator = true,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Factory method to create a memory-efficient configuration that minimizes allocations.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured memory-efficient instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateMemoryEfficient(int initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                MaximumCapacity = initialCapacity * 2,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = false,
                DetailedLogging = false,
                CollectMetrics = false,
                EnableAutoShrink = true,
                ShrinkThreshold = 0.5f,
                ShrinkInterval = 5.0f,
                UseExponentialGrowth = false,
                GrowthIncrement = 4,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Factory method to create a balanced configuration suitable for general use.
        /// Balances performance and safety with reasonable defaults.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured balanced instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static NativePoolConfig CreateBalanced(int initialCapacity = 32)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            return new NativePoolConfig
            {
                InitialCapacity = initialCapacity,
                NativeAllocator = Allocator.Persistent,
                UseSafetyChecks = true,
                DetailedLogging = false,
                CollectMetrics = true,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                EnableAutoShrink = true,
                ShrinkThreshold = 0.3f,
                ShrinkInterval = 60.0f,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                VerifyAllocator = true,
                AutoDisposeOnRelease = true
            };
        }
        
        /// <summary>
        /// Creates a configuration from an existing IPoolConfig.
        /// </summary>
        /// <param name="baseConfig">The base configuration to copy from.</param>
        /// <returns>A new NativePoolConfig with copied settings.</returns>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null.</exception>
        public static NativePoolConfig FromExisting(IPoolConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
                
            return new NativePoolConfig(baseConfig);
        }
        
        #endregion
    }
}