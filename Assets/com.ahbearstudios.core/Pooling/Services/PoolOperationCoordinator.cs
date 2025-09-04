using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool operation coordination.
    /// Handles complex Get/Return operation logic with proper async handling, cancellation, timeouts, and monitoring.
    /// Follows CLAUDE.md patterns for performance, error handling, and correlation tracking.
    /// </summary>
    public sealed class PoolOperationCoordinator : IPoolOperationCoordinator
    {
        #region Private Fields

        private readonly PoolingServiceConfiguration _configuration;
        private readonly IPoolRegistry _poolRegistry;
        private readonly IPoolCircuitBreakerHandler _circuitBreakerHandler;
        private readonly IPoolMessagePublisher _messagePublisher;
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IPoolAutoScalingService _autoScalingService;
        private readonly IPoolErrorRecoveryService _errorRecoveryService;
        private readonly IPoolPerformanceMonitorService _performanceMonitorService;

        private readonly FixedString128Bytes _correlationId;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Performance monitoring
        private readonly ProfilerMarker _getMarker;
        private readonly ProfilerMarker _returnMarker;
        private readonly ProfilerMarker _getBatchMarker;
        private readonly ProfilerMarker _returnBatchMarker;

        // Operation tracking
        private readonly ConcurrentDictionary<Guid, OperationContext> _activeOperations;
        
        // Performance metrics
        private long _totalGetOperations;
        private long _totalReturnOperations;
        private long _totalBatchGetOperations;
        private long _totalBatchReturnOperations;
        private long _totalFailedOperations;
        private long _totalTimeoutOperations;
        private long _totalValidationFailures;

        // Configuration
        private TimeSpan _defaultTimeout;
        private int _maxRetryAttempts;
        private bool _validationEnabled;

        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolOperationCoordinator.
        /// </summary>
        public PoolOperationCoordinator(
            PoolingServiceConfiguration configuration,
            IPoolRegistry poolRegistry,
            IPoolCircuitBreakerHandler circuitBreakerHandler,
            IPoolMessagePublisher messagePublisher,
            ILoggingService loggingService,
            IProfilerService profilerService = null,
            IAlertService alertService = null,
            IPoolAutoScalingService autoScalingService = null,
            IPoolErrorRecoveryService errorRecoveryService = null,
            IPoolPerformanceMonitorService performanceMonitorService = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _poolRegistry = poolRegistry ?? throw new ArgumentNullException(nameof(poolRegistry));
            _circuitBreakerHandler = circuitBreakerHandler ?? throw new ArgumentNullException(nameof(circuitBreakerHandler));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService;
            _alertService = alertService;
            _autoScalingService = autoScalingService;
            _errorRecoveryService = errorRecoveryService;
            _performanceMonitorService = performanceMonitorService;

            // Generate correlation ID for tracking
            _correlationId = DeterministicIdGenerator.GenerateCorrelationFixedString("PoolOperationCoordinator", _configuration.ServiceName.IsEmpty ? "Default" : _configuration.ServiceName.ToString());
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize profiler markers
            _getMarker = new ProfilerMarker("PoolOperationCoordinator.Get");
            _returnMarker = new ProfilerMarker("PoolOperationCoordinator.Return");
            _getBatchMarker = new ProfilerMarker("PoolOperationCoordinator.GetBatch");
            _returnBatchMarker = new ProfilerMarker("PoolOperationCoordinator.ReturnBatch");

            // Initialize collections
            _activeOperations = new ConcurrentDictionary<Guid, OperationContext>();

            // Initialize configuration
            _defaultTimeout = configuration.AsyncOperationTimeout;
            _maxRetryAttempts = configuration.MaxRetryAttempts;
            _validationEnabled = configuration.EnableObjectValidation;

            _loggingService.LogInfo($"[{_correlationId}] PoolOperationCoordinator initialized");
        }

        #endregion

        #region Synchronous Operations

        /// <inheritdoc />
        public T CoordinateGet<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();
                
                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;
                var operationId = DeterministicIdGenerator.GeneratePoolOperationId(poolTypeName, "Get", $"{effectiveCorrelationId:N}");

                // Track operation start
                var operationContext = CreateOperationContext("get", poolTypeName, operationId, effectiveCorrelationId);
                _activeOperations.TryAdd(operationId, operationContext);

                try
                {
                    Interlocked.Increment(ref _totalGetOperations);

                    // Validate operation is allowed
                    if (!ValidateOperation<T>("get", effectiveCorrelationId))
                    {
                        throw new InvalidOperationException($"Get operation for {poolTypeName} is currently blocked by circuit breaker or validation");
                    }

                    // Publish operation started message
                    _messagePublisher.PublishOperationStartedAsync("get", poolTypeName, typeof(T).Name, operationId, effectiveCorrelationId).Forget();

                    // Get pool and retrieve object
                    var pool = _poolRegistry.GetPool<T>();
                    if (pool == null)
                    {
                        throw new InvalidOperationException($"No pool registered for type {poolTypeName}. Call RegisterPool<T>() first.");
                    }

                    var startTime = DateTime.UtcNow;
                    var item = pool.Get();
                    var duration = DateTime.UtcNow - startTime;

                    // Publish operation completed message
                    _messagePublisher.PublishOperationCompletedAsync("get", poolTypeName, typeof(T).Name, operationId, duration, effectiveCorrelationId).Forget();

                    // Publish object retrieved message
                    _messagePublisher.PublishObjectRetrievedAsync(item, pool, effectiveCorrelationId).Forget();

                    _loggingService.LogDebug($"[{_correlationId}] Successfully retrieved {poolTypeName} object in {duration.TotalMilliseconds}ms");

                    return item;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalFailedOperations);
                    
                    _loggingService.LogException($"Failed to get {poolTypeName} object", ex);
                    
                    // Publish operation failed message
                    _messagePublisher.PublishOperationFailedAsync("get", poolTypeName, typeof(T).Name, operationId, ex, effectiveCorrelationId).Forget();
                    
                    throw;
                }
                finally
                {
                    _activeOperations.TryRemove(operationId, out _);
                }
            }
        }

        /// <inheritdoc />
        public void CoordinateReturn<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_returnMarker.Auto())
            {
                ThrowIfDisposed();
                
                if (item == null) return;

                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;
                var operationId = DeterministicIdGenerator.GeneratePoolOperationId(poolTypeName, "Return", $"{effectiveCorrelationId:N}");

                // Track operation start
                var operationContext = CreateOperationContext("return", poolTypeName, operationId, effectiveCorrelationId);
                _activeOperations.TryAdd(operationId, operationContext);

                try
                {
                    Interlocked.Increment(ref _totalReturnOperations);

                    // Validate operation is allowed
                    if (!ValidateOperation<T>("return", effectiveCorrelationId))
                    {
                        _loggingService.LogWarning($"[{_correlationId}] Return operation for {poolTypeName} is blocked, disposing object");
                        if (item is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        return;
                    }

                    // Validate object for return
                    var isValid = ValidateObjectForReturn(item, effectiveCorrelationId);

                    // Publish operation started message
                    _messagePublisher.PublishOperationStartedAsync("return", poolTypeName, typeof(T).Name, operationId, effectiveCorrelationId).Forget();

                    var startTime = DateTime.UtcNow;
                    var pool = _poolRegistry.GetPool<T>();
                    
                    if (pool == null)
                    {
                        _loggingService.LogWarning($"[{_correlationId}] No pool registered for {poolTypeName}, disposing object");
                        if (item is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        return;
                    }

                    if (isValid)
                    {
                        pool.Return(item);
                    }
                    else
                    {
                        _loggingService.LogWarning($"[{_correlationId}] Object validation failed for {poolTypeName}, disposing instead of returning to pool");
                        if (item is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    var duration = DateTime.UtcNow - startTime;

                    // Publish operation completed message
                    _messagePublisher.PublishOperationCompletedAsync("return", poolTypeName, typeof(T).Name, operationId, duration, effectiveCorrelationId).Forget();

                    // Publish object returned message
                    _messagePublisher.PublishObjectReturnedAsync(item, pool, isValid, effectiveCorrelationId).Forget();

                    _loggingService.LogDebug($"[{_correlationId}] Successfully returned {poolTypeName} object in {duration.TotalMilliseconds}ms (valid: {isValid})");
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalFailedOperations);
                    
                    _loggingService.LogException($"Failed to return {poolTypeName} object", ex);
                    
                    // Publish operation failed message
                    _messagePublisher.PublishOperationFailedAsync("return", poolTypeName, typeof(T).Name, operationId, ex, effectiveCorrelationId).Forget();
                    
                    // Still try to dispose the item
                    if (item is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            _loggingService.LogException($"Failed to dispose {poolTypeName} object after return failure", disposeEx);
                        }
                    }
                }
                finally
                {
                    _activeOperations.TryRemove(operationId, out _);
                }
            }
        }

        #endregion

        #region Asynchronous Operations

        /// <inheritdoc />
        public async UniTask<T> CoordinateGetAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new()
        {
            return await CoordinateGetAsync<T>(CancellationToken.None, correlationId);
        }

        /// <inheritdoc />
        public async UniTask<T> CoordinateGetAsync<T>(CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            return await CoordinateGetAsync<T>(_defaultTimeout, cancellationToken, correlationId);
        }

        /// <inheritdoc />
        public async UniTask<T> CoordinateGetAsync<T>(TimeSpan timeout, CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_getMarker.Auto())
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();

                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;
                var operationId = DeterministicIdGenerator.GeneratePoolOperationId(poolTypeName, "GetAsync", $"{effectiveCorrelationId:N}");

                try
                {
                    Interlocked.Increment(ref _totalGetOperations);

                    // Create combined cancellation token for timeout
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
                    timeoutCts.CancelAfter(timeout);

                    var getOperation = async () =>
                    {
                        // Use UniTask.RunOnThreadPool for potentially blocking operations
                        return await UniTask.RunOnThreadPool(() =>
                        {
                            return CoordinateGet<T>(effectiveCorrelationId);
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
                            _maxRetryAttempts);
                        return result;
                    }

                    // Execute directly if no specialized services
                    return await getOperation();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _loggingService.LogWarning($"[{_correlationId}] GetAsync<{poolTypeName}> was cancelled by caller");
                    throw;
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref _totalTimeoutOperations);
                    _loggingService.LogWarning($"[{_correlationId}] GetAsync<{poolTypeName}> timed out after {timeout.TotalMilliseconds}ms");
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
        public async UniTask CoordinateReturnAsync<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            await CoordinateReturnAsync(item, CancellationToken.None, correlationId);
        }

        /// <inheritdoc />
        public async UniTask CoordinateReturnAsync<T>(T item, CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_returnMarker.Auto())
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();

                if (item == null) return;

                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;

                try
                {
                    var returnOperation = async () =>
                    {
                        // Use UniTask.RunOnThreadPool for potentially blocking validation operations
                        await UniTask.RunOnThreadPool(() =>
                        {
                            CoordinateReturn(item, effectiveCorrelationId);
                        }, true, cancellationToken);
                    };

                    // Apply error recovery if available
                    if (_errorRecoveryService != null)
                    {
                        await _errorRecoveryService.ExecuteWithErrorHandling(
                            poolTypeName,
                            "return",
                            async () => await returnOperation(),
                            _maxRetryAttempts);
                    }
                    else
                    {
                        await returnOperation();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _loggingService.LogWarning($"[{_correlationId}] ReturnAsync<{poolTypeName}> was cancelled by caller");
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

        #region Batch Operations

        /// <inheritdoc />
        public T[] CoordinateGetBatch<T>(int count, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_getBatchMarker.Auto())
            {
                ThrowIfDisposed();

                if (count <= 0) return Array.Empty<T>();

                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;
                var operationId = DeterministicIdGenerator.GeneratePoolOperationId(poolTypeName, "GetBatch", $"{effectiveCorrelationId:N}");

                try
                {
                    Interlocked.Increment(ref _totalBatchGetOperations);

                    // Validate operation is allowed
                    if (!ValidateOperation<T>("get", effectiveCorrelationId))
                    {
                        throw new InvalidOperationException($"Batch get operation for {poolTypeName} is currently blocked");
                    }

                    var results = new T[count];
                    var retrievedCount = 0;

                    // Publish operation started message
                    _messagePublisher.PublishOperationStartedAsync("get-batch", poolTypeName, typeof(T).Name, operationId, effectiveCorrelationId).Forget();

                    var startTime = DateTime.UtcNow;
                    
                    try
                    {
                        // Get all objects individually - could be optimized with batch pool operations in the future
                        for (int i = 0; i < count; i++)
                        {
                            results[i] = CoordinateGet<T>(effectiveCorrelationId);
                            retrievedCount++;
                        }

                        var duration = DateTime.UtcNow - startTime;

                        // Publish operation completed message
                        _messagePublisher.PublishOperationCompletedAsync("get-batch", poolTypeName, typeof(T).Name, operationId, duration, effectiveCorrelationId).Forget();

                        _loggingService.LogDebug($"[{_correlationId}] Successfully retrieved batch of {count} {poolTypeName} objects in {duration.TotalMilliseconds}ms");

                        return results;
                    }
                    catch
                    {
                        // Clean up any objects that were successfully retrieved
                        for (int i = 0; i < retrievedCount; i++)
                        {
                            if (results[i] != null)
                            {
                                try
                                {
                                    CoordinateReturn(results[i], effectiveCorrelationId);
                                }
                                catch (Exception cleanupEx)
                                {
                                    _loggingService.LogException($"Failed to return {poolTypeName} object during batch cleanup", cleanupEx);
                                }
                            }
                        }
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalFailedOperations);
                    
                    _loggingService.LogException($"Failed to get batch of {count} {poolTypeName} objects", ex);
                    
                    // Publish operation failed message
                    _messagePublisher.PublishOperationFailedAsync("get-batch", poolTypeName, typeof(T).Name, operationId, ex, effectiveCorrelationId).Forget();
                    
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public void CoordinateReturnBatch<T>(T[] items, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            using (_returnBatchMarker.Auto())
            {
                ThrowIfDisposed();

                if (items == null || items.Length == 0) return;

                var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
                var poolTypeName = typeof(T).Name;
                var operationId = DeterministicIdGenerator.GeneratePoolOperationId(poolTypeName, "ReturnBatch", $"{effectiveCorrelationId:N}");

                try
                {
                    Interlocked.Increment(ref _totalBatchReturnOperations);

                    // Publish operation started message
                    _messagePublisher.PublishOperationStartedAsync("return-batch", poolTypeName, typeof(T).Name, operationId, effectiveCorrelationId).Forget();

                    var startTime = DateTime.UtcNow;

                    // Return all objects individually - could be optimized with batch pool operations in the future
                    foreach (var item in items.AsValueEnumerable().Where(i => i != null))
                    {
                        try
                        {
                            CoordinateReturn(item, effectiveCorrelationId);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogException($"Failed to return {poolTypeName} object in batch operation", ex);
                            // Continue with other items
                        }
                    }

                    var duration = DateTime.UtcNow - startTime;

                    // Publish operation completed message
                    _messagePublisher.PublishOperationCompletedAsync("return-batch", poolTypeName, typeof(T).Name, operationId, duration, effectiveCorrelationId).Forget();

                    _loggingService.LogDebug($"[{_correlationId}] Successfully returned batch of {items.Length} {poolTypeName} objects in {duration.TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalFailedOperations);
                    
                    _loggingService.LogException($"Failed to return batch of {items.Length} {poolTypeName} objects", ex);
                    
                    // Publish operation failed message
                    _messagePublisher.PublishOperationFailedAsync("return-batch", poolTypeName, typeof(T).Name, operationId, ex, effectiveCorrelationId).Forget();
                }
            }
        }

        /// <inheritdoc />
        public async UniTask<T[]> CoordinateGetBatchAsync<T>(int count, CancellationToken cancellationToken = default, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            // For now, delegate to synchronous method wrapped in UniTask
            // Could be optimized with true async batch operations in the future
            return await UniTask.RunOnThreadPool(() => CoordinateGetBatch<T>(count, correlationId), true, cancellationToken);
        }

        /// <inheritdoc />
        public async UniTask CoordinateReturnBatchAsync<T>(T[] items, CancellationToken cancellationToken = default, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            // For now, delegate to synchronous method wrapped in UniTask
            // Could be optimized with true async batch operations in the future
            await UniTask.RunOnThreadPool(() => CoordinateReturnBatch(items, correlationId), true, cancellationToken);
        }

        #endregion

        #region Operation Validation

        /// <inheritdoc />
        public bool ValidateOperation<T>(string operationType, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();

            if (!_validationEnabled) return true;

            var poolTypeName = typeof(T).Name;

            try
            {
                // Check circuit breaker state
                if (!_circuitBreakerHandler.ShouldAllowPoolOperation<T>(operationType))
                {
                    _loggingService.LogWarning($"[{_correlationId}] Operation '{operationType}' blocked for {poolTypeName} by circuit breaker");
                    return false;
                }

                // Check if pool is registered
                if (!_poolRegistry.IsPoolRegistered<T>())
                {
                    _loggingService.LogWarning($"[{_correlationId}] Operation '{operationType}' blocked for {poolTypeName} - no pool registered");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Error validating operation '{operationType}' for {poolTypeName}", ex);
                return false; // Fail safe
            }
        }

        /// <inheritdoc />
        public bool ValidateObjectForReturn<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();

            if (item == null) return false;
            if (!_validationEnabled) return true;

            try
            {
                // Basic object validation
                if (!item.IsValid())
                {
                    Interlocked.Increment(ref _totalValidationFailures);
                    _loggingService.LogWarning($"[{_correlationId}] Object validation failed for {typeof(T).Name} - object reports invalid state");
                    return false;
                }

                // Check if object can be pooled
                if (!item.CanBePooled())
                {
                    _loggingService.LogDebug($"[{_correlationId}] Object cannot be pooled for {typeof(T).Name}");
                    return false;
                }

                // Check for corruption
                if (item.CorruptionDetected)
                {
                    Interlocked.Increment(ref _totalValidationFailures);
                    _loggingService.LogWarning($"[{_correlationId}] Object corruption detected for {typeof(T).Name}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalValidationFailures);
                _loggingService.LogException($"Error validating object for return {typeof(T).Name}", ex);
                return false; // Fail safe
            }
        }

        #endregion

        #region Performance Monitoring

        /// <inheritdoc />
        public object GetPerformanceMetrics()
        {
            ThrowIfDisposed();

            return new
            {
                TotalGetOperations = Interlocked.Read(ref _totalGetOperations),
                TotalReturnOperations = Interlocked.Read(ref _totalReturnOperations),
                TotalBatchGetOperations = Interlocked.Read(ref _totalBatchGetOperations),
                TotalBatchReturnOperations = Interlocked.Read(ref _totalBatchReturnOperations),
                TotalFailedOperations = Interlocked.Read(ref _totalFailedOperations),
                TotalTimeoutOperations = Interlocked.Read(ref _totalTimeoutOperations),
                TotalValidationFailures = Interlocked.Read(ref _totalValidationFailures),
                ActiveOperationsCount = _activeOperations.Count,
                DefaultTimeout = _defaultTimeout,
                MaxRetryAttempts = _maxRetryAttempts,
                ValidationEnabled = _validationEnabled
            };
        }

        /// <inheritdoc />
        public void ResetPerformanceMetrics()
        {
            ThrowIfDisposed();

            Interlocked.Exchange(ref _totalGetOperations, 0);
            Interlocked.Exchange(ref _totalReturnOperations, 0);
            Interlocked.Exchange(ref _totalBatchGetOperations, 0);
            Interlocked.Exchange(ref _totalBatchReturnOperations, 0);
            Interlocked.Exchange(ref _totalFailedOperations, 0);
            Interlocked.Exchange(ref _totalTimeoutOperations, 0);
            Interlocked.Exchange(ref _totalValidationFailures, 0);

            _loggingService.LogInfo($"[{_correlationId}] Performance metrics reset");
        }

        #endregion

        #region Configuration

        /// <inheritdoc />
        public void UpdateDefaultTimeout(TimeSpan timeout)
        {
            ThrowIfDisposed();

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));

            _defaultTimeout = timeout;
            _loggingService.LogInfo($"[{_correlationId}] Default timeout updated to {timeout.TotalMilliseconds}ms");
        }

        /// <inheritdoc />
        public void UpdateMaxRetryAttempts(int maxRetries)
        {
            ThrowIfDisposed();

            if (maxRetries < 0)
                throw new ArgumentException("Max retries cannot be negative", nameof(maxRetries));

            _maxRetryAttempts = maxRetries;
            _loggingService.LogInfo($"[{_correlationId}] Max retry attempts updated to {maxRetries}");
        }

        /// <inheritdoc />
        public void SetValidationEnabled(bool enableValidation)
        {
            ThrowIfDisposed();

            _validationEnabled = enableValidation;
            _loggingService.LogInfo($"[{_correlationId}] Validation {(enableValidation ? "enabled" : "disabled")}");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates an operation context for tracking.
        /// </summary>
        private OperationContext CreateOperationContext(string operationType, string poolType, Guid operationId, Guid correlationId)
        {
            return new OperationContext
            {
                OperationId = operationId,
                OperationType = operationType,
                PoolType = poolType,
                CorrelationId = correlationId,
                StartTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolOperationCoordinator));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the operation coordinator and its resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                _activeOperations.Clear();
                
                _disposed = true;
                
                _loggingService.LogInfo($"[{_correlationId}] PoolOperationCoordinator disposed");
            }
        }

        #endregion

        #region Private Data Structures

        /// <summary>
        /// Context information for tracking active operations.
        /// </summary>
        private record struct OperationContext
        {
            public Guid OperationId { get; init; }
            public string OperationType { get; init; }
            public string PoolType { get; init; }
            public Guid CorrelationId { get; init; }
            public DateTime StartTime { get; init; }
        }

        #endregion
    }
}