using System;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Jobs;
using AhBearStudios.Core.Pooling.Pools.Native;
using AhBearStudios.Core.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;

namespace AhBearStudios.Core.Pooling.Processing
{
    /// <summary>
    /// Processes data from native pools using the job system.
    /// Optimized for Unity Collections v2 and Burst compilation.
    /// </summary>
    /// <typeparam name="T">Type of data to process. Must be unmanaged.</typeparam>
    public sealed class PooledDataProcessor<T> : IDisposable where T : unmanaged
    {
        private readonly INativePoolRegistry _poolRegistry;
        private readonly IPoolProfiler _profiler;
        private readonly IPoolLogger _logger;
        private readonly NativePoolHandle _poolHandle;
        private readonly UnsafeList<int> _activeIndices;
        private readonly int _batchSize;
        private readonly ProfilerMarker _processingMarker;
        private bool _isDisposed;

        /// <summary>
        /// Gets the number of items currently being tracked by the processor
        /// </summary>
        public int ActiveCount => _activeIndices.IsCreated ? _activeIndices.Length : 0;

        /// <summary>
        /// Gets the pool handle associated with this processor
        /// </summary>
        public NativePoolHandle PoolHandle => _poolHandle;

        /// <summary>
        /// Gets whether the processor has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Initializes a new instance of the PooledDataProcessor class
        /// </summary>
        /// <param name="poolHandle">Handle to the pool containing data to process</param>
        /// <param name="batchSize">Batch size for parallel processing jobs. Default is 64.</param>
        /// <param name="service">Optional service locator for dependency injection</param>
        public PooledDataProcessor(NativePoolHandle poolHandle, int batchSize = 64, IPoolingService service = null)
        {
            if (!poolHandle.IsValid)
                throw new ArgumentException("Pool handle is not valid", nameof(poolHandle));

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero");

            // Get dependencies from service locator or use singletons
            var services = service ?? DefaultPoolingServices.Instance;
            _poolRegistry = services.GetService<INativePoolRegistry>() ?? NativePoolRegistry.Instance;
            _profiler = services.GetService<IPoolProfiler>();
            _logger = services.GetService<IPoolLogger>();

            _poolHandle = poolHandle;
            _batchSize = batchSize;
            _processingMarker = new ProfilerMarker($"PooledDataProcessor<{typeof(T).Name}>.Process");

            // Initialize active indices list
            _activeIndices = new UnsafeList<int>(_poolRegistry.GetCapacity(poolHandle.PoolId), Allocator.Persistent);
            RefreshActiveIndices();
        }

        /// <summary>
        /// Refreshes the internal list of active indices from the pool
        /// </summary>
        public void RefreshActiveIndices()
        {
            ThrowIfDisposed();

            if (!_poolHandle.IsValid)
                return;

            _activeIndices.Clear();

            // Use the ref UnsafeList<int> overload
            _poolRegistry.GetActiveIndices(_poolHandle.PoolId, ref _activeIndices);
        }
        
        /// <summary>
        /// Processes all active items in the pool using a provided job
        /// </summary>
        /// <param name="processItem">Function pointer to the processing function</param>
        /// <param name="deltaTime">Delta time for time-based processing</param>
        /// <returns>JobHandle that can be used to chain dependencies</returns>
        public JobHandle ProcessItems(FunctionPointer<PooledProcessingJob<T>.ProcessItemDelegate> processItem, float deltaTime = 0)
        {
            ThrowIfDisposed();

            using (_processingMarker.Auto())
            {
                RefreshActiveIndices();

                if (_activeIndices.Length == 0)
                    return default;

                var job = new PooledProcessingJob<T>
                {
                    Handle = _poolHandle,
                    ProcessItem = processItem,
                    DeltaTime = deltaTime
                };

                return job.Schedule(_activeIndices, _batchSize, default);
            }
        }

        /// <summary>
        /// Gets a read-only view of the active indices
        /// </summary>
        /// <param name="allocator">Allocator to use for the new array</param>
        /// <returns>NativeArray containing the active indices</returns>
        public NativeArray<int> GetActiveIndices(Allocator allocator)
        {
            ThrowIfDisposed();

            var result = new NativeArray<int>(_activeIndices.Length, allocator, NativeArrayOptions.UninitializedMemory);
    
            // Copy elements individually
            for (int i = 0; i < _activeIndices.Length; i++)
            {
                result[i] = _activeIndices[i];
            }
    
            return result;
        }

        /// <summary>
        /// Gets all data from active indices in the pool
        /// </summary>
        /// <param name="allocator">Allocator to use for the returned array</param>
        /// <returns>NativeArray containing a copy of all active data</returns>
        public NativeArray<T> GetActiveData(Allocator allocator)
        {
            ThrowIfDisposed();

            var result = new NativeArray<T>(_activeIndices.Length, allocator, NativeArrayOptions.UninitializedMemory);
            
            for (int i = 0; i < _activeIndices.Length; i++)
            {
                int index = _activeIndices[i];
                result[i] = _poolRegistry.GetValueThreadSafe<T>(_poolHandle.PoolId, index);
            }
            
            return result;
        }

        /// <summary>
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="index">Index of the item to release</param>
        /// <returns>True if the item was successfully released, false otherwise</returns>
        public bool ReleaseItem(int index)
        {
            ThrowIfDisposed();
            
            bool result = _poolRegistry.TryReleaseIndex(_poolHandle.PoolId, index);
            
            if (result)
            {
                RefreshActiveIndices();
            }
            
            return result;
        }

        /// <summary>
        /// Updates a value at the specified index in the pool
        /// </summary>
        /// <param name="index">Index to update</param>
        /// <param name="value">New value</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateValue(int index, T value)
        {
            ThrowIfDisposed();
            
            if (!_poolRegistry.IsIndexActive(_poolHandle.PoolId, index))
                return false;
                
            _poolRegistry.SetValueThreadSafe(_poolHandle.PoolId, index, value);
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PooledDataProcessor<T>));
        }

        /// <summary>
        /// Disposes resources used by the processor
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_activeIndices.IsCreated)
                {
                    _activeIndices.Dispose();
                }
                
                _isDisposed = true;
            }
        }
    }
}