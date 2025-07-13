namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Degradation levels for graceful degradation
/// </summary>
public enum DegradationLevel
{
    /// <summary>
    /// No degradation - full functionality
    /// </summary>
    None,

    /// <summary>
    /// Minor degradation - some non-essential features disabled
    /// </summary>
    Minor,

    /// <summary>
    /// Moderate degradation - significant features disabled
    /// </summary>
    Moderate,

    /// <summary>
    /// Severe degradation - only essential features available
    /// </summary>
    Severe,

    /// <summary>
    /// System disabled - emergency mode only
    /// </summary>
    Disabled
}