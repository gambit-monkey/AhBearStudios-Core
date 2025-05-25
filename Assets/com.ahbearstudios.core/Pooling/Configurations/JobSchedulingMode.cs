namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Defines the scheduling mode for job operations.
    /// </summary>
    public enum JobSchedulingMode
    {
        /// <summary>
        /// Jobs are executed sequentially.
        /// Best for debugging and when operations must be performed in a specific order.
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Jobs are executed in parallel.
        /// Best for maximizing CPU utilization.
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Jobs are executed in batch groups.
        /// Balances parallelism and memory locality.
        /// </summary>
        Batched,
        
        /// <summary>
        /// Jobs are executed on worker threads.
        /// Useful for long-running operations that shouldn't block the main thread.
        /// </summary>
        Worker
    }
}