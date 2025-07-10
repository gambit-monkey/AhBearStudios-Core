using Unity.Burst;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Interface for Burst-compatible native pools optimized for maximum performance
    /// with Unity's Burst compiler and job system.
    /// </summary>
    public interface IBurstCompatibleNativePool : INativePool
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
        /// Gets a handle that provides direct access to the pool for use in jobs.
        /// </summary>
        /// <returns>A handle for using the pool in jobs</returns>
        [BurstCompile]
        NativePoolHandle GetHandle();

        /// <summary>
        /// Gets a read-only handle that provides direct access to the pool for use in jobs.
        /// Only allows reading of pool data, for thread-safe parallel operations.
        /// </summary>
        /// <returns>A read-only handle for using the pool in jobs</returns>
        [BurstCompile]
        NativePoolReadHandle GetReadOnlyHandle();
        
        /// <summary>
        /// Ensures the pool is not disposed and can be used safely
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the pool has been disposed</exception>
        void CheckDisposed();
    }

    /// <summary>
    /// Generic interface for Burst-compatible native pools of specific item types.
    /// Optimized for maximum performance with Unity's Burst compiler and job system.
    /// </summary>
    /// <typeparam name="T">Type of items in the native pool (must be unmanaged)</typeparam>
    public interface IBurstCompatibleNativePool<T> : IBurstCompatibleNativePool, INativePool<T> 
        where T : unmanaged
    {
        /// <summary>
        /// Gets a value at the specified index with Burst compatibility
        /// </summary>
        /// <param name="index">Index to access</param>
        /// <returns>The value at the specified index</returns>
        [BurstCompile]
        T Get(int index);

        /// <summary>
        /// Sets a value at the specified index with Burst compatibility
        /// </summary>
        /// <param name="index">Index to set</param>
        /// <param name="value">Value to set</param>
        [BurstCompile]
        void Set(int index, T value);

        /// <summary>
        /// Checks if an index is active with Burst compatibility
        /// </summary>
        /// <param name="index">Index to check</param>
        /// <returns>True if active, false otherwise</returns>
        [BurstCompile]
        bool IsActive(int index);
    }
}