using System.Collections.Concurrent;
using System.Threading;
using Unity.Profiling;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Pooling.Pools
{
    /// <summary>
    /// Generic object pool implementation for any IPooledObject type.
    /// Thread-safe with concurrent collections and performance monitoring.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    /// <typeparam name="T">Type that implements IPooledObject</typeparam>
    public sealed class GenericObjectPool<T> : IObjectPool<T>, IDisposable 
        where T : class, IPooledObject, new()
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly PoolConfiguration _configuration;
        private readonly IPoolingStrategy _strategy;
        private readonly PoolStatistics _statistics;
        private readonly Timer _maintenanceTimer;
        private readonly IMessageBusService _messageBusService;
        private readonly object _statsLock = new object();
        private readonly ProfilerMarker _getMarker;
        private readonly ProfilerMarker _returnMarker;
        private readonly ProfilerMarker _maintenanceMarker;
        
        private volatile bool _disposed = false;
        private int _totalCount = 0;
        private int _activeCount = 0;
        private long _totalGets = 0;
        private long _totalReturns = 0;
        private long _totalCreations = 0;
        private DateTime _lastMaintenance = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new GenericObjectPool instance.
        /// </summary>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="strategy">Pooling strategy to use</param>
        public GenericObjectPool(
            PoolConfiguration configuration, 
            IMessageBusService messageBusService = null,
            IPoolingStrategy strategy = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _messageBusService = messageBusService; // Optional - can be null
            _strategy = strategy ?? new DefaultPoolingStrategy();
            _objects = new ConcurrentQueue<T>();
            _statistics = new PoolStatistics { CreatedAt = DateTime.UtcNow };
            
            // Initialize profiler markers
            var typeName = typeof(T).Name;
            _getMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Get");
            _returnMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Return");
            _maintenanceMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Maintenance");

            if (!_strategy.ValidateConfiguration(_configuration))
                throw new ArgumentException("Invalid pool configuration for strategy", nameof(configuration));

            // Pre-populate with initial capacity
            WarmUp(_configuration.InitialCapacity);

            // Setup maintenance timer
            var interval = _strategy.GetValidationInterval();
            _maintenanceTimer = new Timer(PerformMaintenance, null, interval, interval);
        }

        /// <summary>
        /// Gets the name of this pool.
        /// </summary>
        public string Name => _configuration.Name;

        /// <summary>
        /// Gets the total number of objects in the pool.
        /// </summary>
        public int Count => _totalCount;

        /// <summary>
        /// Gets the number of objects available for use.
        /// </summary>
        public int AvailableCount => _objects.Count;

        /// <summary>
        /// Gets the number of objects currently in use.
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// Gets the configuration for this pool.
        /// </summary>
        public PoolConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the pooling strategy used by this pool.
        /// </summary>
        public IPoolingStrategy Strategy => _strategy;

        /// <summary>
        /// Gets an object from the pool, creating a new one if necessary.
        /// </summary>
        public T Get()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();

                T item = null;
                
                // Try to get from pool
                while (_objects.TryDequeue(out item))
                {
                    if (item.IsValid())
                    {
                        break;
                    }
                    // Invalid object, destroy it
                    DestroyObject(item);
                    item = null;
                }

                // Create new if needed
                if (item == null)
                {
                    if (_totalCount >= _configuration.MaxCapacity)
                    {
                        // Pool is at max capacity, wait or throw based on configuration
                        if (!_configuration.BlockWhenExhausted)
                        {
                            throw new InvalidOperationException($"Pool '{Name}' has reached maximum capacity of {_configuration.MaxCapacity}");
                        }
                        
                        // Simple spin wait for demo - production would use better waiting mechanism
                        var waitStart = DateTime.UtcNow;
                        while ((DateTime.UtcNow - waitStart).TotalMilliseconds < _configuration.MaxWaitTime)
                        {
                            if (_objects.TryDequeue(out item) && item.IsValid())
                            {
                                break;
                            }
                            Thread.Yield();
                        }
                        
                        if (item == null)
                        {
                            throw new TimeoutException($"Timeout waiting for available object in pool '{Name}'");
                        }
                    }
                    else
                    {
                        item = CreateNewObject();
                    }
                }

                // Configure object for use
                item.PoolName = Name;
                item.PoolId = Guid.NewGuid();
                item.LastUsed = DateTime.UtcNow;
                item.OnGet();
                
                Interlocked.Increment(ref _activeCount);
                Interlocked.Increment(ref _totalGets);
                
                // Publish object retrieved message if message bus is available
                PublishObjectRetrievedMessage(item);
                
                return item;
            }
        }

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        public void Return(T item)
        {
            using (_returnMarker.Auto())
            {
                if (item == null) return;
                if (_disposed) 
                {
                    DestroyObject(item);
                    return;
                }

                // Validate object before returning
                if (!item.CanBePooled())
                {
                    DestroyObject(item);
                    return;
                }

                // Reset and return to pool
                item.OnReturn();
                item.Reset();
                
                _objects.Enqueue(item);
                Interlocked.Decrement(ref _activeCount);
                Interlocked.Increment(ref _totalReturns);
                
                // Publish object returned message if message bus is available
                PublishObjectReturnedMessage(item);
            }
        }

        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            
            while (_objects.TryDequeue(out var item))
            {
                DestroyObject(item);
            }
            
            _totalCount = _activeCount;
        }

        /// <summary>
        /// Removes excess objects from the pool to reduce memory usage.
        /// </summary>
        public void TrimExcess()
        {
            ThrowIfDisposed();
            
            var targetCount = Math.Max(_configuration.InitialCapacity, _activeCount);
            var toRemove = _objects.Count - targetCount;
            
            for (int i = 0; i < toRemove; i++)
            {
                if (_objects.TryDequeue(out var item))
                {
                    DestroyObject(item);
                }
            }
        }

        /// <summary>
        /// Validates all objects in the pool.
        /// </summary>
        public bool Validate()
        {
            ThrowIfDisposed();
            
            var tempList = new System.Collections.Generic.List<T>();
            var allValid = true;
            
            // Dequeue all objects for validation
            while (_objects.TryDequeue(out var item))
            {
                if (item.IsValid())
                {
                    tempList.Add(item);
                }
                else
                {
                    allValid = false;
                    DestroyObject(item);
                }
            }
            
            // Re-enqueue valid objects
            foreach (var item in tempList)
            {
                _objects.Enqueue(item);
            }
            
            return allValid;
        }

        /// <summary>
        /// Gets statistics about this pool's usage.
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                return new PoolStatistics
                {
                    TotalCreations = _totalCreations,
                    TotalGets = _totalGets,
                    TotalReturns = _totalReturns,
                    CurrentSize = _totalCount,
                    ActiveObjects = _activeCount,
                    AvailableObjects = _objects.Count,
                    PeakSize = Math.Max(_totalCount, _statistics.PeakSize),
                    CreatedAt = _statistics.CreatedAt,
                    LastUpdated = DateTime.UtcNow,
                    LastMaintenance = _lastMaintenance
                };
            }
        }

        #region Message Publishing
        
        /// <summary>
        /// Publishes a message when an object is retrieved from the pool.
        /// </summary>
        private void PublishObjectRetrievedMessage(T item)
        {
            if (_messageBusService == null) return;
            
            try
            {
                var message = PoolObjectRetrievedMessage.Create(
                    poolName: new FixedString64Bytes(Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: Guid.NewGuid(), // Pool doesn't have a persistent ID
                    objectId: item.PoolId,
                    poolSizeAfter: Count,
                    activeObjectsAfter: ActiveCount
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch
            {
                // Swallow exceptions to avoid disrupting pool operations
            }
        }
        
        /// <summary>
        /// Publishes a message when an object is returned to the pool.
        /// </summary>
        private void PublishObjectReturnedMessage(T item)
        {
            if (_messageBusService == null) return;
            
            try
            {
                var message = PoolObjectReturnedMessage.Create(
                    poolName: new FixedString64Bytes(Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: Guid.NewGuid(), // Pool doesn't have a persistent ID
                    objectId: item.PoolId,
                    poolSizeAfter: Count,
                    activeObjectsAfter: ActiveCount,
                    wasValidOnReturn: item.IsValid()
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch
            {
                // Swallow exceptions to avoid disrupting pool operations
            }
        }
        
        #endregion

        /// <summary>
        /// Creates a new object for the pool.
        /// </summary>
        private T CreateNewObject()
        {
            var item = new T
            {
                PoolName = Name,
                CreatedAt = DateTime.UtcNow
            };
            
            Interlocked.Increment(ref _totalCount);
            Interlocked.Increment(ref _totalCreations);
            
            return item;
        }

        /// <summary>
        /// Destroys an object and removes it from the pool.
        /// </summary>
        private void DestroyObject(T item)
        {
            if (item == null) return;
            
            try
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                Interlocked.Decrement(ref _totalCount);
                
                // Could publish object destroyed message here if needed
                // For now, we're focusing on the main events from IPoolingService
            }
            catch
            {
                // Swallow disposal exceptions
            }
        }

        /// <summary>
        /// Pre-populates the pool with the specified number of objects.
        /// </summary>
        private void WarmUp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = CreateNewObject();
                _objects.Enqueue(item);
            }
        }

        /// <summary>
        /// Performs periodic maintenance on the pool.
        /// </summary>
        private void PerformMaintenance(object state)
        {
            if (_disposed) return;
            
            using (_maintenanceMarker.Auto())
            {
                try
                {
                    _lastMaintenance = DateTime.UtcNow;
                    
                    // Validate objects
                    Validate();
                    
                    // Trim excess if needed
                    if (_objects.Count > _configuration.InitialCapacity * 2)
                    {
                        TrimExcess();
                    }
                    
                    // Let strategy perform its maintenance
                    _strategy.PerformMaintenance(GetStatistics());
                }
                catch
                {
                    // Swallow maintenance exceptions
                }
            }
        }

        /// <summary>
        /// Throws if the pool has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GenericObjectPool<T>));
        }

        /// <summary>
        /// Disposes the pool and all contained objects.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _maintenanceTimer?.Dispose();
            
            Clear();
        }

        /// <summary>
        /// Default pooling strategy implementation.
        /// </summary>
        private class DefaultPoolingStrategy : IPoolingStrategy
        {
            public bool ValidateConfiguration(PoolConfiguration configuration)
            {
                return configuration != null &&
                       configuration.InitialCapacity > 0 &&
                       configuration.MaxCapacity >= configuration.InitialCapacity;
            }

            public TimeSpan GetValidationInterval()
            {
                return TimeSpan.FromMinutes(1);
            }

            public void PerformMaintenance(PoolStatistics statistics)
            {
                // Basic maintenance - can be extended
            }

            public bool ShouldExpandPool(PoolStatistics statistics)
            {
                return statistics.AvailableObjects == 0 && 
                       statistics.CurrentSize < 1000;
            }

            public bool ShouldContractPool(PoolStatistics statistics)
            {
                return statistics.AvailableObjects > statistics.ActiveObjects * 2;
            }

            public int CalculateExpansionSize(PoolStatistics statistics)
            {
                return Math.Min(10, 1000 - statistics.CurrentSize);
            }

            public int CalculateContractionSize(PoolStatistics statistics)
            {
                return statistics.AvailableObjects / 2;
            }
        }
    }
}