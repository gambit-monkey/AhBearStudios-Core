using AhBearStudios.Core.Pooling.Configurations;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Base class for all pool configuration builders implementing the fluent pattern
    /// </summary>
    /// <typeparam name="TConfig">The configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public abstract class PoolConfigBuilderBase<TConfig, TBuilder> : IPoolConfigBuilder<TConfig, TBuilder> 
        where TConfig : IPoolConfig
        where TBuilder : PoolConfigBuilderBase<TConfig, TBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        protected TConfig Config { get; set; }
        
        /// <summary>
        /// Sets the initial capacity of the pool
        /// </summary>
        /// <param name="capacity">The initial capacity value</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithInitialCapacity(int capacity)
        {
            Config.InitialCapacity = capacity;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the maximum size of the pool
        /// </summary>
        /// <param name="maxSize">The maximum size (0 for unlimited)</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithMaxSize(int maxSize)
        {
            Config.MaximumCapacity = maxSize;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the pool should prewarm on initialization
        /// </summary>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithPrewarming(bool prewarm)
        {
            Config.PrewarmOnInit = prewarm;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the pool should collect metrics
        /// </summary>
        /// <param name="collectMetrics">Whether to collect metrics</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithMetricsCollection(bool collectMetrics)
        {
            Config.CollectMetrics = collectMetrics;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the pool should use detailed logging
        /// </summary>
        /// <param name="detailedLogging">Whether to use detailed logging</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithDetailedLogging(bool detailedLogging)
        {
            Config.DetailedLogging = detailedLogging;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the pool should log warnings
        /// </summary>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithWarningLogging(bool logWarnings)
        {
            Config.LogWarnings = logWarnings;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether objects should be reset when released back to the pool
        /// </summary>
        /// <param name="resetOnRelease">Whether to reset objects on release</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithResetOnRelease(bool resetOnRelease)
        {
            Config.ResetOnRelease = resetOnRelease;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        /// <param name="threadingMode">The threading mode to use</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            Config.ThreadingMode = threadingMode;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets whether the pool should automatically shrink
        /// </summary>
        /// <param name="enableAutoShrink">Whether to enable auto-shrinking</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithAutoShrink(bool enableAutoShrink)
        {
            Config.EnableAutoShrink = enableAutoShrink;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the threshold ratio below which the pool will auto-shrink
        /// </summary>
        /// <param name="threshold">The threshold ratio (0.0-1.0)</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithShrinkThreshold(float threshold)
        {
            Config.ShrinkThreshold = threshold;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the minimum time between auto-shrink operations
        /// </summary>
        /// <param name="intervalSeconds">The interval in seconds</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithShrinkInterval(float intervalSeconds)
        {
            Config.ShrinkInterval = intervalSeconds;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Sets the native allocator to use for native collections
        /// </summary>
        /// <param name="allocator">The allocator to use</param>
        /// <returns>The builder instance for method chaining</returns>
        public TBuilder WithNativeAllocator(Allocator allocator)
        {
            Config.NativeAllocator = allocator;
            return (TBuilder)this;
        }
        
        /// <summary>
        /// Builds and returns the completed configuration
        /// </summary>
        /// <returns>The completed configuration object</returns>
        public abstract TConfig Build();
    }
}