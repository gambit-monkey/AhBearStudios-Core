using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Registry data structure for native pools that is compatible with Burst compilation.
    /// Provides safe access to pool data in Burst-compiled jobs.
    /// Uses Unity Collections v2 for high-performance operations.
    /// </summary>
    [BurstCompile]
    public struct NativePoolRegistryData : IDisposable
    {
        // Constants
        private const int MAX_POOLS = 256;

        // Pool status flags
        private const byte UNREGISTERED = 0;
        private const byte REGISTERED = 1;
        private const byte DISPOSED = 2;

        // Internal storage
        private UnsafeList<byte> _poolStatus;
        private UnsafeList<int> _poolCapacities;
        private UnsafeList<int> _poolActiveCount;
        private UnsafeList<UnsafeBitArray> _poolActiveIndices;
        // Safety handles for thread-safe parallel access
        private UnsafeList<AtomicSafetyHandle> _poolSafetyHandles;
        // Generic value storage for thread-safe operations
        private UnsafeList<IntPtr> _poolValueArrayPtrs;
        private UnsafeList<int> _poolValueElementSizes;

        // Whether this registry is created
        private bool _isCreated;
        private Allocator _allocator;

        /// <summary>
        /// Gets whether this registry is created
        /// </summary>
        public bool IsCreated => _isCreated && _poolStatus.IsCreated;

        /// <summary>
        /// Creates a new Burst-compatible native pool registry data
        /// </summary>
        /// <param name="maxPools">Maximum number of pools to support (defaults to 256)</param>
        /// <param name="allocator">Allocator to use for internal storage</param>
        public NativePoolRegistryData(int maxPools = MAX_POOLS, Allocator allocator = Allocator.Persistent)
        {
            if (allocator == Allocator.Temp)
                throw new ArgumentException(
                    "Allocator.Temp is not supported for pool registries as they are expected to have longer lifetimes",
                    nameof(allocator));

            if (maxPools <= 0 || maxPools > MAX_POOLS)
                throw new ArgumentOutOfRangeException(nameof(maxPools), $"maxPools must be between 1 and {MAX_POOLS}");

            _allocator = allocator;
            _poolStatus = new UnsafeList<byte>(maxPools, allocator);
            _poolStatus.Resize(maxPools, NativeArrayOptions.ClearMemory);

            _poolCapacities = new UnsafeList<int>(maxPools, allocator);
            _poolCapacities.Resize(maxPools, NativeArrayOptions.ClearMemory);

            _poolActiveCount = new UnsafeList<int>(maxPools, allocator);
            _poolActiveCount.Resize(maxPools, NativeArrayOptions.ClearMemory);

            _poolActiveIndices = new UnsafeList<UnsafeBitArray>(maxPools, allocator);
            _poolActiveIndices.Resize(maxPools);
            
            _poolSafetyHandles = new UnsafeList<AtomicSafetyHandle>(maxPools, allocator);
            _poolSafetyHandles.Resize(maxPools);

            _poolValueArrayPtrs = new UnsafeList<IntPtr>(maxPools, allocator);
            _poolValueArrayPtrs.Resize(maxPools, NativeArrayOptions.ClearMemory);

            _poolValueElementSizes = new UnsafeList<int>(maxPools, allocator);
            _poolValueElementSizes.Resize(maxPools, NativeArrayOptions.ClearMemory);

            // Initialize all pools as unregistered
            for (int i = 0; i < maxPools; i++)
            {
                _poolStatus[i] = UNREGISTERED;
                _poolCapacities[i] = 0;
                _poolActiveCount[i] = 0;
                _poolActiveIndices[i] = new UnsafeBitArray(64, allocator, NativeArrayOptions.ClearMemory);
            }

            _isCreated = true;
        }

        /// <summary>
        /// Registers a pool with the registry
        /// </summary>
        /// <param name="poolId">ID of the pool to register</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        [BurstCompile]
        public void RegisterPool(int poolId, int initialCapacity = 0)
        {
            CheckCreated();
            CheckPoolId(poolId);

            _poolStatus[poolId] = REGISTERED;
            _poolCapacities[poolId] = initialCapacity;
            _poolActiveCount[poolId] = 0;

            // Initialize or resize bit array if needed
            if (_poolActiveIndices[poolId].IsCreated)
            {
                if (_poolActiveIndices[poolId].Length < initialCapacity)
                {
                    _poolActiveIndices[poolId].Dispose();
                    _poolActiveIndices[poolId] = new UnsafeBitArray(Math.Max(64, initialCapacity), _allocator,
                        NativeArrayOptions.ClearMemory);
                }
                else
                {
                    _poolActiveIndices[poolId].Clear();
                }
            }
            else
            {
                _poolActiveIndices[poolId] = new UnsafeBitArray(Math.Max(64, initialCapacity), _allocator,
                    NativeArrayOptions.ClearMemory);
            }
        }

        /// <summary>
        /// Unregisters a pool from the registry
        /// </summary>
        /// <param name="poolId">ID of the pool to unregister</param>
        [BurstCompile]
        public void UnregisterPool(int poolId)
        {
            CheckCreated();
            CheckPoolId(poolId);

            _poolStatus[poolId] = UNREGISTERED;
            _poolCapacities[poolId] = 0;
            _poolActiveCount[poolId] = 0;

            // Clean up bit array
            if (_poolActiveIndices[poolId].IsCreated)
            {
                _poolActiveIndices[poolId].Dispose();
            }
        }

        /// <summary>
        /// Sets a pool as disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="isDisposed">Whether the pool is disposed</param>
        [BurstCompile]
        public void SetPoolDisposed(int poolId, bool isDisposed)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (isDisposed)
            {
                _poolStatus[poolId] = DISPOSED;
            }
            else if (_poolStatus[poolId] == DISPOSED)
            {
                _poolStatus[poolId] = REGISTERED;
            }
        }
        
        /// <summary>
