using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

public sealed record TemplateConfig
{
    public bool Enabled { get; init; } = true;
    public string TemplateDirectory { get; init; } = "Templates";
    public Dictionary<string, string> CustomTemplates { get; init; } = new();

    public List<string> Validate() => new();
}