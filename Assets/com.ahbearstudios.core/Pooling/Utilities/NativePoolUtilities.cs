using System;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Native;
using AhBearStudios.Core.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Utilities
{
    /// <summary>
    /// Provides utility methods for working with native pools.
    /// Optimized for Unity Collections v2.
    /// </summary>
    [BurstCompile]
    public static class NativePoolUtilities
    {
        private static readonly PoolProfiler _profiler = PoolingServices.Profiler;

        #region AcquireMultiple

        /// <summary>
        /// Acquires multiple items from a native pool and returns their indices.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to acquire items from.</param>
        /// <param name="count">Number of items to acquire.</param>
        /// <param name="allocator">Allocator to use for the returned list of indices.</param>
        /// <returns>UnsafeList of acquired indices.</returns>
        public static UnsafeList<int> AcquireMultiple<T>(
            this NativePool<T> pool, 
            int count, 
            Allocator allocator = Allocator.TempJob) 
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");
                
            _profiler?.BeginSample("AcquireMultiple", $"Pool<{typeof(T).Name}>");
            
            var result = new UnsafeList<int>(count, allocator);

            try
            {
                // Ensure the pool has enough capacity
                pool.EnsureCapacity(pool.ActiveCount + count);

                // Acquire indices
                for (int i = 0; i < count; i++)
                {
                    int idx = pool.AcquireIndex();
                    if (idx < 0)
                        break;
                        
                    result.Add(idx);
                }
                
                _profiler?.EndSample("AcquireMultiple", $"Pool<{typeof(T).Name}>", result.Length, count - result.Length);
                
                return result;
            }
            catch (Exception)
            {
                // Clean up on error
                if (result.IsCreated)
                {
                    // Release any acquired indices
                    for (int i = 0; i < result.Length; i++)
                    {
                        pool.Release(result[i]);
                    }
                    
                    result.Dispose();
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Acquires multiple items from a burst-compatible native pool and returns their indices.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to acquire items from.</param>
        /// <param name="count">Number of items to acquire.</param>
        /// <param name="allocator">Allocator to use for the returned list of indices.</param>
        /// <returns>UnsafeList of acquired indices.</returns>
        public static UnsafeList<int> AcquireMultiple<T>(
            this BurstCompatibleNativePool<T> pool, 
            int count, 
            Allocator allocator = Allocator.TempJob) 
            where T : unmanaged
        {
            if (pool.IsDisposed)
                throw new ArgumentNullException(nameof(pool));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");
                
            _profiler?.BeginSample("AcquireMultiple", $"BurstCompatiblePool<{typeof(T).Name}>");
            
            var result = new UnsafeList<int>(count, allocator);

            try
            {
                // Ensure the pool has enough capacity
                pool.EnsureCapacity(pool.ActiveCount + count);

                // Acquire indices
                for (int i = 0; i < count; i++)
                {
                    int idx = pool.AcquireIndex();
                    if (idx < 0)
                        break;
                        
                    result.Add(idx);
                }
                
                _profiler?.EndSample("AcquireMultiple", $"BurstCompatiblePool<{typeof(T).Name}>", result.Length, count - result.Length);
                
                return result;
            }
            catch (Exception)
            {
                // Clean up on error
                if (result.IsCreated)
                {
                    // Release any acquired indices
                    for (int i = 0; i < result.Length; i++)
                    {
                        pool.Release(result[i]);
                    }
                    
                    result.Dispose();
                }
                
                throw;
            }
        }

        #endregion

        #region AcquireAndInitialize

        /// <summary>
        /// Acquires an item from the pool and initializes it with the provided function.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to acquire from.</param>
        /// <param name="initializer">Function to initialize the item.</param>
        /// <returns>Index of the acquired and initialized item.</returns>
        public static int AcquireAndInitialize<T>(
            this NativePool<T> pool, 
            Func<T, T> initializer) 
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("AcquireAndInitialize", $"Pool<{typeof(T).Name}>");
            
            int index = pool.AcquireIndex();
            
            if (index >= 0)
            {
                T value = pool.GetValue(index);
                T initializedValue = initializer(value);
                pool.SetValue(index, initializedValue);
            }
            
            _profiler?.EndSample("AcquireAndInitialize", $"Pool<{typeof(T).Name}>", index >= 0 ? 1 : 0, index >= 0 ? 0 : 1);
            
            return index;
        }

        /// <summary>
        /// Acquires an item from the burst-compatible pool and initializes it with the provided function.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to acquire from.</param>
        /// <param name="initializer">Function to initialize the item.</param>
        /// <returns>Index of the acquired and initialized item.</returns>
        public static int AcquireAndInitialize<T>(
            this BurstCompatibleNativePool<T> pool, 
            Func<T, T> initializer) 
            where T : unmanaged
        {
            if (pool.IsDisposed)
                throw new ArgumentNullException(nameof(pool));
                
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("AcquireAndInitialize", $"BurstCompatiblePool<{typeof(T).Name}>");
            
            int index = pool.AcquireIndex();
            
            if (index >= 0)
            {
                T value = pool.GetValue(index);
                T initializedValue = initializer(value);
                pool.SetValue(index, initializedValue);
            }
            
            _profiler?.EndSample("AcquireAndInitialize", $"BurstCompatiblePool<{typeof(T).Name}>", index >= 0 ? 1 : 0, index >= 0 ? 0 : 1);
            
            return index;
        }

        #endregion

        #region ReleaseMultiple

        /// <summary>
        /// Releases multiple items back to the pool by their indices.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to release items to.</param>
        /// <param name="indices">UnsafeList of indices to release.</param>
        public static void ReleaseMultiple<T>(
            this NativePool<T> pool, 
            UnsafeList<int> indices) 
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ReleaseMultiple", $"Pool<{typeof(T).Name}>");
            
            for (int i = 0; i < indices.Length; i++)
            {
                pool.Release(indices[i]);
            }
            
            _profiler?.EndSample("ReleaseMultiple", $"Pool<{typeof(T).Name}>", 0, indices.Length);
        }

        /// <summary>
        /// Releases multiple items back to the burst-compatible pool by their indices.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to release items to.</param>
        /// <param name="indices">UnsafeList of indices to release.</param>
        public static void ReleaseMultiple<T>(
            this BurstCompatibleNativePool<T> pool, 
            UnsafeList<int> indices) 
            where T : unmanaged
        {
            if (pool.IsDisposed)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ReleaseMultiple", $"BurstCompatiblePool<{typeof(T).Name}>");
            
            for (int i = 0; i < indices.Length; i++)
            {
                pool.Release(indices[i]);
            }
            
            _profiler?.EndSample("ReleaseMultiple", $"BurstCompatiblePool<{typeof(T).Name}>", 0, indices.Length);
        }

        /// <summary>
        /// Releases multiple items back to the job-compatible pool by their indices.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to release items to.</param>
        /// <param name="indices">UnsafeList of indices to release.</param>
        public static void ReleaseMultiple<T>(
            this JobCompatibleNativePool<T> pool, 
            UnsafeList<int> indices) 
            where T : unmanaged
        {
            if (pool.IsDisposed)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ReleaseMultiple", $"JobCompatiblePool<{typeof(T).Name}>");
            
            for (int i = 0; i < indices.Length; i++)
            {
                pool.Release(indices[i]);
            }
            
            _profiler?.EndSample("ReleaseMultiple", $"JobCompatiblePool<{typeof(T).Name}>", 0, indices.Length);
        }

        /// <summary>
        /// Releases and disposes indices from a native pool.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to release items to.</param>
        /// <param name="indices">UnsafeList of indices to release.</param>
        public static void ReleaseAndDisposeIndices<T>(
            this INativePool<T> pool,
            UnsafeList<int> indices) 
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ReleaseAndDisposeIndices", $"Pool<{typeof(T).Name}>");
            
            for (int i = 0; i < indices.Length; i++)
            {
                pool.Release(indices[i]);
            }
            
            indices.Dispose();
            
            _profiler?.EndSample("ReleaseAndDisposeIndices", $"Pool<{typeof(T).Name}>", 0, 0);
        }

        #endregion
        
        #region Job Scheduling

        /// <summary>
        /// Job that releases indices back to a pool after completion.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        [BurstCompile]
        public struct ReleaseIndicesAfterJobCompletionJob<T> : IJob where T : unmanaged
        {
            /// <summary>
            /// Pool handle to release indices to.
            /// </summary>
            public NativePoolHandle PoolHandle;
            
            /// <summary>
            /// List of indices to release.
            /// </summary>
            [ReadOnly]
            public UnsafeList<int> Indices;
            
            /// <summary>
            /// Executes the job.
            /// </summary>
            [BurstCompile]
            public void Execute()
            {
                if (!Indices.IsCreated)
                    return;
                    
                for (int i = 0; i < Indices.Length; i++)
                {
                    PoolHandle.Release(Indices[i]);
                }
            }
        }
        
        /// <summary>
        /// Job that disposes an UnsafeList after completion.
        /// </summary>
        /// <typeparam name="T">Type of items in the list.</typeparam>
        [BurstCompile]
        public struct DisposeUnsafeListJob<T> : IJob where T : unmanaged
        {
            /// <summary>
            /// List to dispose.
            /// </summary>
            [DeallocateOnJobCompletion]
            public UnsafeList<T> List;
            
            /// <summary>
            /// Executes the job.
            /// </summary>
            [BurstCompile]
            public void Execute()
            {
                // No action needed, list will be disposed by DeallocateOnJobCompletion attribute
            }
        }
        
        /// <summary>
        /// Creates a job to release indices after another job completes.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool to release indices to.</param>
        /// <param name="indices">List of indices to release.</param>
        /// <returns>Job to release indices.</returns>
        public static ReleaseIndicesAfterJobCompletionJob<T> CreateReleaseJob<T>(
            this NativePool<T> pool, 
            UnsafeList<int> indices) 
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            // Get pool handle from registry
            var handle = NativePoolRegistry.Instance.Register(pool);
            
            return new ReleaseIndicesAfterJobCompletionJob<T>
            {
                PoolHandle = handle,
                Indices = indices
            };
        }
        
        /// <summary>
        /// Schedules a job with automatic release of indices after completion.
        /// </summary>
        /// <typeparam name="TJob">Type of job to schedule.</typeparam>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="job">The job to schedule.</param>
        /// <param name="jobHandle">Dependency job handle.</param>
        /// <param name="pool">The pool to release indices to.</param>
        /// <param name="indices">List of indices to release.</param>
        /// <returns>Job handle for the scheduled jobs.</returns>
        public static JobHandle ScheduleWithAutoRelease<TJob, T>(
            this TJob job, 
            JobHandle jobHandle, 
            NativePool<T> pool,
            UnsafeList<int> indices) 
            where TJob : struct, IJob
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (!indices.IsCreated)
                throw new ArgumentException("Indices list is not created", nameof(indices));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ScheduleWithAutoRelease", $"Pool<{typeof(T).Name}>");
            
            // Schedule the main job
            var mainJobHandle = job.Schedule(jobHandle);
            
            // Create a release job
            var releaseJob = CreateReleaseJob(pool, indices);
            
            // Schedule the release job to run after the main job
            var releaseJobHandle = releaseJob.Schedule(mainJobHandle);
            
            // Create a dispose job for the indices list
            var disposeJob = new DisposeUnsafeListJob<int>
            {
                List = indices
            };
            
            // Schedule the dispose job to run after the release job
            var disposeJobHandle = disposeJob.Schedule(releaseJobHandle);
            
            _profiler?.EndSample("ScheduleWithAutoRelease", $"Pool<{typeof(T).Name}>", 0, 0);
            
            return disposeJobHandle;
        }
        
        /// <summary>
        /// Schedules a parallel foreach job to process active items in a pool.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <param name="pool">The pool containing items to process.</param>
        /// <param name="createJobFunc">Function to create the job.</param>
        /// <param name="releaseWhenComplete">Whether to release indices when complete.</param>
        /// <param name="batchSize">Batch size for parallel processing.</param>
        /// <param name="dependency">Dependency job handle.</param>
        /// <returns>Job handle for the scheduled job.</returns>
        public static JobHandle ScheduleParallelForEach<T, TJob>(
            this NativePool<T> pool,
            Func<UnsafeList<int>, TJob> createJobFunc,
            bool releaseWhenComplete = false,
            int batchSize = 32,
            JobHandle dependency = default)
            where TJob : struct, IJobParallelFor // Changed from IJobParallelForDefer to IJobParallelFor
            where T : unmanaged
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (createJobFunc == null)
                throw new ArgumentNullException(nameof(createJobFunc));
                
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            _profiler?.BeginSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>");
            
            // Get active indices
            var indices = new UnsafeList<int>(pool.ActiveCount, Allocator.TempJob);
            
            try
            {
                // Get active indices
                using var tempIndices = pool.GetActiveIndices(Allocator.Temp);
                
                // Copy to the TempJob allocated list
                for (int i = 0; i < tempIndices.Length; i++)
                {
                    indices.Add(tempIndices[i]);
                }
                
                if (indices.Length == 0)
                {
                    // No active indices, nothing to process
                    indices.Dispose();
                    
                    _profiler?.EndSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>", 0, 0);
                    
                    return dependency;
                }
                
                // Create the job
                TJob job = createJobFunc(indices);
                
                // Schedule the job using the proper IJobParallelFor scheduling method
                JobHandle jobHandle = job.Schedule(indices.Length, batchSize, dependency);
                
                if (releaseWhenComplete)
                {
                    // Create a release job
                    var releaseJob = CreateReleaseJob(pool, indices);
                    
                    // Schedule the release job
                    var releaseJobHandle = releaseJob.Schedule(jobHandle);
                    
                    // Create a dispose job
                    var disposeJob = new DisposeUnsafeListJob<int>
                    {
                        List = indices
                    };
                    
                    // Schedule the dispose job
                    jobHandle = disposeJob.Schedule(releaseJobHandle);
                }
                else
                {
                    // Just dispose the indices
                    var disposeJob = new DisposeUnsafeListJob<int>
                    {
                        List = indices
                    };
                    
                    // Schedule the dispose job
                    jobHandle = disposeJob.Schedule(jobHandle);
                }
                
                _profiler?.EndSample("ScheduleParallelForEach", $"Pool<{typeof(T).Name}>", indices.Length, 0);
                
                return jobHandle;
            }
            catch (Exception)
            {
                // Clean up on error
                if (indices.IsCreated)
                {
                    indices.Dispose();
                }
                
                throw;
            }
        }
         
        /// <summary>
        /// Schedules a job to process all active items in a burst-compatible pool.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool.</typeparam>
        /// <typeparam name="TJob">Type of job to schedule.</typeparam>
        /// <param name="pool">The pool containing items to process.</param>
        /// <param name="createJobFunc">Function to create the job.</param>
        /// <param name="batchSize">Batch size for parallel processing.</param>
        /// <param name="dependency">Dependency job handle.</param>
        /// <returns>Job handle for the scheduled job.</returns>
        public static JobHandle ScheduleProcessAllActive<T, TJob>(
            this BurstCompatibleNativePool<T> pool,
            Func<UnsafeList<int>, TJob> createJobFunc,
            int batchSize = 64,
            JobHandle dependency = default)
            where TJob : struct, IJobParallelFor // Changed from IJobParallelForDefer to IJobParallelFor
            where T : unmanaged
        {
            if (pool.IsDisposed)
                throw new ObjectDisposedException(nameof(pool));
                
            if (createJobFunc == null)
                throw new ArgumentNullException(nameof(createJobFunc));
                
            _profiler?.BeginSample("ScheduleProcessAllActive", $"BurstCompatiblePool<{typeof(T).Name}>");
            
            // Get active indices
            var indices = new UnsafeList<int>(pool.ActiveCount, Allocator.TempJob);
            
            try
            {
                // Get active indices
                using var tempIndices = pool.GetActiveIndices(Allocator.Temp);
                
                // Copy to the TempJob allocated list
                for (int i = 0; i < tempIndices.Length; i++)
                {
                    indices.Add(tempIndices[i]);
                }
                
                if (indices.Length == 0)
                {
                    // No active indices, nothing to process
                    indices.Dispose();
                    
                    _profiler?.EndSample("ScheduleProcessAllActive", $"BurstCompatiblePool<{typeof(T).Name}>", 0, 0);
                    
                    return dependency;
                }
                
                // Create the job
                TJob job = createJobFunc(indices);
                
                // Schedule the job using the proper IJobParallelFor scheduling method
                JobHandle jobHandle = job.Schedule(indices.Length, batchSize, dependency);
                
                // Create a dispose job
                var disposeJob = new DisposeUnsafeListJob<int>
                {
                    List = indices
                };
                
                // Schedule the dispose job
                jobHandle = disposeJob.Schedule(jobHandle);
                
                _profiler?.EndSample("ScheduleProcessAllActive", $"BurstCompatiblePool<{typeof(T).Name}>", indices.Length, 0);
                
                return jobHandle;
            }
            catch (Exception)
            {
                // Clean up on error
                if (indices.IsCreated)
                {
                    indices.Dispose();
                }
                
                throw;
            }
        }

        #endregion
    }
}