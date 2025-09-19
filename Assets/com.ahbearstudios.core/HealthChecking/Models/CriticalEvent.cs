using System;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Critical event information for monitoring and alerting.
    /// Represents a significant health event requiring attention.
    /// </summary>
    public sealed record CriticalEvent
    {
        /// <summary>
        /// Gets the timestamp when this critical event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the name of the health check that generated this event.
        /// </summary>
        public string HealthCheckName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the severity level of this critical event.
        /// </summary>
        public HealthStatus Severity { get; init; }

        /// <summary>
        /// Gets the description of this critical event.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets the impact description of this critical event.
        /// </summary>
        public string Impact { get; init; } = string.Empty;

        /// <summary>
        /// Creates a new CriticalEvent with the specified parameters.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="severity">Severity level</param>
        /// <param name="description">Event description</param>
        /// <param name="impact">Impact description</param>
        /// <param name="timestamp">Event timestamp (defaults to current time)</param>
        /// <returns>New CriticalEvent instance</returns>
        public static CriticalEvent Create(
            string healthCheckName,
            HealthStatus severity,
            string description,
            string impact = "",
            DateTime? timestamp = null)
        {
            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description cannot be null or empty", nameof(description));

            return new CriticalEvent
            {
                Timestamp = timestamp ?? DateTime.UtcNow,
                HealthCheckName = healthCheckName,
                Severity = severity,
                Description = description,
                Impact = impact ?? string.Empty
            };
        }

        /// <summary>
        /// Returns a string representation of this critical event.
        /// </summary>
        /// <returns>Critical event summary</returns>
        public override string ToString()
        {
            return $"CriticalEvent: {HealthCheckName} ({Severity}) - {Description} at {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }
}