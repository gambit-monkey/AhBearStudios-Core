using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for Burst-compatible native pools optimized for high-performance scenarios.
    /// Uses composition to wrap NativePoolConfig while adding Burst-specific settings.
    /// Fully compatible with the Unity Collections v2 API.
    /// </summary>
    public sealed class BurstCompatiblePoolConfig : IPoolConfig
    {
        #region Underlying Configuration
        
        /// <summary>
        /// The underlying native pool configuration providing base functionality.
        /// </summary>
        private readonly NativePoolConfig _nativeConfig;
        
        #endregion
        
        #region IPoolConfig Implementation
        
        /// <inheritdoc />
        public string ConfigId
        {
            get => _nativeConfig.ConfigId;
            set => _nativeConfig.ConfigId = value;
        }
        
        /// <inheritdoc />
        public int InitialCapacity
        {
            get => _nativeConfig.InitialCapacity;
            set => _nativeConfig.InitialCapacity = value;
        }
        
        /// <inheritdoc />
        public int MinimumCapacity
        {
            get => _nativeConfig.MinimumCapacity;
            set => _nativeConfig.MinimumCapacity = value;
        }
        
        /// <inheritdoc />
        public int MaximumCapacity
        {
            get => _nativeConfig.MaximumCapacity;
            set => _nativeConfig.MaximumCapacity = value;
        }
        
        /// <inheritdoc />
        public bool PrewarmOnInit
        {
            get => _nativeConfig.PrewarmOnInit;
            set => _nativeConfig.PrewarmOnInit = value;
        }
        
        /// <inheritdoc />
        public bool CollectMetrics
        {
            get => _nativeConfig.CollectMetrics;
            set => _nativeConfig.CollectMetrics = value;
        }
        
        /// <inheritdoc />
        public bool DetailedLogging
        {
            get => _nativeConfig.DetailedLogging;
            set => _nativeConfig.DetailedLogging = value;
        }
        
        /// <inheritdoc />
        public bool LogWarnings
        {
            get => _nativeConfig.LogWarnings;
            set => _nativeConfig.LogWarnings = value;
        }
        
        /// <inheritdoc />
        public bool ResetOnRelease
        {
            get => _nativeConfig.ResetOnRelease;
            set => _nativeConfig.ResetOnRelease = value;
        }
        
        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode
        {
            get => _nativeConfig.ThreadingMode;
            set => _nativeConfig.ThreadingMode = value;
        }
        
        /// <inheritdoc />
        public bool EnableAutoShrink
        {
            get => _nativeConfig.EnableAutoShrink;
            set => _nativeConfig.EnableAutoShrink = value;
        }
        
        /// <inheritdoc />
        public float ShrinkThreshold
        {
            get => _nativeConfig.ShrinkThreshold;
            set => _nativeConfig.ShrinkThreshold = value;
        }
        
        /// <inheritdoc />
        public float ShrinkInterval
        {
            get => _nativeConfig.ShrinkInterval;
            set => _nativeConfig.ShrinkInterval = value;
        }
        
        /// <inheritdoc />
        public Allocator NativeAllocator
        {
            get => _nativeConfig.NativeAllocator;
            set => _nativeConfig.NativeAllocator = value;
        }
        
        /// <inheritdoc />
        public bool UseExponentialGrowth
        {
            get => _nativeConfig.UseExponentialGrowth;
            set => _nativeConfig.UseExponentialGrowth = value;
        }
        
        /// <inheritdoc />
        public float GrowthFactor
        {
            get => _nativeConfig.GrowthFactor;
            set => _nativeConfig.GrowthFactor = value;
        }
        
        /// <inheritdoc />
        public int GrowthIncrement
        {
            get => _nativeConfig.GrowthIncrement;
            set => _nativeConfig.GrowthIncrement = value;
        }
        
        /// <inheritdoc />
        public bool ThrowIfExceedingMaxCount
        {
            get => _nativeConfig.ThrowIfExceedingMaxCount;
            set => _nativeConfig.ThrowIfExceedingMaxCount = value;
        }
        
        #endregion
        
        #region NativePoolConfig Properties
        
        /// <summary>
        /// Gets or sets whether to use safety checks for native containers.
        /// Safety checks can help detect errors but impact performance.
        /// </summary>
        /// <remarks>Set to false in performance-critical code with Burst compilation.</remarks>
        public bool UseSafetyChecks
        {
            get => _nativeConfig.UseSafetyChecks;
            set => _nativeConfig.UseSafetyChecks = value;
        }
        
        /// <summary>
        /// Gets or sets whether to automatically dispose native resources on release.
        /// </summary>
        public bool AutoDisposeOnRelease
        {
            get => _nativeConfig.AutoDisposeOnRelease;
            set => _nativeConfig.AutoDisposeOnRelease = value;
        }
        
        /// <summary>
        /// Gets or sets whether to verify that the correct allocator is used.
        /// </summary>
        public bool VerifyAllocator
        {
            get => _nativeConfig.VerifyAllocator;
            set => _nativeConfig.VerifyAllocator = value;
        }
        
        /// <summary>
        /// Gets or sets flags for native configuration options.
        /// </summary>
        public int NativeConfigFlags
        {
            get => _nativeConfig.NativeConfigFlags;
            set => _nativeConfig.NativeConfigFlags = value;
        }
        
        /// <summary>
        /// Gets or sets whether to use Burst-compatible collections internally.
        /// </summary>
        /// <remarks>Should always be true for BurstCompatiblePoolConfig.</remarks>
        public bool UseBurstCompatibleCollections
        {
            get => _nativeConfig.UseBurstCompatibleCollections;
            set => _nativeConfig.UseBurstCompatibleCollections = value;
        }
        
        #endregion
        
        #region Burst-Specific Properties
        
        /// <summary>
        /// Controls whether to synchronize Burst-compiled code after operations.
        /// When enabled, ensures Burst-compiled jobs complete before returning control.
        /// Disabling improves performance when immediate synchronization isn't needed.
        /// </summary>
        public bool SyncBurstAfterOps { get; set; } = false;
        
        /// <summary>
        /// Controls optimization of memory layout for Burst compatibility.
        /// Affects internal data structures to ensure optimal performance with Burst.
        /// Recommended to keep enabled for best performance.
        /// </summary>
        public bool OptimizeMemoryLayout { get; set; } = true;
        
        /// <summary>
        /// Controls whether all internal collections are Burst-compatible.
        /// Must be true for all Burst-compatible pools to ensure proper execution.
        /// </summary>
        public bool EnsureBurstCompatibility { get; set; } = true;
        
        /// <summary>
        /// Controls whether the pool is optimized for use in Unity Jobs.
        /// Enables specific optimizations for job scheduling and execution.
        /// </summary>
        public bool IsJobOptimized { get; set; } = true;
        
        /// <summary>
        /// Memory alignment in bytes for native containers.
        /// Common values are 4, 8, 16, 32, or 64 bytes depending on SIMD requirements.
        /// </summary>
        /// <remarks>16 is a good default for most Vector operations.</remarks>
        public int MemoryAlignment { get; set; } = 16;
        
        /// <summary>
        /// Controls whether the pool automatically disposes when finalized.
        /// Important for preventing memory leaks with native memory.
        /// </summary>
        public bool DisposeOnFinalize { get; set; } = true;
        
        /// <summary>
        /// Controls cache line padding for optimized memory access in jobs.
        /// Helps prevent false sharing in multithreaded scenarios.
        /// </summary>
        public bool UseCacheLinePadding { get; set; } = false;
        
        /// <summary>
        /// Debug name for the pool (used in Profiler and debugging).
        /// </summary>
        public string DebugName { get; set; } = string.Empty;
        
        /// <summary>
        /// Controls whether the pool uses parallelism with jobs.
        /// Enables parallel processing of pool operations where applicable.
        /// </summary>
        public bool UseParallelJobs { get; set; } = false;
        
        /// <summary>
        /// Minimum batch size for parallel jobs.
        /// Controls the granularity of work division in parallel operations.
        /// </summary>
        /// <remarks>Larger values reduce overhead but may impact load balancing.</remarks>
        public int JobBatchSize { get; set; } = 32;
        
        /// <summary>
        /// Controls whether the pool tracks memory allocations.
        /// Useful for debugging but adds overhead.
        /// </summary>
        public bool TrackAllocations { get; set; } = false;
        
        /// <summary>
        /// Controls whether to use Burst compilation for pool operations.
        /// Should typically be true for maximum performance.
        /// </summary>
        public bool UseBurstCompilation { get; set; } = true;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a default Burst-compatible pool configuration with optimized settings.
        /// </summary>
        public BurstCompatiblePoolConfig()
        {
            _nativeConfig = new NativePoolConfig();
            
            // Default optimal settings for Burst
            UseSafetyChecks = false;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = false;
            VerifyAllocator = false;
            NativeAllocator = Allocator.Persistent;
        }
        
        /// <summary>
        /// Creates a Burst-compatible pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public BurstCompatiblePoolConfig(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            _nativeConfig = new NativePoolConfig(initialCapacity);
            
            // Default optimal settings for Burst
            UseSafetyChecks = false;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = false;
            VerifyAllocator = false;
        }
        
        /// <summary>
        /// Creates a Burst-compatible pool configuration with specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial number of items to create.</param>
        /// <param name="allocator">Native allocator to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public BurstCompatiblePoolConfig(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
            
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
                
            _nativeConfig = new NativePoolConfig(initialCapacity, allocator);
            
            // Default optimal settings for Burst
            UseSafetyChecks = false;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            UseBurstCompatibleCollections = true;
            DetailedLogging = false;
            CollectMetrics = false;
            VerifyAllocator = false;
        }
        
        /// <summary>
        /// Creates a Burst-compatible pool configuration from a base NativePoolConfig.
        /// </summary>
        /// <param name="baseConfig">The base configuration to copy settings from.</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null.</exception>
        public BurstCompatiblePoolConfig(NativePoolConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
        
            // Store the passed NativePoolConfig directly
            _nativeConfig = baseConfig.Clone() as NativePoolConfig;
    
            // Set common NativePoolConfig properties
            UseSafetyChecks = baseConfig.UseSafetyChecks;
            AutoDisposeOnRelease = baseConfig.AutoDisposeOnRelease;
            VerifyAllocator = baseConfig.VerifyAllocator;
            NativeConfigFlags = baseConfig.NativeConfigFlags;
            UseBurstCompatibleCollections = baseConfig.UseBurstCompatibleCollections;
    
            // Override with Burst-friendly values
            UseSafetyChecks = false;
            UseBurstCompatibleCollections = true;
    
            // Set default Burst-specific properties
            SyncBurstAfterOps = true;
            OptimizeMemoryLayout = true;
            EnsureBurstCompatibility = true;
            IsJobOptimized = true;
            MemoryAlignment = 16; // Typical alignment for SIMD
            DisposeOnFinalize = true;
            UseCacheLinePadding = false;
            DebugName = string.Empty;
            UseParallelJobs = true;
            JobBatchSize = 64;
            TrackAllocations = false;
            UseBurstCompilation = true;
        }
        
        /// <summary>
        /// Creates a Burst-compatible pool configuration from an IPoolConfig.
        /// </summary>
        /// <param name="baseConfig">The base configuration to copy settings from.</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null.</exception>
        public BurstCompatiblePoolConfig(IPoolConfig baseConfig) 
        {
            if (baseConfig == null) 
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
            
            // Start with a new NativePoolConfig
            _nativeConfig = new NativePoolConfig();
            
            // Copy IPoolConfig properties
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
            
            // Set Burst-compatible defaults
            UseSafetyChecks = false;
            UseBurstCompatibleCollections = true;
            ThreadingMode = PoolThreadingMode.JobCompatible;
            
            // If it's a NativePoolConfig, copy those properties too
            if (baseConfig is NativePoolConfig nativeConfig) 
            {
                UseSafetyChecks = nativeConfig.UseSafetyChecks;
                AutoDisposeOnRelease = nativeConfig.AutoDisposeOnRelease;
                VerifyAllocator = nativeConfig.VerifyAllocator;
                NativeConfigFlags = nativeConfig.NativeConfigFlags;
                UseBurstCompatibleCollections = nativeConfig.UseBurstCompatibleCollections;
            }
            
            // If it's already a BurstCompatiblePoolConfig, copy those properties too
            if (baseConfig is BurstCompatiblePoolConfig burstConfig) 
            {
                SyncBurstAfterOps = burstConfig.SyncBurstAfterOps;
                OptimizeMemoryLayout = burstConfig.OptimizeMemoryLayout;
                EnsureBurstCompatibility = burstConfig.EnsureBurstCompatibility;
                IsJobOptimized = burstConfig.IsJobOptimized;
                MemoryAlignment = burstConfig.MemoryAlignment;
                DisposeOnFinalize = burstConfig.DisposeOnFinalize;
                UseCacheLinePadding = burstConfig.UseCacheLinePadding;
                DebugName = burstConfig.DebugName;
                UseParallelJobs = burstConfig.UseParallelJobs;
                JobBatchSize = burstConfig.JobBatchSize;
                TrackAllocations = burstConfig.TrackAllocations;
                UseBurstCompilation = burstConfig.UseBurstCompilation;
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
            // Create a clone using the constructor that takes a NativePoolConfig
            // This will properly initialize the readonly _nativeConfig field
            var clone = new BurstCompatiblePoolConfig(_nativeConfig);
    
            // Set the BurstCompatiblePoolConfig-specific properties
            clone.SyncBurstAfterOps = this.SyncBurstAfterOps;
            clone.OptimizeMemoryLayout = this.OptimizeMemoryLayout;
            clone.EnsureBurstCompatibility = this.EnsureBurstCompatibility;
            clone.IsJobOptimized = this.IsJobOptimized;
            clone.MemoryAlignment = this.MemoryAlignment;
            clone.DisposeOnFinalize = this.DisposeOnFinalize;
            clone.UseCacheLinePadding = this.UseCacheLinePadding;
            clone.DebugName = this.DebugName;
            clone.UseParallelJobs = this.UseParallelJobs;
            clone.JobBatchSize = this.JobBatchSize;
            clone.TrackAllocations = this.TrackAllocations;
            clone.UseBurstCompilation = this.UseBurstCompilation;
    
            return clone;
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
                
            registry.RegisterConfigForType<T>(this);
        }
        
        #endregion
        
        #region Builder Support
        
        /// <summary>
        /// Creates a builder initialized with this instance's values.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public BurstCompatiblePoolConfigBuilder ToBuilder()
        {
            return new BurstCompatiblePoolConfigBuilder().FromExisting(this);
        }
        
        /// <summary>
        /// Creates a builder for this configuration type.
        /// </summary>
        /// <returns>A new Burst-compatible pool configuration builder.</returns>
        public static BurstCompatiblePoolConfigBuilder CreateBuilder()
        {
            return new BurstCompatiblePoolConfigBuilder();
        }
        
        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A new Burst-compatible pool configuration builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfigBuilder CreateBuilder(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
        
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }
        
        /// <summary>
        /// Creates a builder for this configuration with the specified initial capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="allocator">Allocator to use.</param>
        /// <returns>A new Burst-compatible pool configuration builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if allocator is invalid.</exception>
        public static BurstCompatiblePoolConfigBuilder CreateBuilder(int initialCapacity, Allocator allocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
        
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
        
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity) 
                .WithAllocator(allocator);
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Factory method to create a high-performance Burst configuration optimized for job system.
        /// Ideal for compute-intensive parallel jobs with Burst compilation.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateJobOptimized(int initialCapacity = 64)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // Performance optimization settings
            config.UseSafetyChecks = false;
            config.DetailedLogging = false;
            config.CollectMetrics = false;
            config.ThreadingMode = PoolThreadingMode.JobCompatible;
            config.UseBurstCompatibleCollections = true;
            config.OptimizeMemoryLayout = true;
            config.SyncBurstAfterOps = false;
            config.UseExponentialGrowth = true;
            config.GrowthFactor = 1.5f;
            config.VerifyAllocator = false;
            config.AutoDisposeOnRelease = true;
            config.IsJobOptimized = true;
            config.UseBurstCompilation = true;
            config.MemoryAlignment = 16;
            config.UseParallelJobs = true;
            config.JobBatchSize = 64;
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a balanced Burst configuration suitable for general use.
        /// Balances performance and safety with reasonable defaults.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateBalanced(int initialCapacity = 32)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // Balanced settings
            config.UseSafetyChecks = true;
            config.DetailedLogging = false;
            config.CollectMetrics = true;
            config.ThreadingMode = PoolThreadingMode.JobCompatible;
            config.UseBurstCompatibleCollections = true;
            config.OptimizeMemoryLayout = true;
            config.SyncBurstAfterOps = true;
            config.UseExponentialGrowth = true;
            config.GrowthFactor = 2.0f;
            config.VerifyAllocator = true;
            config.AutoDisposeOnRelease = true;
            config.IsJobOptimized = true;
            config.UseBurstCompilation = true;
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a debug-friendly Burst configuration for development.
        /// Includes safety features and diagnostics helpful during development.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateDebug(int initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // Debug-friendly settings
            config.UseSafetyChecks = true;
            config.DetailedLogging = true;
            config.CollectMetrics = true;
            config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            config.UseBurstCompatibleCollections = true;
            config.OptimizeMemoryLayout = true;
            config.SyncBurstAfterOps = true;
            config.LogWarnings = true;
            config.VerifyAllocator = true;
            config.AutoDisposeOnRelease = true;
            config.TrackAllocations = true;
            config.UseParallelJobs = false;
            config.DebugName = "DebugPool";
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a configuration optimized for SIMD operations.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured instance for SIMD operations.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateSIMDOptimized(int initialCapacity = 64)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // SIMD optimization settings
            config.UseSafetyChecks = false;
            config.DetailedLogging = false;
            config.CollectMetrics = false;
            config.ThreadingMode = PoolThreadingMode.JobCompatible;
            config.UseBurstCompatibleCollections = true;
            config.OptimizeMemoryLayout = true;
            config.SyncBurstAfterOps = false;
            config.UseExponentialGrowth = true;
            config.VerifyAllocator = false;
            config.AutoDisposeOnRelease = true;
            config.IsJobOptimized = true;
            config.UseBurstCompilation = true;
            config.MemoryAlignment = 32; // Aligned for AVX operations
            config.UseCacheLinePadding = true;
            config.MaximumCapacity = initialCapacity * 2;
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a configuration optimized for parallel compute operations.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="batchSize">Batch size for parallel jobs.</param>
        /// <returns>A fully configured instance for parallel computing.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative or batchSize is less than 1.</exception>
        public static BurstCompatiblePoolConfig CreateParallelCompute(int initialCapacity = 128, int batchSize = 64)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            if (batchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be at least 1");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // Parallel compute optimization settings
            config.UseSafetyChecks = false;
            config.DetailedLogging = false;
            config.CollectMetrics = false;
            config.ThreadingMode = PoolThreadingMode.JobCompatible;
            config.UseBurstCompatibleCollections = true;
            config.OptimizeMemoryLayout = true;
            config.SyncBurstAfterOps = false;
            config.VerifyAllocator = false;
            config.AutoDisposeOnRelease = true;
                        config.IsJobOptimized = true;
            config.UseBurstCompilation = true;
            config.MemoryAlignment = 64; // Cache line size on most architectures
            config.UseCacheLinePadding = true;
            config.UseParallelJobs = true;
            config.JobBatchSize = batchSize;
            config.MaximumCapacity = initialCapacity * 4;
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a memory-efficient Burst configuration.
        /// Minimizes memory overhead at the cost of some performance.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured memory-efficient instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateMemoryEfficient(int initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.Persistent);
            
            // Memory-efficient settings
            config.UseSafetyChecks = false;
            config.DetailedLogging = false;
            config.CollectMetrics = false;
            config.MaximumCapacity = initialCapacity * 2;
            config.EnableAutoShrink = true;
            config.ShrinkThreshold = 0.5f;
            config.ShrinkInterval = 5.0f;
            config.UseExponentialGrowth = false;
            config.GrowthIncrement = 4;
            config.UseCacheLinePadding = false;
            config.MemoryAlignment = 4; // Minimum alignment
            config.TrackAllocations = false;
            config.UseParallelJobs = false;
            
            return config;
        }
        
        /// <summary>
        /// Factory method to create a temporary Burst configuration for short-lived operations.
        /// Uses TempJob allocator and is configured for automatic cleanup.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A fully configured temporary instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative.</exception>
        public static BurstCompatiblePoolConfig CreateTemporary(int initialCapacity = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            var config = new BurstCompatiblePoolConfig(initialCapacity, Allocator.TempJob);
            
            // Temporary pool settings
            config.UseSafetyChecks = true;
            config.DetailedLogging = false;
            config.CollectMetrics = false;
            config.MaximumCapacity = initialCapacity * 2;
            config.EnableAutoShrink = true;
            config.AutoDisposeOnRelease = true;
            config.DisposeOnFinalize = true;
            config.SyncBurstAfterOps = true;
            config.UseExponentialGrowth = false;
            config.GrowthIncrement = 8;
            
            return config;
        }
        
        /// <summary>
        /// Creates a configuration from an existing IPoolConfig, enhancing it with Burst compatibility.
        /// </summary>
        /// <param name="baseConfig">The base configuration to enhance.</param>
        /// <returns>A Burst-compatible version of the input configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null.</exception>
        public static BurstCompatiblePoolConfig FromExisting(IPoolConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
                
            return new BurstCompatiblePoolConfig(baseConfig);
        }
        
        #endregion
    }
}