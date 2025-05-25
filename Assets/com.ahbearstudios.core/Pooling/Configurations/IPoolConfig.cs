using AhBearStudios.Pooling.Core;
using Unity.Collections;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Defines the contract for pool configurations that control behavior of object pools.
    /// </summary>
    public interface IPoolConfig
    {
        /// <summary>
        /// Unique Id of the poolconfiguration
        /// </summary>
        string ConfigId { get; set; }
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        int InitialCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain, preventing
        /// shrinking below this threshold. This ensures consistent performance
        /// during usage spikes.
        /// </summary>
        int MinimumCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum size of the pool (0 for unlimited).
        /// </summary>
        int MaximumCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets whether to prewarm the pool on initialization.
        /// </summary>
        bool PrewarmOnInit { get; set; }
        
        /// <summary>
        /// Gets or sets whether to collect metrics for this pool.
        /// </summary>
        bool CollectMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets whether to log detailed pool operations.
        /// </summary>
        bool DetailedLogging { get; set; }
        
        /// <summary>
        /// Gets or sets whether to log warnings when the pool grows.
        /// </summary>
        bool LogWarnings { get; set; }
        
        /// <summary>
        /// Gets or sets whether to call Reset() on objects when they are released.
        /// </summary>
        bool ResetOnRelease { get; set; }
        
        /// <summary>
        /// Gets or sets the threading mode for this pool.
        /// </summary>
        PoolThreadingMode ThreadingMode { get; set; }
        
        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        bool EnableAutoShrink { get; set; }
        
        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        float ShrinkThreshold { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        float ShrinkInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the native allocator to use for native pools.
        /// </summary>
        Allocator NativeAllocator { get; set; }
        
        /// <summary>
        /// Gets or sets whether to use exponential growth when expanding the pool.
        /// </summary>
        bool UseExponentialGrowth { get; set; }
        
        /// <summary>
        /// Gets or sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        float GrowthFactor { get; set; }
        
        /// <summary>
        /// Gets or sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        int GrowthIncrement { get; set; }
        
        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        bool ThrowIfExceedingMaxCount { get; set; }
        
        /// <summary>
        /// Creates a deep copy of the configuration.
        /// </summary>
        /// <returns>A new instance of the configuration with the same settings.</returns>
        IPoolConfig Clone();
    }
}