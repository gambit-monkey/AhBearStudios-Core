using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Primary pooling service implementation following Builder → Config → Factory → Service pattern.
    /// Orchestrates pool operations by delegating to specialized services for maintainability.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// Refactored architecture with proper separation of concerns following CLAUDE.md guidelines.
    /// </summary>
    public sealed class PoolingService : IPoolingService, IDisposable
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
        private readonly ISerializationService _serializationService;
        
        // Specialized services for complex operations (new architecture)
        private readonly IPoolOperationCoordinator _operationCoordinator;
        private readonly IPoolMessagePublisher _messagePublisher;
        private readonly IPoolCircuitBreakerHandler _circuitBreakerHandler;
        
        // Legacy specialized services (maintained for compatibility)
        private readonly IPoolAutoScalingService _autoScalingService;
        private readonly IPoolErrorRecoveryService _errorRecoveryService;
        private readonly IPoolPerformanceMonitorService _performanceMonitorService;
        
        private readonly ProfilerMarker _registerMarker;
        private readonly FixedString128Bytes _correlationId;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private volatile bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PoolingService with specialized service coordination.
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        /// <param name="poolRegistry">Pool registry for managing pool storage</param>
        /// <param name="poolCreationService">Service for creating pool instances</param>
        /// <param name="loggingService">Logging service for pool operations</param>
        /// <param name="messageBusService">Message bus service for publishing pool events</param>
        /// <param name="operationCoordinator">Operation coordinator for Get/Return logic</param>
        /// <param name="messagePublisher">Message publisher for pool events</param>
        /// <param name="circuitBreakerHandler">Circuit breaker handler for resilience</param>
        /// <param name="serializationService">Serialization service for pool state persistence</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="autoScalingService">Auto-scaling service for pool size management</param>
        /// <param name="errorRecoveryService">Error recovery service for pool resilience</param>
        /// <param name="performanceMonitorService">Performance monitoring service for budget enforcement</param>
        /// <param name="healthCheckService">Health check service for system health monitoring</param>
        public PoolingService(
            PoolingServiceConfiguration configuration,
            IPoolRegistry poolRegistry,
            IPoolCreationService poolCreationService,
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IPoolOperationCoordinator operationCoordinator,
            IPoolMessagePublisher messagePublisher,
            IPoolCircuitBreakerHandler circuitBreakerHandler,
            ISerializationService serializationService,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolAutoScalingService autoScalingService = null,
            IPoolErrorRecoveryService errorRecoveryService = null,
            IPoolPerformanceMonitorService performanceMonitorService = null,
            IHealthCheckService healthCheckService = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _poolRegistry = poolRegistry ?? throw new ArgumentNullException(nameof(poolRegistry));
            _poolCreationService = poolCreationService ?? throw new ArgumentNullException(nameof(poolCreationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _operationCoordinator = operationCoordinator ?? throw new ArgumentNullException(nameof(operationCoordinator));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _circuitBreakerHandler = circuitBreakerHandler ?? throw new ArgumentNullException(nameof(circuitBreakerHandler));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            
            _alertService = alertService;
            _profilerService = profilerService;
            _autoScalingService = autoScalingService;
            _errorRecoveryService = errorRecoveryService;
            _performanceMonitorService = performanceMonitorService;
            _healthCheckService = healthCheckService;
            
            // Generate correlation ID for tracking
            _correlationId = DeterministicIdGenerator.GenerateCorrelationFixedString("PoolingService", _configuration.ServiceName.ToString());
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize profiler markers (only for registration, other operations handled by coordinator)
            _registerMarker = new ProfilerMarker("PoolingService.Register");
            
            // Subscribe to circuit breaker state change messages from HealthChecking system
            if (_configuration.EnableCircuitBreaker && _healthCheckService != null)
            {
                _messageBusService.SubscribeToMessage<HealthCheckCircuitBreakerStateChangedMessage>(OnHealthCheckCircuitBreakerStateChanged);
            }
            
            _loggingService.LogInfo($"[{_correlationId}] PoolingService initialized: {_configuration.ServiceName}");
        }
        
        #endregion
        
        #region Core Pool Operations (Delegated to OperationCoordinator)
        
        /// <inheritdoc />
        public T Get<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return _operationCoordinator.CoordinateGet<T>(Guid.Parse(_correlationId.ToString()));
        }
        
        /// <inheritdoc />
        public void Return<T>(T item) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            _operationCoordinator.CoordinateReturn(item, Guid.Parse(_correlationId.ToString()));
        }
        
        /// <inheritdoc />
        public async UniTask<T> GetAsync<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return await _operationCoordinator.CoordinateGetAsync<T>(Guid.Parse(_correlationId.ToString()));
        }
        
        /// <summary>
        /// Gets an object from the specified pool type asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Object from the pool</returns>
        public async UniTask<T> GetAsync<T>(CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return await _operationCoordinator.CoordinateGetAsync<T>(cancellationToken, Guid.Parse(_correlationId.ToString()));
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
            ThrowIfDisposed();
            return await _operationCoordinator.CoordinateGetAsync<T>(timeout, cancellationToken, Guid.Parse(_correlationId.ToString()));
        }
        
        /// <inheritdoc />
        public async UniTask ReturnAsync<T>(T item) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            await _operationCoordinator.CoordinateReturnAsync(item, Guid.Parse(_correlationId.ToString()));
        }
        
        /// <summary>
        /// Returns an object to its pool asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public async UniTask ReturnAsync<T>(T item, CancellationToken cancellationToken) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            await _operationCoordinator.CoordinateReturnAsync(item, cancellationToken, Guid.Parse(_correlationId.ToString()));
        }
        
        #endregion
        
        #region Pool Registration (Orchestration Logic)
        
        /// <inheritdoc />
        public void RegisterPool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            using (_registerMarker.Auto())
            {
                ThrowIfDisposed();
                
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));
                
                var poolTypeName = typeof(T).Name;
                var correlationId = Guid.Parse(_correlationId.ToString());
                
                if (_poolRegistry.IsPoolRegistered<T>())
                {
                    throw new InvalidOperationException($"Pool for type {poolTypeName} is already registered.");
                }
                
                try
                {
                    // Create pool using the creation service
                    var pool = _poolCreationService.CreatePool<T>(configuration);
                    
                    // Register with the pool registry
                    if (!_poolRegistry.RegisterPool(pool))
                    {
                        pool.Dispose();
                        throw new InvalidOperationException($"Failed to register pool for type {poolTypeName}.");
                    }
                    
                    // Register pool with circuit breaker handler
                    _circuitBreakerHandler.RegisterPool(poolTypeName, typeof(T).Name);
                    
                    // Register pool with specialized services if available
                    _autoScalingService?.RegisterPool(poolTypeName, pool);
                    _errorRecoveryService?.RegisterPool(poolTypeName, pool);
                    _performanceMonitorService?.RegisterPool(poolTypeName, pool, configuration.PerformanceBudget);
                    
                    // Persist pool configuration if serialization is available
                    _ = PersistPoolConfigurationAsync(poolTypeName, configuration, correlationId);
                    
                    _loggingService.LogInfo($"[{_correlationId}] Successfully registered pool for type {poolTypeName}");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to register pool for type {poolTypeName}", ex);
                    
                    _alertService?.RaiseAlert(
                        $"Pool registration failed for {poolTypeName}: {ex.Message}",
                        Alerting.Models.AlertSeverity.Critical,
                        "PoolingService");
                    
                    throw;
                }
            }
        }
        
        /// <inheritdoc />
        public void RegisterPool<T>(string poolName = null) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var pool = _poolCreationService.CreatePool<T>(poolName);
            
            if (!_poolRegistry.RegisterPool(pool))
            {
                pool.Dispose();
                throw new InvalidOperationException($"Failed to register pool for type {typeof(T).Name}.");
            }
            
            var typeName = typeof(T).Name;
            
            // Register with circuit breaker handler
            _circuitBreakerHandler.RegisterPool(typeName, typeof(T).Name);
            
            // Register with specialized services
            _autoScalingService?.RegisterPool(typeName, pool);
            _errorRecoveryService?.RegisterPool(typeName, pool);
            _performanceMonitorService?.RegisterPool(typeName, pool, _configuration.DefaultPerformanceBudget);
            
            _loggingService.LogInfo($"[{_correlationId}] Successfully registered pool for type {typeName}");
        }
        
        /// <inheritdoc />
        public void UnregisterPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var poolTypeName = typeof(T).Name;
            
            if (_poolRegistry.UnregisterPool<T>())
            {
                // Unregister from circuit breaker handler
                _circuitBreakerHandler.UnregisterPool(poolTypeName);
                
                // Unregister from specialized services
                _autoScalingService?.UnregisterPool(poolTypeName);
                _errorRecoveryService?.UnregisterPool(poolTypeName);
                _performanceMonitorService?.UnregisterPool(poolTypeName);
                
                // Remove persisted configuration
                _ = RemovePersistedPoolConfigurationAsync(poolTypeName, Guid.Parse(_correlationId.ToString()));
                
                _loggingService.LogInfo($"[{_correlationId}] Successfully unregistered pool for type {poolTypeName}");
            }
        }
        
        /// <inheritdoc />
        public bool IsPoolRegistered<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            return _poolRegistry.IsPoolRegistered<T>();
        }
        
        #endregion
        
        #region Statistics and Monitoring (Delegated to Registry)
        
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
        public async UniTask<PoolStateSnapshot> GetPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("GetPoolStateSnapshot", typeof(T).Name);
            try
            {
                var poolType = typeof(T);
                var pool = _poolRegistry.GetPool<T>();
                if (pool == null)
                {
                    _loggingService.LogWarning($"[{_correlationId}] Cannot create state snapshot - pool not found for {poolType.Name}");
                    return null;
                }

                // Create deterministic pool ID
                var poolId = DeterministicIdGenerator.GeneratePoolId(poolType.FullName ?? poolType.Name, pool.Name);
                
                // Get current statistics
                var statistics = await GetPoolStatisticsAsync<T>(correlationId);
                
                // Create comprehensive snapshot (same logic as SavePoolStateSnapshotAsync but return instead of persist)
                var snapshot = PoolStateSnapshot.FromPoolData(
                    poolId: poolId,
                    poolName: pool.Name ?? poolType.Name,
                    poolType: poolType.FullName ?? poolType.Name,
                    strategyName: pool.GetType().Name,
                    statistics: statistics,
                    initialCapacity: pool.Count,
                    maxCapacity: int.MaxValue,
                    minCapacity: 0
                );

                // Add performance metrics if available
                var performanceMetrics = _profilerService?.GetMetrics(ProfilerTag.Pooling);
                if (performanceMetrics != null)
                {
                    snapshot.SetCustomProperty("PerformanceMetrics", performanceMetrics.ToString());
                }

                // Add health status if available
                if (_healthCheckService != null)
                {
                    var healthStatus = await _healthCheckService.GetOverallHealthStatusAsync();
                    snapshot.HealthStatus = healthStatus.ToString();
                    snapshot.LastHealthCheck = DateTime.UtcNow;
                }

                _loggingService.LogDebug($"[{_correlationId}] Created pool state snapshot for {poolType.Name}: {snapshot.GetSummary()}");
                return snapshot;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to create pool state snapshot for {typeof(T).Name}", ex);
                return null;
            }
        }
        
        /// <inheritdoc />
        public async UniTask<bool> SavePoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SavePoolStateSnapshot", typeof(T).Name);
            try
            {
                await SavePoolStateSnapshotAsync<T>(correlationId);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to save pool state snapshot for {typeof(T).Name}", ex);
                return false;
            }
        }
        
        /// <inheritdoc />
        public async UniTask<PoolStateSnapshot> LoadPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("LoadPoolStateSnapshot", typeof(T).Name);
            return await LoadPoolStateSnapshotAsync<T>(correlationId);
        }
        
        /// <inheritdoc />
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            
            var result = _poolRegistry.ValidateAllPools();
            
            if (!result)
            {
                var statistics = _poolRegistry.GetAllPoolStatistics();
                var totalIssues = statistics.Values.AsValueEnumerable().Count(s => s.FailedGets > 0);
                _messagePublisher.PublishValidationIssuesAsync("AllPools", "Mixed", totalIssues, statistics.Count, Guid.Parse(_correlationId.ToString())).Forget();
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
                var poolTypeName = typeof(T).Name;
                _messagePublisher.PublishValidationIssuesAsync(poolTypeName, poolTypeName, 1, 1, Guid.Parse(_correlationId.ToString())).Forget();
            }
            
            return result;
        }
        
        #endregion
        
        #region Maintenance (Delegated to Registry)
        
        /// <inheritdoc />
        public void ClearAllPools()
        {
            ThrowIfDisposed();
            _poolRegistry.ClearAllPools();
            
            _loggingService.LogInfo($"[{_correlationId}] Cleared all pools");
        }
        
        /// <inheritdoc />
        public void ClearPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            _poolRegistry.ClearPool<T>();
            
            _loggingService.LogDebug($"[{_correlationId}] Cleared pool for {typeof(T).Name}");
        }
        
        /// <inheritdoc />
        public void TrimAllPools()
        {
            ThrowIfDisposed();
            _poolRegistry.TrimAllPools();
            
            _loggingService.LogInfo($"[{_correlationId}] Trimmed all pools");
        }
        
        /// <inheritdoc />
        public void TrimPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            _poolRegistry.TrimPool<T>();
            
            _loggingService.LogDebug($"[{_correlationId}] Trimmed pool for {typeof(T).Name}");
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
            return new NetworkPoolingConfigBuilder(new Factories.PooledNetworkBufferFactory());
        }
        
        #endregion
        
        #region Specialized Service Access
        
        /// <summary>
        /// Gets the operation coordinator for advanced pool operation management.
        /// </summary>
        public IPoolOperationCoordinator OperationCoordinator => _operationCoordinator;
        
        /// <summary>
        /// Gets the message publisher for advanced pool event publishing.
        /// </summary>
        public IPoolMessagePublisher MessagePublisher => _messagePublisher;
        
        /// <summary>
        /// Gets the circuit breaker handler for advanced resilience management.
        /// </summary>
        public IPoolCircuitBreakerHandler CircuitBreakerHandler => _circuitBreakerHandler;
        
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
                _loggingService.LogDebug($"[{_correlationId}] Health check service not available, skipping health check registration");
                return;
            }
            
            try
            {
                // Register main pooling service health check
                var poolingHealthCheck = new HealthChecks.PoolingServiceHealthCheck(this);
                _healthCheckService.RegisterHealthCheck(poolingHealthCheck);
                
                _loggingService.LogInfo($"[{_correlationId}] Registered PoolingService health check");
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
        /// Validates a pooled object using operation coordinator validation.
        /// </summary>
        /// <param name="pooledObject">Object to validate</param>
        /// <returns>True if the object is valid for use</returns>
        public bool ValidatePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null) return false;
            
            // Delegate to operation coordinator for consistency
            return pooledObject.IsValid() && pooledObject.CanBePooled() && !pooledObject.CorruptionDetected;
        }
        
        /// <summary>
        /// Resets a pooled object to its initial state.
        /// </summary>
        /// <param name="pooledObject">Object to reset</param>
        public void ResetPooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            pooledObject?.Reset();
        }
        
        /// <summary>
        /// Determines if a pooled object should be disposed based on its health.
        /// </summary>
        /// <param name="pooledObject">Object to check</param>
        /// <returns>True if the object should be disposed</returns>
        public bool ShouldDisposePooledObject(IPooledObject pooledObject)
        {
            ThrowIfDisposed();
            
            if (pooledObject == null) return true;
            
            return !ValidatePooledObject(pooledObject);
        }
        
        #endregion
        
        #region Private Implementation
        
        /// <summary>
        /// Handles circuit breaker state change messages from HealthChecking system.
        /// Delegates to circuit breaker handler for proper coordination.
        /// </summary>
        /// <param name="message">The circuit breaker state change message from HealthChecking</param>
        private void OnHealthCheckCircuitBreakerStateChanged(HealthCheckCircuitBreakerStateChangedMessage message)
        {
            try
            {
                // Delegate to circuit breaker handler for proper coordination
                _circuitBreakerHandler.HandleCircuitBreakerStateChangeAsync(message, Guid.Parse(_correlationId.ToString())).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to handle HealthCheck circuit breaker state change message", ex);
            }
        }
        
        /// <summary>
        /// Persists pool configuration for recovery and analysis.
        /// </summary>
        private async UniTask PersistPoolConfigurationAsync(string poolType, PoolConfiguration configuration, Guid correlationId)
        {
            try
            {
                var configData = _serializationService.Serialize(configuration);
                var configKey = $"pool_config_{poolType}";
                
                // Store configuration data for persistence system
                // This could be extended to use a proper data store in the future
                await PersistDataAsync(configKey, configData, correlationId);
                
                _loggingService.LogDebug($"[{_correlationId}] Persisted {configData.Length} bytes of config for {poolType}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to persist pool configuration for {poolType}", ex);
                // Don't throw - configuration persistence is not critical for pool operation
            }
        }
        
        /// <summary>
        /// Removes persisted pool configuration.
        /// </summary>
        private async UniTask RemovePersistedPoolConfigurationAsync(string poolType, Guid correlationId)
        {
            try
            {
                var configKey = $"pool_config_{poolType}";
                
                // Remove configuration from persistence system
                await RemovePersistedDataAsync(configKey, correlationId);
                
                _loggingService.LogDebug($"[{_correlationId}] Removed persisted config for {poolType}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to remove persisted pool configuration for {poolType}", ex);
                // Don't throw - configuration cleanup is not critical
            }
        }
        
        #region Pool State Persistence
        
        /// <summary>
        /// Creates and persists a complete pool state snapshot.
        /// Includes statistics, configuration, and health information.
        /// </summary>
        /// <typeparam name="T">The pooled object type</typeparam>
        /// <param name="correlationId">Correlation ID for tracking</param>
        private async UniTask SavePoolStateSnapshotAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            try
            {
                var poolType = typeof(T);
                var pool = _poolRegistry.GetPool<T>();
                if (pool == null)
                {
                    _loggingService.LogWarning($"[{_correlationId}] Cannot save state snapshot - pool not found for {poolType.Name}");
                    return;
                }

                // Create deterministic pool ID
                var poolId = DeterministicIdGenerator.GeneratePoolId(poolType.FullName ?? poolType.Name, pool.Name);
                
                // Get current statistics
                var statistics = await GetPoolStatisticsAsync<T>(correlationId);
                
                // Create comprehensive snapshot
                var snapshot = PoolStateSnapshot.FromPoolData(
                    poolId: poolId,
                    poolName: pool.Name ?? poolType.Name,
                    poolType: poolType.FullName ?? poolType.Name,
                    strategyName: pool.GetType().Name, // Pool strategy name
                    statistics: statistics,
                    initialCapacity: pool.Count, // Current count as proxy for initial
                    maxCapacity: int.MaxValue, // Would need to get from configuration
                    minCapacity: 0
                );

                // Add performance metrics if available
                var performanceMetrics = _profilerService?.GetMetrics(ProfilerTag.Pooling);
                if (performanceMetrics != null)
                {
                    // Add performance data to custom properties
                    snapshot.SetCustomProperty("PerformanceMetrics", performanceMetrics.ToString());
                }

                // Add health status if available
                if (_healthCheckService != null)
                {
                    var healthStatus = await _healthCheckService.GetOverallHealthStatusAsync();
                    snapshot.HealthStatus = healthStatus.ToString();
                    snapshot.LastHealthCheck = DateTime.UtcNow;
                }

                // Serialize and persist snapshot
                var snapshotData = _serializationService.Serialize(snapshot);
                var snapshotKey = $"pool_snapshot_{poolType.FullName ?? poolType.Name}";
                
                await PersistDataAsync(snapshotKey, snapshotData, correlationId);
                
                _loggingService.LogDebug($"[{_correlationId}] Saved pool state snapshot for {poolType.Name}: {snapshotData.Length} bytes");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to save pool state snapshot for {typeof(T).Name}", ex);
                // Don't throw - snapshot persistence is not critical for pool operation
            }
        }

        /// <summary>
        /// Loads a pool state snapshot from persistent storage.
        /// </summary>
        /// <typeparam name="T">The pooled object type</typeparam>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>The loaded pool state snapshot, or null if not found</returns>
        private async UniTask<PoolStateSnapshot> LoadPoolStateSnapshotAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            try
            {
                var poolType = typeof(T);
                var snapshotKey = $"pool_snapshot_{poolType.FullName ?? poolType.Name}";
                
                var snapshotData = await LoadPersistedDataAsync(snapshotKey, correlationId);
                if (snapshotData == null || snapshotData.Length == 0)
                {
                    _loggingService.LogDebug($"[{_correlationId}] No pool state snapshot found for {poolType.Name}");
                    return null;
                }

                var snapshot = _serializationService.Deserialize<PoolStateSnapshot>(snapshotData);
                
                if (snapshot == null || !snapshot.IsValid())
                {
                    _loggingService.LogWarning($"[{_correlationId}] Invalid pool state snapshot for {poolType.Name}");
                    return null;
                }

                _loggingService.LogDebug($"[{_correlationId}] Loaded pool state snapshot for {poolType.Name}: {snapshot.GetSummary()}");
                return snapshot;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to load pool state snapshot for {typeof(T).Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Removes a pool state snapshot from persistent storage.
        /// </summary>
        /// <typeparam name="T">The pooled object type</typeparam>
        /// <param name="correlationId">Correlation ID for tracking</param>
        private async UniTask RemovePoolStateSnapshotAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            try
            {
                var poolType = typeof(T);
                var snapshotKey = $"pool_snapshot_{poolType.FullName ?? poolType.Name}";
                
                await RemovePersistedDataAsync(snapshotKey, correlationId);
                
                _loggingService.LogDebug($"[{_correlationId}] Removed pool state snapshot for {poolType.Name}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to remove pool state snapshot for {typeof(T).Name}", ex);
                // Don't throw - snapshot cleanup is not critical
            }
        }

        /// <summary>
        /// Gets current pool statistics for the specified pool type.
        /// </summary>
        /// <typeparam name="T">The pooled object type</typeparam>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Current pool statistics</returns>
        private async UniTask<PoolStatistics> GetPoolStatisticsAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            try
            {
                var pool = _poolRegistry.GetPool<T>();
                if (pool == null)
                {
                    return new PoolStatistics();
                }

                // Create statistics from current pool state
                var statistics = new PoolStatistics
                {
                    TotalCount = pool.Count,
                    AvailableCount = pool.Count - pool.ActiveCount,
                    ActiveCount = pool.ActiveCount,
                    LastUpdated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow // Would ideally track actual creation time
                };

                // Add more detailed statistics if available
                // This would be populated by pool monitoring over time
                
                await UniTask.Yield(); // Maintain async pattern
                return statistics;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to get pool statistics for {typeof(T).Name}", ex);
                return new PoolStatistics();
            }
        }

        /// <summary>
        /// Generic method to persist data with proper error handling.
        /// </summary>
        /// <param name="key">The persistence key</param>
        /// <param name="data">The data to persist</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        private async UniTask PersistDataAsync(string key, byte[] data, Guid correlationId)
        {
            try
            {
                // In a production system, this would use a proper data store:
                // - File system for local persistence
                // - Database for shared persistence 
                // - Cloud storage for distributed scenarios
                // - Memory-mapped files for high performance
                
                // For now, we'll create a simple temporary storage approach
                // This could be extended with proper data store integration
                
                var tempPath = $"/tmp/ahbear_pool_data_{key}";
                await System.IO.File.WriteAllBytesAsync(tempPath, data);
                
                _loggingService.LogDebug($"[{_correlationId}] Persisted {data.Length} bytes to {tempPath}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to persist data for key {key}", ex);
                throw; // Re-throw for caller to handle
            }
        }

        /// <summary>
        /// Generic method to load persisted data with proper error handling.
        /// </summary>
        /// <param name="key">The persistence key</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>The loaded data, or null if not found</returns>
        private async UniTask<byte[]> LoadPersistedDataAsync(string key, Guid correlationId)
        {
            try
            {
                var tempPath = $"/tmp/ahbear_pool_data_{key}";
                
                if (!System.IO.File.Exists(tempPath))
                {
                    return null;
                }

                var data = await System.IO.File.ReadAllBytesAsync(tempPath);
                
                _loggingService.LogDebug($"[{_correlationId}] Loaded {data.Length} bytes from {tempPath}");
                return data;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to load persisted data for key {key}", ex);
                return null;
            }
        }

        /// <summary>
        /// Generic method to remove persisted data with proper error handling.
        /// </summary>
        /// <param name="key">The persistence key</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        private async UniTask RemovePersistedDataAsync(string key, Guid correlationId)
        {
            try
            {
                var tempPath = $"/tmp/ahbear_pool_data_{key}";
                
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                    _loggingService.LogDebug($"[{_correlationId}] Removed persisted data from {tempPath}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to remove persisted data for key {key}", ex);
                throw; // Re-throw for caller to handle
            }
        }

        #endregion

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolingService));
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
                try
                {
                    // Cancel any ongoing operations
                    _cancellationTokenSource?.Cancel();
                    
                    // Dispose specialized services (new architecture)
                    _operationCoordinator?.Dispose();
                    _messagePublisher?.Dispose();
                    _circuitBreakerHandler?.Dispose();
                    
                    // Dispose pool registry (which disposes all pools)
                    _poolRegistry?.Dispose();
                    
                    // Dispose legacy specialized services
                    _autoScalingService?.Dispose();
                    _errorRecoveryService?.Dispose();
                    _performanceMonitorService?.Dispose();
                    
                    // Dispose cancellation token source
                    _cancellationTokenSource?.Dispose();
                    
                    _disposed = true;
                    _loggingService.LogInfo($"[{_correlationId}] PoolingService disposed: {_configuration.ServiceName}");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException("Error during PoolingService disposal", ex);
                }
            }
        }
        
        #endregion
    }
}