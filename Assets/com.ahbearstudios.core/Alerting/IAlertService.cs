using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting;

/// <summary>
/// Placeholder interface for alert service integration.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Raises an alert with the specified details.
    /// </summary>
    /// <param name="message">The alert message</param>
    /// <param name="severity">The alert severity</param>
    /// <param name="source">The alert source</param>
    /// <param name="tag">The alert tag</param>
    void RaiseAlert(string message, AlertSeverity severity, string source, string tag);
}