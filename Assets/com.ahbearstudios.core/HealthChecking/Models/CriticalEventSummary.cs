using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Critical event summary for monitoring dashboards.
    /// Provides a summarized view of critical events for display purposes.
    /// </summary>
    public sealed record CriticalEventSummary
    {
        /// <summary>
        /// Gets the timestamp when this summary was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; }

        /// <summary>
        /// Gets the critical events included in this summary.
        /// </summary>
        public IReadOnlyList<CriticalEvent> Events { get; init; } = new List<CriticalEvent>();

        /// <summary>
        /// Gets the event counts by type.
        /// </summary>
        public IReadOnlyDictionary<string, int> EventCountsByType { get; init; } =
            new Dictionary<string, int>();

        /// <summary>
        /// Gets the current overall health status.
        /// </summary>
        public OverallHealthStatus CurrentStatus { get; init; }

        /// <summary>
        /// Creates a new CriticalEventSummary with the specified parameters.
        /// </summary>
        /// <param name="events">Critical events to include</param>
        /// <param name="eventCountsByType">Event counts by type</param>
        /// <param name="currentStatus">Current overall health status</param>
        /// <param name="generatedAt">Generation timestamp (defaults to current time)</param>
        /// <returns>New CriticalEventSummary instance</returns>
        public static CriticalEventSummary Create(
            List<CriticalEvent> events = null,
            Dictionary<string, int> eventCountsByType = null,
            OverallHealthStatus currentStatus = OverallHealthStatus.Unknown,
            DateTime? generatedAt = null)
        {
            return new CriticalEventSummary
            {
                GeneratedAt = generatedAt ?? DateTime.UtcNow,
                Events = events ?? new List<CriticalEvent>(),
                EventCountsByType = eventCountsByType ?? new Dictionary<string, int>(),
                CurrentStatus = currentStatus
            };
        }

        /// <summary>
        /// Returns a string representation of this critical event summary.
        /// </summary>
        /// <returns>Critical event summary</returns>
        public override string ToString()
        {
            return $"CriticalEventSummary: {Events.Count} events, Status: {CurrentStatus}, Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss}";
        }
    }
}