using System;
using System.Threading.Tasks;
using UnityEngine;
using AhBearStudios.Pooling.Core.Pooling.Adapters;
using AhBearStudios.Pooling.Core.Pooling.Async;
using AhBearStudios.Pooling.Core.Pooling.Core;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles async object pools using the AsyncPoolAdapter
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates an async pool by wrapping any IPool with an AsyncPoolAdapter
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="basePool">The base pool to adapt to async operations</param>
        /// <param name="asyncFactory">Optional async factory function to create new instances</param>
        /// <param name="ownsPool">Whether the adapter should dispose the base pool when disposed</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An IAsyncPool that wraps the provided base pool</returns>
        public IAsyncPool<T> CreateAsyncPool<T>(
            IPool<T> basePool,
            Func<Task<T>> asyncFactory = null,
            bool ownsPool = false,
            string poolName = null)
        {
            EnsureInitialized();

            if (basePool == null)
                throw new ArgumentNullException(nameof(basePool));

            string name = poolName ?? $"AsyncPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is IAsyncPool<T> asyncPool)
                {
                    _logger?.LogInfoInstance($"Returning existing async pool: {name}");
                    return asyncPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not an AsyncPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfoInstance($"Creating new AsyncPool<{typeof(T).Name}> adapter: {name}");
            
            // Create a new async pool adapter
            var asyncAdapter = asyncFactory != null
                ? new AsyncPoolAdapter<T>(basePool, (ct) => asyncFactory(), ownsPool)
                : new AsyncPoolAdapter<T>(basePool, ownsPool);
            
            // Register the pool
            Registry.RegisterPool(asyncAdapter, name);
            
            return asyncAdapter;
        }

        /// <summary>
        /// Creates an async object pool
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Async factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An AsyncPool that wraps a standard object pool</returns>
        public IAsyncPool<T> CreateAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            // First create a standard pool with a synchronous factory that blocks on the async factory
            var syncFactory = new Func<T>(() => asyncFactory().GetAwaiter().GetResult());
            // Using IPool<T> interface to avoid specific pool type issues
            IPool<T> basePool = CreatePool(syncFactory, initialCapacity, maxSize, prewarm, resetAction, null);
            
            // Now wrap it with an async adapter - explicitly specify the type parameter
            return CreateAsyncPool<T>(basePool, asyncFactory, true, poolName);
        }

        /// <summary>
        /// Creates an async GameObject pool
        /// </summary>
        /// <param name="asyncFactory">Async factory function to create new GameObjects</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset GameObjects when returned to pool</param>
        /// <param name="worldPositionStays">Whether to preserve world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An AsyncPool that wraps a GameObject pool</returns>
        public IAsyncPool<GameObject> CreateAsyncGameObjectPool(
            Func<Task<GameObject>> asyncFactory,
            Transform parent = null,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<GameObject> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            // First create a standard GameObject pool with a synchronous factory that blocks on the async factory
            var syncFactory = new Func<GameObject>(() => asyncFactory().GetAwaiter().GetResult());
            var prefab = syncFactory();
            var basePool = CreateGameObjectPool(
                prefab, 
                parent, 
                initialCapacity, 
                maxSize, 
                prewarm, 
                resetAction, 
                worldPositionStays, 
                null);
            
            // Now wrap it with an async adapter - explicitly specify the type parameter
            return CreateAsyncPool<GameObject>(basePool, asyncFactory, true, poolName);
        }

        /// <summary>
        /// Creates an async component pool
        /// </summary>
        /// <typeparam name="T">Type of components to pool</typeparam>
        /// <param name="asyncFactory">Async factory function to create new components</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset components when returned to pool</param>
        /// <param name="worldPositionStays">Whether to preserve world position when reparenting</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An AsyncPool that wraps a component pool</returns>
        public IAsyncPool<T> CreateAsyncComponentPool<T>(
            Func<Task<T>> asyncFactory,
            Transform parent = null,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null) where T : Component
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            // First create a standard component pool with a synchronous factory that blocks on the async factory
            var syncFactory = new Func<T>(() => asyncFactory().GetAwaiter().GetResult());
            var prefab = syncFactory();
            var basePool = CreateComponentPool(
                prefab,
                parent,
                initialCapacity,
                maxSize, 
                prewarm, 
                resetAction, 
                worldPositionStays, 
                null);
            
            // Now wrap it with an async adapter - explicitly specify the type parameter
            return CreateAsyncPool<T>(basePool, asyncFactory, true, poolName);
        }

        /// <summary>
        /// Creates a thread-safe async object pool
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Async factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An AsyncPool that wraps a thread-safe object pool</returns>
        public IAsyncPool<T> CreateThreadSafeAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (asyncFactory == null)
                throw new ArgumentNullException(nameof(asyncFactory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            // First create a thread-safe pool with a synchronous factory that blocks on the async factory
            var syncFactory = new Func<T>(() => asyncFactory().GetAwaiter().GetResult());
            IPool<T> basePool = CreateThreadSafePool(syncFactory, initialCapacity, maxSize, prewarm, resetAction, null);
            
            // Now wrap it with an async adapter - explicitly specify the type parameter
            return CreateAsyncPool<T>(basePool, asyncFactory, true, poolName);
        }

        /// <summary>
        /// Creates an async pool wrapped around a semaphore-controlled object pool.
        /// This prioritizes async operations with semaphore-based concurrency control.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Async factory function to create new instances</param>
        /// <param name="maxConcurrency">Maximum number of concurrent users</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>An AsyncPool that wraps a semaphore-controlled pool</returns>
        public IAsyncPool<T> CreateAsyncSemaphorePool<T>(
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

            // First create a semaphore pool with a synchronous factory that blocks on the async factory
            var syncFactory = new Func<T>(() => asyncFactory().GetAwaiter().GetResult());
            IPool<T> basePool = CreateSemaphorePool(syncFactory, maxConcurrency, initialCapacity, maxSize, prewarm, resetAction, null);
            
            // Now wrap it with an async adapter - explicitly specify the type parameter
            return CreateAsyncPool<T>(basePool, asyncFactory, true, poolName);
        }

        /// <summary>
        /// Extension method to convert any pool to an async pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">The source pool to convert</param>
        /// <param name="asyncFactory">Optional async factory function</param>
        /// <param name="poolName">Optional name for the new pool</param>
        /// <returns>An async pool wrapping the source pool</returns>
        public IAsyncPool<T> ToAsyncPool<T>(
            IPool<T> pool,
            Func<Task<T>> asyncFactory = null,
            string poolName = null)
        {
            return CreateAsyncPool<T>(pool, asyncFactory, false, poolName);
        }
    }
}