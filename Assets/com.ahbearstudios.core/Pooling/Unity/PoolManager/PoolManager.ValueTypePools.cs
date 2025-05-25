using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Native;
using AhBearStudios.Pooling.Core.Pooling.Managed;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles value type pools
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a value type pool for structs
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ValueTypePool</returns>
        public ValueTypePool<T> CreateValueTypePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"ValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ValueTypePool<T> valuePool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return valuePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ValueTypePool<{typeof(T).Name}>: {name}");
            
            // Create a pool configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                CollectMetrics = _collectMetrics
            };
            
            // Create new pool
            var pool = new ValueTypePool<T>(factory, config, resetAction, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a value type pool using configuration object
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ValueTypePool</returns>
        public ValueTypePool<T> CreateValueTypePool<T>(
            Func<T> factory,
            PoolConfig config,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"ValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ValueTypePool<T> valuePool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return valuePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ValueTypePool<{typeof(T).Name}>: {name}");
            
            // Create new pool
            var pool = new ValueTypePool<T>(factory, config, resetAction, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a thread-safe value type pool for structs
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadSafeValueTypePool</returns>
        public ThreadSafeValueTypePool<T> CreateThreadSafeValueTypePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            string name = poolName ?? $"TSValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ThreadSafeValueTypePool<T> threadSafeValuePool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return threadSafeValuePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ThreadSafeValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ThreadSafeValueTypePool<{typeof(T).Name}>: {name}");
            
            // Create a pool configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                ThreadingMode = PoolThreadingMode.ThreadSafe,
                CollectMetrics = _collectMetrics
            };
            
            // Create new pool
            var pool = new ThreadSafeValueTypePool<T>(factory, config, resetAction, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a thread-safe value type pool using configuration object
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ThreadSafeValueTypePool</returns>
        public ThreadSafeValueTypePool<T> CreateThreadSafeValueTypePool<T>(
            Func<T> factory,
            PoolConfig config,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"TSValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ThreadSafeValueTypePool<T> threadSafeValuePool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return threadSafeValuePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ThreadSafeValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ThreadSafeValueTypePool<{typeof(T).Name}>: {name}");
            
            // Make sure the threading mode is set properly
            var threadSafeConfig = config.Clone();
            threadSafeConfig.ThreadingMode = PoolThreadingMode.ThreadSafe;
            
            // Create new pool
            var pool = new ThreadSafeValueTypePool<T>(factory, threadSafeConfig, resetAction, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a Burst-compatible value type pool that works with the Unity Burst compiler
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new BurstCompatibleValueTypePool</returns>
        public BurstCompatibleValueTypePool<T> CreateBurstCompatibleValueTypePool<T>(
            Func<T> factory,
            Allocator allocator,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");
                
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
            
            if (allocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp is not supported for pools as it's too short-lived", nameof(allocator));

            string name = poolName ?? $"BurstValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists in native registry
            if (NativeRegistry.TryGetPoolByName(name, out INativePool nativePool))
            {
                if (nativePool is BurstCompatibleValueTypePool<T> burstPool)
                {
                    _logger?.LogInfo($"Returning existing burst value pool: {name}");
                    return burstPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a BurstCompatibleValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new BurstCompatibleValueTypePool<{typeof(T).Name}>: {name} with allocator {allocator}");
            
            // Create a pool configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                NativeAllocator = allocator,
                CollectMetrics = _collectMetrics
            };
            
            // Create new pool
            var pool = new BurstCompatibleValueTypePool<T>(factory, config, name);
            
            // Register with native registry
            NativeRegistry.RegisterPool(pool, name);
            
            // Register with global registry for Burst access
            var handle = NativeRegistry.Register(pool);
            pool.SetPoolHandle(handle);
            
            return pool;
        }

        /// <summary>
        /// Creates a Burst-compatible value type pool using configuration object
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new BurstCompatibleValueTypePool</returns>
        public BurstCompatibleValueTypePool<T> CreateBurstCompatibleValueTypePool<T>(
            Func<T> factory,
            PoolConfig config,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            if (config.NativeAllocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), config.NativeAllocator))
                throw new ArgumentException("Invalid allocator specified in config", nameof(config));
            
            if (config.NativeAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp is not supported for pools as it's too short-lived", nameof(config));

            string name = poolName ?? $"BurstValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists in native registry
            if (NativeRegistry.TryGetPoolByName(name, out INativePool nativePool))
            {
                if (nativePool is BurstCompatibleValueTypePool<T> burstPool)
                {
                    _logger?.LogInfo($"Returning existing burst value pool: {name}");
                    return burstPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a BurstCompatibleValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new BurstCompatibleValueTypePool<{typeof(T).Name}>: {name} with allocator {config.NativeAllocator}");
            
            // Create new pool
            var pool = new BurstCompatibleValueTypePool<T>(factory, config, name);
            
            // Register with native registry
            NativeRegistry.RegisterPool(pool, name);
            
            // Register with global registry for Burst access
            var handle = NativeRegistry.Register(pool);
            pool.SetPoolHandle(handle);
            
            return pool;
        }

        /// <summary>
        /// Creates a job-compatible value type pool that can be safely used within Unity Jobs
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleValueTypePool</returns>
        public JobCompatibleValueTypePool<T> CreateJobCompatibleValueTypePool<T>(
            Func<T> factory,
            Allocator allocator,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");
                
            if (allocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), allocator))
                throw new ArgumentException("Invalid allocator specified", nameof(allocator));
            
            if (allocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp is not supported for pools as it's too short-lived", nameof(allocator));

            string name = poolName ?? $"JobValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists in native registry
            if (NativeRegistry.TryGetPoolByName(name, out INativePool nativePool))
            {
                if (nativePool is JobCompatibleValueTypePool<T> jobPool)
                {
                    _logger?.LogInfo($"Returning existing job value pool: {name}");
                    return jobPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a JobCompatibleValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new JobCompatibleValueTypePool<{typeof(T).Name}>: {name} with allocator {allocator}");
            
            // Create a pool configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                NativeAllocator = allocator,
                ThreadingMode = PoolThreadingMode.JobSafe,
                CollectMetrics = _collectMetrics
            };
            
            // Create new pool
            var pool = new JobCompatibleValueTypePool<T>(factory, config, name);
            
            // Register with native registry
            NativeRegistry.RegisterPool(pool, name);
            
            // Register with global registry for job system access
            var handle = NativeRegistry.Register(pool);
            pool.SetPoolHandle(handle);
            
            return pool;
        }

        /// <summary>
        /// Creates a job-compatible value type pool using configuration object
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleValueTypePool</returns>
        public JobCompatibleValueTypePool<T> CreateJobCompatibleValueTypePool<T>(
            Func<T> factory,
            PoolConfig config,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            if (config.NativeAllocator <= Allocator.None || !Enum.IsDefined(typeof(Allocator), config.NativeAllocator))
                throw new ArgumentException("Invalid allocator specified in config", nameof(config));
            
            if (config.NativeAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp is not supported for pools as it's too short-lived", nameof(config));

            string name = poolName ?? $"JobValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists in native registry
            if (NativeRegistry.TryGetPoolByName(name, out INativePool nativePool))
            {
                if (nativePool is JobCompatibleValueTypePool<T> jobPool)
                {
                    _logger?.LogInfo($"Returning existing job value pool: {name}");
                    return jobPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a JobCompatibleValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new JobCompatibleValueTypePool<{typeof(T).Name}>: {name} with allocator {config.NativeAllocator}");
            
            // Make sure the threading mode is set properly for job safety
            var jobSafeConfig = config.Clone();
            jobSafeConfig.ThreadingMode = PoolThreadingMode.JobSafe;
            
            // Create new pool
            var pool = new JobCompatibleValueTypePool<T>(factory, jobSafeConfig, name);
            
            // Register with native registry
            NativeRegistry.RegisterPool(pool, name);
            
            // Register with global registry for job system access
            var handle = NativeRegistry.Register(pool);
            pool.SetPoolHandle(handle);
            
            return pool;
        }

        /// <summary>
        /// Creates a fixed-size value type pool that never expands
        /// </summary>
        /// <typeparam name="T">Type of values to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="capacity">Fixed capacity of the pool</param>
        /// <param name="resetAction">Optional action to reset values when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ValueTypePool with fixed capacity</returns>
        public ValueTypePool<T> CreateFixedSizeValueTypePool<T>(
            Func<T> factory,
            int capacity,
            Action<ref T> resetAction = null,
            string poolName = null) where T : struct
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

            string name = poolName ?? $"FixedValuePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ValueTypePool<T> valuePool)
                {
                    _logger?.LogInfo($"Returning existing fixed-size value pool: {name}");
                    return valuePool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ValueTypePool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new fixed-size ValueTypePool<{typeof(T).Name}>: {name} with capacity {capacity}");
            
            // Create a fixed-size pool configuration (max size = initial capacity)
            var config = new PoolConfig
            {
                InitialCapacity = capacity,
                MaxSize = capacity,
                PrewarmOnInit = true, // Always prewarm fixed-size pools
                CollectMetrics = _collectMetrics
            };
            
            // Create new pool
            var pool = new ValueTypePool<T>(factory, config, resetAction, name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }
    }
}