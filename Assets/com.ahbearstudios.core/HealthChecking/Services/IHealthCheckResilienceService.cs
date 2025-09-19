using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service responsible for health check resilience including circuit breakers and degradation management.
    /// Consolidates circuit breaker and degradation functionality for unified resilience strategy.
    /// Uses IMessageBusService and IAlertService directly for notifications.
    /// </summary>
    public interface IHealthCheckResilienceService
    {
        /// <summary>
        /// Gets whether a specific operation can be executed based on circuit breaker state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>True if operation can execute, false if circuit breaker is open</returns>
        bool CanExecuteOperation(FixedString64Bytes operationName);

        /// <summary>
        /// Records a successful operation execution for circuit breaker tracking.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="executionTime">Time taken to execute</param>
        void RecordSuccess(FixedString64Bytes operationName, TimeSpan executionTime);

        /// <summary>
        /// Records a failed operation execution for circuit breaker tracking.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="exception">Exception that occurred (optional)</param>
        /// <param name="executionTime">Time taken before failure</param>
        void RecordFailure(FixedString64Bytes operationName, Exception exception, TimeSpan executionTime);

        /// <summary>
        /// Gets the current circuit breaker state for a specific operation.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Circuit breaker state</returns>
        CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName);

        /// <summary>
        /// Gets all circuit breaker states.
        /// </summary>
        /// <returns>Dictionary of operation names to circuit breaker states</returns>
        Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates();

        /// <summary>
        /// Manually forces a circuit breaker to open state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing open</param>
        void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Manually forces a circuit breaker to closed state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing closed</param>
        void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Resets a circuit breaker to closed state if possible.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        bool ResetCircuitBreaker(FixedString64Bytes operationName);

        /// <summary>
        /// Gets the current system degradation level.
        /// </summary>
        /// <returns>Current degradation level</returns>
        DegradationLevel GetCurrentDegradationLevel();

        /// <summary>
        /// Sets the system degradation level based on health check results.
        /// </summary>
        /// <param name="level">Degradation level to set</param>
        /// <param name="reason">Reason for the change</param>
        /// <param name="triggeredBy">Health check that triggered the change</param>
        /// <returns>Task representing the degradation level change</returns>
        UniTask SetDegradationLevelAsync(DegradationLevel level, string reason, string triggeredBy = null);

        /// <summary>
        /// Updates system degradation based on overall health status.
        /// Complex logic that analyzes multiple health check results.
        /// </summary>
        /// <param name="healthResults">Current health check results</param>
        /// <param name="overallStatus">Overall system status</param>
        /// <returns>Task representing the degradation assessment</returns>
        UniTask UpdateDegradationFromHealthStatusAsync(
            Dictionary<string, HealthCheckResult> healthResults,
            OverallHealthStatus overallStatus);

        /// <summary>
        /// Gets features that should be disabled at the current degradation level.
        /// </summary>
        /// <returns>List of features to disable</returns>
        List<FixedString64Bytes> GetDisabledFeatures();

        /// <summary>
        /// Gets services that should be degraded at the current degradation level.
        /// </summary>
        /// <returns>List of services to degrade</returns>
        List<FixedString64Bytes> getDegradedServices();

        /// <summary>
        /// Initiates recovery procedures for a specific degradation scenario.
        /// </summary>
        /// <param name="scenario">Recovery scenario to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the recovery operation</returns>
        UniTask InitiateRecoveryAsync(RecoveryScenario scenario, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recovery suggestions based on current circuit breaker and degradation state.
        /// </summary>
        /// <returns>List of recovery recommendations</returns>
        List<RecoveryRecommendation> GetRecoveryRecommendations();

        /// <summary>
        /// Gets resilience statistics for monitoring and analysis.
        /// </summary>
        /// <returns>Dictionary of resilience metrics</returns>
        Dictionary<string, object> GetResilienceStatistics();

        /// <summary>
        /// Validates resilience configuration and health.
        /// </summary>
        /// <returns>True if resilience systems are healthy, false otherwise</returns>
        bool ValidateResilienceHealth();
    }

    /// <summary>
    /// Recovery scenario types for degradation handling.
    /// </summary>
    public enum RecoveryScenario
    {
        /// <summary>
        /// Graceful recovery with gradual feature restoration.
        /// </summary>
        GracefulRecovery,

        /// <summary>
        /// Emergency recovery with aggressive measures.
        /// </summary>
        EmergencyRecovery,

        /// <summary>
        /// Circuit breaker specific recovery.
        /// </summary>
        CircuitBreakerRecovery,

        /// <summary>
        /// Service-specific recovery procedures.
        /// </summary>
        ServiceRecovery,

        /// <summary>
        /// Full system restart recovery.
        /// </summary>
        SystemRestart
    }

    /// <summary>
    /// Recovery recommendation for current system state.
    /// </summary>
    public class RecoveryRecommendation
    {
        /// <summary>
        /// Priority of the recommendation (1 = highest).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Recovery action to take.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Reason for the recommendation.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Expected impact of the recovery action.
        /// </summary>
        public string ExpectedImpact { get; set; }

        /// <summary>
        /// Estimated time to recovery.
        /// </summary>
        public TimeSpan EstimatedRecoveryTime { get; set; }

        /// <summary>
        /// Risk level of the recovery action.
        /// </summary>
        public RiskLevel Risk { get; set; }

        /// <summary>
        /// Services or features affected by the recovery.
        /// </summary>
        public List<string> AffectedComponents { get; set; } = new();
    }

    /// <summary>
    /// Risk level for recovery actions.
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// Low risk, safe to execute automatically.
        /// </summary>
        Low,

        /// <summary>
        /// Medium risk, requires monitoring.
        /// </summary>
        Medium,

        /// <summary>
        /// High risk, requires approval or manual intervention.
        /// </summary>
        High,

        /// <summary>
        /// Critical risk, should only be used as last resort.
        /// </summary>
        Critical
    }
}