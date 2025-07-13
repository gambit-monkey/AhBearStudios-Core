using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Feature flag integration configuration
/// </summary>
public sealed record FeatureFlagIntegrationConfig
{
    /// <summary>
    /// Whether feature flag integration is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Feature flags to control for each degradation level
    /// </summary>
    public Dictionary<DegradationLevel, HashSet<string>> FeatureFlags { get; init; } = new();

    /// <summary>
    /// Feature flag service endpoint
    /// </summary>
    public string ServiceEndpoint { get; init; } = string.Empty;

    /// <summary>
    /// Validates feature flag integration configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Enabled && string.IsNullOrWhiteSpace(ServiceEndpoint))
            errors.Add("ServiceEndpoint is required when feature flag integration is enabled");

        return errors;
    }
}