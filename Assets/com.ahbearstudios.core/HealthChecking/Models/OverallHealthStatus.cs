namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Represents the overall health status of the entire system.
    /// Aggregates individual health check statuses into a comprehensive view.
    /// </summary>
    public enum OverallHealthStatus
    {
        /// <summary>
        /// The overall status is unknown or not yet determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// All components are healthy and functioning normally.
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// Some components have warnings but system is operational.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Some components are degraded but system is still functional.
        /// </summary>
        Degraded = 3,

        /// <summary>
        /// Critical components are unhealthy, system functionality is impaired.
        /// </summary>
        Unhealthy = 4,

        /// <summary>
        /// System is in a critical state requiring immediate attention.
        /// </summary>
        Critical = 5,

        /// <summary>
        /// System is offline or completely unavailable.
        /// </summary>
        Offline = 6
    }
}