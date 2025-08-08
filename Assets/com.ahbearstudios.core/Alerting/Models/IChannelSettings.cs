namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Marker interface for all channel-specific settings.
/// Provides type safety for channel configuration settings.
/// </summary>
public interface IChannelSettings
{
    /// <summary>
    /// Validates the settings configuration.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    bool IsValid();
}