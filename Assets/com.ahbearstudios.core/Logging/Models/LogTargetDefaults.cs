namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Default configuration values for log targets.
    /// Provides predefined settings optimized for different Unity game scenarios.
    /// </summary>
    public enum LogTargetDefaults
    {
        /// <summary>
        /// High-performance settings optimized for Unity games.
        /// Minimal overhead, frame budget aware, optimized for 60+ FPS.
        /// </summary>
        GameOptimized,

        /// <summary>
        /// Development-friendly settings with detailed logging.
        /// Comprehensive logging, profiling enabled, verbose output.
        /// </summary>
        Development,

        /// <summary>
        /// Production settings with minimal overhead.
        /// Error-focused logging, performance optimized, reduced verbosity.
        /// </summary>
        Production,

        /// <summary>
        /// Mobile-optimized settings for battery and performance.
        /// Minimal allocations, reduced I/O, power-efficient.
        /// </summary>
        Mobile,

        /// <summary>
        /// Console/PC optimized settings.
        /// Balanced performance and detail, suitable for PC/console platforms.
        /// </summary>
        Console,

        /// <summary>
        /// Minimal settings for testing scenarios.
        /// Lightweight, fast, suitable for unit and integration tests.
        /// </summary>
        Testing
    }
}