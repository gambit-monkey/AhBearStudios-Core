namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Execution type enumeration
/// </summary>
public enum ExecutionType
{
    Scheduled,
    Manual,
    Recovery,
    Adaptive
}