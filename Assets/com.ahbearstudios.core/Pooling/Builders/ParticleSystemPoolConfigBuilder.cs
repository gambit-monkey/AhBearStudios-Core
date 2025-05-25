using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Builder for particle system pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for Unity ParticleSystem pooling with a fluent API.
    /// </summary>
    public class
        ParticleSystemPoolConfigBuilder : IPoolConfigBuilder<ParticleSystemPoolConfig, ParticleSystemPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ParticleSystemPoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public ParticleSystemPoolConfigBuilder()
        {
            _config = new ParticleSystemPoolConfig();
        }
        
        /// <summary>
        /// Creates a new builder initialized with an existing particle system configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ParticleSystemPoolConfigBuilder(ParticleSystemPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = config.Clone() as ParticleSystemPoolConfig 
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder initialized with a generic pool configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ParticleSystemPoolConfigBuilder(IPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = new ParticleSystemPoolConfig(config);
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically clean up particles and resources when releasing to pool
        /// </summary>
        /// <param name="autoCleanup">Whether to automatically clean up on release</param>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder WithAutoCleanup(bool autoCleanup)
        {
            _config.StopOnRelease = autoCleanup;
            _config.ClearParticlesOnRelease = autoCleanup;
            _config.DisableEmissionOnRelease = autoCleanup;
            return this;
        }

        /// <summary>
        /// Sets whether to cache materials used by the particle systems
        /// </summary>
        /// <param name="cacheMaterials">Whether to cache materials</param>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder WithCacheMaterials(bool cacheMaterials)
        {
            // Materials caching is part of component caching
            _config.CacheComponents = cacheMaterials;
            return this;
        }

        /// <summary>
        /// Configures the pool for high-density particle systems
        /// </summary>
        /// <param name="optimize">Whether to enable high-density optimizations</param>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder WithHighDensityOptimization(bool optimize)
        {
            if (optimize)
            {
                _config.CacheComponents = true;
                _config.PrewarmParticles = true;
                _config.UseExponentialGrowth = true;
                _config.GrowthFactor = 2.0f;
                _config.StopOnRelease = true;
                _config.ClearParticlesOnRelease = true;
            }

            return this;
        }

        /// <summary>
        /// Configures the pool for low overhead operation (mobile-friendly)
        /// </summary>
        /// <param name="lowOverhead">Whether to enable low overhead mode</param>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder WithLowOverheadMode(bool lowOverhead)
        {
            if (lowOverhead)
            {
                _config.CacheComponents = true;
                _config.DetailedLogging = false;
                _config.CollectMetrics = false;
                _config.EnableAutoShrink = true;
                _config.UseExponentialGrowth = false;
                _config.ClearParticlesOnRelease = true;
            }

            return this;
        }

        /// <summary>
        /// Configures the pool for persistent particle effects
        /// </summary>
        /// <param name="persistent">Whether to enable persistent mode</param>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder WithPersistentMode(bool persistent)
        {
            if (persistent)
            {
                _config.StopOnRelease = !persistent;
                _config.ClearParticlesOnRelease = !persistent;
                _config.DisableEmissionOnRelease = !persistent;
                _config.DefaultLoopingEffectDuration = persistent ? 30.0f : 5.0f;
            }

            return this;
        }

        /// <summary>
        /// Sets the parent transform for pooled particle systems
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithParentTransform(Transform parent)
        {
            _config.ParentTransform = parent;
            _config.UseParentTransform = parent != null;
            return this;
        }

        /// <summary>
        /// Sets whether to pre-warm particle systems
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithParticlePrewarming(bool prewarm)
        {
            _config.PrewarmParticles = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to stop systems on release
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithStopOnRelease(bool stopOnRelease)
        {
            _config.StopOnRelease = stopOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to clear particles on release
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithClearParticlesOnRelease(bool clearOnRelease)
        {
            _config.ClearParticlesOnRelease = clearOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to disable emission on release
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithDisableEmissionOnRelease(bool disableEmission)
        {
            _config.DisableEmissionOnRelease = disableEmission;
            return this;
        }

        /// <summary>
        /// Sets whether to cache particle system components
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithComponentCaching(bool cacheComponents)
        {
            _config.CacheComponents = cacheComponents;
            return this;
        }

        /// <summary>
        /// Sets the default duration for looping effects
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithDefaultLoopingEffectDuration(float duration)
        {
            _config.DefaultLoopingEffectDuration = Mathf.Max(0.1f, duration);
            return this;
        }

        /// <summary>
        /// Sets whether the pool should prewarm on initialization
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for the pool
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to enable detailed logging for the pool
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings for the pool
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to reset particle systems on release
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets whether to automatically shrink the pool when usage falls below a threshold
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Sets the usage threshold below which the pool will be automatically shrunk
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithShrinkThreshold(float shrinkThreshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(shrinkThreshold);
            return this;
        }

        /// <summary>
        /// Sets the time interval between auto-shrink operations
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithShrinkInterval(float shrinkInterval)
        {
            _config.ShrinkInterval = Mathf.Max(1.0f, shrinkInterval);
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth for the pool
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential pool growth
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Mathf.Max(1.01f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets the fixed increment value for linear pool growth
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithGrowthIncrement(int growthIncrement)
        {
            _config.GrowthIncrement = Mathf.Max(1, growthIncrement);
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when exceeding max count
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        /// <summary>
        /// Sets whether to use parent transform for pooled particle systems
        /// </summary>
        public ParticleSystemPoolConfigBuilder WithUseParentTransform(bool useParentTransform)
        {
            _config.UseParentTransform = useParentTransform;
            return this;
        }
        
                /// <summary>
        /// Initializes this builder with settings from an existing configuration.
        /// Allows for fluent method chaining.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ParticleSystemPoolConfigBuilder FromExisting(ParticleSystemPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            
            // Copy common IPoolConfig properties
            _config.ConfigId = config.ConfigId;
            _config.InitialCapacity = config.InitialCapacity;
            _config.MinimumCapacity = config.MinimumCapacity;
            _config.MaximumCapacity = config.MaximumCapacity;
            _config.PrewarmOnInit = config.PrewarmOnInit;
            _config.CollectMetrics = config.CollectMetrics;
            _config.DetailedLogging = config.DetailedLogging;
            _config.LogWarnings = config.LogWarnings;
            _config.ResetOnRelease = config.ResetOnRelease;
            _config.ThreadingMode = config.ThreadingMode;
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;
            
            // Copy ParticleSystem-specific properties
            _config.UseParentTransform = config.UseParentTransform;
            _config.ParentTransform = config.ParentTransform;
            _config.PrewarmParticles = config.PrewarmParticles;
            _config.StopOnRelease = config.StopOnRelease;
            _config.ClearParticlesOnRelease = config.ClearParticlesOnRelease;
            _config.DefaultLoopingEffectDuration = config.DefaultLoopingEffectDuration;
            _config.DisableEmissionOnRelease = config.DisableEmissionOnRelease;
            _config.CacheComponents = config.CacheComponents;
            _config.OptimizeForOneShot = config.OptimizeForOneShot;
            _config.UseSafetyHandles = config.UseSafetyHandles;
            _config.UseSIMDAlignment = config.UseSIMDAlignment;
            _config.SimulateOnPrewarm = config.SimulateOnPrewarm;
            _config.OptimizeForCacheCoherence = config.OptimizeForCacheCoherence;
            _config.ValidateParticleState = config.ValidateParticleState;
            _config.PrewarmSimulationTime = config.PrewarmSimulationTime;
            
            return this;
        }
        
        /// <summary>
        /// Initializes this builder with settings from an existing IPoolConfig.
        /// For particle system specific settings, defaults will be used unless the source
        /// is a ParticleSystemPoolConfig.
        /// </summary>
        /// <param name="config">The configuration to copy settings from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ParticleSystemPoolConfigBuilder FromExisting(IPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            
            // Copy common IPoolConfig properties
            _config.ConfigId = config.ConfigId;
            _config.InitialCapacity = config.InitialCapacity;
            _config.MinimumCapacity = config.MinimumCapacity;
            _config.MaximumCapacity = config.MaximumCapacity;
            _config.PrewarmOnInit = config.PrewarmOnInit;
            _config.CollectMetrics = config.CollectMetrics;
            _config.DetailedLogging = config.DetailedLogging;
            _config.LogWarnings = config.LogWarnings;
            _config.ResetOnRelease = config.ResetOnRelease;
            _config.ThreadingMode = config.ThreadingMode;
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;
            
            // If source is a ParticleSystemPoolConfig, also copy those specific properties
            if (config is ParticleSystemPoolConfig particleConfig)
            {
                _config.CopyParticleSystemSpecificProperties(particleConfig);
            }
            
            return this;
        }

        /// <summary>
        /// Configures for one-shot effects optimization
        /// </summary>
        public ParticleSystemPoolConfigBuilder AsOneShotOptimized()
        {
            _config.OptimizeForOneShot = true;
            _config.ClearParticlesOnRelease = true;
            _config.StopOnRelease = true;
            _config.DisableEmissionOnRelease = true;
            _config.CacheComponents = true;
            return this;
        }

        /// <summary>
        /// Configures for reusable effects
        /// </summary>
        public ParticleSystemPoolConfigBuilder AsReusableEffects()
        {
            _config.StopOnRelease = true;
            _config.ClearParticlesOnRelease = true;
            _config.DisableEmissionOnRelease = false;
            _config.CacheComponents = true;
            _config.UseExponentialGrowth = true;
            return this;
        }

        /// <summary>
        /// Configures for high-performance settings
        /// </summary>
        public ParticleSystemPoolConfigBuilder AsHighPerformance()
        {
            _config.CacheComponents = true;
            _config.EnableAutoShrink = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.DetailedLogging = false;
            _config.PrewarmParticles = true;
            return this;
        }

        /// <summary>
        /// Configures for debugging mode
        /// </summary>
        public ParticleSystemPoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with thread-safe settings for particle systems
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder AsThreadSafe()
        {
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            _config.LogWarnings = true;
            _config.CollectMetrics = true;
            _config.ThrowIfExceedingMaxCount = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with memory-efficient settings for particle systems
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder AsMemoryEfficient()
        {
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 2;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.3f;
            _config.ShrinkInterval = 20.0f;
            _config.CacheComponents = false;
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.ClearParticlesOnRelease = true;
            return this;
        }

        /// <summary>
        /// Configures the builder with settings optimized for trail-type particle effects
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder AsTrailEffectsOptimized()
        {
            _config.CacheComponents = true;
            _config.StopOnRelease = true;
            _config.ClearParticlesOnRelease = true;
            _config.DisableEmissionOnRelease = true;
            _config.PrewarmParticles = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            return this;
        }

        /// <summary>
        /// Configures the builder with settings for heavy VFX systems with many particles
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder AsVfxHeavy()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 10);
            _config.CacheComponents = true;
            _config.PrewarmParticles = true;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 2;
            _config.StopOnRelease = true;
            _config.ClearParticlesOnRelease = true;
            _config.DefaultLoopingEffectDuration = 10.0f;
            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings suitable for most use cases
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ParticleSystemPoolConfigBuilder AsBalanced()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 8);
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.3f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.ShrinkInterval = 30.0f;
            _config.CacheComponents = true;
            _config.StopOnRelease = true;
            _config.ClearParticlesOnRelease = true;
            _config.DisableEmissionOnRelease = true;
            _config.ThreadingMode = PoolThreadingMode.SingleThreaded;
            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ParticleSystemPoolConfig Build()
        {
            ValidateConfiguration();
            var clone = _config.Clone() as ParticleSystemPoolConfig;
            return clone ?? throw new InvalidOperationException("Failed to clone configuration");
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

            if (_config.DefaultLoopingEffectDuration <= 0)
                throw new InvalidOperationException("Default looping effect duration must be positive");

            if (_config.UseParentTransform && _config.ParentTransform == null)
                Debug.LogWarning("Parent transform usage is enabled but no transform is set");
        }
    }
}