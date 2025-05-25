namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Defines how the value type pool handles overflow situations.
    /// </summary>
    public enum OverflowHandlingType
    {
        /// <summary>
        /// Throws an exception when the pool capacity is exceeded.
        /// </summary>
        ThrowException,
        
        /// <summary>
        /// Returns a default value when the pool capacity is exceeded.
        /// </summary>
        ReturnDefault,
        
        /// <summary>
        /// Attempts to grow the pool beyond its configured maximum capacity.
        /// </summary>
        GrowBeyondMax,
        
        /// <summary>
        /// Blocks the requesting thread until capacity becomes available.
        /// </summary>
        Block,
        
        /// <summary>
        /// Reuses the least recently used item from the pool.
        /// </summary>
        ReuseLRU,
    
        /// <summary>
        /// Returns null or default when capacity is exceeded
        /// </summary>
        ReturnNull,
    
        /// <summary>
        /// Grows the pool automatically when capacity is exceeded
        /// </summary>
        AutoGrow,
    
        /// <summary>
        /// Blocks until an item becomes available
        /// </summary>
        WaitForAvailable
    }
    
    /// <summary>
    /// Defines the structural layout type for memory organization.
    /// </summary>
    public enum StructLayoutType
    {
        /// <summary>
        /// Members are laid out sequentially in memory.
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Members are explicitly positioned at specific offsets.
        /// </summary>
        Explicit,
        
        /// <summary>
        /// Members are arranged for optimal access by the compiler.
        /// </summary>
        Auto,
        
        /// <summary>
        /// Members are aligned for SIMD vector operations.
        /// </summary>
        Vectorized
    }
}