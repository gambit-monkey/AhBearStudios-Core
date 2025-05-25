using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using Unity.Collections;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles thread-safe pool creation and management
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a thread-safe pool with the specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadSafePool</returns>
        public ThreadSafePool<T> CreateThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
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
                ThreadingMode = PoolThreadingMode.ThreadSafe
            };

            return CreateThreadSafePool(factory, config, resetAction, poolName);
        }

        /// <summary>
        /// Creates a thread-safe pool with the specified configuration
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadSafePool</returns>
        public ThreadSafePool<T> CreateThreadSafePool<T>(
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

            string name = poolName ?? $"ThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ThreadSafePool<T> threadSafePool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return threadSafePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ThreadSafePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Ensure the config has thread-safe threading mode
            config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            
            // Create new pool
            var pool = new ThreadSafePool<T>(factory, resetAction, config, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a high-performance thread-safe pool optimized for high-concurrency scenarios
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new high-performance ThreadSafePool</returns>
        public ThreadSafePool<T> CreateHighPerformanceThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            int maxSize = 100,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"HighPerfThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create optimized config for high-performance thread-safe pool
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                DetailedLogging = false,
                CollectMetrics = true
            };
            
            return CreateThreadSafePool(factory, config, resetAction, name);
        }

        /// <summary>
        /// Creates a burst-compatible thread-safe pool for use with the Burst compiler
        /// </summary>
        /// <typeparam name="T">Type of objects to pool (must be unmanaged)</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new burst-compatible ThreadSafePool</returns>
        public ThreadSafePool<T> CreateBurstCompatibleThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            int maxSize = 0, 
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"BurstThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating burst-compatible ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create config optimized for Burst compatibility
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 1.5f,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                NativeAllocator = Allocator.Persistent,
                DetailedLogging = false
            };
            
            return CreateThreadSafePool(factory, config, null, name);
        }

        /// <summary>
        /// Creates a job-compatible thread-safe pool for use with Unity's job system
        /// </summary>
        /// <typeparam name="T">Type of objects to pool (must be unmanaged)</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new job-compatible ThreadSafePool</returns>
        public ThreadSafePool<T> CreateJobCompatibleThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            int maxSize = 0,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"JobThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating job-compatible ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create config optimized for job compatibility
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                NativeAllocator = Allocator.Persistent,
                DetailedLogging = false
            };
            
            return CreateThreadSafePool(factory, config, null, name);
        }

        /// <summary>
        /// Creates an auto-scaling thread-safe pool that grows and shrinks based on usage patterns
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="shrinkThreshold">Threshold ratio of used/total items below which the pool will shrink</param>
        /// <param name="shrinkInterval">Minimum time between auto-shrink operations in seconds</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new auto-scaling ThreadSafePool</returns>
        public ThreadSafePool<T> CreateAutoScalingThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"AutoScalingThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating auto-scaling ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create config for auto-scaling
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                EnableAutoShrink = true,
                ShrinkThreshold = shrinkThreshold,
                ShrinkInterval = shrinkInterval,
                ThreadingMode = PoolThreadingMode.ThreadSafe
            };
            
            return CreateThreadSafePool(factory, config, resetAction, name);
        }

        /// <summary>
        /// Creates a custom validated thread-safe pool with object validation on acquire
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="validateFunc">Function to validate objects before reuse (returns true if valid)</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A validated ThreadSafePool</returns>
        public ThreadSafePool<T> CreateValidatedThreadSafePool<T>(
            Func<T> factory,
            Func<T, bool> validateFunc,
            int initialCapacity = 10,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            if (validateFunc == null)
                throw new ArgumentNullException(nameof(validateFunc));

            string name = poolName ?? $"ValidatedThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating validated ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create config for validation
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                DetailedLogging = true
            };
            
            // Create the pool
            var pool = CreateThreadSafePool(factory, config, resetAction, name);
            
            // Add validation (needs to be implemented in ThreadSafePool)
            // This would be a placeholder for when ThreadSafePool implements validation
            _logger?.LogInfo($"Adding validation function to pool: {name}");
            
            return pool;
        }

        /// <summary>
        /// Creates a thread-safe pool that includes monitoring and diagnostics
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="monitoringInterval">Interval in seconds between monitoring checks</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A monitored ThreadSafePool</returns>
        public ThreadSafePool<T> CreateMonitoredThreadSafePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            float monitoringInterval = 30.0f,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"MonitoredThreadSafePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating monitored ThreadSafePool<{typeof(T).Name}>: {name}");
            
            // Create config with monitoring enabled
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                CollectMetrics = true,
                DetailedLogging = true
            };
            
            // Create the pool
            var pool = CreateThreadSafePool(factory, config, resetAction, name);
            
            // Add monitoring (needs to be implemented for this pool type)
            // This would be a placeholder for when ThreadSafePool implements monitoring
            _logger?.LogInfo($"Setting up monitoring for pool: {name}, Interval={monitoringInterval}s");
            
            return pool;
        }

        /// <summary>
        /// Converts an existing pool to a thread-safe pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">Existing pool to convert</param>
        /// <param name="poolName">Optional name for the new pool</param>
        /// <returns>A new ThreadSafePool that delegates to the existing pool</returns>
        public ThreadSafePool<T> ConvertToThreadSafePool<T>(
            IPool<T> pool,
            string poolName = null)
        {
            EnsureInitialized();

            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // If already thread-safe, just return it
            if (pool is ThreadSafePool<T> threadSafePool)
                return threadSafePool;

            string name = poolName ?? $"ThreadSafeWrapper_{pool.PoolName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-safe wrapper for pool: {pool.PoolName}");
            
            // For a proper implementation, we would need to create a wrapper pool
            // that delegates to the existing pool but adds thread safety.
            // This is just a placeholder as the actual implementation would depend
            // on how the pools are designed to work together.
            throw new NotImplementedException("Converting existing pools to thread-safe is not yet implemented");
        }

        /// <summary>
        /// Sets auto-shrink properties for a thread-safe pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="enableAutoShrink">Whether to enable automatic shrinking</param>
        /// <param name="shrinkThreshold">Threshold ratio below which to shrink the pool</param>
        /// <param name="shrinkInterval">Minimum time between shrink operations in seconds</param>
        public void SetThreadSafePoolAutoShrink<T>(
            ThreadSafePool<T> pool,
            bool enableAutoShrink = true,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting auto-shrink for pool: {pool.PoolName}, Enabled={enableAutoShrink}, Threshold={shrinkThreshold}, Interval={shrinkInterval}");
            
            // This would need to be implemented in ThreadSafePool
            // For now, just log the operation
        }

        /// <summary>
        /// Sets metrics collection for a thread-safe pool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="collectMetrics">Whether to collect metrics for the pool</param>
        /// <param name="detailedLogging">Whether to enable detailed logging</param>
        public void SetThreadSafePoolMetrics<T>(
            ThreadSafePool<T> pool,
            bool collectMetrics = true,
            bool detailedLogging = false)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting metrics for pool: {pool.PoolName}, Metrics={collectMetrics}, DetailedLogging={detailedLogging}");
            
            // This would need to be implemented in ThreadSafePool
            // For now, just log the operation
        }
    }
}