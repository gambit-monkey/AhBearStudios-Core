using System;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Pools.Native;
using AhBearStudios.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Pooling.Jobs
{
    /// <summary>
    /// Helper class for scheduling pooled processing jobs.
    /// Contains non-Burst compatible methods separated from the job struct.
    /// </summary>
    /// <typeparam name="T">Type of data to process</typeparam>
    public static class PooledProcessingJobScheduler<T> where T : unmanaged
    {
        // Static profiler reference
        private static readonly PoolProfiler _profiler = PoolingServices.Profiler;

        /// <summary>
        /// Schedules a job to process items in a native pool
        /// </summary>
        /// <param name="pool">The native pool to process</param>
        /// <param name="processor">Function to process each item</param>
        /// <param name="deltaTime">Delta time for time-based processing</param>
        /// <param name="dependency">Job dependency</param>
        /// <param name="batchSize">Batch size for parallel processing</param>
        /// <returns>Job handle that will automatically clean up resources</returns>
        public static NativePoolJobHandle<T> ScheduleProcessing(
            INativePool<T> pool,
            PooledProcessingJob<T>.ProcessItemDelegate processor,
            float deltaTime = 0,
            JobHandle dependency = default,
            int batchSize = 32)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (pool.IsDisposed) throw new ObjectDisposedException(nameof(pool));

            _profiler?.BeginSample("ScheduleProcessing", $"Pool<{typeof(T).Name}>");

            // Get pool handle
            NativePoolHandle handle;
            if (pool is JobCompatibleNativePool<T> jobCompatible)
            {
                handle = jobCompatible.GetHandle();
            }
            else
            {
                handle = NativePoolRegistry.Instance.Register(pool);
            }

            // Get active indices
            var indices = new UnsafeList<int>(pool.Capacity, Allocator.TempJob);
            try
            {
                handle.GetActiveIndicesUnsafe(ref indices);

                if (indices.Length == 0)
                {
                    _profiler?.EndSample("ScheduleProcessing", $"Pool<{typeof(T).Name}>", 0, 0);
                    return new NativePoolJobHandle<T>(dependency, handle, indices);
                }

                var job = new PooledProcessingJob<T>
                {
                    Handle = handle,
                    ProcessItem = BurstCompiler.CompileFunctionPointer(processor),
                    DeltaTime = deltaTime,
                    PoolIndices = indices
                };

                JobHandle jobHandle = job.Schedule(indices.Length, batchSize, dependency);
                
                _profiler?.EndSample("ScheduleProcessing", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
                return new NativePoolJobHandle<T>(jobHandle, handle, indices);
            }
            catch (Exception)
            {
                indices.Dispose();
                throw;
            }
        }
        
        /// <summary>
        /// Schedules multiple jobs to process items in sequence
        /// </summary>
        /// <param name="pool">The native pool to process</param>
        /// <param name="processors">Array of functions to process each item</param>
        /// <param name="deltaTime">Delta time for time-based processing</param>
        /// <param name="dependency">Job dependency</param>
        /// <param name="batchSize">Batch size for parallel processing</param>
        /// <returns>Job handle that will automatically clean up resources</returns>
        public static NativePoolJobHandle<T> ScheduleProcessingBatch(
            INativePool<T> pool,
            PooledProcessingJob<T>.ProcessItemDelegate[] processors,
            float deltaTime = 0,
            JobHandle dependency = default,
            int batchSize = 32)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (processors == null || processors.Length == 0) 
                throw new ArgumentException("No processors provided", nameof(processors));
            if (pool.IsDisposed) throw new ObjectDisposedException(nameof(pool));

            _profiler?.BeginSample("ScheduleProcessingBatch", $"Pool<{typeof(T).Name}>");

            // Get pool handle
            NativePoolHandle handle;
            if (pool is JobCompatibleNativePool<T> jobCompatible)
            {
                handle = jobCompatible.GetHandle();
            }
            else
            {
                handle = NativePoolRegistry.Instance.Register(pool);
            }

            // Get active indices
            var indices = new UnsafeList<int>(pool.Capacity, Allocator.TempJob);
            try
            {
                handle.GetActiveIndicesUnsafe(ref indices);

                if (indices.Length == 0)
                {
                    _profiler?.EndSample("ScheduleProcessingBatch", $"Pool<{typeof(T).Name}>", 0, 0);
                    return new NativePoolJobHandle<T>(dependency, handle, indices);
                }

                var jobHandle = dependency;

                // Schedule each job with the previous job as a dependency
                foreach (var processor in processors)
                {
                    var job = new PooledProcessingJob<T>
                    {
                        Handle = handle,
                        ProcessItem = BurstCompiler.CompileFunctionPointer(processor),
                        DeltaTime = deltaTime,
                        PoolIndices = indices
                    };

                    jobHandle = job.Schedule(indices.Length, batchSize, jobHandle);
                }

                _profiler?.EndSample("ScheduleProcessingBatch", $"Pool<{typeof(T).Name}>", pool.ActiveCount, pool.InactiveCount);
                return new NativePoolJobHandle<T>(jobHandle, handle, indices);
            }
            catch (Exception)
            {
                indices.Dispose();
                throw;
            }
        }
    }
}