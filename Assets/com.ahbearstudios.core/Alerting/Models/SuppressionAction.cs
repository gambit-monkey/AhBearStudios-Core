using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Defines actions to take when suppression criteria are met.
    /// </summary>
    public enum SuppressionAction
    {
        /// <summary>
        /// Completely suppress the alert - it will not be processed further.
        /// </summary>
        Suppress,

        /// <summary>
        /// Queue the alert for later processing when suppression window expires.
        /// </summary>
        Queue,

        /// <summary>
        /// Aggregate the alert with similar alerts into a summary.
        /// </summary>
        Aggregate,

        /// <summary>
        /// Escalate the alert to a higher priority channel despite suppression.
        /// </summary>
        Escalate,

        /// <summary>
        /// Modify the alert (typically reduce severity) but continue processing.
        /// </summary>
        Modify
    }
}