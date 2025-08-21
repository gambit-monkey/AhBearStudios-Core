using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Alerting.Configs;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Defines the contract for alert delivery channels in the AhBearStudios Core Alert System.
    /// Channels are responsible for delivering alerts to specific destinations (logs, console, UI, etc.).
    /// Designed for Unity game development with async UniTask operations and zero-allocation patterns.
    /// </summary>
    public interface IAlertChannel : IDisposable
    {
        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets whether this channel is currently enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets whether this channel is currently healthy and can deliver alerts.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Gets the minimum severity level this channel will process.
        /// Alerts below this threshold will be filtered out.
        /// </summary>
        AlertSeverity MinimumSeverity { get; set; }

        /// <summary>
        /// Gets the maximum number of alerts this channel can process per second.
        /// Used for rate limiting and performance protection.
        /// </summary>
        int MaxAlertsPerSecond { get; set; }

        /// <summary>
        /// Gets the current configuration for this channel.
        /// </summary>
        ChannelConfig Configuration { get; }

        /// <summary>
        /// Gets performance statistics for this channel.
        /// </summary>
        ChannelStatistics Statistics { get; }

        /// <summary>
        /// Sends an alert through this channel synchronously.
        /// Implementations should be fast and non-blocking where possible.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if the alert was sent successfully</returns>
        bool SendAlert(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Sends an alert through this channel asynchronously using UniTask.
        /// Preferred method for I/O bound operations and external integrations.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with send result</returns>
        UniTask<bool> SendAlertAsync(Alert alert, Guid correlationId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple alerts as a batch for efficiency.
        /// Implementations can optimize bulk operations.
        /// </summary>
        /// <param name="alerts">Collection of alerts to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with number of successfully sent alerts</returns>
        UniTask<int> SendAlertBatchAsync(IEnumerable<Alert> alerts, Guid correlationId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the channel connectivity and health.
        /// Should verify that the channel can deliver alerts to its destination.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health check result</returns>
        UniTask<ChannelHealthResult> TestHealthAsync(Guid correlationId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the channel with the provided configuration.
        /// Called during service startup or configuration changes.
        /// </summary>
        /// <param name="config">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with initialization result</returns>
        UniTask<bool> InitializeAsync(ChannelConfig config, Guid correlationId = default);

        /// <summary>
        /// Enables the channel for alert processing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void Enable(Guid correlationId = default);

        /// <summary>
        /// Disables the channel temporarily without disposing resources.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void Disable(Guid correlationId = default);

        /// <summary>
        /// Flushes any buffered alerts to ensure delivery.
        /// Should be called before shutdown or when immediate delivery is required.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask representing the flush operation</returns>
        UniTask FlushAsync(Guid correlationId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets channel statistics and error counters.
        /// Used for maintenance and monitoring purposes.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetStatistics(Guid correlationId = default);

        // Message bus integration for channel events
        // Events have been replaced with IMessage pattern for better decoupling
        // AlertChannelHealthChangedMessage, AlertDeliveryFailedMessage, and AlertChannelConfigurationChangedMessage
        // are published through IMessageBusService
    }

    /// <summary>
    /// Result of a channel health check operation.
    /// </summary>
    public readonly record struct ChannelHealthResult
    {
        /// <summary>
        /// Gets whether the channel is healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the health status message.
        /// </summary>
        public FixedString512Bytes StatusMessage { get; init; }

        /// <summary>
        /// Gets the timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the duration of the health check operation.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets any exception that occurred during the health check.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Creates a healthy result.
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="duration">Check duration</param>
        /// <returns>Healthy channel result</returns>
        public static ChannelHealthResult Healthy(string message = "Channel is operational", TimeSpan duration = default)
        {
            return new ChannelHealthResult
            {
                IsHealthy = true,
                StatusMessage = message,
                Timestamp = DateTime.UtcNow,
                Duration = duration
            };
        }

        /// <summary>
        /// Creates an unhealthy result.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Optional exception</param>
        /// <param name="duration">Check duration</param>
        /// <returns>Unhealthy channel result</returns>
        public static ChannelHealthResult Unhealthy(string message, Exception exception = null, TimeSpan duration = default)
        {
            return new ChannelHealthResult
            {
                IsHealthy = false,
                StatusMessage = message,
                Timestamp = DateTime.UtcNow,
                Duration = duration,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Statistics tracking for alert channel performance and reliability.
    /// </summary>
    public readonly record struct ChannelStatistics
    {
        /// <summary>
        /// Gets the total number of alerts processed by this channel.
        /// </summary>
        public long TotalAlertsProcessed { get; init; }

        /// <summary>
        /// Gets the number of alerts successfully delivered.
        /// </summary>
        public long SuccessfulDeliveries { get; init; }

        /// <summary>
        /// Gets the number of failed deliveries.
        /// </summary>
        public long FailedDeliveries { get; init; }

        /// <summary>
        /// Gets the average delivery time in milliseconds.
        /// </summary>
        public double AverageDeliveryTimeMs { get; init; }

        /// <summary>
        /// Gets the maximum delivery time recorded in milliseconds.
        /// </summary>
        public double MaxDeliveryTimeMs { get; init; }

        /// <summary>
        /// Gets the current delivery rate per second.
        /// </summary>
        public double CurrentDeliveryRate { get; init; }

        /// <summary>
        /// Gets the timestamp when statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        /// Gets the success rate as a percentage (0-100).
        /// </summary>
        public double SuccessRate => TotalAlertsProcessed > 0 
            ? (double)SuccessfulDeliveries / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the failure rate as a percentage (0-100).
        /// </summary>
        public double FailureRate => TotalAlertsProcessed > 0 
            ? (double)FailedDeliveries / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Creates empty statistics.
        /// </summary>
        /// <returns>Empty statistics instance</returns>
        public static ChannelStatistics Empty => new ChannelStatistics
        {
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Event arguments for channel health changes.
    /// </summary>
    public sealed class ChannelHealthChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the previous health status.
        /// </summary>
        public bool PreviousHealthStatus { get; init; }

        /// <summary>
        /// Gets the current health status.
        /// </summary>
        public bool CurrentHealthStatus { get; init; }

        /// <summary>
        /// Gets the health check result.
        /// </summary>
        public ChannelHealthResult HealthResult { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }
    }

    /// <summary>
    /// Event arguments for alert delivery failures.
    /// </summary>
    public sealed class AlertDeliveryFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the channel name where delivery failed.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the alert that failed to be delivered.
        /// </summary>
        public Alert Alert { get; init; }

        /// <summary>
        /// Gets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Gets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets whether this was a final failure (no more retries).
        /// </summary>
        public bool IsFinalFailure { get; init; }
    }

    /// <summary>
    /// Event arguments for channel configuration changes.
    /// </summary>
    public sealed class ChannelConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the previous configuration.
        /// </summary>
        public ChannelConfig PreviousConfiguration { get; init; }

        /// <summary>
        /// Gets the new configuration.
        /// </summary>
        public ChannelConfig NewConfiguration { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }
    }
}