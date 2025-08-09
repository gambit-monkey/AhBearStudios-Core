using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Defines the types of suppression rules available in the system.
    /// Each type implements a different suppression algorithm and logic.
    /// </summary>
    public enum SuppressionType
    {
        /// <summary>
        /// Suppresses duplicate alerts based on content similarity.
        /// </summary>
        Duplicate,

        /// <summary>
        /// Limits the rate of alerts from specific sources or overall.
        /// </summary>
        RateLimit,

        /// <summary>
        /// Applies different suppression rules based on business hours.
        /// </summary>
        BusinessHours,

        /// <summary>
        /// Suppresses alerts based on threshold values or counts.
        /// </summary>
        Threshold,

        /// <summary>
        /// Combines multiple suppression types with complex logic.
        /// </summary>
        Composite,

        /// <summary>
        /// Custom suppression logic defined by filter expressions.
        /// </summary>
        Custom
    }
}