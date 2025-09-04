namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Simple execution type for health checks
/// </summary>
public enum ExecutionType
{
    /// <summary>
    /// Scheduled execution at regular intervals
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// Manual execution triggered by user or system
    /// </summary>
    Manual,
    
    /// <summary>
    /// Emergency execution triggered by system alerts
    /// </summary>
    Emergency
}