using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Profiling;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Primary pooling service implementation following Builder → Config → Factory → Service pattern.
    /// Manages object pools with production-ready features including health monitoring and validation.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public class PoolingService : IPoolingService, IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> _pools;
        private readonly IPoolValidationService _validationService;
        private readonly IPooledNetworkBufferFactory _bufferFactory;
        private readonly INetworkPoolingConfigBuilder _configBuilder;
        private readonly IMessageBusService _messageBusService;
        private readonly ProfilerMarker _getMarker;
        private readonly ProfilerMarker _returnMarker;
        private readonly ProfilerMarker _registerMarker;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the PoolingService.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing pool events</param>
        /// <param name="validationService">Service for pool validation operations</param>
        /// <param name="bufferFactory">Factory for creating network buffers</param>
        /// <param name="configBuilder">Builder for creating pool configurations</param>
        public PoolingService(
            IMessageBusService messageBusService,
            IPoolValidationService validationService = null,
            IPooledNetworkBufferFactory bufferFactory = null,
            INetworkPoolingConfigBuilder configBuilder = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _pools = new ConcurrentDictionary<Type, object>();
            _validationService = validationService ?? new PoolValidationService();
            _bufferFactory = bufferFactory ?? new PooledNetworkBufferFactory();
            _configBuilder = configBuilder ?? new NetworkPoolingConfigBuilder(_bufferFactory);
            
            // Initialize profiler markers
            _getMarker = new ProfilerMarker("PoolingService.Get");
            _returnMarker = new ProfilerMarker("PoolingService.Return");
            _registerMarker = new ProfilerMarker("PoolingService.Register");
        }

        /// <summary>
        /// Gets an object from the appropriate pool.
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <returns>Object from the pool</returns>
        public T Get<T>() where T : class, IPooledObject, new()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();

                if (!_pools.TryGetValue(typeof(T), out var poolObj))
                {
                    throw new InvalidOperationException($"No pool registered for type {typeof(T).Name}. Call RegisterPool<T>() first.");
                }

                if (poolObj is not GenericObjectPool<T> pool)
                {
                    throw new InvalidOperationException($"Pool for type {typeof(T).Name} is not the expected type.");
                }

                var item = pool.Get();
                
                // Publish pool object retrieved message
                PublishObjectRetrievedMessage(item, pool);
                
                return item;
            }
        }

        /// <summary>
        /// Returns an object to the appropriate pool.
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="item">Object to return to the pool</param>
        public void Return<T>(T item) where T : class, IPooledObject
        {
            using (_returnMarker.Auto())
            {
                ThrowIfDisposed();

                if (item == null) return;

                if (!_pools.TryGetValue(typeof(T), out var poolObj))
                {
                    // If no pool is registered, dispose if possible
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    return;
                }

                if (poolObj is GenericObjectPool<T> pool)
                {
                    pool.Return(item);
                    
                    // Publish pool object returned message
                    PublishObjectReturnedMessage(item, pool);
                }
                else
                {
                    // Pool type mismatch, dispose item
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Registers a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to register pool for</typeparam>
        /// <param name="configuration">Pool configuration</param>
        public void RegisterPool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            using (_registerMarker.Auto())
            {
                ThrowIfDisposed();

                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                var poolType = typeof(T);
                
                if (_pools.ContainsKey(poolType))
                {
                    throw new InvalidOperationException($"Pool for type {poolType.Name} is already registered.");
                }

                var pool = new GenericObjectPool<T>(configuration, _messageBusService);
                
                if (!_pools.TryAdd(poolType, pool))
                {
                    pool.Dispose();
                    throw new InvalidOperationException($"Failed to register pool for type {poolType.Name}.");
                }
            }
        }

        /// <summary>
        /// Creates a network pooling configuration using the builder pattern.
        /// </summary>
        /// <returns>Network pooling configuration builder</returns>
        public INetworkPoolingConfigBuilder CreateNetworkPoolingConfig()
        {
            ThrowIfDisposed();
            return new NetworkPoolingConfigBuilder(_bufferFactory);
        }

        /// <summary>
        /// Gets statistics for all registered pools.
        /// </summary>
        /// <returns>Dictionary of pool statistics by type name</returns>
        public Dictionary<string, PoolStatistics> GetAllPoolStatistics()
        {
            ThrowIfDisposed();
            
            var statistics = new Dictionary<string, PoolStatistics>();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool poolInterface)
                {
                    statistics[kvp.Key.Name] = poolInterface.GetStatistics();
                }
            }
            
            return statistics;
        }

        /// <summary>
        /// Gets an object from the specified pool type asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <returns>Object from the pool</returns>
        public async UniTask<T> GetAsync<T>() where T : class, IPooledObject, new()
        {
            return await UniTask.FromResult(Get<T>());
        }

        /// <summary>
        /// Returns an object to its pool asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        public async UniTask ReturnAsync<T>(T item) where T : class, IPooledObject
        {
            Return(item);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Registers a pool for the specified type with default configuration.
        /// </summary>
        /// <typeparam name="T">Type to register pool for</typeparam>
        /// <param name="poolName">Name of the pool</param>
        public void RegisterPool<T>(string poolName = null) where T : class, IPooledObject, new()
        {
            var name = poolName ?? typeof(T).Name;
            var config = PoolConfiguration.CreateDefault(name);
            RegisterPool<T>(config);
        }

        /// <summary>
        /// Unregisters and disposes a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to unregister pool for</typeparam>
        public void UnregisterPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            var poolType = typeof(T);
            if (_pools.TryRemove(poolType, out var pool))
            {
                if (pool is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Checks if a pool is registered for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if pool is registered</returns>
        public bool IsPoolRegistered<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            return _pools.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Gets statistics for a specific pool type.
        /// </summary>
        /// <typeparam name="T">Pool type to get statistics for</typeparam>
        /// <returns>Pool statistics or null if not registered</returns>
        public PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is IObjectPool poolInterface)
            {
                return poolInterface.GetStatistics();
            }
            
            return null;
        }

        /// <summary>
        /// Validates a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to validate</typeparam>
        /// <returns>True if pool is healthy</returns>
        public bool ValidatePool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is IObjectPool poolInterface)
            {
                var isValid = poolInterface.Validate();
                if (!isValid)
                {
                    PublishValidationIssuesMessage(typeof(T).Name, poolInterface, 1);
                }
                return isValid;
            }
            
            return false;
        }

        /// <summary>
        /// Clears all pools and releases resources.
        /// </summary>
        public void ClearAllPools()
        {
            ThrowIfDisposed();
            
            foreach (var pool in _pools.Values)
            {
                if (pool is IObjectPool poolInterface)
                {
                    poolInterface.Clear();
                }
            }
        }

        /// <summary>
        /// Clears all objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to clear</typeparam>
        public void ClearPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is IObjectPool poolInterface)
            {
                poolInterface.Clear();
            }
        }

        /// <summary>
        /// Removes excess objects from all pools to reduce memory usage.
        /// </summary>
        public void TrimAllPools()
        {
            ThrowIfDisposed();
            
            foreach (var pool in _pools.Values)
            {
                if (pool is IObjectPool poolInterface)
                {
                    poolInterface.TrimExcess();
                }
            }
        }

        /// <summary>
        /// Removes excess objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to trim</typeparam>
        public void TrimPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is IObjectPool poolInterface)
            {
                poolInterface.TrimExcess();
            }
        }

        /// <summary>
        /// Validates all pools and returns health status.
        /// </summary>
        /// <returns>True if all pools are healthy</returns>
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            
            bool allValid = true;
            int totalIssues = 0;
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    if (!pool.Validate())
                    {
                        allValid = false;
                        totalIssues++;
                    }
                }
            }
            
            if (totalIssues > 0)
            {
                PublishValidationIssuesMessage("AllPools", null, totalIssues);
            }
            
            return allValid;
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolingService));
        }

        #region Message Bus Integration
        
        /// <summary>
        /// Gets the message bus service used for publishing pool events.
        /// </summary>
        public IMessageBusService MessageBus => _messageBusService;
        
        #endregion
        
        #region Private Message Publishing
        
        /// <summary>
        /// Publishes a message when an object is retrieved from a pool.
        /// </summary>
        private void PublishObjectRetrievedMessage<T>(T item, GenericObjectPool<T> pool) where T : class, IPooledObject
        {
            try
            {
                var message = PoolObjectRetrievedMessage.Create(
                    poolName: new FixedString64Bytes(pool.Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: Guid.NewGuid(), // Pool doesn't have ID, using new GUID
                    objectId: item.PoolId,
                    poolSizeAfter: pool.Count,
                    activeObjectsAfter: pool.ActiveCount
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch
            {
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }
        
        /// <summary>
        /// Publishes a message when an object is returned to a pool.
        /// </summary>
        private void PublishObjectReturnedMessage<T>(T item, GenericObjectPool<T> pool) where T : class, IPooledObject
        {
            try
            {
                var message = PoolObjectReturnedMessage.Create(
                    poolName: new FixedString64Bytes(pool.Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: Guid.NewGuid(), // Pool doesn't have ID, using new GUID
                    objectId: item.PoolId,
                    poolSizeAfter: pool.Count,
                    activeObjectsAfter: pool.ActiveCount,
                    wasValidOnReturn: item.IsValid()
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch
            {
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }
        
        /// <summary>
        /// Publishes a message when pool validation finds issues.
        /// </summary>
        private void PublishValidationIssuesMessage(string poolName, IObjectPool pool, int issueCount)
        {
            try
            {
                var message = PoolValidationIssuesMessage.Create(
                    poolName: new FixedString64Bytes(poolName),
                    objectTypeName: new FixedString64Bytes(poolName), // Use pool name as object type for general validation
                    poolId: Guid.NewGuid(), // Pool doesn't have ID, using new GUID
                    issueCount: issueCount,
                    objectsValidated: pool?.Count ?? 0,
                    invalidObjects: issueCount,
                    corruptedObjects: 0, // Would need more detailed validation to determine this
                    severity: issueCount > 5 ? ValidationSeverity.Major : ValidationSeverity.Moderate
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch
            {
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }
        
        #endregion

        /// <summary>
        /// Disposes the pooling service and all registered pools.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var pool in _pools.Values)
                {
                    if (pool is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                _pools.Clear();
                _disposed = true;
            }
        }
    }
}