using System;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration for alert aggregation when suppression action is set to Aggregate.
    /// Defines how alerts are grouped together and when aggregated alerts are dispatched.
    /// </summary>
    public sealed record AggregationConfig
    {
        /// <summary>
        /// Gets the field used for grouping alerts during aggregation.
        /// Alerts with the same group key are aggregated together.
        /// </summary>
        public AggregationGroupBy GroupBy { get; init; } = AggregationGroupBy.Source;

        /// <summary>
        /// Gets the maximum number of alerts that can be aggregated into a single group.
        /// When this limit is reached, the aggregated alert is immediately dispatched.
        /// </summary>
        public int MaxGroupSize { get; init; } = 20;

        /// <summary>
        /// Gets the maximum time to wait before flushing a partial aggregation group.
        /// Ensures aggregated alerts are not delayed indefinitely.
        /// </summary>
        public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets whether statistical information is included in aggregated alerts.
        /// When enabled, aggregated alerts include count, frequency, and timing statistics.
        /// </summary>
        public bool IncludeStatistics { get; init; } = true;

        /// <summary>
        /// Gets whether the original alert details are preserved in the aggregated alert.
        /// When enabled, the first and last alerts in the group are included for context.
        /// </summary>
        public bool PreserveDetails { get; init; } = true;

        /// <summary>
        /// Gets the severity level assigned to aggregated alerts.
        /// Can be different from the original alerts to indicate aggregation.
        /// </summary>
        public AlertSeverity AggregatedSeverity { get; init; } = AlertSeverity.Warning;

        /// <summary>
        /// Gets whether aggregated alerts should use the highest severity from the group.
        /// When true, AggregatedSeverity is ignored and the highest severity is used.
        /// </summary>
        public bool UseHighestSeverity { get; init; } = true;

        /// <summary>
        /// Gets the message template for aggregated alerts.
        /// Supports placeholders: {Count}, {GroupKey}, {TimeSpan}, {FirstMessage}, {LastMessage}.
        /// </summary>
        public string MessageTemplate { get; init; } = "Aggregated {Count} alerts from {GroupKey} over {TimeSpan}";

        /// <summary>
        /// Validates the aggregation configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (MaxGroupSize < 2)
                throw new InvalidOperationException("Max group size must be at least 2.");

            if (FlushInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Flush interval must be greater than zero.");

            if (string.IsNullOrWhiteSpace(MessageTemplate))
                throw new InvalidOperationException("Message template cannot be empty or whitespace.");
        }

        /// <summary>
        /// Gets the default aggregation configuration.
        /// </summary>
        public static AggregationConfig Default => new()
        {
            GroupBy = AggregationGroupBy.Source,
            MaxGroupSize = 20,
            FlushInterval = TimeSpan.FromMinutes(5),
            IncludeStatistics = true,
            PreserveDetails = true,
            AggregatedSeverity = AlertSeverity.Warning,
            UseHighestSeverity = true,
            MessageTemplate = "Aggregated {Count} alerts from {GroupKey} over {TimeSpan}"
        };
    }
}