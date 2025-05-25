using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// Interface for all native pool implementations that use Unity Collections v2.
    /// This interface extends the base IPool interface with functionality specific to
    /// native memory management.
    /// </summary>
    public interface INativePool : IPool, IShrinkablePool
    {
        /// <summary>
        /// Gets the unique identifier for this pool as a Burst-compatible FixedString64Bytes
        /// </summary>
        FixedString64Bytes NativeId { get; }
        
        /// <summary>
        /// Gets the allocator used by this pool
        /// </summary>
        Allocator Allocator { get; }
        
        /// <summary>
        /// Gets the capacity of the pool
        /// </summary>
        int Capacity { get; }
        
        /// <summary>
        /// Releases an item at the specified index back to the pool
        /// </summary>
        /// <param name="index">Index of the item to release</param>
        void Release(int index);
        
        /// <summary>
        /// Checks if an index is active in the pool
        /// </summary>
        /// <param name="index">Index to check</param>
        /// <returns>True if the item at the specified index is active, false otherwise</returns>
        bool IsIndexActive(int index);
        
        /// <summary>
        /// Gets the number of active items in the pool
        /// </summary>
        /// <returns>Number of active items</returns>
        int GetActiveCount();
        
        /// <summary>
        /// Gets an UnsafeList of active indices in the pool.
        /// Uses Unity Collections v2 for better performance with Burst and Jobs.
        /// </summary>
        /// <param name="allocator">Allocator to use for the returned list</param>
        /// <returns>An UnsafeList containing all active indices</returns>
        UnsafeList<int> GetActiveIndices(Allocator allocator);
        
        /// <summary>
        /// Gets active indices in the pool without allocating new memory.
        /// This writes to a pre-allocated UnsafeList and returns the count of active indices.
        /// 
        /// Note: This method is designed for Burst-compatible code.
        /// </summary>
        /// <param name="indices">Pre-allocated UnsafeList to store the active indices</param>
        /// <returns>Number of active indices written to the list</returns>
        int GetActiveIndicesUnsafe(ref UnsafeList<int> indices);
        
        /// <summary>
        /// Gets active indices in the pool writing directly to a provided pointer.
        /// This method is designed to be Burst-compatible.
        /// </summary>
        /// <param name="indicesPtr">Pointer to an array where indices will be written</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of active indices written to the array</returns>
        unsafe int GetActiveIndicesUnsafePtr(int* indicesPtr, int maxLength);
    }
    
    /// <summary>
    /// Generic interface for native pools of specific item types using Unity Collections v2.
    /// </summary>
    /// <typeparam name="T">Type of items in the native pool</typeparam>
    public interface INativePool<T> : INativePool, IPool<T> where T : unmanaged
    {
        /// <summary>
        /// Gets the value at a specific index
        /// </summary>
        /// <param name="index">Index to retrieve</param>
        /// <returns>The value at the specified index</returns>
        T GetValue(int index);
        
        /// <summary>
        /// Sets the value at a specific index
        /// </summary>
        /// <param name="index">Index to set</param>
        /// <param name="value">Value to set</param>
        void SetValue(int index, T value);
        
        /// <summary>
        /// Acquires an index from the pool
        /// </summary>
        /// <returns>The acquired index, or -1 if the pool is full</returns>
        int AcquireIndex();
    }
}