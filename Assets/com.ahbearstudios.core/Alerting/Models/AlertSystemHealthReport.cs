using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive health report for the entire alerting system.
    /// Provides detailed status information about all subsystems and their health.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly struct AlertSystemHealthReport
    {
        /// <summary>
        /// Gets the timestamp when this health report was generated.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the overall health status of the alerting system.
        /// </summary>
        public bool OverallHealth { get; init; }

        /// <summary>
        /// Gets whether the main alert service is enabled.
        /// </summary>
        public bool ServiceEnabled { get; init; }

        /// <summary>
        /// Gets whether emergency mode is currently active.
        /// </summary>
        public bool EmergencyModeActive { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures detected.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the timestamp of the last health check performed.
        /// </summary>
        public DateTime LastHealthCheck { get; init; }

        /// <summary>
        /// Gets whether the channel service is healthy.
        /// </summary>
        public bool ChannelServiceHealth { get; init; }

        /// <summary>
        /// Gets the number of healthy channels.
        /// </summary>
        public int HealthyChannelCount { get; init; }

        /// <summary>
        /// Gets the total number of registered channels.
        /// </summary>
        public int TotalChannelCount { get; init; }

        /// <summary>
        /// Gets whether the filter service is healthy.
        /// </summary>
        public bool FilterServiceHealth { get; init; }

        /// <summary>
        /// Gets the number of active filters.
        /// </summary>
        public int ActiveFilterCount { get; init; }

        /// <summary>
        /// Gets whether the suppression service is healthy.
        /// </summary>
        public bool SuppressionServiceHealth { get; init; }

        /// <summary>
        /// Gets the number of active suppression rules.
        /// </summary>
        public int SuppressionRuleCount { get; init; }

        /// <summary>
        /// Gets the current number of active alerts.
        /// </summary>
        public int ActiveAlertCount { get; init; }

        /// <summary>
        /// Gets the current memory usage in bytes (approximation).
        /// </summary>
        public long MemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets the average response time for alert processing in milliseconds.
        /// </summary>
        public double AverageResponseTimeMs { get; init; }

        /// <summary>
        /// Gets any critical issues detected during the health check.
        /// </summary>
        public FixedString512Bytes CriticalIssues { get; init; }

        /// <summary>
        /// Gets any warnings detected during the health check.
        /// </summary>
        public FixedString512Bytes Warnings { get; init; }

        /// <summary>
        /// Gets recommendations for improving system health.
        /// </summary>
        public FixedString512Bytes Recommendations { get; init; }

        /// <summary>
        /// Creates a new health report with basic information.
        /// </summary>
        /// <param name="overallHealth">Overall system health</param>
        /// <param name="serviceEnabled">Whether service is enabled</param>
        /// <param name="emergencyMode">Whether emergency mode is active</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <returns>New health report instance</returns>
        public static AlertSystemHealthReport Create(
            bool overallHealth,
            bool serviceEnabled,
            bool emergencyMode = false,
            int consecutiveFailures = 0)
        {
            return new AlertSystemHealthReport
            {
                Timestamp = DateTime.UtcNow,
                OverallHealth = overallHealth,
                ServiceEnabled = serviceEnabled,
                EmergencyModeActive = emergencyMode,
                ConsecutiveFailures = consecutiveFailures,
                LastHealthCheck = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets a summary of the health report for logging.
        /// </summary>
        /// <returns>Health report summary</returns>
        public override string ToString()
        {
            return $"AlertSystemHealth: Overall={OverallHealth}, Enabled={ServiceEnabled}, " +
                   $"Emergency={EmergencyModeActive}, Failures={ConsecutiveFailures}, " +
                   $"Channels={HealthyChannelCount}/{TotalChannelCount}, " +
                   $"Filters={ActiveFilterCount}, Alerts={ActiveAlertCount}";
        }

        /// <summary>
        /// Gets the health status as a simple enum value.
        /// </summary>
        /// <returns>Health status level</returns>
        public HealthStatus GetHealthStatus()
        {
            if (!ServiceEnabled)
                return HealthStatus.Offline;

            if (ConsecutiveFailures >= 5)
                return HealthStatus.Critical;

            if (ConsecutiveFailures >= 3 || EmergencyModeActive)
                return HealthStatus.Warning;

            if (OverallHealth && ChannelServiceHealth && FilterServiceHealth && SuppressionServiceHealth)
                return HealthStatus.Healthy;

            return HealthStatus.Degraded;
        }
    }
}