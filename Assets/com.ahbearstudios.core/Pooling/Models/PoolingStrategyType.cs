namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Enumeration of available pooling strategy types.
    /// Used to specify which strategy should be used for pool management.
    /// </summary>
    public enum PoolingStrategyType
    {
        /// <summary>
        /// Default general-purpose pooling strategy with balanced performance.
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Fixed-size pooling strategy that maintains a constant pool size.
        /// Best for predictable memory usage and consistent performance.
        /// </summary>
        FixedSize = 1,
        
        /// <summary>
        /// Dynamic pooling strategy that adjusts pool size based on demand.
        /// Good for variable workloads and memory efficiency.
        /// </summary>
        Dynamic = 2,
        
        /// <summary>
        /// High-performance strategy optimized for 60+ FPS gameplay.
        /// Minimizes allocations and frame time variance.
        /// </summary>
        HighPerformance = 3,
        
        /// <summary>
        /// Adaptive network strategy for handling network traffic spikes.
        /// Optimized for network buffer pooling and burst handling.
        /// </summary>
        AdaptiveNetwork = 4,
        
        /// <summary>
        /// Circuit breaker strategy with failure detection and recovery.
        /// Provides resilience against cascading pool failures.
        /// </summary>
        CircuitBreaker = 5
    }
}