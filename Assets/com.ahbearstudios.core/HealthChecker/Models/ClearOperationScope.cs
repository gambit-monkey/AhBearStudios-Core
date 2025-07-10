namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the scope of the clear operation.
/// </summary>
public enum ClearOperationScope : byte
{
    /// <summary>
    /// No components were cleared.
    /// </summary>
    None = 0,

    /// <summary>
    /// 1-5 components were cleared - minimal scope.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// 6-20 components were cleared - moderate scope.
    /// </summary>
    Moderate = 2,

    /// <summary>
    /// 21-50 components were cleared - extensive scope.
    /// </summary>
    Extensive = 3,

    /// <summary>
    /// Over 50 components were cleared - complete scope.
    /// </summary>
    Complete = 4
}