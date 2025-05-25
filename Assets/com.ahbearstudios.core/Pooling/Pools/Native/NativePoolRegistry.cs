using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// Registry for native pools that is compatible with Burst compilation.
    /// This registry manages the lifecycle of native pools and provides access to them.
    /// </summary>
    [BurstCompile]
    public class NativePoolRegistry : INativePoolRegistry
    {
        // Singleton instance for global access
        private static NativePoolRegistry s_instance;

        // Static registry data for Burst compatibility
        private static NativePoolRegistryData s_registryData;

        /// <summary>
        /// Gets the global instance of the pool registry
        /// </summary>
        public static NativePoolRegistry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new NativePoolRegistry();
                }

                return s_instance;
            }
        }

        /// <summary>
        /// Gets a read-only reference to the shared registry data for Burst compatibility
        /// </summary>
        public static ref readonly NativePoolRegistryData RegistryData
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new NativePoolRegistry();
                }

                return ref s_registryData;
            }
        }

        // Shared registry data for job compatibility
        private SharedRegistryData _sharedData;

        // Dictionary mapping pool IDs to their type IDs
        private Dictionary<int, int> _poolTypeIds;

        // Dictionary mapping pool IDs to their actual pool instances
        private Dictionary<int, object> _poolInstances;

        // Dictionary mapping type hash codes to type IDs
        private Dictionary<int, int> _typeHashToTypeId;

        // Counter for generating unique type IDs
        private int _nextTypeId = 0;

        // Counter for generating unique pool IDs
        private int _nextPoolId = 0;

        // Whether this registry is created and valid
        private bool _isCreated;

        // Whether this registry is disposed
        private bool _isDisposed;

        // Allocator used for native collections
        private Allocator _allocator;

        /// <summary>
        /// Gets whether this registry is created and valid
        /// </summary>
        public bool IsCreated => _isCreated && !_isDisposed && s_registryData.IsCreated;

        /// <summary>
        /// Gets the maximum number of pools supported by this registry
        /// </summary>
        public int MaxPools { get; }

        /// <summary>
        /// Creates a new native pool registry
        /// </summary>
        /// <param name="maxPools">Maximum number of pools to support (defaults to 256)</param>
        /// <param name="allocator">Allocator to use for registry data (defaults to Persistent)</param>
        public NativePoolRegistry(int maxPools = 256, Allocator allocator = Allocator.Persistent)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator", nameof(allocator));

            if (maxPools <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPools), "Max pools must be greater than 0");

            _allocator = allocator;
            MaxPools = maxPools;

            // Initialize the static registry data if not already created
            if (!s_registryData.IsCreated)
            {
                s_registryData = new NativePoolRegistryData(maxPools, allocator);
            }

            _sharedData = new SharedRegistryData(maxPools, allocator);
            _poolTypeIds = new Dictionary<int, int>();
            _poolInstances = new Dictionary<int, object>();
            _typeHashToTypeId = new Dictionary<int, int>();
            _isCreated = true;
            _isDisposed = false;
        }

        /// <summary>
        /// Registers a pool with the registry
        /// </summary>
        /// <param name="typeId">Type ID of the pool</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>ID of the registered pool</returns>
        public int RegisterPool(int typeId, int initialCapacity = 0)
        {
            CheckDisposed();

            int poolId = _nextPoolId++;

            if (poolId >= MaxPools)
                throw new InvalidOperationException($"Maximum number of pools ({MaxPools}) reached");

            s_registryData.RegisterPool(poolId, initialCapacity);
            _sharedData.RegisterPool(poolId);
            _sharedData.UpdateCapacity(poolId, initialCapacity);
            _poolTypeIds[poolId] = typeId;

            return poolId;
        }

        /// <summary>
        /// Registers a typed pool instance with the registry
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">Pool instance to register</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>NativePoolHandle with the registered pool ID</returns>
        public NativePoolHandle Register<T>(INativePool<T> pool, int initialCapacity = 0) where T : unmanaged
        {
            CheckDisposed();

            // Get or create type ID for T
            int typeHash = typeof(T).GetHashCode();
            int typeId;

            if (!_typeHashToTypeId.TryGetValue(typeHash, out typeId))
            {
                typeId = _nextTypeId++;
                _typeHashToTypeId[typeHash] = typeId;
            }

            int poolId = RegisterPool(typeId, initialCapacity);
            _poolInstances[poolId] = pool;

            return new NativePoolHandle(poolId);
        }

        /// <summary>
        /// Unregisters a pool from the registry
        /// </summary>
        /// <param name="poolId">ID of the pool to unregister</param>
        public void Unregister(int poolId)
        {
            CheckDisposed();

            if (!IsPoolRegistered(poolId))
                return;

            s_registryData.UnregisterPool(poolId);
            _sharedData.UnregisterPool(poolId);
            _poolTypeIds.Remove(poolId);
            _poolInstances.Remove(poolId);
        }

        /// <summary>
        /// Gets the type ID for a specific type
        /// </summary>
        /// <typeparam name="T">Type to get ID for</typeparam>
        /// <returns>Type ID, or -1 if not found</returns>
        public int GetTypeId<T>() where T : unmanaged
        {
            CheckDisposed();

            int typeHash = typeof(T).GetHashCode();
            if (_typeHashToTypeId.TryGetValue(typeHash, out int typeId))
            {
                return typeId;
            }

            return -1;
        }

        /// <summary>
        /// Gets a typed pool by ID
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>The pool instance, or null if not found</returns>
        public INativePool<T> GetPool<T>(int poolId) where T : unmanaged
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return null;

            if (_poolInstances.TryGetValue(poolId, out object pool))
            {
                if (pool is INativePool<T> typedPool)
                {
                    return typedPool;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets a pool as disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="isDisposed">Whether the pool is disposed</param>
        public void SetPoolDisposed(int poolId, bool isDisposed)
        {
            CheckDisposed();

            if (!IsPoolRegistered(poolId))
                return;

            s_registryData.SetPoolDisposed(poolId, isDisposed);
            _sharedData.SetPoolDisposed(poolId, isDisposed);

            if (isDisposed)
            {
                _poolInstances.Remove(poolId);
            }
        }

        /// <summary>
        /// Updates the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="capacity">New capacity</param>
        public void UpdateCapacity(int poolId, int capacity)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return;

            s_registryData.UpdateCapacity(poolId, capacity);
            _sharedData.UpdateCapacity(poolId, capacity);
        }

        /// <summary>
        /// Updates the active count of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="count">New active count</param>
        public void UpdateActiveCount(int poolId, int count)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return;

            s_registryData.UpdateActiveCount(poolId, count);
            _sharedData.UpdateActiveCount(poolId, count);
        }

        /// <summary>
        /// Sets an index as active or inactive
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to set</param>
        /// <param name="active">Whether the index is active</param>
        public void SetIndexActive(int poolId, int index, bool active)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return;

            s_registryData.SetIndexActive(poolId, index, active);
            _sharedData.SetIndexActive(poolId, index, active);
        }

        /// <summary>
        /// Tries to acquire an index from a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Index acquired, or -1 if full</returns>
        public int TryAcquireIndex(int poolId)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return -1;

            return s_registryData.TryAcquireIndex(poolId);
        }

        /// <summary>
        /// Tries to release an index back to a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool TryReleaseIndex(int poolId, int index)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return false;

            return s_registryData.TryReleaseIndex(poolId, index);
        }

        /// <summary>
        /// Gets active indices for a specific pool using NativeArray
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">NativeArray to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        public int GetActiveIndices(int poolId, NativeArray<int> indices)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return 0;

            return s_registryData.GetActiveIndicesUnsafe(poolId, indices);
        }

        /// <summary>
        /// Gets active indices for a specific pool using UnsafeList
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indices">UnsafeList to populate with active indices</param>
        /// <returns>Number of active indices written</returns>
        public int GetActiveIndices(int poolId, ref UnsafeList<int> indices)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                return 0;

            return s_registryData.GetActiveIndicesUnsafe(poolId, ref indices);
        }

        /// <summary>
        /// Gets active indices for a specific pool writing directly to a pointer
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="indicesPtr">Pointer to array where indices will be written</param>
        /// <param name="maxLength">Maximum number of indices to write</param>
        /// <returns>Number of active indices written</returns>
        public unsafe int GetActiveIndicesPtr(int poolId, int* indicesPtr, int maxLength)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId) || indicesPtr == null || maxLength <= 0)
                return 0;

            int count = 0;
            int capacity = s_registryData.GetCapacity(poolId);

            for (int i = 0; i < capacity && count < maxLength; i++)
            {
                if (s_registryData.IsIndexActive(poolId, i))
                {
                    indicesPtr[count++] = i;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a pool is registered with the registry
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if registered, false otherwise</returns>
        public bool IsPoolRegistered(int poolId)
        {
            if (_isDisposed)
                return false;

            return s_registryData.IsPoolRegistered(poolId);
        }

        /// <summary>
        /// Checks if a pool is valid (registered and not disposed)
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsPoolValid(int poolId)
        {
            if (_isDisposed)
                return false;

            return s_registryData.IsPoolValid(poolId);
        }

        /// <summary>
        /// Checks if a pool is disposed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>True if disposed, false otherwise</returns>
        public bool IsPoolDisposed(int poolId)
        {
            if (_isDisposed)
                return true;

            return s_registryData.IsPoolDisposed(poolId);
        }

        /// <summary>
        /// Gets the safety handle for a specific pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>AtomicSafetyHandle for the pool</returns>
        public unsafe AtomicSafetyHandle GetSafetyHandle(int poolId)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                throw new ArgumentException($"Pool {poolId} is not valid");

            return s_registryData.GetPoolSafetyHandle(poolId);
        }

        /// <summary>
        /// Thread-safe version of TryAcquireIndex for parallel writers
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Index acquired, or -1 if full</returns>
        [BurstCompile]
        public int AcquireIndexThreadSafe(int poolId)
        {
            if (_isDisposed)
                return -1;

            return s_registryData.AcquireIndexThreadSafe(poolId);
        }

        /// <summary>
        /// Thread-safe version of TryReleaseIndex for parallel writers
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to release</param>
        [BurstCompile]
        public void ReleaseIndexThreadSafe(int poolId, int index)
        {
            if (_isDisposed)
                return;

            s_registryData.ReleaseIndexThreadSafe(poolId, index);
        }

        /// <summary>
        /// Sets a value at a specific index in a thread-safe manner
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to set</param>
        /// <param name="value">Value to set</param>
        [BurstCompile]
        public void SetValueThreadSafe<T>(int poolId, int index, T value) where T : unmanaged
        {
            if (_isDisposed)
                return;

            s_registryData.SetValueThreadSafe(poolId, index, value);
        }

        /// <summary>
        /// Gets a value from a specific index in a thread-safe manner
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to get</param>
        /// <returns>Value at the specified index</returns>
        [BurstCompile]
        public T GetValueThreadSafe<T>(int poolId, int index) where T : unmanaged
        {
            if (_isDisposed)
                return default;

            return s_registryData.GetValueThreadSafe<T>(poolId, index);
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
            if (_isDisposed)
                return false;

            return s_registryData.IsIndexActiveThreadSafe(poolId, index);
        }

        /// <summary>
        /// Gets the type ID of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Type ID of the pool, or -1 if not found</returns>
        public int GetPoolTypeId(int poolId)
        {
            CheckDisposed();

            if (_poolTypeIds.TryGetValue(poolId, out int typeId))
                return typeId;

            return -1;
        }

        /// <summary>
        /// Gets the number of active items in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of active items</returns>
        public int GetActiveCount(int poolId)
        {
            if (_isDisposed)
                return 0;

            return s_registryData.GetActiveCount(poolId);
        }

        /// <summary>
        /// Gets the capacity of a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Capacity of the pool</returns>
        public int GetCapacity(int poolId)
        {
            if (_isDisposed)
                return 0;

            return s_registryData.GetCapacity(poolId);
        }

        /// <summary>
        /// Gets a read-only reference to the shared registry data for job compatibility
        /// </summary>
        /// <returns>Read-only reference to shared registry data</returns>
        public ref readonly SharedRegistryData GetSharedData()
        {
            CheckDisposed();
            return ref _sharedData;
        }

        /// <summary>
        /// Creates a handle to a pool that can be passed to burst-compiled jobs
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Handle to the pool</returns>
        public NativePoolHandle CreateHandle(int poolId)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                throw new ArgumentException($"Pool {poolId} is not valid");

            return new NativePoolHandle(poolId);
        }

        /// <summary>
        /// Creates a read-only handle to a pool that can be passed to burst-compiled jobs
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Read-only handle to the pool</returns>
        public NativePoolReadHandle CreateReadOnlyHandle(int poolId)
        {
            CheckDisposed();

            if (!IsPoolValid(poolId))
                throw new ArgumentException($"Pool {poolId} is not valid");

            return new NativePoolReadHandle(poolId);
        }

        /// <summary>
        /// Checks if an index is active in a pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="index">Index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        public bool IsIndexActive(int poolId, int index)
        {
            if (_isDisposed)
                return false;

            return s_registryData.IsIndexActive(poolId, index);
        }

        /// <summary>
        /// Checks if the registry is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(NativePoolRegistry));
        }

        /// <summary>
        /// Disposes resources used by this registry
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_sharedData.IsCreated)
                    _sharedData.Dispose();

                _poolTypeIds.Clear();
                _poolTypeIds = null;

                _poolInstances.Clear();
                _poolInstances = null;

                _typeHashToTypeId.Clear();
                _typeHashToTypeId = null;

                // If we're the global instance, clear it
                if (s_instance == this)
                {
                    // Only dispose the static registry data when the singleton is disposed
                    if (s_registryData.IsCreated)
                    {
                        s_registryData.Dispose();
                    }

                    s_instance = null;
                }
            }
        }
    }
}