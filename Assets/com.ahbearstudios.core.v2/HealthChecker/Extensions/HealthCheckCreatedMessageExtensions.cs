using AhBearStudios.Core.HealthCheck.Messages;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck.Extensions;

/// <summary>
/// Extension methods for HealthCheckCreatedMessage to provide additional functionality.
/// </summary>
public static class HealthCheckCreatedMessageExtensions
{
    /// <summary>
    /// Gets the performance category based on creation duration.
    /// </summary>
    /// <param name="message">The message to categorize.</param>
    /// <returns>The performance category.</returns>
    public static PerformanceCategory GetPerformanceCategory(this HealthCheckCreatedMessage message)
    {
        return message.CreationDurationMs switch
        {
            < 10.0 => PerformanceCategory.Excellent,
            < 50.0 => PerformanceCategory.Good,
            < 200.0 => PerformanceCategory.Acceptable,
            < 1000.0 => PerformanceCategory.Slow,
            _ => PerformanceCategory.VerySlow
        };
    }

    /// <summary>
    /// Checks if the message should trigger performance alerts.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <param name="thresholdMs">The performance threshold in milliseconds.</param>
    /// <returns>True if an alert should be triggered; otherwise, false.</returns>
    public static bool ShouldTriggerPerformanceAlert(this HealthCheckCreatedMessage message, double thresholdMs = 1000.0)
    {
        return message.CreationDurationMs > thresholdMs;
    }

    /// <summary>
    /// Gets a formatted duration string for display purposes.
    /// </summary>
    /// <param name="message">The message containing the duration.</param>
    /// <returns>A formatted duration string.</returns>
    public static string GetFormattedDuration(this HealthCheckCreatedMessage message)
    {
        var ms = message.CreationDurationMs;
        return ms switch
        {
            < 1.0 => $"{ms:F2}ms",
            < 1000.0 => $"{ms:F1}ms",
            < 60000.0 => $"{ms / 1000.0:F2}s",
            _ => $"{TimeSpan.FromMilliseconds(ms):mm\\:ss\\.fff}"
        };
    }

    /// <summary>
    /// Creates a metric key for aggregation and monitoring.
    /// </summary>
    /// <param name="message">The message to create a key for.</param>
    /// <returns>A metric key for aggregation.</returns>
    public static string GetMetricKey(this HealthCheckCreatedMessage message)
    {
        var environment = message.HasEnvironment ? message.Environment.ToString() : "unknown";
        return $"healthcheck.created.{message.HealthCheckType}.{environment}";
    }

    /// <summary>
    /// Checks if the message matches the specified filter criteria.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <param name="healthCheckTypeFilter">Optional health check type filter.</param>
    /// <param name="environmentFilter">Optional environment filter.</param>
    /// <param name="severityFilter">Optional minimum severity filter.</param>
    /// <param name="tagFilter">Optional tag filter (message must contain this tag).</param>
    /// <returns>True if the message matches all specified filters; otherwise, false.</returns>
    public static bool MatchesFilter(
        this HealthCheckCreatedMessage message,
        string healthCheckTypeFilter = null,
        string environmentFilter = null,
        MessageSeverity? severityFilter = null,
        string tagFilter = null)
    {
        // Check health check type filter
        if (!string.IsNullOrEmpty(healthCheckTypeFilter) &&
            !message.HealthCheckType.ToString().Equals(healthCheckTypeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check environment filter
        if (!string.IsNullOrEmpty(environmentFilter) &&
            !message.Environment.ToString().Equals(environmentFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check severity filter
        if (severityFilter.HasValue && message.Severity < severityFilter.Value)
        {
            return false;
        }

        // Check tag filter
        if (!string.IsNullOrEmpty(tagFilter) && !message.HasTag(tagFilter))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a summary object suitable for metrics collection and monitoring.
    /// </summary>
    /// <param name="message">The message to create a summary for.</param>
    /// <returns>A summary object containing key metrics.</returns>
    public static HealthCheckCreationSummary CreateSummary(this HealthCheckCreatedMessage message)
    {
        return new HealthCheckCreationSummary(
            HealthCheckType: message.HealthCheckType.ToString(),
            CreationDurationMs: message.CreationDurationMs,
            PerformanceCategory: message.GetPerformanceCategory(),
            Environment: message.HasEnvironment ? message.Environment.ToString() : "unknown",
            HasWarnings: message.Severity >= MessageSeverity.Warning,
            TagCount: message.GetTagsArray().Length,
            Timestamp: message.Timestamp);
    }
}