using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Native;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class implementation of PoolManager that provides methods for creating and managing
    /// job-compatible native pools, optimized for use with Unity's Jobs system.
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a new job-compatible native pool with the specified parameters.
        /// This type of pool is designed for optimal performance with Unity's job system.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool instance</returns>
        public JobCompatibleNativePool<T> CreateJobCompatibleNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"JobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating JobCompatibleNativePool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new JobCompatibleNativePool<T>(initialCapacity, allocator, defaultValue, poolName);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a new job-compatible native pool using the provided configuration.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool instance</returns>
        public JobCompatibleNativePool<T> CreateJobCompatibleNativePool<T>(
            PoolConfig config,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            if (config == null)
                config = new PoolConfig();
                
            return CreateJobCompatibleNativePool<T>(
                config.InitialCapacity,
                config.NativeAllocator,
                defaultValue,
                poolName);
        }

        /// <summary>
        /// Creates a high-performance job-compatible native pool optimized for frequent access in jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool instance configured for high performance</returns>
        public JobCompatibleNativePool<T> CreateHighPerformanceJobCompatibleNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"HighPerformanceJobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating high-performance JobCompatibleNativePool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new JobCompatibleNativePool<T>(initialCapacity, allocator, defaultValue, poolName);
            // Configure for high performance
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
        /// Creates a fixed-size job-compatible native pool that doesn't grow or shrink.
        /// Optimized for deterministic behavior in jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="capacity">Fixed capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new fixed-size JobCompatibleNativePool instance</returns>
        public JobCompatibleNativePool<T> CreateFixedSizeJobCompatibleNativePool<T>(
            int capacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"FixedSizeJobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating fixed-size JobCompatibleNativePool '{poolName}' with capacity {capacity}");
            
            var pool = new JobCompatibleNativePool<T>(capacity, allocator, defaultValue, poolName);
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
        /// Creates a job-compatible native pool of NativeArray instances.
        /// </summary>
        /// <typeparam name="T">The type of elements in the arrays. Must be unmanaged (blittable).</typeparam>
        /// <param name="arrayLength">Length of each NativeArray</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool of NativeArrays</returns>
        public JobCompatibleNativePool<NativeArray<T>> CreateJobCompatibleNativeArrayPool<T>(
            int arrayLength,
            int initialCapacity,
            Allocator allocator,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"JobCompatibleNativeArrayPool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating JobCompatibleNativeArrayPool '{poolName}' with array length {arrayLength} and capacity {initialCapacity}");
            
            // Create the pool
            var pool = new JobCompatibleNativePool<NativeArray<T>>(initialCapacity, allocator, default, poolName);
            
            // Initialize with empty arrays
            for (int i = 0; i < initialCapacity; i++)
            {
                var array = new NativeArray<T>(arrayLength, allocator);
                pool.Set(i, array);
            }
            
            // Set custom dispose action to clean up NativeArrays
            pool.SetDisposeAction(arrays =>
            {
                // Dispose all native arrays in the pool
                for (int i = 0; i < pool.Capacity; i++)
                {
                    if (pool.IsIndexActive(i))
                        continue;
                        
                    var array = pool.Get(i);
                    if (array.IsCreated)
                        array.Dispose();
                }
            });
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a job-compatible native pool specifically for use with ParallelFor jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool instance optimized for parallel jobs</returns>
        public JobCompatibleNativePool<T> CreateParallelJobCompatibleNativePool<T>(
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"ParallelJobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating parallel job-compatible native pool '{poolName}' with capacity {initialCapacity}");
            
            var pool = new JobCompatibleNativePool<T>(initialCapacity, allocator, defaultValue, poolName);
            pool.SetParallelJobSafe(true);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Creates a job-compatible native pool of NativeList instances.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists. Must be unmanaged (blittable).</typeparam>
        /// <param name="initialListCapacity">Initial capacity of each NativeList</param>
        /// <param name="initialPoolCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool of NativeLists</returns>
        public JobCompatibleNativePool<NativeList<T>> CreateJobCompatibleNativeListPool<T>(
            int initialListCapacity,
            int initialPoolCapacity,
            Allocator allocator,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"JobCompatibleNativeListPool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating JobCompatibleNativeListPool '{poolName}' with list capacity {initialListCapacity} and pool capacity {initialPoolCapacity}");
            
            // Create the pool
            var pool = new JobCompatibleNativePool<NativeList<T>>(initialPoolCapacity, allocator, default, poolName);
            
            // Initialize with empty lists
            for (int i = 0; i < initialPoolCapacity; i++)
            {
                var list = new NativeList<T>(initialListCapacity, allocator);
                pool.Set(i, list);
            }
            
            // Setup custom reset action to clear lists when released
            pool.SetResetAction(list =>
            {
                if (list.IsCreated)
                    list.Clear();
            });
            
            // Set custom dispose action to clean up NativeLists
            pool.SetDisposeAction(lists =>
            {
                // Dispose all native lists in the pool
                for (int i = 0; i < pool.Capacity; i++)
                {
                    if (pool.IsIndexActive(i))
                        continue;
                        
                    var list = pool.Get(i);
                    if (list.IsCreated)
                        list.Dispose();
                }
            });
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }

        /// <summary>
        /// Gets a job handle to a pool for use in jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to get a handle for</param>
        /// <returns>A handle to the pool that can be used in jobs</returns>
        public NativePoolHandle GetJobPoolHandle<T>(JobCompatibleNativePool<T> pool) where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            return pool.GetHandle();
        }

        /// <summary>
        /// Gets a read-only job handle to a pool for use in jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to get a handle for</param>
        /// <returns>A read-only handle to the pool that can be used in jobs</returns>
        public NativePoolReadHandle GetJobPoolReadHandle<T>(JobCompatibleNativePool<T> pool) where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            return pool.GetReadOnlyHandle();
        }

        /// <summary>
        /// Gets a reference to a pool by its registry handle, for use in jobs.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="handle">The handle to the pool</param>
        /// <returns>The pool associated with the handle</returns>
        public JobCompatibleNativePool<T> GetJobCompatibleNativePoolByHandle<T>(NativePoolHandle handle) 
            where T : unmanaged
        {
            EnsureInitialized();
            
            if (NativeRegistry == null)
                return null;
                
            return NativeRegistry.GetPool<T>(handle.Id) as JobCompatibleNativePool<T>;
        }

        /// <summary>
        /// Disposes a job-compatible native pool and unregisters it.
        /// </summary>
        /// <typeparam name="T">The type of elements in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="pool">The pool to dispose</param>
        public void DisposeAndUnregisterJobCompatibleNativePool<T>(JobCompatibleNativePool<T> pool) 
            where T : unmanaged
        {
            if (pool == null || pool.IsDisposed)
                return;
                
            _logger?.LogInfo($"Disposing and unregistering JobCompatibleNativePool '{pool.PoolName}'");
            
            // Unregister from native registry if it has a handle
            if (pool.GetHandle().IsValid && NativeRegistry != null)
            {
                NativeRegistry.Unregister(pool.GetHandle().Id);
            }
            
            // Dispose the pool
            pool.Dispose();
        }
        
        /// <summary>
        /// Creates a semaphore-controlled job-compatible native pool for thread-safe access.
        /// </summary>
        /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged (blittable).</typeparam>
        /// <param name="maxConcurrency">Maximum number of concurrent acquisitions</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items in the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new SemaphorePool wrapping a JobCompatibleNativePool</returns>
        public SemaphorePool<T> CreateSemaphoreJobCompatibleNativePool<T>(
            int maxConcurrency,
            int initialCapacity,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"SemaphoreJobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating semaphore job-compatible native pool '{poolName}' with capacity {initialCapacity} and concurrency {maxConcurrency}");
            
            // Create the inner pool
            var innerPool = CreateJobCompatibleNativePool<T>(initialCapacity, allocator, defaultValue, $"{poolName}_Inner");
            
            // Wrap it in a semaphore pool
            return CreateSemaphorePool(innerPool, maxConcurrency, poolName);
        }
        
        /// <summary>
        /// Creates a JobCompatibleNativePool specifically configured for particle simulation data.
        /// </summary>
        /// <typeparam name="T">The particle data type. Must be unmanaged (blittable).</typeparam>
        /// <param name="maxParticleCount">Maximum number of particles</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <param name="defaultValue">Default value for new items</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new JobCompatibleNativePool optimized for particle simulation</returns>
        public JobCompatibleNativePool<T> CreateParticleDataJobCompatibleNativePool<T>(
            int maxParticleCount,
            Allocator allocator,
            T defaultValue = default,
            string poolName = null) where T : unmanaged
        {
            EnsureInitialized();
            
            poolName ??= $"ParticleDataJobCompatibleNativePool<{typeof(T).Name}>_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating particle data JobCompatibleNativePool '{poolName}' with capacity {maxParticleCount}");
            
            // Create a job-friendly pool with specific settings for particle data
            var pool = new JobCompatibleNativePool<T>(maxParticleCount, allocator, defaultValue, poolName);
            pool.SetParallelJobSafe(true);
            pool.SetHighPerformanceMode(true);
            
            // Register with native registry
            if (NativeRegistry != null)
            {
                var handle = NativeRegistry.Register(pool);
                pool.SetRegistryHandle(handle);
            }
            
            return pool;
        }
    }
}