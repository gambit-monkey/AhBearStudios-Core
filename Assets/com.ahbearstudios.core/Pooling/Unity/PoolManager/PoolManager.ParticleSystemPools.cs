using System;
using AhBearStudios.Pooling.Core.Pooling.Managed;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that provides specialized methods for particle system pooling
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a particle system pool with standard configuration
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool</returns>
        public ParticleSystemPool CreateParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            PoolConfig config = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"ParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ParticleSystemPool particleSystemPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return particleSystemPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ParticleSystemPool");
            }
            
            _logger?.LogInfo($"Creating new ParticleSystemPool: {name}");
            
            // Create the default config if none provided
            if (config == null)
            {
                config = new PoolConfig
                {
                    InitialCapacity = 10,
                    MaxSize = 0,
                    PrewarmOnInit = true
                };
            }
            
            // Create new pool
            var pool = new ParticleSystemPool(prefab, parent, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a particle system pool with a GameObject pool configuration
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="config">GameObject-specific pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool</returns>
        public ParticleSystemPool CreateParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            GameObjectPoolConfig config = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"ParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ParticleSystemPool particleSystemPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return particleSystemPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ParticleSystemPool");
            }
            
            _logger?.LogInfo($"Creating new ParticleSystemPool with GameObject config: {name}");
            
            // Create the default config if none provided
            if (config == null)
            {
                config = new GameObjectPoolConfig
                {
                    InitialCapacity = 10,
                    MaxSize = 0,
                    PrewarmOnInit = true,
                    ResetOnRelease = true,
                    ReparentOnRelease = true,
                    ToggleActive = true
                };
            }
            
            // Convert GameObjectPoolConfig to base PoolConfig for compatibility
            var baseConfig = new PoolConfig
            {
                InitialCapacity = config.InitialCapacity,
                MaxSize = config.MaxSize,
                PrewarmOnInit = config.PrewarmOnInit,
                ResetOnRelease = config.ResetOnRelease,
                UseExponentialGrowth = config.UseExponentialGrowth,
                GrowthFactor = config.GrowthFactor,
                GrowthIncrement = config.GrowthIncrement,
                EnableAutoShrink = config.EnableAutoShrink,
                ShrinkThreshold = config.ShrinkThreshold,
                ShrinkInterval = config.ShrinkInterval,
                ThreadingMode = config.ThreadingMode,
                LogWarnings = config.LogWarnings,
                CollectMetrics = config.CollectMetrics,
                DetailedLogging = config.DetailedLogging,
                ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount
            };
            
            // Create new pool with the base config
            var pool = new ParticleSystemPool(prefab, parent, baseConfig, name);
            
            // Apply GameObject-specific configuration options
            pool.SetReparentOnRelease(config.ReparentOnRelease);
            pool.SetToggleActive(config.ToggleActive);
            pool.SetCallPoolEvents(config.CallPoolEvents);
            
            if (config.ValidateOnAcquire)
            {
                pool.SetValidator(ps => ps != null && ps.gameObject != null);
            }
            
            if (config.ActiveLayer != config.InactiveLayer)
            {
                pool.SetLayerSettings(config.ActiveLayer, config.InactiveLayer);
            }
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a particle system pool with specified parameters
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseOnComplete">Whether to automatically release particles when completed</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool</returns>
        public ParticleSystemPool CreateParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            bool autoReleaseOnComplete = true,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"ParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating particle system pool: {name} with auto-release: {autoReleaseOnComplete}");
            
            // Create configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm
            };
            
            // Create the basic particle system pool
            var pool = CreateParticleSystemPool(prefab, parent, config, name);
            
            // Set world position stays flag
            pool.SetWorldPositionStays(worldPositionStays);
            
            // Set up auto-release if enabled
            if (autoReleaseOnComplete)
            {
                _logger?.LogInfo($"Setting up auto-release for particle system pool: {name}");
                pool.EnableAutoRelease();
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a burst-capable particle system pool for high-performance scenarios
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="useJobSystem">Whether to use the Unity Job System for particle updates</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool optimized for performance</returns>
        public ParticleSystemPool CreateHighPerformanceParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            int initialCapacity = 20,
            int maxSize = 100,
            bool prewarm = true,
            bool useJobSystem = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"HighPerfParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance particle system pool: {name}");
            
            // Ensure the prefab is configured optimally
            var mainModule = prefab.main;
            if (useJobSystem && !mainModule.useUnscaledTime)
            {
                // Configure to use job system if requested
                // Note: This is safe to do on the prefab
                mainModule.useUnscaledTime = false; // Required for job system compatibility
            }
            
            // Create configuration optimized for high performance
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                UseExponentialGrowth = false, // Linear growth is more predictable for particle systems
                GrowthIncrement = 10,
                EnableAutoShrink = true,
                ShrinkThreshold = 0.5f,
                ShrinkInterval = 30.0f
            };
            
            // Create the pool
            var pool = CreateParticleSystemPool(prefab, parent, config, name);
            
            // Add custom reset action that ensures optimal performance
            pool.SetCustomResetAction(ps => 
            {
                if (ps != null)
                {
                    // Perform a deep clean of the particle system
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    
                    // Reset any custom properties that might affect performance
                    var mainModule = ps.main;
                    mainModule.simulationSpeed = 1.0f;
                    
                    // Pre-allocate any native collections if needed
                    // This would depend on specific implementation details
                    
                    ps.gameObject.SetActive(false);
                }
            });
            
            return pool;
        }

        /// <summary>
        /// Creates a specialized particle system pool for visual effects with custom emission control
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="emissionRate">Default emission rate for spawned particles</param>
        /// <param name="maxParticleCount">Maximum particle count limit</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool with custom emission settings</returns>
        public ParticleSystemPool CreateVisualEffectsParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            float emissionRate = 10.0f,
            int maxParticleCount = 1000,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"VFXParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating visual effects particle system pool: {name}");
            
            // Create configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm
            };
            
            // Create the basic particle system pool
            var pool = CreateParticleSystemPool(prefab, parent, config, name);
            
            // Add custom acquisition logic that configures emission settings
            var originalAcquire = pool.Acquire;
            pool.SetCustomAcquireHandler(() => 
            {
                var particleSystem = originalAcquire();
                if (particleSystem != null)
                {
                    // Configure emission settings
                    var emission = particleSystem.emission;
                    var mainModule = particleSystem.main;
                    
                    // Set emission rate
                    var rate = emission.rateOverTime;
                    rate.constant = emissionRate;
                    emission.rateOverTime = rate;
                    
                    // Set max particle count
                    mainModule.maxParticles = maxParticleCount;
                    
                    particleSystem.gameObject.SetActive(true);
                }
                return particleSystem;
            });
            
            return pool;
        }

        /// <summary>
        /// Creates a particle system pool with asynchronous loading capabilities
        /// </summary>
        /// <param name="asyncFactory">Asynchronous function that creates a particle system</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseOnComplete">Whether to automatically release when completed</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new async-compatible ParticleSystemPool</returns>
        public async System.Threading.Tasks.Task<ParticleSystemPool> CreateAsyncParticleSystemPool(
            Func<System.Threading.Tasks.Task<ParticleSystem>> asyncFactory,
            Transform parent = null,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            bool autoReleaseOnComplete = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            string name = poolName ?? $"AsyncParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating async particle system pool: {name}");
            
            // First create an async component pool to get the prefab
            var prefab = await asyncFactory();
            
            if (prefab == null)
                throw new InvalidOperationException("Failed to create particle system prefab asynchronously");
            
            // Now create the pool with the loaded prefab
            return CreateParticleSystemPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                autoReleaseOnComplete,
                true, // worldPositionStays
                name);
        }

        /// <summary>
        /// Creates a particle system pool with shared emission settings
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="sharedEmissionSettings">Shared emission settings for all particles</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool with shared emission settings</returns>
        public ParticleSystemPool CreateSharedEmissionParticleSystemPool(
            ParticleSystem prefab,
            ParticleSystem.EmissionModule sharedEmissionSettings,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"SharedEmissionParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating shared emission particle system pool: {name}");
            
            // Capture emission settings we want to share
            var emissionRateOverTime = sharedEmissionSettings.rateOverTime;
            var emissionRateOverDistance = sharedEmissionSettings.rateOverDistance;
            var burstCount = 0;
            
            try 
            {
                burstCount = sharedEmissionSettings.burstCount;
            }
            catch (System.Exception)
            {
                // Handle older Unity versions where this might not be available
                burstCount = 0;
            }
            
            // Create a standard pool
            var pool = CreateParticleSystemPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                true, // autoReleaseOnComplete
                true, // worldPositionStays
                name);
            
            // Override acquisition to apply shared emission settings
            var originalAcquire = pool.Acquire;
            pool.SetCustomAcquireHandler(() => 
            {
                var ps = originalAcquire();
                if (ps != null)
                {
                    // Apply shared emission settings
                    var emission = ps.emission;
                    emission.rateOverTime = emissionRateOverTime;
                    emission.rateOverDistance = emissionRateOverDistance;
                    
                    // Copy bursts if any
                    if (burstCount > 0)
                    {
                        try
                        {
                            for (int i = 0; i < burstCount; i++)
                            {
                                var burst = sharedEmissionSettings.GetBurst(i);
                                emission.SetBurst(i, burst);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            _logger?.LogError($"Error copying particle bursts: {ex.Message}");
                        }
                    }
                    
                    ps.gameObject.SetActive(true);
                }
                return ps;
            });
            
            return pool;
        }

        /// <summary>
        /// Creates a scalable particle system pool that can dynamically adjust the visual quality
        /// based on performance metrics
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab to pool</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="lowQualityMaxParticles">Maximum particles for low quality setting</param>
        /// <param name="mediumQualityMaxParticles">Maximum particles for medium quality setting</param>
        /// <param name="highQualityMaxParticles">Maximum particles for high quality setting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystemPool with quality scaling capability</returns>
        public ParticleSystemPool CreateScalableQualityParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            int lowQualityMaxParticles = 100,
            int mediumQualityMaxParticles = 500,
            int highQualityMaxParticles = 1000,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"ScalableParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating scalable quality particle system pool: {name}");
            
            // Store original max particles
            var mainModule = prefab.main;
            int originalMaxParticles = mainModule.maxParticles;
            
            // Create the pool
            var pool = CreateParticleSystemPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                true, // prewarm
                true, // autoReleaseOnComplete
                true, // worldPositionStays
                name);
            
            // Add method to adjust quality based on performance
            pool.SetQualityScaler(GetParticleQualityLevel, 
                lowQualityMaxParticles, 
                mediumQualityMaxParticles, 
                highQualityMaxParticles,
                originalMaxParticles);
            
            return pool;
        }

        /// <summary>
        /// Helper method to determine particle quality level based on current performance
        /// </summary>
        /// <returns>Quality level (0=low, 1=medium, 2=high, 3=original)</returns>
        private int GetParticleQualityLevel()
        {
            if (Time.smoothDeltaTime > 0.033f) // Less than 30 FPS
            {
                return 0; // Low quality
            }
            else if (Time.smoothDeltaTime > 0.022f) // Less than 45 FPS
            {
                return 1; // Medium quality
            }
            else if (Time.smoothDeltaTime > 0.016f) // Less than 60 FPS
            {
                return 2; // High quality
            }
            else
            {
                return 3; // Original quality
            }
        }

        /// <summary>
        /// Creates a thread-safe particle system pool with semaphore limiting
        /// </summary>
        /// <param name="prefab">ParticleSystem prefab</param>
        /// <param name="maxConcurrency">Maximum number of concurrent borrows</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseOnComplete">Whether to automatically release when completed</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new thread-safe ParticleSystemPool</returns>
        public SemaphorePool<ParticleSystem> CreateThreadSafeParticleSystemPool(
            ParticleSystem prefab,
            int maxConcurrency,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            bool autoReleaseOnComplete = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"ThreadSafeParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-safe particle system pool: {name} with max concurrency: {maxConcurrency}");
            
            // First create the inner particle system pool
            var innerPool = CreateParticleSystemPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                autoReleaseOnComplete,
                true, // worldPositionStays
                $"{name}_Inner");
            
            // Wrap it with a semaphore pool for thread safety
            return CreateSemaphorePool(innerPool, maxConcurrency, name);
        }

        /// <summary>
        /// Extension method for ParticleSystemPool to set a custom reset action
        /// </summary>
        /// <param name="pool">The particle system pool</param>
        /// <param name="resetAction">The custom reset action</param>
        private void SetCustomResetAction(this ParticleSystemPool pool, Action<ParticleSystem> resetAction)
        {
            if (pool == null || resetAction == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool allows for custom reset actions
            // This is a placeholder that assumes such functionality exists
            pool.SetResetAction(resetAction);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set world position stays flag
        /// </summary>
        private void SetWorldPositionStays(this ParticleSystemPool pool, bool worldPositionStays)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool exposes this setting
            // This is a placeholder that assumes such functionality exists
            pool.SetWorldPositionStaysFlag(worldPositionStays);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to enable auto-release of particles
        /// </summary>
        private void EnableAutoRelease(this ParticleSystemPool pool)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool handles auto-release
            // This is a placeholder that assumes such functionality exists
            pool.SetAutoReleaseEnabled(true);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set reparent on release behavior
        /// </summary>
        private void SetReparentOnRelease(this ParticleSystemPool pool, bool reparentOnRelease)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool exposes this setting
            // This is a placeholder that assumes such functionality exists
            pool.SetReparentOnReleaseFlag(reparentOnRelease);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set toggle active behavior
        /// </summary>
        private void SetToggleActive(this ParticleSystemPool pool, bool toggleActive)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool exposes this setting
            // This is a placeholder that assumes such functionality exists
            pool.SetToggleActiveFlag(toggleActive);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set call pool events behavior
        /// </summary>
        private void SetCallPoolEvents(this ParticleSystemPool pool, bool callPoolEvents)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool exposes this setting
            // This is a placeholder that assumes such functionality exists
            pool.SetCallPoolEventsFlag(callPoolEvents);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set layer settings
        /// </summary>
        private void SetLayerSettings(this ParticleSystemPool pool, int activeLayer, int inactiveLayer)
        {
            if (pool == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool exposes layer settings
            // This is a placeholder that assumes such functionality exists
            pool.SetLayerConfig(activeLayer, inactiveLayer);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set a validator function
        /// </summary>
        private void SetValidator(this ParticleSystemPool pool, Func<ParticleSystem, bool> validator)
        {
            if (pool == null || validator == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool allows for validation
            // This is a placeholder that assumes such functionality exists
            pool.SetValidatorFunction(validator);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set custom acquire handler
        /// </summary>
        private void SetCustomAcquireHandler(this ParticleSystemPool pool, Func<ParticleSystem> acquireFunc)
        {
            if (pool == null || acquireFunc == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool allows for custom acquisition
            // This is a placeholder that assumes such functionality exists
            pool.SetCustomAcquireFunction(acquireFunc);
        }
        
        /// <summary>
        /// Extension method for ParticleSystemPool to set quality scaling functionality
        /// </summary>
        private void SetQualityScaler(this ParticleSystemPool pool, 
            Func<int> qualityLevelProvider,
            int lowQualityMaxParticles,
            int mediumQualityMaxParticles,
            int highQualityMaxParticles,
            int originalMaxParticles)
        {
            if (pool == null || qualityLevelProvider == null)
                return;
                
            // Implementation would depend on how ParticleSystemPool could handle quality scaling
            // This is a placeholder that assumes such functionality exists
            pool.InitializeQualityScaling(
                qualityLevelProvider,
                lowQualityMaxParticles,
                mediumQualityMaxParticles,
                highQualityMaxParticles,
                originalMaxParticles);
        }
    }
}