namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Execution history entry
/// </summary>
internal sealed class ExecutionHistoryEntry
{
    public DateTime ExecutionTime { get; set; }
    public TimeSpan Duration { get; set; }
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}