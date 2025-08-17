using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Common.Models;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive alerting system statistics for monitoring and performance tracking.
    /// Provides detailed metrics about alert processing, channel performance, and system health.
    /// Designed for Unity game development with zero-allocation patterns. Serialization is handled through ISerializationService.
    /// </summary>
    public readonly record struct AlertStatistics
    {
        /// <summary>
        /// Gets the timestamp when these statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of alerts processed by the system.
        /// </summary>
        public long TotalAlertsProcessed { get; init; }

        /// <summary>
        /// Gets the total number of alerts successfully delivered.
        /// </summary>
        public long TotalAlertsDelivered { get; init; }

        /// <summary>
        /// Gets the total number of alerts that failed delivery.
        /// </summary>
        public long TotalAlertsFailedDelivery { get; init; }

        /// <summary>
        /// Gets the total number of alerts suppressed by filters.
        /// </summary>
        public long TotalAlertsSuppressed { get; init; }

        /// <summary>
        /// Gets the current number of active alerts in the system.
        /// </summary>
        public int CurrentActiveAlerts { get; init; }

        /// <summary>
        /// Gets the current number of acknowledged but unresolved alerts.
        /// </summary>
        public int CurrentAcknowledgedAlerts { get; init; }

        /// <summary>
        /// Gets the alert processing rate per second (recent average).
        /// </summary>
        public double AlertsPerSecond { get; init; }

        /// <summary>
        /// Gets the average alert processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the maximum alert processing time recorded in milliseconds.
        /// </summary>
        public double MaxProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the average alert delivery time in milliseconds.
        /// </summary>
        public double AverageDeliveryTimeMs { get; init; }

        /// <summary>
        /// Gets the maximum alert delivery time recorded in milliseconds.
        /// </summary>
        public double MaxDeliveryTimeMs { get; init; }

        /// <summary>
        /// Gets alert counts by severity level.
        /// </summary>
        public AlertSeverityStatistics SeverityBreakdown { get; init; } = AlertSeverityStatistics.Empty;

        /// <summary>
        /// Gets statistics for registered alert channels.
        /// </summary>
        public IReadOnlyList<ChannelMetrics> ChannelStatistics { get; init; } = Array.Empty<ChannelMetrics>();

        /// <summary>
        /// Gets statistics for active alert filters.
        /// </summary>
        public IReadOnlyList<AlertFilterMetrics> FilterStatistics { get; init; } = Array.Empty<AlertFilterMetrics>();

        /// <summary>
        /// Gets the top alert sources by volume.
        /// </summary>
        public Dictionary<string, long> TopAlertSources { get; init; } = new();

        /// <summary>
        /// Gets the most frequent alert tags.
        /// </summary>
        public Dictionary<string, long> FrequentAlertTags { get; init; } = new();

        /// <summary>
        /// Gets system resource usage during alert processing.
        /// </summary>
        public SystemResourceStats ResourceUsage { get; init; } = SystemResourceStats.Empty;

        /// <summary>
        /// Gets error and exception statistics.
        /// </summary>
        public ErrorStatistics Errors { get; init; } = ErrorStatistics.Empty;

        /// <summary>
        /// Gets the overall delivery success rate as a percentage (0-100).
        /// </summary>
        public double DeliverySuccessRate => TotalAlertsProcessed > 0 
            ? (double)TotalAlertsDelivered / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the alert suppression rate as a percentage (0-100).
        /// </summary>
        public double SuppressionRate => TotalAlertsProcessed > 0 
            ? (double)TotalAlertsSuppressed / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the overall system health score (0-100).
        /// </summary>
        public double SystemHealthScore => CalculateHealthScore();

        /// <summary>
        /// Initializes a new instance of the AlertStatistics struct.
        /// </summary>
        public AlertStatistics()
        {
            TotalAlertsProcessed = 0;
            TotalAlertsDelivered = 0;
            TotalAlertsFailedDelivery = 0;
            TotalAlertsSuppressed = 0;
            CurrentActiveAlerts = 0;
            CurrentAcknowledgedAlerts = 0;
            AlertsPerSecond = 0;
            AverageProcessingTimeMs = 0;
            MaxProcessingTimeMs = 0;
            AverageDeliveryTimeMs = 0;
            MaxDeliveryTimeMs = 0;
        }

        /// <summary>
        /// Creates empty statistics.
        /// </summary>
        /// <returns>Empty statistics instance</returns>
        public static AlertStatistics Empty => new();

        /// <summary>
        /// Creates statistics with current timestamp.
        /// </summary>
        /// <param name="totalProcessed">Total alerts processed</param>
        /// <param name="totalDelivered">Total alerts delivered</param>
        /// <param name="totalFailed">Total failed deliveries</param>
        /// <param name="totalSuppressed">Total suppressed alerts</param>
        /// <param name="currentActive">Current active alerts</param>
        /// <param name="currentAcknowledged">Current acknowledged alerts</param>
        /// <returns>Statistics instance</returns>
        public static AlertStatistics Create(
            long totalProcessed,
            long totalDelivered,
            long totalFailed,
            long totalSuppressed,
            int currentActive,
            int currentAcknowledged)
        {
            return new AlertStatistics
            {
                LastUpdated = DateTime.UtcNow,
                TotalAlertsProcessed = totalProcessed,
                TotalAlertsDelivered = totalDelivered,
                TotalAlertsFailedDelivery = totalFailed,
                TotalAlertsSuppressed = totalSuppressed,
                CurrentActiveAlerts = currentActive,
                CurrentAcknowledgedAlerts = currentAcknowledged
            };
        }

        /// <summary>
        /// Creates a copy with updated timestamp.
        /// </summary>
        /// <returns>Updated statistics instance</returns>
        public AlertStatistics WithUpdatedTimestamp()
        {
            return this with { LastUpdated = DateTime.UtcNow };
        }

        /// <summary>
        /// Merges statistics from multiple sources.
        /// </summary>
        /// <param name="other">Other statistics to merge</param>
        /// <returns>Combined statistics</returns>
        public AlertStatistics Merge(AlertStatistics other)
        {
            if (other == null) return this;

            var mergedChannelStats = ChannelStatistics.Concat(other.ChannelStatistics).ToList();
            var mergedFilterStats = FilterStatistics.Concat(other.FilterStatistics).ToList();
            var mergedTopSources = TopAlertSources.AsValueEnumerable()
                .Concat(other.TopAlertSources.AsValueEnumerable())
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.AsValueEnumerable().Sum(kvp => kvp.Value));
            var mergedFrequentTags = FrequentAlertTags.AsValueEnumerable()
                .Concat(other.FrequentAlertTags.AsValueEnumerable())
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.AsValueEnumerable().Sum(kvp => kvp.Value));

            return new AlertStatistics
            {
                LastUpdated = DateTime.UtcNow,
                TotalAlertsProcessed = TotalAlertsProcessed + other.TotalAlertsProcessed,
                TotalAlertsDelivered = TotalAlertsDelivered + other.TotalAlertsDelivered,
                TotalAlertsFailedDelivery = TotalAlertsFailedDelivery + other.TotalAlertsFailedDelivery,
                TotalAlertsSuppressed = TotalAlertsSuppressed + other.TotalAlertsSuppressed,
                CurrentActiveAlerts = Math.Max(CurrentActiveAlerts, other.CurrentActiveAlerts),
                CurrentAcknowledgedAlerts = Math.Max(CurrentAcknowledgedAlerts, other.CurrentAcknowledgedAlerts),
                AlertsPerSecond = (AlertsPerSecond + other.AlertsPerSecond) / 2,
                AverageProcessingTimeMs = (AverageProcessingTimeMs + other.AverageProcessingTimeMs) / 2,
                MaxProcessingTimeMs = Math.Max(MaxProcessingTimeMs, other.MaxProcessingTimeMs),
                AverageDeliveryTimeMs = (AverageDeliveryTimeMs + other.AverageDeliveryTimeMs) / 2,
                MaxDeliveryTimeMs = Math.Max(MaxDeliveryTimeMs, other.MaxDeliveryTimeMs),
                SeverityBreakdown = SeverityBreakdown.Merge(other.SeverityBreakdown),
                ChannelStatistics = mergedChannelStats,
                FilterStatistics = mergedFilterStats,
                TopAlertSources = mergedTopSources,
                FrequentAlertTags = mergedFrequentTags,
                ResourceUsage = ResourceUsage.Merge(other.ResourceUsage),
                Errors = Errors.Merge(other.Errors)
            };
        }

        /// <summary>
        /// Calculates the overall system health score.
        /// </summary>
        /// <returns>Health score between 0-100</returns>
        private double CalculateHealthScore()
        {
            double score = 100.0;

            // Reduce score based on delivery failure rate
            if (TotalAlertsProcessed > 0)
            {
                var failureRate = (double)TotalAlertsFailedDelivery / TotalAlertsProcessed;
                score -= failureRate * 50; // Up to 50 points for delivery failures
            }

            // Reduce score based on processing time
            if (AverageProcessingTimeMs > 100) // If processing takes more than 100ms
            {
                score -= Math.Min(25, AverageProcessingTimeMs / 10); // Up to 25 points for slow processing
            }

            // Reduce score based on error rate
            score -= Math.Min(15, Errors.ErrorRate * 15); // Up to 15 points for errors

            // Reduce score based on resource usage
            if (ResourceUsage.MemoryUsagePercent > 80)
            {
                score -= 10; // 10 points for high memory usage
            }

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Gets a summary report of the statistics.
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummaryReport()
        {
            return $@"Alert System Statistics (Updated: {LastUpdated:yyyy-MM-dd HH:mm:ss UTC})
Total Processed: {TotalAlertsProcessed:N0}
Successfully Delivered: {TotalAlertsDelivered:N0} ({DeliverySuccessRate:F1}%)
Failed Delivery: {TotalAlertsFailedDelivery:N0}
Suppressed: {TotalAlertsSuppressed:N0} ({SuppressionRate:F1}%)
Currently Active: {CurrentActiveAlerts:N0}
Currently Acknowledged: {CurrentAcknowledgedAlerts:N0}
Processing Rate: {AlertsPerSecond:F1} alerts/sec
Avg Processing Time: {AverageProcessingTimeMs:F1}ms
Avg Delivery Time: {AverageDeliveryTimeMs:F1}ms
System Health Score: {SystemHealthScore:F1}/100";
        }

    }

}