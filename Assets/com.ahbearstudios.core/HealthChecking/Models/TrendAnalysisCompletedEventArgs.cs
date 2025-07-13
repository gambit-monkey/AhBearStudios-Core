using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for trend analysis completion
/// </summary>
public sealed class TrendAnalysisCompletedEventArgs : EventArgs
{
    public List<HealthTrendAnalysis> Analyses { get; init; } = new();
    public DateTime Timestamp { get; init; }
}