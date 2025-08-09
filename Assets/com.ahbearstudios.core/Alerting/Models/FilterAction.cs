namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Action to take when a filter matches an alert.
    /// </summary>
    public enum FilterAction
    {
        /// <summary>
        /// Allow the alert to pass through.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Suppress the alert (block it).
        /// </summary>
        Suppress = 1,

        /// <summary>
        /// Modify the alert and pass it through.
        /// </summary>
        Modify = 2,

        /// <summary>
        /// Defer the alert for later processing.
        /// </summary>
        Defer = 3
    }
}