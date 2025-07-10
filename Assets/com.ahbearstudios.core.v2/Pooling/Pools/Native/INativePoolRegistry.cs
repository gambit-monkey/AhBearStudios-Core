using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Interface for a registry that manages native pools.
    /// Enables dependency injection and abstraction of pool registry operations.
    /// </summary>
    public interface INativePoolRegistry : IDisposable
    {
        /// <summary>
        /// Gets whether this registry is created and valid
        /// </summary>
        bool IsCreated { get; }
        
        /// <summary>
        /// Gets the maximum number of pools supported by this registry
        /// </summary>
        int MaxPools { get; }
        
        /// <summary>
        /// Registers a pool with the registry
        /// </summary>
        /// <param name="typeId">Type ID of the pool</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>ID of the registered pool</returns>
        int RegisterPool(int typeId, int initialCapacity = 0);
        
        /// <summary>
        /// Registers a typed pool instance with the registry
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">Pool instance to register</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>NativePoolHandle with the registered pool ID</returns>
        NativePoolHandle Register<T>(INativePool<T> pool, int initialCapacity = 0) where T : unmanaged;
        
        /// <summary>
        /// Unregisters a pool from the registry
        /// </summary>
        /// <param name="poolId">ID of the pool to unregister</param>
        void Unregister(int poolId);
        
        /// <summary>
        /// Gets the type ID for a specific type
        /// </summary>
        /// <typeparam name="T">Type to get ID for</typeparam>
        /// <returns>Type ID, or -1 if not found</returns>
        int GetTypeId<T>() where T : unmanaged;
        
        /// <summary>
        /// Gets a typed pool by ID
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>The pool instance, or null if not found</returns>
        INativePool<T> GetPool<T>(int poolId) where T : unmanaged;
        
        /// <summary>
        /// Sets a pool as disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="isDisposed">Whether the pool is disposed</param>
        void SetPoolDisposed(int poolId, bool isDisposed);
        
        /// <summary>
        /// Updates the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="capacity">New capacity</param>
        void UpdateCapacity(int poolId, int capacity);
        
        /// <summary>
        /// Updates the active count of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="count">New active count</param>
        void UpdateActiveCount(int poolId, int count);
        
        /// <summary>
        /// Sets an index as active or inactive
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to set</param>
        /// <param name="active">Whether the index is active</param>
        void SetIndexActive(int poolId, int index, bool active);
        
        /// <summary>
        /// Tries to acquire an index from a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Index acquired, or -1 if full</returns>
        int TryAcquireIndex(int poolId);
        
        /// <summary>
        /// Tries to release an index back to a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        /// <returns>True if successful, false otherwise</returns>
        bool TryReleaseIndex(int poolId, int index);
        
        /// <summary>
        /// Gets active indices for a specific pool using NativeArray
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">NativeArray to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        int GetActiveIndices(int poolId, NativeArray<int> indices);
        
        /// <summary>
        /// Gets active indices for a specific pool using UnsafeList
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">UnsafeList to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        int GetActiveIndices(int poolId, ref UnsafeList<int> indices);
        
        /// <summary>
        /// Gets active indices for a specific pool writing directly to a pointer
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indicesPtr">Pointer to array where indices will be written</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of active indices written</returns>
        unsafe int GetActiveIndicesPtr(int poolId, int* indicesPtr, int maxLength);
        
        /// <summary>
        /// Checks if a pool is registered with the registry
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if registered, false otherwise</returns>
        bool IsPoolRegistered(int poolId);
        
        /// <summary>
        /// Checks if a pool is valid (registered and not disposed)
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsPoolValid(int poolId);
        
        /// <summary>
        /// Checks if a pool is disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if disposed, false otherwise</returns>
        bool IsPoolDisposed(int poolId);
        
        /// <summary>
        /// Gets the type ID of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Type ID of the pool, or -1 if not found</returns>
        int GetPoolTypeId(int poolId);
        
        /// <summary>
        /// Gets the number of active items in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of active items</returns>
        int GetActiveCount(int poolId);
        
        /// <summary>
        /// Gets the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Capacity of the pool</returns>
        int GetCapacity(int poolId);
        
        /// <summary>
        /// Creates a handle to a pool that can be passed to burst-compiled jobs
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Handle to the pool</returns>
        NativePoolHandle CreateHandle(int poolId);
        
        /// <summary>
        /// Creates a read-only handle to a pool that can be passed to burst-compiled jobs
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Read-only handle to the pool</returns>
        NativePoolReadHandle CreateReadOnlyHandle(int poolId);
        
        /// <summary>
        /// Checks if an index is active in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        bool IsIndexActive(int poolId, int index);
        // <summary>
        /// Gets the safety handle for a specific pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>AtomicSafetyHandle for the pool</returns>
        unsafe AtomicSafetyHandle GetSafetyHandle(int poolId);
        
        /// <summary>
        /// Thread-safe version of TryAcquireIndex for parallel writers
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Index acquired, or -1 if full</returns>
        int AcquireIndexThreadSafe(int poolId);
        
        /// <summary>
        /// Thread-safe version of TryReleaseIndex for parallel writers
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        void ReleaseIndexThreadSafe(int poolId, int index);
        
        /// <summary>
        /// Sets a value at a specific index in a thread-safe manner
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to set</param>
        /// <param name="value">Value to set</param>
        void SetValueThreadSafe<T>(int poolId, int index, T value) where T : unmanaged;
        
        /// <summary>
        /// Gets a value from a specific index in a thread-safe manner
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to get</param>
        /// <returns>Value at the specified index</returns>
        T GetValueThreadSafe<T>(int poolId, int index) where T : unmanaged;
        
        /// <summary>
        /// Checks if an index is active in a thread-safe manner
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        bool IsIndexActiveThreadSafe(int poolId, int index);
    }
}