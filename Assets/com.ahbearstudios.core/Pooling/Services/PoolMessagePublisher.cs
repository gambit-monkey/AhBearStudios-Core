using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool message publishing service.
    /// Centralizes message creation and publishing with proper ID generation and correlation tracking.
    /// Follows CLAUDE.md patterns for consistent messaging and zero-allocation where possible.
    /// </summary>
    public sealed class PoolMessagePublisher : IPoolMessagePublisher
    {
        #region Private Fields

        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly Guid _correlationId;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolMessagePublisher.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for diagnostics</param>
        /// <param name="profilerService">Optional profiler service for performance monitoring</param>
        public PoolMessagePublisher(
            IMessageBusService messageBusService,
            ILoggingService loggingService,
            IProfilerService profilerService = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService;

            // Generate deterministic correlation ID for tracking
            _correlationId = DeterministicIdGenerator.GenerateCorrelationId(
                "PoolMessagePublisher", 
                $"{DateTime.UtcNow.Ticks}");
            _cancellationTokenSource = new CancellationTokenSource();

            _loggingService.LogInfo($"[{_correlationId}] PoolMessagePublisher initialized");
        }

        #endregion

        #region Object Lifecycle Messages

        /// <inheritdoc />
        public async UniTask PublishObjectRetrievedAsync<T>(T item, IObjectPool<T> pool, Guid correlationId = default) 
            where T : class, IPooledObject
        {
            ThrowIfDisposed();

            if (item == null || pool == null) return;

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishObjectRetrieved");

            try
            {
                var message = PoolObjectRetrievedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(pool.Name ?? typeof(T).Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: GetDeterministicPoolId(pool),
                    objectId: item.PoolId,
                    poolSizeAfter: pool.Count,
                    activeObjectsAfter: pool.ActiveCount,
                    correlationId: correlationId != default ? correlationId : _correlationId,
                    source: new FixedString64Bytes("PoolMessagePublisher")
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogDebug($"[{_correlationId}] Published object retrieved message for {typeof(T).Name}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish object retrieved message", ex);
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }

        /// <inheritdoc />
        public async UniTask PublishObjectReturnedAsync<T>(T item, IObjectPool<T> pool, bool wasValid, Guid correlationId = default) 
            where T : class, IPooledObject
        {
            ThrowIfDisposed();

            if (item == null || pool == null) return;

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishObjectReturned");

            try
            {
                var message = PoolObjectReturnedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(pool.Name ?? typeof(T).Name),
                    objectTypeName: new FixedString64Bytes(typeof(T).Name),
                    poolId: GetDeterministicPoolId(pool),
                    objectId: item.PoolId,
                    poolSizeAfter: pool.Count,
                    activeObjectsAfter: pool.ActiveCount,
                    wasValidOnReturn: wasValid,
                    correlationId: correlationId != default ? correlationId : _correlationId,
                    source: new FixedString64Bytes("PoolMessagePublisher")
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogDebug($"[{_correlationId}] Published object returned message for {typeof(T).Name}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish object returned message", ex);
                // Swallow message publishing exceptions to avoid affecting pool operations
            }
        }

        #endregion

        #region Pool Status Messages

        /// <inheritdoc />
        public async UniTask PublishCapacityReachedAsync(
            string poolName, 
            string poolType, 
            int currentCapacity, 
            int maxCapacity, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishCapacityReached");

            try
            {
                var message = PoolCapacityReachedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(poolName ?? "Unknown"),
                    objectTypeName: new FixedString64Bytes(poolType ?? "Unknown"),
                    poolId: DeterministicIdGenerator.GeneratePoolId(poolType ?? "Unknown", poolName),
                    currentCapacity: currentCapacity,
                    maxCapacity: maxCapacity,
                    activeObjects: currentCapacity, // Assume current capacity equals active objects for capacity-reached scenario
                    severity: currentCapacity >= maxCapacity ? CapacitySeverity.Critical : CapacitySeverity.Warning,
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogWarning($"[{_correlationId}] Pool capacity reached: {poolName} ({currentCapacity}/{maxCapacity})");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish capacity reached message", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask PublishValidationIssuesAsync(
            string poolName, 
            string poolType, 
            int issueCount, 
            int objectsValidated, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishValidationIssues");

            try
            {
                var message = PoolValidationIssuesMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(poolName ?? "Unknown"),
                    objectTypeName: new FixedString64Bytes(poolType ?? "Unknown"),
                    poolId: DeterministicIdGenerator.GeneratePoolId(poolType ?? "Unknown", poolName),
                    issueCount: issueCount,
                    objectsValidated: objectsValidated,
                    invalidObjects: issueCount, // Assuming all issues are invalid objects for now
                    corruptedObjects: 0, // Would need more detailed validation to determine this
                    severity: issueCount > 5 ? ValidationSeverity.Major : ValidationSeverity.Moderate,
                    correlationId: correlationId != default ? correlationId : _correlationId,
                    source: new FixedString64Bytes("PoolMessagePublisher")
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogWarning($"[{_correlationId}] Pool validation issues: {poolName} ({issueCount}/{objectsValidated})");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish validation issues message", ex);
            }
        }

        #endregion

        #region Pool Operations Messages

        /// <inheritdoc />
        public async UniTask PublishOperationStartedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishOperationStarted");

            try
            {
                var message = PoolOperationStartedMessage.CreateFromFixedStrings(
                    poolName: poolName ?? "Unknown",
                    strategyName: "DefaultStrategy", // Default strategy name since not provided in method parameters
                    operationType: operationType ?? "Unknown",
                    poolSizeAtStart: 0, // Would need to be provided by caller for accurate tracking
                    activeObjectsAtStart: 0, // Would need to be provided by caller for accurate tracking
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogDebug($"[{_correlationId}] Pool operation started: {operationType} on {poolName}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish operation started message", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask PublishOperationCompletedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            TimeSpan duration, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishOperationCompleted");

            try
            {
                var message = PoolOperationCompletedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(poolName ?? "Unknown"),
                    strategyName: new FixedString64Bytes("DefaultStrategy"), // Default strategy name since not provided in method parameters
                    operationType: new FixedString64Bytes(operationType ?? "Unknown"),
                    durationMs: duration.TotalMilliseconds,
                    poolSizeAfter: 0, // Would need to be provided by caller for accurate tracking
                    activeObjectsAfter: 0, // Would need to be provided by caller for accurate tracking
                    isSuccessful: true,
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogDebug($"[{_correlationId}] Pool operation completed: {operationType} on {poolName} in {duration.TotalMilliseconds}ms");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish operation completed message", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask PublishOperationFailedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            Exception exception, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishOperationFailed");

            try
            {
                var message = PoolOperationFailedMessage.CreateFromFixedStrings(
                    poolName: new FixedString64Bytes(poolName ?? "Unknown"),
                    strategyName: new FixedString64Bytes("DefaultStrategy"), // Default strategy name since not provided in method parameters
                    operationType: new FixedString64Bytes(operationType ?? "Unknown"),
                    errorMessage: new FixedString512Bytes(exception?.Message?.Length <= 512 ? exception.Message : exception?.Message?[..512] ?? "Unknown error"),
                    exceptionType: new FixedString128Bytes(exception?.GetType().Name?.Length <= 128 ? exception.GetType().Name : exception?.GetType().Name?[..128] ?? "Exception"),
                    errorCount: 1, // Single error occurrence
                    poolSizeAtFailure: 0, // Would need to be provided by caller for accurate tracking
                    activeObjectsAtFailure: 0, // Would need to be provided by caller for accurate tracking
                    correlationId: correlationId != default ? correlationId : _correlationId,
                    source: new FixedString64Bytes("PoolMessagePublisher")
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogError($"[{_correlationId}] Pool operation failed: {operationType} on {poolName} - {exception?.Message}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish operation failed message", ex);
            }
        }

        #endregion

        #region Pool Scaling Messages

        /// <inheritdoc />
        public async UniTask PublishPoolExpansionAsync(
            string poolName, 
            string poolType, 
            int oldSize, 
            int newSize, 
            string reason, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishPoolExpansion");

            try
            {
                var message = PoolExpansionMessage.CreateFromFixedStrings(
                    strategyName: "DefaultStrategy", // Default strategy name since not provided in method parameters
                    oldSize: oldSize,
                    newSize: newSize,
                    reason: reason ?? "Automatic expansion",
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogInfo($"[{_correlationId}] Pool expanded: {poolName} ({oldSize} -> {newSize}) - {reason}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish pool expansion message", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask PublishPoolContractionAsync(
            string poolName, 
            string poolType, 
            int oldSize, 
            int newSize, 
            string reason, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishPoolContraction");

            try
            {
                var message = PoolContractionMessage.CreateFromFixedStrings(
                    strategyName: "DefaultStrategy", // Default strategy name since not provided in method parameters
                    oldSize: oldSize,
                    newSize: newSize,
                    reason: reason ?? "Automatic contraction",
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogInfo($"[{_correlationId}] Pool contracted: {poolName} ({oldSize} -> {newSize}) - {reason}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish pool contraction message", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask PublishBufferExhaustionAsync(
            string poolName, 
            string poolType, 
            int requestedCount, 
            int availableCount, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishBufferExhaustion");

            try
            {
                var message = PoolBufferExhaustionMessage.CreateFromFixedStrings(
                    strategyName: "DefaultStrategy", // Default strategy name since not provided in method parameters
                    exhaustionCount: requestedCount - availableCount, // Calculate exhaustion as difference between requested and available
                    source: new FixedString64Bytes("PoolMessagePublisher"),
                    correlationId: correlationId != default ? correlationId : _correlationId
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogWarning($"[{_correlationId}] Buffer exhaustion: {poolName} requested {requestedCount}, available {availableCount}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish buffer exhaustion message", ex);
            }
        }

        #endregion

        #region Health and Performance Messages

        /// <inheritdoc />
        public async UniTask PublishStrategyHealthStatusAsync(
            string strategyName, 
            string poolName, 
            string healthStatus, 
            string performanceMetrics, 
            Guid correlationId = default)
        {
            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope("PoolMessagePublisher.PublishStrategyHealthStatus");

            try
            {
                var message = PoolStrategyHealthStatusMessage.CreateFromFixedStrings(
                    strategyName: new FixedString64Bytes(strategyName ?? "Unknown"),
                    isHealthy: ParseHealthStatus(healthStatus) == StrategyHealth.Healthy,
                    errorCount: 0, // Would need to be tracked externally
                    lastHealthCheckTicks: DateTime.UtcNow.Ticks,
                    statusMessage: new FixedString512Bytes(healthStatus ?? "Unknown"),
                    averageOperationDurationMs: 0.0, // Default value - would need to be tracked externally
                    totalOperations: 0, // Default value - would need to be tracked externally  
                    successRatePercentage: 100.0, // Default value - would need to be tracked externally
                    correlationId: correlationId != default ? correlationId : _correlationId,
                    source: new FixedString64Bytes("PoolMessagePublisher")
                );

                await _messageBusService.PublishMessageAsync(message, _cancellationTokenSource.Token);

                _loggingService.LogDebug($"[{_correlationId}] Strategy health status: {strategyName} on {poolName} - {healthStatus}");
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                _loggingService.LogException("Failed to publish strategy health status message", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets a deterministic identifier for the pool based on its type and name.
        /// Provides consistent pool IDs that remain stable across application restarts.
        /// </summary>
        private static Guid GetDeterministicPoolId<T>(IObjectPool<T> pool) where T : class, IPooledObject
        {
            return DeterministicIdGenerator.GeneratePoolId(
                typeof(T).FullName ?? typeof(T).Name, 
                pool.Name);
        }

        /// <summary>
        /// Parses health status string to StrategyHealth enum.
        /// </summary>
        private static StrategyHealth ParseHealthStatus(string healthStatus)
        {
            return healthStatus?.ToLowerInvariant() switch
            {
                "healthy" => StrategyHealth.Healthy,
                "degraded" => StrategyHealth.Degraded,
                "unhealthy" => StrategyHealth.Unhealthy,
                "failed" => StrategyHealth.Unhealthy, // Map "failed" to Unhealthy
                "circuitbreakeropen" => StrategyHealth.CircuitBreakerOpen,
                _ => StrategyHealth.Unknown
            };
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolMessagePublisher));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message publisher and its resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
                
                _loggingService.LogInfo($"[{_correlationId}] PoolMessagePublisher disposed");
            }
        }

        #endregion
    }
}