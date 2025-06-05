namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Enumeration of available logging presets for different scenarios.
    /// </summary>
    public enum LoggingPreset
    {
        /// <summary>
        /// Development preset with verbose logging and debugging features.
        /// </summary>
        Development,
        
        /// <summary>
        /// Production preset with optimized performance and minimal logging.
        /// </summary>
        Production,
        
        /// <summary>
        /// High-performance preset with maximum optimization and minimal overhead.
        /// </summary>
        HighPerformance,
        
        /// <summary>
        /// Custom preset using manually configured target configurations.
        /// </summary>
        Custom
    }
}