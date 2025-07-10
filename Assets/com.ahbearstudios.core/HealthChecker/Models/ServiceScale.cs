namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the scale category of the health check service.
/// </summary>
public enum ServiceScale : byte
{
    /// <summary>
    /// No health checks registered.
    /// </summary>
    Empty = 0,

    /// <summary>
    /// 1-5 health checks registered - small scale.
    /// </summary>
    Small = 1,

    /// <summary>
    /// 6-20 health checks registered - medium scale.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 21-50 health checks registered - large scale.
    /// </summary>
    Large = 3,

    /// <summary>
    /// Over 50 health checks registered - enterprise scale.
    /// </summary>
    Enterprise = 4
}