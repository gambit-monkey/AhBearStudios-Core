using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Monitoring configuration for degradation events
/// </summary>
public sealed record DegradationMonitoringConfig
{
    /// <summary>
    /// Whether to enable detailed monitoring
    /// </summary>
    public bool EnableDetailedMonitoring { get; init; } = true;

    /// <summary>
    /// Whether to enable alerting for degradation events
    /// </summary>
    public bool EnableAlerting { get; init; } = true;

    /// <summary>
    /// Alert severities for each degradation level
    /// </summary>
    public Dictionary<DegradationLevel, AlertSeverity> AlertSeverities { get; init; } = new()
    {
        [DegradationLevel.Minor] = AlertSeverity.Warning,
        [DegradationLevel.Moderate] = AlertSeverity.Warning,
        [DegradationLevel.Severe] = AlertSeverity.Critical,
        [DegradationLevel.Disabled] = AlertSeverity.Critical
    };

    /// <summary>
    /// Whether to enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; init; } = true;

    /// <summary>
    /// Metrics collection interval
    /// </summary>
    public TimeSpan MetricsInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates degradation monitoring configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MetricsInterval <= TimeSpan.Zero)
            errors.Add("MetricsInterval must be greater than zero");

        foreach (var severity in AlertSeverities.Values)
        {
            if (!Enum.IsDefined(typeof(AlertSeverity), severity))
                errors.Add($"Invalid alert severity: {severity}");
        }

        return errors;
    }
}