using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Profiling;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool error recovery service.
    /// Provides comprehensive error handling with automatic recovery mechanisms for pool operations.
    /// Uses Unity-optimized patterns and zero-allocation operations for game performance.
    /// </summary>
    public sealed class PoolErrorRecoveryService : IPoolErrorRecoveryService
    {
        #region Private Fields
        
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        
        private readonly ConcurrentDictionary<string, IObjectPool> _registeredPools;
        private readonly ConcurrentDictionary<string, ErrorRecoveryMetrics> _recoveryMetrics;
        
        private volatile bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PoolErrorRecoveryService.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        public PoolErrorRecoveryService(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IAlertService alertService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _alertService = alertService;
            _profilerService = profilerService;
            
            _registeredPools = new ConcurrentDictionary<string, IObjectPool>();
            _recoveryMetrics = new ConcurrentDictionary<string, ErrorRecoveryMetrics>();
        }
        
        #endregion
        
        #region Pool Registration
        
        /// <inheritdoc />
        public void RegisterPool(string poolTypeName, IObjectPool pool)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                throw new ArgumentException("Pool type name cannot be null or whitespace", nameof(poolTypeName));
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            
            _registeredPools.TryAdd(poolTypeName, pool);
            _recoveryMetrics.TryAdd(poolTypeName, new ErrorRecoveryMetrics());
            
            _loggingService.LogInfo($"Registered pool {poolTypeName} for error recovery");
        }
        
        /// <inheritdoc />
        public void UnregisterPool(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                return;
            
            _registeredPools.TryRemove(poolTypeName, out _);
            _recoveryMetrics.TryRemove(poolTypeName, out _);
            
            _loggingService.LogInfo($"Unregistered pool {poolTypeName} from error recovery");
        }
        
        #endregion
        
        #region Error Recovery Operations
        
        /// <inheritdoc />
        public async UniTask<T> ExecuteWithErrorHandling<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            int maxRetries = 3)
        {
            ThrowIfDisposed();
            
            var metrics = _recoveryMetrics.GetOrAdd(poolTypeName, _ => new ErrorRecoveryMetrics());
            var attempt = 0;
            
            using var scope = _profilerService?.BeginScope($"PoolErrorRecovery.Execute.{poolTypeName}.{operationType}");
            
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
                    _alertService?.RaiseAlert(
                        $"Pool {poolTypeName} operation {operationType} failed permanently: {ex.Message}",
                        AlertSeverity.Critical,
                        $"PoolErrorRecoveryService.{poolTypeName}"
                    );
                    
                    // Attempt emergency recovery for critical failures
                    if (metrics.ConsecutiveFailures >= 5)
                    {
                        PerformEmergencyRecovery(poolTypeName).Forget();
                    }
                    
                    throw new InvalidOperationException($"Pool operation failed for {poolTypeName}.{operationType}: {ex.Message}", ex);
                }
            }
            
            // This should never be reached due to the exception handling above
            throw new InvalidOperationException("Unexpected error in retry logic");
        }
        
        /// <inheritdoc />
        public async UniTask ExecuteWithErrorHandling(
            string poolTypeName,
            string operationType,
            Func<UniTask> operation,
            int maxRetries = 3)
        {
            await ExecuteWithErrorHandling(poolTypeName, operationType, async () =>
            {
                await operation();
                return true; // Dummy return value
            }, maxRetries);
        }
        
        #endregion
        
        #region Manual Recovery
        
        /// <inheritdoc />
        public async UniTask ForcePoolRecovery(string poolTypeName)
        {
            ThrowIfDisposed();
            
            _loggingService.LogInfo($"Forcing recovery for pool {poolTypeName}");
            await AttemptPoolRecovery(poolTypeName, new InvalidOperationException("Manual recovery requested"));
        }
        
        /// <inheritdoc />
        public async UniTask PerformEmergencyRecovery(string poolTypeName)
        {
            ThrowIfDisposed();
            
            try
            {
                _loggingService.LogCritical($"Performing emergency recovery for pool {poolTypeName}");
                
                if (!_registeredPools.TryGetValue(poolTypeName, out var oldPool))
                {
                    _loggingService.LogError($"Cannot perform emergency recovery: pool {poolTypeName} not registered");
                    return;
                }
                
                // Clear the pool to prevent further issues
                try
                {
                    oldPool.Clear();
                    _loggingService.LogInfo($"Cleared pool {poolTypeName} during emergency recovery");
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to clear pool {poolTypeName} during emergency recovery", ex);
                }
                
                // Reset recovery metrics
                if (_recoveryMetrics.TryGetValue(poolTypeName, out var metrics))
                {
                    metrics.ConsecutiveFailures = 0;
                    metrics.IsInRecoveryMode = false;
                }
                
                // Publish emergency recovery message
                _alertService?.RaiseAlert(
                    $"Emergency recovery completed for pool {poolTypeName}",
                    AlertSeverity.Warning,
                    $"PoolErrorRecoveryService.EmergencyRecovery.{poolTypeName}"
                );
                
                _loggingService.LogInfo($"Emergency recovery completed for pool {poolTypeName}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Emergency recovery failed for pool {poolTypeName}", ex);
                
                // Last resort: alert that the pool is completely non-functional
                _alertService?.RaiseAlert(
                    $"Pool {poolTypeName} is completely non-functional and requires manual intervention",
                    AlertSeverity.Critical,
                    $"PoolErrorRecoveryService.EmergencyRecovery.{poolTypeName}"
                );
            }
        }
        
        #endregion
        
        #region Statistics and Health
        
        /// <inheritdoc />
        public Dictionary<string, object> GetErrorRecoveryStatistics()
        {
            ThrowIfDisposed();
            
            return _recoveryMetrics.AsValueEnumerable()
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)new
                    {
                        ConsecutiveFailures = kvp.Value.ConsecutiveFailures,
                        TotalRecoveryAttempts = kvp.Value.TotalRecoveryAttempts,
                        SuccessfulRecoveries = kvp.Value.SuccessfulRecoveries,
                        RecoverySuccessRate = kvp.Value.RecoverySuccessRate,
                        LastFailureTime = kvp.Value.LastFailureTime,
                        LastRecoveryAttempt = kvp.Value.LastRecoveryAttempt,
                        IsInRecoveryMode = kvp.Value.IsInRecoveryMode,
                        LastExceptionMessage = kvp.Value.LastException?.Message
                    });
        }
        
        /// <inheritdoc />
        public object GetPoolErrorRecoveryMetrics(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (!_recoveryMetrics.TryGetValue(poolTypeName, out var metrics))
                return null;
            
            return new
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
        
        /// <inheritdoc />
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
        
        #region Private Implementation
        
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
            var random = Random.CreateFromIndex((uint)DateTime.UtcNow.Ticks);
            var jitter = TimeSpan.FromMilliseconds(random.NextInt(0, 50));
            
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
                
                if (!_registeredPools.TryGetValue(poolTypeName, out var pool))
                {
                    _loggingService.LogError($"Cannot recover pool {poolTypeName}: pool not found");
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
                    PerformEmergencyRecovery(poolTypeName).Forget();
                }
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
                var message = PoolOperationCompletedMessage.Create(
                    poolName: poolTypeName,
                    strategyName: "RecoveryStrategy",
                    operationType: $"{operationType}_recovery",
                    duration: TimeSpan.Zero,
                    poolSizeAfter: 0, // Recovery operation, size unknown
                    activeObjectsAfter: 0, // Recovery operation, active objects unknown
                    isSuccessful: true,
                    source: "PoolErrorRecoveryService"
                );
                
                _messageBusService.PublishMessageAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool recovery message", ex);
            }
        }
        
        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolErrorRecoveryService));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the error recovery service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _registeredPools.Clear();
                _recoveryMetrics.Clear();
                _disposed = true;
            }
        }
        
        #endregion
    }
}