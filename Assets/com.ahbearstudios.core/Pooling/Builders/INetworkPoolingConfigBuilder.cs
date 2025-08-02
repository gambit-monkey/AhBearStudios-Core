using AhBearStudios.Core.Pooling.Configs;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder interface for constructing NetworkPoolingConfig instances.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public interface INetworkPoolingConfigBuilder
    {
        /// <summary>
        /// Creates a default network pooling configuration optimized for FishNet + MemoryPack.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithDefaults();

        /// <summary>
        /// Creates a high-performance configuration for intensive network operations.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithHighPerformance();

        /// <summary>
        /// Creates a memory-optimized configuration for resource-constrained environments.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithMemoryOptimized();

        /// <summary>
        /// Configures small buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithSmallBufferPool(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Configures medium buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithMediumBufferPool(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Configures large buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithLargeBufferPool(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Configures compression buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkPoolingConfigBuilder WithCompressionBufferPool(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Builds the configured NetworkPoolingConfig instance.
        /// </summary>
        /// <returns>Configured NetworkPoolingConfig</returns>
        NetworkPoolingConfig Build();
    }
}