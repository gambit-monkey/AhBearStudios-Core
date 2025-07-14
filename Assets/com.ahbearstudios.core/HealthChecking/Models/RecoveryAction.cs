namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Recovery actions when schedule is broken
/// </summary>
public enum RecoveryAction
{
    ImmediateExecution,
    NextScheduledTime,
    Reset
}