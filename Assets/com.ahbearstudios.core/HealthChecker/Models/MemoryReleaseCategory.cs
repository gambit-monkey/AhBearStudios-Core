namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the category of memory released during the clear operation.
/// </summary>
public enum MemoryReleaseCategory : byte
{
    /// <summary>
    /// No memory was released.
    /// </summary>
    None = 0,

    /// <summary>
    /// Less than 1KB was released - minimal.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// 1KB to 1MB was released - small.
    /// </summary>
    Small = 2,

    /// <summary>
    /// 1MB to 10MB was released - moderate.
    /// </summary>
    Moderate = 3,

    /// <summary>
    /// 10MB to 100MB was released - large.
    /// </summary>
    Large = 4,

    /// <summary>
    /// Over 100MB was released - massive.
    /// </summary>
    Massive = 5
}