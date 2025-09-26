using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Production-ready implementation of alert state management service.
    /// Handles active alerts, history, acknowledgment, resolution, and source severity management.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public sealed class AlertStateManagementService : IAlertStateManagementService
    {
        #region Private Fields

        private readonly object _syncLock = new object();
        private readonly Dictionary<Guid, Alert> _activeAlerts = new Dictionary<Guid, Alert>();
        private readonly Queue<Alert> _alertHistory = new Queue<Alert>();
        private readonly Dictionary<FixedString64Bytes, AlertSeverity> _sourceSeverityOverrides = new Dictionary<FixedString64Bytes, AlertSeverity>();

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IProfilerService _profilerService;

        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private const int MaxHistorySize = 10000;

        // Statistics tracking
        private long _totalAcknowledged;
        private long _totalResolved;
        private readonly List<TimeSpan> _acknowledgmentTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _resolutionTimes = new List<TimeSpan>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertStateManagementService class.
        /// </summary>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="messageBusService">Message bus service for publishing state change events</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        public AlertStateManagementService(
            ILoggingService loggingService = null,
            IMessageBusService messageBusService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService;
            _messageBusService = messageBusService;
            _profilerService = profilerService;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertStateManagement", "Initialization");
            LogInfo("Alert state management service initialized", correlationId);
        }

        #endregion

        #region IAlertStateManagementService Implementation

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled && !_isDisposed;

        /// <inheritdoc />
        public int ActiveAlertCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _activeAlerts.Count;
                }
            }
        }

        /// <inheritdoc />
        public int HistoryCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _alertHistory.Count;
                }
            }
        }

        /// <inheritdoc />
        public void StoreAlert(Alert alert, Guid correlationId = default)
        {
            if (!IsEnabled || alert == null) return;

            correlationId = EnsureCorrelationId(correlationId, "StoreAlert", alert.Source.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.StoreAlert"))
            {
                lock (_syncLock)
                {
                    if (_activeAlerts.ContainsKey(alert.Id))
                    {
                        // Update existing alert (increment count)
                        var existingAlert = _activeAlerts[alert.Id];
                        _activeAlerts[alert.Id] = existingAlert.IncrementCount();
                        alert = _activeAlerts[alert.Id];
                    }
                    else
                    {
                        _activeAlerts[alert.Id] = alert;
                    }

                    // Add to history
                    _alertHistory.Enqueue(alert);
                    while (_alertHistory.Count > MaxHistorySize)
                        _alertHistory.Dequeue();
                }

                LogDebug($"Alert stored: {alert.Id} from {alert.Source}", correlationId);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Alert> GetActiveAlerts(Guid correlationId = default)
        {
            if (!IsEnabled) return Enumerable.Empty<Alert>();

            correlationId = EnsureCorrelationId(correlationId, "GetActiveAlerts", "System");

            using (_profilerService?.BeginScope("AlertStateManagement.GetActiveAlerts"))
            {
                lock (_syncLock)
                {
                    var activeAlerts = _activeAlerts.Values.AsValueEnumerable()
                        .Where(a => a.State == AlertState.Active)
                        .ToList();

                    LogDebug($"Retrieved {activeAlerts.Count} active alerts", correlationId);
                    return activeAlerts;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<Alert> GetAlertHistory(TimeSpan period, Guid correlationId = default)
        {
            if (!IsEnabled) return Enumerable.Empty<Alert>();

            correlationId = EnsureCorrelationId(correlationId, "GetAlertHistory", period.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.GetAlertHistory"))
            {
                var cutoff = DateTime.UtcNow - period;
                lock (_syncLock)
                {
                    var historyAlerts = _alertHistory.AsValueEnumerable()
                        .Where(a => a.Timestamp >= cutoff)
                        .ToList();

                    LogDebug($"Retrieved {historyAlerts.Count} historical alerts for period {period}", correlationId);
                    return historyAlerts;
                }
            }
        }

        /// <inheritdoc />
        public Alert GetAlert(Guid alertId, Guid correlationId = default)
        {
            if (!IsEnabled) return null;

            correlationId = EnsureCorrelationId(correlationId, "GetAlert", alertId.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.GetAlert"))
            {
                lock (_syncLock)
                {
                    _activeAlerts.TryGetValue(alertId, out var alert);
                    return alert;
                }
            }
        }

        /// <inheritdoc />
        public bool AcknowledgeAlert(Guid alertId, string acknowledgedBy = "System", Guid correlationId = default)
        {
            if (!IsEnabled) return false;

            correlationId = EnsureCorrelationId(correlationId, "AcknowledgeAlert", alertId.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.AcknowledgeAlert"))
            {
                var startTime = DateTime.UtcNow;

                lock (_syncLock)
                {
                    if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    {
                        LogWarning($"Alert not found for acknowledgment: {alertId}", correlationId);
                        return false;
                    }

                    if (alert.IsAcknowledged)
                    {
                        LogDebug($"Alert already acknowledged: {alertId}", correlationId);
                        return false;
                    }

                    var acknowledgedAlert = alert.Acknowledge(acknowledgedBy);
                    _activeAlerts[alertId] = acknowledgedAlert;
                    _totalAcknowledged++;

                    // Track acknowledgment time
                    var acknowledgmentTime = DateTime.UtcNow - alert.Timestamp;
                    _acknowledgmentTimes.Add(acknowledgmentTime);
                    if (_acknowledgmentTimes.Count > 1000) // Keep last 1000 for average
                        _acknowledgmentTimes.RemoveAt(0);

                    // Publish acknowledgment message
                    PublishAlertAcknowledgedMessage(acknowledgedAlert, correlationId);

                    LogInfo($"Alert acknowledged: {alertId} by {acknowledgedBy}", correlationId);
                    return true;
                }
            }
        }

        /// <inheritdoc />
        public bool ResolveAlert(Guid alertId, string resolvedBy = "System", Guid correlationId = default)
        {
            if (!IsEnabled) return false;

            correlationId = EnsureCorrelationId(correlationId, "ResolveAlert", alertId.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.ResolveAlert"))
            {
                lock (_syncLock)
                {
                    if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    {
                        LogWarning($"Alert not found for resolution: {alertId}", correlationId);
                        return false;
                    }

                    if (alert.IsResolved)
                    {
                        LogDebug($"Alert already resolved: {alertId}", correlationId);
                        return false;
                    }

                    var resolvedAlert = alert.Resolve(resolvedBy);
                    _activeAlerts[alertId] = resolvedAlert;
                    _totalResolved++;

                    // Track resolution time
                    var resolutionTime = DateTime.UtcNow - alert.Timestamp;
                    _resolutionTimes.Add(resolutionTime);
                    if (_resolutionTimes.Count > 1000) // Keep last 1000 for average
                        _resolutionTimes.RemoveAt(0);

                    // Publish resolution message
                    PublishAlertResolvedMessage(resolvedAlert, correlationId);

                    LogInfo($"Alert resolved: {alertId} by {resolvedBy}", correlationId);
                    return true;
                }
            }
        }

        /// <inheritdoc />
        public int AcknowledgeAlerts(IEnumerable<Guid> alertIds, string acknowledgedBy = "System", Guid correlationId = default)
        {
            if (!IsEnabled || alertIds == null) return 0;

            correlationId = EnsureCorrelationId(correlationId, "AcknowledgeAlerts", "Bulk");

            using (_profilerService?.BeginScope("AlertStateManagement.AcknowledgeAlerts"))
            {
                var acknowledgedCount = 0;
                foreach (var alertId in alertIds.AsValueEnumerable())
                {
                    if (AcknowledgeAlert(alertId, acknowledgedBy, correlationId))
                        acknowledgedCount++;
                }

                LogInfo($"Bulk acknowledgment completed: {acknowledgedCount} alerts acknowledged by {acknowledgedBy}", correlationId);
                return acknowledgedCount;
            }
        }

        /// <inheritdoc />
        public int ResolveAlerts(IEnumerable<Guid> alertIds, string resolvedBy = "System", Guid correlationId = default)
        {
            if (!IsEnabled || alertIds == null) return 0;

            correlationId = EnsureCorrelationId(correlationId, "ResolveAlerts", "Bulk");

            using (_profilerService?.BeginScope("AlertStateManagement.ResolveAlerts"))
            {
                var resolvedCount = 0;
                foreach (var alertId in alertIds.AsValueEnumerable())
                {
                    if (ResolveAlert(alertId, resolvedBy, correlationId))
                        resolvedCount++;
                }

                LogInfo($"Bulk resolution completed: {resolvedCount} alerts resolved by {resolvedBy}", correlationId);
                return resolvedCount;
            }
        }

        /// <inheritdoc />
        public void SetSourceMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity, Guid correlationId = default)
        {
            if (!IsEnabled || source.IsEmpty) return;

            correlationId = EnsureCorrelationId(correlationId, "SetSourceMinimumSeverity", source.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.SetSourceMinimumSeverity"))
            {
                lock (_syncLock)
                {
                    _sourceSeverityOverrides[source] = minimumSeverity;
                }

                LogInfo($"Source minimum severity set: {source} -> {minimumSeverity}", correlationId);
            }
        }

        /// <inheritdoc />
        public AlertSeverity? GetSourceMinimumSeverity(FixedString64Bytes source)
        {
            if (!IsEnabled || source.IsEmpty) return null;

            lock (_syncLock)
            {
                return _sourceSeverityOverrides.TryGetValue(source, out var severity) ? severity : null;
            }
        }

        /// <inheritdoc />
        public bool RemoveSourceSeverityOverride(FixedString64Bytes source, Guid correlationId = default)
        {
            if (!IsEnabled || source.IsEmpty) return false;

            correlationId = EnsureCorrelationId(correlationId, "RemoveSourceSeverityOverride", source.ToString());

            using (_profilerService?.BeginScope("AlertStateManagement.RemoveSourceSeverityOverride"))
            {
                bool removed;
                lock (_syncLock)
                {
                    removed = _sourceSeverityOverrides.Remove(source);
                }

                if (removed)
                {
                    LogInfo($"Source severity override removed: {source}", correlationId);
                }

                return removed;
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString64Bytes, AlertSeverity> GetAllSourceSeverityOverrides()
        {
            if (!IsEnabled) return new Dictionary<FixedString64Bytes, AlertSeverity>();

            lock (_syncLock)
            {
                return new Dictionary<FixedString64Bytes, AlertSeverity>(_sourceSeverityOverrides);
            }
        }

        /// <inheritdoc />
        public AlertStateStatistics GetStatistics()
        {
            if (!IsEnabled) return AlertStateStatistics.Empty;

            lock (_syncLock)
            {
                var averageAcknowledgmentTime = _acknowledgmentTimes.Count > 0
                    ? TimeSpan.FromTicks((long)_acknowledgmentTimes.AsValueEnumerable().Select(t => t.Ticks).Average())
                    : TimeSpan.Zero;

                var averageResolutionTime = _resolutionTimes.Count > 0
                    ? TimeSpan.FromTicks((long)_resolutionTimes.AsValueEnumerable().Select(t => t.Ticks).Average())
                    : TimeSpan.Zero;

                return new AlertStateStatistics
                {
                    ActiveAlertCount = _activeAlerts.Count,
                    HistoryCount = _alertHistory.Count,
                    TotalAcknowledged = _totalAcknowledged,
                    TotalResolved = _totalResolved,
                    SourceSeverityOverrides = _sourceSeverityOverrides.Count,
                    AverageAcknowledgmentTime = averageAcknowledgmentTime,
                    AverageResolutionTime = averageResolutionTime,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc />
        public void PerformMaintenance(Guid correlationId = default)
        {
            if (!IsEnabled) return;

            correlationId = EnsureCorrelationId(correlationId, "PerformMaintenance", "System");

            using (_profilerService?.BeginScope("AlertStateManagement.PerformMaintenance"))
            {
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
        }

        /// <inheritdoc />
        public void ClearHistory(Guid correlationId = default)
        {
            if (!IsEnabled) return;

            correlationId = EnsureCorrelationId(correlationId, "ClearHistory", "System");

            using (_profilerService?.BeginScope("AlertStateManagement.ClearHistory"))
            {
                lock (_syncLock)
                {
                    var historyCount = _alertHistory.Count;
                    _alertHistory.Clear();
                    LogInfo($"Alert history cleared: {historyCount} entries removed", correlationId);
                }
            }
        }

        /// <inheritdoc />
        public ValidationResult ValidateState(Guid correlationId = default)
        {
            if (!IsEnabled) return ValidationResult.Failure(new[] { new ValidationError("Service is disabled") }, "AlertStateManagement");

            correlationId = EnsureCorrelationId(correlationId, "ValidateState", "System");

            using (_profilerService?.BeginScope("AlertStateManagement.ValidateState"))
            {
                var errors = new List<ValidationError>();

                lock (_syncLock)
                {
                    // Validate active alerts
                    foreach (var alert in _activeAlerts.Values)
                    {
                        if (alert == null)
                        {
                            errors.Add(new ValidationError("Null alert found in active alerts"));
                        }
                        else if (alert.Id == Guid.Empty)
                        {
                            errors.Add(new ValidationError("Alert with empty ID found"));
                        }
                    }

                    // Check for memory leaks
                    if (_alertHistory.Count > MaxHistorySize * 1.1) // 10% tolerance
                    {
                        errors.Add(new ValidationError($"Alert history size exceeds maximum: {_alertHistory.Count} > {MaxHistorySize}"));
                    }
                }

                var result = errors.Count > 0
                    ? ValidationResult.Failure(errors, "AlertStateManagement")
                    : ValidationResult.Success("AlertStateManagement");

                LogDebug($"State validation completed: {errors.Count} errors found", correlationId);
                return result;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed) return;

            _isEnabled = false;
            _isDisposed = true;

            lock (_syncLock)
            {
                _activeAlerts.Clear();
                _alertHistory.Clear();
                _sourceSeverityOverrides.Clear();
                _acknowledgmentTimes.Clear();
                _resolutionTimes.Clear();
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertStateManagement", "Disposal");
            LogInfo("Alert state management service disposed", correlationId);
        }

        #endregion

        #region Private Helper Methods

        private Guid EnsureCorrelationId(Guid correlationId, string operation, string context)
        {
            return correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId($"AlertStateManagement.{operation}", context)
                : correlationId;
        }

        private void PublishAlertAcknowledgedMessage(Alert alert, Guid correlationId)
        {
            try
            {
                var message = AlertAcknowledgedMessage.Create(alert, "AlertStateManagementService", correlationId);
                _messageBusService?.PublishMessageAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertAcknowledgedMessage: {ex.Message}", correlationId);
            }
        }

        private void PublishAlertResolvedMessage(Alert alert, Guid correlationId)
        {
            try
            {
                var message = AlertResolvedMessage.Create(alert, "AlertStateManagementService", correlationId);
                _messageBusService?.PublishMessageAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertResolvedMessage: {ex.Message}", correlationId);
            }
        }

        private void LogDebug(string message, Guid correlationId)
        {
            _loggingService?.LogDebug($"[AlertStateManagementService] {message}", correlationId.ToString(), "AlertStateManagementService");
        }

        private void LogInfo(string message, Guid correlationId)
        {
            _loggingService?.LogInfo($"[AlertStateManagementService] {message}", correlationId.ToString(), "AlertStateManagementService");
        }

        private void LogWarning(string message, Guid correlationId)
        {
            _loggingService?.LogWarning($"[AlertStateManagementService] {message}", correlationId.ToString(), "AlertStateManagementService");
        }

        private void LogError(string message, Guid correlationId)
        {
            _loggingService?.LogError($"[AlertStateManagementService] {message}", correlationId.ToString(), "AlertStateManagementService");
        }

        #endregion
    }
}