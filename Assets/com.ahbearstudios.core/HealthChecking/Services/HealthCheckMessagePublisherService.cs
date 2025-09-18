using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health check message publisher service.
    /// Handles all message bus communications for health check events using proper IMessage patterns.
    /// Follows CLAUDE.md patterns for IMessage creation with static factory methods.
    /// </summary>
    public sealed class HealthCheckMessagePublisherService : IHealthCheckMessagePublisherService
    {
        private readonly IMessageBusService _messageBus;
        private readonly ILoggingService _logger;
        private readonly Guid _serviceId;

        // Publishing statistics
        private long _totalMessagesPublished;
        private long _successfulPublications;
        private long _failedPublications;
        private readonly Dictionary<string, long> _messageTypeCounters;
        private readonly object _statsLock = new object();

        /// <summary>
        /// Initializes a new instance of the HealthCheckMessagePublisherService.
        /// </summary>
        /// <param name="messageBus">Message bus service for publishing messages</param>
        /// <param name="logger">Logging service for publisher operations</param>
        public HealthCheckMessagePublisherService(
            IMessageBusService messageBus,
            ILoggingService logger)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckMessagePublisherService");
            _messageTypeCounters = new Dictionary<string, long>();

            _logger.LogDebug("HealthCheckMessagePublisherService initialized with ID: {ServiceId}", _serviceId);
        }

        /// <inheritdoc />
        public async UniTask PublishHealthCheckCompletedAsync(
            string healthCheckName,
            HealthStatus status,
            string message,
            TimeSpan duration,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var completionMessage = HealthCheckCompletedMessage.Create(
                    healthCheckName: healthCheckName,
                    status: status,
                    message: message,
                    duration: duration,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(completionMessage, cancellationToken);

                UpdateStatistics("HealthCheckCompleted", success: true);

                _logger.LogDebug("Published health check completed message for {HealthCheckName}", healthCheckName);
            }
            catch (Exception ex)
            {
                UpdateStatistics("HealthCheckCompleted", success: false);
                _logger.LogError(ex, "Failed to publish health check completed message for {HealthCheckName}", healthCheckName);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishHealthStatusChangedAsync(
            HealthStatus oldStatus,
            HealthStatus newStatus,
            double overallHealthScore,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var statusChangeMessage = HealthCheckStatusChangedMessage.Create(
                    oldStatus: oldStatus,
                    newStatus: newStatus,
                    overallHealthScore: overallHealthScore,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(statusChangeMessage, cancellationToken);

                UpdateStatistics("HealthStatusChanged", success: true);

                _logger.LogInfo("Published health status change message: {OldStatus} -> {NewStatus}", oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                UpdateStatistics("HealthStatusChanged", success: false);
                _logger.LogError(ex, "Failed to publish health status change message");
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishCircuitBreakerStateChangedAsync(
            string operationName,
            CircuitBreakerState oldState,
            CircuitBreakerState newState,
            string reason,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var circuitBreakerMessage = HealthCheckCircuitBreakerStateChangedMessage.Create(
                    operationName: operationName,
                    oldState: oldState,
                    newState: newState,
                    reason: reason,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(circuitBreakerMessage, cancellationToken);

                UpdateStatistics("CircuitBreakerStateChanged", success: true);

                _logger.LogInfo("Published circuit breaker state change message for {Operation}: {OldState} -> {NewState}",
                    operationName, oldState, newState);
            }
            catch (Exception ex)
            {
                UpdateStatistics("CircuitBreakerStateChanged", success: false);
                _logger.LogError(ex, "Failed to publish circuit breaker state change message for {Operation}", operationName);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishDegradationLevelChangedAsync(
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason,
            List<string> affectedSystems,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var degradationMessage = HealthCheckDegradationChangeMessage.Create(
                    oldLevel: oldLevel,
                    newLevel: newLevel,
                    reason: reason,
                    affectedSystems: affectedSystems,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(degradationMessage, cancellationToken);

                UpdateStatistics("DegradationLevelChanged", success: true);

                _logger.LogWarning("Published degradation level change message: {OldLevel} -> {NewLevel}, Reason: {Reason}",
                    oldLevel, newLevel, reason);
            }
            catch (Exception ex)
            {
                UpdateStatistics("DegradationLevelChanged", success: false);
                _logger.LogError(ex, "Failed to publish degradation level change message");
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishHealthCheckAlertAsync(
            string alertType,
            HealthStatus severity,
            string message,
            string healthCheckName,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var alertMessage = HealthCheckAlertMessage.Create(
                    alertType: alertType,
                    severity: severity,
                    message: message,
                    healthCheckName: healthCheckName,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(alertMessage, cancellationToken);

                UpdateStatistics("HealthCheckAlert", success: true);

                _logger.LogWarning("Published health check alert: {AlertType} for {HealthCheckName}", alertType, healthCheckName);
            }
            catch (Exception ex)
            {
                UpdateStatistics("HealthCheckAlert", success: false);
                _logger.LogError(ex, "Failed to publish health check alert for {HealthCheckName}", healthCheckName);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishConfigurationChangedAsync(
            string healthCheckName,
            string changeType,
            string previousValue,
            string newValue,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configMessage = HealthCheckConfigurationChangedMessage.Create(
                    healthCheckName: healthCheckName,
                    changeType: changeType,
                    previousValue: previousValue,
                    newValue: newValue,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(configMessage, cancellationToken);

                UpdateStatistics("ConfigurationChanged", success: true);

                _logger.LogInfo("Published configuration change message for {HealthCheckName}: {ChangeType}",
                    healthCheckName, changeType);
            }
            catch (Exception ex)
            {
                UpdateStatistics("ConfigurationChanged", success: false);
                _logger.LogError(ex, "Failed to publish configuration change message for {HealthCheckName}", healthCheckName);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishBatchHealthCheckResultsAsync(
            Dictionary<string, HealthCheckResult> results,
            OverallHealthStatus overallStatus,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Publish individual completion messages for each result
                var tasks = new List<UniTask>();

                foreach (var (name, result) in results)
                {
                    var task = PublishHealthCheckCompletedAsync(
                        name, result.Status, result.Message, result.Duration, correlationId, cancellationToken);
                    tasks.Add(task);
                }

                await UniTask.WhenAll(tasks);

                UpdateStatistics("BatchResults", success: true);

                _logger.LogDebug("Published batch health check results for {Count} checks", results.Count);
            }
            catch (Exception ex)
            {
                UpdateStatistics("BatchResults", success: false);
                _logger.LogError(ex, "Failed to publish batch health check results");
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishPerformanceThresholdExceededAsync(
            string healthCheckName,
            TimeSpan actualDuration,
            TimeSpan thresholdDuration,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var performanceMessage = HealthCheckPerformanceThresholdExceededMessage.Create(
                    healthCheckName: healthCheckName,
                    actualDuration: actualDuration,
                    thresholdDuration: thresholdDuration,
                    source: "HealthCheckService",
                    correlationId: correlationId);

                await _messageBus.PublishMessageAsync(performanceMessage, cancellationToken);

                UpdateStatistics("PerformanceThresholdExceeded", success: true);

                _logger.LogWarning("Published performance threshold exceeded message for {HealthCheckName}: {ActualDuration}ms > {ThresholdDuration}ms",
                    healthCheckName, actualDuration.TotalMilliseconds, thresholdDuration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                UpdateStatistics("PerformanceThresholdExceeded", success: false);
                _logger.LogError(ex, "Failed to publish performance threshold exceeded message for {HealthCheckName}", healthCheckName);
                throw;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetPublishingStatistics()
        {
            lock (_statsLock)
            {
                var stats = new Dictionary<string, object>
                {
                    ["TotalMessagesPublished"] = _totalMessagesPublished,
                    ["SuccessfulPublications"] = _successfulPublications,
                    ["FailedPublications"] = _failedPublications,
                    ["SuccessRate"] = _totalMessagesPublished > 0
                        ? (double)_successfulPublications / _totalMessagesPublished
                        : 0.0,
                    ["MessageTypeCounters"] = new Dictionary<string, long>(_messageTypeCounters)
                };

                return stats;
            }
        }

        /// <inheritdoc />
        public void ResetPublishingStatistics()
        {
            lock (_statsLock)
            {
                _totalMessagesPublished = 0;
                _successfulPublications = 0;
                _failedPublications = 0;
                _messageTypeCounters.Clear();

                _logger.LogInfo("Publishing statistics reset for HealthCheckMessagePublisherService");
            }
        }

        private void UpdateStatistics(string messageType, bool success)
        {
            lock (_statsLock)
            {
                _totalMessagesPublished++;

                if (success)
                    _successfulPublications++;
                else
                    _failedPublications++;

                if (!_messageTypeCounters.ContainsKey(messageType))
                    _messageTypeCounters[messageType] = 0;

                _messageTypeCounters[messageType]++;
            }
        }
    }
}