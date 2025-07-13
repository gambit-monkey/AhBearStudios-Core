using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

public sealed record VisualizationConfig
{
    public bool Enabled { get; init; } = true;
    public List<string> ChartTypes { get; init; } = new() { "line", "bar", "pie", "gauge" };
    public Dictionary<string, object> ChartDefaults { get; init; } = new();

    public List<string> Validate() => new();
}