namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Alert statistics broken down by severity level.
    /// </summary>
    public readonly record struct AlertSeverityStatistics
    {
        /// <summary>
        /// Gets the count of critical alerts.
        /// </summary>
        public long CriticalCount { get; init; }

        /// <summary>
        /// Gets the count of error alerts.
        /// </summary>
        public long ErrorCount { get; init; }

        /// <summary>
        /// Gets the count of warning alerts.
        /// </summary>
        public long WarningCount { get; init; }

        /// <summary>
        /// Gets the count of information alerts.
        /// </summary>
        public long InformationCount { get; init; }

        /// <summary>
        /// Gets the count of debug alerts.
        /// </summary>
        public long DebugCount { get; init; }

        /// <summary>
        /// Gets the total count across all severities.
        /// </summary>
        public long TotalCount => CriticalCount + ErrorCount + WarningCount + InformationCount + DebugCount;

        /// <summary>
        /// Creates empty severity statistics.
        /// </summary>
        public static AlertSeverityStatistics Empty => new();

        /// <summary>
        /// Gets count for specific severity.
        /// </summary>
        /// <param name="severity">Alert severity</param>
        /// <returns>Count for severity</returns>
        public long GetCount(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Critical => CriticalCount,
                AlertSeverity.Emergency => ErrorCount,
                AlertSeverity.Warning => WarningCount,
                AlertSeverity.Info => InformationCount,
                AlertSeverity.Debug => DebugCount,
                _ => 0
            };
        }

        /// <summary>
        /// Merges with other severity statistics.
        /// </summary>
        /// <param name="other">Other statistics to merge</param>
        /// <returns>Merged statistics</returns>
        public AlertSeverityStatistics Merge(AlertSeverityStatistics other)
        {
            return new AlertSeverityStatistics
            {
                CriticalCount = CriticalCount + other.CriticalCount,
                ErrorCount = ErrorCount + other.ErrorCount,
                WarningCount = WarningCount + other.WarningCount,
                InformationCount = InformationCount + other.InformationCount,
                DebugCount = DebugCount + other.DebugCount
            };
        }
    }
}