using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Primary alerting service interface providing centralized alert management
    /// with correlation tracking and comprehensive system integration.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// 
    /// Production-ready features:
    /// - Integrated channel, filter, and suppression services
    /// - Health monitoring and diagnostics
    /// - Configuration hot-reload capabilities
    /// - Emergency failover and circuit breaker patterns
    /// - Comprehensive metrics and performance monitoring
    /// - Bulk operations for high-throughput scenarios
    /// </summary>
    public interface IAlertService : IDisposable
    {
        #region Core Properties and State
        
        /// <summary>
        /// Gets whether the alerting service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Gets whether the service is healthy and functioning normally.
        /// </summary>
        bool IsHealthy { get; }
        
        /// <summary>
        /// Gets the current service configuration.
        /// </summary>
        AlertServiceConfiguration Configuration { get; }
        
        /// <summary>
        /// Gets the integrated channel service for advanced channel management.
        /// </summary>
        IAlertChannelService ChannelService { get; }
        
        /// <summary>
        /// Gets the integrated filter service for sophisticated filtering.
        /// </summary>
        IAlertFilterService FilterService { get; }
        
        /// <summary>
        /// Gets the integrated suppression service for deduplication and rate limiting.
        /// </summary>
        IAlertSuppressionService SuppressionService { get; }

        #endregion

        #region Core Alert Operations
        
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

        #endregion

        #region Bulk Operations
        
        /// <summary>
        /// Raises multiple alerts in a single batch operation for performance.
        /// </summary>
        /// <param name="alerts">Collection of alerts to raise</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the batch operation</returns>
        UniTask RaiseAlertsAsync(IEnumerable<Alert> alerts, Guid correlationId = default);
        
        /// <summary>
        /// Acknowledges multiple alerts by their IDs.
        /// </summary>
        /// <param name="alertIds">Collection of alert IDs to acknowledge</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the batch operation</returns>
        UniTask AcknowledgeAlertsAsync(IEnumerable<Guid> alertIds, Guid correlationId = default);
        
        /// <summary>
        /// Resolves multiple alerts by their IDs.
        /// </summary>
        /// <param name="alertIds">Collection of alert IDs to resolve</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the batch operation</returns>
        UniTask ResolveAlertsAsync(IEnumerable<Guid> alertIds, Guid correlationId = default);

        #endregion

        #region Configuration Management
        
        /// <summary>
        /// Updates the service configuration with hot-reload capability.
        /// </summary>
        /// <param name="configuration">New configuration to apply</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with configuration update result</returns>
        UniTask<bool> UpdateConfigurationAsync(AlertServiceConfiguration configuration, Guid correlationId = default);
        
        /// <summary>
        /// Reloads configuration from the original source.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the reload operation</returns>
        UniTask ReloadConfigurationAsync(Guid correlationId = default);
        
        /// <summary>
        /// Gets the default configuration for the current environment.
        /// </summary>
        /// <returns>Default configuration</returns>
        AlertServiceConfiguration GetDefaultConfiguration();

        #endregion

        #region Health Monitoring and Diagnostics
        
        /// <summary>
        /// Performs a comprehensive health check of the alerting system.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with comprehensive health report</returns>
        UniTask<AlertSystemHealthReport> PerformHealthCheckAsync(Guid correlationId = default);
        
        /// <summary>
        /// Gets detailed diagnostic information about the alerting system.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Comprehensive diagnostic report</returns>
        AlertSystemDiagnostics GetDiagnostics(Guid correlationId = default);
        
        /// <summary>
        /// Gets performance metrics for all subsystems.
        /// </summary>
        /// <returns>Comprehensive performance metrics</returns>
        AlertSystemPerformanceMetrics GetPerformanceMetrics();
        
        /// <summary>
        /// Resets all performance metrics and statistics.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetMetrics(Guid correlationId = default);

        #endregion

        #region Emergency Operations
        
        /// <summary>
        /// Enables emergency mode, bypassing filters and suppression for critical alerts.
        /// </summary>
        /// <param name="reason">Reason for enabling emergency mode</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void EnableEmergencyMode(string reason, Guid correlationId = default);
        
        /// <summary>
        /// Disables emergency mode and restores normal operations.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void DisableEmergencyMode(Guid correlationId = default);
        
        /// <summary>
        /// Gets whether emergency mode is currently active.
        /// </summary>
        bool IsEmergencyModeActive { get; }
        
        /// <summary>
        /// Performs emergency escalation for failed alert delivery.
        /// </summary>
        /// <param name="alert">Alert that failed to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the escalation operation</returns>
        UniTask PerformEmergencyEscalationAsync(Alert alert, Guid correlationId = default);

        #endregion

        #region Statistics and Monitoring
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

        #endregion

        #region Service Control
        
        /// <summary>
        /// Starts the alerting service and all subsystems.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the startup operation</returns>
        UniTask StartAsync(Guid correlationId = default);
        
        /// <summary>
        /// Stops the alerting service and all subsystems gracefully.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the shutdown operation</returns>
        UniTask StopAsync(Guid correlationId = default);
        
        /// <summary>
        /// Restarts the alerting service with current configuration.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the restart operation</returns>
        UniTask RestartAsync(Guid correlationId = default);

        #endregion

        // Message bus integration for system integration
        // Events have been replaced with IMessage pattern for better decoupling
        // AlertRaisedMessage, AlertAcknowledgedMessage, AlertResolvedMessage, and AlertSystemHealthChangedMessage
        // are published through IMessageBusService
    }
}