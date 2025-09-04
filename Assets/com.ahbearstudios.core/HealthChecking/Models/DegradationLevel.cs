namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Defines the different levels of system degradation.
    /// Used for graceful degradation and feature management during health issues.
    /// </summary>
    public enum DegradationLevel
    {
        /// <summary>
        /// No degradation - all systems operating normally.
        /// Full functionality is available.
        /// </summary>
        None = 0,

        /// <summary>
        /// Minor degradation - some non-critical features may be disabled.
        /// Core functionality remains available.
        /// </summary>
        Minor = 1,

        /// <summary>
        /// Moderate degradation - significant features may be disabled.
        /// Essential functionality is prioritized.
        /// </summary>
        Moderate = 2,

        /// <summary>
        /// Severe degradation - only critical features are available.
        /// Most functionality is disabled to preserve system stability.
        /// </summary>
        Severe = 3,

        /// <summary>
        /// System disabled - emergency mode only.
        /// Only absolutely essential operations are permitted.
        /// </summary>
        Disabled = 4
    }
}