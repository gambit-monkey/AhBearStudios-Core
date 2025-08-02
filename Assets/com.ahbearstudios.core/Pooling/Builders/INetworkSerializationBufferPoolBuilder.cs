using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder interface for constructing NetworkSerializationBufferPool instances.
    /// Provides fluent API for configuring the buffer pool service with all required dependencies.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public interface INetworkSerializationBufferPoolBuilder
    {
        /// <summary>
        /// Sets the logging service for the buffer pool.
        /// </summary>
        /// <param name="loggingService">Logging service instance</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithLoggingService(ILoggingService loggingService);

        /// <summary>
        /// Sets the network pooling configuration.
        /// </summary>
        /// <param name="configuration">Network pooling configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithConfiguration(NetworkPoolingConfig configuration);

        /// <summary>
        /// Sets the network buffer pool factory.
        /// </summary>
        /// <param name="poolFactory">Network buffer pool factory</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithPoolFactory(INetworkBufferPoolFactory poolFactory);

        /// <summary>
        /// Sets the pool validation service.
        /// </summary>
        /// <param name="validationService">Pool validation service</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithValidationService(IPoolValidationService validationService);

        /// <summary>
        /// Sets the pooling strategy to use for all buffer pools.
        /// </summary>
        /// <param name="strategy">Pooling strategy</param>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithPoolingStrategy(IPoolingStrategy strategy);

        /// <summary>
        /// Configures the builder with default settings optimized for FishNet + MemoryPack.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithDefaults();

        /// <summary>
        /// Configures the builder with high-performance settings for intensive network operations.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithHighPerformance();

        /// <summary>
        /// Configures the builder with memory-optimized settings for resource-constrained environments.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        INetworkSerializationBufferPoolBuilder WithMemoryOptimized();

        /// <summary>
        /// Builds the configured NetworkSerializationBufferPool instance.
        /// </summary>
        /// <returns>Configured NetworkSerializationBufferPool service</returns>
        NetworkSerializationBufferPool Build();

        /// <summary>
        /// Validates the current builder state and returns any validation errors.
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        System.Collections.Generic.List<string> Validate();
    }
}