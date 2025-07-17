using Unity.Collections;
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

    /// <summary>
    /// Raises an alert with the specified details using Unity.Collections FixedString types.
    /// Designed for Unity Job System and Burst compatibility.
    /// </summary>
    /// <param name="message">The alert message</param>
    /// <param name="severity">The alert severity</param>
    /// <param name="source">The alert source</param>
    /// <param name="tag">The alert tag</param>
    void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag);
}