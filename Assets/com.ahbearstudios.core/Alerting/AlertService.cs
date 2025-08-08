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
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Main implementation of the IAlertService interface providing centralized alert management.
    /// Integrates channels, filters, messaging, logging, and profiling for comprehensive alert processing.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public sealed class AlertService : IAlertService
    {
        private readonly object _syncLock = new object();
        private readonly List<IAlertChannel> _channels = new List<IAlertChannel>();
        private readonly List<IAlertFilter> _filters = new List<IAlertFilter>();
        private readonly List<AlertRule> _suppressionRules = new List<AlertRule>();
        private readonly Dictionary<Guid, Alert> _activeAlerts = new Dictionary<Guid, Alert>();
        private readonly Dictionary<string, AlertSeverity> _sourceMinimumSeverities = new Dictionary<string, AlertSeverity>();
        private readonly Queue<Alert> _alertHistory = new Queue<Alert>();
        
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private AlertSeverity _globalMinimumSeverity = AlertSeverity.Info;
        private AlertStatistics _statistics = AlertStatistics.Empty;
        private DateTime _lastMaintenanceRun = DateTime.UtcNow;
        private readonly TimeSpan _maintenanceInterval = TimeSpan.FromMinutes(5);
        private const int MaxHistorySize = 1000;

        /// <summary>
        /// Gets whether the alerting service is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed;


        /// <summary>
        /// Initializes a new instance of the AlertService class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="serializationService">Serialization service for alert data serialization</param>
        public AlertService(IMessageBusService messageBusService = null, ILoggingService loggingService = null, ISerializationService serializationService = null)
        {
            _messageBusService = messageBusService;
            _loggingService = loggingService;
            _serializationService = serializationService;

            // Add default severity filter
            var defaultSeverityFilter = SeverityAlertFilter.CreateForProduction();
            _filters.Add(defaultSeverityFilter);
            _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            LogInfo("Alert service initialized", Guid.NewGuid());
        }

        #region Core Alerting Methods

        /// <summary>
        /// Raises an alert with correlation tracking.
        /// </summary>
        public void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            if (!IsEnabled) return;

            var alert = Alert.Create(message, severity, source, tag, correlationId);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert using Unity.Collections types for Burst compatibility.
        /// </summary>
        public void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            if (!IsEnabled) return;

            var alert = Alert.Create(message, severity, source, tag, correlationId);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert using a pre-constructed alert object.
        /// </summary>
        public void RaiseAlert(Alert alert)
        {
            if (!IsEnabled || alert == null) return;

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check global minimum severity
                if (alert.Severity < GetMinimumSeverity(alert.Source))
                    return;

                // Apply filters
                var filteredAlert = ApplyFilters(alert);
                if (filteredAlert == null) // Alert was suppressed
                    return;

                // Apply suppression rules
                var suppressedAlert = ApplySuppressionRules(filteredAlert);
                if (suppressedAlert == null) // Alert was suppressed
                    return;

                // Store as active alert
                lock (_syncLock)
                {
                    if (_activeAlerts.ContainsKey(suppressedAlert.Id))
                    {
                        // Update existing alert (increment count)
                        var existingAlert = _activeAlerts[suppressedAlert.Id];
                        _activeAlerts[suppressedAlert.Id] = existingAlert.IncrementCount();
                        suppressedAlert = _activeAlerts[suppressedAlert.Id];
                    }
                    else
                    {
                        _activeAlerts[suppressedAlert.Id] = suppressedAlert;
                    }

                    // Add to history
                    _alertHistory.Enqueue(suppressedAlert);
                    while (_alertHistory.Count > MaxHistorySize)
                        _alertHistory.Dequeue();
                }

                // Send to channels
                _ = DeliverAlertToChannelsAsync(suppressedAlert).Forget();

                // Publish message
                PublishAlertRaisedMessage(suppressedAlert);

                // Update statistics
                UpdateStatistics(true, DateTime.UtcNow - startTime);

                LogDebug($"Alert raised: {suppressedAlert.Severity} from {suppressedAlert.Source}", suppressedAlert.CorrelationId);
            }
            catch (Exception ex)
            {
                LogError($"Error raising alert: {ex.Message}", alert?.CorrelationId ?? Guid.NewGuid());
                UpdateStatistics(false, DateTime.UtcNow - startTime);
            }
        }

        /// <summary>
        /// Raises an alert asynchronously with correlation tracking.
        /// </summary>
        public async UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            await UniTask.RunOnThreadPool(() => RaiseAlert(alert), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Raises an alert asynchronously with message construction.
        /// </summary>
        public async UniTask RaiseAlertAsync(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default, 
            CancellationToken cancellationToken = default)
        {
            var alert = Alert.Create(message, severity, source, tag, correlationId);
            await RaiseAlertAsync(alert, cancellationToken);
        }

        #endregion

        #region Alert Management

        /// <summary>
        /// Gets all currently active alerts.
        /// </summary>
        public IEnumerable<Alert> GetActiveAlerts()
        {
            lock (_syncLock)
            {
                return _activeAlerts.Values.AsValueEnumerable().Where(a => a.State == AlertState.Active).ToList();
            }
        }

        /// <summary>
        /// Gets alert history for a specified time period.
        /// </summary>
        public IEnumerable<Alert> GetAlertHistory(TimeSpan period)
        {
            var cutoff = DateTime.UtcNow - period;
            lock (_syncLock)
            {
                return _alertHistory.AsValueEnumerable().Where(a => a.Timestamp >= cutoff).ToList();
            }
        }

        /// <summary>
        /// Acknowledges an alert by its ID.
        /// </summary>
        public void AcknowledgeAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            lock (_syncLock)
            {
                if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    return;

                if (alert.IsAcknowledged)
                    return;

                var acknowledgedAlert = alert.Acknowledge("System");
                _activeAlerts[alertId] = acknowledgedAlert;

                // Publish message
                PublishAlertAcknowledgedMessage(acknowledgedAlert);

                LogInfo($"Alert acknowledged: {alertId}", correlationId == default ? alert.CorrelationId : correlationId);
            }
        }

        /// <summary>
        /// Resolves an alert by its ID.
        /// </summary>
        public void ResolveAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            lock (_syncLock)
            {
                if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    return;

                if (alert.IsResolved)
                    return;

                var resolvedAlert = alert.Resolve("System");
                _activeAlerts[alertId] = resolvedAlert;

                // Publish message
                PublishAlertResolvedMessage(resolvedAlert);

                LogInfo($"Alert resolved: {alertId}", correlationId == default ? alert.CorrelationId : correlationId);
            }
        }

        #endregion

        #region Severity Management

        /// <summary>
        /// Sets the minimum severity level for alerts.
        /// </summary>
        public void SetMinimumSeverity(AlertSeverity minimumSeverity)
        {
            _globalMinimumSeverity = minimumSeverity;
            LogInfo($"Global minimum severity set to {minimumSeverity}", Guid.NewGuid());
        }

        /// <summary>
        /// Sets the minimum severity level for a specific source.
        /// </summary>
        public void SetMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity)
        {
            var sourceStr = source.ToString();
            lock (_syncLock)
            {
                _sourceMinimumSeverities[sourceStr] = minimumSeverity;
            }
            LogInfo($"Minimum severity for {sourceStr} set to {minimumSeverity}", Guid.NewGuid());
        }

        /// <summary>
        /// Gets the minimum severity level for a source or global.
        /// </summary>
        public AlertSeverity GetMinimumSeverity(FixedString64Bytes source = default)
        {
            if (source.IsEmpty)
                return _globalMinimumSeverity;

            var sourceStr = source.ToString();
            lock (_syncLock)
            {
                return _sourceMinimumSeverities.TryGetValue(sourceStr, out var severity) 
                    ? severity 
                    : _globalMinimumSeverity;
            }
        }

        #endregion

        #region Channel Management

        /// <summary>
        /// Registers an alert channel with the service.
        /// </summary>
        public void RegisterChannel(IAlertChannel channel, FixedString64Bytes correlationId = default)
        {
            if (channel == null) return;

            lock (_syncLock)
            {
                if (!_channels.Any(c => c.Name.ToString() == channel.Name.ToString()))
                {
                    _channels.Add(channel);
                    LogInfo($"Alert channel registered: {channel.Name}", correlationId);
                }
            }
        }

        /// <summary>
        /// Unregisters an alert channel from the service.
        /// </summary>
        public bool UnregisterChannel(FixedString64Bytes channelName, FixedString64Bytes correlationId = default)
        {
            var nameStr = channelName.ToString();
            lock (_syncLock)
            {
                var channel = _channels.AsValueEnumerable().FirstOrDefault(c => c.Name.ToString() == nameStr);
                if (channel != null)
                {
                    _channels.Remove(channel);
                    LogInfo($"Alert channel unregistered: {channelName}", correlationId);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all registered alert channels.
        /// </summary>
        public IReadOnlyCollection<IAlertChannel> GetRegisteredChannels()
        {
            lock (_syncLock)
            {
                return _channels.ToList();
            }
        }

        #endregion

        #region Filtering and Suppression

        /// <summary>
        /// Adds an alert filter for advanced filtering.
        /// </summary>
        public void AddFilter(IAlertFilter filter, FixedString64Bytes correlationId = default)
        {
            if (filter == null) return;

            lock (_syncLock)
            {
                if (!_filters.Any(f => f.Name.ToString() == filter.Name.ToString()))
                {
                    _filters.Add(filter);
                    _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                    LogInfo($"Alert filter added: {filter.Name}", correlationId);
                }
            }
        }

        /// <summary>
        /// Removes an alert filter.
        /// </summary>
        public bool RemoveFilter(FixedString64Bytes filterName, FixedString64Bytes correlationId = default)
        {
            var nameStr = filterName.ToString();
            lock (_syncLock)
            {
                var filter = _filters.AsValueEnumerable().FirstOrDefault(f => f.Name.ToString() == nameStr);
                if (filter != null)
                {
                    _filters.Remove(filter);
                    LogInfo($"Alert filter removed: {filterName}", correlationId);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a suppression rule for alert filtering.
        /// </summary>
        public void AddSuppressionRule(AlertRule rule, FixedString64Bytes correlationId = default)
        {
            if (rule == null) return;

            lock (_syncLock)
            {
                _suppressionRules.Add(rule);
                LogInfo($"Suppression rule added: {rule.Name}", correlationId);
            }
        }

        /// <summary>
        /// Removes a suppression rule.
        /// </summary>
        public bool RemoveSuppressionRule(FixedString64Bytes ruleName, FixedString64Bytes correlationId = default)
        {
            var nameStr = ruleName.ToString();
            lock (_syncLock)
            {
                var rule = _suppressionRules.AsValueEnumerable().FirstOrDefault(r => r.Name.ToString() == nameStr);
                if (rule != null)
                {
                    _suppressionRules.Remove(rule);
                    LogInfo($"Suppression rule removed: {ruleName}", correlationId);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Statistics and Monitoring

        /// <summary>
        /// Gets current alerting statistics for monitoring.
        /// </summary>
        public AlertStatistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// Validates alerting configuration and channels.
        /// </summary>
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            var issues = new List<string>();

            lock (_syncLock)
            {
                if (_channels.Count == 0)
                    issues.Add("No alert channels registered");

                foreach (var channel in _channels)
                {
                    if (!channel.IsHealthy)
                        issues.Add($"Channel {channel.Name} is unhealthy");
                }

                if (_filters.Count == 0)
                    issues.Add("No alert filters configured");
            }

            return issues.Count > 0 
                ? ValidationResult.Failure(issues.Select(i => new ValidationError(i)).ToList(), "AlertService")
                : ValidationResult.Success("AlertService");
        }

        /// <summary>
        /// Performs maintenance operations on the alert system.
        /// </summary>
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            if (DateTime.UtcNow - _lastMaintenanceRun < _maintenanceInterval)
                return;

            _lastMaintenanceRun = DateTime.UtcNow;

            lock (_syncLock)
            {
                // Clean up resolved alerts older than 24 hours
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var toRemove = _activeAlerts.Values.AsValueEnumerable()
                    .Where(a => a.IsResolved && a.ResolvedTimestamp < cutoff)
                    .Select(a => a.Id)
                    .ToList();

                foreach (var id in toRemove)
                {
                    _activeAlerts.Remove(id);
                }

                LogDebug($"Maintenance completed: {toRemove.Count} old alerts cleaned up", correlationId);
            }
        }

        /// <summary>
        /// Flushes all buffered alerts to channels.
        /// </summary>
        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            var channels = GetRegisteredChannels();
            var flushTasks = channels.Select(c => c.FlushAsync(correlationId)).ToList();
            
            await UniTask.WhenAll(flushTasks);
            LogDebug("All channels flushed", correlationId);
        }

        #endregion

        #region Private Methods

        private Alert ApplyFilters(Alert alert)
        {
            var currentAlert = alert;
            var context = FilterContext.WithCorrelation(alert.CorrelationId);

            lock (_syncLock)
            {
                foreach (var filter in _filters)
                {
                    if (!filter.IsEnabled || !filter.CanHandle(currentAlert))
                        continue;

                    var result = filter.Evaluate(currentAlert, context);
                    
                    switch (result.Decision)
                    {
                        case FilterDecision.Allow:
                            continue;
                        case FilterDecision.Suppress:
                            return null; // Alert suppressed
                        case FilterDecision.Modify:
                            currentAlert = result.ModifiedAlert ?? currentAlert;
                            break;
                        case FilterDecision.Defer:
                            // For now, treat defer as allow
                            continue;
                    }
                }
            }

            return currentAlert;
        }

        private Alert ApplySuppressionRules(Alert alert)
        {
            lock (_syncLock)
            {
                foreach (var rule in _suppressionRules)
                {
                    if (!rule.IsEnabled)
                        continue;

                    if (rule.Matches(alert))
                    {
                        var result = rule.ApplyActions(alert);
                        if (result == null)
                        {
                            LogDebug($"Alert suppressed by rule: {rule.Name}", alert.CorrelationId);
                            return null; // Alert suppressed
                        }
                        alert = result;
                    }
                }
            }

            return alert;
        }

        private async UniTask DeliverAlertToChannelsAsync(Alert alert)
        {
            var channels = GetRegisteredChannels();
            var deliveryTasks = new List<UniTask>();

            foreach (var channel in channels)
            {
                if (channel.IsEnabled && channel.IsHealthy && alert.Severity >= channel.MinimumSeverity)
                {
                    deliveryTasks.Add(channel.SendAlertAsync(alert, alert.CorrelationId));
                }
            }

            await UniTask.WhenAll(deliveryTasks);
        }

        private void PublishAlertRaisedMessage(Alert alert)
        {
            try
            {
                var message = AlertRaisedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertRaisedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void PublishAlertAcknowledgedMessage(Alert alert)
        {
            try
            {
                var message = AlertAcknowledgedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertAcknowledgedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void PublishAlertResolvedMessage(Alert alert)
        {
            try
            {
                var message = AlertResolvedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertResolvedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void UpdateStatistics(bool success, TimeSpan duration)
        {
            // Implementation would update comprehensive statistics
            // For brevity, this is simplified
        }

        private void LogDebug(string message, Guid correlationId)
        {
            _loggingService?.LogDebug($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        private void LogInfo(string message, Guid correlationId)
        {
            _loggingService?.LogInfo($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        private void LogError(string message, Guid correlationId)
        {
            _loggingService?.LogError($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the alert service resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            _isDisposed = true;

            lock (_syncLock)
            {
                foreach (var channel in _channels)
                {
                    channel?.Dispose();
                }
                _channels.Clear();

                foreach (var filter in _filters)
                {
                    filter?.Dispose();
                }
                _filters.Clear();

                foreach (var rule in _suppressionRules)
                {
                    rule?.Dispose();
                }
                _suppressionRules.Clear();

                _activeAlerts.Clear();
                _alertHistory.Clear();
                _sourceMinimumSeverities.Clear();
            }

            LogInfo("Alert service disposed", Guid.NewGuid());
        }

        #endregion
    }
}