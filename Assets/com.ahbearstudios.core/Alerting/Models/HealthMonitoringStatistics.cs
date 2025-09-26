using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Statistics for health monitoring operations.
    /// Tracks system health metrics, failure rates, and recovery times.
    /// </summary>
    public readonly record struct HealthMonitoringStatistics
    {
        /// <summary>
        /// Total number of health checks performed.
        /// </summary>
        public long TotalHealthChecks { get; init; }

        /// <summary>
        /// Number of successful health checks.
        /// </summary>
        public long SuccessfulHealthChecks { get; init; }

        /// <summary>
        /// Number of failed health checks.
        /// </summary>
        public long FailedHealthChecks { get; init; }

        /// <summary>
        /// Current consecutive failure count.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Timestamp of last health check.
        /// </summary>
        public DateTime LastHealthCheck { get; init; }

        /// <summary>
        /// Timestamp of last successful health check.
        /// </summary>
        public DateTime LastSuccessfulHealthCheck { get; init; }

        /// <summary>
        /// Number of times emergency mode was activated.
        /// </summary>
        public long EmergencyModeActivations { get; init; }

        /// <summary>
        /// Total time spent in emergency mode.
        /// </summary>
        public TimeSpan TotalEmergencyModeTime { get; init; }

        /// <summary>
        /// Number of emergency escalations performed.
        /// </summary>
        public long EmergencyEscalations { get; init; }

        /// <summary>
        /// Current circuit breaker state.
        /// </summary>
        public CircuitBreakerState CircuitBreakerState { get; init; }

        /// <summary>
        /// Number of times circuit breaker opened.
        /// </summary>
        public long CircuitBreakerOpenCount { get; init; }

        /// <summary>
        /// Average recovery time from circuit breaker open state.
        /// </summary>
        public TimeSpan AverageRecoveryTime { get; init; }

        /// <summary>
        /// System uptime percentage over monitoring period.
        /// </summary>
        public double UptimePercentage { get; init; }

        /// <summary>
        /// Timestamp when statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        /// Creates an empty statistics instance.
        /// </summary>
        public static HealthMonitoringStatistics Empty => new()
        {
            TotalHealthChecks = 0,
            SuccessfulHealthChecks = 0,
            FailedHealthChecks = 0,
            ConsecutiveFailures = 0,
            LastHealthCheck = DateTime.MinValue,
            LastSuccessfulHealthCheck = DateTime.MinValue,
            EmergencyModeActivations = 0,
            TotalEmergencyModeTime = TimeSpan.Zero,
            EmergencyEscalations = 0,
            CircuitBreakerState = CircuitBreakerState.Closed,
            CircuitBreakerOpenCount = 0,
            AverageRecoveryTime = TimeSpan.Zero,
            UptimePercentage = 100.0,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Calculates health check success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalHealthChecks > 0 ? (double)SuccessfulHealthChecks / TotalHealthChecks * 100.0 : 100.0;

        /// <summary>
        /// Calculates health check failure rate as a percentage.
        /// </summary>
        public double FailureRate => TotalHealthChecks > 0 ? (double)FailedHealthChecks / TotalHealthChecks * 100.0 : 0.0;
    }
}