using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Notification configuration for report events
/// </summary>
public sealed record NotificationConfig
{
    /// <summary>
    /// Whether notifications are enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Notification channels configuration
    /// </summary>
    public List<NotificationChannel> Channels { get; init; } = new();

    /// <summary>
    /// Events that trigger notifications
    /// </summary>
    public HashSet<NotificationEvent> NotificationEvents { get; init; } = new();

    /// <summary>
    /// Validates notification configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        foreach (var channel in Channels)
        {
            errors.AddRange(channel.Validate());
        }

        foreach (var notificationEvent in NotificationEvents)
        {
            if (!Enum.IsDefined(typeof(NotificationEvent), notificationEvent))
                errors.Add($"Invalid notification event: {notificationEvent}");
        }

        return errors;
    }
}