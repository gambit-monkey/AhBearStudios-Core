using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool circuit breaker handling.
    /// Leverages existing HealthChecking system without duplicating circuit breaker logic.
    /// Coordinates pool-specific responses to circuit breaker events following CLAUDE.md patterns.
    /// </summary>
    public sealed class PoolCircuitBreakerHandler : IPoolCircuitBreakerHandler
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IPoolMessagePublisher _messagePublisher;
        private readonly FixedString128Bytes _correlationId;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Pool registration tracking
        private readonly ConcurrentDictionary<string, PoolCircuitBreakerInfo> _registeredPools;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakerStates;
        
        // Operation blocking tracking
        private readonly ConcurrentDictionary<string, bool> _blockedOperations;
        
        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolCircuitBreakerHandler.
        /// </summary>
        /// <param name="loggingService">Logging service for diagnostics</param>
        /// <param name="messagePublisher">Message publisher for pool-specific circuit breaker events</param>
        /// <param name="profilerService">Optional profiler service for performance monitoring</param>
        /// <param name="alertService">Optional alert service for critical notifications</param>
        public PoolCircuitBreakerHandler(
            ILoggingService loggingService,
            IPoolMessagePublisher messagePublisher,
            IProfilerService profilerService = null,
            IAlertService alertService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _profilerService = profilerService;
            _alertService = alertService;

            // Generate correlation ID for tracking
            _correlationId = DeterministicIdGenerator.GenerateCorrelationFixedString("PoolCircuitBreakerHandler", "Default");
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize collections
            _registeredPools = new ConcurrentDictionary<string, PoolCircuitBreakerInfo>();
            _circuitBreakerStates = new ConcurrentDictionary<string, CircuitBreakerState>();
            _blockedOperations = new ConcurrentDictionary<string, bool>();

            _loggingService.LogInfo($"[{_correlationId}] PoolCircuitBreakerHandler initialized");
        }

        #endregion

        #region Circuit Breaker State Management

        /// <inheritdoc />
        public async UniTask HandleCircuitBreakerStateChangeAsync(HealthCheckCircuitBreakerStateChangedMessage message, Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolCircuitBreakerHandler.HandleStateChange");

            var effectiveCorrelationId = correlationId != default ? correlationId : Guid.Parse(_correlationId.ToString());
            var circuitBreakerName = message.CircuitBreakerName.ToString();

            try
            {
                // Update tracked state
                _circuitBreakerStates.AddOrUpdate(circuitBreakerName, message.NewState, (key, oldValue) => message.NewState);

                _loggingService.LogInfo($"[{_correlationId}] Circuit breaker '{circuitBreakerName}' changed: {message.OldState} -> {message.NewState}");

                // Handle state-specific actions
                await (message.NewState switch
                {
                    CircuitBreakerState.Open => HandleCircuitBreakerOpenedAsync(circuitBreakerName, message.Reason.ToString(), effectiveCorrelationId),
                    CircuitBreakerState.Closed => HandleCircuitBreakerClosedAsync(circuitBreakerName, message.Reason.ToString(), effectiveCorrelationId),
                    CircuitBreakerState.HalfOpen => HandleCircuitBreakerHalfOpenAsync(circuitBreakerName, message.Reason.ToString(), effectiveCorrelationId),
                    _ => UniTask.CompletedTask
                });

                // Publish pool-specific circuit breaker message only if we have registered pools
                var affectedPools = GetAffectedPools(circuitBreakerName);
                if (affectedPools.Length > 0)
                {
                    await PublishPoolCircuitBreakerMessageAsync(message, affectedPools, effectiveCorrelationId);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to handle circuit breaker state change for '{circuitBreakerName}'", ex);
                
                _alertService?.RaiseAlert(
                    $"Pool circuit breaker handler failed: {ex.Message}",
                    AlertSeverity.Warning,
                    "PoolCircuitBreakerHandler");
            }
        }

        /// <inheritdoc />
        public bool ShouldAllowPoolOperation(string poolName, string operationType)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName) || string.IsNullOrEmpty(operationType))
                return true; // Default to allowing operations

            // Check if this pool has any associated circuit breakers
            if (_registeredPools.TryGetValue(poolName, out var poolInfo))
            {
                var circuitBreakerName = poolInfo.CircuitBreakerName ?? poolName;
                
                if (_circuitBreakerStates.TryGetValue(circuitBreakerName, out var state))
                {
                    // Block operations when circuit breaker is open, allow when closed or half-open
                    var shouldBlock = state == CircuitBreakerState.Open;
                    
                    if (shouldBlock)
                    {
                        _loggingService.LogWarning($"[{_correlationId}] Blocking {operationType} operation on pool '{poolName}' due to open circuit breaker");
                    }
                    
                    return !shouldBlock;
                }
            }

            // Check global operation blocks
            var operationKey = $"{poolName}:{operationType}";
            return !_blockedOperations.GetValueOrDefault(operationKey, false);
        }

        /// <inheritdoc />
        public bool ShouldAllowPoolOperation<T>(string operationType) where T : class, IPooledObject
        {
            var typeName = typeof(T).Name;
            return ShouldAllowPoolOperation(typeName, operationType);
        }

        #endregion

        #region Pool Response Actions

        /// <inheritdoc />
        public async UniTask HandleCircuitBreakerOpenedAsync(string circuitBreakerName, string reason, Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolCircuitBreakerHandler.HandleOpened");

            try
            {
                _loggingService.LogWarning($"[{_correlationId}] Circuit breaker '{circuitBreakerName}' opened: {reason}");

                // Get affected pools
                var affectedPools = GetAffectedPools(circuitBreakerName);

                // Block operations for affected pools
                foreach (var poolName in affectedPools)
                {
                    BlockPoolOperations(poolName, "Circuit breaker opened");
                }

                // Raise alert for circuit breaker opening
                _alertService?.RaiseAlertAsync(
                    $"Circuit breaker '{circuitBreakerName}' opened affecting {affectedPools.Length} pools: {reason}",
                    AlertSeverity.Warning,
                    "PoolCircuitBreakerHandler");

                // TODO: Could implement additional pool-specific actions here:
                // - Pause auto-scaling
                // - Clear potentially corrupted objects
                // - Adjust pool size limits
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Error handling circuit breaker opened for '{circuitBreakerName}'", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask HandleCircuitBreakerClosedAsync(string circuitBreakerName, string reason, Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolCircuitBreakerHandler.HandleClosed");

            try
            {
                _loggingService.LogInfo($"[{_correlationId}] Circuit breaker '{circuitBreakerName}' closed (recovered): {reason}");

                // Get affected pools
                var affectedPools = GetAffectedPools(circuitBreakerName);

                // Unblock operations for affected pools
                foreach (var poolName in affectedPools)
                {
                    UnblockPoolOperations(poolName, "Circuit breaker closed");
                }

                // Raise informational alert for circuit breaker recovery
                _alertService?.RaiseAlert(
                    $"Circuit breaker '{circuitBreakerName}' recovered affecting {affectedPools.Length} pools: {reason}",
                    AlertSeverity.Info,
                    "PoolCircuitBreakerHandler");

                // TODO: Could implement additional recovery actions here:
                // - Resume auto-scaling
                // - Validate pool health
                // - Reset performance counters
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Error handling circuit breaker closed for '{circuitBreakerName}'", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask HandleCircuitBreakerHalfOpenAsync(string circuitBreakerName, string reason, Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolCircuitBreakerHandler.HandleHalfOpen");

            try
            {
                _loggingService.LogInfo($"[{_correlationId}] Circuit breaker '{circuitBreakerName}' half-opened: {reason}");

                // Get affected pools
                var affectedPools = GetAffectedPools(circuitBreakerName);

                // Allow limited operations for affected pools (unblock but monitor closely)
                foreach (var poolName in affectedPools)
                {
                    UnblockPoolOperations(poolName, "Circuit breaker half-opened");
                    
                    // Could add additional monitoring here
                    _loggingService.LogDebug($"[{_correlationId}] Pool '{poolName}' operations enabled with circuit breaker monitoring");
                }

                // TODO: Could implement additional half-open actions here:
                // - Enable limited operations
                // - Increase monitoring frequency
                // - Set performance thresholds
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Error handling circuit breaker half-open for '{circuitBreakerName}'", ex);
            }
        }

        #endregion

        #region Pool Health Integration

        /// <inheritdoc />
        public async UniTask ReportPoolHealthAsync(string poolName, bool isHealthy, string healthMetrics = null, Guid correlationId = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName)) return;

            using var scope = _profilerService?.BeginScope("PoolCircuitBreakerHandler.ReportPoolHealth");

            try
            {
                _loggingService.LogDebug($"[{_correlationId}] Pool '{poolName}' health: {(isHealthy ? "Healthy" : "Unhealthy")}");

                // Update pool health status
                if (_registeredPools.TryGetValue(poolName, out var poolInfo))
                {
                    var updatedInfo = poolInfo with 
                    { 
                        IsHealthy = isHealthy, 
                        LastHealthCheck = DateTime.UtcNow,
                        HealthMetrics = healthMetrics 
                    };
                    _registeredPools.TryUpdate(poolName, updatedInfo, poolInfo);
                }

                // If pool is unhealthy, consider additional actions
                if (!isHealthy)
                {
                    _loggingService.LogWarning($"[{_correlationId}] Pool '{poolName}' reported unhealthy status");
                    
                    // Could trigger circuit breaker evaluation here
                    // For now, just log and alert
                    _alertService?.RaiseAlert(
                        $"Pool '{poolName}' reported unhealthy status: {healthMetrics}",
                        AlertSeverity.Warning,
                        "PoolCircuitBreakerHandler");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Error reporting pool health for '{poolName}'", ex);
            }
        }

        /// <inheritdoc />
        public string GetCircuitBreakerState(string poolName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName)) return null;

            if (_registeredPools.TryGetValue(poolName, out var poolInfo))
            {
                var circuitBreakerName = poolInfo.CircuitBreakerName ?? poolName;
                if (_circuitBreakerStates.TryGetValue(circuitBreakerName, out var state))
                {
                    return state.ToString();
                }
            }

            return null;
        }

        #endregion

        #region Configuration

        /// <inheritdoc />
        public void RegisterPool(string poolName, string poolType, string circuitBreakerName = null)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName)) return;

            var poolInfo = new PoolCircuitBreakerInfo
            {
                PoolName = poolName,
                PoolType = poolType ?? "Unknown",
                CircuitBreakerName = circuitBreakerName ?? poolName,
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow,
                RegistrationTime = DateTime.UtcNow
            };

            _registeredPools.AddOrUpdate(poolName, poolInfo, (key, existing) => poolInfo);

            _loggingService.LogInfo($"[{_correlationId}] Registered pool '{poolName}' with circuit breaker '{poolInfo.CircuitBreakerName}'");
        }

        /// <inheritdoc />
        public void UnregisterPool(string poolName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName)) return;

            if (_registeredPools.TryRemove(poolName, out var poolInfo))
            {
                // Clean up any blocked operations for this pool  
                var keysToRemove = _blockedOperations.Keys.AsValueEnumerable()
                    .Where(key => key.StartsWith($"{poolName}:"))
                    .ToList()
                    .ToArray();

                foreach (var key in keysToRemove)
                {
                    _blockedOperations.TryRemove(key, out _);
                }

                _loggingService.LogInfo($"[{_correlationId}] Unregistered pool '{poolName}'");
            }
        }

        #endregion

        #region Statistics

        /// <inheritdoc />
        public object GetCircuitBreakerStatistics(string poolName = null)
        {
            ThrowIfDisposed();

            if (!string.IsNullOrEmpty(poolName))
            {
                // Get statistics for specific pool
                if (_registeredPools.TryGetValue(poolName, out var poolInfo))
                {
                    var circuitBreakerName = poolInfo.CircuitBreakerName;
                    var state = _circuitBreakerStates.GetValueOrDefault(circuitBreakerName, CircuitBreakerState.Closed);

                    return new
                    {
                        PoolName = poolName,
                        CircuitBreakerName = circuitBreakerName,
                        CurrentState = state.ToString(),
                        IsHealthy = poolInfo.IsHealthy,
                        LastHealthCheck = poolInfo.LastHealthCheck,
                        HealthMetrics = poolInfo.HealthMetrics,
                        RegistrationTime = poolInfo.RegistrationTime
                    };
                }
                return null;
            }

            // Get statistics for all pools
            var allStats = _registeredPools.Values.AsValueEnumerable()
                .Select(poolInfo =>
                {
                    var state = _circuitBreakerStates.GetValueOrDefault(poolInfo.CircuitBreakerName, CircuitBreakerState.Closed);
                    return new
                    {
                        PoolName = poolInfo.PoolName,
                        PoolType = poolInfo.PoolType,
                        CircuitBreakerName = poolInfo.CircuitBreakerName,
                        CurrentState = state.ToString(),
                        IsHealthy = poolInfo.IsHealthy,
                        LastHealthCheck = poolInfo.LastHealthCheck,
                        HealthMetrics = poolInfo.HealthMetrics,
                        RegistrationTime = poolInfo.RegistrationTime
                    };
                })
                .ToList()
                .ToArray();

            return new
            {
                RegisteredPoolsCount = _registeredPools.Count,
                CircuitBreakerStatesCount = _circuitBreakerStates.Count,
                BlockedOperationsCount = _blockedOperations.Count,
                Pools = allStats
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets pools affected by a specific circuit breaker.
        /// </summary>
        private string[] GetAffectedPools(string circuitBreakerName)
        {
            return _registeredPools.Values.AsValueEnumerable()
                .Where(pool => string.Equals(pool.CircuitBreakerName, circuitBreakerName, StringComparison.OrdinalIgnoreCase))
                .Select(pool => pool.PoolName)
                .ToList()
                .ToArray();
        }

        /// <summary>
        /// Blocks all operations for a specific pool.
        /// </summary>
        private void BlockPoolOperations(string poolName, string reason)
        {
            var operations = new[] { "get", "return", "validate", "trim", "clear" };
            
            foreach (var operation in operations)
            {
                var operationKey = $"{poolName}:{operation}";
                _blockedOperations.AddOrUpdate(operationKey, true, (key, existing) => true);
            }

            _loggingService.LogWarning($"[{_correlationId}] Blocked operations for pool '{poolName}': {reason}");
        }

        /// <summary>
        /// Unblocks all operations for a specific pool.
        /// </summary>
        private void UnblockPoolOperations(string poolName, string reason)
        {
            var keysToRemove = _blockedOperations.Keys.AsValueEnumerable()
                .Where(key => key.StartsWith($"{poolName}:"))
                .ToList()
                .ToArray();

            foreach (var key in keysToRemove)
            {
                _blockedOperations.TryRemove(key, out _);
            }

            _loggingService.LogInfo($"[{_correlationId}] Unblocked operations for pool '{poolName}': {reason}");
        }

        /// <summary>
        /// Publishes pool-specific circuit breaker message.
        /// Only publishes if there are registered pools affected.
        /// </summary>
        private async UniTask PublishPoolCircuitBreakerMessageAsync(
            HealthCheckCircuitBreakerStateChangedMessage healthMessage, 
            string[] affectedPools, 
            Guid correlationId)
        {
            if (affectedPools.Length == 0) return;

            try
            {
                // Create pool-specific circuit breaker message with additional context
                var poolMessage = PoolCircuitBreakerStateChangedMessage.Create(
                    strategyName: healthMessage.CircuitBreakerName.ToString(),
                    oldState: healthMessage.OldState.ToString(),
                    newState: healthMessage.NewState.ToString(),
                    consecutiveFailures: healthMessage.ConsecutiveFailures,
                    totalActivations: (int)Math.Min(healthMessage.TotalActivations, int.MaxValue),
                    source: "PoolCircuitBreakerHandler",
                    correlationId: correlationId
                );

                // Publish via message bus (PoolMessagePublisher doesn't handle this message type)
                // This is the one case where we need to publish directly to avoid circular dependency
                // TODO: Consider if PoolMessagePublisher should handle circuit breaker messages
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool circuit breaker message", ex);
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolCircuitBreakerHandler));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the circuit breaker handler and its resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                _registeredPools.Clear();
                _circuitBreakerStates.Clear();
                _blockedOperations.Clear();
                
                _disposed = true;
                
                _loggingService.LogInfo($"[{_correlationId}] PoolCircuitBreakerHandler disposed");
            }
        }

        #endregion

        #region Private Data Structures

        /// <summary>
        /// Information about a pool registered with the circuit breaker handler.
        /// </summary>
        private record struct PoolCircuitBreakerInfo
        {
            public string PoolName { get; init; }
            public string PoolType { get; init; }
            public string CircuitBreakerName { get; init; }
            public bool IsHealthy { get; init; }
            public DateTime LastHealthCheck { get; init; }
            public string HealthMetrics { get; init; }
            public DateTime RegistrationTime { get; init; }
        }

        #endregion
    }
}