using System;
using System.Threading;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Pools.Managed;

namespace AhBearStudios.Core.Pooling.Unity
{
    /// <summary>
    /// Partial class for PoolManager that handles thread-local pool creation and management
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a thread-local pool with the specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool for each thread</param>
        /// <param name="maxSize">Maximum pool size for each thread (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadLocalPool</returns>
        public ThreadLocalPool<T> CreateThreadLocalPool<T>(
            Func<T> factory,
            int initialCapacity = 5,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null)
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
                PrewarmOnInit = prewarm,
                ThreadingMode = PoolThreadingMode.ThreadLocal
            };

            return CreateThreadLocalPool(factory, config, resetAction, poolName);
        }

        /// <summary>
        /// Creates a thread-local pool with the specified configuration
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadLocalPool</returns>
        public ThreadLocalPool<T> CreateThreadLocalPool<T>(
            Func<T> factory,
            PoolConfig config,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"ThreadLocalPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ThreadLocalPool<T> threadLocalPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return threadLocalPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ThreadLocalPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ThreadLocalPool<{typeof(T).Name}>: {name}");
            
            // Ensure the config has thread-local threading mode
            config.ThreadingMode = PoolThreadingMode.ThreadLocal;
            
            // Create new pool
            var pool = new ThreadLocalPool<T>(factory, resetAction, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a high-performance thread-local pool optimized for multithreaded scenarios
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool for each thread</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new high-performance ThreadLocalPool</returns>
        public ThreadLocalPool<T> CreateHighPerformanceThreadLocalPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"HighPerfThreadLocalPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance ThreadLocalPool<{typeof(T).Name}>: {name}");
            
            // Create optimized config for high-performance thread-local pool
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = 0, // Unlimited for high-performance
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                ThreadingMode = PoolThreadingMode.ThreadLocal,
                CollectMetrics = false,
                DetailedLogging = false
            };
            
            return CreateThreadLocalPool(factory, config, resetAction, name);
        }

        /// <summary>
        /// Creates a thread-local pool with separate configurations for each thread
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="threadConfigSelector">Function to select configuration based on thread ID</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadLocalPool with thread-specific configurations</returns>
        public ThreadLocalPool<T> CreateThreadSpecificPool<T>(
            Func<T> factory,
            Func<int, PoolConfig> threadConfigSelector,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            if (threadConfigSelector == null)
                throw new ArgumentNullException(nameof(threadConfigSelector));

            string name = poolName ?? $"ThreadSpecificPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-specific pool: {name}");
            
            // Get configuration for current thread as base config
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            var baseConfig = threadConfigSelector(currentThreadId);
            
            if (baseConfig == null)
                throw new InvalidOperationException("Thread config selector returned null for the current thread");
                
            // Ensure the base config has thread-local threading mode
            baseConfig.ThreadingMode = PoolThreadingMode.ThreadLocal;
            
            // Create the thread-local pool with the base config
            // The actual per-thread config selection would need to be handled inside ThreadLocalPool
            // This would require extending ThreadLocalPool to support this feature
            var pool = new ThreadLocalPool<T>(factory, resetAction, baseConfig, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a task-optimized thread-local pool for parallel processing
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="expectedThreadCount">Expected number of threads that will access the pool</param>
        /// <param name="itemsPerThread">Initial number of items per thread</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new task-optimized ThreadLocalPool</returns>
        public ThreadLocalPool<T> CreateTaskOptimizedThreadLocalPool<T>(
            Func<T> factory,
            int expectedThreadCount = 8,
            int itemsPerThread = 5,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"TaskOptimizedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating task-optimized ThreadLocalPool<{typeof(T).Name}> for {expectedThreadCount} threads: {name}");
            
            // Create config optimized for parallel tasks
            var config = new PoolConfig
            {
                InitialCapacity = itemsPerThread,
                MaxSize = itemsPerThread * 2, // Limit per-thread pool size
                PrewarmOnInit = true,
                UseExponentialGrowth = false,
                GrowthIncrement = itemsPerThread,
                ThreadingMode = PoolThreadingMode.ThreadLocal,
                EnableAutoShrink = true,
                ShrinkThreshold = 0.3f,
                ShrinkInterval = 30.0f
            };
            
            return CreateThreadLocalPool(factory, config, resetAction, name);
        }

        /// <summary>
        /// Creates a job-system optimized thread-local pool for Unity Jobs
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity for each thread</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new job-system optimized ThreadLocalPool</returns>
        public ThreadLocalPool<T> CreateJobSystemThreadLocalPool<T>(
            Func<T> factory,
            int initialCapacity = 8,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"JobSystemPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating job-system optimized ThreadLocalPool<{typeof(T).Name}>: {name}");
            
            // Create config optimized for Unity job system
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = 0, // Unlimited for job system
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                ThreadingMode = PoolThreadingMode.ThreadLocal,
                EnableAutoShrink = false
            };
            
            return CreateThreadLocalPool(factory, config, resetAction, name);
        }

        /// <summary>
        /// Sets monitoring properties for a thread-local pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="collectMetrics">Whether to collect metrics for the pool</param>
        /// <param name="detailedLogging">Whether to enable detailed logging</param>
        public void SetThreadLocalPoolMonitoring<T>(
            ThreadLocalPool<T> pool,
            bool collectMetrics = true,
            bool detailedLogging = false)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting monitoring for pool: {pool.PoolName}, Metrics={collectMetrics}, DetailedLogging={detailedLogging}");
            
            // This would need to be implemented in ThreadLocalPool
            // For now, just log the operation
        }

        /// <summary>
        /// Sets auto-shrink properties for a thread-local pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="enableAutoShrink">Whether to enable automatic shrinking</param>
        /// <param name="shrinkThreshold">Threshold ratio below which to shrink the pool</param>
        /// <param name="shrinkInterval">Minimum time between shrink operations</param>
        public void SetThreadLocalPoolAutoShrink<T>(
            ThreadLocalPool<T> pool,
            bool enableAutoShrink = true,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting auto-shrink for pool: {pool.PoolName}, Enabled={enableAutoShrink}, Threshold={shrinkThreshold}, Interval={shrinkInterval}");
            
            // This would need to be implemented in ThreadLocalPool
            // For now, just log the operation
        }
    }
}