using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Notification channel configuration
/// </summary>
public sealed record NotificationChannel
{
    /// <summary>
    /// Channel name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Channel type
    /// </summary>
    public NotificationChannelType Type { get; init; } = NotificationChannelType.Email;

    /// <summary>
    /// Channel configuration
    /// </summary>
    public Dictionary<string, object> Config { get; init; } = new();

    /// <summary>
    /// Whether channel is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Validates notification channel
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Notification channel Name cannot be null or empty");

        if (!Enum.IsDefined(typeof(NotificationChannelType), Type))
            errors.Add($"Invalid notification channel type: {Type}");

        return errors;
    }
}