using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Specialized service responsible for publishing health check messages.
    /// Handles all message bus communications for health check events and status changes.
    /// Follows CLAUDE.md patterns for specialized service delegation and IMessage creation.
    /// </summary>
    public interface IHealthCheckMessagePublisherService
    {
        /// <summary>
        /// Publishes a health check completion message.
        /// </summary>
        /// <param name="healthCheckName">Name of the completed health check</param>
        /// <param name="status">Health status result</param>
        /// <param name="message">Health check message</param>
        /// <param name="duration">Duration of the health check</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishHealthCheckCompletedAsync(
            string healthCheckName,
            HealthStatus status,
            string message,
            TimeSpan duration,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a health status change message when overall system health changes.
        /// </summary>
        /// <param name="oldStatus">Previous health status</param>
        /// <param name="newStatus">New health status</param>
        /// <param name="overallHealthScore">Overall health score</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishHealthStatusChangedAsync(
            HealthStatus oldStatus,
            HealthStatus newStatus,
            double overallHealthScore,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a circuit breaker state change message.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="oldState">Previous circuit breaker state</param>
        /// <param name="newState">New circuit breaker state</param>
        /// <param name="reason">Reason for the state change</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishCircuitBreakerStateChangedAsync(
            string operationName,
            CircuitBreakerState oldState,
            CircuitBreakerState newState,
            string reason,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a degradation level change message.
        /// </summary>
        /// <param name="oldLevel">Previous degradation level</param>
        /// <param name="newLevel">New degradation level</param>
        /// <param name="reason">Reason for the change</param>
        /// <param name="affectedSystems">Systems affected by the degradation</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishDegradationLevelChangedAsync(
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason,
            List<string> affectedSystems,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a health check alert message for critical health issues.
        /// </summary>
        /// <param name="alertType">Type of alert</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="message">Alert message</param>
        /// <param name="healthCheckName">Name of the health check that triggered the alert</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishHealthCheckAlertAsync(
            string alertType,
            HealthStatus severity,
            string message,
            string healthCheckName,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a health check configuration change message.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="changeType">Type of configuration change</param>
        /// <param name="previousValue">Previous configuration value</param>
        /// <param name="newValue">New configuration value</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishConfigurationChangedAsync(
            string healthCheckName,
            string changeType,
            string previousValue,
            string newValue,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a batch of health check results for multiple checks.
        /// </summary>
        /// <param name="results">Dictionary of health check results</param>
        /// <param name="overallStatus">Overall health status</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishBatchHealthCheckResultsAsync(
            Dictionary<string, HealthCheckResult> results,
            OverallHealthStatus overallStatus,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a performance threshold exceeded message.
        /// </summary>
        /// <param name="healthCheckName">Name of the slow health check</param>
        /// <param name="actualDuration">Actual execution duration</param>
        /// <param name="thresholdDuration">Threshold that was exceeded</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask PublishPerformanceThresholdExceededAsync(
            string healthCheckName,
            TimeSpan actualDuration,
            TimeSpan thresholdDuration,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets message publishing statistics for monitoring.
        /// </summary>
        /// <returns>Dictionary of publishing metrics</returns>
        Dictionary<string, object> GetPublishingStatistics();

        /// <summary>
        /// Resets message publishing statistics.
        /// </summary>
        void ResetPublishingStatistics();
    }
}