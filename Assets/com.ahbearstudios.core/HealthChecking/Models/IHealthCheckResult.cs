using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Interface for health check results
/// </summary>
public interface IHealthCheckResult
{
    /// <summary>
    /// Name of the health check
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Health status
    /// </summary>
    HealthStatus Status { get; }

    /// <summary>
    /// Result message
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Detailed description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Execution duration
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Execution timestamp
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Exception that occurred, if any
    /// </summary>
    Exception Exception { get; }

    /// <summary>
    /// Additional diagnostic data
    /// </summary>
    Dictionary<string, object> Data { get; }

    /// <summary>
    /// Whether the result indicates healthy status
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Whether the result indicates degraded status
    /// </summary>
    bool IsDegraded { get; }

    /// <summary>
    /// Whether the result indicates unhealthy status
    /// </summary>
    bool IsUnhealthy { get; }
}