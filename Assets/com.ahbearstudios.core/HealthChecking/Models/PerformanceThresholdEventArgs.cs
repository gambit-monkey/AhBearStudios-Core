using System;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Event arguments for performance threshold exceeded events.
    /// </summary>
    public sealed record PerformanceThresholdEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the threshold violation.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the type of threshold that was exceeded.
        /// </summary>
        public PerformanceThresholdType ThresholdType { get; init; }

        /// <summary>
        /// Gets the name of the health check that exceeded the threshold.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the actual value that exceeded the threshold.
        /// </summary>
        public double ActualValue { get; init; }

        /// <summary>
        /// Gets the threshold value that was exceeded.
        /// </summary>
        public double ThresholdValue { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets additional context about the threshold violation.
        /// </summary>
        public string Context { get; init; }
    }
}