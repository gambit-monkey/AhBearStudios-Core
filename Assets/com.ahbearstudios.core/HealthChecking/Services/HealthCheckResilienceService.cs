using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Implementation of health check resilience service managing circuit breakers and degradation.
    /// Uses IMessageBusService and IAlertService directly for notifications.
    /// Consolidates circuit breaker and degradation logic for unified resilience strategy.
    /// </summary>
    public sealed class HealthCheckResilienceService : IHealthCheckResilienceService
    {
        private readonly ILoggingService _logger;
        private readonly IMessageBusService _messageBus;
        private readonly IAlertService _alertService;
        private readonly Guid _serviceId;

        // Circuit breaker tracking
        private readonly ConcurrentDictionary<FixedString64Bytes, CircuitBreakerMetrics> _circuitBreakers = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, DateTime> _lastFailureTime = new();
        private readonly object _circuitBreakerLock = new();

        // Degradation state
        private volatile DegradationLevel _currentDegradationLevel = DegradationLevel.None;
        private readonly List<FixedString64Bytes> _disabledFeatures = new();
        private readonly List<FixedString64Bytes> _degradedServices = new();
        private readonly object _degradationLock = new();

        // Configuration
        private readonly int _failureThreshold = 5;
        private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _halfOpenRetryDelay = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the HealthCheckResilienceService.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="messageBus">Message bus for state change notifications</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        public HealthCheckResilienceService(
            ILoggingService logger,
            IMessageBusService messageBus,
            IAlertService alertService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckResilienceService");

            _logger.LogDebug("HealthCheckResilienceService initialized with ID: {ServiceId}", _serviceId);
        }

        /// <inheritdoc />
        public bool CanExecuteOperation(FixedString64Bytes operationName)
        {
            if (!_circuitBreakers.TryGetValue(operationName, out var metrics))
            {
                // First time seeing this operation, allow execution
                return true;
            }

            var state = DetermineCircuitBreakerState(metrics, operationName);

            return state switch
            {
                CircuitBreakerState.Closed => true,
                CircuitBreakerState.Open => false,
                CircuitBreakerState.HalfOpen => ShouldAllowHalfOpenAttempt(operationName),
                _ => true
            };
        }

        /// <inheritdoc />
        public void RecordSuccess(FixedString64Bytes operationName, TimeSpan executionTime)
        {
            var metrics = _circuitBreakers.GetOrAdd(operationName, _ => new CircuitBreakerMetrics());

            lock (_circuitBreakerLock)
            {
                metrics.SuccessCount++;
                metrics.ConsecutiveFailures = 0;
                metrics.LastSuccessTime = DateTime.UtcNow;
                metrics.TotalExecutionTime += executionTime;

                // Reset to closed state if we were in half-open
                var previousState = DetermineCircuitBreakerState(metrics, operationName);
                if (previousState == CircuitBreakerState.HalfOpen)
                {
                    metrics.LastStateChange = DateTime.UtcNow;
                    PublishCircuitBreakerStateChange(operationName, CircuitBreakerState.HalfOpen, CircuitBreakerState.Closed);
                }
            }

            _logger.LogDebug("Recorded success for operation '{Operation}' in {Time}ms",
                operationName, executionTime.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void RecordFailure(FixedString64Bytes operationName, Exception exception, TimeSpan executionTime)
        {
            var metrics = _circuitBreakers.GetOrAdd(operationName, _ => new CircuitBreakerMetrics());

            lock (_circuitBreakerLock)
            {
                var previousState = DetermineCircuitBreakerState(metrics, operationName);

                metrics.FailureCount++;
                metrics.ConsecutiveFailures++;
                metrics.LastFailureTime = DateTime.UtcNow;
                metrics.LastException = exception;

                _lastFailureTime[operationName] = DateTime.UtcNow;

                // Check if we should trip the circuit breaker
                if (metrics.ConsecutiveFailures >= _failureThreshold && previousState == CircuitBreakerState.Closed)
                {
                    metrics.LastStateChange = DateTime.UtcNow;
                    PublishCircuitBreakerStateChange(operationName, CircuitBreakerState.Closed, CircuitBreakerState.Open);

                    // Send critical alert for circuit breaker trip
                    _ = _alertService.RaiseAlertAsync(
                        $"Circuit Breaker Tripped: {operationName}",
                        $"Operation '{operationName}' has failed {metrics.ConsecutiveFailures} consecutive times. Circuit breaker is now OPEN.",
                        AlertSeverity.Critical,
                        new[] { new FixedString64Bytes("CircuitBreaker"), operationName });
                }
            }

            _logger.LogWarning("Recorded failure for operation '{Operation}': {Exception}",
                operationName, exception?.Message ?? "Unknown error");
        }

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            if (!_circuitBreakers.TryGetValue(operationName, out var metrics))
            {
                return CircuitBreakerState.Closed;
            }

            return DetermineCircuitBreakerState(metrics, operationName);
        }

        /// <inheritdoc />
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return _circuitBreakers.ToDictionary(
                kvp => kvp.Key,
                kvp => DetermineCircuitBreakerState(kvp.Value, kvp.Key));
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            var metrics = _circuitBreakers.GetOrAdd(operationName, _ => new CircuitBreakerMetrics());
            var previousState = DetermineCircuitBreakerState(metrics, operationName);

            lock (_circuitBreakerLock)
            {
                metrics.ForcedState = CircuitBreakerState.Open;
                metrics.ForcedReason = reason;
                metrics.LastStateChange = DateTime.UtcNow;
            }

            PublishCircuitBreakerStateChange(operationName, previousState, CircuitBreakerState.Open);

            _logger.LogWarning("Forced circuit breaker OPEN for '{Operation}': {Reason}",
                operationName, reason);
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            var metrics = _circuitBreakers.GetOrAdd(operationName, _ => new CircuitBreakerMetrics());
            var previousState = DetermineCircuitBreakerState(metrics, operationName);

            lock (_circuitBreakerLock)
            {
                metrics.ForcedState = CircuitBreakerState.Closed;
                metrics.ForcedReason = reason;
                metrics.ConsecutiveFailures = 0; // Reset failures when manually closed
                metrics.LastStateChange = DateTime.UtcNow;
            }

            PublishCircuitBreakerStateChange(operationName, previousState, CircuitBreakerState.Closed);

            _logger.LogInfo("Forced circuit breaker CLOSED for '{Operation}': {Reason}",
                operationName, reason);
        }

        /// <inheritdoc />
        public bool ResetCircuitBreaker(FixedString64Bytes operationName)
        {
            if (!_circuitBreakers.TryGetValue(operationName, out var metrics))
            {
                return false;
            }

            var previousState = DetermineCircuitBreakerState(metrics, operationName);

            lock (_circuitBreakerLock)
            {
                metrics.ForcedState = null;
                metrics.ForcedReason = null;
                metrics.ConsecutiveFailures = 0;
                metrics.LastStateChange = DateTime.UtcNow;
            }

            var newState = DetermineCircuitBreakerState(metrics, operationName);
            if (newState != previousState)
            {
                PublishCircuitBreakerStateChange(operationName, previousState, newState);
            }

            _logger.LogInfo("Reset circuit breaker for '{Operation}', new state: {State}",
                operationName, newState);

            return true;
        }

        /// <inheritdoc />
        public DegradationLevel GetCurrentDegradationLevel()
        {
            return _currentDegradationLevel;
        }

        /// <inheritdoc />
        public async UniTask SetDegradationLevelAsync(DegradationLevel level, string reason, string triggeredBy = null)
        {
            var previousLevel = _currentDegradationLevel;

            if (previousLevel == level)
            {
                return; // No change needed
            }

            lock (_degradationLock)
            {
                _currentDegradationLevel = level;

                // Update feature and service lists based on degradation level
                UpdateDisabledFeaturesAndServices(level);
            }

            // Publish degradation change message directly via IMessageBusService
            var message = HealthCheckDegradationChangeMessage.Create(
                previousLevel,
                level,
                reason,
                "HealthCheckResilienceService",
                DeterministicIdGenerator.GenerateCorrelationId("DegradationChange", reason));

            await _messageBus.PublishMessageAsync(message);

            // Send alert for significant degradation changes
            if (level > previousLevel && level >= DegradationLevel.Moderate)
            {
                var severity = level >= DegradationLevel.Severe ? AlertSeverity.Critical : AlertSeverity.Warning;
                await _alertService.RaiseAlertAsync(
                    "System Degradation Level Changed",
                    $"System degradation level changed from {previousLevel} to {level}. Reason: {reason}. Triggered by: {triggeredBy ?? "Unknown"}",
                    severity,
                    new[] { new FixedString64Bytes("Degradation"), new FixedString64Bytes(level.ToString()) });
            }

            _logger.LogWarning("Degradation level changed from {Previous} to {New}: {Reason}",
                previousLevel, level, reason);
        }

        /// <inheritdoc />
        public async UniTask UpdateDegradationFromHealthStatusAsync(
            Dictionary<string, HealthCheckResult> healthResults,
            OverallHealthStatus overallStatus)
        {
            var newDegradationLevel = DetermineDegradationLevel(healthResults, overallStatus);
            var reason = BuildDegradationReason(healthResults, overallStatus);

            await SetDegradationLevelAsync(newDegradationLevel, reason, "HealthStatusUpdate");
        }

        /// <inheritdoc />
        public List<FixedString64Bytes> GetDisabledFeatures()
        {
            lock (_degradationLock)
            {
                return new List<FixedString64Bytes>(_disabledFeatures);
            }
        }

        /// <inheritdoc />
        public List<FixedString64Bytes> getDegradedServices()
        {
            lock (_degradationLock)
            {
                return new List<FixedString64Bytes>(_degradedServices);
            }
        }

        /// <inheritdoc />
        public async UniTask InitiateRecoveryAsync(RecoveryScenario scenario, CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("Initiating recovery scenario: {Scenario}", scenario);

            switch (scenario)
            {
                case RecoveryScenario.GracefulRecovery:
                    await ExecuteGracefulRecoveryAsync(cancellationToken);
                    break;

                case RecoveryScenario.EmergencyRecovery:
                    await ExecuteEmergencyRecoveryAsync(cancellationToken);
                    break;

                case RecoveryScenario.CircuitBreakerRecovery:
                    await ExecuteCircuitBreakerRecoveryAsync(cancellationToken);
                    break;

                case RecoveryScenario.ServiceRecovery:
                    await ExecuteServiceRecoveryAsync(cancellationToken);
                    break;

                case RecoveryScenario.SystemRestart:
                    await ExecuteSystemRestartRecoveryAsync(cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown recovery scenario: {Scenario}", scenario);
                    break;
            }
        }

        /// <inheritdoc />
        public List<RecoveryRecommendation> GetRecoveryRecommendations()
        {
            var recommendations = new List<RecoveryRecommendation>();

            // Analyze circuit breaker states
            var openCircuitBreakers = _circuitBreakers
                .Where(kvp => DetermineCircuitBreakerState(kvp.Value, kvp.Key) == CircuitBreakerState.Open)
                .ToList();

            if (openCircuitBreakers.Any())
            {
                var recommendation = RecoveryRecommendation.Create(
                    priority: 1,
                    action: "Reset Circuit Breakers",
                    reason: $"{openCircuitBreakers.Count} circuit breakers are open",
                    expectedImpact: "Restore failed operations",
                    estimatedRecoveryTime: TimeSpan.FromMinutes(2),
                    risk: RiskLevel.Medium,
                    affectedComponents: openCircuitBreakers.Select(cb => cb.Key.ToString()).ToList());
                recommendations.Add(recommendation);
            }

            // Analyze degradation level
            if (_currentDegradationLevel >= DegradationLevel.Moderate)
            {
                var degradationRecommendation = RecoveryRecommendation.Create(
                    priority: 2,
                    action: "Reduce Degradation Level",
                    reason: $"Current degradation level is {_currentDegradationLevel}",
                    expectedImpact: "Restore disabled features and services",
                    estimatedRecoveryTime: TimeSpan.FromMinutes(5),
                    risk: RiskLevel.Low,
                    affectedComponents: _disabledFeatures.Concat(_degradedServices).Select(f => f.ToString()).ToList());
                recommendations.Add(degradationRecommendation);
            }

            // Add general recommendations
            if (_currentDegradationLevel >= DegradationLevel.Severe)
            {
                var emergencyRecommendation = RecoveryRecommendation.Create(
                    priority: 3,
                    action: "Emergency Recovery Procedures",
                    reason: "System is severely degraded",
                    expectedImpact: "Full system recovery",
                    estimatedRecoveryTime: TimeSpan.FromMinutes(10),
                    risk: RiskLevel.High,
                    affectedComponents: new List<string> { "Entire System" });
                recommendations.Add(emergencyRecommendation);
            }

            return recommendations.OrderBy(r => r.Priority).ToList();
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetResilienceStatistics()
        {
            var stats = new Dictionary<string, object>();

            // Circuit breaker statistics
            var totalCircuitBreakers = _circuitBreakers.Count;
            var openCircuitBreakers = _circuitBreakers.Count(kvp =>
                DetermineCircuitBreakerState(kvp.Value, kvp.Key) == CircuitBreakerState.Open);
            var halfOpenCircuitBreakers = _circuitBreakers.Count(kvp =>
                DetermineCircuitBreakerState(kvp.Value, kvp.Key) == CircuitBreakerState.HalfOpen);

            stats["CircuitBreakers.Total"] = totalCircuitBreakers;
            stats["CircuitBreakers.Open"] = openCircuitBreakers;
            stats["CircuitBreakers.HalfOpen"] = halfOpenCircuitBreakers;
            stats["CircuitBreakers.Closed"] = totalCircuitBreakers - openCircuitBreakers - halfOpenCircuitBreakers;

            // Degradation statistics
            stats["Degradation.Current"] = _currentDegradationLevel.ToString();
            stats["Degradation.DisabledFeatures"] = _disabledFeatures.Count;
            stats["Degradation.DegradedServices"] = _degradedServices.Count;

            // Failure statistics
            var totalFailures = _circuitBreakers.Values.Sum(m => m.FailureCount);
            var totalSuccesses = _circuitBreakers.Values.Sum(m => m.SuccessCount);
            var totalExecutions = totalFailures + totalSuccesses;

            stats["Operations.TotalExecutions"] = totalExecutions;
            stats["Operations.TotalFailures"] = totalFailures;
            stats["Operations.TotalSuccesses"] = totalSuccesses;
            stats["Operations.SuccessRate"] = totalExecutions > 0 ? (double)totalSuccesses / totalExecutions : 0.0;

            return stats;
        }

        /// <inheritdoc />
        public bool ValidateResilienceHealth()
        {
            // Check for excessive circuit breaker failures
            var openCircuitBreakers = _circuitBreakers.Count(kvp =>
                DetermineCircuitBreakerState(kvp.Value, kvp.Key) == CircuitBreakerState.Open);

            if (openCircuitBreakers > _circuitBreakers.Count * 0.5) // More than 50% open
            {
                _logger.LogError("Resilience health check failed: {Open}/{Total} circuit breakers are open",
                    openCircuitBreakers, _circuitBreakers.Count);
                return false;
            }

            // Check degradation level
            if (_currentDegradationLevel >= DegradationLevel.Severe)
            {
                _logger.LogError("Resilience health check failed: System degradation level is {Level}",
                    _currentDegradationLevel);
                return false;
            }

            return true;
        }

        private CircuitBreakerState DetermineCircuitBreakerState(CircuitBreakerMetrics metrics, FixedString64Bytes operationName)
        {
            // Check for forced state first
            if (metrics.ForcedState.HasValue)
            {
                return metrics.ForcedState.Value;
            }

            // Check if we should transition from open to half-open
            if (metrics.ConsecutiveFailures >= _failureThreshold)
            {
                var timeSinceLastStateChange = DateTime.UtcNow - metrics.LastStateChange;
                if (timeSinceLastStateChange >= _circuitBreakerTimeout)
                {
                    return CircuitBreakerState.HalfOpen;
                }
                return CircuitBreakerState.Open;
            }

            return CircuitBreakerState.Closed;
        }

        private bool ShouldAllowHalfOpenAttempt(FixedString64Bytes operationName)
        {
            if (!_lastFailureTime.TryGetValue(operationName, out var lastFailure))
            {
                return true;
            }

            return DateTime.UtcNow - lastFailure >= _halfOpenRetryDelay;
        }

        private void PublishCircuitBreakerStateChange(FixedString64Bytes operationName, CircuitBreakerState from, CircuitBreakerState to)
        {
            var message = HealthCheckCircuitBreakerStateChangedMessage.Create(
                operationName.ToString(),
                from,
                to,
                "Circuit breaker state transition",
                "HealthCheckResilienceService");

            // Publish directly via IMessageBusService
            _ = _messageBus.PublishMessageAsync(message);
        }

        private DegradationLevel DetermineDegradationLevel(Dictionary<string, HealthCheckResult> healthResults, OverallHealthStatus overallStatus)
        {
            var criticalCount = healthResults.Count(r => r.Value.Status == HealthStatus.Critical);
            var unhealthyCount = healthResults.Count(r => r.Value.Status == HealthStatus.Unhealthy);
            var degradedCount = healthResults.Count(r => r.Value.Status == HealthStatus.Degraded);
            var totalCount = healthResults.Count;

            if (totalCount == 0)
                return DegradationLevel.None;

            var unhealthyPercentage = (double)(criticalCount + unhealthyCount) / totalCount;

            return unhealthyPercentage switch
            {
                >= 0.5 => DegradationLevel.Severe,     // 50%+ unhealthy
                >= 0.3 => DegradationLevel.Moderate,   // 30%+ unhealthy
                >= 0.1 => DegradationLevel.Minor,      // 10%+ unhealthy
                _ => DegradationLevel.None
            };
        }

        private string BuildDegradationReason(Dictionary<string, HealthCheckResult> healthResults, OverallHealthStatus overallStatus)
        {
            var unhealthyChecks = healthResults
                .Where(r => r.Value.Status == HealthStatus.Unhealthy || r.Value.Status == HealthStatus.Critical)
                .Select(r => r.Key)
                .ToList();

            if (unhealthyChecks.Any())
            {
                return $"Unhealthy health checks: {string.Join(", ", unhealthyChecks)}";
            }

            return $"Overall status: {overallStatus}";
        }

        private void UpdateDisabledFeaturesAndServices(DegradationLevel level)
        {
            _disabledFeatures.Clear();
            _degradedServices.Clear();

            switch (level)
            {
                case DegradationLevel.Minor:
                    _disabledFeatures.Add("NonEssentialFeatures");
                    break;

                case DegradationLevel.Moderate:
                    _disabledFeatures.Add("NonEssentialFeatures");
                    _disabledFeatures.Add("AdvancedUI");
                    _degradedServices.Add("CachingService");
                    break;

                case DegradationLevel.Severe:
                    _disabledFeatures.Add("NonEssentialFeatures");
                    _disabledFeatures.Add("AdvancedUI");
                    _disabledFeatures.Add("ReportingFeatures");
                    _degradedServices.Add("CachingService");
                    _degradedServices.Add("LoggingService");
                    _degradedServices.Add("ProfilingService");
                    break;
            }
        }

        private async UniTask ExecuteGracefulRecoveryAsync(CancellationToken cancellationToken)
        {
            // Gradually restore services and features
            await SetDegradationLevelAsync(DegradationLevel.Minor, "Graceful recovery initiated");
            await UniTask.Delay(TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
            await SetDegradationLevelAsync(DegradationLevel.None, "Graceful recovery completed");
        }

        private async UniTask ExecuteEmergencyRecoveryAsync(CancellationToken cancellationToken)
        {
            // Force all circuit breakers closed and reset degradation
            foreach (var operationName in _circuitBreakers.Keys)
            {
                ForceCircuitBreakerClosed(operationName, "Emergency recovery");
            }

            await SetDegradationLevelAsync(DegradationLevel.None, "Emergency recovery executed");
        }

        private async UniTask ExecuteCircuitBreakerRecoveryAsync(CancellationToken cancellationToken)
        {
            // Reset all circuit breakers
            foreach (var operationName in _circuitBreakers.Keys)
            {
                ResetCircuitBreaker(operationName);
            }

            await UniTask.CompletedTask;
        }

        private async UniTask ExecuteServiceRecoveryAsync(CancellationToken cancellationToken)
        {
            // Restart degraded services
            await SetDegradationLevelAsync(DegradationLevel.None, "Service recovery executed");
        }

        private async UniTask ExecuteSystemRestartRecoveryAsync(CancellationToken cancellationToken)
        {
            // This would trigger a system restart - for now just alert
            await _alertService.RaiseAlertAsync(
                "System Restart Recovery Initiated",
                "System restart recovery has been triggered. Manual intervention may be required.",
                AlertSeverity.Emergency,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Circuit breaker metrics for tracking state.
        /// </summary>
        private class CircuitBreakerMetrics
        {
            public int FailureCount { get; set; }
            public int SuccessCount { get; set; }
            public int ConsecutiveFailures { get; set; }
            public DateTime LastFailureTime { get; set; }
            public DateTime LastSuccessTime { get; set; }
            public DateTime LastStateChange { get; set; } = DateTime.UtcNow;
            public Exception LastException { get; set; }
            public TimeSpan TotalExecutionTime { get; set; }
            public CircuitBreakerState? ForcedState { get; set; }
            public string ForcedReason { get; set; }
        }
    }
}