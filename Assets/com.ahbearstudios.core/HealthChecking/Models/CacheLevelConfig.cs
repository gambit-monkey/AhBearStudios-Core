using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Cache level configuration for multi-level caching
/// </summary>
public sealed record CacheLevelConfig : IValidatable
{
    public int Level { get; init; }
    public CacheStorageStrategy StorageStrategy { get; init; }
    public TimeSpan TimeToLive { get; init; }
    public int MaxSize { get; init; }
    public CacheEvictionPolicy EvictionPolicy { get; init; }

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (Level < 1)
            errors.Add("Level must be at least 1");
            
        if (TimeToLive <= TimeSpan.Zero)
            errors.Add("TimeToLive must be greater than zero");
            
        if (MaxSize < 1)
            errors.Add("MaxSize must be at least 1");
            
        return errors;
    }
}