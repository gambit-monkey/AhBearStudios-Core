namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Preset configurations for the channel service.
    /// Provides common configuration templates for different scenarios.
    /// </summary>
    public enum ChannelServicePreset
    {
        /// <summary>
        /// Default configuration suitable for most scenarios.
        /// </summary>
        Default,

        /// <summary>
        /// Optimized for high-performance, low-latency scenarios.
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Debug configuration with verbose monitoring.
        /// </summary>
        Debug,

        /// <summary>
        /// Production-ready configuration with balanced settings.
        /// </summary>
        Production
    }
}