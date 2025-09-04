namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Defines the health status levels for the logging system.
    /// </summary>
    public enum LoggingSystemHealthStatus : byte
    {
        /// <summary>
        /// System is fully operational with no issues.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// System is operational but with some performance degradation.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// System has significant issues but is still functional.
        /// </summary>
        Unhealthy = 2,

        /// <summary>
        /// System is in critical state and may not be functional.
        /// </summary>
        Critical = 3,

        /// <summary>
        /// System status is unknown or cannot be determined.
        /// </summary>
        Unknown = 4
    }
}