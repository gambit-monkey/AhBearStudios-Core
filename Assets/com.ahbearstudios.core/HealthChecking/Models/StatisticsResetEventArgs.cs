using System;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Event arguments for statistics reset events.
    /// </summary>
    public sealed record StatisticsResetEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the reset.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the reason for the reset.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets whether this was a full system reset or partial.
        /// </summary>
        public bool IsFullReset { get; init; }

        /// <summary>
        /// Gets the name of the specific health check if partial reset.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }
    }
}