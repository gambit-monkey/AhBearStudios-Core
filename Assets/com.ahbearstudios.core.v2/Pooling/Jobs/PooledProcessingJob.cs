using System.Runtime.InteropServices;
using AhBearStudios.Core.Pooling.Pools.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Jobs
{
    /// <summary>
    /// Burst-compilable job for processing items in a native pool in parallel.
    /// Optimized for Unity Collections v2.
    /// </summary>
    [BurstCompile]
    public struct PooledProcessingJob<T> : IJobParallelFor where T : unmanaged
    {
        /// <summary>
        /// Native pool handle
        /// </summary>
        [ReadOnly] public NativePoolHandle Handle;
        
        /// <summary>
        /// Function pointer to process each item
        /// </summary>
        [ReadOnly] public FunctionPointer<ProcessItemDelegate> ProcessItem;
        
        /// <summary>
        /// Delta time for time-based processing
        /// </summary>
        [ReadOnly] public float DeltaTime;
        
        /// <summary>
        /// List of pool indices to process
        /// </summary>
        [ReadOnly] public UnsafeList<int> PoolIndices;
        
        /// <summary>
        /// Delegate for processing an item in the pool
        /// </summary>
        /// <param name="itemIndex">Index of the item in the pool</param>
        /// <param name="deltaTime">Delta time for time-based processing</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessItemDelegate(int itemIndex, float deltaTime);
        
        /// <summary>
        /// Executes the job for a single index
        /// </summary>
        /// <param name="index">Index in the job array (not directly the pool index)</param>
        [BurstCompile]
        public void Execute(int index)
        {
            int poolIndex = PoolIndices[index];
            if (poolIndex >= 0 && Handle.IsActive(poolIndex))
            {
                ProcessItem.Invoke(poolIndex, DeltaTime);
            }
        }

        /// <summary>
        /// Schedule a PooledProcessingJob with an UnsafeList of indices
        /// </summary>
        /// <param name="indices">UnsafeList of indices to process</param>
        /// <param name="batchSize">Batch size for parallel processing</param>
        /// <param name="dependsOn">Job dependency</param>
        /// <returns>JobHandle for the scheduled job</returns>
        public JobHandle Schedule(UnsafeList<int> indices, int batchSize, JobHandle dependsOn)
        {
            this.PoolIndices = indices;
            return this.Schedule(indices.Length, batchSize, dependsOn);
        }
    }
}