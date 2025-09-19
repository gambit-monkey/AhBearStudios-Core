using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Individual health check event for tracking and analysis.
    /// Represents a discrete event in the health checking system.
    /// </summary>
    public sealed record HealthCheckEvent
    {
        /// <summary>
        /// Gets the timestamp when this event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the type of event (e.g., "Started", "Completed", "Failed").
        /// </summary>
        public string EventType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the name of the health check that generated this event.
        /// </summary>
        public string HealthCheckName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the health status associated with this event.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Gets the event message or description.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets additional metadata associated with this event.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new HealthCheckEvent with the specified parameters.
        /// </summary>
        /// <param name="eventType">Type of event</param>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="status">Health status</param>
        /// <param name="message">Event message</param>
        /// <param name="metadata">Additional metadata</param>
        /// <param name="timestamp">Event timestamp (defaults to current time)</param>
        /// <returns>New HealthCheckEvent instance</returns>
        public static HealthCheckEvent Create(
            string eventType,
            string healthCheckName,
            HealthStatus status,
            string message = "",
            Dictionary<string, object> metadata = null,
            DateTime? timestamp = null)
        {
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            return new HealthCheckEvent
            {
                Timestamp = timestamp ?? DateTime.UtcNow,
                EventType = eventType,
                HealthCheckName = healthCheckName,
                Status = status,
                Message = message ?? string.Empty,
                Metadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Returns a string representation of this health check event.
        /// </summary>
        /// <returns>Health check event summary</returns>
        public override string ToString()
        {
            return $"HealthCheckEvent: {EventType} - {HealthCheckName} ({Status}) at {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }
}