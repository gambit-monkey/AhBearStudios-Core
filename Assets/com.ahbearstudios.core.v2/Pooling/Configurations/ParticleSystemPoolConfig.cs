using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Interfaces;
using AhBearStudios.Core.Pooling.Services;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Configuration for particle system-based pools that manage Unity ParticleSystem instances.
    /// Fully compatible with Unity Collections v2, Burst, and Unity Jobs system.
    /// Uses composition rather than inheritance for better modularity.
    /// </summary>
    public sealed class ParticleSystemPoolConfig : IPoolConfig, IDisposable
    {
        #region IPoolConfig Implementation

        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the maximum size of the pool. Set to 0 for unlimited.
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
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.SingleThreaded;
        
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
        
        #region ParticleSystem Pool Specific Properties
        
        /// <summary>
        /// Gets or sets whether components should be parented under a transform.
        /// </summary>
        public bool UseParentTransform { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the parent transform for pooled particle systems.
        /// </summary>
        public Transform ParentTransform { get; set; } = null;
        
        /// <summary>
        /// Gets or sets whether to prewarm particles when initializing pool items.
        /// </summary>
        public bool PrewarmParticles { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to stop the particle system when released back to the pool.
        /// </summary>
        public bool StopOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to clear particles when the system is released back to the pool.
        /// </summary>
        public bool ClearParticlesOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the default duration in seconds for looping particle effects.
        /// </summary>
        public float DefaultLoopingEffectDuration { get; set; } = 5.0f;
        
        /// <summary>
        /// Gets or sets whether to disable particle emission when released back to the pool.
        /// </summary>
        public bool DisableEmissionOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to cache particle system component references.
        /// </summary>
        public bool CacheComponents { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to optimize the pool for one-shot particle effects.
        /// </summary>
        public bool OptimizeForOneShot { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to use safety handles for native collections.
        /// </summary>
        public bool UseSafetyHandles { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to use SIMD-aligned memory for better performance.
        /// </summary>
        public bool UseSIMDAlignment { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to simulate the particle system when prewarm is enabled.
        /// </summary>
        public bool SimulateOnPrewarm { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to optimize memory layout for cache coherence.
        /// </summary>
        public bool OptimizeForCacheCoherence { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to validate particle system state on get/release operations.
        /// </summary>
        public bool ValidateParticleState { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the time in seconds to simulate particles during prewarm.
        /// </summary>
        public float PrewarmSimulationTime { get; set; } = 1.0f;
        
        #endregion
        
        #region Private Fields
        
        private readonly IPoolLogger _logger;
        private readonly IPoolingService _service;
        private bool _isDisposed;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new instance of the particle system pool configuration.
        /// </summary>
        public ParticleSystemPoolConfig()
        {
            _logger = null;
            _service = null;
        }
        
        /// <summary>
        /// Creates a new instance of the particle system pool configuration with dependency injection.
        /// </summary>
        /// <param name="service">Service locator for pool services</param>
        public ParticleSystemPoolConfig(IPoolingService service = null)
        {
            _service = service ?? DefaultPoolingServices.Instance;
            _logger = _service.GetService<IPoolLogger>();
        }
        
        /// <summary>
        /// Creates a new particle system pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="service">Service locator for pool services</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ParticleSystemPoolConfig(int initialCapacity, IPoolingService service = null) 
            : this(service)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }
        
        /// <summary>
        /// Creates a new particle system pool configuration with the specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="service">Service locator for pool services</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public ParticleSystemPoolConfig(int initialCapacity, int maxSize, IPoolingService service = null) 
            : this(initialCapacity, service)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Maximum size cannot be negative");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new particle system pool configuration with the specified initial and maximum capacities and prewarm setting.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarmOnInit">Whether to prewarm the pool on initialization</param>
        /// <param name="service">Service locator for pool services</param>
        public ParticleSystemPoolConfig(int initialCapacity, int maxSize, bool prewarmOnInit, IPoolingService service = null) 
            : this(initialCapacity, maxSize, service)
        {
            PrewarmOnInit = prewarmOnInit;
        }
        
        /// <summary>
        /// Creates a new particle system pool configuration with the specified ID.
        /// </summary>
        /// <param name="configId">Unique identifier for this configuration</param>
        /// <param name="service">Service locator for pool services</param>
        /// <exception cref="ArgumentException">Thrown if configId is null or empty</exception>
        public ParticleSystemPoolConfig(string configId, IPoolingService service = null) 
            : this(service)
        {
            if (string.IsNullOrEmpty(configId))
                throw new ArgumentException("Config ID cannot be null or empty", nameof(configId));
                
            ConfigId = configId;
        }
        
        /// <summary>
        /// Creates a new particle system pool configuration by copying settings from another configuration.
        /// </summary>
        /// <param name="source">Source configuration to copy from</param>
        /// <param name="service">Service locator for pool services</param>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public ParticleSystemPoolConfig(IPoolConfig source, IPoolingService service = null) 
            : this(service)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source configuration cannot be null");
                
            ConfigId = source.ConfigId;
            InitialCapacity = source.InitialCapacity;
            MaximumCapacity = source.MaximumCapacity;
            PrewarmOnInit = source.PrewarmOnInit;
            CollectMetrics = source.CollectMetrics;
            DetailedLogging = source.DetailedLogging;
            LogWarnings = source.LogWarnings;
            ResetOnRelease = source.ResetOnRelease;
            ThreadingMode = source.ThreadingMode;
            EnableAutoShrink = source.EnableAutoShrink;
            ShrinkThreshold = source.ShrinkThreshold;
            ShrinkInterval = source.ShrinkInterval;
            NativeAllocator = source.NativeAllocator;
            UseExponentialGrowth = source.UseExponentialGrowth;
            GrowthFactor = source.GrowthFactor;
            GrowthIncrement = source.GrowthIncrement;
            ThrowIfExceedingMaxCount = source.ThrowIfExceedingMaxCount;
            
            // Copy particle system specific properties if source is also a ParticleSystemPoolConfig
            if (source is ParticleSystemPoolConfig particleConfig)
            {
                CopyParticleSystemSpecificProperties(particleConfig);
            }
        }
        
        /// <summary>
        /// Converts this configuration to a builder for further modification.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public ParticleSystemPoolConfigBuilder ToBuilder()
        {
            return new ParticleSystemPoolConfigBuilder().FromExisting(this);
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
        /// Registers this configuration for a specific GameObject prefab type.
        /// </summary>
        /// <param name="registry">The registry to register with.</param>
        /// <param name="prefabName">The name of the prefab this configuration is for.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        /// <exception cref="ArgumentException">Thrown if prefabName is null or empty.</exception>
        public void RegisterForPrefab(IPoolConfigRegistry registry, string prefabName)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
                
            if (string.IsNullOrEmpty(prefabName))
                throw new ArgumentException("Prefab name cannot be null or empty", nameof(prefabName));
                
            registry.RegisterConfig($"Prefab:{prefabName}", this);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Copies particle system specific properties from another particle system pool configuration.
        /// </summary>
        /// <param name="particleConfig">Source particle system configuration to copy from</param>
        /// <exception cref="ArgumentNullException">Thrown if particleConfig is null</exception>
        public void CopyParticleSystemSpecificProperties(ParticleSystemPoolConfig particleConfig)
        {
            if (particleConfig == null)
                throw new ArgumentNullException(nameof(particleConfig), "Source particle config cannot be null");
                
            UseParentTransform = particleConfig.UseParentTransform;
            ParentTransform = particleConfig.ParentTransform;
            PrewarmParticles = particleConfig.PrewarmParticles;
            StopOnRelease = particleConfig.StopOnRelease;
            ClearParticlesOnRelease = particleConfig.ClearParticlesOnRelease;
            DefaultLoopingEffectDuration = particleConfig.DefaultLoopingEffectDuration;
            DisableEmissionOnRelease = particleConfig.DisableEmissionOnRelease;
            CacheComponents = particleConfig.CacheComponents;
            OptimizeForOneShot = particleConfig.OptimizeForOneShot;
            UseSafetyHandles = particleConfig.UseSafetyHandles;
            UseSIMDAlignment = particleConfig.UseSIMDAlignment;
            SimulateOnPrewarm = particleConfig.SimulateOnPrewarm;
            OptimizeForCacheCoherence = particleConfig.OptimizeForCacheCoherence;
            ValidateParticleState = particleConfig.ValidateParticleState;
            PrewarmSimulationTime = particleConfig.PrewarmSimulationTime;
        }
        
        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new instance of ParticleSystemPoolConfig with the same settings</returns>
        public IPoolConfig Clone()
        {
            var clone = new ParticleSystemPoolConfig(_service)
            {
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
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount
            };
            
            // Clone particle system specific properties
            clone.CopyParticleSystemSpecificProperties(this);
            
            return clone;
        }
        
        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool Validate()
        {
            // Basic validation
            if (InitialCapacity < 0)
            {
                LogWarning($"Invalid initial capacity: {InitialCapacity}. Must be >= 0.");
                return false;
            }
            
            if (MaximumCapacity < 0)
            {
                LogWarning($"Invalid maximum capacity: {MaximumCapacity}. Must be >= 0.");
                return false;
            }
            
            if (MaximumCapacity > 0 && InitialCapacity > MaximumCapacity)
            {
                LogWarning($"Initial capacity ({InitialCapacity}) exceeds maximum capacity ({MaximumCapacity}).");
                return false;
            }
            
            if (ShrinkThreshold <= 0 || ShrinkThreshold >= 1)
            {
                LogWarning($"Invalid shrink threshold: {ShrinkThreshold}. Must be between 0 and 1.");
                return false;
            }
            
            if (ShrinkInterval <= 0)
            {
                LogWarning($"Invalid shrink interval: {ShrinkInterval}. Must be > 0.");
                return false;
            }
            
            if (GrowthFactor <= 1 && UseExponentialGrowth)
            {
                LogWarning($"Invalid growth factor: {GrowthFactor}. Must be > 1 for exponential growth.");
                return false;
            }
            
            if (GrowthIncrement <= 0 && !UseExponentialGrowth)
            {
                LogWarning($"Invalid growth increment: {GrowthIncrement}. Must be > 0 for linear growth.");
                return false;
            }
            
            // Particle system specific validation
            if (DefaultLoopingEffectDuration <= 0)
            {
                LogWarning($"Invalid default looping effect duration: {DefaultLoopingEffectDuration}. Must be > 0.");
                return false;
            }
            
            if (PrewarmSimulationTime < 0)
            {
                LogWarning($"Invalid prewarm simulation time: {PrewarmSimulationTime}. Must be >= 0.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Logs a warning message if warning logging is enabled.
        /// </summary>
        /// <param name="message">Warning message to log</param>
        public void LogWarning(string message)
        {
            if (!LogWarnings)
                return;
                
            if (_logger != null)
            {
                _logger.LogWarningInstance($"[ParticleSystemPoolConfig] {message}");
            }
            else
            {
                Debug.LogWarning($"[ParticleSystemPoolConfig] {message}");
            }
        }
        
        /// <summary>
        /// Disposes any unmanaged resources held by this configuration.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Release any native resources
            
            _isDisposed = true;
        }
        
        #endregion
    }
}