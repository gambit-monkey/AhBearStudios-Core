using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Interface for Job-compatible native pools optimized for use with 
    /// Unity's job system and parallel processing.
    /// </summary>
    public interface IJobCompatibleNativePool : INativePool
    {
        /// <summary>
        /// Gets whether this pool is created and ready to use
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets a job handle that allows synchronization with previous jobs
        /// that may be accessing this pool.
        /// </summary>
        JobHandle CurrentJobHandle { get; }

        /// <summary>
        /// Sets the job handle to synchronize with before allowing further access to the pool.
        /// </summary>
        /// <param name="handle">Job handle to complete before further pool access</param>
        void SetCurrentJobHandle(JobHandle handle);

        /// <summary>
        /// Gets a handle that provides direct access to the pool for use in jobs.
        /// </summary>
        /// <returns>A handle for using the pool in jobs</returns>
        NativePoolHandle GetJobHandle();

        /// <summary>
        /// Gets a read-only handle that provides direct access to the pool for use in jobs.
        /// Only allows reading of pool data, for thread-safe parallel operations.
        /// </summary>
        /// <returns>A read-only handle for using the pool in jobs</returns>
        NativePoolReadHandle GetReadOnlyJobHandle();

        /// <summary>
        /// Completes any pending jobs that are accessing this pool
        /// before allowing further access.
        /// </summary>
        void CompleteAllJobs();
        
        /// <summary>
        /// Ensures the pool is not disposed and can be used safely
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the pool has been disposed</exception>
        void CheckDisposed();
    }

    /// <summary>
    /// Generic interface for Job-compatible native pools of specific item types.
    /// Optimized for use with Unity's job system and parallel processing.
    /// </summary>
    /// <typeparam name="T">Type of items in the native pool (must be unmanaged)</typeparam>
    public interface IJobCompatibleNativePool<T> : IJobCompatibleNativePool, INativePool<T> 
        where T : unmanaged
    {
        /// <summary>
        /// Gets a value at the specified index with thread safety for jobs
        /// </summary>
        /// <param name="index">Index to access</param>
        /// <returns>The value at the specified index</returns>
        T Get(int index);

        /// <summary>
        /// Sets a value at the specified index with thread safety for jobs
        /// </summary>
        /// <param name="index">Index to set</param>
        /// <param name="value">Value to set</param>
        void Set(int index, T value);

        /// <summary>
        /// Checks if an index is active with thread safety for jobs
        /// </summary>
        /// <param name="index">Index to check</param>
        /// <returns>True if active, false otherwise</returns>
        bool IsActive(int index);

        /// <summary>
        /// Gets a parallel writer handle for use in parallel jobs
        /// </summary>
        /// <returns>A handle that allows concurrent writes from multiple threads</returns>
        NativePoolParallelWriter<T> AsParallelWriter();
    }
}