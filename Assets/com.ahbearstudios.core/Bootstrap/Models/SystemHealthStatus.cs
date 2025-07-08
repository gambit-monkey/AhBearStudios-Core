using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Health status for systems installed by a bootstrap installer.
/// Provides comprehensive operational status information for monitoring and diagnostics.
/// </summary>
public readonly struct SystemHealthStatus
{
    /// <summary>Gets the overall health status of the system.</summary>
    public readonly HealthStatus OverallStatus;
        
    /// <summary>Gets health status for individual services within the system.</summary>
    public readonly IReadOnlyDictionary<string, HealthStatus> ServiceHealthStatuses;
        
    /// <summary>Gets health check results with detailed diagnostic information.</summary>
    public readonly IReadOnlyList<HealthCheckResult> HealthCheckResults;
        
    /// <summary>Gets the timestamp when this health status was last updated.</summary>
    public readonly DateTime LastUpdated;
        
    /// <summary>Gets performance metrics for the system's health monitoring.</summary>
    public readonly HealthMetrics Metrics;
        
    /// <summary>
    /// Initializes a new system health status.
    /// </summary>
    public SystemHealthStatus(HealthStatus overallStatus, 
        IReadOnlyDictionary<string, HealthStatus> serviceStatuses,
        IReadOnlyList<HealthCheckResult> healthCheckResults,
        DateTime lastUpdated, HealthMetrics metrics)
    {
        OverallStatus = overallStatus;
        ServiceHealthStatuses = serviceStatuses;
        HealthCheckResults = healthCheckResults;
        LastUpdated = lastUpdated;
        Metrics = metrics;
    }
}