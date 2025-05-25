using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// A read-only handle to a native pool that can be passed to burst-compiled jobs.
    /// Provides read-only access to the pool's data without requiring the full pool instance.
    /// Updated to use Unity Collections v2.
    /// </summary>
    [BurstCompile]
    public readonly struct NativePoolReadHandle
    {
        /// <summary>
        /// The ID of the pool in the registry
        /// </summary>
        [ReadOnly] public readonly int PoolId;
        
        /// <summary>
        /// Creates a new read-only pool handle
        /// </summary>
        /// <param name="poolId">ID of the pool in the registry</param>
        public NativePoolReadHandle(int poolId)
        {
            PoolId = poolId;
        }
        
        /// <summary>
        /// Checks if an index is active in the pool
        /// </summary>
        /// <param name="index">Index to check</param>
        /// <returns>True if active, false otherwise</returns>
        [BurstCompile]
        public bool IsActive(int index)
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return false;
                
            // Check if index is valid
            return data.IsIndexActive(PoolId, index);
        }
        
        /// <summary>
        /// Checks if an index is active in the pool.
        /// Alias for IsActive to maintain compatibility with INativePool interface.
        /// </summary>
        /// <param name="index">Index to check</param>
        /// <returns>True if active, false otherwise</returns>
        [BurstCompile]
        public bool IsIndexActive(int index)
        {
            return IsActive(index);
        }
        
        /// <summary>
        /// Gets active indices in the pool without allocating new memory.
        /// This writes to a pre-allocated array and returns the count of active indices.
        /// </summary>
        /// <param name="indices">Pre-allocated array to store the active indices</param>
        /// <returns>Number of active indices written to the array</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(NativeArray<int> indices)
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
    
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return 0;
    
            if (!indices.IsCreated)
                return 0;
    
            // Use registry to get all active indices (read-only access)
            return data.GetActiveIndicesUnsafe(PoolId, indices);
        }
        
        /// <summary>
        /// Gets active indices in the pool using Collections v2.
        /// </summary>
        /// <param name="indices">UnsafeList to populate with active indices</param>
        /// <returns>Number of active indices added to the list</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(ref UnsafeList<int> indices)
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
    
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return 0;
    
            if (!indices.IsCreated)
                return 0;
            
            indices.Clear();
            
            // Get capacity of the pool
            int capacity = data.GetCapacity(PoolId);
            
            // Add all active indices to the list
            for (int i = 0; i < capacity; i++)
            {
                if (data.IsIndexActive(PoolId, i))
                {
                    indices.Add(i);
                }
            }
            
            return indices.Length;
        }
        
        /// <summary>
        /// Gets active indices in the pool using a direct pointer.
        /// </summary>
        /// <param name="indicesPtr">Pointer to array to populate with active indices</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public unsafe int GetActiveIndicesUnsafePtr(int* indicesPtr, int maxLength)
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
    
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return 0;
    
            if (indicesPtr == null || maxLength <= 0)
                return 0;
    
            int count = 0;
            int capacity = data.GetCapacity(PoolId);
            
            // Add all active indices to the array
            for (int i = 0; i < capacity && count < maxLength; i++)
            {
                if (data.IsIndexActive(PoolId, i))
                {
                    indicesPtr[count++] = i;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Gets the capacity of the pool
        /// </summary>
        /// <returns>Pool capacity</returns>
        [BurstCompile]
        public int GetCapacity()
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return 0;
                
            // Get capacity
            return data.GetCapacity(PoolId);
        }
        
        /// <summary>
        /// Gets the number of active items in the pool
        /// </summary>
        /// <returns>Number of active items</returns>
        [BurstCompile]
        public int GetActiveCount()
        {
            // Get registry data as readonly reference for Burst compatibility
            ref readonly var data = ref NativePoolRegistry.RegistryData;
            
            // Validate pool ID
            if (!data.IsPoolValid(PoolId))
                return 0;
                
            // Get active count
            return data.GetActiveCount(PoolId);
        }
    }
}