using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZLinq;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health degradation management.
    /// Monitors system health and automatically manages feature availability based on degradation levels.
    /// </summary>
    public sealed class HealthDegradationManager : IHealthDegradationManager
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBus;
        private readonly IProfilerService _profilerService;
        private readonly DegradationThresholds _thresholds;
        private readonly HealthCheckServiceConfig _config;

        private readonly ProfilerMarker _evaluationMarker = new ProfilerMarker("HealthDegradationManager.Evaluate");
        private readonly Guid _managerId;
        private readonly object _stateLock = new();

        // State management
        private DegradationLevel _currentLevel;
        private DateTime _levelSetTime;
        private string _currentLevelReason;
        private bool _automaticDegradationEnabled;
        private bool _disposed;

        // Feature management
        private readonly ConcurrentDictionary<string, ManagedFeature> _managedFeatures;
        private readonly ConcurrentDictionary<string, bool> _featureStates;

        // History and statistics
        private readonly ConcurrentQueue<DegradationLevelChange> _degradationHistory;
        private readonly ConcurrentDictionary<DegradationLevel, TimeSpan> _levelDurations;
        private DateTime _lastEvaluationTime;
        private int _totalLevelChanges;
        private int _automaticChanges;
        private int _manualChanges;

        /// <summary>
        /// Event triggered when the degradation level changes.
        /// </summary>
        public event EventHandler<DegradationLevelChangedEventArgs> DegradationLevelChanged;

        /// <summary>
        /// Event triggered when a feature is automatically enabled or disabled.
        /// </summary>
        public event EventHandler<FeatureToggleEventArgs> FeatureToggled;

        /// <summary>
        /// Gets the current system degradation level.
        /// </summary>
        public DegradationLevel CurrentLevel
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentLevel;
                }
            }
        }

        /// <summary>
        /// Gets the timestamp when the current degradation level was set.
        /// </summary>
        public DateTime LevelSetTime
        {
            get
            {
                lock (_stateLock)
                {
                    return _levelSetTime;
                }
            }
        }

        /// <summary>
        /// Gets the duration the system has been at the current degradation level.
        /// </summary>
        public TimeSpan TimeInCurrentLevel => DateTime.UtcNow - LevelSetTime;

        /// <summary>
        /// Gets the reason for the current degradation level.
        /// </summary>
        public string CurrentLevelReason
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentLevelReason;
                }
            }
        }

        /// <summary>
        /// Gets whether automatic degradation is enabled.
        /// </summary>
        public bool IsAutomaticDegradationEnabled
        {
            get
            {
                lock (_stateLock)
                {
                    return _automaticDegradationEnabled;
                }
            }
        }

        /// <summary>
        /// Initializes a new health degradation manager.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="alertService">Alert service</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="profilerService">Profiler service</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public HealthDegradationManager(
            HealthCheckServiceConfig config,
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBus,
            IProfilerService profilerService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));

            _thresholds = _config.DegradationThresholds;
            _managerId = DeterministicIdGenerator.GenerateHealthCheckId("HealthDegradationManager", Environment.MachineName);

            // Initialize state
            _currentLevel = DegradationLevel.None;
            _levelSetTime = DateTime.UtcNow;
            _currentLevelReason = "Initial state";
            _automaticDegradationEnabled = _config.EnableAutomaticDegradation;
            _lastEvaluationTime = DateTime.UtcNow;

            // Initialize collections
            _managedFeatures = new ConcurrentDictionary<string, ManagedFeature>();
            _featureStates = new ConcurrentDictionary<string, bool>();
            _degradationHistory = new ConcurrentQueue<DegradationLevelChange>();
            _levelDurations = new ConcurrentDictionary<DegradationLevel, TimeSpan>();

            // Initialize level duration tracking
            foreach (DegradationLevel level in Enum.GetValues(typeof(DegradationLevel)))
            {
                _levelDurations[level] = TimeSpan.Zero;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("DegradationManagerInit", _managerId.ToString());
            _logger.LogInfo($"HealthDegradationManager initialized with automatic degradation: {_automaticDegradationEnabled}", correlationId);
        }

        /// <summary>
        /// Evaluates health check results and updates degradation level if necessary.
        /// </summary>
        /// <param name="healthReport">Latest health report to evaluate</param>
        /// <param name="reason">Optional reason for the evaluation</param>
        /// <returns>True if degradation level changed, false otherwise</returns>
        public bool EvaluateAndUpdateDegradationLevel(HealthReport healthReport, string reason = null)
        {
            ThrowIfDisposed();

            if (healthReport == null)
                throw new ArgumentNullException(nameof(healthReport));

            if (!_automaticDegradationEnabled)
                return false;

            using (_evaluationMarker.Auto())
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("DegradationEvaluation", _managerId.ToString());
                _lastEvaluationTime = DateTime.UtcNow;

                try
                {
                    var triggerMetrics = CalculateTriggerMetrics(healthReport);
                    var newLevel = DetermineDegradationLevel(triggerMetrics);
                    var evaluationReason = reason ?? "Automatic evaluation";

                    if (newLevel != _currentLevel)
                    {
                        var changed = InternalSetDegradationLevel(newLevel, evaluationReason, true, triggerMetrics, correlationId);
                        if (changed)
                        {
                            _logger.LogInfo($"Degradation level automatically changed from {_currentLevel} to {newLevel}: {evaluationReason}", correlationId);
                        }
                        return changed;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogException("Error evaluating degradation level", ex,correlationId.ToString());
                    return false;
                }
            }
        }

        /// <summary>
        /// Manually sets the system degradation level.
        /// </summary>
        /// <param name="level">Degradation level to set</param>
        /// <param name="reason">Reason for the manual change</param>
        /// <returns>True if level was changed, false if already at that level</returns>
        public bool SetDegradationLevel(DegradationLevel level, string reason)
        {
            ThrowIfDisposed();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ManualDegradationChange", _managerId.ToString());
            var manualReason = $"Manual: {reason ?? "No reason provided"}";

            var changed = InternalSetDegradationLevel(level, manualReason, false, null, correlationId);
            if (changed)
            {
                _logger.LogInfo($"Degradation level manually changed to {level}: {reason}", correlationId);
            }

            return changed;
        }

        /// <summary>
        /// Registers a feature that should be managed based on degradation level.
        /// </summary>
        /// <param name="featureName">Name of the feature</param>
        /// <param name="minimumLevel">Minimum degradation level at which feature should be disabled</param>
        /// <param name="isEssential">Whether this is an essential feature that should only be disabled at severe levels</param>
        public void RegisterManagedFeature(string featureName, DegradationLevel minimumLevel, bool isEssential = false)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(featureName))
                throw new ArgumentException("Feature name cannot be null or empty", nameof(featureName));

            var feature = new ManagedFeature(featureName, minimumLevel, isEssential);
            _managedFeatures.AddOrUpdate(featureName, feature, (_, _) => feature);

            // Determine initial state based on current degradation level
            var isEnabled = ShouldFeatureBeEnabled(feature, _currentLevel);
            _featureStates.AddOrUpdate(featureName, isEnabled, (_, _) => isEnabled);

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("FeatureRegistration", _managerId.ToString());
            _logger.LogInfo($"Registered managed feature '{featureName}' (minimum level: {minimumLevel}, essential: {isEssential}, enabled: {isEnabled})", correlationId);
        }

        /// <summary>
        /// Unregisters a managed feature.
        /// </summary>
        /// <param name="featureName">Name of the feature to unregister</param>
        /// <returns>True if feature was found and removed</returns>
        public bool UnregisterManagedFeature(string featureName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(featureName))
                return false;

            var removed = _managedFeatures.TryRemove(featureName, out _);
            _featureStates.TryRemove(featureName, out _);

            if (removed)
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("FeatureUnregistration", _managerId.ToString());
                _logger.LogInfo($"Unregistered managed feature '{featureName}'", correlationId);
            }

            return removed;
        }

        /// <summary>
        /// Checks if a specific feature is currently enabled based on degradation level.
        /// </summary>
        /// <param name="featureName">Name of the feature to check</param>
        /// <returns>True if feature is enabled, false if disabled or not found</returns>
        public bool IsFeatureEnabled(string featureName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(featureName))
                return false;

            return _featureStates.TryGetValue(featureName, out var isEnabled) && isEnabled;
        }

        /// <summary>
        /// Gets all currently enabled features.
        /// </summary>
        /// <returns>Collection of enabled feature names</returns>
        public IEnumerable<string> GetEnabledFeatures()
        {
            ThrowIfDisposed();

            return _featureStates.AsValueEnumerable()
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets all currently disabled features.
        /// </summary>
        /// <returns>Collection of disabled feature names</returns>
        public IEnumerable<string> GetDisabledFeatures()
        {
            ThrowIfDisposed();

            return _featureStates.AsValueEnumerable()
                .Where(kvp => !kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Forces a re-evaluation of all managed features based on current degradation level.
        /// </summary>
        /// <param name="reason">Reason for the re-evaluation</param>
        public void RefreshFeatureStates(string reason = null)
        {
            ThrowIfDisposed();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("FeatureRefresh", _managerId.ToString());
            var refreshReason = reason ?? "Manual refresh";

            foreach (var kvp in _managedFeatures)
            {
                var feature = kvp.Value;
                var currentState = _featureStates.GetOrAdd(feature.Name, true);
                var newState = ShouldFeatureBeEnabled(feature, _currentLevel);

                if (currentState != newState)
                {
                    _featureStates[feature.Name] = newState;
                    FireFeatureToggleEvent(feature.Name, newState, _currentLevel, refreshReason, correlationId);
                }
            }

            _logger.LogInfo($"Refreshed feature states for {_managedFeatures.Count} features: {refreshReason}", correlationId);
        }

        /// <summary>
        /// Gets degradation history for analysis.
        /// </summary>
        /// <param name="period">Time period to retrieve history for</param>
        /// <returns>Collection of degradation level changes</returns>
        public IEnumerable<DegradationLevelChange> GetDegradationHistory(TimeSpan period)
        {
            ThrowIfDisposed();

            var cutoffTime = DateTime.UtcNow - period;
            return _degradationHistory.AsValueEnumerable()
                .Where(change => change.Timestamp >= cutoffTime)
                .OrderByDescending(change => change.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Gets statistics about degradation patterns.
        /// </summary>
        /// <returns>Degradation statistics</returns>
        public DegradationStatistics GetStatistics()
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                // Update current level duration
                var timeAtCurrentLevel = TimeInCurrentLevel;
                var updatedDurations = new Dictionary<DegradationLevel, TimeSpan>(_levelDurations);
                updatedDurations[_currentLevel] = _levelDurations[_currentLevel].Add(timeAtCurrentLevel);

                return new DegradationStatistics
                {
                    CurrentLevel = _currentLevel,
                    TimeAtCurrentLevel = timeAtCurrentLevel,
                    TotalLevelChanges = _totalLevelChanges,
                    AutomaticChanges = _automaticChanges,
                    ManualChanges = _manualChanges,
                    ManagedFeatures = _managedFeatures.Count,
                    EnabledFeatures = _featureStates.Values.Count(enabled => enabled),
                    DisabledFeatures = _featureStates.Values.Count(enabled => !enabled),
                    TimeDistribution = new ReadOnlyDictionary<DegradationLevel, TimeSpan>(updatedDurations),
                    AutomaticDegradationEnabled = _automaticDegradationEnabled,
                    LastEvaluation = _lastEvaluationTime,
                    CurrentLevelReason = _currentLevelReason
                };
            }
        }

        /// <summary>
        /// Enables or disables automatic degradation management.
        /// </summary>
        /// <param name="enabled">Whether to enable automatic degradation</param>
        /// <param name="reason">Reason for the change</param>
        public void SetAutomaticDegradation(bool enabled, string reason = null)
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (_automaticDegradationEnabled == enabled)
                    return;

                _automaticDegradationEnabled = enabled;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AutoDegradationToggle", _managerId.ToString());
            _logger.LogInfo($"Automatic degradation {(enabled ? "enabled" : "disabled")}: {reason ?? "No reason provided"}", correlationId);
        }

        private bool InternalSetDegradationLevel(
            DegradationLevel newLevel, 
            string reason, 
            bool isAutomatic, 
            DegradationTriggerMetrics triggerMetrics, 
            Guid correlationId)
        {
            DegradationLevel oldLevel;
            DateTime levelChangeTime;
            TimeSpan durationAtPreviousLevel;

            lock (_stateLock)
            {
                if (_currentLevel == newLevel)
                    return false;

                oldLevel = _currentLevel;
                levelChangeTime = DateTime.UtcNow;
                durationAtPreviousLevel = levelChangeTime - _levelSetTime;

                // Update level duration tracking
                _levelDurations[_currentLevel] = _levelDurations[_currentLevel].Add(durationAtPreviousLevel);

                // Update state
                _currentLevel = newLevel;
                _levelSetTime = levelChangeTime;
                _currentLevelReason = reason;

                // Update statistics
                _totalLevelChanges++;
                if (isAutomatic)
                    _automaticChanges++;
                else
                    _manualChanges++;
            }

            // Record the change in history
            var levelChange = new DegradationLevelChange
            {
                Timestamp = levelChangeTime,
                FromLevel = oldLevel,
                ToLevel = newLevel,
                Reason = reason,
                IsAutomatic = isAutomatic,
                DurationAtPreviousLevel = durationAtPreviousLevel,
                TriggerMetrics = triggerMetrics
            };

            _degradationHistory.Enqueue(levelChange);

            // Limit history size
            while (_degradationHistory.Count > 1000)
            {
                _degradationHistory.TryDequeue(out _);
            }

            // Update feature states based on new degradation level
            UpdateFeatureStatesForNewLevel(newLevel, reason, correlationId);

            // Fire events
            var eventArgs = new DegradationLevelChangedEventArgs
            {
                OldLevel = oldLevel,
                NewLevel = newLevel,
                Reason = reason,
                IsAutomatic = isAutomatic,
                CorrelationId = correlationId,
                TriggerMetrics = triggerMetrics
            };

            DegradationLevelChanged?.Invoke(this, eventArgs);

            // Send alert if degradation worsened
            if (newLevel > oldLevel)
            {
                var severity = newLevel switch
                {
                    DegradationLevel.Minor => Alerting.Models.AlertSeverity.Warning,
                    DegradationLevel.Moderate => Alerting.Models.AlertSeverity.Warning,
                    DegradationLevel.Severe => Alerting.Models.AlertSeverity.Critical,
                    DegradationLevel.Disabled => Alerting.Models.AlertSeverity.Emergency,
                    _ => Alerting.Models.AlertSeverity.Info
                };

                _alertService.RaiseAlert(
                    $"System degradation level increased to {newLevel}: {reason}",
                    severity,
                    "HealthDegradationManager",
                    "DegradationLevelChange",
                    correlationId);
            }

            return true;
        }

        private void UpdateFeatureStatesForNewLevel(DegradationLevel newLevel, string reason, Guid correlationId)
        {
            foreach (var kvp in _managedFeatures)
            {
                var feature = kvp.Value;
                var currentState = _featureStates.GetOrAdd(feature.Name, true);
                var newState = ShouldFeatureBeEnabled(feature, newLevel);

                if (currentState != newState)
                {
                    _featureStates[feature.Name] = newState;
                    FireFeatureToggleEvent(feature.Name, newState, newLevel, reason, correlationId);
                }
            }
        }

        private void FireFeatureToggleEvent(string featureName, bool isEnabled, DegradationLevel triggeringLevel, string reason, Guid correlationId)
        {
            var eventArgs = new FeatureToggleEventArgs
            {
                FeatureName = featureName,
                IsEnabled = isEnabled,
                TriggeringLevel = triggeringLevel,
                Reason = reason,
                CorrelationId = correlationId
            };

            FeatureToggled?.Invoke(this, eventArgs);

            _logger.LogInfo($"Feature '{featureName}' {(isEnabled ? "enabled" : "disabled")} due to degradation level {triggeringLevel}", correlationId);
        }

        private static bool ShouldFeatureBeEnabled(ManagedFeature feature, DegradationLevel currentLevel)
        {
            // Essential features are only disabled at severe levels or higher
            if (feature.IsEssential)
            {
                return currentLevel < DegradationLevel.Severe;
            }

            // Non-essential features are disabled when current level reaches their minimum level
            return currentLevel < feature.MinimumLevel;
        }

        private DegradationTriggerMetrics CalculateTriggerMetrics(HealthReport healthReport)
        {
            var totalChecks = healthReport.TotalChecks;
            var failedChecks = healthReport.UnhealthyCount;
            var failureRate = totalChecks > 0 ? (double)failedChecks / totalChecks : 0.0;

            // Count critical failures (could be enhanced based on health check categories)
            var criticalFailures = healthReport.Results.AsValueEnumerable()
                .Count(r => r.Status == HealthStatus.Unhealthy && r.Category == HealthCheckCategory.System);

            return new DegradationTriggerMetrics
            {
                TotalHealthChecks = totalChecks,
                FailedHealthChecks = failedChecks,
                FailureRate = failureRate,
                CriticalFailures = criticalFailures,
                ExceededThreshold = failureRate,
                EvaluationWindow = _thresholds.EvaluationWindow
            };
        }

        private DegradationLevel DetermineDegradationLevel(DegradationTriggerMetrics metrics)
        {
            var failureRate = metrics.FailureRate;

            // Apply critical check weighting if enabled
            if (_thresholds.WeightCriticalChecks && metrics.CriticalFailures > 0)
            {
                var weightedFailures = metrics.FailedHealthChecks + 
                    (metrics.CriticalFailures * (_thresholds.CriticalCheckWeight - 1.0));
                failureRate = metrics.TotalHealthChecks > 0 ? weightedFailures / metrics.TotalHealthChecks : 0.0;
            }

            // Determine level based on thresholds
            if (failureRate >= _thresholds.DisabledThreshold)
                return DegradationLevel.Disabled;
            if (failureRate >= _thresholds.SevereThreshold)
                return DegradationLevel.Severe;
            if (failureRate >= _thresholds.ModerateThreshold)
                return DegradationLevel.Moderate;
            if (failureRate >= _thresholds.MinorThreshold)
                return DegradationLevel.Minor;

            // Check for recovery
            var successRate = 1.0 - failureRate;
            if (_currentLevel != DegradationLevel.None && successRate >= _thresholds.RecoveryThreshold)
            {
                // Allow recovery to one level better
                return _currentLevel switch
                {
                    DegradationLevel.Disabled => DegradationLevel.Severe,
                    DegradationLevel.Severe => DegradationLevel.Moderate,
                    DegradationLevel.Moderate => DegradationLevel.Minor,
                    DegradationLevel.Minor => DegradationLevel.None,
                    _ => DegradationLevel.None
                };
            }

            return DegradationLevel.None;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthDegradationManager));
        }

        /// <summary>
        /// Disposes the health degradation manager.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("DegradationManagerDispose", _managerId.ToString());
            _logger.LogInfo("HealthDegradationManager disposed", correlationId);
        }

        /// <summary>
        /// Represents a managed feature configuration.
        /// </summary>
        private sealed record ManagedFeature(string Name, DegradationLevel MinimumLevel, bool IsEssential);
    }
}