using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Provides Burst-compatible access to native pools through the pool registry.
    /// This static utility class serves as a bridge between Burst-compiled code and the native pool system.
    /// </summary>
    [BurstCompile]
    public static class NativePoolAccessor
    {
        /// <summary>
        /// Gets a value from a pool by ID and index in a Burst-compatible way
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index in the pool</param>
        /// <returns>Value at the specified index, or default if index is invalid</returns>
        [BurstCompile]
        public static T GetPoolValue<T>(int poolId, int index) where T : unmanaged
        {
            // Create a handle and use it to access the value
            var handle = new NativePoolHandle(poolId);
            return handle.GetValue<T>(index);
        }

        /// <summary>
        /// Sets a value in a pool by ID and index in a Burst-compatible way
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index in the pool</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if value was set, false if index is invalid</returns>
        [BurstCompile]
        public static bool SetPoolValue<T>(int poolId, int index, T value) where T : unmanaged
        {
            // Create a handle and use it to set the value
            var handle = new NativePoolHandle(poolId);
            return handle.SetValue(index, value);
        }
        
        /// <summary>
        /// Acquires an index from a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Acquired index, or -1 if the pool is full or invalid</returns>
        [BurstCompile]
        public static int AcquireIndex(int poolId)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return -1;
                
            // Try to acquire an index
            return data.TryAcquireIndex(poolId);
        }
        
        /// <summary>
        /// Releases an index back to a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        /// <returns>True if the index was successfully released, false otherwise</returns>
        [BurstCompile]
        public static bool ReleaseIndex(int poolId, int index)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return false;
                
            // Try to release the index
            return data.TryReleaseIndex(poolId, index);
        }
        
        /// <summary>
        /// Checks if an index is active in a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        [BurstCompile]
        public static bool IsIndexActive(int poolId, int index)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return false;
                
            // Check if the index is active
            return data.IsIndexActive(poolId, index);
        }
        
        /// <summary>
        /// Gets the number of active items in a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of active items, or 0 if pool is invalid</returns>
        [BurstCompile]
        public static int GetActiveCount(int poolId)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return 0;
                
            // Get the active count
            return data.GetActiveCount(poolId);
        }
        
        /// <summary>
        /// Gets the capacity of a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Capacity of the pool, or 0 if pool is invalid</returns>
        [BurstCompile]
        public static int GetCapacity(int poolId)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return 0;
                
            // Get the capacity
            return data.GetCapacity(poolId);
        }
        
        /// <summary>
        /// Ensures a pool has at least the specified capacity
        /// Note: This method cannot be Burst-compiled as it requires managed pool access
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="capacity">Required capacity</param>
        /// <returns>True if the capacity was ensured, false if the pool is invalid</returns>
        public static bool EnsureCapacity<T>(int poolId, int capacity) where T : unmanaged
        {
            // Get the pool from the registry
            var pool = NativePoolRegistry.Instance.GetPool<T>(poolId);
            if (pool == null)
                return false;
                
            // Ensure capacity
            pool.EnsureCapacity(capacity);
            return true;
        }
        
        /// <summary>
        /// Clears all items from a pool
        /// Note: This method cannot be Burst-compiled as it requires managed pool access
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool was cleared, false if the pool is invalid</returns>
        public static bool Clear<T>(int poolId) where T : unmanaged
        {
            // Get the pool from the registry
            var pool = NativePoolRegistry.Instance.GetPool<T>(poolId);
            if (pool == null)
                return false;
                
            // Clear the pool
            pool.Clear();
            return true;
        }
        
        /// <summary>
        /// Checks if a pool is disposed in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool is disposed or invalid, false otherwise</returns>
        [BurstCompile]
        public static bool IsPoolDisposed(int poolId)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Check if the pool is valid
            if (!data.IsPoolRegistered(poolId))
                return true;
                
            // Check if the pool is disposed
            return data.IsPoolDisposed(poolId);
        }
        
        /// <summary>
        /// Gets a read-only handle to a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Read-only handle to the pool</returns>
        [BurstCompile]
        public static NativePoolReadHandle GetReadOnlyHandle(int poolId)
        {
            return new NativePoolReadHandle(poolId);
        }
        
        /// <summary>
        /// Gets a handle to a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Handle to the pool</returns>
        [BurstCompile]
        public static NativePoolHandle GetHandle(int poolId)
        {
            return new NativePoolHandle(poolId);
        }
        
        /// <summary>
        /// Gets active indices from a pool in a Burst-compatible way without allocating new memory
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">Pre-allocated UnsafeList to store indices</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public static int GetActiveIndices(int poolId, ref UnsafeList<int> indices)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId) || !indices.IsCreated)
                return 0;
                
            // Get active indices
            return data.GetActiveIndicesUnsafe(poolId, ref indices);
        }
        
        /// <summary>
        /// Gets active indices from a pool in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">Pre-allocated NativeArray to store indices</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public static int GetActiveIndices(int poolId, NativeArray<int> indices)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId) || !indices.IsCreated)
                return 0;
                
            // Get active indices
            return data.GetActiveIndicesUnsafe(poolId, indices);
        }
        
        /// <summary>
        /// Gets active indices from a pool in a Burst-compatible way writing directly to a pointer
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indicesPtr">Pointer to array where indices will be written</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public unsafe static int GetActiveIndicesPtr(int poolId, int* indicesPtr, int maxLength)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool and inputs
            if (!data.IsPoolValid(poolId) || indicesPtr == null || maxLength <= 0)
                return 0;
                
            // Get capacity of the pool
            int capacity = data.GetCapacity(poolId);
            int count = 0;
            
            // Add all active indices to the array
            for (int i = 0; i < capacity && count < maxLength; i++)
            {
                if (data.IsIndexActive(poolId, i))
                {
                    indicesPtr[count++] = i;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Creates a new UnsafeList containing all active indices from a pool
        /// Note: Caller is responsible for disposing the returned list
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="allocator">Allocator to use for the new list</param>
        /// <returns>UnsafeList containing all active indices, must be disposed by caller</returns>
        public static UnsafeList<int> CreateActiveIndicesList(int poolId, Allocator allocator)
        {
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate the pool
            if (!data.IsPoolValid(poolId))
                return new UnsafeList<int>(0, allocator);
                
            // Get the capacity and create a list with that initial capacity
            int capacity = data.GetCapacity(poolId);
            var indices = new UnsafeList<int>(capacity, allocator);
            
            // Fill the list
            GetActiveIndices(poolId, ref indices);
            
            return indices;
        }
        
        /// <summary>
        /// Checks if a pool is valid in a Burst-compatible way
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool is valid, false otherwise</returns>
        [BurstCompile]
        public static bool IsPoolValid(int poolId)
        {
            // Use registry data directly for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            return data.IsPoolValid(poolId);
        }
        
        /// <summary>
        /// Acquires a value from a pool, combining acquisition and value retrieval
        /// Note: This method cannot be Burst-compiled as it requires managed pool access
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="acquiredIndex">Output parameter that receives the acquired index</param>
        /// <returns>The acquired value</returns>
        public static T AcquireValue<T>(int poolId, out int acquiredIndex) where T : unmanaged
        {
            acquiredIndex = AcquireIndex(poolId);
            
            if (acquiredIndex < 0)
                return default;
                
            return GetPoolValue<T>(poolId, acquiredIndex);
        }
        
        /// <summary>
        /// Sets and releases a value back to a pool, combining update and release
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        /// <param name="value">Final value to set before releasing</param>
        /// <returns>True if successfully released, false otherwise</returns>
        [BurstCompile]
        public static bool SetAndReleaseValue<T>(int poolId, int index, T value) where T : unmanaged
        {
            // Set the value first
            SetPoolValue(poolId, index, value);
            
            // Then release the index
            return ReleaseIndex(poolId, index);
        }
        
        /// <summary>
        /// Gets the utilization ratio of a pool (active count / capacity)
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Utilization ratio between 0.0 and 1.0, or 0 if pool is invalid</returns>
        [BurstCompile]
        public static float GetUtilizationRatio(int poolId)
        {
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            if (!data.IsPoolValid(poolId))
                return 0f;
                
            int capacity = data.GetCapacity(poolId);
            if (capacity <= 0)
                return 0f;
                
            return (float)data.GetActiveCount(poolId) / capacity;
        }
    }
}