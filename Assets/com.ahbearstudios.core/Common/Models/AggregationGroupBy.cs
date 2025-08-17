namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Defines the fields available for grouping alerts during aggregation.
/// Used across multiple systems for consistent aggregation behavior.
/// </summary>
public enum AggregationGroupBy : byte
{
    /// <summary>
    /// Group alerts by their source system or component.
    /// </summary>
    Source = 0,

    /// <summary>
    /// Group alerts by their tag or category.
    /// </summary>
    Tag = 1,

    /// <summary>
    /// Group alerts by their severity level.
    /// </summary>
    Severity = 2,

    /// <summary>
    /// Group alerts by both source and tag combination.
    /// </summary>
    SourceAndTag = 3,

    /// <summary>
    /// Group alerts by custom expression evaluation.
    /// </summary>
    Custom = 4
}