/// Gets the safety handle for a specific pool
/// </summary>
/// <param name="poolId">ID of the pool</param>
/// <returns>AtomicSafetyHandle for the pool</returns>
[BurstCompile]
public AtomicSafetyHandle GetPoolSafetyHandle(int poolId)
{
    CheckCreated();
    CheckPoolId(poolId);

    return _poolSafetyHandles[poolId];
}

/// <summary>
/// Sets a value at a specific index in a thread-safe manner
/// </summary>
/// <typeparam name="T">Type of value</typeparam>
/// <param name="poolId">ID of the pool</param>
/// <param name="index">Index to set</param>
/// <param name="value">Value to set</param>
[BurstCompile]
public unsafe void SetValueThreadSafe<T>(int poolId, int index, T value) where T : unmanaged
{
    if (!IsPoolValid(poolId))
        return;

    int capacity = GetCapacity(poolId);

    if (index < 0 || index >= capacity)
        return;

    // Initialize value storage if needed
    if (_poolValueArrayPtrs[poolId] == IntPtr.Zero)
    {
        int elementSize = UnsafeUtility.SizeOf<T>();
        void* valueArrayPtr = UnsafeUtility.Malloc(capacity * elementSize, UnsafeUtility.AlignOf<T>(), _allocator);
        _poolValueArrayPtrs[poolId] = (IntPtr)valueArrayPtr;
        _poolValueElementSizes[poolId] = elementSize;
    }

    // Check if the element size matches
    if (_poolValueElementSizes[poolId] != UnsafeUtility.SizeOf<T>())
        return;

    // Get a pointer to our value array and set the value
    void* existingArrayPtr = (void*)_poolValueArrayPtrs[poolId];
    T* typedArray = (T*)existingArrayPtr;
    
    // Thread-safety is handled by checking if the index is active first
    if (IsIndexActiveThreadSafe(poolId, index))
    {
        typedArray[index] = value;
    }
}

