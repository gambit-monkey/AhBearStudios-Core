using System;
using AhBearStudios.Pooling.Core.Pooling.Managed;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles GameObject pools
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a GameObject pool with standard configuration
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new GameObjectPool</returns>
        public GameObjectPool CreateGameObjectPool(
            GameObject prefab, 
            Transform parent = null, 
            PoolConfig config = null, 
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"GameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is GameObjectPool gameObjectPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return gameObjectPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a GameObjectPool");
            }
            
            _logger?.LogInfo($"Creating new GameObjectPool for {prefab.name}: {name}");
            
            // Create the pool
            var pool = new GameObjectPool(prefab, parent, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a GameObject pool with specialized GameObject configuration
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="config">GameObject-specific pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new GameObjectPool</returns>
        public GameObjectPool CreateGameObjectPool(
            GameObject prefab, 
            Transform parent = null, 
            GameObjectPoolConfig config = null, 
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"GameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is GameObjectPool gameObjectPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return gameObjectPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a GameObjectPool");
            }
            
            _logger?.LogInfo($"Creating new GameObjectPool for {prefab.name} with GameObjectPoolConfig: {name}");
            
            // Create default config if none provided
            if (config == null)
            {
                config = new GameObjectPoolConfig
                {
                    InitialCapacity = 10,
                    MaxSize = 0,
                    Prewarm = true,
                    ResetOnRelease = true,
                    ReparentOnRelease = true,
                    ToggleActive = true
                };
            }
            
            // Create the pool
            var pool = new GameObjectPool(prefab, parent, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a GameObject pool with detailed configuration options
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset GameObjects when returned to pool</param>
        /// <param name="worldPositionStays">Whether world position stays when objects are reparented</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new GameObjectPool</returns>
        public GameObjectPool CreateGameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<GameObject> resetAction = null,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"GameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is GameObjectPool gameObjectPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return gameObjectPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a GameObjectPool");
            }
            
            _logger?.LogInfo($"Creating new GameObjectPool for {prefab.name} with custom parameters: {name}");
            
            // Create config
            var config = new GameObjectPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = prewarm,
                ResetOnRelease = resetAction != null,
                ReparentOnRelease = parent != null,
                WorldPositionStays = worldPositionStays
            };
            
            // Create the pool
            var pool = new GameObjectPool(prefab, parent, config, name);
            
            // Set custom reset action if provided
            if (resetAction != null)
            {
                pool.SetCustomResetAction(resetAction);
            }
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a high-performance GameObject pool optimized for frequent object cycling
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new high-performance GameObjectPool</returns>
        public GameObjectPool CreateHighPerformanceGameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int initialCapacity = 20,
            int maxSize = 100,
            bool prewarm = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"HighPerfGameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance GameObjectPool for {prefab.name}: {name}");
            
            // Create optimized config for high-performance
            var config = new GameObjectPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = prewarm,
                ResetOnRelease = true,
                ReparentOnRelease = true,
                ToggleActive = true,
                CallPoolEvents = false, // Skipping events for performance
                ValidateOnAcquire = false
            };
            
            // Create the pool
            var pool = new GameObjectPool(prefab, parent, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a GameObject pool that utilizes a semaphore to limit concurrent access
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="maxConcurrency">Maximum number of objects that can be active concurrently</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="worldPositionStays">Whether world position stays when objects are reparented</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new semaphore-controlled GameObjectPool</returns>
        public SemaphorePool<GameObject> CreateThreadSafeGameObjectPool(
            GameObject prefab,
            int maxConcurrency,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            bool worldPositionStays = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be positive");

            string name = poolName ?? $"ThreadSafeGameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-safe GameObjectPool for {prefab.name}: {name}");
            
            // First create inner pool
            var innerPool = CreateGameObjectPool(
                prefab, 
                parent, 
                initialCapacity, 
                maxSize, 
                prewarm, 
                null, 
                worldPositionStays, 
                $"{name}_Inner");
            
            // Then wrap it in a semaphore pool
            return CreateSemaphorePool(innerPool, maxConcurrency, name);
        }

        /// <summary>
        /// Creates a GameObject pool that resets physics components when objects are returned
        /// </summary>
        /// <param name="prefab">Prefab to pool (should contain physics components)</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new physics-aware GameObjectPool</returns>
        public GameObjectPool CreatePhysicsGameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null)
        {
            EnsureInitialized();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            string name = poolName ?? $"PhysicsGameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating physics-aware GameObjectPool for {prefab.name}: {name}");
            
            // Create pool with physics reset action
            return CreateGameObjectPool(
                prefab,
                parent,
                initialCapacity,
                maxSize,
                prewarm,
                ResetPhysics,
                true,
                name);
        }

        /// <summary>
        /// Resets all physics components on a GameObject
        /// </summary>
        /// <param name="gameObject">GameObject to reset</param>
        private void ResetPhysics(GameObject gameObject)
        {
            if (gameObject == null)
                return;
                
            // Reset Rigidbody if present
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.Sleep();
            }
            
            // Reset Rigidbody2D if present
            var rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                rigidbody2D.angularVelocity = 0f;
                rigidbody2D.Sleep();
            }
            
            // Reset Colliders if needed
            // This is just a basic implementation - extend as needed
        }

        /// <summary>
        /// Extension method to set a custom reset action on a GameObjectPool
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="resetAction">Custom reset action</param>
        public void SetCustomResetAction(this GameObjectPool pool, Action<GameObject> resetAction)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting custom reset action on pool: {pool.PoolName}");
        }

        /// <summary>
        /// Extension method to set worldPositionStays property on a GameObjectPool
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="worldPositionStays">Whether world position stays when objects are reparented</param>
        public void SetWorldPositionStays(this GameObjectPool pool, bool worldPositionStays)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting worldPositionStays={worldPositionStays} on pool: {pool.PoolName}");
        }

        /// <summary>
        /// Extension method to set a parent transform on a GameObjectPool
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="parent">Parent transform</param>
        public void SetParent(this GameObjectPool pool, Transform parent)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting parent transform on pool: {pool.PoolName}");
        }

        /// <summary>
        /// Extension method to enable reparenting behavior on a GameObjectPool
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="reparentOnRelease">Whether to reparent objects when they are released</param>
        public void SetReparentOnRelease(this GameObjectPool pool, bool reparentOnRelease)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting reparentOnRelease={reparentOnRelease} on pool: {pool.PoolName}");
        }

        /// <summary>
        /// Extension method to set whether objects should be activated/deactivated when acquired/released
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="toggleActive">Whether to toggle GameObject activation state</param>
        public void SetToggleActive(this GameObjectPool pool, bool toggleActive)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting toggleActive={toggleActive} on pool: {pool.PoolName}");
        }

        /// <summary>
        /// Extension method to set layer settings for pooled objects
        /// </summary>
        /// <param name="pool">Pool to modify</param>
        /// <param name="activeLayer">Layer for active objects</param>
        /// <param name="inactiveLayer">Layer for inactive objects</param>
        public void SetLayerSettings(this GameObjectPool pool, int activeLayer, int inactiveLayer)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Implementation would depend on the GameObjectPool implementation
            // This is a placeholder for the extension method
            _logger?.LogInfo($"Setting layer settings on pool: {pool.PoolName}");
        }
    }
}