using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Builders;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Common.Extensions;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Refactored AlertService that orchestrates alert processing through decomposed services.
    /// Uses AlertOrchestrationService, AlertStateManagementService, and AlertHealthMonitoringService
    /// for improved maintainability and single responsibility principle compliance.
    ///
    /// Features:
    /// - Decomposed architecture with specialized services
    /// - IProfilerService integration for performance monitoring
    /// - DeterministicIdGenerator for all correlation IDs
    /// - Maintains backward compatibility with existing IAlertService interface
    /// - Thread-safe operations through service delegation
    /// </summary>
    public sealed class AlertService : IAlertService, IDisposable
    {
        #region Dependencies

        private readonly IAlertOrchestrationService _orchestrationService;
        private readonly IAlertStateManagementService _stateManagementService;
        private readonly IAlertHealthMonitoringService _healthMonitoringService;
        private readonly IAlertChannelService _channelService;
        private readonly IAlertFilterService _filterService;
        private readonly IAlertSuppressionService _suppressionService;
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        private readonly IProfilerService _profilerService;

        #endregion

        #region State

        private AlertServiceConfiguration _configuration;
        private volatile bool _isStarted;
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;

        // Backward compatibility collections
        private readonly List<IAlertChannel> _legacyChannels = new List<IAlertChannel>();
        private readonly List<IAlertFilter> _legacyFilters = new List<IAlertFilter>();

        #endregion

        #region IAlertService Properties

        /// <summary>
        /// Gets whether the alerting service is enabled and operational.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed && _isStarted;

        /// <summary>
        /// Gets whether the service is healthy and functioning normally.
        /// </summary>
        public bool IsHealthy => IsEnabled &&
                                _healthMonitoringService?.IsHealthy == true &&
                                _channelService?.IsEnabled == true &&
                                _filterService?.IsEnabled == true &&
                                _suppressionService?.IsEnabled == true;

        /// <summary>
        /// Gets the current service configuration.
        /// </summary>
        public AlertServiceConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the integrated channel service for advanced channel management.
        /// </summary>
        public IAlertChannelService ChannelService => _channelService;

        /// <summary>
        /// Gets the integrated filter service for sophisticated filtering.
        /// </summary>
        public IAlertFilterService FilterService => _filterService;

        /// <summary>
        /// Gets the integrated suppression service for deduplication and rate limiting.
        /// </summary>
        public IAlertSuppressionService SuppressionService => _suppressionService;

        /// <summary>
        /// Gets whether emergency mode is currently active.
        /// </summary>
        public bool IsEmergencyModeActive => _healthMonitoringService?.IsEmergencyModeActive ?? false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertService with decomposed service dependencies.
        /// </summary>
        /// <param name="orchestrationService">Service for alert orchestration</param>
        /// <param name="stateManagementService">Service for alert state management</param>
        /// <param name="healthMonitoringService">Service for health monitoring</param>
        /// <param name="channelService">Channel management service</param>
        /// <param name="filterService">Filter management service</param>
        /// <param name="suppressionService">Suppression management service</param>
        /// <param name="configuration">Service configuration</param>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="serializationService">Serialization service for alert data serialization</param>
        /// <param name="poolingService">Pooling service for efficient alert container management</param>
        /// <param name="profilerService">Profiling service for performance monitoring</param>
        public AlertService(
            IAlertOrchestrationService orchestrationService,
            IAlertStateManagementService stateManagementService,
            IAlertHealthMonitoringService healthMonitoringService,
            IAlertChannelService channelService,
            IAlertFilterService filterService,
            IAlertSuppressionService suppressionService,
            AlertServiceConfiguration configuration,
            IMessageBusService messageBusService = null,
            ILoggingService loggingService = null,
            ISerializationService serializationService = null,
            IPoolingService poolingService = null,
            IProfilerService profilerService = null)
        {
            _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
            _stateManagementService = stateManagementService ?? throw new ArgumentNullException(nameof(stateManagementService));
            _healthMonitoringService = healthMonitoringService ?? throw new ArgumentNullException(nameof(healthMonitoringService));
            _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _suppressionService = suppressionService ?? throw new ArgumentNullException(nameof(suppressionService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _messageBusService = messageBusService;
            _loggingService = loggingService;
            _serializationService = serializationService;
            _poolingService = poolingService;
            _profilerService = profilerService;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.Constructor", "RefactoredInitialization");
            _loggingService?.LogInfo("AlertService initialized with decomposed services architecture", correlationId: correlationId);
        }


        #endregion

        #region IAlertService Implementation

        /// <summary>
        /// Raises an alert with correlation tracking.
        /// </summary>
        public void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlert"))
            {
                if (!IsEnabled) return;

                var alert = CreateAlert(message, severity, source, tag, correlationId);
                RaiseAlert(alert);
            }
        }

        /// <summary>
        /// Raises an alert using Unity.Collections types for Burst compatibility.
        /// </summary>
        public void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlert"))
            {
                if (!IsEnabled) return;

                var alert = CreateAlert(message.ToString(), severity, source, tag, correlationId);
                RaiseAlert(alert);
            }
        }

        /// <summary>
        /// Raises an alert using a pre-constructed alert object.
        /// </summary>
        public void RaiseAlert(Alert alert)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlert"))
            {
                if (!IsEnabled || alert == null) return;

                var correlationId = alert.CorrelationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("AlertService.RaiseAlert", alert.Id.ToString())
                    : alert.CorrelationId;

                try
                {
                    // Process alert through orchestration service
                    _orchestrationService.ProcessAlertAsync(alert, correlationId).Forget();

                    _loggingService?.LogDebug(
                        "Alert {AlertId} raised: {Severity} from {Source}",
                        alert.Id,
                        alert.Severity,
                        alert.Source,
                        correlationId: correlationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(
                        ex,
                        "Failed to raise alert {AlertId}: {ErrorMessage}",
                        alert.Id,
                        ex.Message,
                        correlationId: correlationId);
                }
            }
        }

        /// <summary>
        /// Asynchronously raises an alert with correlation tracking.
        /// </summary>
        public async UniTask RaiseAlertAsync(string message, AlertSeverity severity, string source,
            string tag = null, Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlertAsync"))
            {
                if (!IsEnabled) return;

                var fixedSource = source.ToFixedString64();
                var fixedTag = string.IsNullOrEmpty(tag) ? default : tag.ToFixedString32();
                var alert = CreateAlert(message, severity, fixedSource, fixedTag, correlationId);

                await RaiseAlertAsync(alert, cancellationToken);
            }
        }

        /// <summary>
        /// Asynchronously raises an alert using Unity.Collections types.
        /// </summary>
        public async UniTask RaiseAlertAsync(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlertAsync"))
            {
                if (!IsEnabled) return;

                var alert = CreateAlert(message.ToString(), severity, source, tag, correlationId);
                await RaiseAlertAsync(alert, cancellationToken);
            }
        }

        /// <summary>
        /// Asynchronously raises an alert using a pre-constructed alert object.
        /// </summary>
        public async UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            using (_profilerService?.BeginScope("AlertService.RaiseAlertAsync"))
            {
                if (!IsEnabled || alert == null) return;

                var correlationId = alert.CorrelationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("AlertService.RaiseAlertAsync", alert.Id.ToString())
                    : alert.CorrelationId;

                try
                {
                    await _orchestrationService.ProcessAlertAsync(alert, correlationId, cancellationToken);

                    _loggingService?.LogDebug(
                        "Alert {AlertId} raised asynchronously: {Severity} from {Source}",
                        alert.Id,
                        alert.Severity,
                        alert.Source,
                        correlationId: correlationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(
                        ex,
                        "Failed to raise alert {AlertId} asynchronously: {ErrorMessage}",
                        alert.Id,
                        ex.Message,
                        correlationId: correlationId);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets all currently active alerts.
        /// </summary>
        public IEnumerable<Alert> GetActiveAlerts()
        {
            using (_profilerService?.BeginScope("AlertService.GetActiveAlerts"))
            {
                if (!IsEnabled) return Array.Empty<Alert>();

                try
                {
                    return _stateManagementService.GetActiveAlerts();
                }
                catch (Exception ex)
                {
                    var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.GetActiveAlerts", "Error");
                    _loggingService?.LogError(ex, "Failed to get active alerts: {ErrorMessage}", ex.Message, correlationId: correlationId);
                    return Array.Empty<Alert>();
                }
            }
        }

        /// <summary>
        /// Gets alert history for a specified time period.
        /// </summary>
        public IEnumerable<Alert> GetAlertHistory(TimeSpan period)
        {
            using (_profilerService?.BeginScope("AlertService.GetAlertHistory"))
            {
                if (!IsEnabled) return Array.Empty<Alert>();

                try
                {
                    return _stateManagementService.GetAlertHistory(period);
                }
                catch (Exception ex)
                {
                    var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.GetAlertHistory", period.ToString());
                    _loggingService?.LogError(ex, "Failed to get alert history: {ErrorMessage}", ex.Message, correlationId: correlationId);
                    return Array.Empty<Alert>();
                }
            }
        }

        /// <summary>
        /// Acknowledges an alert, marking it as handled.
        /// </summary>
        public void AcknowledgeAlert(Guid alertId, string acknowledgedBy, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertService.AcknowledgeAlert"))
            {
                if (!IsEnabled) return;

                var finalCorrelationId = correlationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("AlertService.AcknowledgeAlert", alertId.ToString())
                    : correlationId;

                try
                {
                    _stateManagementService.AcknowledgeAlertAsync(alertId, acknowledgedBy, finalCorrelationId).Forget();

                    _loggingService?.LogInfo(
                        "Alert {AlertId} acknowledged by {AcknowledgedBy}",
                        alertId,
                        acknowledgedBy,
                        correlationId: finalCorrelationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(
                        ex,
                        "Failed to acknowledge alert {AlertId}: {ErrorMessage}",
                        alertId,
                        ex.Message,
                        correlationId: finalCorrelationId);
                }
            }
        }

        /// <summary>
        /// Resolves an alert, marking it as resolved.
        /// </summary>
        public void ResolveAlert(Guid alertId, string resolvedBy, string resolution = null, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertService.ResolveAlert"))
            {
                if (!IsEnabled) return;

                var finalCorrelationId = correlationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("AlertService.ResolveAlert", alertId.ToString())
                    : correlationId;

                try
                {
                    _stateManagementService.ResolveAlertAsync(alertId, resolvedBy, resolution, finalCorrelationId).Forget();

                    _loggingService?.LogInfo(
                        "Alert {AlertId} resolved by {ResolvedBy}",
                        alertId,
                        resolvedBy,
                        correlationId: finalCorrelationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(
                        ex,
                        "Failed to resolve alert {AlertId}: {ErrorMessage}",
                        alertId,
                        ex.Message,
                        correlationId: finalCorrelationId);
                }
            }
        }

        /// <summary>
        /// Gets service statistics.
        /// </summary>
        public AlertStatistics GetStatistics()
        {
            using (_profilerService?.BeginScope("AlertService.GetStatistics"))
            {
                if (!IsEnabled) return AlertStatistics.Empty;

                try
                {
                    var stateStats = _stateManagementService.GetStatistics();
                    var healthStats = _healthMonitoringService.GetStatistics();

                    // Combine statistics from decomposed services
                    return new AlertStatistics
                    {
                        TotalAlertsRaised = stateStats.TotalResolved + stateStats.TotalAcknowledged + stateStats.ActiveAlertCount,
                        ActiveAlerts = stateStats.ActiveAlertCount,
                        AcknowledgedAlerts = stateStats.TotalAcknowledged,
                        ResolvedAlerts = stateStats.TotalResolved,
                        AlertsInHistory = stateStats.HistoryCount,
                        AverageProcessingTime = TimeSpan.Zero, // Would need to be tracked separately
                        LastUpdated = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.GetStatistics", "Error");
                    _loggingService?.LogError(ex, "Failed to get statistics: {ErrorMessage}", ex.Message, correlationId: correlationId);
                    return AlertStatistics.Empty;
                }
            }
        }

        /// <summary>
        /// Starts the alert service operations.
        /// </summary>
        public async UniTask StartAsync()
        {
            using (_profilerService?.BeginScope("AlertService.StartAsync"))
            {
                if (_isStarted || _isDisposed) return;

                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.StartAsync", "ServiceStartup");

                try
                {
                    _loggingService?.LogInfo("Starting AlertService with decomposed architecture", correlationId: correlationId);

                    // Start all decomposed services
                    await _healthMonitoringService.StartAsync();

                    _isStarted = true;
                    _isEnabled = true;

                    _loggingService?.LogInfo("AlertService started successfully", correlationId: correlationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(ex, "Failed to start AlertService: {ErrorMessage}", ex.Message, correlationId: correlationId);
                    throw;
                }
            }
        }

        /// <summary>
        /// Stops the alert service operations.
        /// </summary>
        public async UniTask StopAsync()
        {
            using (_profilerService?.BeginScope("AlertService.StopAsync"))
            {
                if (!_isStarted || _isDisposed) return;

                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.StopAsync", "ServiceShutdown");

                try
                {
                    _loggingService?.LogInfo("Stopping AlertService", correlationId: correlationId);

                    _isEnabled = false;

                    // Stop all decomposed services
                    await _healthMonitoringService.StopAsync();

                    _isStarted = false;

                    _loggingService?.LogInfo("AlertService stopped successfully", correlationId: correlationId);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(ex, "Failed to stop AlertService: {ErrorMessage}", ex.Message, correlationId: correlationId);
                    throw;
                }
            }
        }

        #endregion

        #region Legacy Compatibility Methods

        /// <summary>
        /// Adds a channel to the service (legacy compatibility).
        /// </summary>
        public void AddChannel(IAlertChannel channel)
        {
            if (channel == null) return;

            _legacyChannels.Add(channel);
            _channelService?.RegisterChannel(channel);

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.AddChannel", channel.GetType().Name);
            _loggingService?.LogInfo("Legacy channel {ChannelType} added", channel.GetType().Name, correlationId: correlationId);
        }

        /// <summary>
        /// Removes a channel from the service (legacy compatibility).
        /// </summary>
        public void RemoveChannel(IAlertChannel channel)
        {
            if (channel == null) return;

            _legacyChannels.Remove(channel);
            _channelService?.UnregisterChannel(channel);

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.RemoveChannel", channel.GetType().Name);
            _loggingService?.LogInfo("Legacy channel {ChannelType} removed", channel.GetType().Name, correlationId: correlationId);
        }

        /// <summary>
        /// Adds a filter to the service (legacy compatibility).
        /// </summary>
        public void AddFilter(IAlertFilter filter)
        {
            if (filter == null) return;

            _legacyFilters.Add(filter);
            // Legacy filters would need to be wrapped for new architecture

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.AddFilter", filter.GetType().Name);
            _loggingService?.LogInfo("Legacy filter {FilterType} added", filter.GetType().Name, correlationId: correlationId);
        }

        /// <summary>
        /// Removes a filter from the service (legacy compatibility).
        /// </summary>
        public void RemoveFilter(IAlertFilter filter)
        {
            if (filter == null) return;

            _legacyFilters.Remove(filter);
            // Legacy filters would need to be unwrapped from new architecture

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.RemoveFilter", filter.GetType().Name);
            _loggingService?.LogInfo("Legacy filter {FilterType} removed", filter.GetType().Name, correlationId: correlationId);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates an alert with proper correlation ID generation.
        /// </summary>
        private Alert CreateAlert(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertService.CreateAlert", message)
                : correlationId;

            var alertId = DeterministicIdGenerator.GenerateAlertId(message, source.ToString(), finalCorrelationId);

            return new Alert
            {
                Id = alertId,
                Message = message,
                Severity = severity,
                Source = source,
                Tag = tag,
                Timestamp = DateTime.UtcNow,
                CorrelationId = finalCorrelationId,
                State = AlertState.Active,
                Count = 1
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertService.Dispose", "ServiceDisposal");

            try
            {
                _loggingService?.LogInfo("AlertService disposing", correlationId: correlationId);

                _isEnabled = false;

                // Dispose decomposed services
                if (_orchestrationService is IDisposable orchestrationDisposable)
                    orchestrationDisposable.Dispose();

                if (_stateManagementService is IDisposable stateDisposable)
                    stateDisposable.Dispose();

                if (_healthMonitoringService is IDisposable healthDisposable)
                    healthDisposable.Dispose();
            }
            catch (Exception ex)
            {
                // Ignore exceptions during disposal
                _loggingService?.LogWarning("Exception during AlertService disposal: {ErrorMessage}", ex.Message, correlationId: correlationId);
            }
            finally
            {
                _isDisposed = true;
            }
        }

        #endregion
    }
}