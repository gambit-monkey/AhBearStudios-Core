namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Enumeration of available pool implementation types.
    /// Used to specify which pool implementation should be used for specific object types.
    /// </summary>
    public enum PoolType
    {
        /// <summary>
        /// Generic object pool suitable for most object types.
        /// Default choice with balanced performance and flexibility.
        /// </summary>
        Generic = 0,
        
        /// <summary>
        /// Specialized pool for small network buffers (up to 1KB).
        /// Optimized for high-frequency small packet operations.
        /// </summary>
        SmallBuffer = 1,
        
        /// <summary>
        /// Specialized pool for medium network buffers (1KB to 64KB).
        /// Balanced for typical network message sizes.
        /// </summary>
        MediumBuffer = 2,
        
        /// <summary>
        /// Specialized pool for large network buffers (64KB+).
        /// Optimized for bulk data transfer and file operations.
        /// </summary>
        LargeBuffer = 3,
        
        /// <summary>
        /// Specialized pool for compression operations.
        /// Designed for temporary compression/decompression buffers.
        /// </summary>
        CompressionBuffer = 4,
        
        /// <summary>
        /// Specialized pool for managed log data objects.
        /// Optimized for structured logging data with lifecycle management.
        /// </summary>
        ManagedLogData = 5
    }
}