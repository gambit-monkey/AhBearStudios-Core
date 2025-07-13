using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Configuration for degradation impact assessment
/// </summary>
public sealed record DegradationImpactConfig
{
    /// <summary>
    /// Impact level when this health check is degraded
    /// </summary>
    public DegradationLevel DegradedImpact { get; init; } = DegradationLevel.Minor;

    /// <summary>
    /// Impact level when this health check is unhealthy
    /// </summary>
    public DegradationLevel UnhealthyImpact { get; init; } = DegradationLevel.Moderate;

    /// <summary>
    /// Features that should be disabled when this check is unhealthy
    /// </summary>
    public HashSet<FixedString64Bytes> DisabledFeatures { get; init; } = new();

    /// <summary>
    /// Services that should be degraded when this check is unhealthy
    /// </summary>
    public HashSet<FixedString64Bytes> DegradedServices { get; init; } = new();

    /// <summary>
    /// Validates degradation impact configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(DegradationLevel), DegradedImpact))
            errors.Add($"Invalid degraded impact level: {DegradedImpact}");

        if (!Enum.IsDefined(typeof(DegradationLevel), UnhealthyImpact))
            errors.Add($"Invalid unhealthy impact level: {UnhealthyImpact}");

        if (DegradedImpact >= UnhealthyImpact)
            errors.Add("Unhealthy impact should be more severe than degraded impact");

        return errors;
    }
}