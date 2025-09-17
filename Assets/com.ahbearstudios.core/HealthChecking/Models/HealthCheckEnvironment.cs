namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Enumeration of health check environments
    /// </summary>
    public enum HealthCheckEnvironment
    {
        /// <summary>
        /// Development environment
        /// </summary>
        Development,

        /// <summary>
        /// Testing environment
        /// </summary>
        Testing,

        /// <summary>
        /// Staging environment
        /// </summary>
        Staging,

        /// <summary>
        /// Production environment
        /// </summary>
        Production
    }
}