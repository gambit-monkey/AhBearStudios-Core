namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Environment types for system configuration.
/// Used across multiple systems for consistent environment-specific behavior.
/// </summary>
public enum EnvironmentType : byte
{
    /// <summary>
    /// Development environment with verbose logging and minimal filtering.
    /// </summary>
    Development = 0,

    /// <summary>
    /// Testing environment with controlled output and monitoring.
    /// </summary>
    Testing = 1,

    /// <summary>
    /// Staging environment with production-like configuration but additional monitoring.
    /// </summary>
    Staging = 2,

    /// <summary>
    /// Production environment with optimized performance and minimal noise.
    /// </summary>
    Production = 3
}