using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

public sealed record FilteringConfig
{
    public bool Enabled { get; init; } = true;
    public Dictionary<string, object> DefaultFilters { get; init; } = new();
    public List<string> AvailableFilters { get; init; } = new();

    public List<string> Validate() => new();
}