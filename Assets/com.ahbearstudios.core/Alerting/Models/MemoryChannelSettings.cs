using System;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for in-memory alert channels.
/// Provides configuration options for memory-based alert storage and retrieval.
/// </summary>
public sealed record MemoryChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets the maximum number of alerts to store in memory.
    /// When this limit is reached, oldest alerts are removed to make room for new ones.
    /// </summary>
    public int MaxStoredAlerts { get; init; } = 1000;

    /// <summary>
    /// Gets whether to use a circular buffer for alert storage.
    /// When enabled, old alerts are automatically overwritten when capacity is reached.
    /// </summary>
    public bool CircularBuffer { get; init; } = true;

    /// <summary>
    /// Gets whether to preserve the order of alerts as they were received.
    /// When disabled, alerts may be stored in a more efficient but unordered manner.
    /// </summary>
    public bool PreserveOrder { get; init; } = true;

    /// <summary>
    /// Gets the maximum age for stored alerts before they are automatically removed.
    /// Set to TimeSpan.Zero to disable age-based expiration.
    /// </summary>
    public TimeSpan MaxAlertAge { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets whether to enable automatic cleanup of expired alerts.
    /// </summary>
    public bool AutoCleanup { get; init; } = true;

    /// <summary>
    /// Gets the interval between automatic cleanup operations.
    /// Only applies when AutoCleanup is enabled.
    /// </summary>
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets whether to include performance metrics for memory operations.
    /// </summary>
    public bool IncludeMetrics { get; init; } = false;

    /// <summary>
    /// Gets whether to allow retrieval of stored alerts.
    /// When disabled, alerts are stored but cannot be retrieved (write-only mode).
    /// </summary>
    public bool AllowRetrieval { get; init; } = true;

    /// <summary>
    /// Gets whether duplicate alerts should be automatically detected and suppressed.
    /// </summary>
    public bool SuppressDuplicates { get; init; } = false;

    /// <summary>
    /// Gets the time window for duplicate detection.
    /// Only applies when SuppressDuplicates is enabled.
    /// </summary>
    public TimeSpan DuplicateWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the default memory channel settings.
    /// </summary>
    public static MemoryChannelSettings Default => new();

    /// <summary>
    /// Validates the memory channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return MaxStoredAlerts > 0 &&
               MaxAlertAge >= TimeSpan.Zero &&
               CleanupInterval > TimeSpan.Zero &&
               DuplicateWindow >= TimeSpan.Zero;
    }
}