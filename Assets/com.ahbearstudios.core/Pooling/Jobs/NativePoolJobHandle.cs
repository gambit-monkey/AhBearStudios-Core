using System;
using AhBearStudios.Core.Pooling.Pools.Native;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Jobs
{
    /// <summary>
    /// Represents a job handle for a native pool job that will release indices when complete
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public readonly struct NativePoolJobHandle<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// The underlying job handle
        /// </summary>
        public readonly JobHandle JobHandle;
        
        /// <summary>
        /// The pool handle
        /// </summary>
        public readonly NativePoolHandle PoolHandle;
        
        /// <summary>
        /// The indices that should be released
        /// </summary>
        private readonly UnsafeList<int> _indices;
        
        /// <summary>
        /// Creates a new NativePoolJobHandle
        /// </summary>
        /// <param name="jobHandle">The job handle</param>
        /// <param name="poolHandle">The pool handle</param>
        /// <param name="indices">The indices that should be released when the job completes</param>
        public NativePoolJobHandle(JobHandle jobHandle, NativePoolHandle poolHandle, UnsafeList<int> indices)
        {
            JobHandle = jobHandle;
            PoolHandle = poolHandle;
            _indices = indices;
        }
        
        /// <summary>
        /// Completes the job and releases the indices
        /// </summary>
        public void Complete()
        {
            JobHandle.Complete();
            
            // Release all indices
            for (int i = 0; i < _indices.Length; i++)
            {
                PoolHandle.Release(_indices[i]);
            }
            
            // Dispose indices list
            if (_indices.IsCreated)
            {
                _indices.Dispose();
            }
        }
        
        /// <summary>
        /// Disposes resources used by this handle
        /// </summary>
        public void Dispose()
        {
            Complete();
        }
        
        /// <summary>
        /// Implicitly converts a NativePoolJobHandle to a JobHandle
        /// </summary>
        /// <param name="handle">The NativePoolJobHandle to convert</param>
        /// <returns>A JobHandle</returns>
        public static implicit operator JobHandle(NativePoolJobHandle<T> handle)
        {
            return handle.JobHandle;
        }
    }
}