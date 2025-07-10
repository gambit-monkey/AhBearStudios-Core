using System;
using Unity.Collections;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Jobs
{
    /// <summary>
    /// Extension methods for scheduling jobs with auto-release of resources
    /// </summary>
    public static class JobSchedulingExtensions
    {
        /// <summary>
        /// Schedules a job with auto-release of native collections
        /// </summary>
        /// <typeparam name="TJob">Type of job, must implement IJob</typeparam>
        /// <param name="job">The job</param>
        /// <param name="dependency">Job dependency</param>
        /// <param name="collections">Native collections to dispose after job completes</param>
        /// <returns>Job handle for the combined operations</returns>
        public static JobHandle ScheduleWithAutoDispose<TJob>(
            this TJob job,
            JobHandle dependency,
            params IDisposable[] collections)
            where TJob : struct, IJob
        {
            var jobHandle = job.Schedule(dependency);
            
            // Chain dispose operations
            if (collections != null && collections.Length > 0)
            {
                for (int i = 0; i < collections.Length; i++)
                {
                    var collection = collections[i];
                    if (collection != null)
                    {
                        // For collections that support deferred disposal
                        if (collection is INativeDisposable nativeDisposable)
                        {
                            jobHandle = nativeDisposable.Dispose(jobHandle);
                        }
                    }
                }
            }
            
            return jobHandle;
        }
        
        /// <summary>
        /// Schedules a parallel job with auto-release of native collections
        /// </summary>
        /// <typeparam name="TJob">Type of job, must implement IJobParallelFor</typeparam>
        /// <param name="job">The job</param>
        /// <param name="arrayLength">Length of the array being processed</param>
        /// <param name="innerloopBatchCount">Batch count for parallel processing</param>
        /// <param name="dependency">Job dependency</param>
        /// <param name="collections">Native collections to dispose after job completes</param>
        /// <returns>Job handle for the combined operations</returns>
        public static JobHandle ScheduleWithAutoDispose<TJob>(
            this TJob job,
            int arrayLength,
            int innerloopBatchCount,
            JobHandle dependency,
            params IDisposable[] collections)
            where TJob : struct, IJobParallelFor
        {
            var jobHandle = job.Schedule(arrayLength, innerloopBatchCount, dependency);
            
            // Chain dispose operations
            if (collections != null && collections.Length > 0)
            {
                for (int i = 0; i < collections.Length; i++)
                {
                    var collection = collections[i];
                    if (collection != null)
                    {
                        // For collections that support deferred disposal
                        if (collection is INativeDisposable nativeDisposable)
                        {
                            jobHandle = nativeDisposable.Dispose(jobHandle);
                        }
                    }
                }
            }
            
            return jobHandle;
        }
    }
}