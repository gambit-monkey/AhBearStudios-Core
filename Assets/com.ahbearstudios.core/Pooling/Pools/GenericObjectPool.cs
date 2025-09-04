using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Profiling;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Common.Utilities;

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
        private readonly ProfilerMarker _validateMarker;
        
        private volatile bool _disposed = false;
        private int _totalCount = 0;
        private int _activeCount = 0;
        private long _totalGets = 0;
        private long _totalReturns = 0;
        private long _totalCreations = 0;
        private int _objectCounter = 0;
        private DateTime _lastMaintenance = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new GenericObjectPool instance.
        /// Use a factory to create pools with proper strategy dependencies.
        /// </summary>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="strategy">Pooling strategy to use (required)</param>
        public GenericObjectPool(
            PoolConfiguration configuration, 
            IMessageBusService messageBusService = null,
            IPoolingStrategy strategy = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _messageBusService = messageBusService; // Optional - can be null
            _strategy = strategy ?? throw new ArgumentException("Strategy cannot be null. Use a factory to create pools with proper strategy dependencies.", nameof(strategy));
            _objects = new ConcurrentQueue<T>();
            _statistics = new PoolStatistics 
            { 
                CreatedAt = DateTime.UtcNow,
                LastMaintenance = DateTime.UtcNow,
                InitialCapacity = _configuration.InitialCapacity,
                MaxCapacity = _configuration.MaxCapacity
            };
            
            // Initialize profiler markers
            var typeName = typeof(T).Name;
            _getMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Get");
            _returnMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Return");
            _maintenanceMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Maintenance");
            _validateMarker = new ProfilerMarker($"GenericObjectPool<{typeName}>.Validate");

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
                
                // Notify strategy that operation is starting
                _strategy.OnPoolOperationStart();
                var operationStart = DateTime.UtcNow;

                try
                {
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
                    item.PoolId = DeterministicIdGenerator.GeneratePooledObjectId(typeof(T).Name, Name, _objectCounter);
                    item.LastUsed = DateTime.UtcNow;
                    item.OnGet();
                    
                    Interlocked.Increment(ref _activeCount);
                    Interlocked.Increment(ref _totalGets);
                    
                    // Update peak active count if needed (thread-safe)
                    lock (_statsLock)
                    {
                        if (_activeCount > _statistics.PeakActiveCount)
                        {
                            _statistics.PeakActiveCount = _activeCount;
                        }
                    }
                
                    // Publish object retrieved message if message bus is available
                    PublishObjectRetrievedMessage(item);
                    
                    // Notify strategy that operation completed
                    var operationDuration = DateTime.UtcNow - operationStart;
                    _strategy.OnPoolOperationComplete(operationDuration);
                    
                    return item;
                }
                catch (Exception ex)
                {
                    // Notify strategy of error
                    _strategy.OnPoolError(ex);
                    throw; // Re-throw to maintain original behavior
                }
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
            using (_validateMarker.Auto())
            {
                ThrowIfDisposed();
                
                // Use managed collection for reference types but with ZLinq optimization
                var validObjects = new System.Collections.Generic.List<T>();
                var allValid = true;
                
                // Dequeue all objects for validation
                while (_objects.TryDequeue(out var item))
                {
                    if (item.IsValid())
                    {
                        validObjects.Add(item);
                    }
                    else
                    {
                        allValid = false;
                        DestroyObject(item);
                    }
                }
                
                // Re-enqueue valid objects
                foreach (var item in validObjects)
                {
                    _objects.Enqueue(item);
                }
                
                return allValid;
            }
        }

        /// <summary>
        /// Gets statistics about this pool's usage.
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                // Update peak size if current total is higher
                if (_totalCount > _statistics.PeakSize)
                {
                    _statistics.PeakSize = _totalCount;
                }
                
                // Update real-time values in statistics
                _statistics.TotalCreated = _totalCreations;
                _statistics.TotalGets = _totalGets;
                _statistics.TotalReturns = _totalReturns;
                _statistics.TotalCount = _totalCount;
                _statistics.ActiveCount = _activeCount;
                _statistics.AvailableCount = _objects.Count;
                _statistics.LastUpdated = DateTime.UtcNow;
                _statistics.LastMaintenance = _lastMaintenance;
                
                return new PoolStatistics
                {
                    TotalCreated = _statistics.TotalCreated,
                    TotalGets = _statistics.TotalGets,
                    TotalReturns = _statistics.TotalReturns,
                    TotalCount = _statistics.TotalCount,
                    ActiveCount = _statistics.ActiveCount,
                    AvailableCount = _statistics.AvailableCount,
                    PeakSize = _statistics.PeakSize,
                    PeakActiveCount = _statistics.PeakActiveCount,
                    CreatedAt = _statistics.CreatedAt,
                    LastUpdated = _statistics.LastUpdated,
                    LastMaintenance = _statistics.LastMaintenance,
                    InitialCapacity = _configuration.InitialCapacity,
                    MaxCapacity = _configuration.MaxCapacity
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
                var message = PoolObjectRetrievedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: DeterministicIdGenerator.GeneratePoolId(typeof(T).Name, Name), // Generate deterministic pool ID
                    objectId: item.PoolId,
                    poolSizeAfter: Count,
                    activeObjectsAfter: ActiveCount
                );
                
                _messageBusService.PublishMessageAsync(message).Forget();
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
                var message = PoolObjectReturnedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: DeterministicIdGenerator.GeneratePoolId(typeof(T).Name, Name), // Generate deterministic pool ID
                    objectId: item.PoolId,
                    poolSizeAfter: Count,
                    activeObjectsAfter: ActiveCount,
                    wasValidOnReturn: item.IsValid()
                );
                
                _messageBusService.PublishMessageAsync(message).Forget();
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
            var objectId = Interlocked.Increment(ref _objectCounter);
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
                    
                    // Update statistics maintenance timestamp
                    lock (_statsLock)
                    {
                        _statistics.LastMaintenance = _lastMaintenance;
                    }
                    
                    // Validate objects
                    Validate();
                    
                    // Trim excess if needed
                    if (_objects.Count > _configuration.InitialCapacity * 2)
                    {
                        TrimExcess();
                    }
                    
                    // Let strategy know maintenance operation is starting
                    _strategy.OnPoolOperationStart();
                    
                    // Get maintenance duration for strategy performance monitoring
                    var maintenanceStart = DateTime.UtcNow;
                    
                    // Strategy can evaluate pool health and trigger circuit breaker if needed
                    var stats = GetStatistics();
                    if (_strategy.ShouldTriggerCircuitBreaker(stats))
                    {
                        // Log circuit breaker trigger (could publish message here if needed)
                        // For now, we'll just note it happened
                    }
                    
                    // Notify strategy that maintenance operation completed
                    var maintenanceDuration = DateTime.UtcNow - maintenanceStart;
                    _strategy.OnPoolOperationComplete(maintenanceDuration);
                }
                catch (Exception ex)
                {
                    // Notify strategy of maintenance error
                    _strategy.OnPoolError(ex);
                    // Swallow maintenance exceptions to prevent pool failure
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

    }
}