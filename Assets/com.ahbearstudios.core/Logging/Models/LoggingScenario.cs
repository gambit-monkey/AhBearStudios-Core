namespace AhBearStudios.Core.Logging.Models;

/// <summary>
/// Enumeration of common logging scenarios for preset configurations.
/// </summary>
public enum LoggingScenario
{
    /// <summary>
    /// Production environment with enterprise-grade logging.
    /// </summary>
    Production,

    /// <summary>
    /// Development environment with comprehensive debugging.
    /// </summary>
    Development,

    /// <summary>
    /// Testing environment with extensive log capture.
    /// </summary>
    Testing,

    /// <summary>
    /// Staging environment with production-like settings.
    /// </summary>
    Staging,

    /// <summary>
    /// High-availability production with redundant logging.
    /// </summary>
    HighAvailability,

    /// <summary>
    /// Cloud deployment with network-based logging.
    /// </summary>
    CloudDeployment,

    /// <summary>
    /// Mobile/embedded deployment with minimal overhead.
    /// </summary>
    Mobile,

    /// <summary>
    /// Performance testing with optimized configuration.
    /// </summary>
    PerformanceTesting
}