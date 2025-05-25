using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Managed;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using Unity.Collections;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles managed object pools
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a managed object pool with standard configuration
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if a pool with the same name already exists but is of a different type</exception>
        public ManagedPool<T> CreateManagedPool<T>(
            Func<T> factory,
            PoolConfig config = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"ManagedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ManagedPool<T> managedPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return managedPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ManagedPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ManagedPool<{typeof(T).Name}>: {name}");
            
            // Create the default config if none provided
            config ??= new PoolConfig
            {
                InitialCapacity = 10,
                MaxSize = 0,
                PrewarmOnInit = true
            };
            
            // Create new pool
            var pool = new ManagedPool<T>(factory, null, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            // Register with diagnostics if available
            if (_diagnostics != null)
            {
                _diagnostics.RegisterPool(pool, name);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a managed object pool with specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is negative</exception>
        /// <exception cref="InvalidOperationException">Thrown if a pool with the same name already exists but is of a different type</exception>
        public ManagedPool<T> CreateManagedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm
            };

            return CreateManagedPool(factory, config, resetAction, poolName);
        }

        /// <summary>
        /// Creates a managed object pool with custom configuration and reset action
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory or config is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if a pool with the same name already exists but is of a different type</exception>
        public ManagedPool<T> CreateManagedPool<T>(
            Func<T> factory,
            PoolConfig config,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"ManagedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ManagedPool<T> managedPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return managedPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ManagedPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ManagedPool<{typeof(T).Name}>: {name}");
            
            // Create new pool
            var pool = new ManagedPool<T>(factory, resetAction, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            // Register with diagnostics if available
            if (_diagnostics != null)
            {
                _diagnostics.RegisterPool(pool, name);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a specialized managed pool with custom validation and lifecycle management
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional action to validate objects before reuse</param>
        /// <param name="disposeAction">Optional action to dispose objects when released from pool</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new specialized ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if a pool with the same name already exists but is of a different type</exception>
        public ManagedPool<T> CreateSpecializedManagedPool<T>(
            Func<T> factory,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            Action<T> disposeAction = null,
            PoolConfig config = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"SpecializedManagedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ManagedPool<T> managedPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return managedPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ManagedPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SpecializedManagedPool<{typeof(T).Name}>: {name}");
            
            // Create the default config if none provided
            config ??= new PoolConfig
            {
                InitialCapacity = 10,
                MaxSize = 0,
                PrewarmOnInit = true
            };
            
            // Create specialized managed pool
            var pool = new ManagedPool<T>(factory, resetAction, config, name);
            
            // Set validation and disposal functions through extension methods if they exist
            // or add them to the pool's properties
            if (validateAction != null)
            {
                // Extension method to set validator (if available)
                if (pool is IValidatable<T> validatablePool)
                {
                    validatablePool.SetValidator(validateAction);
                }
                else
                {
                    _logger?.LogInfo($"Validation function provided but pool does not implement IValidatable<{typeof(T).Name}>");
                }
            }
            
            if (disposeAction != null)
            {
                // Extension method to set dispose action (if available)
                if (pool is IDisposable disposablePool)
                {
                    disposablePool.SetDisposeAction(disposeAction);
                }
                else
                {
                    _logger?.LogInfo($"Disposal function provided but pool does not implement IDisposable<{typeof(T).Name}>");
                }
            }
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            // Register with diagnostics if available
            if (_diagnostics != null)
            {
                _diagnostics.RegisterPool(pool, name);
            }
            
            return pool;
        }
        
        /// <summary>
        /// Creates a high-performance managed pool optimized for frequently used objects
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new high-performance ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null</exception>
        public ManagedPool<T> CreateHighPerformanceManagedPool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            // High-performance pools use a larger initial capacity and prewarming
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = 0, // Unlimited to avoid performance hits when growing
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                ResetOnRelease = resetAction != null,
                CollectMetrics = _collectMetrics
            };
            
            // Use a custom name if none provided
            if (string.IsNullOrEmpty(poolName))
            {
                poolName = $"HighPerformanceManagedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
            
            return CreateManagedPool(factory, config, resetAction, poolName);
        }
        
        /// <summary>
        /// Creates a managed pool with auto-shrinking capability to optimize memory usage
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="shrinkThreshold">Usage ratio threshold below which the pool will shrink</param>
        /// <param name="shrinkInterval">Time in seconds between shrink operations</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new auto-shrinking ManagedPool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if shrinkThreshold is not between 0 and 1</exception>
        public ManagedPool<T> CreateAutoShrinkManagedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            if (shrinkThreshold < 0f || shrinkThreshold > 1f)
                throw new ArgumentOutOfRangeException(nameof(shrinkThreshold), "Shrink threshold must be between 0 and 1");
                
            // Configure auto-shrinking pool
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = 0,
                PrewarmOnInit = true,
                EnableAutoShrink = true,
                ShrinkThreshold = shrinkThreshold,
                ShrinkInterval = shrinkInterval,
                ResetOnRelease = resetAction != null,
                CollectMetrics = _collectMetrics
            };
            
            // Use a custom name if none provided
            if (string.IsNullOrEmpty(poolName))
            {
                poolName = $"AutoShrinkManagedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
            
            return CreateManagedPool(factory, config, resetAction, poolName);
        }
        
        /// <summary>
        /// Checks if a managed pool exists for the specified type
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <returns>True if a managed pool exists for the specified type, false otherwise</returns>
        public bool HasManagedPool<T>() where T : class
        {
            EnsureInitialized();
            return Registry.TryGetPoolByType<T>(out _);
        }
        
        /// <summary>
        /// Gets an existing managed pool for the specified type
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <returns>The managed pool if it exists, null otherwise</returns>
        public ManagedPool<T> GetManagedPool<T>() where T : class
        {
            EnsureInitialized();
            
            if (Registry.TryGetPoolByType<T>(out var pool) && pool is ManagedPool<T> managedPool)
            {
                return managedPool;
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets or creates a managed pool for the specified type
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="factory">Factory function to create new instances if pool doesn't exist</param>
        /// <param name="config">Optional pool configuration if pool needs to be created</param>
        /// <returns>The managed pool</returns>
        /// <exception cref="ArgumentNullException">Thrown if factory is null and pool doesn't exist</exception>
        public ManagedPool<T> GetOrCreateManagedPool<T>(Func<T> factory = null, PoolConfig config = null) where T : class
        {
            EnsureInitialized();
            
            if (Registry.TryGetPoolByType<T>(out var pool) && pool is ManagedPool<T> managedPool)
            {
                return managedPool;
            }
            
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory), 
                    $"No managed pool exists for type {typeof(T).Name} and no factory function was provided to create one");
            }
            
            return CreateManagedPool(factory, config);
        }
    }
}