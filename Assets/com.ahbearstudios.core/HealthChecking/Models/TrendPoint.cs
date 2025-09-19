using System;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Trend data point for health check analysis.
    /// Represents a single data point in trend analysis.
    /// </summary>
    public sealed record TrendPoint
    {
        /// <summary>
        /// Gets the timestamp of this data point.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the value at this data point.
        /// </summary>
        public double Value { get; init; }

        /// <summary>
        /// Gets the health status at this data point.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Creates a new TrendPoint with the specified parameters.
        /// </summary>
        /// <param name="timestamp">Timestamp of the data point</param>
        /// <param name="value">Value at this point</param>
        /// <param name="status">Health status at this point</param>
        /// <returns>New TrendPoint instance</returns>
        public static TrendPoint Create(
            DateTime timestamp,
            double value,
            HealthStatus status = HealthStatus.Unknown)
        {
            return new TrendPoint
            {
                Timestamp = timestamp,
                Value = value,
                Status = status
            };
        }

        /// <summary>
        /// Returns a string representation of this trend point.
        /// </summary>
        /// <returns>Trend point summary</returns>
        public override string ToString()
        {
            return $"TrendPoint: {Value:F2} ({Status}) at {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }
}