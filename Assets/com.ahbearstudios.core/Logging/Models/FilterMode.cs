namespace AhBearStudios.Core.Logging.Models;

/// <summary>
/// Defines the mode for filter operations.
/// </summary>
public enum FilterMode
{
    /// <summary>
    /// Include messages that match the filter criteria.
    /// </summary>
    Include,

    /// <summary>
    /// Exclude messages that match the filter criteria.
    /// </summary>
    Exclude
}