using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Custom cache policy configuration
/// </summary>
public sealed record CachePolicyConfig : IValidatable
{
    public string Name { get; init; } = string.Empty;
    public TimeSpan TimeToLive { get; init; }
    public CacheInvalidationStrategy InvalidationStrategy { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name cannot be empty");
            
        if (TimeToLive <= TimeSpan.Zero)
            errors.Add("TimeToLive must be greater than zero");
            
        return errors;
    }
}