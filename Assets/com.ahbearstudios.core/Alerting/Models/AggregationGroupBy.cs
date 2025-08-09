using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Defines the fields available for grouping alerts during aggregation.
    /// </summary>
    public enum AggregationGroupBy
    {
        /// <summary>
        /// Group alerts by their source system or component.
        /// </summary>
        Source,

        /// <summary>
        /// Group alerts by their tag or category.
        /// </summary>
        Tag,

        /// <summary>
        /// Group alerts by their severity level.
        /// </summary>
        Severity,

        /// <summary>
        /// Group alerts by both source and tag combination.
        /// </summary>
        SourceAndTag,

        /// <summary>
        /// Group alerts by custom expression evaluation.
        /// </summary>
        Custom
    }
}