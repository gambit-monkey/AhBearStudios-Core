namespace AhBearStudios.Pooling.Pools.Async
{
    /// <summary>
    /// Predefined usage patterns for asynchronous object pools
    /// </summary>
    public enum AsyncPoolUsagePattern
    {
        /// <summary>
        /// Default balanced configuration
        /// </summary>
        Default,
        
        /// <summary>
        /// Configuration optimized for handling resource-intensive operations
        /// </summary>
        ResourceIntensive,
        
        /// <summary>
        /// Configuration optimized for high-throughput scenarios
        /// </summary>
        HighThroughput,
        
        /// <summary>
        /// Configuration optimized for low-latency scenarios
        /// </summary>
        LowLatency,
        
        /// <summary>
        /// Configuration optimized for background processing
        /// </summary>
        BackgroundProcessing,
        
        /// <summary>
        /// Configuration optimized for asset loading operations
        /// </summary>
        AssetLoading,
        
        /// <summary>
        /// Configuration optimized for network operations
        /// </summary>
        NetworkOperations,
        
        /// <summary>
        /// Configuration optimized for computational tasks
        /// </summary>
        ComputationalTasks,
        
        /// <summary>
        /// Configuration optimized for UI responsiveness
        /// </summary>
        UIResponsiveness,
        
        /// <summary>
        /// Configuration optimized for streaming data
        /// </summary>
        DataStreaming
    }
}