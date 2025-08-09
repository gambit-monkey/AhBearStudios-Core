namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Enumeration of supported filter types.
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Filter based on alert severity level.
        /// </summary>
        Severity = 0,

        /// <summary>
        /// Filter based on alert source.
        /// </summary>
        Source = 1,

        /// <summary>
        /// Filter based on rate limiting.
        /// </summary>
        RateLimit = 2,

        /// <summary>
        /// Filter based on alert content/message.
        /// </summary>
        Content = 3,

        /// <summary>
        /// Filter based on time of day/week.
        /// </summary>
        TimeBased = 4,

        /// <summary>
        /// Filter that combines multiple child filters.
        /// </summary>
        Composite = 5,

        /// <summary>
        /// Filter based on alert tags.
        /// </summary>
        Tag = 6,

        /// <summary>
        /// Filter based on correlation IDs.
        /// </summary>
        Correlation = 7,

        /// <summary>
        /// Filter that allows all alerts (pass-through).
        /// </summary>
        PassThrough = 8,

        /// <summary>
        /// Filter that blocks all alerts.
        /// </summary>
        Block = 9
    }
}