/// <summary>
/// Gets a value from a specific index in a thread-safe manner
/// </summary>
/// <typeparam name="T">Type of value</typeparam>
/// <param name="poolId">ID of the pool</param>
/// <param name="index">Index to get</param>
/// <returns>Value at the specified index</returns>
[BurstCompile]
public unsafe T GetValueThreadSafe<T>(int poolId, int index) where T : unmanaged
{
    if (!IsPoolValid(poolId))
        return default;

    int capacity = GetCapacity(poolId);

    if (index < 0 || index >= capacity)
        return default;

    // If no value array exists or element size doesn't match, return default
    if (_poolValueArrayPtrs[poolId] == IntPtr.Zero || _poolValueElementSizes[poolId] != UnsafeUtility.SizeOf<T>())
        return default;

    // Get a pointer to our value array and get the value
    void* valueArrayPtr = (void*)_poolValueArrayPtrs[poolId];
    T* typedArray = (T*)valueArrayPtr;
    
    // Thread-safety is handled by checking if the index is active first
    if (IsIndexActiveThreadSafe(poolId, index))
    {
        return typedArray[index];
    }
    
    return default;
}

        /// <summary>
        /// Updates the active count of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="count">New active count</param>
        [BurstCompile]
        public void UpdateActiveCount(int poolId, int count)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (_poolStatus[poolId] != REGISTERED)
                return;

            _poolActiveCount[poolId] = count;
        }

        /// <summary>
        /// Updates the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="capacity">New capacity</param>
        [BurstCompile]
        public void UpdateCapacity(int poolId, int capacity)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (_poolStatus[poolId] != REGISTERED)
                return;

            int oldCapacity = _poolCapacities[poolId];
            _poolCapacities[poolId] = capacity;

            // Resize bit array if needed
            if (capacity > oldCapacity && _poolActiveIndices[poolId].IsCreated)
            {
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

        /// <summary>
        /// Sets a specific index as active or inactive
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to modify</param>
        /// <param name="active">True to set active, false to set inactive</param>
        [BurstCompile]
        public void SetIndexActive(int poolId, int index, bool active)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (_poolStatus[poolId] != REGISTERED || !_poolActiveIndices[poolId].IsCreated)
                return;

            if (index < 0 || index >= _poolCapacities[poolId])
                return;

            bool wasActive = _poolActiveIndices[poolId].IsSet(index);
            _poolActiveIndices[poolId].Set(index, active);

            // Update active count
            if (active && !wasActive)
            {
                _poolActiveCount[poolId]++;
            }
            else if (!active && wasActive)
            {
                _poolActiveCount[poolId]--;
            }
        }

        /// <summary>
        /// Acquires an index in a thread-safe manner for parallel writer access
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Acquired index, or -1 if full</returns>
        [BurstCompile]
        public int AcquireIndexThreadSafe(int poolId)
        {
            if (!IsPoolValid(poolId))
                return -1;

            int capacity = GetCapacity(poolId);

            if (!_poolActiveIndices[poolId].IsCreated)
                return -1;

            // Atomically increment active count first
            int originalActiveCount = System.Threading.Interlocked.Increment(ref _poolActiveCount.ElementAt(poolId));

            // If we've exceeded capacity, undo the increment and return -1
            if (originalActiveCount > capacity)
            {
                System.Threading.Interlocked.Decrement(ref _poolActiveCount.ElementAt(poolId));
                return -1;
            }

            // Find an available index using atomic operations
            for (int i = 0; i < capacity; i++)
            {
                // Try to atomically set the bit from 0 to 1
                if (_poolActiveIndices[poolId].TestAndSet(i) == false)
                {
                    // We successfully acquired this index
                    return i;
                }
            }

            // If we reached here, we couldn't find an available index
            // Undo the active count increment and return -1
            System.Threading.Interlocked.Decrement(ref _poolActiveCount.ElementAt(poolId));
            return -1;
        }

        /// <summary>
        /// Releases an index in a thread-safe manner for parallel writer access
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        [BurstCompile]
        public void ReleaseIndexThreadSafe(int poolId, int index)
        {
            if (!IsPoolValid(poolId))
                return;

            int capacity = GetCapacity(poolId);

            if (index < 0 || index >= capacity)
                return;

            if (!_poolActiveIndices[poolId].IsCreated)
                return;

            // Only release if it was active
            if (_poolActiveIndices[poolId].TestAndClear(index))
            {
                // Atomically decrement the active count
                System.Threading.Interlocked.Decrement(ref _poolActiveCount.ElementAt(poolId));
            }
        }

        /// <summary>
        /// Checks if an index is active in a thread-safe manner
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        [BurstCompile]
        public bool IsIndexActiveThreadSafe(int poolId, int index)
        {
            if (!IsPoolValid(poolId))
                return false;

            int capacity = GetCapacity(poolId);

            if (index < 0 || index >= capacity)
                return false;

            if (!_poolActiveIndices[poolId].IsCreated)
                return false;

            // Read the bit in a thread-safe manner
            return _poolActiveIndices[poolId].IsSet(index);
        }

        /// <summary>
        /// Gets active indices for a specific pool using a direct pointer
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indicesPtr">Pointer to where indices should be written</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of indices written</returns>
        [BurstCompile]
        public unsafe int GetActiveIndicesPtr(int poolId, int* indicesPtr, int maxLength)
        {
            if (!IsPoolValid(poolId) || indicesPtr == null || maxLength <= 0)
                return 0;

            int capacity = GetCapacity(poolId);
            int count = 0;
            int maxCount = Math.Min(maxLength, GetActiveCount(poolId));

            if (!_poolActiveIndices[poolId].IsCreated)
                return 0;

            for (int i = 0; i < capacity && count < maxCount; i++)
            {
                if (_poolActiveIndices[poolId].IsSet(i))
                {
                    indicesPtr[count++] = i;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a pool is registered
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool is registered, false otherwise</returns>
        [BurstCompile]
        public bool IsPoolRegistered(int poolId)
        {
            if (!_isCreated || !_poolStatus.IsCreated || poolId < 0 || poolId >= _poolStatus.Length)
                return false;

            return _poolStatus[poolId] == REGISTERED;
        }

        /// <summary>
        /// Checks if a pool is valid (registered and not disposed)
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool is valid, false otherwise</returns>
        [BurstCompile]
        public bool IsPoolValid(int poolId)
        {
            if (!_isCreated || !_poolStatus.IsCreated || poolId < 0 || poolId >= _poolStatus.Length)
                return false;

            return _poolStatus[poolId] == REGISTERED;
        }

        /// <summary>
        /// Checks if a pool is disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if the pool is disposed, false otherwise</returns>
        [BurstCompile]
        public bool IsPoolDisposed(int poolId)
        {
            if (!_isCreated || !_poolStatus.IsCreated || poolId < 0 || poolId >= _poolStatus.Length)
                return true;

            return _poolStatus[poolId] == DISPOSED;
        }

        /// <summary>
        /// Gets the number of active items in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of active items</returns>
        [BurstCompile]
        public int GetActiveCount(int poolId)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (_poolStatus[poolId] != REGISTERED)
                return 0;

            return _poolActiveCount[poolId];
        }

        /// <summary>
        /// Gets the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Capacity of the pool</returns>
        [BurstCompile]
        public int GetCapacity(int poolId)
        {
            CheckCreated();
            CheckPoolId(poolId);

            if (_poolStatus[poolId] != REGISTERED)
                return 0;

            return _poolCapacities[poolId];
        }

        /// <summary>
        /// Checks if a specific index is active
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        [BurstCompile]
        public bool IsIndexActive(int poolId, int index)
        {
            if (!IsPoolValid(poolId) || !_poolActiveIndices[poolId].IsCreated)
                return false;

            if (index < 0 || index >= _poolCapacities[poolId])
                return false;

            return _poolActiveIndices[poolId].IsSet(index);
        }

        /// <summary>
        /// Gets active indices for a specific pool using Unity Collections v2.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">UnsafeList to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(int poolId, ref UnsafeList<int> indices)
        {
            if (!IsPoolValid(poolId) || !indices.IsCreated)
                return 0;

            indices.Clear();
            int capacity = _poolCapacities[poolId];

            if (!_poolActiveIndices[poolId].IsCreated)
                return 0;

            for (int i = 0; i < capacity; i++)
            {
                if (_poolActiveIndices[poolId].IsSet(i))
                {
                    indices.Add(i);
                }
            }

            return indices.Length;
        }

        /// <summary>
        /// Gets active indices for a specific pool using NativeArray.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">NativeArray to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(int poolId, NativeArray<int> indices)
        {
            if (!IsPoolValid(poolId) || !indices.IsCreated)
                return 0;

            int count = 0;
            int capacity = _poolCapacities[poolId];
            int maxIndices = Math.Min(indices.Length, _poolActiveCount[poolId]);

            if (!_poolActiveIndices[poolId].IsCreated)
                return 0;

            for (int i = 0; i < capacity && count < maxIndices; i++)
            {
                if (_poolActiveIndices[poolId].IsSet(i))
                {
                    indices[count++] = i;
                }
            }

            return count;
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
            if (_poolActiveCount[poolId] >= capacity)
                return -1;

            if (!_poolActiveIndices[poolId].IsCreated)
                return -1;

            // Find first available bit
            for (int i = 0; i < capacity; i++)
            {
                if (!_poolActiveIndices[poolId].IsSet(i))
                {
                    // Mark as active
                    _poolActiveIndices[poolId].Set(i, true);
                    _poolActiveCount[poolId]++;
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

            if (!_poolActiveIndices[poolId].IsCreated)
                return false;

            // Check if index is active
            if (_poolActiveIndices[poolId].IsSet(index))
            {
                // Mark as inactive
                _poolActiveIndices[poolId].Set(index, false);
                if (_poolActiveCount[poolId] > 0)
                {
                    _poolActiveCount[poolId]--;
                }

                return true;
            }

            return false; // Index wasn't active
        }

        /// <summary>
        /// Checks if the registry is created
        /// </summary>
        [BurstCompile]
        private void CheckCreated()
        {
            if (!_isCreated || !_poolStatus.IsCreated)
                throw new ObjectDisposedException(nameof(NativePoolRegistryData));
        }

        /// <summary>
        /// Checks if a pool ID is valid
        /// </summary>
        /// <param name="poolId">ID to check</param>
        [BurstCompile]
        private void CheckPoolId(int poolId)
        {
            if (poolId < 0 || poolId >= _poolStatus.Length)
                throw new ArgumentOutOfRangeException(nameof(poolId),
                    $"Pool ID must be between 0 and {_poolStatus.Length - 1}");
        }

        /// <summary>
        /// Disposes resources used by this registry
        /// </summary>
        public unsafe void Dispose()
        {
            if (_poolSafetyHandles.IsCreated)
                _poolSafetyHandles.Dispose();
    
            if (_poolValueArrayPtrs.IsCreated)
            {
                // Free each allocated value array
                for (int i = 0; i < _poolValueArrayPtrs.Length; i++)
                {
                    if (_poolValueArrayPtrs[i] != IntPtr.Zero)
                    {
                        UnsafeUtility.Free((void*)_poolValueArrayPtrs[i], _allocator);
                    }
                }
                _poolValueArrayPtrs.Dispose();
            }

            if (_poolValueElementSizes.IsCreated)
                _poolValueElementSizes.Dispose();
            
            if (_isCreated)
            {
                if (_poolStatus.IsCreated)
                    _poolStatus.Dispose();

                if (_poolCapacities.IsCreated)
                    _poolCapacities.Dispose();

                if (_poolActiveCount.IsCreated)
                    _poolActiveCount.Dispose();

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

                _isCreated = false;
            }
        }
    }
}