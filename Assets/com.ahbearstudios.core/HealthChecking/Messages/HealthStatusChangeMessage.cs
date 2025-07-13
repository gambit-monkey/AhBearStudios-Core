using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message bus message for health status changes
/// </summary>
public sealed record HealthStatusChangeMessage
{
    /// <summary>
    /// Gets the source of the health status change.
    /// </summary>
    /// <remarks>
    /// This property represents the origin or context in which the health status
    /// change occurred. It provides information about the component, service,
    /// or process that triggered the change, helping to identify the source of
    /// the health update.
    /// </remarks>
    public string Source { get; init; }

    /// <summary>
    /// Represents the previous health status before a change occurred.
    /// </summary>
    /// <remarks>
    /// The <c>OldStatus</c> property holds the health status value prior to the current update.
    /// Possible values are defined in the <see cref="HealthStatus"/> enum.
    /// </remarks>
    public HealthStatus OldStatus { get; init; }

    /// <summary>
    /// Gets or initializes the new health status after a change.
    /// </summary>
    /// <remarks>
    /// This property represents the updated <see cref="HealthStatus"/> reflecting
    /// the current state after a health status change event.
    /// </remarks>
    public HealthStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the timestamp indicating when the health status change occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the scores for various health check categories related to the health status change.
    /// The dictionary uses <see cref="HealthCheckCategory"/> as keys and scores as double values.
    /// </summary>
    public Dictionary<HealthCheckCategory, double> CategoryScores { get; init; } = new();
}