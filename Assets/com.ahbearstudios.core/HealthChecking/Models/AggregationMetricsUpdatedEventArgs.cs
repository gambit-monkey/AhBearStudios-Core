namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for aggregation metrics updates
/// </summary>
public sealed class AggregationMetricsUpdatedEventArgs : EventArgs
{
    public AggregationMetrics Metrics { get; init; }
}