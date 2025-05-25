using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Managed;
using AhBearStudios.Pooling.Core.Pooling.Native;
using Unity.Collections;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles semaphore-limited pools.
    /// Semaphore pools limit concurrent active items for resource management.
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a semaphore pool that wraps an existing pool and limits concurrent access
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="innerPool">The underlying pool to limit access to</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool</returns>
        public SemaphorePool<T> CreateSemaphorePool<T>(
            IPool<T> innerPool,
            int maxConcurrency,
            string poolName = null)
        {
            EnsureInitialized();

            if (innerPool == null)
                throw new ArgumentNullException(nameof(innerPool));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"SemaphorePool_{innerPool.PoolName}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> semaphorePool)
                {
                    _logger?.LogInfo($"Returning existing semaphore pool: {name}");
                    return semaphorePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphorePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // Create new pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited object pool with specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool</returns>
        public SemaphorePool<T> CreateSemaphorePool<T>(
            Func<T> factory,
            int maxConcurrency,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"SemaphorePool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> semaphorePool)
                {
                    _logger?.LogInfo($"Returning existing semaphore pool: {name}");
                    return semaphorePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphorePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // First create the inner object pool
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                ThreadingMode = PoolThreadingMode.ThreadSafe, // Ensure thread safety for the inner pool
                CollectMetrics = _collectMetrics
            };
            
            var innerPoolName = $"Inner_{name}";
            var innerPool = new ManagedPool<T>(factory, resetAction, config, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited object pool using configuration object
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool</returns>
        public SemaphorePool<T> CreateSemaphorePool<T>(
            Func<T> factory,
            int maxConcurrency,
            PoolConfig config,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"SemaphorePool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> semaphorePool)
                {
                    _logger?.LogInfo($"Returning existing semaphore pool: {name}");
                    return semaphorePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphorePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // Ensure thread safety for the inner pool
            var threadSafeConfig = config.Clone();
            threadSafeConfig.ThreadingMode = PoolThreadingMode.ThreadSafe;
            
            // Create the inner object pool
            var innerPoolName = $"Inner_{name}";
            var innerPool = new ManagedPool<T>(factory, resetAction, threadSafeConfig, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited value type pool 
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool for value types</returns>
        public SemaphorePool<T> CreateSemaphoreValueTypePool<T>(
            Func<T> factory,
            int maxConcurrency,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"SemaphoreValuePool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> semaphorePool)
                {
                    _logger?.LogInfo($"Returning existing semaphore value pool: {name}");
                    return semaphorePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphoreValueTypePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // First create the inner value type pool
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                ThreadingMode = PoolThreadingMode.ThreadSafe, // Ensure thread safety for the inner pool
                CollectMetrics = _collectMetrics
            };
            
            var innerPoolName = $"Inner_{name}";
            var innerPool = new ThreadSafeValueTypePool<T>(factory, config, resetAction, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited native pool that can be used with the Jobs system
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool for native values</returns>
        public SemaphorePool<T> CreateSemaphoreNativePool<T>(
            int maxConcurrency,
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");
                
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
            
            if (allocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp is not supported for pools as it's too short-lived", nameof(allocator));

            string name = poolName ?? $"SemaphoreNativePool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (NativeRegistry.TryGetPoolByName(name, out INativePool nativePool))
            {
                if (nativePool is SemaphorePool<T> semaphoreNativePool)
                {
                    _logger?.LogInfo($"Returning existing semaphore native pool: {name}");
                    return semaphoreNativePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphoreNativePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // First create the inner native pool
            var innerPoolName = $"Inner_{name}";
            var innerPool = CreateNativePool(initialCapacity, allocator, defaultValue, maxSize, prewarm, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool with the registry
            Registry.RegisterPool(pool, name);
            NativeRegistry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited pool for GameObjects
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="worldPositionStays">Whether to preserve world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool for GameObjects</returns>
        public SemaphorePool<UnityEngine.GameObject> CreateSemaphoreGameObjectPool(
            UnityEngine.GameObject prefab,
            int maxConcurrency,
            UnityEngine.Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<UnityEngine.GameObject> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"SemaphoreGameObjectPool_{prefab.name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<UnityEngine.GameObject> semaphoreGameObjectPool)
                {
                    _logger?.LogInfo($"Returning existing semaphore GameObject pool: {name}");
                    return semaphoreGameObjectPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<GameObject>");
            }
            
            _logger?.LogInfo($"Creating new SemaphoreGameObjectPool with max concurrency {maxConcurrency}: {name}");
            
            // First create the inner GameObject pool
            var innerPoolName = $"Inner_{name}";
            var innerPool = CreateGameObjectPool(prefab, parent, initialCapacity, maxSize, prewarm, resetAction, worldPositionStays, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<UnityEngine.GameObject>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited pool for async operations
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Async factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool for async operations</returns>
        public SemaphorePool<T> CreateSemaphoreAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            int maxConcurrency,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"SemaphoreAsyncPool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> semaphoreAsyncPool)
                {
                    _logger?.LogInfo($"Returning existing semaphore async pool: {name}");
                    return semaphoreAsyncPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new SemaphoreAsyncPool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // Create the inner async object pool
            var innerPoolName = $"Inner_{name}";
            var innerPool = CreateAsyncPool(asyncFactory, initialCapacity, maxSize, prewarm, resetAction, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-limited pool that uses an advanced pool internally
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of items that can be active simultaneously</param>
        /// <param name="config">Advanced pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional function to validate objects before reuse</param>
        /// <param name="disposeAction">Optional action to dispose objects when released from pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool with an advanced inner pool</returns>
        public SemaphorePool<T> CreateAdvancedSemaphorePool<T>(
            Func<T> factory,
            int maxConcurrency,
            Advanced.AdvancedPoolConfig config,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            Action<T> disposeAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"AdvancedSemaphorePool_{typeof(T).Name}_{maxConcurrency}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is SemaphorePool<T> advancedSemaphorePool)
                {
                    _logger?.LogInfo($"Returning existing advanced semaphore pool: {name}");
                    return advancedSemaphorePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a SemaphorePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new AdvancedSemaphorePool<{typeof(T).Name}> with max concurrency {maxConcurrency}: {name}");
            
            // Create an advanced inner pool with thread safety enabled
            var threadSafeConfig = config.Clone();
            threadSafeConfig.EnableThreadSafety = true;
            
            var innerPoolName = $"Inner_{name}";
            var innerPool = CreateAdvancedPool(factory, threadSafeConfig, resetAction, validateAction, disposeAction, innerPoolName);
            
            // Create the semaphore pool that wraps the inner pool
            var pool = new SemaphorePool<T>(innerPool, maxConcurrency);
            
            // Register the pool (we only register the outer semaphore pool)
            Registry.RegisterPool(pool, name);
            
            return pool;
        }
    }
}