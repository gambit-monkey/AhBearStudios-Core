using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance metrics for alert filters.
    /// Tracks filtering effectiveness, performance, and operational statistics.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertFilterMetrics
    {
        /// <summary>
        /// Gets the filter name.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the filter type.
        /// </summary>
        public FilterType FilterType { get; init; }

        /// <summary>
        /// Gets the total number of alerts processed by this filter.
        /// </summary>
        public long TotalAlertsProcessed { get; init; }

        /// <summary>
        /// Gets the number of alerts allowed through the filter.
        /// </summary>
        public long AlertsAllowed { get; init; }

        /// <summary>
        /// Gets the number of alerts blocked by the filter.
        /// </summary>
        public long AlertsBlocked { get; init; }

        /// <summary>
        /// Gets the number of errors during filter processing.
        /// </summary>
        public long ProcessingErrors { get; init; }

        /// <summary>
        /// Gets the total processing time for all alerts.
        /// </summary>
        public TimeSpan TotalProcessingTime { get; init; }

        /// <summary>
        /// Gets the last processing timestamp.
        /// </summary>
        public DateTime? LastProcessingTime { get; init; }

        /// <summary>
        /// Gets the filter effectiveness rate as a percentage (0-100).
        /// </summary>
        public double FilterEffectiveness => TotalAlertsProcessed > 0 
            ? (double)AlertsBlocked / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the pass rate as a percentage (0-100).
        /// </summary>
        public double PassRate => TotalAlertsProcessed > 0 
            ? (double)AlertsAllowed / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the error rate as a percentage (0-100).
        /// </summary>
        public double ErrorRate => TotalAlertsProcessed > 0 
            ? (double)ProcessingErrors / TotalAlertsProcessed * 100 
            : 0;

        /// <summary>
        /// Gets the average processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs => TotalAlertsProcessed > 0 
            ? TotalProcessingTime.TotalMilliseconds / TotalAlertsProcessed 
            : 0;

        /// <summary>
        /// Creates empty filter metrics.
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <param name="filterType">Filter type</param>
        /// <returns>Empty metrics instance</returns>
        public static AlertFilterMetrics CreateEmpty(FixedString64Bytes filterName, FilterType filterType)
        {
            return new AlertFilterMetrics
            {
                FilterName = filterName,
                FilterType = filterType,
                TotalAlertsProcessed = 0,
                AlertsAllowed = 0,
                AlertsBlocked = 0,
                ProcessingErrors = 0,
                TotalProcessingTime = TimeSpan.Zero,
                LastProcessingTime = null
            };
        }

        /// <summary>
        /// Creates a copy with updated processing results.
        /// </summary>
        /// <param name="allowed">Whether the alert was allowed</param>
        /// <param name="processingTime">Time taken to process</param>
        /// <param name="hadError">Whether an error occurred</param>
        /// <returns>Updated metrics instance</returns>
        public AlertFilterMetrics WithProcessingResult(bool allowed, TimeSpan processingTime, bool hadError = false)
        {
            return this with
            {
                TotalAlertsProcessed = TotalAlertsProcessed + 1,
                AlertsAllowed = allowed ? AlertsAllowed + 1 : AlertsAllowed,
                AlertsBlocked = allowed ? AlertsBlocked : AlertsBlocked + 1,
                ProcessingErrors = hadError ? ProcessingErrors + 1 : ProcessingErrors,
                TotalProcessingTime = TotalProcessingTime + processingTime,
                LastProcessingTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Merges with other filter metrics.
        /// </summary>
        /// <param name="other">Other metrics to merge</param>
        /// <returns>Merged metrics</returns>
        public AlertFilterMetrics Merge(AlertFilterMetrics other)
        {
            return new AlertFilterMetrics
            {
                FilterName = FilterName,
                FilterType = FilterType,
                TotalAlertsProcessed = TotalAlertsProcessed + other.TotalAlertsProcessed,
                AlertsAllowed = AlertsAllowed + other.AlertsAllowed,
                AlertsBlocked = AlertsBlocked + other.AlertsBlocked,
                ProcessingErrors = ProcessingErrors + other.ProcessingErrors,
                TotalProcessingTime = TotalProcessingTime + other.TotalProcessingTime,
                LastProcessingTime = other.LastProcessingTime ?? LastProcessingTime
            };
        }
    }
}