using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for overall degradation changes
/// </summary>
public sealed class OverallDegradationChangedEventArgs : EventArgs
{
    public DegradationLevel OldLevel { get; init; }
    public DegradationLevel NewLevel { get; init; }
    public DateTime Timestamp { get; init; }
    public List<string> AffectedSystems { get; init; } = new();
}