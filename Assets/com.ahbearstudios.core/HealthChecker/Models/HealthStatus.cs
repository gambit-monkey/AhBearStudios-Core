namespace AhBearStudios.Core.HealthCheck.Models
{
    /// <summary>
    /// Indicates the health state of a check.
    /// </summary>
    public enum HealthStatus : byte
    {
        Healthy = 0,
        Degraded = 1,
        Unhealthy = 2
    }
}