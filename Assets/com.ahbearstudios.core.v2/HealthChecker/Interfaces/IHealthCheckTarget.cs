namespace AhBearStudios.Core.HealthCheck.Interfaces
{
    /// <summary>
    /// Receives health check results.
    /// </summary>
    public interface IHealthCheckTarget
    {
        void Handle(in HealthCheckResult result);
    }
}