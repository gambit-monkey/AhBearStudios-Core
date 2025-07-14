namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Actions to take during blackout periods
/// </summary>
public enum BlackoutAction
{
    Skip,
    Queue,
    Execute
}