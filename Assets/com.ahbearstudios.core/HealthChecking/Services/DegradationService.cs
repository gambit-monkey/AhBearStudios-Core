using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service responsible for managing graceful degradation of system functionality
    /// </summary>
    /// <remarks>
    /// Implements comprehensive graceful degradation management including automatic degradation
    /// based on health status, manual degradation control, and recovery management with
    /// configurable thresholds and policies
    /// </remarks>
    public sealed class DegradationService : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly DegradationThresholds _degradationThresholds;
        private readonly DegradationConfig _degradationConfig;

        private readonly ConcurrentDictionary<FixedString64Bytes, SystemDegradationState> _systemStates;
        private readonly ConcurrentDictionary<FixedString64Bytes, DegradationRule> _degradationRules;
        private readonly ConcurrentDictionary<FixedString64Bytes, RecoveryTracker> _recoveryTrackers;
        private readonly ReaderWriterLockSlim _degradationLock;

        private Timer _degradationEvaluationTimer;
        private Timer _recoveryEvaluationTimer;
        private DegradationLevel _overallDegradationLevel = DegradationLevel.None;
        private DateTime _lastDegradationChange = DateTime.UtcNow;
        private bool _disposed;

        /// <summary>
        /// Occurs when degradation level changes for a system
        /// </summary>
        public event EventHandler<DegradationChangedEventArgs> DegradationChanged;

        /// <summary>
        /// Occurs when overall degradation level changes
        /// </summary>
        public event EventHandler<OverallDegradationChangedEventArgs> OverallDegradationChanged;

        /// <summary>
        /// Occurs when automatic recovery is initiated
        /// </summary>
        public event EventHandler<RecoveryInitiatedEventArgs> RecoveryInitiated;

        /// <summary>
        /// Occurs when recovery is completed
        /// </summary>
        public event EventHandler<RecoveryCompletedEventArgs> RecoveryCompleted;

        /// <summary>
        /// Initializes the degradation service with required dependencies
        /// </summary>
        /// <param name="logger">Logging service for degradation operations</param>
        /// <param name="alertService">Alert service for degradation notifications</param>
        /// <param name="messageBusService">Message bus for publishing degradation events</param>
        /// <param name="degradationThresholds">Degradation thresholds configuration</param>
        /// <param name="degradationConfig">Degradation behavior configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public DegradationService(
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBusService,
            DegradationThresholds degradationThresholds,
            DegradationConfig degradationConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _degradationThresholds =
                degradationThresholds ?? throw new ArgumentNullException(nameof(degradationThresholds));
            _degradationConfig = degradationConfig ?? throw new ArgumentNullException(nameof(degradationConfig));

            _systemStates = new ConcurrentDictionary<FixedString64Bytes, SystemDegradationState>();
            _degradationRules = new ConcurrentDictionary<FixedString64Bytes, DegradationRule>();
            _recoveryTrackers = new ConcurrentDictionary<FixedString64Bytes, RecoveryTracker>();
            _degradationLock = new ReaderWriterLockSlim();

            ValidateConfigurationOrThrow();
            InitializeDefaultRules();
            InitializeTimers();

            _logger.LogInfo("DegradationService initialized with graceful degradation management");
        }

        /// <summary>
        /// Enables graceful degradation for a specific system
        /// </summary>
        /// <param name="systemName">Name of the system to degrade</param>
        /// <param name="degradationLevel">Level of degradation to apply</param>
        /// <param name="reason">Reason for degradation</param>
        /// <param name="isAutomatic">Whether this is an automatic degradation</param>
        /// <exception cref="ArgumentException">Thrown when system name is invalid</exception>
        public void EnableGracefulDegradation(
            FixedString64Bytes systemName,
            DegradationLevel degradationLevel,
            string reason = null,
            bool isAutomatic = false)
        {
            if (systemName.IsEmpty)
                throw new ArgumentException("System name cannot be empty", nameof(systemName));

            ThrowIfDisposed();

            try
            {
                _degradationLock.EnterWriteLock();
                try
                {
                    var currentState = _systemStates.GetOrAdd(systemName, _ => CreateNewSystemState(systemName));
                    var previousLevel = currentState.CurrentLevel;

                    // Check if degradation level is actually changing
                    if (currentState.CurrentLevel == degradationLevel)
                    {
                        _logger.LogDebug($"System {systemName} already at degradation level {degradationLevel}");
                        return;
                    }

                    // Apply degradation with escalation delay if configured
                    if (ShouldApplyEscalationDelay(currentState, degradationLevel))
                    {
                        ScheduleDegradationChange(systemName, degradationLevel, reason, isAutomatic);
                        return;
                    }

                    // Apply degradation immediately
                    ApplyDegradationToSystem(currentState, degradationLevel, reason, isAutomatic);

                    // Update overall degradation level
                    UpdateOverallDegradationLevel();

                    // Trigger events and notifications
                    OnDegradationChanged(systemName, previousLevel, degradationLevel, reason, isAutomatic);

                    _logger.LogInfo($"Applied {degradationLevel} degradation to {systemName}" +
                                    (string.IsNullOrEmpty(reason) ? "" : $" - Reason: {reason}"));
                }
                finally
                {
                    _degradationLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to enable degradation for system {systemName}");
                throw;
            }
        }

        /// <summary>
        /// Disables graceful degradation for a specific system
        /// </summary>
        /// <param name="systemName">Name of the system to restore</param>
        /// <param name="reason">Reason for restoration</param>
        /// <param name="isAutomatic">Whether this is an automatic recovery</param>
        /// <returns>True if degradation was disabled, false if system was not degraded</returns>
        public bool DisableGracefulDegradation(
            FixedString64Bytes systemName,
            string reason = null,
            bool isAutomatic = false)
        {
            if (systemName.IsEmpty)
                throw new ArgumentException("System name cannot be empty", nameof(systemName));

            ThrowIfDisposed();

            try
            {
                _degradationLock.EnterWriteLock();
                try
                {
                    if (!_systemStates.TryGetValue(systemName, out var currentState))
                    {
                        _logger.LogDebug($"No degradation state found for system {systemName}");
                        return false;
                    }

                    if (currentState.CurrentLevel == DegradationLevel.None)
                    {
                        _logger.LogDebug($"System {systemName} is not currently degraded");
                        return false;
                    }

                    var previousLevel = currentState.CurrentLevel;

                    // Apply de-escalation delay if configured
                    if (ShouldApplyDeEscalationDelay(currentState))
                    {
                        ScheduleRecovery(systemName, reason, isAutomatic);
                        return true;
                    }

                    // Restore system immediately
                    RestoreSystemFromDegradation(currentState, reason, isAutomatic);

                    // Update overall degradation level
                    UpdateOverallDegradationLevel();

                    // Trigger events and notifications
                    OnDegradationChanged(systemName, previousLevel, DegradationLevel.None, reason, isAutomatic);

                    _logger.LogInfo($"Restored {systemName} from {previousLevel} degradation" +
                                    (string.IsNullOrEmpty(reason) ? "" : $" - Reason: {reason}"));

                    return true;
                }
                finally
                {
                    _degradationLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to disable degradation for system {systemName}");
                return false;
            }
        }

        /// <summary>
        /// Gets the current degradation status for all systems
        /// </summary>
        /// <returns>Dictionary of system names and their degradation levels</returns>
        public Dictionary<string, DegradationLevel> GetDegradationStatus()
        {
            ThrowIfDisposed();

            try
            {
                _degradationLock.EnterReadLock();
                try
                {
                    return _systemStates.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value.CurrentLevel);
                }
                finally
                {
                    _degradationLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to get degradation status");
                return new Dictionary<string, DegradationLevel>();
            }
        }

        /// <summary>
        /// Gets the current overall degradation level
        /// </summary>
        /// <returns>Overall degradation level across all systems</returns>
        public DegradationLevel GetOverallDegradationLevel()
        {
            ThrowIfDisposed();
            return _overallDegradationLevel;
        }

        /// <summary>
        /// Gets detailed degradation information for a specific system
        /// </summary>
        /// <param name="systemName">Name of the system</param>
        /// <returns>Detailed degradation information</returns>
        public SystemDegradationInfo GetSystemDegradationInfo(FixedString64Bytes systemName)
        {
            ThrowIfDisposed();

            try
            {
                _degradationLock.EnterReadLock();
                try
                {
                    if (_systemStates.TryGetValue(systemName, out var state))
                    {
                        return new SystemDegradationInfo
                        {
                            SystemName = systemName.ToString(),
                            CurrentLevel = state.CurrentLevel,
                            PreviousLevel = state.PreviousLevel,
                            DegradationStartTime = state.DegradationStartTime,
                            LastLevelChange = state.LastLevelChange,
                            Reason = state.Reason,
                            IsAutomatic = state.IsAutomatic,
                            DisabledFeatures = state.DisabledFeatures.ToList(),
                            DegradedServices = state.DegradedServices.ToList(),
                            RecoveryInProgress = _recoveryTrackers.ContainsKey(systemName)
                        };
                    }

                    return new SystemDegradationInfo
                    {
                        SystemName = systemName.ToString(),
                        CurrentLevel = DegradationLevel.None
                    };
                }
                finally
                {
                    _degradationLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to get degradation info for system {systemName}");
                return new SystemDegradationInfo
                {
                    SystemName = systemName.ToString(),
                    CurrentLevel = DegradationLevel.None
                };
            }
        }

        /// <summary>
        /// Evaluates health data and applies automatic degradation based on thresholds
        /// </summary>
        /// <param name="healthData">Current health check results</param>
        public void EvaluateHealthAndApplyDegradation(IEnumerable<HealthCheckResult> healthData)
        {
            if (healthData == null)
                throw new ArgumentNullException(nameof(healthData));

            ThrowIfDisposed();

            if (!_degradationThresholds.Enabled)
            {
                _logger.LogDebug("Automatic degradation is disabled");
                return;
            }

            try
            {
                var healthResults = healthData.ToList();
                var degradationLevel = CalculateDegradationLevelFromHealth(healthResults);

                if (degradationLevel != _overallDegradationLevel)
                {
                    ApplyOverallDegradation(degradationLevel, "Automatic degradation based on health thresholds");
                }

                // Evaluate individual system degradation if configured
                if (_degradationConfig.EnableIndividualSystemDegradation)
                {
                    EvaluateIndividualSystemDegradation(healthResults);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to evaluate health and apply degradation");
            }
        }

        /// <summary>
        /// Registers a custom degradation rule for a system
        /// </summary>
        /// <param name="systemName">Name of the system</param>
        /// <param name="rule">Degradation rule to register</param>
        /// <exception cref="ArgumentNullException">Thrown when rule is null</exception>
        public void RegisterDegradationRule(FixedString64Bytes systemName, DegradationRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            ThrowIfDisposed();

            try
            {
                _degradationRules.AddOrUpdate(systemName, rule, (_, _) => rule);
                _logger.LogInfo($"Registered degradation rule for system {systemName}");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to register degradation rule for system {systemName}");
                throw;
            }
        }

        /// <summary>
        /// Initiates manual recovery for a system
        /// </summary>
        /// <param name="systemName">Name of the system to recover</param>
        /// <param name="reason">Reason for recovery</param>
        /// <returns>True if recovery was initiated</returns>
        public bool InitiateRecovery(FixedString64Bytes systemName, string reason = null)
        {
            ThrowIfDisposed();

            try
            {
                _degradationLock.EnterReadLock();
                try
                {
                    if (!_systemStates.ContainsKey(systemName))
                    {
                        _logger.LogWarning($"Cannot initiate recovery for unknown system {systemName}");
                        return false;
                    }

                    if (_recoveryTrackers.ContainsKey(systemName))
                    {
                        _logger.LogWarning($"Recovery already in progress for system {systemName}");
                        return false;
                    }
                }
                finally
                {
                    _degradationLock.ExitReadLock();
                }

                StartRecoveryProcess(systemName, reason, false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to initiate recovery for system {systemName}");
                return false;
            }
        }

        #region Private Implementation

        private void ValidateConfigurationOrThrow()
        {
            var thresholdErrors = _degradationThresholds.Validate();
            var configErrors = _degradationConfig.Validate();

            var allErrors = thresholdErrors.Concat(configErrors).ToList();

            if (allErrors.Count > 0)
            {
                var errorMessage = $"Invalid degradation configuration: {string.Join(", ", allErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private void InitializeDefaultRules()
        {
            // Initialize default degradation rules for common system types
            var defaultRules = new Dictionary<string, DegradationRule>
            {
                ["DatabaseSystem"] = CreateDatabaseDegradationRule(),
                ["NetworkSystem"] = CreateNetworkDegradationRule(),
                ["CacheSystem"] = CreateCacheDegradationRule(),
                ["MessageBusSystem"] = CreateMessageBusDegradationRule()
            };

            foreach (var kvp in defaultRules)
            {
                var systemName = new FixedString64Bytes(kvp.Key);
                _degradationRules.TryAdd(systemName, kvp.Value);
            }

            _logger.LogDebug($"Initialized {defaultRules.Count} default degradation rules");
        }

        private void InitializeTimers()
        {
            if (_degradationConfig.EnableAutomaticEvaluation)
            {
                _degradationEvaluationTimer = new Timer(
                    EvaluateDegradationStates,
                    null,
                    _degradationConfig.EvaluationInterval,
                    _degradationConfig.EvaluationInterval);
            }

            if (_degradationThresholds.EnableAutoRecovery)
            {
                _recoveryEvaluationTimer = new Timer(
                    EvaluateRecoveryStates,
                    null,
                    _degradationThresholds.RecoveryWindow,
                    _degradationThresholds.RecoveryWindow);
            }

            _logger.LogDebug("Initialized degradation service timers");
        }

        private SystemDegradationState CreateNewSystemState(FixedString64Bytes systemName)
        {
            return new SystemDegradationState
            {
                SystemName = systemName,
                CurrentLevel = DegradationLevel.None,
                PreviousLevel = DegradationLevel.None,
                DegradationStartTime = null,
                LastLevelChange = DateTime.UtcNow,
                Reason = string.Empty,
                IsAutomatic = false,
                DisabledFeatures = new HashSet<FixedString64Bytes>(),
                DegradedServices = new HashSet<FixedString64Bytes>()
            };
        }

        private bool ShouldApplyEscalationDelay(SystemDegradationState currentState, DegradationLevel newLevel)
        {
            if (!_degradationThresholds.EscalationDelay.HasValue ||
                _degradationThresholds.EscalationDelay == TimeSpan.Zero)
                return false;

            // Apply delay only when escalating degradation level
            return newLevel > currentState.CurrentLevel;
        }

        private bool ShouldApplyDeEscalationDelay(SystemDegradationState currentState)
        {
            if (!_degradationThresholds.DeEscalationDelay.HasValue ||
                _degradationThresholds.DeEscalationDelay == TimeSpan.Zero)
                return false;

            // Apply delay when improving from degraded state
            return currentState.CurrentLevel > DegradationLevel.None;
        }

        private void ScheduleDegradationChange(
            FixedString64Bytes systemName,
            DegradationLevel degradationLevel,
            string reason,
            bool isAutomatic)
        {
            Task.Delay(_degradationThresholds.EscalationDelay ?? TimeSpan.Zero)
                .ContinueWith(_ =>
                {
                    if (!_disposed)
                    {
                        EnableGracefulDegradation(systemName, degradationLevel, reason, isAutomatic);
                    }
                });

            _logger.LogDebug($"Scheduled degradation change for {systemName} to {degradationLevel} " +
                             $"in {_degradationThresholds.EscalationDelay}");
        }

        private void ScheduleRecovery(FixedString64Bytes systemName, string reason, bool isAutomatic)
        {
            Task.Delay(_degradationThresholds.DeEscalationDelay ?? TimeSpan.Zero)
                .ContinueWith(_ =>
                {
                    if (!_disposed)
                    {
                        DisableGracefulDegradation(systemName, reason, isAutomatic);
                    }
                });

            _logger.LogDebug($"Scheduled recovery for {systemName} " +
                             $"in {_degradationThresholds.DeEscalationDelay}");
        }

        private void ApplyDegradationToSystem(
            SystemDegradationState state,
            DegradationLevel degradationLevel,
            string reason,
            bool isAutomatic)
        {
            state.PreviousLevel = state.CurrentLevel;
            state.CurrentLevel = degradationLevel;
            state.LastLevelChange = DateTime.UtcNow;
            state.Reason = reason ?? string.Empty;
            state.IsAutomatic = isAutomatic;

            if (state.DegradationStartTime == null && degradationLevel != DegradationLevel.None)
            {
                state.DegradationStartTime = DateTime.UtcNow;
            }
            else if (degradationLevel == DegradationLevel.None)
            {
                state.DegradationStartTime = null;
            }

            // Apply degradation configuration based on level
            ApplyDegradationConfiguration(state, degradationLevel);
        }

        private void RestoreSystemFromDegradation(SystemDegradationState state, string reason, bool isAutomatic)
        {
            state.PreviousLevel = state.CurrentLevel;
            state.CurrentLevel = DegradationLevel.None;
            state.LastLevelChange = DateTime.UtcNow;
            state.DegradationStartTime = null;
            state.Reason = reason ?? "System restored";
            state.IsAutomatic = isAutomatic;

            // Clear degradation configuration
            state.DisabledFeatures.Clear();
            state.DegradedServices.Clear();
        }

        private void ApplyDegradationConfiguration(SystemDegradationState state, DegradationLevel level)
        {
            // Clear previous configuration
            state.DisabledFeatures.Clear();
            state.DegradedServices.Clear();

            // Apply configuration based on degradation level
            var config = level switch
            {
                DegradationLevel.Minor => _degradationThresholds.MinorDegradation,
                DegradationLevel.Moderate => _degradationThresholds.ModerateDegradation,
                DegradationLevel.Severe => _degradationThresholds.SevereDegradation,
                DegradationLevel.Disabled => _degradationThresholds.DisabledState,
                _ => null
            };

            if (config != null)
            {
                foreach (var feature in config.DisabledFeatures)
                {
                    state.DisabledFeatures.Add(feature);
                }

                foreach (var service in config.DegradedServices)
                {
                    state.DegradedServices.Add(service);
                }
            }
        }

        private void UpdateOverallDegradationLevel()
        {
            var previousLevel = _overallDegradationLevel;
            var systemLevels = _systemStates.Values.Select(s => s.CurrentLevel).ToList();

            if (!systemLevels.Any())
            {
                _overallDegradationLevel = DegradationLevel.None;
            }
            else
            {
                // Use the highest degradation level as overall level
                _overallDegradationLevel = systemLevels.Max();
            }

            if (_overallDegradationLevel != previousLevel)
            {
                _lastDegradationChange = DateTime.UtcNow;
                OnOverallDegradationChanged(previousLevel, _overallDegradationLevel);
            }
        }

        private DegradationLevel CalculateDegradationLevelFromHealth(List<HealthCheckResult> healthResults)
        {
            if (!healthResults.Any())
                return DegradationLevel.None;

            var totalChecks = healthResults.Count;
            var unhealthyCount = healthResults.Count(r => r.Status == HealthStatus.Unhealthy);
            var degradedCount = healthResults.Count(r => r.Status == HealthStatus.Degraded);

            var unhealthyPercentage = (double)unhealthyCount / totalChecks;
            var totalIssuesPercentage = (double)(unhealthyCount + degradedCount) / totalChecks;

            // Use weighted calculation if enabled
            if (_degradationThresholds.UseWeightedCalculation)
            {
                var weightedScore = CalculateWeightedHealthScore(healthResults);
                var unhealthyScore = 1.0 - weightedScore;

                if (unhealthyScore >= _degradationThresholds.DisabledThreshold)
                    return DegradationLevel.Disabled;
                if (unhealthyScore >= _degradationThresholds.SevereThreshold)
                    return DegradationLevel.Severe;
                if (unhealthyScore >= _degradationThresholds.ModerateThreshold)
                    return DegradationLevel.Moderate;
                if (unhealthyScore >= _degradationThresholds.MinorThreshold)
                    return DegradationLevel.Minor;
            }
            else
            {
                if (unhealthyPercentage >= _degradationThresholds.DisabledThreshold)
                    return DegradationLevel.Disabled;
                if (unhealthyPercentage >= _degradationThresholds.SevereThreshold)
                    return DegradationLevel.Severe;
                if (unhealthyPercentage >= _degradationThresholds.ModerateThreshold)
                    return DegradationLevel.Moderate;
                if (totalIssuesPercentage >= _degradationThresholds.MinorThreshold)
                    return DegradationLevel.Minor;
            }

            return DegradationLevel.None;
        }

        private double CalculateWeightedHealthScore(List<HealthCheckResult> healthResults)
        {
            var weightedScore = 0.0;
            var totalWeight = 0.0;

            foreach (var result in healthResults)
            {
                var weight = _degradationThresholds.CategoryWeights.GetValueOrDefault(result.Category, 1.0);
                var resultScore = result.Status switch
                {
                    HealthStatus.Healthy => 1.0,
                    HealthStatus.Degraded => 0.5,
                    HealthStatus.Unhealthy => 0.0,
                    HealthStatus.Unknown => 0.3,
                    _ => 0.0
                };

                weightedScore += resultScore * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedScore / totalWeight : 0.0;
        }

        private void ApplyOverallDegradation(DegradationLevel degradationLevel, string reason)
        {
            // Apply degradation to all systems that don't have manual overrides
            foreach (var kvp in _systemStates)
            {
                var systemName = kvp.Key;
                var systemState = kvp.Value;

                // Skip systems with manual degradation
                if (!systemState.IsAutomatic && systemState.CurrentLevel != DegradationLevel.None)
                    continue;

                // Apply degradation rule if available
                if (_degradationRules.TryGetValue(systemName, out var rule))
                {
                    var systemDegradationLevel = rule.CalculateDegradationLevel(degradationLevel);
                    if (systemDegradationLevel != systemState.CurrentLevel)
                    {
                        EnableGracefulDegradation(systemName, systemDegradationLevel, reason, true);
                    }
                }
                else if (systemState.CurrentLevel != degradationLevel)
                {
                    EnableGracefulDegradation(systemName, degradationLevel, reason, true);
                }
            }
        }

        private void EvaluateIndividualSystemDegradation(List<HealthCheckResult> healthResults)
        {
            var systemHealthGroups = healthResults
                .Where(r => !string.IsNullOrEmpty(r.Name))
                .GroupBy(r => ExtractSystemNameFromHealthCheck(r.Name));

            foreach (var systemGroup in systemHealthGroups)
            {
                var systemName = new FixedString64Bytes(systemGroup.Key);
                var systemResults = systemGroup.ToList();
                var systemDegradationLevel = CalculateDegradationLevelFromHealth(systemResults);

                if (_systemStates.TryGetValue(systemName, out var systemState))
                {
                    if (systemState.CurrentLevel != systemDegradationLevel && systemState.IsAutomatic)
                    {
                        EnableGracefulDegradation(systemName, systemDegradationLevel,
                            "Individual system health evaluation", true);
                    }
                }
                else if (systemDegradationLevel != DegradationLevel.None)
                {
                    EnableGracefulDegradation(systemName, systemDegradationLevel,
                        "Individual system health evaluation", true);
                }
            }
        }

        private string ExtractSystemNameFromHealthCheck(string healthCheckName)
        {
            // Simple extraction - in practice this would be more sophisticated
            var parts = healthCheckName.Split('.');
            return parts.Length > 0 ? parts[0] : healthCheckName;
        }

        private void StartRecoveryProcess(FixedString64Bytes systemName, string reason, bool isAutomatic)
        {
            var recoveryTracker = new RecoveryTracker
            {
                SystemName = systemName,
                StartTime = DateTime.UtcNow,
                Reason = reason ?? "Manual recovery initiated",
                IsAutomatic = isAutomatic,
                SuccessfulChecks = 0,
                TotalChecks = 0
            };

            _recoveryTrackers.TryAdd(systemName, recoveryTracker);
            OnRecoveryInitiated(systemName, reason, isAutomatic);

            _logger.LogInfo($"Started recovery process for system {systemName}");
        }

        private void EvaluateDegradationStates(object state)
        {
            try
            {
                _degradationLock.EnterReadLock();
                try
                {
                    // Check for systems that may need automatic recovery
                    foreach (var kvp in _systemStates)
                    {
                        var systemName = kvp.Key;
                        var systemState = kvp.Value;

                        if (systemState.IsAutomatic &&
                            systemState.CurrentLevel != DegradationLevel.None &&
                            _degradationThresholds.EnableAutoRecovery)
                        {
                            // Check if system should start recovery
                            if (!_recoveryTrackers.ContainsKey(systemName) &&
                                ShouldStartAutoRecovery(systemState))
                            {
                                StartRecoveryProcess(systemName, "Automatic recovery evaluation", true);
                            }
                        }
                    }
                }
                finally
                {
                    _degradationLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during degradation state evaluation");
            }
        }

        private void EvaluateRecoveryStates(object state)
        {
            try
            {
                var completedRecoveries = new List<FixedString64Bytes>();

                foreach (var kvp in _recoveryTrackers)
                {
                    var systemName = kvp.Key;
                    var tracker = kvp.Value;

                    if (ShouldCompleteRecovery(tracker))
                    {
                        completedRecoveries.Add(systemName);
                        DisableGracefulDegradation(systemName, "Automatic recovery completed", true);
                        OnRecoveryCompleted(systemName, tracker.Reason, tracker.IsAutomatic, true);
                    }
                    else if (ShouldFailRecovery(tracker))
                    {
                        completedRecoveries.Add(systemName);
                        OnRecoveryCompleted(systemName, tracker.Reason, tracker.IsAutomatic, false);
                    }
                }

                // Remove completed recoveries
                foreach (var systemName in completedRecoveries)
                {
                    _recoveryTrackers.TryRemove(systemName, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during recovery state evaluation");
            }
        }

        private bool ShouldStartAutoRecovery(SystemDegradationState systemState)
        {
            // Start recovery if system has been degraded for minimum time
            var degradationDuration = DateTime.UtcNow - (systemState.DegradationStartTime ?? DateTime.UtcNow);
            return degradationDuration >= _degradationConfig.MinimumDegradationDuration;
        }

        private bool ShouldCompleteRecovery(RecoveryTracker tracker)
        {
            if (tracker.TotalChecks < _degradationConfig.MinimumRecoveryChecks)
                return false;

            var successRate = (double)tracker.SuccessfulChecks / tracker.TotalChecks;
            return successRate >= _degradationThresholds.RecoveryThreshold;
        }

        private bool ShouldFailRecovery(RecoveryTracker tracker)
        {
            var recoveryDuration = DateTime.UtcNow - tracker.StartTime;
            return recoveryDuration >= _degradationConfig.MaximumRecoveryDuration;
        }

        private DegradationRule CreateDatabaseDegradationRule()
        {
            return new DegradationRule
            {
                SystemType = "Database",
                MinorThreshold = 0.1,
                ModerateThreshold = 0.3,
                SevereThreshold = 0.6,
                DisabledThreshold = 0.8,
                CustomLogic = level => level switch
                {
                    DegradationLevel.Minor => DegradationLevel.Minor,
                    DegradationLevel.Moderate => DegradationLevel.Moderate,
                    DegradationLevel.Severe => DegradationLevel.Severe,
                    DegradationLevel.Disabled => DegradationLevel.Disabled,
                    _ => DegradationLevel.None
                }
            };
        }

        private DegradationRule CreateNetworkDegradationRule()
        {
            return new DegradationRule
            {
                SystemType = "Network",
                MinorThreshold = 0.15,
                ModerateThreshold = 0.35,
                SevereThreshold = 0.7,
                DisabledThreshold = 0.9,
                CustomLogic = level => level switch
                {
                    DegradationLevel.Minor => DegradationLevel.None, // Network is more resilient
                    DegradationLevel.Moderate => DegradationLevel.Minor,
                    DegradationLevel.Severe => DegradationLevel.Moderate,
                    DegradationLevel.Disabled => DegradationLevel.Severe,
                    _ => DegradationLevel.None
                }
            };
        }

        private DegradationRule CreateCacheDegradationRule()
        {
            return new DegradationRule
            {
                SystemType = "Cache",
                MinorThreshold = 0.2,
                ModerateThreshold = 0.4,
                SevereThreshold = 0.6,
                DisabledThreshold = 0.8,
                CustomLogic = level => level switch
                {
                    DegradationLevel.Minor => DegradationLevel.None, // Cache can fail gracefully
                    DegradationLevel.Moderate => DegradationLevel.None,
                    DegradationLevel.Severe => DegradationLevel.Minor,
                    DegradationLevel.Disabled => DegradationLevel.Moderate,
                    _ => DegradationLevel.None
                }
            };
        }

        private DegradationRule CreateMessageBusDegradationRule()
        {
            return new DegradationRule
            {
                SystemType = "MessageBus",
                MinorThreshold = 0.1,
                ModerateThreshold = 0.25,
                SevereThreshold = 0.5,
                DisabledThreshold = 0.75,
                CustomLogic = level => level // Direct mapping for message bus
            };
        }

        private void OnDegradationChanged(
            FixedString64Bytes systemName,
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason,
            bool isAutomatic)
        {
            try
            {
                var eventArgs = new DegradationChangedEventArgs
                {
                    SystemName = systemName.ToString(),
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Reason = reason,
                    IsAutomatic = isAutomatic,
                    Timestamp = DateTime.UtcNow
                };

                DegradationChanged?.Invoke(this, eventArgs);

                // Publish message bus event
                PublishDegradationChangeMessage(systemName, oldLevel, newLevel, reason, isAutomatic);

                // Generate alert
                HandleDegradationAlert(systemName, oldLevel, newLevel, reason);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking DegradationChanged event");
            }
        }

        private void OnOverallDegradationChanged(DegradationLevel oldLevel, DegradationLevel newLevel)
        {
            try
            {
                var eventArgs = new OverallDegradationChangedEventArgs
                {
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Timestamp = DateTime.UtcNow,
                    AffectedSystems = _systemStates.Values
                        .Where(s => s.CurrentLevel != DegradationLevel.None)
                        .Select(s => s.SystemName.ToString())
                        .ToList()
                };

                OverallDegradationChanged?.Invoke(this, eventArgs);

                // Generate overall degradation alert
                HandleOverallDegradationAlert(oldLevel, newLevel);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking OverallDegradationChanged event");
            }
        }

        private void OnRecoveryInitiated(FixedString64Bytes systemName, string reason, bool isAutomatic)
        {
            try
            {
                var eventArgs = new RecoveryInitiatedEventArgs
                {
                    SystemName = systemName.ToString(),
                    Reason = reason,
                    IsAutomatic = isAutomatic,
                    Timestamp = DateTime.UtcNow
                };

                RecoveryInitiated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking RecoveryInitiated event");
            }
        }

        private void OnRecoveryCompleted(FixedString64Bytes systemName, string reason, bool isAutomatic,
            bool successful)
        {
            try
            {
                var eventArgs = new RecoveryCompletedEventArgs
                {
                    SystemName = systemName.ToString(),
                    Reason = reason,
                    IsAutomatic = isAutomatic,
                    Successful = successful,
                    Timestamp = DateTime.UtcNow
                };

                RecoveryCompleted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking RecoveryCompleted event");
            }
        }

        private void PublishDegradationChangeMessage(
            FixedString64Bytes systemName,
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason,
            bool isAutomatic)
        {
            try
            {
                var message = new DegradationChangeMessage
                {
                    SystemName = systemName.ToString(),
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Reason = reason,
                    IsAutomatic = isAutomatic,
                    Timestamp = DateTime.UtcNow
                };

                var publisher = _messageBusService.GetPublisher<DegradationChangeMessage>();
                publisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to publish degradation change message");
            }
        }

        private void HandleDegradationAlert(
            FixedString64Bytes systemName,
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason)
        {
            var severity = newLevel switch
            {
                DegradationLevel.Disabled => AlertSeverity.Critical,
                DegradationLevel.Severe => AlertSeverity.High,
                DegradationLevel.Moderate => AlertSeverity.Medium,
                DegradationLevel.Minor => AlertSeverity.Low,
                DegradationLevel.None when oldLevel != DegradationLevel.None => AlertSeverity.Info,
                _ => (AlertSeverity?)null
            };

            if (severity.HasValue)
            {
                _alertService.RaiseAlert(
                    new FixedString64Bytes($"Degradation.{systemName}"),
                    severity.Value,
                    new FixedString512Bytes($"System {systemName} degradation: {oldLevel} -> {newLevel}. {reason}"));
            }
        }

        private void HandleOverallDegradationAlert(DegradationLevel oldLevel, DegradationLevel newLevel)
        {
            var severity = newLevel switch
            {
                DegradationLevel.Disabled => AlertSeverity.Critical,
                DegradationLevel.Severe => AlertSeverity.High,
                DegradationLevel.Moderate => AlertSeverity.Medium,
                DegradationLevel.Minor => AlertSeverity.Low,
                DegradationLevel.None when oldLevel != DegradationLevel.None => AlertSeverity.Info,
                _ => (AlertSeverity?)null
            };

            if (severity.HasValue)
            {
                _alertService.RaiseAlert(
                    new FixedString64Bytes("Degradation.Overall"),
                    severity.Value,
                    new FixedString512Bytes($"Overall system degradation: {oldLevel} -> {newLevel}"));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DegradationService));
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _degradationEvaluationTimer?.Dispose();
                _degradationEvaluationTimer = null;

                _recoveryEvaluationTimer?.Dispose();
                _recoveryEvaluationTimer = null;

                _degradationLock.EnterWriteLock();
                try
                {
                    _systemStates.Clear();
                    _degradationRules.Clear();
                    _recoveryTrackers.Clear();
                }
                finally
                {
                    _degradationLock.ExitWriteLock();
                }

                _degradationLock?.Dispose();

                _logger.LogInfo("DegradationService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during DegradationService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}