using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Pools.Advanced;
using AhBearStudios.Core.Pooling.Pools.Unity;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Unity
{
    /// <summary>
    /// Partial class for PoolManager that handles component-based pools
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a component pool with standard configuration
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="resetAction">Optional action to reset components when returned to pool</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool</returns>
        public ComponentPool<T> CreateComponentPool<T>(
            T prefab,
            Transform parent = null,
            PoolConfig config = null,
            Action<T> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"ComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ComponentPool<T> componentPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return componentPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ComponentPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ComponentPool<{typeof(T).Name}>: {name}");
            
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
            var pool = new ComponentPool<T>(prefab, parent, config, name, resetAction, worldPositionStays);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a component pool with specified parameters
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset components when returned to pool</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool</returns>
        public ComponentPool<T> CreateComponentPool<T>(
            T prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm
            };

            return CreateComponentPool(prefab, parent, config, resetAction, worldPositionStays, poolName);
        }

        /// <summary>
        /// Creates a UI component pool with UI-specific defaults
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform for spawned instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset components when returned to pool</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting (defaults to false for UI)</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool configured for UI components</returns>
        public ComponentPool<T> CreateUIComponentPool<T>(
            T prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            bool worldPositionStays = false,
            string poolName = null) where T : Component
        {
            poolName = poolName ?? $"UIComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating UI component pool: {poolName}");
            
            // Use default implementation but with UI-specific defaults
            return CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                poolName);
        }

        /// <summary>
        /// Creates a particle system pool with auto-release functionality
        /// </summary>
        /// <param name="prefab">Particle system prefab</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseOnComplete">Whether to automatically release particles when completed</param>
        /// <param name="customResetAction">Optional custom reset action</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ParticleSystem ComponentPool</returns>
        public ComponentPool<ParticleSystem> CreateAutoReleaseParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            bool autoReleaseOnComplete = true,
            Action<ParticleSystem> customResetAction = null,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"ParticleSystemPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating auto-release particle system pool: {name}");
            
            // Create a reset action that incorporates the auto-release behavior
            Action<ParticleSystem> resetAction = ps => 
            {
                // Apply custom reset if provided
                customResetAction?.Invoke(ps);
                
                // Default reset behavior
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            };
            
            // Create the pool with standard parameters
            var pool = CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                name);
            
            // Set up auto-release functionality if requested
            if (autoReleaseOnComplete)
            {
                _logger?.LogInfo($"Setting up auto-release for particle system pool: {name}");
                
                // Use a custom acquire logic that monitors for completion and auto-returns
                var originalAcquire = pool.Acquire;
                pool.SetCustomAcquireHandler(original =>
                {
                    var ps = originalAcquire();
                    if (ps != null)
                    {
                        ps.gameObject.SetActive(true);
                        
                        // Use coroutine runner to check for completion
                        _coroutineRunner.StartCoroutine(MonitorParticleSystemCompletion(ps, pool));
                    }
                    return ps;
                });
            }
            
            return pool;
        }

        /// <summary>
        /// Creates an advanced component pool with extended configuration options
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="config">Advanced pool configuration</param>
        /// <param name="resetAction">Optional action to reset components</param>
        /// <param name="validateAction">Optional action to validate components before reuse</param>
        /// <param name="onAcquireAction">Optional action to perform when a component is acquired</param>
        /// <param name="onReleaseAction">Optional action to perform when a component is released</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool with advanced configuration</returns>
        public ComponentPool<T> CreateAdvancedComponentPool<T>(
            T prefab,
            Transform parent = null,
            AdvancedPoolConfig config = null,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            Action<T> onAcquireAction = null,
            Action<T> onReleaseAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"AdvancedComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating advanced component pool: {name}");
            
            // Use default advanced config if none provided
            if (config == null)
            {
                config = new AdvancedPoolConfig
                {
                    InitialCapacity = 10,
                    MaxSize = 0,
                    PrewarmOnInit = true,
                    EnableAutoExpand = true,
                    EnablePriorityAcquisition = false
                };
            }
            
            // Convert from AdvancedPoolConfig to standard PoolConfig
            var standardConfig = new PoolConfig
            {
                InitialCapacity = config.InitialCapacity,
                MaxSize = config.MaxSize,
                PrewarmOnInit = config.PrewarmOnInit,
                UseExponentialGrowth = config.UseExponentialGrowth,
                GrowthFactor = config.GrowthFactor,
                EnableAutoShrink = config.EnableAutoShrink,
                ShrinkThreshold = config.ShrinkThreshold,
                ShrinkInterval = config.ShrinkInterval,
                CollectMetrics = config.CollectMetrics,
                DetailedLogging = config.DetailedLogging
            };
            
            // Create the base component pool
            var pool = CreateComponentPool(
                prefab,
                parent,
                standardConfig,
                resetAction,
                worldPositionStays,
                name);
            
            // Add validation if provided
            if (validateAction != null)
            {
                pool.SetValidator(validateAction);
            }
            
            // Add custom acquire action if provided
            if (onAcquireAction != null)
            {
                var originalAcquire = pool.Acquire;
                pool.SetCustomAcquireHandler(original =>
                {
                    var component = originalAcquire();
                    onAcquireAction?.Invoke(component);
                    return component;
                });
            }
            
            // Add custom release action if provided
            if (onReleaseAction != null)
            {
                var originalRelease = pool.Release;
                pool.SetCustomReleaseHandler(component =>
                {
                    onReleaseAction?.Invoke(component);
                    originalRelease(component);
                });
            }
            
            return pool;
        }

        /// <summary>
        /// Creates an animator component pool with state management
        /// </summary>
        /// <param name="prefab">Animator prefab</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="defaultState">Optional default animation state to reset to</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new Animator ComponentPool</returns>
        public ComponentPool<Animator> CreateAnimatorPool(
            Animator prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            string defaultState = null,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"AnimatorPool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating animator pool: {name} with default state: {defaultState ?? "None"}");
            
            // Define the reset action for animators
            Action<Animator> resetAction = animator =>
            {
                animator.gameObject.SetActive(false);
                animator.enabled = true;
                animator.speed = 1.0f;
                animator.Rebind();
                animator.ResetTrigger(0); // Reset all triggers
                
                // Play default state if specified
                if (!string.IsNullOrEmpty(defaultState))
                {
                    animator.Play(defaultState, 0, 0);
                }
            };
            
            return CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                name);
        }

        /// <summary>
        /// Creates an audio source pool with auto-release functionality
        /// </summary>
        /// <param name="prefab">AudioSource prefab</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseOnComplete">Whether to auto-release sources when finished playing</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new AudioSource ComponentPool</returns>
        public ComponentPool<AudioSource> CreateAudioSourcePool(
            AudioSource prefab,
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

            string name = poolName ?? $"AudioSourcePool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating audio source pool: {name}");
            
            // Define reset action for audio sources
            Action<AudioSource> resetAction = audioSource =>
            {
                audioSource.Stop();
                audioSource.clip = null;
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;
                audioSource.loop = false;
                audioSource.spatialBlend = 0.0f;
                audioSource.priority = 128;
                audioSource.gameObject.SetActive(false);
            };
            
            var pool = CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                name);
            
            // Set up auto-release functionality if requested
            if (autoReleaseOnComplete)
            {
                _logger?.LogInfo($"Setting up auto-release for audio source pool: {name}");
                
                // Use a custom acquire logic that monitors for completion and auto-returns
                var originalAcquire = pool.Acquire;
                pool.SetCustomAcquireHandler(original =>
                {
                    var audioSource = originalAcquire();
                    if (audioSource != null && !audioSource.loop)
                    {
                        audioSource.gameObject.SetActive(true);
                        
                        // Use coroutine runner to check for completion
                        _coroutineRunner.StartCoroutine(MonitorAudioSourceCompletion(audioSource, pool));
                    }
                    return audioSource;
                });
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a thread-safe component pool with semaphore limiting
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="maxConcurrency">Maximum number of concurrent borrows</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset components</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool with thread safety</returns>
        public SemaphorePool<T> CreateThreadSafeComponentPool<T>(
            T prefab,
            Transform parent = null,
            int maxConcurrency = 4,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"ThreadSafeComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-safe component pool: {name} with max concurrency: {maxConcurrency}");
            
            // First create the inner component pool
            var innerPool = CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                $"{name}_Inner");
            
            // Wrap it with a semaphore pool for thread safety
            return CreateSemaphorePool(innerPool, maxConcurrency, name);
        }

        /// <summary>
        /// Creates a component pool with physics component reset functionality
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetPhysics">Whether to reset physics properties</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool for physics components</returns>
        public ComponentPool<T> CreatePhysicsComponentPool<T>(
            T prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            bool resetPhysics = true,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"PhysicsComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating physics component pool: {name}");
            
            // Create reset action that handles physics components
            Action<T> resetAction = component =>
            {
                if (!resetPhysics)
                    return;
                
                // Reset position/rotation
                component.transform.localPosition = Vector3.zero;
                component.transform.localRotation = Quaternion.identity;
                
                // Reset Rigidbody if present
                var rigidbody = component.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.Sleep();
                }
                
                // Reset Rigidbody2D if present
                var rigidbody2D = component.GetComponent<Rigidbody2D>();
                if (rigidbody2D != null)
                {
                    rigidbody2D.linearVelocity = Vector2.zero;
                    rigidbody2D.angularVelocity = 0f;
                    rigidbody2D.Sleep();
                }
                
                // Deactivate gameObject
                component.gameObject.SetActive(false);
            };
            
            return CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                name);
        }

        /// <summary>
        /// Creates a component pool with auto-release based on a condition
        /// </summary>
        /// <typeparam name="T">Type of component to pool</typeparam>
        /// <param name="prefab">Prefab containing the component</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="autoReleaseCondition">Function that determines when to auto-release</param>
        /// <param name="checkInterval">Time between condition checks in seconds</param>
        /// <param name="resetAction">Optional action to reset components</param>
        /// <param name="worldPositionStays">Whether to maintain world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComponentPool with auto-release functionality</returns>
        public ComponentPool<T> CreateAutoReleaseComponentPool<T>(
            T prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Func<T, bool> autoReleaseCondition = null,
            float checkInterval = 1.0f,
            Action<T> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (autoReleaseCondition == null)
                throw new ArgumentNullException(nameof(autoReleaseCondition), "Auto-release condition cannot be null");

            if (checkInterval <= 0)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), "Check interval must be positive");

            string name = poolName ?? $"AutoReleaseComponentPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating auto-release component pool: {name} with check interval: {checkInterval}s");
            
            // Create the base component pool
            var pool = CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                resetAction,
                worldPositionStays,
                name);
            
            // Track active components for auto-release checking
            var activeComponents = new List<T>();
            
            // Override acquire to track active components
            var originalAcquire = pool.Acquire;
            pool.SetCustomAcquireHandler(original =>
            {
                var component = originalAcquire();
                if (component != null)
                {
                    lock (activeComponents)
                    {
                        activeComponents.Add(component);
                    }
                    
                    // Start auto-release monitoring if this is the first item
                    if (activeComponents.Count == 1)
                    {
                        _coroutineRunner.StartCoroutine(MonitorComponentsForAutoRelease(
                            pool, 
                            activeComponents, 
                            autoReleaseCondition, 
                            checkInterval));
                    }
                }
                return component;
            });
            
            // Override release to stop tracking released components
            var originalRelease = pool.Release;
            pool.SetCustomReleaseHandler(component =>
            {
                lock (activeComponents)
                {
                    activeComponents.Remove(component);
                }
                originalRelease(component);
            });
            
            return pool;
        }

        #region Helper Coroutines

        /// <summary>
        /// Coroutine to monitor a particle system and auto-release it when complete
        /// </summary>
        private System.Collections.IEnumerator MonitorParticleSystemCompletion(ParticleSystem ps, ComponentPool<ParticleSystem> pool)
        {
            if (ps == null || pool == null || pool.IsDisposed)
                yield break;
            
            // Wait until particle system is not playing
            while (ps.isPlaying && ps.gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // Auto-release if still valid
            if (ps != null && pool != null && !pool.IsDisposed && ps.gameObject.activeInHierarchy)
            {
                pool.Release(ps);
                _logger?.LogDebug($"Auto-released particle system to pool: {pool.PoolName}");
            }
        }
        
        /// <summary>
        /// Coroutine to monitor an audio source and auto-release it when complete
        /// </summary>
        private System.Collections.IEnumerator MonitorAudioSourceCompletion(AudioSource audioSource, ComponentPool<AudioSource> pool)
        {
            if (audioSource == null || pool == null || pool.IsDisposed)
                yield break;
            
            // Wait until audio is not playing
            while (audioSource.isPlaying && audioSource.gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // Auto-release if still valid
            if (audioSource != null && pool != null && !pool.IsDisposed && audioSource.gameObject.activeInHierarchy)
            {
                pool.Release(audioSource);
                _logger?.LogDebug($"Auto-released audio source to pool: {pool.PoolName}");
            }
        }
        
        /// <summary>
        /// Coroutine to monitor components and auto-release them based on a condition
        /// </summary>
        private System.Collections.IEnumerator MonitorComponentsForAutoRelease<T>(
            ComponentPool<T> pool,
            List<T> activeComponents,
            Func<T, bool> autoReleaseCondition,
            float checkInterval) where T : Component
        {
            if (pool == null || activeComponents == null || autoReleaseCondition == null)
                yield break;
            
            while (pool != null && !pool.IsDisposed && activeComponents.Count > 0)
            {
                var componentsToRelease = new List<T>();
                
                // Check which components should be released
                lock (activeComponents)
                {
                    for (int i = activeComponents.Count - 1; i >= 0; i--)
                    {
                        var component = activeComponents[i];
                        if (component != null && autoReleaseCondition(component))
                        {
                            componentsToRelease.Add(component);
                        }
                    }
                }
                
                // Release components that meet the condition
                foreach (var component in componentsToRelease)
                {
                    if (component != null && pool != null && !pool.IsDisposed)
                    {
                        pool.Release(component);
                        _logger?.LogDebug($"Auto-released component to pool: {pool.PoolName}");
                    }
                }
                
                // Wait for next check
                yield return new WaitForSeconds(checkInterval);
            }
        }
        
        #endregion
    }
}