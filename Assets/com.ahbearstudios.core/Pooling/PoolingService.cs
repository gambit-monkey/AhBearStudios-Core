using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.HealthChecks;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Primary pooling service implementation following Builder → Config → Factory → Service pattern.
    /// Orchestrates pool operations by delegating to specialized services for maintainability.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// Simplified architecture with proper separation of concerns.
    /// </summary>
    public class PoolingService : IPoolingService, IDisposable
    {
        #region Private Fields
        
        private readonly PoolingServiceConfiguration _configuration;
        private readonly IPoolRegistry _poolRegistry;
        private readonly IPoolCreationService _poolCreationService;
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;
        
        // Specialized services for complex operations
        private readonly IPoolAutoScalingService _autoScalingService;
        private readonly IPoolErrorRecoveryService _errorRecoveryService;
        private readonly IPoolPerformanceMonitorService _performanceMonitorService;
        
        private readonly ProfilerMarker _getMarker;
        private readonly ProfilerMarker _returnMarker;
        private readonly ProfilerMarker _registerMarker;
        private bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PoolingService.
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        /// <param name="poolRegistry">Pool registry for managing pool storage</param>
        /// <param name="poolCreationService">Service for creating pool instances</param>
        /// <param name="loggingService">Logging service for pool operations</param>
        /// <param name="messageBusService">Message bus service for publishing pool events</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="autoScalingService">Auto-scaling service for pool size management</param>
        /// <param name="errorRecoveryService">Error recovery service for pool resilience</param>
        /// <param name="performanceMonitorService">Performance monitoring service for budget enforcement</param>
        /// <param name="healthCheckService">Health check service for system health monitoring</param>
        /// <param name="circuitBreakerFactory">Factory for creating circuit breakers to protect pool operations</param>
        public PoolingService(
            PoolingServiceConfiguration configuration,
            IPoolRegistry poolRegistry,
            IPoolCreationService poolCreationService,
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolAutoScalingService autoScalingService = null,
            IPoolErrorRecoveryService errorRecoveryService = null,
            IPoolPerformanceMonitorService performanceMonitorService = null,
            IHealthCheckService healthCheckService = null,
            ICircuitBreakerFactory circuitBreakerFactory = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _poolRegistry = poolRegistry ?? throw new ArgumentNullException(nameof(poolRegistry));
            _poolCreationService = poolCreationService ?? throw new ArgumentNullException(nameof(poolCreationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _alertService = alertService;
            _profilerService = profilerService;
            _autoScalingService = autoScalingService;
            _errorRecoveryService = errorRecoveryService;
            _performanceMonitorService = performanceMonitorService;
            _healthCheckService = healthCheckService;
            _circuitBreakerFactory = circuitBreakerFactory;
            
            // Initialize profiler markers
            _getMarker = new ProfilerMarker("PoolingService.Get");
            _returnMarker = new ProfilerMarker("PoolingService.Return");
            _registerMarker = new ProfilerMarker("PoolingService.Register");
            
            // Subscribe to circuit breaker state change messages from HealthChecking system
            if (_configuration.EnableCircuitBreaker)
            {
                _messageBusService.SubscribeToMessage<HealthCheckCircuitBreakerStateChangedMessage>(OnHealthCheckCircuitBreakerStateChanged);
            }
            
            _loggingService.LogInfo($"PoolingService initialized: {_configuration.ServiceName}");
        }
        
        #endregion
        
        #region Core Pool Operations
        
        /// <inheritdoc />
        public T Get<T>() where T : class, IPooledObject, new()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();
                
                var pool = _poolRegistry.GetPool<T>();
                if (pool == null)
                {
                    throw new InvalidOperationException($"No pool registered for type {typeof(T).Name}. Call RegisterPool<T>() first.");
                }
                
                var item = pool.Get();
                
                // Publish pool object retrieved message
                PublishObjectRetrievedMessage(item, pool);
                
                return item;
            }
        }
        
        /// <inheritdoc />
        public void Return<T>(T item) where T : class, IPooledObject, new()
        {
            using (_returnMarker.Auto())
            {
                ThrowIfDisposed();
                
                if (item == null) return;
                
                var pool = _poolRegistry.GetPool<T>();
                if (pool == null)
                {
                    // If no pool is registered, dispose if possible
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    return;
                }
                
                pool.Return(item);
                
                // Publish pool object returned message
                PublishObjectReturnedMessage(item, pool);
            }
        }
        
        /// <inheritdoc />
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
                    // Delegate to specialized services if available
                    var getOperation = async () =>
                    {
                        // Create a timeout token that combines with the provided cancellation token
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(timeout);
                        
                        // Use UniTask.RunOnThreadPool for potentially blocking operations
                        return await UniTask.RunOnThreadPool(() =>
                        {
                            timeoutCts.Token.ThrowIfCancellationRequested();
                            
                            var pool = _poolRegistry.GetPool<T>();
                            if (pool == null)
                            {
                                throw new InvalidOperationException($"No pool registered for type {poolTypeName}. Call RegisterPool<T>() first.");
                            }
                            
                            var item = pool.Get();
                            
                            // Publish pool object retrieved message
                            PublishObjectRetrievedMessage(item, pool);
                            
                            return item;
                        }, true, timeoutCts.Token);
                    };
                    
                    // Apply performance monitoring if available
                    if (_performanceMonitorService != null)
                    {
                        var result = default(T);
                        await _performanceMonitorService.ExecuteWithPerformanceBudget(
                            poolTypeName,
                            "get",
                            async () => { result = await getOperation(); },
                            _configuration.DefaultPerformanceBudget);
                        return result;
                    }
                    
                    // Apply error recovery if available
                    if (_errorRecoveryService != null)
                    {
                        var result = default(T);
                        await _errorRecoveryService.ExecuteWithErrorHandling(
                            poolTypeName,
                            "get",
                            async () => { result = await getOperation(); },
                            _configuration.MaxRetryAttempts);
                        return result;
                    }
                    
                    // Execute directly if no specialized services
                    return await getOperation();
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
        
        /// <inheritdoc />
        public async UniTask ReturnAsync<T>(T item) where T : class, IPooledObject, new()
        {
            await ReturnAsync(item, CancellationToken.None);
        }
        
        /// <summary>
        /// Returns an object to its pool asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public async UniTask ReturnAsync<T>(T item, CancellationToken cancellationToken) where T : class, IPooledObject, new()
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
                    var returnOperation = async () =>
                    {
                        // Use UniTask.RunOnThreadPool for potentially blocking validation operations
                        await UniTask.RunOnThreadPool(() =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            var pool = _poolRegistry.GetPool<T>();
                            if (pool == null)
                            {
                                // If no pool is registered, dispose if possible
                                if (item is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                                return;
                            }
                            
                            pool.Return(item);
                            
                            // Publish pool object returned message
                            PublishObjectReturnedMessage(item, pool);
                        }, true, cancellationToken);
                    };
                    
                    // Apply error recovery if available
                    if (_errorRecoveryService != null)
                    {
                        await _errorRecoveryService.ExecuteWithErrorHandling(
                            poolTypeName,
                            "return",
                            async () => await returnOperation(),
                            _configuration.MaxRetryAttempts);
                    }
                    else
                    {
                        await returnOperation();
                    }
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
        
        #endregion
        
        #region Pool Registration
        
        /// <inheritdoc />
        public void RegisterPool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            using (_registerMarker.Auto())
            {
                ThrowIfDisposed();
                
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));
                
                var poolTypeName = typeof(T).Name;
                
                if (_poolRegistry.IsPoolRegistered<T>())
                {
                    throw new InvalidOperationException($"Pool for type {poolTypeName} is already registered.");
                }
                
                // Create pool using the creation service
                var pool = _poolCreationService.CreatePool<T>(configuration);
                
                // Register with the pool registry
                if (!_poolRegistry.RegisterPool(pool))
                {
                    pool.Dispose();
                    throw new InvalidOperationException($"Failed to register pool for type {poolTypeName}.");
                }
                
                // Register pool with specialized services if available
                _autoScalingService?.RegisterPool(poolTypeName, pool);
                _errorRecoveryService?.RegisterPool(poolTypeName, pool);
                _performanceMonitorService?.RegisterPool(poolTypeName, pool, configuration.PerformanceBudget);
                
                _loggingService.LogInfo($"Successfully registered pool for type {poolTypeName}");
            }
        }
        
        /// <inheritdoc />
        public void RegisterPool<T>(string poolName = null) where T : class, IPooledObject, new()
        {
            var pool = _poolCreationService.CreatePool<T>(poolName);
            
            if (!_poolRegistry.RegisterPool(pool))
            {
                pool.Dispose();
                throw new InvalidOperationException($"Failed to register pool for type {typeof(T).Name}.");
            }
            
            var typeName = typeof(T).Name;
            _autoScalingService?.RegisterPool(typeName, pool);
            _errorRecoveryService?.RegisterPool(typeName, pool);
            _performanceMonitorService?.RegisterPool(typeName, pool, _configuration.DefaultPerformanceBudget);
            
            _loggingService.LogInfo($"Successfully registered pool for type {typeName}");
        }
        
        /// <inheritdoc />
        public void UnregisterPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var poolTypeName = typeof(T).Name;
            
            if (_poolRegistry.UnregisterPool<T>())
            {
                // Unregister from specialized services
                _autoScalingService?.UnregisterPool(poolTypeName);
                _errorRecoveryService?.UnregisterPool(poolTypeName);
                _performanceMonitorService?.UnregisterPool(poolTypeName);
                
                _loggingService.LogInfo($"Successfully unregistered pool for type {poolTypeName}");
            }
        }
        
        /// <inheritdoc />
        public bool IsPoolRegistered<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return _poolRegistry.IsPoolRegistered<T>();
        }
        
        #endregion
        
        #region Statistics and Monitoring
        
        /// <inheritdoc />
        public Dictionary<string, PoolStatistics> GetAllPoolStatistics()
        {
            ThrowIfDisposed();
            return _poolRegistry.GetAllPoolStatistics();
        }
        
        /// <inheritdoc />
        public PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return _poolRegistry.GetPoolStatistics<T>();
        }
        
        /// <inheritdoc />
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            
            var result = _poolRegistry.ValidateAllPools();
            
            if (!result)
            {
                var statistics = _poolRegistry.GetAllPoolStatistics();
                var totalIssues = statistics.Count(s => s.Value.FailedGets > 0);
                PublishValidationIssuesMessage("AllPools", null, totalIssues);
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public bool ValidatePool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var result = _poolRegistry.ValidatePool<T>();
            
            if (!result)
            {
                var pool = _poolRegistry.GetPool<T>();
                PublishValidationIssuesMessage(typeof(T).Name, pool, 1);
            }
            
            return result;
        }
        
        #endregion
        
        #region Maintenance
        
        /// <inheritdoc />
        public void ClearAllPools()
        {
            ThrowIfDisposed();
            _poolRegistry.ClearAllPools();
        }
        
        /// <inheritdoc />
        public void ClearPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            _poolRegistry.ClearPool<T>();
        }
        
        /// <inheritdoc />
        public void TrimAllPools()
        {
            ThrowIfDisposed();
            _poolRegistry.TrimAllPools();
        }
        
        /// <inheritdoc />
        public void TrimPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            _poolRegistry.TrimPool<T>();
        }
        
        #endregion
        
        #region Configuration and Builder Integration
        
        /// <summary>
        /// Creates a network pooling configuration using the builder pattern.
        /// </summary>
        /// <returns>Network pooling configuration builder</returns>
        public INetworkPoolingConfigBuilder CreateNetworkPoolingConfig()
        {
            ThrowIfDisposed();
            return new NetworkPoolingConfigBuilder(new PooledNetworkBufferFactory());
        }
        
        #endregion
        
        #region Specialized Service Access
        
        /// <summary>
        /// Gets the auto-scaling service for advanced pool size management.
        /// </summary>
        public IPoolAutoScalingService AutoScalingService => _autoScalingService;
        
        /// <summary>
        /// Gets the error recovery service for advanced error handling.
        /// </summary>
        public IPoolErrorRecoveryService ErrorRecoveryService => _errorRecoveryService;
        
        /// <summary>
        /// Gets the performance monitor service for advanced performance tracking.
        /// </summary>
        public IPoolPerformanceMonitorService PerformanceMonitorService => _performanceMonitorService;
        
        #endregion
        
        #region Message Bus Integration
        
        /// <inheritdoc />
        public IMessageBusService MessageBus => _messageBusService;
        
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
        /// Validates a pooled object using basic validation.
        /// </summary>
        /// <param name="pooledObject">Object to validate</param>
        /// <returns>True if the object is valid for use</returns>
        public bool ValidatePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return false;
            
            return pooledObject.IsValid();
        }
        
        /// <summary>
        /// Resets a pooled object to its initial state.
        /// </summary>
        /// <param name="pooledObject">Object to reset</param>
        public void ResetPooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return;
            
            pooledObject.Reset();
        }
        
        /// <summary>
        /// Determines if a pooled object should be disposed based on its health.
        /// </summary>
        /// <param name="pooledObject">Object to check</param>
        /// <returns>True if the object should be disposed</returns>
        public bool ShouldDisposePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null)
                return true;
            
            return !pooledObject.CanBePooled() || pooledObject.CorruptionDetected;
        }
        
        #endregion
        
        #region Private Implementation
        
        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolingService));
        }
        
        
        
        
        
        
        /// <summary>
        /// Handles circuit breaker state change messages from HealthChecking system.
        /// This will be activated when HealthChecking publishes HealthCheckCircuitBreakerStateChangedMessage.
        /// </summary>
        /// <param name="message">The circuit breaker state change message from HealthChecking</param>
        private void OnHealthCheckCircuitBreakerStateChanged(HealthCheckCircuitBreakerStateChangedMessage message)
        {
            try
            {
                // Extract circuit breaker information from the strongly-typed message
                string circuitBreakerName = message.CircuitBreakerName.ToString();
                string oldState = message.OldState.ToString();
                string newState = message.NewState.ToString();
                string reason = message.Reason ?? "State change";
                int consecutiveFailures = message.ConsecutiveFailures;
                long totalActivations = message.TotalActivations;
                
                _loggingService.LogInfo($"Circuit breaker '{circuitBreakerName}' changed state from {oldState} to {newState}. Reason: {reason}");
                
                // Re-publish as PoolCircuitBreakerStateChangedMessage for pool-specific handling
                var poolMessage = PoolCircuitBreakerStateChangedMessage.Create(
                    strategyName: circuitBreakerName,
                    oldState: oldState,
                    newState: newState,
                    consecutiveFailures: consecutiveFailures,
                    totalActivations: (int)Math.Min(totalActivations, int.MaxValue),
                    source: "PoolingService"
                );
                
                _messageBusService.PublishMessageAsync(poolMessage).Forget();
                
                // Raise alerts for concerning state changes using proper enum comparison
                if (message.NewState == CircuitBreakerState.Open)
                {
                    _alertService.RaiseAlert(
                        $"Circuit breaker '{circuitBreakerName}' opened due to failures: {reason}",
                        AlertSeverity.Warning,
                        "PoolingService.CircuitBreaker"
                    );
                }
                else if (message.NewState == CircuitBreakerState.Closed && message.OldState == CircuitBreakerState.Open)
                {
                    _alertService.RaiseAlert(
                        $"Circuit breaker '{circuitBreakerName}' recovered and closed",
                        AlertSeverity.Info,
                        "PoolingService.CircuitBreaker"
                    );
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to handle HealthCheck circuit breaker state change message", ex);
            }
        }
        
        
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
                
                _messageBusService.PublishMessageAsync(message).Forget();
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
                
                _messageBusService.PublishMessageAsync(message).Forget();
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
                
                _messageBusService.PublishMessageAsync(message).Forget();
            }
            catch
            {
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the pooling service and all registered pools.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose pool registry (which disposes all pools)
                _poolRegistry?.Dispose();
                
                // Dispose specialized services
                _autoScalingService?.Dispose();
                _errorRecoveryService?.Dispose();
                _performanceMonitorService?.Dispose();
                
                _disposed = true;
                _loggingService.LogInfo($"PoolingService disposed: {_configuration.ServiceName}");
            }
        }
        
        #endregion
    }
}