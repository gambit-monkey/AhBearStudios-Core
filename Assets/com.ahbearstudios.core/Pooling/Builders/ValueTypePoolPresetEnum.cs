namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Predefined presets for value type pool configuration
    /// </summary>
    public enum ValueTypePoolPreset
    {
        /// <summary>
        /// Default balanced configuration
        /// </summary>
        Default,

        /// <summary>
        /// Configuration optimized for maximum performance
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Configuration optimized for Burst compatibility
        /// </summary>
        BurstCompatible,

        /// <summary>
        /// Configuration optimized for memory efficiency
        /// </summary>
        MemoryEfficient
    }
}