using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Native;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class implementation of PoolManager that provides methods for creating and managing
    /// native pools optimized for unmanaged types.
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a new native pool with the specified parameters.
        /// Native pools are optimized for unmanaged types and offer better performance for value types.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new NativePool instance</returns>
        public NativePool<T> CreateNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"NativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating NativePool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new NativePool<T>(initialCapacity, allocator, defaultValue, maxSize, prewarm, true, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a new native pool using the provided configuration.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new NativePool instance</returns>
        public NativePool<T> CreateNativePool<T>(
            PoolConfig config,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            if (config == null)
                config = new PoolConfig();
                
            return CreateNativePool<T>(
                config.InitialCapacity,
                config.NativeAllocator,
                defaultValue,
                config.MaxSize,
                config.PrewarmOnInit,
                poolName);
        }

        /// <summary>
        /// Creates a high-performance native pool optimized for maximum throughput.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new NativePool instance configured for high performance</returns>
        public NativePool<T> CreateHighPerformanceNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"HighPerformanceNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance NativePool '{poolName}' with capacity {initialCapacity}");
            
            // For high performance, we use a larger initial capacity and no max size
            var pool = new NativePool<T>(initialCapacity, allocator, defaultValue, 0, true, false, poolName);
            pool.SetHighPerformanceMode(true);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a fixed-size native pool that doesn't grow or shrink.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="capacity">Fixed capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new fixed-size NativePool instance</returns>
        public NativePool<T> CreateFixedSizeNativePool<T>(
            int capacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"FixedSizeNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating fixed-size NativePool '{poolName}' with capacity {capacity}");
            
            // For fixed size pools, set max size equal to capacity
            var pool = new NativePool<T>(capacity, allocator, defaultValue, capacity, true, true, poolName);
            pool.SetFixedSize(true);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a native pool of NativeArray instances.
        /// </summary>
        /// <typeparam name="T">The type of elements in the arrays. Must be unmanaged (blittable).</typeparam>
        /// <param name="arrayLength">Length of each NativeArray</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A specialized NativeArrayPool instance</returns>
        public NativeArrayPool<T> CreateNativeArrayPool<T>(
            int arrayLength,
            int initialCapacity,
            Allocator allocator,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"NativeArrayPool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating NativeArrayPool '{poolName}' with array length {arrayLength} and capacity {initialCapacity}");
            
            var pool = new NativeArrayPool<T>(arrayLength, initialCapacity, allocator, maxSize, prewarm, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a native pool of NativeArray instances using the provided configuration.
        /// </summary>
        /// <typeparam name="T">The type of elements in the arrays. Must be unmanaged (blittable).</typeparam>
        /// <param name="arrayLength">Length of each NativeArray</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A specialized NativeArrayPool instance</returns>
        public NativeArrayPool<T> CreateNativeArrayPool<T>(
            int arrayLength,
            PoolConfig config,
            string poolName = null) where T : unmanaged
        {
            if (config == null)
                config = new PoolConfig();
                
            return CreateNativeArrayPool<T>(
                arrayLength,
                config.InitialCapacity,
                config.NativeAllocator,
                config.MaxSize,
                config.PrewarmOnInit,
                poolName);
        }

        /// <summary>
        /// Creates a native pool of NativeList instances.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialListCapacity">Initial capacity of each NativeList</param>
        /// <param name="initialPoolCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A specialized NativeListPool instance</returns>
        public NativeListPool<T> CreateNativeListPool<T>(
            int initialListCapacity,
            int initialPoolCapacity,
            Allocator allocator,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"NativeListPool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating NativeListPool '{poolName}' with list capacity {initialListCapacity} and pool capacity {initialPoolCapacity}");
            
            var pool = new NativeListPool<T>(initialListCapacity, initialPoolCapacity, allocator, maxSize, prewarm, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a native pool of NativeList instances using the provided configuration.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialListCapacity">Initial capacity of each NativeList</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A specialized NativeListPool instance</returns>
        public NativeListPool<T> CreateNativeListPool<T>(
            int initialListCapacity,
            PoolConfig config,
            string poolName = null) where T : unmanaged
        {
            if (config == null)
                config = new PoolConfig();
                
            return CreateNativeListPool<T>(
                initialListCapacity,
                config.InitialCapacity,
                config.NativeAllocator,
                config.MaxSize,
                config.PrewarmOnInit,
                poolName);
        }

        /// <summary>
        /// Creates a native pool of UnsafeList instances.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialListCapacity">Initial capacity of each UnsafeList</param>
        /// <param name="initialPoolCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A specialized UnsafeListPool instance</returns>
        public UnsafeListPool<T> CreateUnsafeListPool<T>(
            int initialListCapacity,
            int initialPoolCapacity,
            Allocator allocator,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"UnsafeListPool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating UnsafeListPool '{poolName}' with list capacity {initialListCapacity} and pool capacity {initialPoolCapacity}");
            
            var pool = new UnsafeListPool<T>(initialListCapacity, initialPoolCapacity, allocator, maxSize, prewarm, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a thread-safe native pool that can be safely accessed from multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A thread-safe NativePool instance</returns>
        public ThreadSafeNativePool<T> CreateThreadSafeNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            int maxSize = 0,
            bool prewarm = true,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"ThreadSafeNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating ThreadSafeNativePool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new ThreadSafeNativePool<T>(initialCapacity, allocator, defaultValue, maxSize, prewarm, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a semaphore-controlled native pool for thread-safe access with concurrency limits.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="maxConcurrency">Maximum number of concurrent acquisitions</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="maxSize">Maximum size of the pool (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool on creation</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool wrapping a NativePool</returns>
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
            
            poolName ??= $"SemaphoreNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating SemaphoreNativePool '{poolName}' with capacity {initialCapacity} and concurrency {maxConcurrency}");
            
            // Create the inner pool
            var innerPool = CreateNativePool<T>(initialCapacity, allocator, defaultValue, maxSize, prewarm, $"{poolName}_Inner");
            
            // Wrap it in a semaphore pool
            return CreateSemaphorePool(innerPool, maxConcurrency, poolName);
        }

        /// <summary>
        /// Creates a native pool specifically configured for particle simulation data.
        /// </summary>
        /// <typeparam name="T">The particle data type. Must be unmanaged (blittable).</typeparam>
        /// <param name="maxParticleCount">Maximum number of particles</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new NativePool optimized for particle simulation</returns>
        public NativePool<T> CreateParticleDataNativePool<T>(
            int maxParticleCount,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"ParticleDataNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating particle data NativePool '{poolName}' with capacity {maxParticleCount}");
            
            // Create a pool with specific settings for particle data
            var pool = new NativePool<T>(maxParticleCount, allocator, defaultValue, 0, true, false, poolName);
            pool.SetHighPerformanceMode(true);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Gets a native pool by its registry handle.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="handle">The handle to the pool</param>
        /// <returns>The pool associated with the handle</returns>
        public NativePool<T> GetNativePoolByHandle<T>(NativePoolHandle handle) where T : unmanaged
        {
            EnsureInitialized();
            
            if (NativeRegistry == null)
                return null;
                
            return NativeRegistry.GetPool<T>(handle.Id) as NativePool<T>;
        }

        /// <summary>
        /// Disposes a native pool and unregisters it from the registry.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to dispose</param>
        public void DisposeAndUnregisterNativePool<T>(NativePool<T> pool) where T : unmanaged
        {
            if (pool == null || pool.IsDisposed)
                return;
                
            _logger?.LogInfo($"Disposing and unregistering NativePool '{pool.PoolName}'");
            
            // Unregister from native registry if it has a handle
            if (pool.GetHandle().IsValid && NativeRegistry != null)
            {
                NativeRegistry.Unregister(pool.GetHandle().Id);
            }
            
            // Dispose the pool
            pool.Dispose();
        }

        /// <summary>
        /// Creates a native pool that can dynamically resize based on usage patterns.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="shrinkThreshold">Usage ratio below which the pool will shrink</param>
        /// <param name="shrinkInterval">Time in seconds between shrink checks</param>
        /// <param name="defaultValue">Default value for new items</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new auto-scaling NativePool instance</returns>
        public NativePool<T> CreateAutoScalingNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"AutoScalingNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating auto-scaling NativePool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new NativePool<T>(initialCapacity, allocator, defaultValue, 0, true, true, poolName);
            pool.SetAutoShrink(true, shrinkThreshold, shrinkInterval);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            // Setup monitoring for auto-shrink
            if (_coroutineRunner != null)
            {
                _coroutineRunner.StartPoolMonitoring(pool, shrinkInterval);
            }
            
            return pool;
        }

        /// <summary>
        /// Sets auto-shrink behavior for a native pool.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="enableAutoShrink">Whether to enable auto-shrink</param>
        /// <param name="shrinkThreshold">Usage ratio below which the pool will shrink</param>
        /// <param name="shrinkInterval">Time in seconds between shrink checks</param>
        public void SetNativePoolAutoShrink<T>(
            NativePool<T> pool,
            bool enableAutoShrink = true,
            float shrinkThreshold = 0.25f,
            float shrinkInterval = 60.0f) where T : unmanaged
        {
            if (pool == null || pool.IsDisposed)
                return;
                
            pool.SetAutoShrink(enableAutoShrink, shrinkThreshold, shrinkInterval);
            
            // Setup or remove monitoring for auto-shrink
            if (_coroutineRunner != null)
            {
                if (enableAutoShrink)
                    _coroutineRunner.StartPoolMonitoring(pool, shrinkInterval);
                else
                    _coroutineRunner.StopPoolMonitoring(pool);
            }
        }

        /// <summary>
        /// Sets monitoring behavior for a native pool.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to configure</param>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <param name="detailedLogging">Whether to enable detailed logging</param>
        public void SetNativePoolMonitoring<T>(
            NativePool<T> pool,
            bool collectMetrics = true,
            bool detailedLogging = false) where T : unmanaged
        {
            if (pool == null || pool.IsDisposed)
                return;
                
            pool.SetMetricsCollection(collectMetrics);
            pool.SetDetailedLogging(detailedLogging);
            
            // Register with diagnostics if needed
            if (collectMetrics && _diagnostics != null)
            {
                _diagnostics.RegisterPool(pool);
            }
        }
    }
}