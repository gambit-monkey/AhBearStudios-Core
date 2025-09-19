using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Correlated events for troubleshooting health check issues.
    /// Provides event correlation analysis for debugging and monitoring.
    /// </summary>
    public sealed record CorrelatedHealthEvents
    {
        /// <summary>
        /// Gets the correlation ID that ties these events together.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the primary events in this correlation.
        /// </summary>
        public IReadOnlyList<HealthCheckEvent> Events { get; init; } = new List<HealthCheckEvent>();

        /// <summary>
        /// Gets related events grouped by relationship type.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<HealthCheckEvent>> RelatedEvents { get; init; } =
            new Dictionary<string, IReadOnlyList<HealthCheckEvent>>();

        /// <summary>
        /// Gets the total duration covered by all correlated events.
        /// </summary>
        public TimeSpan TotalDuration { get; init; }

        /// <summary>
        /// Gets the identified root cause of the correlated events.
        /// </summary>
        public string RootCause { get; init; } = string.Empty;

        /// <summary>
        /// Creates a new CorrelatedHealthEvents with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="events">Primary events</param>
        /// <param name="relatedEvents">Related events by relationship type</param>
        /// <param name="totalDuration">Total duration</param>
        /// <param name="rootCause">Identified root cause</param>
        /// <returns>New CorrelatedHealthEvents instance</returns>
        public static CorrelatedHealthEvents Create(
            Guid correlationId,
            List<HealthCheckEvent> events = null,
            Dictionary<string, List<HealthCheckEvent>> relatedEvents = null,
            TimeSpan totalDuration = default,
            string rootCause = "")
        {
            var relatedEventsReadOnly = new Dictionary<string, IReadOnlyList<HealthCheckEvent>>();
            if (relatedEvents != null)
            {
                foreach (var kvp in relatedEvents)
                {
                    relatedEventsReadOnly[kvp.Key] = kvp.Value?.AsReadOnly() ?? new List<HealthCheckEvent>().AsReadOnly();
                }
            }

            return new CorrelatedHealthEvents
            {
                CorrelationId = correlationId,
                Events = events?.AsReadOnly() ?? new List<HealthCheckEvent>().AsReadOnly(),
                RelatedEvents = relatedEventsReadOnly,
                TotalDuration = totalDuration,
                RootCause = rootCause ?? string.Empty
            };
        }

        /// <summary>
        /// Returns a string representation of this correlated events collection.
        /// </summary>
        /// <returns>Correlated events summary</returns>
        public override string ToString()
        {
            return $"CorrelatedHealthEvents: {Events.Count} events, {RelatedEvents.Count} related groups, {TotalDuration.TotalSeconds:F1}s duration";
        }
    }
}