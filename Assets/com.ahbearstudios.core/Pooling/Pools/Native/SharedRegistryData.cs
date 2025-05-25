using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// Shared data structure for registry operations in jobs.
    /// Uses Unity Collections v2 for high-performance integration with burst-compiled jobs.
    /// </summary>
    [BurstCompile]
    public struct SharedRegistryData : IDisposable
    {
        // Maximum number of pools in the registry
        private readonly int _maxPools;
        
        // Array of pool validity flags
        [NativeDisableParallelForRestriction]
        private UnsafeList<bool> _validPools;
        
        // Array of pool dispose flags
        [NativeDisableParallelForRestriction]
        private UnsafeList<bool> _disposedPools;
        
        // Array of pool capacities
        [NativeDisableParallelForRestriction]
        private UnsafeList<int> _poolCapacities;
        
        // Array of pool active counts
        [NativeDisableParallelForRestriction]
        private UnsafeList<int> _poolActiveCounts;
        
        // BitArray to track indices within pools
        [NativeDisableParallelForRestriction]
        private UnsafeList<UnsafeBitArray> _poolActiveIndices;
        
        // Allocator used for native arrays
        private readonly Allocator _allocator;
        
        /// <summary>
        /// Whether this registry data is created and valid
        /// </summary>
        public bool IsCreated => _validPools.IsCreated && !_disposedPools.IsCreated;
        
        /// <summary>
        /// Creates a new shared registry data
        /// </summary>
        /// <param name="maxPools">Maximum number of pools</param>
        /// <param name="allocator">Allocator to use</param>
        public SharedRegistryData(int maxPools, Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator", nameof(allocator));
                
            _maxPools = maxPools;
            _allocator = allocator;
            
            _validPools = new UnsafeList<bool>(maxPools, allocator);
            _validPools.Resize(maxPools, NativeArrayOptions.ClearMemory);
            
            _disposedPools = new UnsafeList<bool>(maxPools, allocator);
            _disposedPools.Resize(maxPools, NativeArrayOptions.ClearMemory);
            
            _poolCapacities = new UnsafeList<int>(maxPools, allocator);
            _poolCapacities.Resize(maxPools, NativeArrayOptions.ClearMemory);
            
            _poolActiveCounts = new UnsafeList<int>(maxPools, allocator);
            _poolActiveCounts.Resize(maxPools, NativeArrayOptions.ClearMemory);
            
            _poolActiveIndices = new UnsafeList<UnsafeBitArray>(maxPools, allocator);
            _poolActiveIndices.Resize(maxPools);
            
            // Initialize empty BitArrays for each pool
            for (int i = 0; i < maxPools; i++)
            {
                _poolActiveIndices[i] = new UnsafeBitArray(64, allocator, NativeArrayOptions.ClearMemory);
            }
        }
        
        /// <summary>
        /// Registers a pool with the registry
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        [BurstCompile]
        public void RegisterPool(int poolId)
        {
            if (poolId < _maxPools)
            {
                _validPools[poolId] = true;
                _disposedPools[poolId] = false;
            }
        }
        
        /// <summary>
        /// Unregisters a pool from the registry
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        [BurstCompile]
        public void UnregisterPool(int poolId)
        {
            if (poolId < _maxPools)
            {
                _validPools[poolId] = false;
                
                // Clear bit array for reuse
                if (_poolActiveIndices[poolId].IsCreated)
                {
                    _poolActiveIndices[poolId].Clear();
                }
                
                _poolActiveCounts[poolId] = 0;
                _poolCapacities[poolId] = 0;
            }
        }
        
        /// <summary>
        /// Updates the capacity of a pool and resizes its bit array
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="capacity">New capacity</param>
        [BurstCompile]
        public void UpdateCapacity(int poolId, int capacity)
        {
            if (poolId < _maxPools)
            {
                int oldCapacity = _poolCapacities[poolId];
                _poolCapacities[poolId] = capacity;
                
                // Resize bit array if needed
                if (_poolActiveIndices[poolId].IsCreated)
                {
                    if (capacity > oldCapacity)
                    {
                        // We need a new bit array with larger capacity
                        var oldBitArray = _poolActiveIndices[poolId];
                        var newBitArray = new UnsafeBitArray(capacity, _allocator, NativeArrayOptions.ClearMemory);
                        
                        // Copy existing bits
                        for (int i = 0; i < oldCapacity && i < capacity; i++)
                        {
                            if (oldBitArray.IsSet(i))
                            {
                                newBitArray.Set(i, true);
                            }
                        }
                        
                        oldBitArray.Dispose();
                        _poolActiveIndices[poolId] = newBitArray;
                    }
                }
                else
                {
                    _poolActiveIndices[poolId] = new UnsafeBitArray(capacity, _allocator, NativeArrayOptions.ClearMemory);
                }
            }
        }
        
        /// <summary>
        /// Updates the active count of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="activeCount">New active count</param>
        [BurstCompile]
        public void UpdateActiveCount(int poolId, int activeCount)
        {
            if (poolId < _maxPools && _validPools[poolId])
            {
                _poolActiveCounts[poolId] = activeCount;
            }
        }
        
        /// <summary>
        /// Sets the active state of a specific index in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index in the pool</param>
        /// <param name="active">Whether the index is active</param>
        [BurstCompile]
        public void SetIndexActive(int poolId, int index, bool active)
        {
            if (poolId < _maxPools && _validPools[poolId] && index < _poolCapacities[poolId])
            {
                var bitArray = _poolActiveIndices[poolId];
                if (bitArray.IsCreated)
                {
                    bitArray.Set(index, active);
                    
                    // Update active count accordingly
                    if (active)
                    {
                        _poolActiveCounts[poolId] = _poolActiveCounts[poolId] + 1;
                    }
                    else if (_poolActiveCounts[poolId] > 0)
                    {
                        _poolActiveCounts[poolId] = _poolActiveCounts[poolId] - 1;
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets the disposed state of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="disposed">Whether the pool is disposed</param>
        [BurstCompile]
        public void SetPoolDisposed(int poolId, bool disposed)
        {
            if (poolId < _maxPools)
            {
                _disposedPools[poolId] = disposed;
                if (disposed)
                {
                    _validPools[poolId] = false;
                }
            }
        }
        
        /// <summary>
        /// Gets active indices for a specific pool without allocating new memory.
        /// This writes to a pre-allocated array and returns the count of active indices.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">Pre-allocated array to store the active indices</param>
        /// <returns>Number of active indices written to the array</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(int poolId, NativeArray<int> indices)
        {
            if (!IsPoolValid(poolId) || !indices.IsCreated)
            {
                return 0;
            }
    
            int capacity = GetCapacity(poolId);
            var bitArray = _poolActiveIndices[poolId];
            
            if (!bitArray.IsCreated || capacity == 0)
            {
                return 0;
            }
            
            int count = 0;
            int maxCount = Math.Min(indices.Length, GetActiveCount(poolId));
            
            for (int i = 0; i < capacity && count < maxCount; i++)
            {
                if (bitArray.IsSet(i))
                {
                    indices[count++] = i;
                }
            }
    
            return count;
        }
        
        /// <summary>
        /// Gets active indices for a specific pool using UnsafeList
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">UnsafeList to store the active indices</param>
        /// <returns>Number of active indices written to the list</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(int poolId, ref UnsafeList<int> indices)
        {
            if (!IsPoolValid(poolId) || !indices.IsCreated)
            {
                return 0;
            }
            
            indices.Clear();
            
            int capacity = GetCapacity(poolId);
            var bitArray = _poolActiveIndices[poolId];
            
            if (!bitArray.IsCreated || capacity == 0)
            {
                return 0;
            }
            
            for (int i = 0; i < capacity; i++)
            {
                if (bitArray.IsSet(i))
                {
                    indices.Add(i);
                }
            }
            
            return indices.Length;
        }
        
        /// <summary>
        /// Checks if a pool is valid (registered and not disposed)
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if valid, false otherwise</returns>
        [BurstCompile]
        public bool IsPoolValid(int poolId)
        {
            return poolId >= 0 && poolId < _maxPools && _validPools[poolId] && !_disposedPools[poolId];
        }
        
        /// <summary>
        /// Gets the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Pool capacity</returns>
        [BurstCompile]
        public int GetCapacity(int poolId)
        {
            if (poolId >= 0 && poolId < _maxPools)
            {
                return _poolCapacities[poolId];
            }
            return 0;
        }
        
        /// <summary>
        /// Gets the number of active items in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of active items</returns>
        [BurstCompile]
        public int GetActiveCount(int poolId)
        {
            if (poolId >= 0 && poolId < _maxPools)
            {
                return _poolActiveCounts[poolId];
            }
            return 0;
        }
        
        /// <summary>
        /// Checks if an index is active in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        [BurstCompile]
        public bool IsIndexActive(int poolId, int index)
        {
            if (!IsPoolValid(poolId))
            {
                return false;
            }
            
            var bitArray = _poolActiveIndices[poolId];
            if (!bitArray.IsCreated || index < 0 || index >= GetCapacity(poolId))
            {
                return false;
            }
            
            return bitArray.IsSet(index);
        }
        
        /// <summary>
        /// Tries to acquire an index from a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Acquired index, or -1 if full</returns>
        [BurstCompile]
        public int TryAcquireIndex(int poolId)
        {
            if (!IsPoolValid(poolId))
                return -1;
                
            int capacity = GetCapacity(poolId);
            int activeCount = GetActiveCount(poolId);
            
            if (activeCount >= capacity)
                return -1;
                
            var bitArray = _poolActiveIndices[poolId];
            if (!bitArray.IsCreated)
                return -1;
                
            // Find first available bit
            for (int i = 0; i < capacity; i++)
            {
                if (!bitArray.IsSet(i))
                {
                    // Mark as active
                    bitArray.Set(i, true);
                    _poolActiveCounts[poolId] = activeCount + 1;
                    return i;
                }
            }
            
            return -1; // No available indices
        }
        
        /// <summary>
        /// Tries to release an index back to a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        /// <returns>True if successful, false otherwise</returns>
        [BurstCompile]
        public bool TryReleaseIndex(int poolId, int index)
        {
            if (!IsPoolValid(poolId))
                return false;
                
            int capacity = GetCapacity(poolId);
            
            if (index < 0 || index >= capacity)
                return false;
                
            var bitArray = _poolActiveIndices[poolId];
            if (!bitArray.IsCreated)
                return false;
                
            // Check if index is active
            if (bitArray.IsSet(index))
            {
                // Mark as inactive
                bitArray.Set(index, false);
                int activeCount = GetActiveCount(poolId);
                if (activeCount > 0)
                {
                    _poolActiveCounts[poolId] = activeCount - 1;
                }
                return true;
            }
            
            return false; // Index wasn't active
        }
        
        /// <summary>
        /// Disposes resources used by this registry
        /// </summary>
        public void Dispose()
        {
            if (_validPools.IsCreated)
            {
                _validPools.Dispose();
            }
            
            if (_disposedPools.IsCreated)
            {
                _disposedPools.Dispose();
            }
            
            if (_poolCapacities.IsCreated)
            {
                _poolCapacities.Dispose();
            }
            
            if (_poolActiveCounts.IsCreated)
            {
                _poolActiveCounts.Dispose();
            }
            
            if (_poolActiveIndices.IsCreated)
            {
                // Dispose each bit array
                for (int i = 0; i < _poolActiveIndices.Length; i++)
                {
                    if (_poolActiveIndices[i].IsCreated)
                    {
                        _poolActiveIndices[i].Dispose();
                    }
                }
                _poolActiveIndices.Dispose();
            }
        }
    }
}