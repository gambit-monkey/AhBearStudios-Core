namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Enum defining compression levels
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>
        /// No compression
        /// </summary>
        NoCompression = 0,
    
        /// <summary>
        /// Fastest compression (less compression, faster processing)
        /// </summary>
        Fastest = 1,
    
        /// <summary>
        /// Optimal balance between compression ratio and speed
        /// </summary>
        Optimal = 2,
    
        /// <summary>
        /// Best compression (more compression, slower processing)
        /// </summary>
        Best = 3
    }
}