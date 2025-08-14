using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Defines the contract for alert filtering in the AhBearStudios Core Alert System.
    /// Filters determine whether alerts should be processed, suppressed, or modified before delivery.
    /// Designed for Unity game development with zero-allocation patterns and high-performance evaluation.
    /// </summary>
    public interface IAlertFilter : IDisposable
    {
        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets whether this filter is currently enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the priority order for this filter.
        /// Lower numbers indicate higher priority (executed first).
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Gets the filter execution statistics.
        /// </summary>
        FilterStatistics Statistics { get; }

        /// <summary>
        /// Evaluates whether an alert should be allowed through the filter.
        /// This is the core filtering logic that determines alert fate.
        /// </summary>
        /// <param name="alert">The alert to evaluate</param>
        /// <param name="context">Additional filtering context</param>
        /// <returns>Filter decision result</returns>
        FilterResult Evaluate(Alert alert, FilterContext context = default);

        /// <summary>
        /// Determines if this filter can handle the specified alert type.
        /// Used for optimization to skip unnecessary filter evaluations.
        /// </summary>
        /// <param name="alert">The alert to check</param>
        /// <returns>True if this filter applies to the alert</returns>
        bool CanHandle(Alert alert);

        /// <summary>
        /// Configures the filter with new settings.
        /// Implementation should validate configuration and update behavior.
        /// </summary>
        /// <param name="configuration">Filter configuration data</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if configuration was applied successfully</returns>
        bool Configure(Dictionary<string, object> configuration, Guid correlationId = default);

        /// <summary>
        /// Validates the filter configuration without applying changes.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        FilterValidationResult ValidateConfiguration(Dictionary<string, object> configuration);

        /// <summary>
        /// Resets filter statistics and internal state.
        /// Used for maintenance and monitoring purposes.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void Reset(Guid correlationId = default);

        /// <summary>
        /// Gets detailed information about the filter's current state and configuration.
        /// </summary>
        /// <returns>Filter diagnostic information</returns>
        FilterDiagnostics GetDiagnostics();

        // Message bus integration for filter events
        // Events have been replaced with IMessage pattern for better decoupling
        // FilterConfigurationChangedMessage and FilterStatisticsUpdatedMessage
        // are published through IMessageBusService
    }

    /// <summary>
    /// Result of a filter evaluation operation.
    /// </summary>
    public readonly record struct FilterResult
    {
        /// <summary>
        /// Gets the filter decision.
        /// </summary>
        public FilterDecision Decision { get; init; }

        /// <summary>
        /// Gets the reason for the filter decision.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        /// <summary>
        /// Gets any modifications to be applied to the alert.
        /// </summary>
        public Alert ModifiedAlert { get; init; }

        /// <summary>
        /// Gets additional metadata from the filter evaluation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; }

        /// <summary>
        /// Gets the processing time for this filter evaluation in ticks.
        /// </summary>
        public long ProcessingTimeTicks { get; init; }

        /// <summary>
        /// Gets the processing time as TimeSpan.
        /// </summary>
        public TimeSpan ProcessingTime => new TimeSpan(ProcessingTimeTicks);

        /// <summary>
        /// Creates an allow result.
        /// </summary>
        /// <param name="reason">Optional reason</param>
        /// <param name="processingTime">Processing duration</param>
        /// <returns>Allow filter result</returns>
        public static FilterResult Allow(string reason = "Filter passed", TimeSpan processingTime = default)
        {
            return new FilterResult
            {
                Decision = FilterDecision.Allow,
                Reason = reason,
                ProcessingTimeTicks = processingTime.Ticks,
                Metadata = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a suppress result.
        /// </summary>
        /// <param name="reason">Suppression reason</param>
        /// <param name="processingTime">Processing duration</param>
        /// <returns>Suppress filter result</returns>
        public static FilterResult Suppress(string reason, TimeSpan processingTime = default)
        {
            return new FilterResult
            {
                Decision = FilterDecision.Suppress,
                Reason = reason,
                ProcessingTimeTicks = processingTime.Ticks,
                Metadata = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a modify result with an altered alert.
        /// </summary>
        /// <param name="modifiedAlert">The modified alert</param>
        /// <param name="reason">Modification reason</param>
        /// <param name="processingTime">Processing duration</param>
        /// <returns>Modify filter result</returns>
        public static FilterResult Modify(Alert modifiedAlert, string reason = "Alert modified", TimeSpan processingTime = default)
        {
            return new FilterResult
            {
                Decision = FilterDecision.Modify,
                Reason = reason,
                ModifiedAlert = modifiedAlert,
                ProcessingTimeTicks = processingTime.Ticks,
                Metadata = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a defer result for later processing.
        /// </summary>
        /// <param name="reason">Deferral reason</param>
        /// <param name="processingTime">Processing duration</param>
        /// <returns>Defer filter result</returns>
        public static FilterResult Defer(string reason, TimeSpan processingTime = default)
        {
            return new FilterResult
            {
                Decision = FilterDecision.Defer,
                Reason = reason,
                ProcessingTimeTicks = processingTime.Ticks,
                Metadata = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Filter decision enumeration.
    /// </summary>
    public enum FilterDecision : byte
    {
        /// <summary>
        /// Allow the alert to continue processing.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Suppress the alert (do not deliver).
        /// </summary>
        Suppress = 1,

        /// <summary>
        /// Modify the alert and continue processing.
        /// </summary>
        Modify = 2,

        /// <summary>
        /// Defer processing for later evaluation.
        /// </summary>
        Defer = 3
    }

    /// <summary>
    /// Context information provided to filters for evaluation.
    /// </summary>
    public readonly record struct FilterContext
    {
        /// <summary>
        /// Gets the correlation ID for this filtering operation.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the timestamp when filtering began.
        /// </summary>
        public DateTime FilteringStartTime { get; init; }

        /// <summary>
        /// Gets additional context properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; init; }

        /// <summary>
        /// Gets the number of previous filter evaluations for this alert.
        /// </summary>
        public int FilterChainPosition { get; init; }

        /// <summary>
        /// Gets recent alerts for pattern analysis.
        /// </summary>
        public IReadOnlyList<Alert> RecentAlerts { get; init; }

        /// <summary>
        /// Creates an empty filter context.
        /// </summary>
        /// <returns>Empty context</returns>
        public static FilterContext Empty => new FilterContext
        {
            CorrelationId = Guid.NewGuid(),
            FilteringStartTime = DateTime.UtcNow,
            Properties = new Dictionary<string, object>(),
            RecentAlerts = Array.Empty<Alert>()
        };

        /// <summary>
        /// Creates a context with correlation ID.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Context with correlation</returns>
        public static FilterContext WithCorrelation(Guid correlationId)
        {
            return new FilterContext
            {
                CorrelationId = correlationId,
                FilteringStartTime = DateTime.UtcNow,
                Properties = new Dictionary<string, object>(),
                RecentAlerts = Array.Empty<Alert>()
            };
        }
    }

    /// <summary>
    /// Statistics for filter performance and behavior.
    /// </summary>
    public readonly record struct FilterStatistics
    {
        /// <summary>
        /// Gets the total number of alerts evaluated by this filter.
        /// </summary>
        public long TotalEvaluations { get; init; }

        /// <summary>
        /// Gets the number of alerts allowed through.
        /// </summary>
        public long AllowedCount { get; init; }

        /// <summary>
        /// Gets the number of alerts suppressed.
        /// </summary>
        public long SuppressedCount { get; init; }

        /// <summary>
        /// Gets the number of alerts modified.
        /// </summary>
        public long ModifiedCount { get; init; }

        /// <summary>
        /// Gets the number of alerts deferred.
        /// </summary>
        public long DeferredCount { get; init; }

        /// <summary>
        /// Gets the average evaluation time in milliseconds.
        /// </summary>
        public double AverageEvaluationTimeMs { get; init; }

        /// <summary>
        /// Gets the maximum evaluation time recorded in milliseconds.
        /// </summary>
        public double MaxEvaluationTimeMs { get; init; }

        /// <summary>
        /// Gets the timestamp when statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        /// Gets the suppression rate as a percentage (0-100).
        /// </summary>
        public double SuppressionRate => TotalEvaluations > 0 
            ? (double)SuppressedCount / TotalEvaluations * 100 
            : 0;

        /// <summary>
        /// Gets the modification rate as a percentage (0-100).
        /// </summary>
        public double ModificationRate => TotalEvaluations > 0 
            ? (double)ModifiedCount / TotalEvaluations * 100 
            : 0;

        /// <summary>
        /// Creates empty statistics.
        /// </summary>
        /// <returns>Empty statistics instance</returns>
        public static FilterStatistics Empty => new FilterStatistics
        {
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Result of filter configuration validation.
    /// </summary>
    public readonly record struct FilterValidationResult
    {
        /// <summary>
        /// Gets whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets validation error messages.
        /// </summary>
        public IReadOnlyList<string> Errors { get; init; }

        /// <summary>
        /// Gets validation warning messages.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; }

        /// <summary>
        /// Creates a valid result.
        /// </summary>
        /// <returns>Valid configuration result</returns>
        public static FilterValidationResult Valid()
        {
            return new FilterValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };
        }

        /// <summary>
        /// Creates an invalid result with errors.
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <param name="warnings">Optional warnings</param>
        /// <returns>Invalid configuration result</returns>
        public static FilterValidationResult Invalid(IReadOnlyList<string> errors, IReadOnlyList<string> warnings = null)
        {
            return new FilterValidationResult
            {
                IsValid = false,
                Errors = errors ?? Array.Empty<string>(),
                Warnings = warnings ?? Array.Empty<string>()
            };
        }
    }

    /// <summary>
    /// Diagnostic information about filter state and performance.
    /// </summary>
    public readonly record struct FilterDiagnostics
    {
        /// <summary>
        /// Gets the filter name.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets whether the filter is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the filter priority.
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// Gets the current configuration summary.
        /// </summary>
        public Dictionary<string, object> Configuration { get; init; }

        /// <summary>
        /// Gets the current statistics.
        /// </summary>
        public FilterStatistics Statistics { get; init; }

        /// <summary>
        /// Gets the last evaluation timestamp.
        /// </summary>
        public DateTime? LastEvaluation { get; init; }

        /// <summary>
        /// Gets recent evaluation performance metrics.
        /// </summary>
        public IReadOnlyList<double> RecentEvaluationTimes { get; init; }
    }

    /// <summary>
    /// Event arguments for filter configuration changes.
    /// </summary>
    public sealed class FilterConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the filter name.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the previous configuration.
        /// </summary>
        public Dictionary<string, object> PreviousConfiguration { get; init; }

        /// <summary>
        /// Gets the new configuration.
        /// </summary>
        public Dictionary<string, object> NewConfiguration { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }
    }

    /// <summary>
    /// Event arguments for filter statistics updates.
    /// </summary>
    public sealed class FilterStatisticsUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the filter name.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the updated statistics.
        /// </summary>
        public FilterStatistics Statistics { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }
    }
}