namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Result of an individual database test operation
/// </summary>
internal sealed class DatabaseTestResult
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public string Details { get; set; }
    public object Result { get; set; }
    public Exception Exception { get; set; }
}