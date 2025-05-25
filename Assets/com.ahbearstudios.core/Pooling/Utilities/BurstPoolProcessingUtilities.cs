using System;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Utilities
{
    /// <summary>
    /// Provides high-performance, Burst-compatible utility functions for processing 
    /// native pools efficiently, including parallel processing capabilities.
    /// </summary>
    public static class BurstPoolProcessingUtilities
    {
        private static readonly PoolProfiler _profiler;
        
        /// <summary>
        /// Static constructor to initialize services
        /// </summary>
        static BurstPoolProcessingUtilities()
        {
            if (!PoolingServices.HasService<PoolProfiler>())
            {
                PoolingServices.Initialize();
            }
            
            _profiler = PoolingServices.Profiler;
        }
        
        /// <summary>
        /// Copies active items from a source pool to a destination pool.
        /// </summary>
        /// <typeparam name="T">Type of items in the pools</typeparam>
        /// <param name="sourcePool">Source pool to copy from</param>
        /// <param name="destPool">Destination pool to copy to</param>
        /// <param name="allocator">Allocator to use for temporary storage</param>
        /// <returns>Array of indices in the destination pool where items were copied</returns>
        /// <exception cref="ArgumentNullException">Thrown if either pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if either pool is disposed</exception>
        public static NativeArray<int> CopyActiveItems<T>(
            INativePool<T> sourcePool, 
            INativePool<T> destPool,
            Allocator allocator) where T : unmanaged
        {
            _profiler?.BeginSample("CopyActiveItems", $"Pools<{typeof(T).Name}>");
            
            if (sourcePool == null)
                throw new ArgumentNullException(nameof(sourcePool));
                
            if (destPool == null)
                throw new ArgumentNullException(nameof(destPool));
                
            if (sourcePool.IsDisposed)
                throw new ObjectDisposedException(nameof(sourcePool));
                
            if (destPool.IsDisposed)
                throw new ObjectDisposedException(nameof(destPool));
            
            // Get all active indices from source pool
            var activeIndices = sourcePool.GetActiveIndices(allocator);
            
            // Early out if no active items
            if (activeIndices.Length == 0)
            {
                _profiler?.EndSample("CopyActiveItems", $"Pools<{typeof(T).Name}>", 0, 0);
                return new NativeArray<int>(0, allocator);
            }
            
            // Ensure destination pool has enough capacity
            destPool.EnsureCapacity(destPool.Capacity + activeIndices.Length);
            
            // Create array for destination indices
            var destIndices = new NativeArray<int>(activeIndices.Length, allocator, NativeArrayOptions.UninitializedMemory);
            
            // Copy each active item to the destination pool
            for (int i = 0; i < activeIndices.Length; i++)
            {
                // Get item from source
                T item = sourcePool.GetValue(activeIndices[i]);
                
                // Acquire index in destination pool
                int destIndex = destPool.AcquireIndex();
                
                // If dest pool is full, resize remaining results and break
                if (destIndex < 0)
                {
                    var truncatedDestIndices = new NativeArray<int>(i, allocator, NativeArrayOptions.UninitializedMemory);
                    for (int j = 0; j < i; j++)
                    {
                        truncatedDestIndices[j] = destIndices[j];
                    }
                    destIndices.Dispose();
                    activeIndices.Dispose();
                    
                    _profiler?.EndSample("CopyActiveItems", $"Pools<{typeof(T).Name}>", destPool.ActiveCount, destPool.InactiveCount);
                    return truncatedDestIndices;
                }
                
                // Store value in destination pool
                destPool.SetValue(destIndex, item);
                destIndices[i] = destIndex;
            }
            
            // Clean up source indices array
            activeIndices.Dispose();
            
            _profiler?.EndSample("CopyActiveItems", $"Pools<{typeof(T).Name}>", destPool.ActiveCount, destPool.InactiveCount);
            
            return destIndices;
        }
        
        /// <summary>
        /// Filters items in a pool based on a predicate and returns the indices of matching items.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">Pool to filter</param>
        /// <param name="allocator">Allocator to use for the results</param>
        /// <param name="filterFunc">Predicate to filter items</param>
        /// <returns>UnsafeList containing indices of matching items</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool or filterFunc is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        public static UnsafeList<int> FilterItems<T>(
            INativePool<T> pool, 
            Allocator allocator,
            Func<T, bool> filterFunc) where T : unmanaged
        {
            _profiler?.BeginSample("FilterItems", $"Pool<{typeof(T).Name}>");
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (filterFunc == null)
                throw new ArgumentNullException(nameof(filterFunc));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
            
            // Get active indices
            var activeIndices = pool.GetActiveIndices(Allocator.Temp);
            
            // Create result list with initial capacity matching active count
            var results = new UnsafeList<int>(activeIndices.Length, allocator);
            
            // Apply filter to each active item
            for (int i = 0; i < activeIndices.Length; i++)
            {
                int index = activeIndices[i];
                T item = pool.GetValue(index);
                
                if (filterFunc(item))
                {
                    results.Add(index);
                }
            }
            
            // Clean up temporary array
            activeIndices.Dispose();
            
            _profiler?.EndSample("FilterItems", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
            
            return results;
        }
        
        /// <summary>
        /// Processes items in a pool and returns the results.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <typeparam name="TResult">Type of the processed results</typeparam>
        /// <param name="pool">Pool to process</param>
        /// <param name="processFunc">Function to process each item</param>
        /// <param name="allocator">Allocator to use for the results</param>
        /// <returns>UnsafeList containing the processed results</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool or processFunc is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        public static UnsafeList<TResult> ProcessItems<T, TResult>(
            INativePool<T> pool, 
            Func<T, TResult> processFunc,
            Allocator allocator) where T : unmanaged where TResult : unmanaged
        {
            _profiler?.BeginSample("ProcessItems", $"Pool<{typeof(T).Name}>");
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (processFunc == null)
                throw new ArgumentNullException(nameof(processFunc));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
            
            // Get active indices
            var activeIndices = pool.GetActiveIndices(Allocator.Temp);
            
            // Create result list with initial capacity matching active count
            var results = new UnsafeList<TResult>(activeIndices.Length, allocator);
            
            // Process each active item
            for (int i = 0; i < activeIndices.Length; i++)
            {
                int index = activeIndices[i];
                T item = pool.GetValue(index);
                
                TResult result = processFunc(item);
                results.Add(result);
            }
            
            // Clean up temporary array
            activeIndices.Dispose();
            
            _profiler?.EndSample("ProcessItems", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
            
            return results;
        }
        
        /// <summary>
        /// Maps items from a source pool to a target pool using a transformation function.
        /// </summary>
        /// <typeparam name="TSource">Type of items in the source pool</typeparam>
        /// <typeparam name="TTarget">Type of items in the target pool</typeparam>
        /// <param name="sourcePool">Source pool to map from</param>
        /// <param name="targetPool">Target pool to map to</param>
        /// <param name="transformFunc">Function to transform source items to target items</param>
        /// <param name="allocator">Allocator to use for the results</param>
        /// <returns>UnsafeList containing indices in the target pool of mapped items</returns>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if either pool is disposed</exception>
        public static UnsafeList<int> MapItems<TSource, TTarget>(
            INativePool<TSource> sourcePool, 
            INativePool<TTarget> targetPool, 
            Func<TSource, TTarget> transformFunc,
            Allocator allocator) where TSource : unmanaged where TTarget : unmanaged
        {
            _profiler?.BeginSample("MapItems", $"Pools<{typeof(TSource).Name},{typeof(TTarget).Name}>");
            
            if (sourcePool == null)
                throw new ArgumentNullException(nameof(sourcePool));
                
            if (targetPool == null)
                throw new ArgumentNullException(nameof(targetPool));
                
            if (transformFunc == null)
                throw new ArgumentNullException(nameof(transformFunc));
                
            if (sourcePool.IsDisposed)
                throw new ObjectDisposedException(nameof(sourcePool));
                
            if (targetPool.IsDisposed)
                throw new ObjectDisposedException(nameof(targetPool));
            
            // Get active indices from source pool
            var activeIndices = sourcePool.GetActiveIndices(Allocator.Temp);
            
            // Ensure target pool has enough capacity
            targetPool.EnsureCapacity(targetPool.Capacity + activeIndices.Length);
            
            // Create list for target indices
            var targetIndices = new UnsafeList<int>(activeIndices.Length, allocator);
            
            // Map each active item to the target pool
            for (int i = 0; i < activeIndices.Length; i++)
            {
                // Get source item
                TSource sourceItem = sourcePool.GetValue(activeIndices[i]);
                
                // Transform to target type
                TTarget targetItem = transformFunc(sourceItem);
                
                // Acquire index in target pool
                int targetIndex = targetPool.AcquireIndex();
                
                // If target pool is full, break
                if (targetIndex < 0)
                    break;
                
                // Store value in target pool
                targetPool.SetValue(targetIndex, targetItem);
                targetIndices.Add(targetIndex);
            }
            
            // Clean up temporary array
            activeIndices.Dispose();
            
            _profiler?.EndSample("MapItems", $"Pools<{typeof(TSource).Name},{typeof(TTarget).Name}>", 
                targetPool.ActiveCount, targetPool.InactiveCount);
            
            return targetIndices;
        }
        
        /// <summary>
        /// Executes an action for each active item in a pool.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">Pool to iterate</param>
        /// <param name="action">Action to execute for each item, receiving the item and its index</param>
        /// <exception cref="ArgumentNullException">Thrown if pool or action is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        public static void ForEach<T>(
            INativePool<T> pool, 
            Action<T, int> action) where T : unmanaged
        {
            _profiler?.BeginSample("ForEach", $"Pool<{typeof(T).Name}>");
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
            
            // Get active indices
            var activeIndices = pool.GetActiveIndices(Allocator.Temp);
            
            // Execute action for each active item
            for (int i = 0; i < activeIndices.Length; i++)
            {
                int index = activeIndices[i];
                T item = pool.GetValue(index);
                
                action(item, index);
            }
            
            // Clean up temporary array
            activeIndices.Dispose();
            
            _profiler?.EndSample("ForEach", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
        }
        
        /// <summary>
        /// Schedules a job to process all active items in a pool in parallel.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <typeparam name="TJob">Type of the job to schedule, must implement IJobParallelFor</typeparam>
        /// <param name="pool">Pool to process</param>
        /// <param name="createJobFunc">Function to create the job with the active indices</param>
        /// <param name="dependency">Job dependency</param>
        /// <param name="batchSize">Batch size for parallel processing</param>
        /// <returns>JobHandle for the scheduled job</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool or createJobFunc is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        public static JobHandle ScheduleParallelForEach<T, TJob>(
            INativePool<T> pool, 
            Func<UnsafeList<int>, TJob> createJobFunc,
            JobHandle dependency = default,
            int batchSize = 32) 
            where T : unmanaged 
            where TJob : struct, IJobParallelFor
        {
            _profiler?.BeginSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>");
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (createJobFunc == null)
                throw new ArgumentNullException(nameof(createJobFunc));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
            
            // Create list with active indices using Persistent allocator (will be disposed after job completes)
            var activeIndices = new UnsafeList<int>(pool.ActiveCount, Allocator.Persistent);
            
            // Fill list with active indices
            using (var tempIndices = pool.GetActiveIndices(Allocator.Temp))
            {
                activeIndices.Resize(tempIndices.Length, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < tempIndices.Length; i++)
                {
                    activeIndices[i] = tempIndices[i];
                }
            }
            
            // Early out if no active items
            if (activeIndices.Length == 0)
            {
                activeIndices.Dispose();
                _profiler?.EndSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
                return dependency;
            }
            
            // Create the job
            TJob job = createJobFunc(activeIndices);
            
            // Schedule the job
            var jobHandle = job.Schedule(activeIndices.Length, batchSize, dependency);
            
            // Schedule a job to dispose the indices list after the main job completes
            var disposeJob = new DisposeUnsafeListJob<int> { List = activeIndices };
            var finalHandle = disposeJob.Schedule(jobHandle);
            
            _profiler?.EndSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
            
            return finalHandle;
        }
        
        /// <summary>
        /// Job to dispose an UnsafeList when a job completes
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        [BurstCompile]
        private struct DisposeUnsafeListJob<T> : IJob where T : unmanaged
        {
            [DeallocateOnJobCompletion] public UnsafeList<T> List;
            
            /// <summary>
            /// Executes the job to dispose the list
            /// </summary>
            public void Execute() { }
        }
    }
}