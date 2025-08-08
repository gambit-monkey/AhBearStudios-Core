using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for log-based alert channels.
/// Provides configuration options for logging system integration.
/// </summary>
public sealed record LogChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets the log level to use when writing alerts to the logging system.
    /// This maps alert severities to appropriate log levels.
    /// </summary>
    public LogLevel DefaultLogLevel { get; init; } = LogLevel.Warning;

    /// <summary>
    /// Gets whether alerts should include structured data when logging.
    /// When enabled, alert properties are logged as structured fields.
    /// </summary>
    public bool UseStructuredLogging { get; init; } = true;

    /// <summary>
    /// Gets whether correlation IDs should be included in log entries.
    /// </summary>
    public bool IncludeCorrelationId { get; init; } = true;

    /// <summary>
    /// Gets whether alert context information should be included in logs.
    /// </summary>
    public bool IncludeContext { get; init; } = true;

    /// <summary>
    /// Gets whether performance metrics should be included in log entries.
    /// </summary>
    public bool IncludeMetrics { get; init; } = false;

    /// <summary>
    /// Gets the log category to use for alert entries.
    /// This helps with filtering and routing within the logging system.
    /// </summary>
    public string LogCategory { get; init; } = "Alerts";

    /// <summary>
    /// Gets whether sensitive information should be sanitized before logging.
    /// When enabled, potentially sensitive data is masked or removed.
    /// </summary>
    public bool SanitizeSensitiveData { get; init; } = true;

    /// <summary>
    /// Gets the maximum length for logged messages before truncation.
    /// Set to 0 for no limit.
    /// </summary>
    public int MaxMessageLength { get; init; } = 4000;

    /// <summary>
    /// Gets whether stack traces should be included for error-level alerts.
    /// </summary>
    public bool IncludeStackTrace { get; init; } = true;

    /// <summary>
    /// Gets the default log channel settings.
    /// </summary>
    public static LogChannelSettings Default => new();

    /// <summary>
    /// Validates the log channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return MaxMessageLength >= 0 &&
               !string.IsNullOrWhiteSpace(LogCategory);
    }
}