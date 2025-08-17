using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive diagnostic information for the alerting system.
    /// Provides detailed technical information for troubleshooting and monitoring.
    /// Designed for Unity game development with efficient data structures.
    /// </summary>
    public readonly struct AlertSystemDiagnostics
    {
        /// <summary>
        /// Gets the version of the alert service.
        /// </summary>
        public FixedString32Bytes ServiceVersion { get; init; }

        /// <summary>
        /// Gets whether the service is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets whether the service is healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets whether the service has been started.
        /// </summary>
        public bool IsStarted { get; init; }

        /// <summary>
        /// Gets whether emergency mode is active.
        /// </summary>
        public bool EmergencyModeActive { get; init; }

        /// <summary>
        /// Gets the reason for emergency mode activation.
        /// </summary>
        public FixedString512Bytes EmergencyModeReason { get; init; }

        /// <summary>
        /// Gets the number of currently active alerts.
        /// </summary>
        public int ActiveAlertCount { get; init; }

        /// <summary>
        /// Gets the number of alerts in history.
        /// </summary>
        public int HistoryCount { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the timestamp of the last maintenance run.
        /// </summary>
        public DateTime LastMaintenanceRun { get; init; }

        /// <summary>
        /// Gets the timestamp of the last health check.
        /// </summary>
        public DateTime LastHealthCheck { get; init; }

        /// <summary>
        /// Gets the service uptime since last start.
        /// </summary>
        public TimeSpan Uptime { get; init; }

        /// <summary>
        /// Gets the total number of alerts processed since startup.
        /// </summary>
        public long TotalAlertsProcessed { get; init; }

        /// <summary>
        /// Gets the total number of alerts that failed processing.
        /// </summary>
        public long TotalAlertsFailed { get; init; }

        /// <summary>
        /// Gets the configuration summary as a string.
        /// </summary>
        public FixedString512Bytes ConfigurationSummary { get; init; }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets the number of garbage collections performed.
        /// </summary>
        public int GarbageCollectionCount { get; init; }

        /// <summary>
        /// Gets the status of each subsystem.
        /// </summary>
        public IReadOnlyDictionary<string, bool> SubsystemStatuses { get; init; }

        /// <summary>
        /// Gets performance counters for the system.
        /// </summary>
        public IReadOnlyDictionary<string, long> PerformanceCounters { get; init; }

        /// <summary>
        /// Gets any error messages from recent operations.
        /// </summary>
        public FixedString512Bytes RecentErrors { get; init; }

        /// <summary>
        /// Gets the timestamp when diagnostics were captured.
        /// </summary>
        public DateTime DiagnosticTimestamp { get; init; }

        /// <summary>
        /// Creates a basic diagnostic report.
        /// </summary>
        /// <param name="serviceVersion">Version of the service</param>
        /// <param name="isEnabled">Whether service is enabled</param>
        /// <param name="isHealthy">Whether service is healthy</param>
        /// <param name="isStarted">Whether service is started</param>
        /// <returns>New diagnostic report</returns>
        public static AlertSystemDiagnostics Create(
            string serviceVersion,
            bool isEnabled,
            bool isHealthy,
            bool isStarted)
        {
            return new AlertSystemDiagnostics
            {
                ServiceVersion = serviceVersion ?? "Unknown",
                IsEnabled = isEnabled,
                IsHealthy = isHealthy,
                IsStarted = isStarted,
                DiagnosticTimestamp = DateTime.UtcNow,
                SubsystemStatuses = new Dictionary<string, bool>(),
                PerformanceCounters = new Dictionary<string, long>()
            };
        }

        /// <summary>
        /// Gets the success rate for alert processing.
        /// </summary>
        /// <returns>Success rate as percentage (0.0 to 1.0)</returns>
        public double GetSuccessRate()
        {
            if (TotalAlertsProcessed == 0)
                return 1.0; // No alerts processed means 100% success

            return (double)(TotalAlertsProcessed - TotalAlertsFailed) / TotalAlertsProcessed;
        }

        /// <summary>
        /// Gets the failure rate for alert processing.
        /// </summary>
        /// <returns>Failure rate as percentage (0.0 to 1.0)</returns>
        public double GetFailureRate()
        {
            return 1.0 - GetSuccessRate();
        }

        /// <summary>
        /// Gets a summary of key diagnostic metrics.
        /// </summary>
        /// <returns>Diagnostic summary string</returns>
        public override string ToString()
        {
            var successRate = GetSuccessRate() * 100;
            return $"AlertSystemDiagnostics: v{ServiceVersion}, " +
                   $"Enabled={IsEnabled}, Healthy={IsHealthy}, Started={IsStarted}, " +
                   $"Emergency={EmergencyModeActive}, " +
                   $"Active={ActiveAlertCount}, History={HistoryCount}, " +
                   $"Processed={TotalAlertsProcessed}, SuccessRate={successRate:F1}%, " +
                   $"Memory={MemoryUsageBytes / (1024 * 1024)}MB, " +
                   $"Uptime={Uptime.TotalHours:F1}h";
        }

        /// <summary>
        /// Determines if the system is in a critical state requiring immediate attention.
        /// </summary>
        /// <returns>True if system is in critical state</returns>
        public bool IsCritical()
        {
            return !IsHealthy || 
                   ConsecutiveFailures >= 5 || 
                   GetFailureRate() > 0.1 || // More than 10% failure rate
                   !IsEnabled;
        }

        /// <summary>
        /// Gets the overall system health rating.
        /// </summary>
        /// <returns>Health rating from 0 (critical) to 100 (excellent)</returns>
        public int GetHealthRating()
        {
            if (!IsEnabled || !IsStarted)
                return 0;

            int rating = 100;

            // Deduct points for failures
            rating -= ConsecutiveFailures * 10;

            // Deduct points for failure rate
            var failureRate = GetFailureRate();
            rating -= (int)(failureRate * 50);

            // Deduct points for emergency mode
            if (EmergencyModeActive)
                rating -= 20;

            // Deduct points for unhealthy state
            if (!IsHealthy)
                rating -= 30;

            return Math.Max(0, Math.Min(100, rating));
        }
    }
}