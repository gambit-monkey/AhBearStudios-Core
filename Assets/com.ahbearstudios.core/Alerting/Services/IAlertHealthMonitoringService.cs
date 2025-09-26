using System;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for monitoring alert system health and managing emergency operations.
    /// Handles health checks, diagnostics, emergency mode, circuit breaker patterns, and performance metrics.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertHealthMonitoringService : IDisposable
    {
        #region Health Status Properties

        /// <summary>
        /// Gets whether the health monitoring service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets whether the alert system is healthy and functioning normally.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Gets whether emergency mode is currently active.
        /// </summary>
        bool IsEmergencyModeActive { get; }

        /// <summary>
        /// Gets the number of consecutive failures recorded.
        /// </summary>
        int ConsecutiveFailures { get; }

        /// <summary>
        /// Gets the reason for emergency mode activation (if active).
        /// </summary>
        string EmergencyModeReason { get; }

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Performs a comprehensive health check of the alerting system.
        /// Evaluates all subsystems and their integration status.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask with comprehensive health report</returns>
        UniTask<AlertSystemHealthReport> PerformHealthCheckAsync(Guid correlationId = default);

        /// <summary>
        /// Gets detailed diagnostic information about the alerting system.
        /// Includes performance metrics, error counts, and subsystem status.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Comprehensive diagnostic report</returns>
        AlertSystemDiagnostics GetDiagnostics(Guid correlationId = default);

        /// <summary>
        /// Gets current performance metrics for all monitored subsystems.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Comprehensive performance metrics</returns>
        AlertSystemPerformanceMetrics GetPerformanceMetrics(Guid correlationId = default);

        /// <summary>
        /// Records a system operation failure for health tracking.
        /// </summary>
        /// <param name="operationType">Type of operation that failed</param>
        /// <param name="errorMessage">Error message or details</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void RecordFailure(string operationType, string errorMessage, Guid correlationId = default);

        /// <summary>
        /// Records a successful system operation for health tracking.
        /// </summary>
        /// <param name="operationType">Type of operation that succeeded</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void RecordSuccess(string operationType, TimeSpan duration, Guid correlationId = default);

        #endregion

        #region Emergency Mode Management

        /// <summary>
        /// Enables emergency mode, bypassing filters and suppression for critical alerts.
        /// </summary>
        /// <param name="reason">Reason for enabling emergency mode</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void EnableEmergencyMode(string reason, Guid correlationId = default);

        /// <summary>
        /// Disables emergency mode and restores normal operations.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void DisableEmergencyMode(Guid correlationId = default);

        /// <summary>
        /// Performs emergency escalation for failed alert delivery.
        /// Routes alerts through emergency channels and notifies administrators.
        /// </summary>
        /// <param name="alert">Alert that failed to deliver</param>
        /// <param name="failureReason">Reason for delivery failure</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask representing the escalation operation</returns>
        UniTask PerformEmergencyEscalationAsync(Alert alert, string failureReason, Guid correlationId = default);

        /// <summary>
        /// Evaluates whether emergency mode should be automatically triggered.
        /// Based on failure rates, error patterns, and system health metrics.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>True if emergency mode should be triggered</returns>
        bool ShouldTriggerEmergencyMode(Guid correlationId = default);

        #endregion

        #region Circuit Breaker Management

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        CircuitBreakerState CircuitBreakerState { get; }

        /// <summary>
        /// Manually opens the circuit breaker to prevent further operations.
        /// </summary>
        /// <param name="reason">Reason for opening circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void OpenCircuitBreaker(string reason, Guid correlationId = default);

        /// <summary>
        /// Manually closes the circuit breaker to resume normal operations.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void CloseCircuitBreaker(Guid correlationId = default);

        /// <summary>
        /// Attempts to recover from circuit breaker open state.
        /// Tests system health and closes circuit breaker if conditions are met.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask with recovery attempt result</returns>
        UniTask<bool> AttemptCircuitBreakerRecoveryAsync(Guid correlationId = default);

        #endregion

        #region Metrics and Statistics

        /// <summary>
        /// Resets all health monitoring metrics and statistics.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void ResetMetrics(Guid correlationId = default);

        /// <summary>
        /// Gets health monitoring statistics for reporting and analysis.
        /// </summary>
        /// <returns>Health monitoring statistics</returns>
        HealthMonitoringStatistics GetStatistics();

        /// <summary>
        /// Validates health monitoring configuration and setup.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Validation result with any issues found</returns>
        ValidationResult ValidateConfiguration(Guid correlationId = default);

        #endregion

        #region Maintenance Operations

        /// <summary>
        /// Performs routine maintenance operations on health monitoring data.
        /// Cleans up old metrics, rotates logs, and optimizes performance tracking.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void PerformMaintenance(Guid correlationId = default);

        /// <summary>
        /// Archives current health data for long-term analysis.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask representing the archival operation</returns>
        UniTask ArchiveHealthDataAsync(Guid correlationId = default);

        #endregion
    }
}