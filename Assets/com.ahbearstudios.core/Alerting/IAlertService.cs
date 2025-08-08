using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Primary alerting service interface providing centralized alert management
    /// with correlation tracking and comprehensive system integration.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// </summary>
    public interface IAlertService : IDisposable
    {
        // Configuration and runtime state properties
        /// <summary>
        /// Gets whether the alerting service is enabled.
        /// </summary>
        bool IsEnabled { get; }

        // Core alerting methods with Unity.Collections v2 correlation tracking
        /// <summary>
        /// Raises an alert with correlation tracking.
        /// </summary>
        /// <param name="message">The alert message</param>
        /// <param name="severity">Alert severity level</param>
        /// <param name="source">Source system or component</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default);

        /// <summary>
        /// Raises an alert using Unity.Collections types for Burst compatibility.
        /// </summary>
        /// <param name="message">The alert message</param>
        /// <param name="severity">Alert severity level</param>
        /// <param name="source">Source system or component</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default);

        /// <summary>
        /// Raises an alert using a pre-constructed alert object.
        /// </summary>
        /// <param name="alert">The alert to raise</param>
        void RaiseAlert(Alert alert);

        /// <summary>
        /// Raises an alert asynchronously with correlation tracking.
        /// </summary>
        /// <param name="alert">The alert to raise</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask representing the operation</returns>
        UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default);

        /// <summary>
        /// Raises an alert asynchronously with message construction.
        /// </summary>
        /// <param name="message">The alert message</param>
        /// <param name="severity">Alert severity level</param>
        /// <param name="source">Source system or component</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask representing the operation</returns>
        UniTask RaiseAlertAsync(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default,
            CancellationToken cancellationToken = default);

        // Severity management
        /// <summary>
        /// Sets the minimum severity level for alerts.
        /// </summary>
        /// <param name="minimumSeverity">Minimum severity level</param>
        void SetMinimumSeverity(AlertSeverity minimumSeverity);

        /// <summary>
        /// Sets the minimum severity level for a specific source.
        /// </summary>
        /// <param name="source">Source system</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        void SetMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity);

        /// <summary>
        /// Gets the minimum severity level for a source or global.
        /// </summary>
        /// <param name="source">Source system (optional)</param>
        /// <returns>Minimum severity level</returns>
        AlertSeverity GetMinimumSeverity(FixedString64Bytes source = default);

        // Channel management
        /// <summary>
        /// Registers an alert channel with the service.
        /// </summary>
        /// <param name="channel">The alert channel to register</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterChannel(IAlertChannel channel, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters an alert channel from the service.
        /// </summary>
        /// <param name="channelName">Name of the channel to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if channel was unregistered</returns>
        bool UnregisterChannel(FixedString64Bytes channelName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets all registered alert channels.
        /// </summary>
        /// <returns>Collection of registered channels</returns>
        IReadOnlyCollection<IAlertChannel> GetRegisteredChannels();

        // Filtering and suppression
        /// <summary>
        /// Adds an alert filter for advanced filtering.
        /// </summary>
        /// <param name="filter">Alert filter to add</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void AddFilter(IAlertFilter filter, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Removes an alert filter.
        /// </summary>
        /// <param name="filterName">Name of filter to remove</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was removed</returns>
        bool RemoveFilter(FixedString64Bytes filterName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Adds a suppression rule for alert filtering.
        /// </summary>
        /// <param name="rule">Alert rule to add</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void AddSuppressionRule(AlertRule rule, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Removes a suppression rule.
        /// </summary>
        /// <param name="ruleName">Name of rule to remove</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if rule was removed</returns>
        bool RemoveSuppressionRule(FixedString64Bytes ruleName, FixedString64Bytes correlationId = default);

        // Alert management
        /// <summary>
        /// Gets all currently active alerts.
        /// </summary>
        /// <returns>Collection of active alerts</returns>
        IEnumerable<Alert> GetActiveAlerts();

        /// <summary>
        /// Gets alert history for a specified time period.
        /// </summary>
        /// <param name="period">Time period for history</param>
        /// <returns>Collection of historical alerts</returns>
        IEnumerable<Alert> GetAlertHistory(TimeSpan period);

        /// <summary>
        /// Acknowledges an alert by its ID.
        /// </summary>
        /// <param name="alertId">Alert ID to acknowledge</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void AcknowledgeAlert(Guid alertId, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Resolves an alert by its ID.
        /// </summary>
        /// <param name="alertId">Alert ID to resolve</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResolveAlert(Guid alertId, FixedString64Bytes correlationId = default);

        // Statistics and monitoring
        /// <summary>
        /// Gets current alerting statistics for monitoring.
        /// </summary>
        /// <returns>Current alerting statistics</returns>
        AlertStatistics GetStatistics();

        /// <summary>
        /// Validates alerting configuration and channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Performs maintenance operations on the alert system.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void PerformMaintenance(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Flushes all buffered alerts to channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the flush operation</returns>
        UniTask FlushAsync(FixedString64Bytes correlationId = default);

        // Message bus integration for system integration
        // Events have been replaced with IMessage pattern for better decoupling
        // AlertRaisedMessage, AlertAcknowledgedMessage, AlertResolvedMessage, and AlertSystemHealthChangedMessage
        // are published through IMessageBusService
    }
}