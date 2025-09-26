using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Statistics for alert state management operations.
    /// Tracks performance and usage metrics for state management service.
    /// </summary>
    public readonly record struct AlertStateStatistics
    {
        /// <summary>
        /// Number of currently active alerts.
        /// </summary>
        public int ActiveAlertCount { get; init; }

        /// <summary>
        /// Number of alerts in history.
        /// </summary>
        public int HistoryCount { get; init; }

        /// <summary>
        /// Number of alerts acknowledged.
        /// </summary>
        public long TotalAcknowledged { get; init; }

        /// <summary>
        /// Number of alerts resolved.
        /// </summary>
        public long TotalResolved { get; init; }

        /// <summary>
        /// Number of source severity overrides configured.
        /// </summary>
        public int SourceSeverityOverrides { get; init; }

        /// <summary>
        /// Average time to acknowledge alerts.
        /// </summary>
        public TimeSpan AverageAcknowledgmentTime { get; init; }

        /// <summary>
        /// Average time to resolve alerts.
        /// </summary>
        public TimeSpan AverageResolutionTime { get; init; }

        /// <summary>
        /// Timestamp when statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        /// Creates an empty statistics instance.
        /// </summary>
        public static AlertStateStatistics Empty => new()
        {
            ActiveAlertCount = 0,
            HistoryCount = 0,
            TotalAcknowledged = 0,
            TotalResolved = 0,
            SourceSeverityOverrides = 0,
            AverageAcknowledgmentTime = TimeSpan.Zero,
            AverageResolutionTime = TimeSpan.Zero,
            LastUpdated = DateTime.UtcNow
        };
    }
}