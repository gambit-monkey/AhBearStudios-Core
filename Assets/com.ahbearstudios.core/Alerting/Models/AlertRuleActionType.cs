namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Types of actions that can be applied by rules.
    /// </summary>
    public enum AlertRuleActionType : byte
    {
        /// <summary>
        /// Suppress the alert completely.
        /// </summary>
        Suppress = 0,

        /// <summary>
        /// Modify the alert severity.
        /// </summary>
        ModifySeverity = 1,

        /// <summary>
        /// Add or modify the alert tag.
        /// </summary>
        AddTag = 2,

        /// <summary>
        /// Route alert to specific channels.
        /// </summary>
        Route = 3,

        /// <summary>
        /// Add metadata to the alert.
        /// </summary>
        AddMetadata = 4
    }
}