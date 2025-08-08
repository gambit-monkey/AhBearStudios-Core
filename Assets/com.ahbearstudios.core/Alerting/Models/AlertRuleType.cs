namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Type of alert rule defining its behavior.
    /// </summary>
    public enum AlertRuleType : byte
    {
        /// <summary>
        /// Rule that filters alerts based on conditions.
        /// </summary>
        Filter = 0,

        /// <summary>
        /// Rule that suppresses matching alerts.
        /// </summary>
        Suppression = 1,

        /// <summary>
        /// Rule that limits alert rate from sources.
        /// </summary>
        RateLimit = 2,

        /// <summary>
        /// Rule that triggers based on threshold values.
        /// </summary>
        Threshold = 3,

        /// <summary>
        /// Rule that modifies alert properties.
        /// </summary>
        Transformation = 4,

        /// <summary>
        /// Rule that routes alerts to specific channels.
        /// </summary>
        Routing = 5
    }
}