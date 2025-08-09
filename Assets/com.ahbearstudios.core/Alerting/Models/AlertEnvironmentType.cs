namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Environment types for alert service configuration.
    /// </summary>
    public enum AlertEnvironmentType
    {
        /// <summary>
        /// Development environment with verbose logging and minimal filtering.
        /// </summary>
        Development = 0,

        /// <summary>
        /// Testing environment with in-memory channels and controlled output.
        /// </summary>
        Testing = 1,

        /// <summary>
        /// Staging environment with production-like configuration but additional monitoring.
        /// </summary>
        Staging = 2,

        /// <summary>
        /// Production environment with optimized performance and minimal noise.
        /// </summary>
        Production = 3
    }
}