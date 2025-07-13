using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Additional configuration classes for comprehensive reporting
/// </summary>
public sealed record AggregationConfig
{
    public bool Enabled { get; init; } = true;
    public TimeSpan AggregationInterval { get; init; } = TimeSpan.FromMinutes(5);
    public List<string> AggregationFunctions { get; init; } = new() { "avg", "min", "max", "count" };

    public List<string> Validate() => new();
}