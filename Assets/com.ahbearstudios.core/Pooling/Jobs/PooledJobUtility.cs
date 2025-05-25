using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AhBearStudios.Pooling.Jobs
{
    /// <summary>
    /// Provides utility methods for working with pooled objects in Unity Jobs.
    /// </summary>
    [BurstCompile]
    public static class PooledJobUtility
    {
        /// <summary>
        /// Schedules a job to release a batch of pooled items.
        /// </summary>
        /// <param name="indicesToRelease">Indices to release.</param>
        /// <param name="poolId">The pool ID.</param>
        /// <returns>JobHandle for the scheduled job.</returns>
        public static JobHandle ScheduleReleaseItems(NativeArray<int> indicesToRelease, int poolId)
        {
            var job = new ReleasePoolItemsJob
            {
                IndicesToRelease = indicesToRelease,
                PoolId = poolId
            };
            
            return job.Schedule();
        }
        
        /// <summary>
        /// Job for releasing multiple items back to a native pool.
        /// </summary>
        [BurstCompile]
        public struct ReleasePoolItemsJob : IJob
        {
            /// <summary>
            /// Array of indices to release.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> IndicesToRelease;
            
            /// <summary>
            /// ID of the pool to release items to.
            /// </summary>
            public int PoolId;
            
            /// <summary>
            /// Executes the job to release all items.
            /// </summary>
            public void Execute()
            {
                // In a Burst job context, we cannot call methods that might box value types
                // Instead, we'll use direct unsafe access to operate on the pool data
                
                // This is a simplified version, in production you'd have proper unsafe access methods
                for (int i = 0; i < IndicesToRelease.Length; i++)
                {
                    int index = IndicesToRelease[i];
                    // Using a hypothetical direct access method that's Burst compatible
                    // NativePoolRegistry.ReleaseDirectUnsafe(PoolId, index);
                }
            }
        }
        
        /// <summary>
        /// Creates a job that processes and releases pool items.
        /// </summary>
        /// <typeparam name="T">Type of data to process.</typeparam>
        /// <param name="data">Data to process.</param>
        /// <param name="poolId">Pool ID.</param>
        /// <param name="tempAllocator">Allocator for temporary data.</param>
        /// <returns>A job handle for the scheduled job.</returns>
        public static JobHandle ScheduleProcessAndRelease<T>(NativeArray<T> data, int poolId, Allocator tempAllocator = Allocator.TempJob)
            where T : unmanaged
        {
            // Important: Use NativeArray or NativeList for Burst compatibility, not UnsafeList
            var tempIndices = new NativeList<int>(data.Length, tempAllocator);
            
            // First job to identify items for release
            var processJob = new ProcessPoolItemsJob<T>
            {
                Data = data,
                Results = tempIndices
            };
            
            var jobHandle = processJob.Schedule();
            
            // Second job to release identified items
            var releaseJob = new BurstCompatibleReleaseJob
            {
                IndicesToRelease = tempIndices.AsDeferredJobArray(),
                PoolId = poolId
            };
            
            return releaseJob.Schedule(jobHandle);
        }
        
        /// <summary>
        /// Job that processes pool items to determine which ones to release.
        /// </summary>
        [BurstCompile]
        public struct ProcessPoolItemsJob<T> : IJob where T : unmanaged
        {
            /// <summary>
            /// Data to process.
            /// </summary>
            [ReadOnly]
            public NativeArray<T> Data;
            
            /// <summary>
            /// Output list of indices to release.
            /// </summary>
            public NativeList<int> Results;
            
            /// <summary>
            /// Executes the job to identify items for release.
            /// </summary>
            public void Execute()
            {
                // In a real implementation, apply your criteria to determine
                // which items should be released
                
                // For example, mark first N items for release
                int count = math.min(Data.Length / 2, 10);
                for (int i = 0; i < count; i++)
                {
                    Results.Add(i);
                }
            }
        }
        
        /// <summary>
        /// Job that releases items in a Burst-compatible way.
        /// </summary>
        [BurstCompile]
        public struct BurstCompatibleReleaseJob : IJob
        {
            /// <summary>
            /// Array of indices to release.
            /// </summary>
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<int> IndicesToRelease;
            
            /// <summary>
            /// Pool ID to release items to.
            /// </summary>
            public int PoolId;
            
            /// <summary>
            /// Executes the job to release items.
            /// </summary>
            public void Execute()
            {
                // Direct access to release indices without boxing
                for (int i = 0; i < IndicesToRelease.Length; i++)
                {
                    int index = IndicesToRelease[i];
                    // In production code, you would call a Burst-compatible method here
                    // that directly manipulates the pool's internal data structures
                    // NativePoolRegistry.ReleaseDirectUnsafe(PoolId, index);
                }
            }
        }
    }
}