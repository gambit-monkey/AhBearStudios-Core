using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
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
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Pooling.HealthChecks;
using AhBearStudios.Core.HealthChecking.Factories;

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
        private readonly IPoolingStrategySelector _strategySelector;
        private readonly IPoolTypeSelector _poolTypeSelector;
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;
        private readonly ConcurrentDictionary<string, ICircuitBreaker> _circuitBreakers;
        private readonly ConcurrentDictionary<string, PerformanceBudgetTracker> _performanceTrackers;
        private readonly ProfilerMarker _getMarker;
        private readonly ProfilerMarker _returnMarker;
        private readonly ProfilerMarker _registerMarker;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the PoolingService.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing pool events</param>
        /// <param name="loggingService">Logging service for pool operations</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="healthCheckService">Health check service for system health monitoring</param>
        /// <param name="circuitBreakerFactory">Factory for creating circuit breakers to protect pool operations</param>
        /// <param name="strategySelector">Strategy selector for choosing appropriate pooling strategies</param>
        /// <param name="poolTypeSelector">Pool type selector for choosing appropriate pool implementations</param>
        /// <param name="validationService">Service for pool validation operations</param>
        /// <param name="bufferFactory">Factory for creating network buffers</param>
        /// <param name="configBuilder">Builder for creating pool configurations</param>
        public PoolingService(
            IMessageBusService messageBusService,
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IHealthCheckService healthCheckService = null,
            ICircuitBreakerFactory circuitBreakerFactory = null,
            IPoolingStrategySelector strategySelector = null,
            IPoolTypeSelector poolTypeSelector = null,
            IPoolValidationService validationService = null,
            IPooledNetworkBufferFactory bufferFactory = null,
            INetworkPoolingConfigBuilder configBuilder = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _healthCheckService = healthCheckService; // Optional service
            _circuitBreakerFactory = circuitBreakerFactory; // Optional service
            _circuitBreakers = new ConcurrentDictionary<string, ICircuitBreaker>();
            _performanceTrackers = new ConcurrentDictionary<string, PerformanceBudgetTracker>();
            _pools = new ConcurrentDictionary<Type, object>();
            _validationService = validationService ?? new PoolValidationService();
            _bufferFactory = bufferFactory ?? new PooledNetworkBufferFactory();
            _configBuilder = configBuilder ?? new NetworkPoolingConfigBuilder(_bufferFactory);
            
            // Initialize strategy selector with fallback if none provided
            _strategySelector = strategySelector ?? CreateDefaultStrategySelector();
            
            // Initialize pool type selector with fallback if none provided
            _poolTypeSelector = poolTypeSelector ?? new PoolTypeSelector(_loggingService, _messageBusService);
            
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

                if (poolObj is not IObjectPool<T> pool)
                {
                    throw new InvalidOperationException($"Pool for type {typeof(T).Name} does not implement IObjectPool<T>.");
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

                if (poolObj is IObjectPool<T> pool)
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

                var strategy = _strategySelector.SelectStrategy(configuration);
                var selectedPoolType = _poolTypeSelector.SelectPoolType<T>(configuration);
                var pool = CreatePoolInstance<T>(selectedPoolType, configuration, strategy);
                
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
            return await GetAsync<T>(CancellationToken.None);
        }

        /// <summary>
        /// Gets an object from the specified pool type asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Object from the pool</returns>
        public async UniTask<T> GetAsync<T>(CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            return await GetAsync<T>(TimeSpan.FromSeconds(5), cancellationToken);
        }

        /// <summary>
        /// Gets an object from the specified pool type asynchronously with timeout and cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="timeout">Maximum time to wait for an object</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Object from the pool</returns>
        public async UniTask<T> GetAsync<T>(TimeSpan timeout, CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();

                var poolTypeName = typeof(T).Name;

                try
                {
                    // Get performance budget from pool configuration if available
                    var budget = GetPerformanceBudgetForPool<T>();
                    
                    // Execute with circuit breaker protection and performance monitoring
                    return await ExecuteWithCircuitBreakerAndPerformanceMonitoring(poolTypeName, "get", async ct =>
                    {
                        // Create a timeout token that combines with the provided cancellation token
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        timeoutCts.CancelAfter(timeout);

                        // Use UniTask.RunOnThreadPool for potentially blocking operations
                        return await UniTask.RunOnThreadPool(() =>
                        {
                            timeoutCts.Token.ThrowIfCancellationRequested();
                            
                            if (!_pools.TryGetValue(typeof(T), out var poolObj))
                            {
                                throw new InvalidOperationException($"No pool registered for type {poolTypeName}. Call RegisterPool<T>() first.");
                            }

                            if (poolObj is not IObjectPool<T> pool)
                            {
                                throw new InvalidOperationException($"Pool for type {poolTypeName} does not implement IObjectPool<T>.");
                            }

                            var item = pool.Get();
                            
                            // Publish pool object retrieved message
                            PublishObjectRetrievedMessage(item, pool);
                            
                            return item;
                        }, true, timeoutCts.Token);
                    }, cancellationToken, budget);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _loggingService.LogWarning($"GetAsync<{poolTypeName}> was cancelled by caller");
                    throw;
                }
                catch (OperationCanceledException)
                {
                    _loggingService.LogWarning($"GetAsync<{poolTypeName}> timed out after {timeout.TotalMilliseconds}ms");
                    throw new TimeoutException($"Failed to get object from pool {poolTypeName} within {timeout.TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"GetAsync<{poolTypeName}> failed", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns an object to its pool asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        public async UniTask ReturnAsync<T>(T item) where T : class, IPooledObject
        {
            await ReturnAsync(item, CancellationToken.None);
        }

        /// <summary>
        /// Returns an object to its pool asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public async UniTask ReturnAsync<T>(T item, CancellationToken cancellationToken) where T : class, IPooledObject
        {
            using (_returnMarker.Auto())
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();

                if (item == null) 
                    return;

                var poolTypeName = typeof(T).Name;

                try
                {
                    // Execute with circuit breaker protection
                    await ExecuteWithCircuitBreaker(poolTypeName, async ct =>
                    {
                        // Use UniTask.RunOnThreadPool for potentially blocking validation operations
                        await UniTask.RunOnThreadPool(() =>
                        {
                            ct.ThrowIfCancellationRequested();

                            if (!_pools.TryGetValue(typeof(T), out var poolObj))
                            {
                                // If no pool is registered, dispose if possible
                                if (item is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                                return;
                            }

                            if (poolObj is IObjectPool<T> pool)
                            {
                                // Validate object before returning if validation service is available
                                if (_validationService != null)
                                {
                                    try
                                    {
                                        if (_validationService.ShouldDisposeObject(item))
                                        {
                                            _loggingService.LogDebug($"Object of type {poolTypeName} will be disposed instead of returned to pool");
                                            if (item is IDisposable disposable)
                                            {
                                                disposable.Dispose();
                                            }
                                            return;
                                        }

                                        // Reset the object before returning it
                                        _validationService.ResetPooledObject(item);
                                    }
                                    catch (Exception ex)
                                    {
                                        _loggingService.LogException($"Validation failed for object of type {poolTypeName}, disposing instead of returning", ex);
                                        if (item is IDisposable disposable)
                                        {
                                            disposable.Dispose();
                                        }
                                        return;
                                    }
                                }

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
                        }, true, ct);
                    }, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _loggingService.LogWarning($"ReturnAsync<{poolTypeName}> was cancelled by caller");
                    // Still try to dispose the item if possible
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"ReturnAsync<{poolTypeName}> failed", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets multiple objects from the specified pool type asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="count">Number of objects to get</param>
        /// <param name="timeout">Maximum time to wait for all objects</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>List of objects from the pool</returns>
        public async UniTask<List<T>> GetMultipleAsync<T>(int count, TimeSpan timeout, CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            var results = new List<T>(count);
            
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                // Get objects concurrently but with a limit to avoid overwhelming the pool
                var semaphore = new SemaphoreSlim(Math.Min(count, 10)); // Max 10 concurrent gets
                var tasks = new List<UniTask<T>>(count);

                for (int i = 0; i < count; i++)
                {
                    tasks.Add(GetSingleWithSemaphore<T>(semaphore, timeoutCts.Token));
                }

                var allResults = await UniTask.WhenAll(tasks);
                results.AddRange(allResults);
                
                return results;
            }
            catch (Exception ex)
            {
                // Return any objects we did get back to the pool
                foreach (var item in results)
                {
                    try
                    {
                        await ReturnAsync(item, CancellationToken.None);
                    }
                    catch (Exception returnEx)
                    {
                        _loggingService.LogException($"Failed to return object during cleanup in GetMultipleAsync", returnEx);
                    }
                }
                
                _loggingService.LogException($"GetMultipleAsync<{typeof(T).Name}> failed after getting {results.Count}/{count} objects", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns multiple objects to their pools asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="items">Objects to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public async UniTask ReturnMultipleAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken) where T : class, IPooledObject
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (items == null)
                return;

            var itemsList = items.ToList();
            if (itemsList.Count == 0)
                return;

            try
            {
                // Return objects concurrently but with a limit
                var semaphore = new SemaphoreSlim(Math.Min(itemsList.Count, 10)); // Max 10 concurrent returns
                var tasks = itemsList.Select(item => ReturnSingleWithSemaphore(item, semaphore, cancellationToken));

                await UniTask.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"ReturnMultipleAsync<{typeof(T).Name}> failed for {itemsList.Count} objects", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates all pools asynchronously with timeout support.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for validation</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if all pools are healthy</returns>
        public async UniTask<bool> ValidateAllPoolsAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                return await UniTask.RunOnThreadPool(() =>
                {
                    timeoutCts.Token.ThrowIfCancellationRequested();
                    return ValidateAllPools();
                }, true, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _loggingService.LogWarning("ValidateAllPoolsAsync was cancelled by caller");
                throw;
            }
            catch (OperationCanceledException)
            {
                _loggingService.LogWarning($"ValidateAllPoolsAsync timed out after {timeout.TotalMilliseconds}ms");
                throw new TimeoutException($"Pool validation timed out after {timeout.TotalMilliseconds}ms");
            }
        }

        /// <summary>
        /// Helper method to get a single object with semaphore control.
        /// </summary>
        private async UniTask<T> GetSingleWithSemaphore<T>(SemaphoreSlim semaphore, CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GetAsync<T>(cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Helper method to return a single object with semaphore control.
        /// </summary>
        private async UniTask ReturnSingleWithSemaphore<T>(T item, SemaphoreSlim semaphore, CancellationToken cancellationToken) where T : class, IPooledObject
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ReturnAsync(item, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
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
        /// Validates a specific pool using both basic and comprehensive validation.
        /// </summary>
        /// <typeparam name="T">Pool type to validate</typeparam>
        /// <returns>True if pool is healthy</returns>
        public bool ValidatePool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is IObjectPool poolInterface)
            {
                // Basic pool validation
                var isValid = poolInterface.Validate();
                
                // Enhanced validation using validation service
                if (isValid && _validationService != null)
                {
                    try
                    {
                        // Validate pool configuration and state
                        var statistics = poolInterface.GetStatistics();
                        
                        // Check for validation service specific issues
                        // Since we don't have direct access to pooled objects here,
                        // we validate the pool's overall health
                        if (statistics.ActiveObjects > statistics.TotalCapacity * 0.95) // 95% threshold
                        {
                            _loggingService.LogWarning($"Pool {typeof(T).Name} is near capacity limit");
                            isValid = false;
                        }
                        
                        if (statistics.FailedGets > statistics.TotalGets * 0.1) // 10% failure rate threshold
                        {
                            _loggingService.LogWarning($"Pool {typeof(T).Name} has high failure rate");
                            isValid = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Enhanced validation failed for pool {typeof(T).Name}", ex);
                        isValid = false;
                    }
                }
                
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
        private void PublishObjectRetrievedMessage<T>(T item, IObjectPool<T> pool) where T : class, IPooledObject
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
        private void PublishObjectReturnedMessage<T>(T item, IObjectPool<T> pool) where T : class, IPooledObject
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
        
        #region Private Helper Methods
        
        /// <summary>
        /// Creates a default strategy selector when none is provided.
        /// This provides a fallback that creates default strategies.
        /// </summary>
        /// <returns>Default strategy selector implementation</returns>
        private IPoolingStrategySelector CreateDefaultStrategySelector()
        {
            try
            {
                // Try to create strategy factories with dependencies
                var fixedSizeFactory = new FixedSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, null);
                var dynamicSizeFactory = new DynamicSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService);
                var highPerformanceFactory = new HighPerformanceStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService);
                var adaptiveNetworkFactory = new AdaptiveNetworkStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService);
                var circuitBreakerFactory = new CircuitBreakerStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService);
                
                return new PoolingStrategySelector(
                    _loggingService,
                    _profilerService, 
                    _alertService,
                    _messageBusService,
                    fixedSizeFactory,
                    dynamicSizeFactory,
                    highPerformanceFactory,
                    adaptiveNetworkFactory,
                    circuitBreakerFactory);
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to create default strategy selector, using simple fallback", ex);
                return new SimpleStrategySelector(_loggingService, _profilerService, _alertService, _messageBusService);
            }
        }

        /// <summary>
        /// Creates a pool instance using the appropriate factory based on pool type.
        /// Follows CLAUDE.md Builder → Config → Factory → Service pattern by delegating creation to specialized factories.
        /// </summary>
        /// <typeparam name="T">Type of objects that will be pooled</typeparam>
        /// <param name="poolType">The type of pool to create</param>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="strategy">Pooling strategy to use</param>
        /// <returns>Configured pool instance</returns>
        private IObjectPool<T> CreatePoolInstance<T>(PoolType poolType, PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            _loggingService.LogInfo($"Creating {poolType} pool for {typeof(T).Name}");

            return poolType switch
            {
                PoolType.SmallBuffer => CreateNetworkBufferPool<T>(PoolType.SmallBuffer, configuration, strategy),
                PoolType.MediumBuffer => CreateNetworkBufferPool<T>(PoolType.MediumBuffer, configuration, strategy),
                PoolType.LargeBuffer => CreateNetworkBufferPool<T>(PoolType.LargeBuffer, configuration, strategy),
                PoolType.CompressionBuffer => CreateNetworkBufferPool<T>(PoolType.CompressionBuffer, configuration, strategy),
                PoolType.ManagedLogData => CreateManagedLogDataPool<T>(configuration, strategy),
                PoolType.Generic => CreateGenericPool<T>(configuration, strategy),
                _ => CreateGenericPool<T>(configuration, strategy)
            };
        }

        /// <summary>
        /// Creates network buffer pools using type-safe casting for PooledNetworkBuffer types.
        /// Uses appropriate factories following CLAUDE.md pattern.
        /// </summary>
        private IObjectPool<T> CreateNetworkBufferPool<T>(PoolType poolType, PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            // Network buffer pools only work with PooledNetworkBuffer types
            if (typeof(T) == typeof(PooledNetworkBuffer))
            {
                // Create strategy factory for network buffer pools
                var adaptiveNetworkStrategyFactory = new AdaptiveNetworkStrategyFactory(
                    _loggingService, _profilerService, _alertService, _messageBusService);

                var networkBufferPool = poolType switch
                {
                    PoolType.SmallBuffer => new SmallBufferPool(configuration, adaptiveNetworkStrategyFactory),
                    PoolType.MediumBuffer => new MediumBufferPool(configuration, new HighPerformanceStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService)),
                    PoolType.LargeBuffer => new LargeBufferPool(configuration, new DynamicSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService)),
                    PoolType.CompressionBuffer => new CompressionBufferPool(configuration, adaptiveNetworkStrategyFactory),
                    _ => throw new NotSupportedException($"Network buffer pool type {poolType} is not supported")
                };
                
                return (IObjectPool<T>)(object)networkBufferPool;
            }
            
            // For non-PooledNetworkBuffer types, fall back to generic pool
            _loggingService.LogWarning($"Type {typeof(T).Name} is not compatible with network buffer pool {poolType}, using generic pool");
            return CreateGenericPool<T>(configuration, strategy);
        }

        /// <summary>
        /// Creates managed log data pools using type-safe casting.
        /// Uses appropriate factories following CLAUDE.md pattern.
        /// </summary>
        private IObjectPool<T> CreateManagedLogDataPool<T>(PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            if (typeof(T) == typeof(ManagedLogData))
            {
                // Create strategy factory for managed log data pools  
                var highPerformanceStrategyFactory = new HighPerformanceStrategyFactory(
                    _loggingService, _profilerService, _alertService, _messageBusService);
                    
                var logDataPool = new ManagedLogDataPool(configuration, highPerformanceStrategyFactory);
                return (IObjectPool<T>)(object)logDataPool;
            }
            
            // For non-ManagedLogData types, fall back to generic pool
            _loggingService.LogWarning($"Type {typeof(T).Name} is not compatible with ManagedLogData pool, using generic pool");
            return CreateGenericPool<T>(configuration, strategy);
        }

        /// <summary>
        /// Creates generic pools that can handle any IPooledObject type.
        /// </summary>
        private IObjectPool<T> CreateGenericPool<T>(PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            return new GenericObjectPool<T>(configuration, _messageBusService, strategy);
        }
        
        #endregion
        
        #region Health Integration
        
        /// <summary>
        /// Registers pooling service health checks with the health check service.
        /// This method should be called after the service is fully initialized.
        /// </summary>
        public void RegisterHealthChecks()
        {
            if (_healthCheckService == null)
            {
                _loggingService.LogDebug("Health check service not available, skipping health check registration");
                return;
            }
            
            try
            {
                // Register main pooling service health check
                var poolingHealthCheck = new PoolingServiceHealthCheck(this);
                _healthCheckService.RegisterHealthCheck(poolingHealthCheck);
                
                _loggingService.LogInfo("Registered PoolingService health check");
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to register pooling service health checks", ex);
            }
        }
        
        /// <summary>
        /// Gets the registered health check service.
        /// </summary>
        public IHealthCheckService HealthCheckService => _healthCheckService;
        
        /// <summary>
        /// Validates a pooled object using the validation service.
        /// Provides detailed validation beyond basic pool validation.
        /// </summary>
        /// <param name="pooledObject">Object to validate</param>
        /// <returns>True if the object is valid for use</returns>
        public bool ValidatePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return false;
                
            return _validationService.ValidatePooledObject(pooledObject);
        }
        
        /// <summary>
        /// Resets a pooled object using the validation service.
        /// Handles circuit breaker logic and object cleanup.
        /// </summary>
        /// <param name="pooledObject">Object to reset</param>
        public void ResetPooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return;
                
            _validationService.ResetPooledObject(pooledObject);
        }
        
        /// <summary>
        /// Determines if a pooled object should be disposed using the validation service.
        /// Checks object health and circuit breaker status.
        /// </summary>
        /// <param name="pooledObject">Object to check</param>
        /// <returns>True if the object should be disposed</returns>
        public bool ShouldDisposePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return true;
                
            return _validationService.ShouldDisposeObject(pooledObject);
        }
        
        /// <summary>
        /// Performs comprehensive validation and cleanup of all pools using the validation service.
        /// This method validates individual objects and disposes unhealthy ones.
        /// </summary>
        /// <returns>Number of objects that were disposed due to validation failures</returns>
        public int PerformComprehensivePoolValidation()
        {
            ThrowIfDisposed();
            
            int disposedObjects = 0;
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool<IPooledObject> pool)
                {
                    try
                    {
                        // This would require access to individual pooled objects
                        // Since we don't have direct access, we'll perform pool-level validation
                        var statistics = pool.GetStatistics();
                        
                        // If pool has high failure rate, consider it unhealthy
                        if (statistics.FailedGets > statistics.TotalGets * 0.15) // 15% threshold for cleanup
                        {
                            // Clear and rebuild the pool
                            pool.Clear();
                            disposedObjects += statistics.CurrentSize;
                            
                            _loggingService.LogWarning($"Pool {kvp.Key.Name} was cleared due to high failure rate");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Comprehensive validation failed for pool {kvp.Key.Name}", ex);
                    }
                }
            }
            
            return disposedObjects;
        }
        
        /// <summary>
        /// Gets comprehensive health data for all network buffer pools.
        /// Aggregates statistics from all buffer pool types for health monitoring.
        /// </summary>
        /// <returns>Network buffer pool health data</returns>
        public NetworkBufferPoolHealthData GetNetworkBufferPoolHealthData()
        {
            ThrowIfDisposed();
            
            var healthData = new NetworkBufferPoolHealthData();
            long totalMemoryUsage = 0;
            int totalBuffersCreated = 0;
            int activeBuffers = 0;
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    totalBuffersCreated += pool.Count;
                    activeBuffers += pool.ActiveCount;
                    
                    // Estimate memory usage based on pool type
                    if (kvp.Key.Name.Contains("Buffer") || kvp.Key.Name.Contains("Network"))
                    {
                        totalMemoryUsage += _poolTypeSelector.GetEstimatedMemoryUsage<IPooledObject>(PoolType.MediumBuffer) * pool.Count;
                    }
                    else
                    {
                        totalMemoryUsage += _poolTypeSelector.GetEstimatedMemoryUsage<IPooledObject>(PoolType.Generic) * pool.Count;
                    }
                }
            }
            
            healthData.TotalBuffersCreated = totalBuffersCreated;
            healthData.ActiveBuffers = activeBuffers;
            healthData.MemoryUsageBytes = totalMemoryUsage;
            
            return healthData;
        }
        
        /// <summary>
        /// Gets the health status of all pooling strategies currently in use.
        /// Provides detailed health information for each strategy instance.
        /// </summary>
        /// <returns>Dictionary of strategy health status by pool type name</returns>
        public Dictionary<string, StrategyHealthStatus> GetStrategyHealthStatuses()
        {
            ThrowIfDisposed();
            
            var healthStatuses = new Dictionary<string, StrategyHealthStatus>();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool<IPooledObject> pool)
                {
                    var strategy = pool.Strategy;
                    var poolTypeName = kvp.Key.Name;
                    
                    // Get strategy health status if the strategy supports it
                    try
                    {
                        // Check if strategy is healthy based on basic metrics
                        var statistics = pool.GetStatistics();
                        var isHealthy = pool.Validate();
                        
                        if (isHealthy)
                        {
                            var metrics = new Dictionary<string, object>
                            {
                                ["PoolCount"] = statistics.CurrentSize,
                                ["ActiveCount"] = statistics.ActiveObjects,
                                ["AvailableCount"] = statistics.AvailableObjects
                            };
                            
                            var healthStatus = new StrategyHealthStatus
                            {
                                Status = StrategyHealth.Healthy,
                                Description = $"Strategy for {poolTypeName} operating normally",
                                Timestamp = DateTime.UtcNow,
                                Metrics = metrics,
                                OperationCount = statistics.TotalGets + statistics.TotalReturns
                            };
                            
                            healthStatuses[poolTypeName] = healthStatus;
                        }
                        else
                        {
                            healthStatuses[poolTypeName] = StrategyHealthStatus.Degraded(
                                $"Strategy for {poolTypeName} has validation issues", 
                                "Pool validation failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        healthStatuses[poolTypeName] = StrategyHealthStatus.Unhealthy(
                            $"Strategy for {poolTypeName} encountered an error", 
                            new[] { ex.Message }, 
                            ex);
                    }
                }
            }
            
            return healthStatuses;
        }
        
        /// <summary>
        /// Checks if the pooling service is in a healthy state overall.
        /// Performs comprehensive validation of all pools and strategies.
        /// </summary>
        /// <returns>True if all pools and strategies are healthy</returns>
        public bool IsHealthy()
        {
            ThrowIfDisposed();
            
            try
            {
                // Check all pools are valid
                if (!ValidateAllPools())
                    return false;
                
                // Check memory usage is within reasonable bounds
                var healthData = GetNetworkBufferPoolHealthData();
                if (healthData.MemoryUsageBytes > 1024 * 1024 * 1024) // 1GB threshold
                    return false;
                
                // Check strategy health
                var strategyHealthStatuses = GetStrategyHealthStatuses();
                return strategyHealthStatuses.All(kvp => kvp.Value.IsHealthy || !kvp.Value.IsCritical);
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #region Comprehensive Monitoring
        
        /// <summary>
        /// Publishes comprehensive monitoring messages for all pools and strategies.
        /// This method should be called periodically for monitoring and alerting systems.
        /// </summary>
        public void PublishMonitoringData()
        {
            ThrowIfDisposed();
            
            try
            {
                // Publish health status for all strategies
                var strategyHealthStatuses = GetStrategyHealthStatuses();
                foreach (var kvp in strategyHealthStatuses)
                {
                    PublishStrategyHealthStatusMessage(kvp.Key, kvp.Value);
                }
                
                // Publish network buffer pool health data
                var bufferHealthData = GetNetworkBufferPoolHealthData();
                PublishNetworkBufferHealthMessage(bufferHealthData);
                
                // Publish overall service health
                var isServiceHealthy = IsHealthy();
                if (!isServiceHealthy)
                {
                    PublishServiceDegradationMessage();
                }
                
                _loggingService.LogDebug("Published comprehensive monitoring data for pooling service");
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish monitoring data", ex);
            }
        }
        
        /// <summary>
        /// Gets comprehensive monitoring statistics for all pools.
        /// Provides detailed metrics for monitoring dashboards and alerting.
        /// </summary>
        /// <returns>Dictionary of pool statistics with health indicators</returns>
        public Dictionary<string, object> GetMonitoringStatistics()
        {
            ThrowIfDisposed();
            
            var stats = new Dictionary<string, object>();
            
            try
            {
                // Overall service metrics
                stats["TotalPoolsRegistered"] = _pools.Count;
                stats["ServiceHealthy"] = IsHealthy();
                stats["Timestamp"] = DateTime.UtcNow;
                
                // Network buffer health
                var bufferHealth = GetNetworkBufferPoolHealthData();
                stats["NetworkBuffers"] = new
                {
                    TotalBuffers = bufferHealth.TotalBuffersCreated,
                    ActiveBuffers = bufferHealth.ActiveBuffers,
                    MemoryUsageMB = bufferHealth.MemoryUsageMB,
                    ActiveRatio = bufferHealth.ActiveBufferRatio
                };
                
                // Strategy health summary
                var strategyHealths = GetStrategyHealthStatuses();
                stats["StrategyHealth"] = strategyHealths.Select(kvp => new
                {
                    PoolType = kvp.Key,
                    IsHealthy = kvp.Value.IsHealthy,
                    Status = kvp.Value.Status.ToString(),
                    OperationCount = kvp.Value.OperationCount,
                    HasIssues = kvp.Value.HasIssues
                }).ToList();
                
                // Individual pool statistics
                var poolStats = GetAllPoolStatistics();
                stats["PoolStatistics"] = poolStats.Select(kvp => new
                {
                    PoolType = kvp.Key,
                    CurrentSize = kvp.Value.CurrentSize,
                    ActiveObjects = kvp.Value.ActiveObjects,
                    AvailableObjects = kvp.Value.AvailableObjects,
                    TotalGets = kvp.Value.TotalGets,
                    TotalReturns = kvp.Value.TotalReturns,
                    FailedGets = kvp.Value.FailedGets,
                    SuccessRate = kvp.Value.TotalGets > 0 ? (double)(kvp.Value.TotalGets - kvp.Value.FailedGets) / kvp.Value.TotalGets * 100 : 100
                }).ToList();
                
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to gather monitoring statistics", ex);
                stats["Error"] = ex.Message;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Publishes a strategy health status message for monitoring systems.
        /// </summary>
        private void PublishStrategyHealthStatusMessage(string poolTypeName, StrategyHealthStatus healthStatus)
        {
            try
            {
                var successRate = healthStatus.ErrorCount > 0 && healthStatus.OperationCount > 0
                    ? (double)(healthStatus.OperationCount - healthStatus.ErrorCount) / healthStatus.OperationCount * 100
                    : 100.0;
                
                var message = PoolStrategyHealthStatusMessage.Create(
                    strategyName: poolTypeName,
                    isHealthy: healthStatus.IsHealthy,
                    errorCount: (int)healthStatus.ErrorCount,
                    lastHealthCheck: healthStatus.Timestamp,
                    statusMessage: healthStatus.Description,
                    averageOperationDurationMs: healthStatus.AverageOperationTime.TotalMilliseconds,
                    totalOperations: healthStatus.OperationCount,
                    successRatePercentage: successRate,
                    source: "PoolingService"
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to publish strategy health status for {poolTypeName}", ex);
            }
        }
        
        /// <summary>
        /// Publishes network buffer pool health information.
        /// </summary>
        private void PublishNetworkBufferHealthMessage(NetworkBufferPoolHealthData healthData)
        {
            try
            {
                // Check if memory usage is concerning
                if (healthData.MemoryUsageMB > 100) // 100MB threshold
                {
                    var thresholds = new NetworkBufferHealthThresholds();
                    var isWarning = healthData.MemoryUsageBytes > thresholds.WarningMemoryUsageBytes;
                    var isCritical = healthData.MemoryUsageBytes > thresholds.CriticalMemoryUsageBytes;
                    
                    if (isCritical)
                    {
                        _alertService.RaiseAlert(
                            AlertSeverity.Critical,
                            $"Network buffer pool memory usage critical: {healthData.MemoryUsageMB:F1}MB",
                            "PoolingService.NetworkBuffers"
                        );
                    }
                    else if (isWarning)
                    {
                        _alertService.RaiseAlert(
                            AlertSeverity.Warning,
                            $"Network buffer pool memory usage high: {healthData.MemoryUsageMB:F1}MB",
                            "PoolingService.NetworkBuffers"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish network buffer health message", ex);
            }
        }
        
        /// <summary>
        /// Publishes a service degradation message when the service is unhealthy.
        /// </summary>
        private void PublishServiceDegradationMessage()
        {
            try
            {
                _alertService.RaiseAlert(
                    AlertSeverity.Warning,
                    "Pooling service is experiencing degraded performance",
                    "PoolingService"
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish service degradation message", ex);
            }
        }
        
        #endregion
        
        #region Circuit Breaker Integration
        
        /// <summary>
        /// Gets or creates a circuit breaker for pool operations.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <returns>Circuit breaker instance</returns>
        private ICircuitBreaker GetOrCreateCircuitBreaker(string poolTypeName)
        {
            if (_circuitBreakerFactory == null)
                return null;
                
            return _circuitBreakers.GetOrAdd(poolTypeName, typeName =>
            {
                try
                {
                    var circuitBreaker = _circuitBreakerFactory.CreateCircuitBreaker($"Pool_{typeName}");
                    
                    // Subscribe to circuit breaker state changes
                    circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
                    
                    _loggingService.LogInfo($"Created circuit breaker for pool type: {typeName}");
                    return circuitBreaker;
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to create circuit breaker for pool type: {typeName}", ex);
                    return null;
                }
            });
        }
        
        /// <summary>
        /// Handles circuit breaker state changes and publishes appropriate messages.
        /// </summary>
        private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo($"Circuit breaker '{e.Name}' changed state from {e.PreviousState} to {e.NewState}. Reason: {e.Reason}");
                
                // Publish circuit breaker state change message
                var message = PoolCircuitBreakerStateChangedMessage.Create(
                    circuitBreakerName: e.Name.ToString(),
                    previousState: e.PreviousState.ToString(),
                    newState: e.NewState.ToString(),
                    reason: e.Reason ?? "No reason provided",
                    timestamp: DateTime.UtcNow,
                    source: "PoolingService"
                );
                
                _messageBusService.PublishAsync(message).Forget();
                
                // Raise alerts for concerning state changes
                if (e.NewState == CircuitBreakerState.Open)
                {
                    _alertService.RaiseAlert(
                        AlertSeverity.Warning,
                        $"Circuit breaker '{e.Name}' opened due to failures: {e.Reason}",
                        "PoolingService.CircuitBreaker"
                    );
                }
                else if (e.NewState == CircuitBreakerState.Closed && e.PreviousState == CircuitBreakerState.Open)
                {
                    _alertService.RaiseAlert(
                        AlertSeverity.Info,
                        $"Circuit breaker '{e.Name}' recovered and closed",
                        "PoolingService.CircuitBreaker"
                    );
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to handle circuit breaker state change", ex);
            }
        }
        
        /// <summary>
        /// Executes a pool operation with circuit breaker protection and performance monitoring.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation for performance tracking</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="budget">Performance budget to enforce</param>
        /// <returns>Result of the operation</returns>
        private async UniTask<T> ExecuteWithCircuitBreakerAndPerformanceMonitoring<T>(
            string poolTypeName,
            string operationType,
            Func<CancellationToken, UniTask<T>> operation,
            CancellationToken cancellationToken,
            PerformanceBudget budget = null)
        {
            // Wrap the operation with performance monitoring
            return await ExecuteWithPerformanceBudget(poolTypeName, operationType, async () =>
            {
                var circuitBreaker = GetOrCreateCircuitBreaker(poolTypeName);
                
                if (circuitBreaker == null)
                {
                    // No circuit breaker available, execute directly
                    return await operation(cancellationToken);
                }
                
                try
                {
                    // Convert UniTask to Task for circuit breaker compatibility
                    var result = await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        var uniTaskResult = await operation(ct);
                        return uniTaskResult;
                    }, cancellationToken);
                    
                    return result;
                }
                catch (CircuitBreakerOpenException)
                {
                    _loggingService.LogWarning($"Circuit breaker is open for pool type: {poolTypeName}");
                    throw new InvalidOperationException($"Pool operations are currently unavailable for {poolTypeName} due to circuit breaker protection");
                }
            }, budget);
        }
        
        /// <summary>
        /// Executes a pool operation with circuit breaker protection (void return).
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async UniTask ExecuteWithCircuitBreaker(
            string poolTypeName,
            Func<CancellationToken, UniTask> operation,
            CancellationToken cancellationToken)
        {
            var circuitBreaker = GetOrCreateCircuitBreaker(poolTypeName);
            
            if (circuitBreaker == null)
            {
                // No circuit breaker available, execute directly
                await operation(cancellationToken);
                return;
            }
            
            try
            {
                // Convert UniTask to Task for circuit breaker compatibility
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    await operation(ct);
                }, cancellationToken);
            }
            catch (CircuitBreakerOpenException)
            {
                _loggingService.LogWarning($"Circuit breaker is open for pool type: {poolTypeName}");
                throw new InvalidOperationException($"Pool operations are currently unavailable for {poolTypeName} due to circuit breaker protection");
            }
        }
        
        /// <summary>
        /// Gets circuit breaker statistics for monitoring.
        /// </summary>
        /// <returns>Dictionary of circuit breaker statistics by pool type</returns>
        public Dictionary<string, object> GetCircuitBreakerStatistics()
        {
            ThrowIfDisposed();
            
            var stats = new Dictionary<string, object>();
            
            foreach (var kvp in _circuitBreakers)
            {
                try
                {
                    var circuitBreakerStats = kvp.Value.GetStatistics();
                    stats[kvp.Key] = new
                    {
                        State = kvp.Value.State.ToString(),
                        FailureCount = kvp.Value.FailureCount,
                        LastFailureTime = kvp.Value.LastFailureTime,
                        LastStateChangeTime = kvp.Value.LastStateChangeTime,
                        TotalRequests = circuitBreakerStats.TotalRequests,
                        SuccessfulRequests = circuitBreakerStats.SuccessfulRequests,
                        FailedRequests = circuitBreakerStats.FailedRequests,
                        SuccessRate = circuitBreakerStats.SuccessRatePercentage
                    };
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to get statistics for circuit breaker: {kvp.Key}", ex);
                    stats[kvp.Key] = new { Error = ex.Message };
                }
            }
            
            return stats;
        }
        
        #endregion
        
        #region Performance Budget Monitoring
        
        /// <summary>
        /// Internal class to track performance budget violations.
        /// </summary>
        private class PerformanceBudgetTracker
        {
            public PerformanceBudget Budget { get; }
            public long TotalOperations { get; private set; }
            public long BudgetViolations { get; private set; }
            public TimeSpan TotalOperationTime { get; private set; }
            public TimeSpan MaxOperationTime { get; private set; }
            public DateTime LastViolationTime { get; private set; }
            
            public PerformanceBudgetTracker(PerformanceBudget budget)
            {
                Budget = budget ?? throw new ArgumentNullException(nameof(budget));
            }
            
            public void RecordOperation(TimeSpan operationTime, string operationType)
            {
                Interlocked.Increment(ref TotalOperations);
                
                lock (this)
                {
                    TotalOperationTime = TotalOperationTime.Add(operationTime);
                    
                    if (operationTime > MaxOperationTime)
                        MaxOperationTime = operationTime;
                }
                
                // Check if operation exceeded budget
                var budgetLimit = GetBudgetLimitForOperation(operationType);
                if (operationTime > budgetLimit)
                {
                    Interlocked.Increment(ref BudgetViolations);
                    LastViolationTime = DateTime.UtcNow;
                }
            }
            
            private TimeSpan GetBudgetLimitForOperation(string operationType)
            {
                return operationType.ToLowerInvariant() switch
                {
                    "get" or "return" => Budget.MaxOperationTime,
                    "validation" => Budget.MaxValidationTime,
                    "expansion" => Budget.MaxExpansionTime,
                    "contraction" => Budget.MaxContractionTime,
                    _ => Budget.MaxOperationTime
                };
            }
            
            public double GetViolationRate() => TotalOperations > 0 ? (double)BudgetViolations / TotalOperations * 100 : 0;
            
            public TimeSpan GetAverageOperationTime() => TotalOperations > 0 ? 
                new TimeSpan(TotalOperationTime.Ticks / TotalOperations) : TimeSpan.Zero;
        }
        
        /// <summary>
        /// Gets or creates a performance tracker for a pool type.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="budget">Performance budget to use</param>
        /// <returns>Performance tracker instance</returns>
        private PerformanceBudgetTracker GetOrCreatePerformanceTracker(string poolTypeName, PerformanceBudget budget = null)
        {
            return _performanceTrackers.GetOrAdd(poolTypeName, typeName =>
            {
                var effectiveBudget = budget ?? PerformanceBudget.For60FPS(); // Default to 60 FPS budget
                _loggingService.LogInfo($"Created performance tracker for pool type: {typeName} with {effectiveBudget.TargetFrameRate} FPS budget");
                return new PerformanceBudgetTracker(effectiveBudget);
            });
        }
        
        /// <summary>
        /// Executes an operation with performance budget monitoring.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation (get, return, validation, etc.)</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="budget">Performance budget to enforce</param>
        /// <returns>Result of the operation</returns>
        private async UniTask<T> ExecuteWithPerformanceBudget<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            PerformanceBudget budget = null)
        {
            var tracker = GetOrCreatePerformanceTracker(poolTypeName, budget);
            
            if (!tracker.Budget.EnablePerformanceMonitoring)
            {
                // Performance monitoring disabled, execute directly
                return await operation();
            }
            
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var result = await operation();
                
                stopwatch.Stop();
                var operationTime = stopwatch.Elapsed;
                
                // Record performance metrics
                tracker.RecordOperation(operationTime, operationType);
                
                // Check for budget violations and log warnings
                if (tracker.Budget.LogPerformanceWarnings)
                {
                    var budgetLimit = GetBudgetLimitForOperationType(operationType, tracker.Budget);
                    if (operationTime > budgetLimit)
                    {
                        var violationMessage = $"Performance budget violated for {poolTypeName}.{operationType}: " +
                                             $"{operationTime.TotalMilliseconds:F2}ms > {budgetLimit.TotalMilliseconds:F2}ms limit";
                        
                        _loggingService.LogWarning(violationMessage);
                        
                        // Raise alert for significant violations
                        if (operationTime.TotalMilliseconds > budgetLimit.TotalMilliseconds * 2)
                        {
                            _alertService.RaiseAlert(
                                AlertSeverity.Warning,
                                violationMessage,
                                $"PoolingService.PerformanceBudget.{poolTypeName}"
                            );
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Still record the operation time even if it failed
                tracker.RecordOperation(stopwatch.Elapsed, operationType);
                throw;
            }
        }
        
        /// <summary>
        /// Executes an operation with performance budget monitoring (void return).
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="budget">Performance budget to enforce</param>
        private async UniTask ExecuteWithPerformanceBudget(
            string poolTypeName,
            string operationType,
            Func<UniTask> operation,
            PerformanceBudget budget = null)
        {
            await ExecuteWithPerformanceBudget(poolTypeName, operationType, async () =>
            {
                await operation();
                return true; // Dummy return value
            }, budget);
        }
        
        /// <summary>
        /// Gets the budget limit for a specific operation type.
        /// </summary>
        private TimeSpan GetBudgetLimitForOperationType(string operationType, PerformanceBudget budget)
        {
            return operationType.ToLowerInvariant() switch
            {
                "get" or "return" => budget.MaxOperationTime,
                "validation" => budget.MaxValidationTime,
                "expansion" => budget.MaxExpansionTime,
                "contraction" => budget.MaxContractionTime,
                _ => budget.MaxOperationTime
            };
        }
        
        /// <summary>
        /// Gets the performance budget for a specific pool type.
        /// </summary>
        /// <typeparam name="T">Pool type</typeparam>
        /// <returns>Performance budget or null if not configured</returns>
        private PerformanceBudget GetPerformanceBudgetForPool<T>() where T : class, IPooledObject
        {
            try
            {
                if (_pools.TryGetValue(typeof(T), out var poolObj) && poolObj is IObjectPool pool)
                {
                    var configuration = pool.Configuration;
                    return configuration?.PerformanceBudget; // May return null if not configured
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to get performance budget for pool type {typeof(T).Name}", ex);
            }
            
            return null; // Fall back to default budget in performance tracker
        }
        
        /// <summary>
        /// Gets comprehensive performance statistics for all pool types.
        /// </summary>
        /// <returns>Dictionary of performance statistics by pool type</returns>
        public Dictionary<string, object> GetPerformanceStatistics()
        {
            ThrowIfDisposed();
            
            var stats = new Dictionary<string, object>();
            
            foreach (var kvp in _performanceTrackers)
            {
                try
                {
                    var tracker = kvp.Value;
                    stats[kvp.Key] = new
                    {
                        TotalOperations = tracker.TotalOperations,
                        BudgetViolations = tracker.BudgetViolations,
                        ViolationRatePercentage = tracker.GetViolationRate(),
                        AverageOperationTimeMs = tracker.GetAverageOperationTime().TotalMilliseconds,
                        MaxOperationTimeMs = tracker.MaxOperationTime.TotalMilliseconds,
                        LastViolationTime = tracker.LastViolationTime,
                        TargetFrameRate = tracker.Budget.TargetFrameRate,
                        MaxOperationTimeBudgetMs = tracker.Budget.MaxOperationTime.TotalMilliseconds,
                        FrameTimePercentage = tracker.Budget.FrameTimePercentage
                    };
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to get performance statistics for pool type: {kvp.Key}", ex);
                    stats[kvp.Key] = new { Error = ex.Message };
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// Checks if any pools are consistently violating performance budgets.
        /// </summary>
        /// <returns>True if performance is acceptable across all pools</returns>
        public bool IsPerformanceAcceptable()
        {
            ThrowIfDisposed();
            
            foreach (var kvp in _performanceTrackers)
            {
                var tracker = kvp.Value;
                var violationRate = tracker.GetViolationRate();
                
                // Consider performance unacceptable if more than 10% of operations violate the budget
                if (violationRate > 10.0)
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} has high performance budget violation rate: {violationRate:F1}%");
                    return false;
                }
                
                // Check if average operation time is approaching the budget limit
                var averageTime = tracker.GetAverageOperationTime();
                var budgetLimit = tracker.Budget.MaxOperationTime;
                
                if (averageTime.TotalMilliseconds > budgetLimit.TotalMilliseconds * 0.8) // 80% of budget
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} average operation time is approaching budget limit: " +
                                             $"{averageTime.TotalMilliseconds:F2}ms (limit: {budgetLimit.TotalMilliseconds:F2}ms)");
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region Automatic Pool Scaling
        
        /// <summary>
        /// Internal class to track pool scaling metrics.
        /// </summary>
        private class PoolScalingMetrics
        {
            public int ConsecutiveHighUtilization { get; set; }
            public int ConsecutiveLowUtilization { get; set; }
            public DateTime LastScaleUpTime { get; set; }
            public DateTime LastScaleDownTime { get; set; }
            public int CurrentCapacity { get; set; }
            public double AverageUtilization { get; private set; }
            private readonly Queue<double> _utilizationHistory = new(60); // Keep 60 samples
            
            public void RecordUtilization(double utilization)
            {
                _utilizationHistory.Enqueue(utilization);
                if (_utilizationHistory.Count > 60)
                    _utilizationHistory.Dequeue();
                
                AverageUtilization = _utilizationHistory.Average();
            }
        }
        
        private readonly ConcurrentDictionary<string, PoolScalingMetrics> _scalingMetrics = new();
        private Timer _autoScalingTimer;
        
        /// <summary>
        /// Starts automatic pool scaling based on performance metrics.
        /// </summary>
        /// <param name="checkInterval">Interval between scaling checks</param>
        public void StartAutoScaling(TimeSpan checkInterval)
        {
            ThrowIfDisposed();
            
            if (_autoScalingTimer != null)
            {
                _loggingService.LogWarning("Auto-scaling is already running");
                return;
            }
            
            _autoScalingTimer = new Timer(
                callback: _ => PerformAutoScaling(),
                state: null,
                dueTime: checkInterval,
                period: checkInterval
            );
            
            _loggingService.LogInfo($"Started automatic pool scaling with {checkInterval.TotalSeconds}s check interval");
        }
        
        /// <summary>
        /// Stops automatic pool scaling.
        /// </summary>
        public void StopAutoScaling()
        {
            ThrowIfDisposed();
            
            _autoScalingTimer?.Dispose();
            _autoScalingTimer = null;
            
            _loggingService.LogInfo("Stopped automatic pool scaling");
        }
        
        /// <summary>
        /// Performs automatic scaling checks and adjustments for all pools.
        /// </summary>
        private void PerformAutoScaling()
        {
            if (_disposed) return;
            
            try
            {
                foreach (var kvp in _pools)
                {
                    var poolType = kvp.Key;
                    var poolTypeName = poolType.Name;
                    
                    if (kvp.Value is IObjectPool pool)
                    {
                        var metrics = _scalingMetrics.GetOrAdd(poolTypeName, _ => new PoolScalingMetrics
                        {
                            CurrentCapacity = pool.Configuration.InitialCapacity
                        });
                        
                        // Calculate utilization
                        var statistics = pool.GetStatistics();
                        var utilization = statistics.CurrentSize > 0 
                            ? (double)statistics.ActiveObjects / statistics.CurrentSize 
                            : 0;
                        
                        metrics.RecordUtilization(utilization);
                        
                        // Determine scaling action
                        var scalingAction = DetermineScalingAction(poolTypeName, pool, metrics);
                        
                        // Execute scaling if needed
                        if (scalingAction != ScalingAction.None)
                        {
                            ExecutePoolScaling(poolTypeName, pool, metrics, scalingAction).Forget();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Auto-scaling check failed", ex);
            }
        }
        
        /// <summary>
        /// Determines the appropriate scaling action for a pool.
        /// </summary>
        private enum ScalingAction { None, ScaleUp, ScaleDown }
        
        private ScalingAction DetermineScalingAction(string poolTypeName, IObjectPool pool, PoolScalingMetrics metrics)
        {
            var statistics = pool.GetStatistics();
            var config = pool.Configuration;
            
            // Check performance metrics
            var perfTracker = _performanceTrackers.TryGetValue(poolTypeName, out var tracker) ? tracker : null;
            var hasPerformanceIssues = perfTracker?.GetViolationRate() > 5.0; // More than 5% violations
            
            // High utilization threshold (scale up)
            if (metrics.AverageUtilization > 0.8) // 80% utilization
            {
                metrics.ConsecutiveHighUtilization++;
                metrics.ConsecutiveLowUtilization = 0;
                
                // Scale up if:
                // - Consecutive high utilization for 3 checks
                // - Has performance issues
                // - Not at max capacity
                // - Sufficient time since last scale up (cooldown)
                if ((metrics.ConsecutiveHighUtilization >= 3 || hasPerformanceIssues) &&
                    statistics.CurrentSize < config.MaxCapacity &&
                    (DateTime.UtcNow - metrics.LastScaleUpTime).TotalMinutes >= 2)
                {
                    return ScalingAction.ScaleUp;
                }
            }
            // Low utilization threshold (scale down)
            else if (metrics.AverageUtilization < 0.2) // 20% utilization
            {
                metrics.ConsecutiveLowUtilization++;
                metrics.ConsecutiveHighUtilization = 0;
                
                // Scale down if:
                // - Consecutive low utilization for 5 checks
                // - No performance issues
                // - Not at min capacity
                // - Sufficient time since last scale down (cooldown)
                if (metrics.ConsecutiveLowUtilization >= 5 &&
                    !hasPerformanceIssues &&
                    statistics.CurrentSize > config.MinCapacity &&
                    (DateTime.UtcNow - metrics.LastScaleDownTime).TotalMinutes >= 5)
                {
                    return ScalingAction.ScaleDown;
                }
            }
            else
            {
                // Reset consecutive counters for moderate utilization
                metrics.ConsecutiveHighUtilization = Math.Max(0, metrics.ConsecutiveHighUtilization - 1);
                metrics.ConsecutiveLowUtilization = Math.Max(0, metrics.ConsecutiveLowUtilization - 1);
            }
            
            return ScalingAction.None;
        }
        
        /// <summary>
        /// Executes pool scaling operation.
        /// </summary>
        private async UniTask ExecutePoolScaling(string poolTypeName, IObjectPool pool, PoolScalingMetrics metrics, ScalingAction action)
        {
            try
            {
                var config = pool.Configuration;
                var currentSize = pool.GetStatistics().CurrentSize;
                int targetSize;
                
                switch (action)
                {
                    case ScalingAction.ScaleUp:
                        // Calculate scale up size (increase by 50% or min 10 objects)
                        var scaleUpAmount = Math.Max(10, (int)(currentSize * 0.5));
                        targetSize = Math.Min(config.MaxCapacity, currentSize + scaleUpAmount);
                        
                        if (targetSize > currentSize)
                        {
                            _loggingService.LogInfo($"Scaling up pool {poolTypeName} from {currentSize} to {targetSize}");
                            
                            // Pre-warm the pool with new objects
                            await PreWarmPool(poolTypeName, targetSize - currentSize);
                            
                            metrics.LastScaleUpTime = DateTime.UtcNow;
                            metrics.CurrentCapacity = targetSize;
                            
                            // Publish scaling message
                            PublishPoolExpansionMessage(poolTypeName, currentSize, targetSize);
                        }
                        break;
                        
                    case ScalingAction.ScaleDown:
                        // Calculate scale down size (decrease by 30% or min 5 objects)
                        var scaleDownAmount = Math.Max(5, (int)(currentSize * 0.3));
                        targetSize = Math.Max(config.MinCapacity, currentSize - scaleDownAmount);
                        
                        if (targetSize < currentSize)
                        {
                            _loggingService.LogInfo($"Scaling down pool {poolTypeName} from {currentSize} to {targetSize}");
                            
                            // Trim excess objects
                            pool.TrimExcess();
                            
                            metrics.LastScaleDownTime = DateTime.UtcNow;
                            metrics.CurrentCapacity = targetSize;
                            
                            // Publish scaling message
                            PublishPoolContractionMessage(poolTypeName, currentSize, targetSize);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to scale pool {poolTypeName}", ex);
            }
        }
        
        /// <summary>
        /// Pre-warms a pool by creating objects in advance.
        /// </summary>
        private async UniTask PreWarmPool(string poolTypeName, int objectCount)
        {
            try
            {
                _loggingService.LogDebug($"Pre-warming pool {poolTypeName} with {objectCount} objects");
                
                // Get the pool type from the registered pools
                var poolType = _pools.Keys.FirstOrDefault(t => t.Name == poolTypeName);
                if (poolType == null)
                {
                    _loggingService.LogWarning($"Cannot pre-warm pool {poolTypeName}: type not found");
                    return;
                }
                
                // Use reflection to call the generic GetAsync method
                var getMethod = GetType().GetMethod(nameof(GetAsync), new[] { typeof(CancellationToken) });
                if (getMethod == null) return;
                
                var genericMethod = getMethod.MakeGenericMethod(poolType);
                var returnMethod = GetType().GetMethod(nameof(ReturnAsync), new[] { poolType, typeof(CancellationToken) });
                if (returnMethod == null) return;
                
                var genericReturnMethod = returnMethod.MakeGenericMethod(poolType);
                
                // Pre-warm objects
                var preWarmedObjects = new List<object>(objectCount);
                
                for (int i = 0; i < objectCount; i++)
                {
                    try
                    {
                        var task = (UniTask<object>)genericMethod.Invoke(this, new object[] { CancellationToken.None });
                        var obj = await task;
                        preWarmedObjects.Add(obj);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Failed to pre-warm object {i} for pool {poolTypeName}", ex);
                        break;
                    }
                }
                
                // Return all pre-warmed objects to the pool
                foreach (var obj in preWarmedObjects)
                {
                    try
                    {
                        var task = (UniTask)genericReturnMethod.Invoke(this, new[] { obj, CancellationToken.None });
                        await task;
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Failed to return pre-warmed object to pool {poolTypeName}", ex);
                    }
                }
                
                _loggingService.LogDebug($"Pre-warmed {preWarmedObjects.Count} objects for pool {poolTypeName}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Pre-warming failed for pool {poolTypeName}", ex);
            }
        }
        
        /// <summary>
        /// Publishes a pool expansion message.
        /// </summary>
        private void PublishPoolExpansionMessage(string poolTypeName, int previousSize, int newSize)
        {
            try
            {
                var message = PoolExpansionMessage.Create(
                    poolName: poolTypeName,
                    poolTypeName: poolTypeName,
                    previousCapacity: previousSize,
                    newCapacity: newSize,
                    reason: "Automatic scaling due to high utilization",
                    timestamp: DateTime.UtcNow,
                    source: "PoolingService.AutoScaling"
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool expansion message", ex);
            }
        }
        
        /// <summary>
        /// Publishes a pool contraction message.
        /// </summary>
        private void PublishPoolContractionMessage(string poolTypeName, int previousSize, int newSize)
        {
            try
            {
                var message = PoolContractionMessage.Create(
                    poolName: poolTypeName,
                    poolTypeName: poolTypeName,
                    previousCapacity: previousSize,
                    newCapacity: newSize,
                    reason: "Automatic scaling due to low utilization",
                    timestamp: DateTime.UtcNow,
                    source: "PoolingService.AutoScaling"
                );
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool contraction message", ex);
            }
        }
        
        /// <summary>
        /// Gets automatic scaling statistics for monitoring.
        /// </summary>
        /// <returns>Dictionary of scaling statistics by pool type</returns>
        public Dictionary<string, object> GetAutoScalingStatistics()
        {
            ThrowIfDisposed();
            
            var stats = new Dictionary<string, object>();
            
            foreach (var kvp in _scalingMetrics)
            {
                var metrics = kvp.Value;
                stats[kvp.Key] = new
                {
                    AverageUtilization = metrics.AverageUtilization,
                    ConsecutiveHighUtilization = metrics.ConsecutiveHighUtilization,
                    ConsecutiveLowUtilization = metrics.ConsecutiveLowUtilization,
                    LastScaleUpTime = metrics.LastScaleUpTime,
                    LastScaleDownTime = metrics.LastScaleDownTime,
                    CurrentCapacity = metrics.CurrentCapacity
                };
            }
            
            return stats;
        }
        
        #endregion
        
        #region Error Handling and Recovery
        
        /// <summary>
        /// Internal class to track error recovery metrics.
        /// </summary>
        private class ErrorRecoveryMetrics
        {
            public int ConsecutiveFailures { get; set; }
            public int TotalRecoveryAttempts { get; set; }
            public int SuccessfulRecoveries { get; set; }
            public DateTime LastFailureTime { get; set; }
            public DateTime LastRecoveryAttempt { get; set; }
            public Exception LastException { get; set; }
            public bool IsInRecoveryMode { get; set; }
            
            public double RecoverySuccessRate => TotalRecoveryAttempts > 0 
                ? (double)SuccessfulRecoveries / TotalRecoveryAttempts * 100 
                : 0;
        }
        
        private readonly ConcurrentDictionary<string, ErrorRecoveryMetrics> _recoveryMetrics = new();
        
        /// <summary>
        /// Executes an operation with comprehensive error handling and automatic recovery.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>Result of the operation</returns>
        private async UniTask<T> ExecuteWithErrorHandling<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            int maxRetries = 3)
        {
            var metrics = _recoveryMetrics.GetOrAdd(poolTypeName, _ => new ErrorRecoveryMetrics());
            var attempt = 0;
            
            while (attempt <= maxRetries)
            {
                try
                {
                    var result = await operation();
                    
                    // Success - reset failure tracking
                    if (metrics.ConsecutiveFailures > 0)
                    {
                        _loggingService.LogInfo($"Pool {poolTypeName} operation {operationType} recovered after {metrics.ConsecutiveFailures} failures");
                        metrics.ConsecutiveFailures = 0;
                        metrics.IsInRecoveryMode = false;
                        
                        // Publish recovery message if this was a significant recovery
                        if (metrics.ConsecutiveFailures >= 3)
                        {
                            PublishPoolRecoveryMessage(poolTypeName, operationType);
                        }
                    }
                    
                    return result;
                }
                catch (Exception ex) when (IsRecoverableException(ex) && attempt < maxRetries)
                {
                    attempt++;
                    metrics.ConsecutiveFailures++;
                    metrics.LastFailureTime = DateTime.UtcNow;
                    metrics.LastException = ex;
                    
                    _loggingService.LogWarning($"Pool {poolTypeName} operation {operationType} failed (attempt {attempt}/{maxRetries + 1}): {ex.Message}");
                    
                    // Calculate backoff delay
                    var backoffDelay = CalculateBackoffDelay(attempt);
                    
                    // Attempt recovery if in recovery mode
                    if (metrics.ConsecutiveFailures >= 3 && !metrics.IsInRecoveryMode)
                    {
                        metrics.IsInRecoveryMode = true;
                        await AttemptPoolRecovery(poolTypeName, ex);
                    }
                    
                    // Wait before retry
                    if (backoffDelay > TimeSpan.Zero)
                    {
                        await UniTask.Delay(backoffDelay);
                    }
                }
                catch (Exception ex)
                {
                    // Non-recoverable exception or max retries exceeded
                    metrics.ConsecutiveFailures++;
                    metrics.LastFailureTime = DateTime.UtcNow;
                    metrics.LastException = ex;
                    
                    _loggingService.LogError($"Pool {poolTypeName} operation {operationType} failed permanently after {attempt} attempts: {ex.Message}");
                    
                    // Raise critical alert for permanent failures
                    _alertService.RaiseAlert(
                        AlertSeverity.Critical,
                        $"Pool {poolTypeName} operation {operationType} failed permanently: {ex.Message}",
                        $"PoolingService.ErrorRecovery.{poolTypeName}"
                    );
                    
                    // Attempt emergency recovery for critical failures
                    if (metrics.ConsecutiveFailures >= 5)
                    {
                        _ = EmergencyPoolRecovery(poolTypeName).Forget();
                    }
                    
                    throw new PoolOperationFailedException(poolTypeName, operationType, ex);
                }
            }
            
            // This should never be reached due to the exception handling above
            throw new InvalidOperationException("Unexpected error in retry logic");
        }
        
        /// <summary>
        /// Determines if an exception is recoverable through retry logic.
        /// </summary>
        private static bool IsRecoverableException(Exception ex)
        {
            return ex switch
            {
                // Transient exceptions that can be retried
                TimeoutException => true,
                TaskCanceledException => true,
                InvalidOperationException when ex.Message.Contains("pool") => true,
                
                // Non-recoverable exceptions
                ArgumentNullException => false,
                ArgumentException => false,
                NotSupportedException => false,
                OutOfMemoryException => false,
                
                // Default to recoverable for unknown exceptions
                _ => true
            };
        }
        
        /// <summary>
        /// Calculates exponential backoff delay for retry attempts.
        /// </summary>
        private static TimeSpan CalculateBackoffDelay(int attempt)
        {
            if (attempt <= 1) return TimeSpan.Zero;
            
            // Exponential backoff with jitter: base_delay * (2^attempt) + random_jitter
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
            
            // Add jitter to prevent thundering herd
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
            
            // Cap maximum delay at 5 seconds
            var totalDelay = exponentialDelay + jitter;
            return totalDelay > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : totalDelay;
        }
        
        /// <summary>
        /// Attempts to recover a pool from failures.
        /// </summary>
        private async UniTask AttemptPoolRecovery(string poolTypeName, Exception lastException)
        {
            var metrics = _recoveryMetrics.GetOrAdd(poolTypeName, _ => new ErrorRecoveryMetrics());
            metrics.TotalRecoveryAttempts++;
            metrics.LastRecoveryAttempt = DateTime.UtcNow;
            
            try
            {
                _loggingService.LogInfo($"Attempting recovery for pool {poolTypeName}");
                
                // Find the pool
                var poolType = _pools.Keys.FirstOrDefault(t => t.Name == poolTypeName);
                if (poolType == null)
                {
                    _loggingService.LogError($"Cannot recover pool {poolTypeName}: type not found");
                    return;
                }
                
                if (!_pools.TryGetValue(poolType, out var poolObj) || poolObj is not IObjectPool pool)
                {
                    _loggingService.LogError($"Cannot recover pool {poolTypeName}: pool object not found");
                    return;
                }
                
                // Step 1: Validate the pool
                var isValid = await ValidatePoolAsync(pool);
                if (!isValid)
                {
                    _loggingService.LogWarning($"Pool {poolTypeName} validation failed during recovery");
                }
                
                // Step 2: Clear potentially corrupted objects
                try
                {
                    pool.Clear();
                    _loggingService.LogInfo($"Cleared pool {poolTypeName} during recovery");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to clear pool {poolTypeName} during recovery", ex);
                }
                
                // Step 3: Reset circuit breaker if it exists
                if (_circuitBreakers.TryGetValue(poolTypeName, out var circuitBreaker))
                {
                    circuitBreaker.Reset("Pool recovery operation");
                    _loggingService.LogInfo($"Reset circuit breaker for pool {poolTypeName}");
                }
                
                // Step 4: Pre-warm with a few objects to test functionality
                var config = pool.Configuration;
                var preWarmCount = Math.Min(5, config.InitialCapacity / 4); // Pre-warm 25% or 5 objects, whichever is smaller
                
                if (preWarmCount > 0)
                {
                    await PreWarmPool(poolTypeName, preWarmCount);
                    _loggingService.LogInfo($"Pre-warmed {preWarmCount} objects for pool {poolTypeName} during recovery");
                }
                
                metrics.SuccessfulRecoveries++;
                _loggingService.LogInfo($"Successfully recovered pool {poolTypeName}");
                
                // Publish recovery message
                PublishPoolRecoveryMessage(poolTypeName, "recovery");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Recovery failed for pool {poolTypeName}", ex);
                
                // If recovery fails, consider emergency recovery
                if (metrics.TotalRecoveryAttempts >= 3)
                {
                    _ = EmergencyPoolRecovery(poolTypeName).Forget();
                }
            }
        }
        
        /// <summary>
        /// Performs emergency recovery by recreating the pool entirely.
        /// </summary>
        private async UniTask EmergencyPoolRecovery(string poolTypeName)
        {
            try
            {
                _loggingService.LogCritical($"Performing emergency recovery for pool {poolTypeName}");
                
                // Find the pool type
                var poolType = _pools.Keys.FirstOrDefault(t => t.Name == poolTypeName);
                if (poolType == null)
                {
                    _loggingService.LogError($"Cannot perform emergency recovery: pool type {poolTypeName} not found");
                    return;
                }
                
                // Get the old pool configuration
                IObjectPool oldPool = null;
                if (_pools.TryGetValue(poolType, out var poolObj) && poolObj is IObjectPool pool)
                {
                    oldPool = pool;
                }
                
                // Dispose the old pool
                if (oldPool is IDisposable disposablePool)
                {
                    disposablePool.Dispose();
                }
                
                // Remove from pools collection
                _pools.TryRemove(poolType, out _);
                
                // Re-register the pool with default configuration
                var defaultConfig = new PoolConfiguration
                {
                    InitialCapacity = 10,
                    MaxCapacity = 100,
                    MinCapacity = 5,
                    PerformanceBudget = PerformanceBudget.For60FPS()
                };
                
                // Use reflection to call the generic RegisterPool method
                var registerMethod = GetType().GetMethod(nameof(RegisterPool), new[] { typeof(PoolConfiguration) });
                if (registerMethod != null)
                {
                    var genericMethod = registerMethod.MakeGenericMethod(poolType);
                    genericMethod.Invoke(this, new object[] { defaultConfig });
                    
                    _loggingService.LogInfo($"Emergency recovery completed for pool {poolTypeName}");
                    
                    // Reset recovery metrics
                    var metrics = _recoveryMetrics.GetOrAdd(poolTypeName, _ => new ErrorRecoveryMetrics());
                    metrics.ConsecutiveFailures = 0;
                    metrics.IsInRecoveryMode = false;
                    
                    // Publish emergency recovery message
                    _alertService.RaiseAlert(
                        AlertSeverity.Warning,
                        $"Emergency recovery completed for pool {poolTypeName}",
                        $"PoolingService.EmergencyRecovery.{poolTypeName}"
                    );
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Emergency recovery failed for pool {poolTypeName}", ex);
                
                // Last resort: alert that the pool is completely non-functional
                _alertService.RaiseAlert(
                    AlertSeverity.Critical,
                    $"Pool {poolTypeName} is completely non-functional and requires manual intervention",
                    $"PoolingService.EmergencyRecovery.{poolTypeName}"
                );
            }
        }
        
        /// <summary>
        /// Validates a pool asynchronously.
        /// </summary>
        private async UniTask<bool> ValidatePoolAsync(IObjectPool pool)
        {
            try
            {
                return await UniTask.RunOnThreadPool(() => pool.Validate(), true);
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Pool validation failed", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Publishes a pool recovery message.
        /// </summary>
        private void PublishPoolRecoveryMessage(string poolTypeName, string operationType)
        {
            try
            {
                var message = new PoolOperationCompletedMessage
                {
                    Id = Guid.NewGuid(),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    Source = "PoolingService.Recovery",
                    Priority = MessagePriority.Normal,
                    PoolName = poolTypeName,
                    OperationType = $"{operationType}_recovery",
                    Duration = TimeSpan.Zero,
                    Success = true
                };
                
                _messageBusService.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool recovery message", ex);
            }
        }
        
        /// <summary>
        /// Gets comprehensive error handling and recovery statistics.
        /// </summary>
        /// <returns>Dictionary of error recovery statistics by pool type</returns>
        public Dictionary<string, object> GetErrorRecoveryStatistics()
        {
            ThrowIfDisposed();
            
            var stats = new Dictionary<string, object>();
            
            foreach (var kvp in _recoveryMetrics)
            {
                var metrics = kvp.Value;
                stats[kvp.Key] = new
                {
                    ConsecutiveFailures = metrics.ConsecutiveFailures,
                    TotalRecoveryAttempts = metrics.TotalRecoveryAttempts,
                    SuccessfulRecoveries = metrics.SuccessfulRecoveries,
                    RecoverySuccessRate = metrics.RecoverySuccessRate,
                    LastFailureTime = metrics.LastFailureTime,
                    LastRecoveryAttempt = metrics.LastRecoveryAttempt,
                    IsInRecoveryMode = metrics.IsInRecoveryMode,
                    LastExceptionMessage = metrics.LastException?.Message
                };
            }
            
            return stats;
        }
        
        /// <summary>
        /// Forces recovery for a specific pool type.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type to recover</param>
        public async UniTask ForcePoolRecovery(string poolTypeName)
        {
            ThrowIfDisposed();
            
            _loggingService.LogInfo($"Forcing recovery for pool {poolTypeName}");
            await AttemptPoolRecovery(poolTypeName, new InvalidOperationException("Manual recovery requested"));
        }
        
        /// <summary>
        /// Checks the overall health of the error recovery system.
        /// </summary>
        /// <returns>True if the recovery system is functioning well</returns>
        public bool IsRecoverySystemHealthy()
        {
            ThrowIfDisposed();
            
            foreach (var kvp in _recoveryMetrics)
            {
                var metrics = kvp.Value;
                
                // Consider unhealthy if:
                // - More than 10 consecutive failures
                // - Recovery success rate below 50%
                // - Currently in recovery mode for more than 10 minutes
                if (metrics.ConsecutiveFailures > 10)
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} has {metrics.ConsecutiveFailures} consecutive failures");
                    return false;
                }
                
                if (metrics.TotalRecoveryAttempts > 0 && metrics.RecoverySuccessRate < 50)
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} has low recovery success rate: {metrics.RecoverySuccessRate:F1}%");
                    return false;
                }
                
                if (metrics.IsInRecoveryMode && 
                    (DateTime.UtcNow - metrics.LastRecoveryAttempt).TotalMinutes > 10)
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} has been in recovery mode for too long");
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        /// <summary>
        /// Custom exception for pool operation failures.
        /// </summary>
        public class PoolOperationFailedException : Exception
        {
            public string PoolTypeName { get; }
            public string OperationType { get; }
            
            public PoolOperationFailedException(string poolTypeName, string operationType, Exception innerException)
                : base($"Pool operation failed for {poolTypeName}.{operationType}: {innerException.Message}", innerException)
            {
                PoolTypeName = poolTypeName;
                OperationType = operationType;
            }
        }

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
                
                // Stop auto-scaling if running
                _autoScalingTimer?.Dispose();
                _autoScalingTimer = null;
                
                // Dispose circuit breakers
                foreach (var circuitBreaker in _circuitBreakers.Values)
                {
                    if (circuitBreaker is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                _pools.Clear();
                _circuitBreakers.Clear();
                _scalingMetrics.Clear();
                _performanceTrackers.Clear();
                _recoveryMetrics.Clear();
                _disposed = true;
            }
        }
    }
}