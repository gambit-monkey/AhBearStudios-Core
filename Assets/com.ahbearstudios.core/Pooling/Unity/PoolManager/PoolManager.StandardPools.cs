// using System;
// using AhBearStudios.Pooling.Core;
//
// namespace AhBearStudios.Pooling
// {
//     /// <summary>
//     /// Partial class for PoolManager that handles standard (non-threaded) object pools
//     /// </summary>
//     public partial class PoolManager
//     {
//         /// <summary>
//         /// Creates a standard managed object pool
//         /// </summary>
//         /// <typeparam name="T">Type of objects to pool</typeparam>
//         /// <param name="factory">Factory function to create new instances</param>
//         /// <param name="initialCapacity">Initial capacity of the pool</param>
//         /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
//         /// <param name="prewarm">Whether to prewarm the pool</param>
//         /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
//         /// <param name="poolName">Optional name for the pool</param>
//         /// <returns>A new ObjectPool</returns>
//         public ObjectPool<T> CreatePool<T>(
//             Func<T> factory,
//             int initialCapacity = 10,
//             int maxSize = 0,
//             bool prewarm = true,
//             Action<T> resetAction = null,
//             string poolName = null) where T : class
//         {
//             EnsureInitialized();
//
//             if (factory == null)
//                 throw new ArgumentNullException(nameof(factory));
//
//             if (initialCapacity < 0)
//                 throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");
//
//             string name = poolName ?? $"Pool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
//             
//             // Check if pool already exists
//             if (Registry.HasPool(name))
//             {
//                 var existingPool = Registry.GetPool(name);
//                 if (existingPool is ObjectPool<T> objectPool)
//                 {
//                     _logger?.LogInfo($"Returning existing pool: {name}");
//                     return objectPool;
//                 }
//                 
//                 throw new InvalidOperationException($"Pool with name {name} already exists but is not an ObjectPool<{typeof(T).Name}>");
//             }
//             
//             _logger?.LogInfo($"Creating new ObjectPool<{typeof(T).Name}>: {name}");
//             
//             // Create new pool
//             var pool = new ObjectPool<T>(factory, initialCapacity, maxSize, prewarm, resetAction, name);
//             
//             // Register the pool
//             Registry.RegisterPool(pool, name);
//             
//             return pool;
//         }
//
//         /// <summary>
//         /// Creates a standard managed object pool using a PoolConfig
//         /// </summary>
//         /// <typeparam name="T">Type of objects to pool</typeparam>
//         /// <param name="factory">Factory function to create new instances</param>
//         /// <param name="config">Pool configuration</param>
//         /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
//         /// <param name="poolName">Optional name for the pool</param>
//         /// <returns>A new ObjectPool</returns>
//         public ObjectPool<T> CreatePool<T>(
//             Func<T> factory,
//             PoolConfig config,
//             Action<T> resetAction = null,
//             string poolName = null) where T : class
//         {
//             if (config == null)
//                 throw new ArgumentNullException(nameof(config));
//                 
//             return CreatePool(
//                 factory, 
//                 config.InitialCapacity, 
//                 config.MaximumCapacity, 
//                 config.Prewarm, 
//                 resetAction, 
//                 poolName);
//         }
//
//         /// <summary>
//         /// Creates a high-performance standard object pool optimized for single-threaded use
//         /// </summary>
//         /// <typeparam name="T">Type of objects to pool</typeparam>
//         /// <param name="factory">Factory function to create new instances</param>
//         /// <param name="initialCapacity">Initial capacity of the pool</param>
//         /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
//         /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
//         /// <param name="poolName">Optional name for the pool</param>
//         /// <returns>A new high-performance ObjectPool</returns>
//         public ObjectPool<T> CreateHighPerformancePool<T>(
//             Func<T> factory,
//             int initialCapacity = 20,
//             int maxSize = 100,
//             Action<T> resetAction = null,
//             string poolName = null) where T : class
//         {
//             EnsureInitialized();
//
//             if (factory == null)
//                 throw new ArgumentNullException(nameof(factory));
//
//             string name = poolName ?? $"HighPerfPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
//             
//             _logger?.LogInfo($"Creating high-performance ObjectPool<{typeof(T).Name}>: {name}");
//             
//             // Create optimized config for high-performance pool
//             var config = new PoolConfig
//             {
//                 InitialCapacity = initialCapacity,
//                 MaximumCapacity = maxSize,
//                 PrewarmOnInit = true,
//                 UseExponentialGrowth = true,
//                 GrowthFactor = 2.0f,
//                 ThreadingMode = PoolThreadingMode.None,
//                 CollectMetrics = false,
//                 DetailedLogging = false
//             };
//             
//             return CreatePool(factory, config, resetAction, name);
//         }
//
//         /// <summary>
//         /// Sets auto-shrink properties for a standard object pool
//         /// </summary>
//         /// <typeparam name="T">Type of objects in the pool</typeparam>
//         /// <param name="pool">The pool to configure</param>
//         /// <param name="enableAutoShrink">Whether to enable automatic shrinking</param>
//         /// <param name="shrinkThreshold">Threshold ratio below which to shrink the pool</param>
//         /// <param name="shrinkInterval">Minimum time between shrink operations</param>
//         public void SetPoolAutoShrink<T>(
//             ObjectPool<T> pool,
//             bool enableAutoShrink = true,
//             float shrinkThreshold = 0.25f,
//             float shrinkInterval = 60.0f) where T : class
//         {
//             if (pool == null)
//                 throw new ArgumentNullException(nameof(pool));
//                 
//             _logger?.LogInfo($"Setting auto-shrink for pool: {pool.PoolName}, Enabled={enableAutoShrink}, Threshold={shrinkThreshold}, Interval={shrinkInterval}");
//             
//             // This would need to be implemented in ObjectPool
//             // For now, just log the operation
//         }
//
//         /// <summary>
//         /// Sets monitoring properties for a standard object pool
//         /// </summary>
//         /// <typeparam name="T">Type of objects in the pool</typeparam>
//         /// <param name="pool">The pool to configure</param>
//         /// <param name="collectMetrics">Whether to collect metrics for the pool</param>
//         /// <param name="detailedLogging">Whether to enable detailed logging</param>
//         public void SetPoolMetrics<T>(
//             ObjectPool<T> pool,
//             bool collectMetrics = true,
//             bool detailedLogging = false) where T : class
//         {
//             if (pool == null)
//                 throw new ArgumentNullException(nameof(pool));
//                 
//             _logger?.LogInfo($"Setting monitoring for pool: {pool.PoolName}, Metrics={collectMetrics}, DetailedLogging={detailedLogging}");
//             
//             // This would need to be implemented in ObjectPool
//             // For now, just log the operation
//         }
//     }
// }