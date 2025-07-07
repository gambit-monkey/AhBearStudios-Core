namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines the direction of a performance trend.
/// </summary>
public enum TrendDirection : byte
{
    /// <summary>
    /// Performance is declining compared to historical averages.
    /// </summary>
    Declining = 0,

    /// <summary>
    /// Performance is stable with no significant change.
    /// </summary>
    Stable = 1,

    /// <summary>
    /// Performance is improving compared to historical averages.
    /// </summary>
    Improving = 2
}