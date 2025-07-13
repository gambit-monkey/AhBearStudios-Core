using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Represents the result of a health check operation.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Gets the health status.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the description of the health check result.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets additional data associated with the health check.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets the exception associated with the health check, if any.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the HealthCheckResult.
    /// </summary>
    /// <param name="status">The health status</param>
    /// <param name="description">The description</param>
    /// <param name="data">Additional data</param>
    /// <param name="exception">Associated exception</param>
    private HealthCheckResult(
        HealthStatus status,
        string description,
        IReadOnlyDictionary<string, object> data = null,
        Exception exception = null)
    {
        Status = status;
        Description = description ?? string.Empty;
        Data = data ?? new Dictionary<string, object>();
        Exception = exception;
    }

    /// <summary>
    /// Creates a healthy health check result.
    /// </summary>
    /// <param name="description">The description</param>
    /// <param name="data">Additional data</param>
    /// <returns>A healthy health check result</returns>
    public static HealthCheckResult Healthy(string description = null, IReadOnlyDictionary<string, object> data = null)
    {
        return new HealthCheckResult(HealthStatus.Healthy, description, data);
    }

    /// <summary>
    /// Creates a degraded health check result.
    /// </summary>
    /// <param name="description">The description</param>
    /// <param name="data">Additional data</param>
    /// <returns>A degraded health check result</returns>
    public static HealthCheckResult Degraded(string description = null, IReadOnlyDictionary<string, object> data = null)
    {
        return new HealthCheckResult(HealthStatus.Degraded, description, data);
    }

    /// <summary>
    /// Creates an unhealthy health check result.
    /// </summary>
    /// <param name="description">The description</param>
    /// <param name="data">Additional data</param>
    /// <param name="exception">Associated exception</param>
    /// <returns>An unhealthy health check result</returns>
    public static HealthCheckResult Unhealthy(string description = null,
        IReadOnlyDictionary<string, object> data = null, Exception exception = null)
    {
        return new HealthCheckResult(HealthStatus.Unhealthy, description, data, exception);
    }

    /// <summary>
    /// Creates an unknown health check result.
    /// </summary>
    /// <param name="description">The description</param>
    /// <param name="data">Additional data</param>
    /// <returns>An unknown health check result</returns>
    public static HealthCheckResult Unknown = new HealthCheckResult(HealthStatus.Unknown, "Health status unknown");
}