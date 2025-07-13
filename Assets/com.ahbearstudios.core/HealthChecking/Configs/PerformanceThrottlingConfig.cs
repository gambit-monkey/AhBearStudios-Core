using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Performance throttling configuration
/// </summary>
public sealed record PerformanceThrottlingConfig
{
    /// <summary>
    /// Whether performance throttling is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// CPU usage limits for each degradation level
    /// </summary>
    public Dictionary<DegradationLevel, double> CpuLimits { get; init; } = new();

    /// <summary>
    /// Memory usage limits for each degradation level
    /// </summary>
    public Dictionary<DegradationLevel, long> MemoryLimits { get; init; } = new();

    /// <summary>
    /// Request rate limits for each degradation level
    /// </summary>
    public Dictionary<DegradationLevel, double> RequestRateLimits { get; init; } = new();

    /// <summary>
    /// Creates performance throttling configuration for high availability systems
    /// </summary>
    /// <returns>High availability performance throttling configuration</returns>
    public static PerformanceThrottlingConfig ForHighAvailability()
    {
        return new PerformanceThrottlingConfig
        {
            Enabled = true,
            CpuLimits = new Dictionary<DegradationLevel, double>
            {
                [DegradationLevel.Minor] = 0.8,
                [DegradationLevel.Moderate] = 0.6,
                [DegradationLevel.Severe] = 0.4,
                [DegradationLevel.Disabled] = 0.1
            },
            RequestRateLimits = new Dictionary<DegradationLevel, double>
            {
                [DegradationLevel.Minor] = 1000,
                [DegradationLevel.Moderate] = 500,
                [DegradationLevel.Severe] = 200,
                [DegradationLevel.Disabled] = 50
            }
        };
    }

    /// <summary>
    /// Validates performance throttling configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        foreach (var limit in CpuLimits.Values)
        {
            if (limit < 0.0 || limit > 1.0)
                errors.Add("CPU limits must be between 0.0 and 1.0");
        }

        foreach (var limit in MemoryLimits.Values)
        {
            if (limit < 0)
                errors.Add("Memory limits must be non-negative");
        }

        foreach (var limit in RequestRateLimits.Values)
        {
            if (limit < 0)
                errors.Add("Request rate limits must be non-negative");
        }

        return errors;
    }
}