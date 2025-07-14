namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Actions to take when execution is attempted outside allowed windows
/// </summary>
public enum OutOfWindowAction
{
    Skip,
    Queue,
    Execute
}