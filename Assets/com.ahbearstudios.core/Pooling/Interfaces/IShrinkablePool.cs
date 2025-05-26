namespace AhBearStudios.Core.Pooling.Interfaces
{
    /// <summary>
    /// Interface for pools that can shrink their capacity to reclaim memory.
    /// Extends the base IPool interface with functionality for pool shrinking.
    /// </summary>
    public interface IShrinkablePool : IPool
    {
        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        bool SupportsAutoShrink { get; }
        
        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs.
        /// For example, 0.5 means shrink when usage is below 50% of capacity.</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        bool TryShrink(float threshold);
        
        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain 
        /// even when shrinking
        /// </summary>
        int MinimumCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        int MaximumCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the shrink interval in seconds.
        /// If automatic shrinking is enabled, this determines how frequently
        /// the pool will attempt to shrink.
        /// </summary>
        float ShrinkInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand.
        /// For example, 2.0 means double the capacity.
        /// </summary>
        float GrowthFactor { get; set; }
        
        /// <summary>
        /// Gets or sets the shrink threshold.
        /// This is the usage-to-capacity ratio below which shrinking will occur.
        /// </summary>
        float ShrinkThreshold { get; set; }
        
        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        void SetAutoShrink(bool enabled);
        
        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        bool ShrinkTo(int targetCapacity);
    }
}