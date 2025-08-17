using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive performance metrics for the alerting system.
    /// Tracks performance counters, timing data, and throughput statistics.
    /// Designed for Unity game development with zero-allocation monitoring.
    /// </summary>
    public struct AlertSystemPerformanceMetrics
    {
        /// <summary>
        /// Gets the timestamp when metrics collection started.
        /// </summary>
        public DateTime MetricsStartTime { get; set; }

        /// <summary>
        /// Gets the timestamp when metrics were last updated.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Gets the total number of alerts processed.
        /// </summary>
        public long TotalAlertsProcessed { get; set; }

        /// <summary>
        /// Gets the number of alerts processed successfully.
        /// </summary>
        public long SuccessfulAlerts { get; set; }

        /// <summary>
        /// Gets the number of alerts that failed processing.
        /// </summary>
        public long FailedAlerts { get; set; }

        /// <summary>
        /// Gets the number of alerts that were suppressed.
        /// </summary>
        public long SuppressedAlerts { get; set; }

        /// <summary>
        /// Gets the minimum processing time in milliseconds.
        /// </summary>
        public double MinProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets the maximum processing time in milliseconds.
        /// </summary>
        public double MaxProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets the average processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets the 95th percentile processing time in milliseconds.
        /// </summary>
        public double P95ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets the current alerts per second rate.
        /// </summary>
        public double AlertsPerSecond { get; set; }

        /// <summary>
        /// Gets the peak alerts per second rate recorded.
        /// </summary>
        public double PeakAlertsPerSecond { get; set; }

        /// <summary>
        /// Gets the total time spent processing alerts in milliseconds.
        /// </summary>
        public double TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets the number of bulk operations performed.
        /// </summary>
        public long BulkOperationsCount { get; set; }

        /// <summary>
        /// Gets the average time for bulk operations in milliseconds.
        /// </summary>
        public double AverageBulkOperationTimeMs { get; set; }

        /// <summary>
        /// Gets the number of health checks performed.
        /// </summary>
        public long HealthChecksPerformed { get; set; }

        /// <summary>
        /// Gets the average health check time in milliseconds.
        /// </summary>
        public double AverageHealthCheckTimeMs { get; set; }

        /// <summary>
        /// Gets the number of configuration reloads performed.
        /// </summary>
        public long ConfigurationReloads { get; set; }

        /// <summary>
        /// Gets the number of emergency escalations performed.
        /// </summary>
        public long EmergencyEscalations { get; set; }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        public long CurrentMemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets the number of garbage collections triggered.
        /// </summary>
        public int GarbageCollections { get; set; }

        /// <summary>
        /// Gets the number of times the circuit breaker was triggered.
        /// </summary>
        public long CircuitBreakerTriggers { get; set; }

        /// <summary>
        /// Gets channel-specific performance metrics.
        /// </summary>
        public Dictionary<string, ChannelPerformanceMetrics> ChannelMetrics { get; set; }

        /// <summary>
        /// Gets filter-specific performance metrics.
        /// </summary>
        public Dictionary<string, FilterPerformanceMetrics> FilterMetrics { get; set; }

        /// <summary>
        /// Creates a new performance metrics instance with default values.
        /// </summary>
        /// <returns>New performance metrics instance</returns>
        public static AlertSystemPerformanceMetrics Create()
        {
            var now = DateTime.UtcNow;
            return new AlertSystemPerformanceMetrics
            {
                MetricsStartTime = now,
                LastUpdateTime = now,
                MinProcessingTimeMs = double.MaxValue,
                MaxProcessingTimeMs = 0,
                ChannelMetrics = new Dictionary<string, ChannelPerformanceMetrics>(),
                FilterMetrics = new Dictionary<string, FilterPerformanceMetrics>()
            };
        }

        /// <summary>
        /// Resets all performance metrics to initial values.
        /// </summary>
        public void Reset()
        {
            var now = DateTime.UtcNow;
            MetricsStartTime = now;
            LastUpdateTime = now;
            TotalAlertsProcessed = 0;
            SuccessfulAlerts = 0;
            FailedAlerts = 0;
            SuppressedAlerts = 0;
            MinProcessingTimeMs = double.MaxValue;
            MaxProcessingTimeMs = 0;
            AverageProcessingTimeMs = 0;
            P95ProcessingTimeMs = 0;
            AlertsPerSecond = 0;
            PeakAlertsPerSecond = 0;
            TotalProcessingTimeMs = 0;
            BulkOperationsCount = 0;
            AverageBulkOperationTimeMs = 0;
            HealthChecksPerformed = 0;
            AverageHealthCheckTimeMs = 0;
            ConfigurationReloads = 0;
            EmergencyEscalations = 0;
            CurrentMemoryUsageBytes = 0;
            PeakMemoryUsageBytes = 0;
            GarbageCollections = 0;
            CircuitBreakerTriggers = 0;
            ChannelMetrics?.Clear();
            FilterMetrics?.Clear();
        }

        /// <summary>
        /// Records a successful alert processing operation.
        /// </summary>
        /// <param name="processingTimeMs">Time taken to process the alert</param>
        public void RecordSuccessfulAlert(double processingTimeMs)
        {
            TotalAlertsProcessed++;
            SuccessfulAlerts++;
            RecordProcessingTime(processingTimeMs);
            UpdateThroughputMetrics();
        }

        /// <summary>
        /// Records a failed alert processing operation.
        /// </summary>
        /// <param name="processingTimeMs">Time taken before failure</param>
        public void RecordFailedAlert(double processingTimeMs)
        {
            TotalAlertsProcessed++;
            FailedAlerts++;
            RecordProcessingTime(processingTimeMs);
            UpdateThroughputMetrics();
        }

        /// <summary>
        /// Records a suppressed alert.
        /// </summary>
        public void RecordSuppressedAlert()
        {
            SuppressedAlerts++;
            UpdateThroughputMetrics();
        }

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        /// <returns>Success rate (0.0 to 1.0)</returns>
        public double GetSuccessRate()
        {
            return TotalAlertsProcessed > 0 ? (double)SuccessfulAlerts / TotalAlertsProcessed : 1.0;
        }

        /// <summary>
        /// Gets the failure rate as a percentage.
        /// </summary>
        /// <returns>Failure rate (0.0 to 1.0)</returns>
        public double GetFailureRate()
        {
            return TotalAlertsProcessed > 0 ? (double)FailedAlerts / TotalAlertsProcessed : 0.0;
        }

        /// <summary>
        /// Gets the suppression rate as a percentage.
        /// </summary>
        /// <returns>Suppression rate (0.0 to 1.0)</returns>
        public double GetSuppressionRate()
        {
            var totalAttempts = TotalAlertsProcessed + SuppressedAlerts;
            return totalAttempts > 0 ? (double)SuppressedAlerts / totalAttempts : 0.0;
        }

        /// <summary>
        /// Gets the total runtime since metrics collection started.
        /// </summary>
        /// <returns>Runtime duration</returns>
        public TimeSpan GetRuntime()
        {
            return DateTime.UtcNow - MetricsStartTime;
        }

        /// <summary>
        /// Gets a summary of the performance metrics.
        /// </summary>
        /// <returns>Performance metrics summary</returns>
        public override string ToString()
        {
            var runtime = GetRuntime();
            var successRate = GetSuccessRate() * 100;
            return $"AlertSystemPerformance: Runtime={runtime.TotalHours:F1}h, " +
                   $"Processed={TotalAlertsProcessed}, Success={successRate:F1}%, " +
                   $"AvgTime={AverageProcessingTimeMs:F2}ms, " +
                   $"Throughput={AlertsPerSecond:F1}/s, " +
                   $"Memory={CurrentMemoryUsageBytes / (1024 * 1024)}MB";
        }

        private void RecordProcessingTime(double processingTimeMs)
        {
            if (processingTimeMs < MinProcessingTimeMs)
                MinProcessingTimeMs = processingTimeMs;
            
            if (processingTimeMs > MaxProcessingTimeMs)
                MaxProcessingTimeMs = processingTimeMs;

            TotalProcessingTimeMs += processingTimeMs;
            AverageProcessingTimeMs = TotalProcessingTimeMs / TotalAlertsProcessed;
            LastUpdateTime = DateTime.UtcNow;
        }

        private void UpdateThroughputMetrics()
        {
            var runtime = GetRuntime();
            if (runtime.TotalSeconds > 0)
            {
                AlertsPerSecond = TotalAlertsProcessed / runtime.TotalSeconds;
                if (AlertsPerSecond > PeakAlertsPerSecond)
                    PeakAlertsPerSecond = AlertsPerSecond;
            }
        }
    }

    /// <summary>
    /// Performance metrics specific to an alert channel.
    /// </summary>
    public struct ChannelPerformanceMetrics
    {
        public long AlertsSent { get; set; }
        public long AlertsFailed { get; set; }
        public double AverageDeliveryTimeMs { get; set; }
        public double MaxDeliveryTimeMs { get; set; }
        public long HealthChecksFailed { get; set; }
    }

    /// <summary>
    /// Performance metrics specific to an alert filter.
    /// </summary>
    public struct FilterPerformanceMetrics
    {
        public long AlertsProcessed { get; set; }
        public long AlertsFiltered { get; set; }
        public double AverageFilterTimeMs { get; set; }
        public double MaxFilterTimeMs { get; set; }
        public long FilterErrors { get; set; }
    }
}