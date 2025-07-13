using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for category health updates
/// </summary>
public sealed class CategoryHealthUpdatedEventArgs : EventArgs
{
    public Dictionary<HealthCheckCategory, double> CategoryScores { get; init; } = new();
    public DateTime Timestamp { get; init; }
}