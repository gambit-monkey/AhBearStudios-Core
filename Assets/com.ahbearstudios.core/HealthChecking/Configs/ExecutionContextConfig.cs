using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Execution context configuration for health checks
/// </summary>
public sealed record ExecutionContextConfig
{
    /// <summary>
    /// Thread priority for health check execution
    /// </summary>
    public ThreadPriority ThreadPriority { get; init; } = ThreadPriority.Normal;

    /// <summary>
    /// Whether to capture execution context
    /// </summary>
    public bool CaptureExecutionContext { get; init; } = true;

    /// <summary>
    /// Whether to suppress execution context flow
    /// </summary>
    public bool SuppressExecutionContextFlow { get; init; } = false;

    /// <summary>
    /// Custom execution context data
    /// </summary>
    public Dictionary<string, object> ContextData { get; init; } = new();

    /// <summary>
    /// Validates execution context configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(ThreadPriority), ThreadPriority))
            errors.Add($"Invalid thread priority: {ThreadPriority}");

        return errors;
    }
}