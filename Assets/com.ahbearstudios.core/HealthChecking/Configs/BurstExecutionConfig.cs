using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Burst execution configuration
/// </summary>
public sealed record BurstExecutionConfig : IValidatable
{
    public int BurstSize { get; init; } = 3;
    public TimeSpan BurstInterval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan BurstCooldown { get; init; } = TimeSpan.FromMinutes(5);
    public bool EnableBurstOnDegradation { get; init; } = true;

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (BurstSize < 1)
            errors.Add("BurstSize must be at least 1");
            
        if (BurstInterval <= TimeSpan.Zero)
            errors.Add("BurstInterval must be greater than zero");
            
        if (BurstCooldown <= TimeSpan.Zero)
            errors.Add("BurstCooldown must be greater than zero");
            
        return errors;
    }
}