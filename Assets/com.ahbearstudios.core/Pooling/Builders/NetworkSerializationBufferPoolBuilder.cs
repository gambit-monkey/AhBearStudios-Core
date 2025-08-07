using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder implementation for constructing NetworkSerializationBufferPool instances.
    /// Provides fluent API for configuring the buffer pool service with all required dependencies.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public class NetworkSerializationBufferPoolBuilder : INetworkSerializationBufferPoolBuilder
    {
        private ILoggingService _loggingService;
        private NetworkPoolingConfig _configuration;
        private INetworkBufferPoolFactory _poolFactory;
        private IPoolValidationService _validationService;
        private readonly IAdaptiveNetworkStrategyFactory _adaptiveNetworkStrategyFactory;
        private readonly IHighPerformanceStrategyFactory _highPerformanceStrategyFactory;
        private readonly IDynamicSizeStrategyFactory _dynamicSizeStrategyFactory;

        /// <summary>
        /// Initializes a new instance of the NetworkSerializationBufferPoolBuilder.
        /// </summary>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for creating adaptive network strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for creating high performance strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for creating dynamic size strategies</param>
        public NetworkSerializationBufferPoolBuilder(
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory)
        {
            _adaptiveNetworkStrategyFactory = adaptiveNetworkStrategyFactory ?? throw new ArgumentNullException(nameof(adaptiveNetworkStrategyFactory));
            _highPerformanceStrategyFactory = highPerformanceStrategyFactory ?? throw new ArgumentNullException(nameof(highPerformanceStrategyFactory));
            _dynamicSizeStrategyFactory = dynamicSizeStrategyFactory ?? throw new ArgumentNullException(nameof(dynamicSizeStrategyFactory));
        }

        /// <summary>
        /// Sets the logging service for the buffer pool.
        /// </summary>
        /// <param name="loggingService">Logging service instance</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithLoggingService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            return this;
        }

        /// <summary>
        /// Sets the network pooling configuration.
        /// </summary>
        /// <param name="configuration">Network pooling configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithConfiguration(NetworkPoolingConfig configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        /// <summary>
        /// Sets the network buffer pool factory.
        /// </summary>
        /// <param name="poolFactory">Network buffer pool factory</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithPoolFactory(INetworkBufferPoolFactory poolFactory)
        {
            _poolFactory = poolFactory ?? throw new ArgumentNullException(nameof(poolFactory));
            return this;
        }

        /// <summary>
        /// Sets the pool validation service.
        /// </summary>
        /// <param name="validationService">Pool validation service</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithValidationService(IPoolValidationService validationService)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            return this;
        }


        /// <summary>
        /// Configures the builder with default settings optimized for FishNet + MemoryPack.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithDefaults()
        {
            // Set default validation service if not already set (needed for factory)
            if (_validationService == null)
            {
                _validationService = new PoolValidationService();
            }

            // Set default pool factory if not already set (needed for config builder)
            if (_poolFactory == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _poolFactory = new NetworkBufferPoolFactory(
                    _validationService,
                    bufferFactory,
                    _adaptiveNetworkStrategyFactory,
                    _highPerformanceStrategyFactory,
                    _dynamicSizeStrategyFactory);
            }

            // Set default configuration if not already set
            if (_configuration == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _configuration = new NetworkPoolingConfigBuilder(bufferFactory)
                    .WithDefaults()
                    .Build();
            }


            return this;
        }

        /// <summary>
        /// Configures the builder with high-performance settings for intensive network operations.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithHighPerformance()
        {
            // Set validation service optimized for performance
            if (_validationService == null)
            {
                _validationService = new PoolValidationService();
            }

            // Set pool factory
            if (_poolFactory == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _poolFactory = new NetworkBufferPoolFactory(
                    _validationService,
                    bufferFactory,
                    _adaptiveNetworkStrategyFactory,
                    _highPerformanceStrategyFactory,
                    _dynamicSizeStrategyFactory);
            }

            // Set high-performance configuration
            if (_configuration == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _configuration = new NetworkPoolingConfigBuilder(bufferFactory)
                    .WithHighPerformance()
                    .Build();
            }


            return this;
        }

        /// <summary>
        /// Configures the builder with memory-optimized settings for resource-constrained environments.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkSerializationBufferPoolBuilder WithMemoryOptimized()
        {
            // Set validation service
            if (_validationService == null)
            {
                _validationService = new PoolValidationService();
            }

            // Set pool factory
            if (_poolFactory == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _poolFactory = new NetworkBufferPoolFactory(
                    _validationService,
                    bufferFactory,
                    _adaptiveNetworkStrategyFactory,
                    _highPerformanceStrategyFactory,
                    _dynamicSizeStrategyFactory);
            }

            // Set memory-optimized configuration
            if (_configuration == null)
            {
                var bufferFactory = new PooledNetworkBufferFactory();
                _configuration = new NetworkPoolingConfigBuilder(bufferFactory)
                    .WithMemoryOptimized()
                    .Build();
            }


            return this;
        }

        /// <summary>
        /// Validates the current builder state and returns any validation errors.
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (_loggingService == null)
                errors.Add("LoggingService is required");

            if (_configuration == null)
                errors.Add("Configuration is required");

            if (_poolFactory == null)
                errors.Add("PoolFactory is required");

            if (_validationService == null)
                errors.Add("ValidationService is required");


            // Validate configuration if present
            if (_configuration != null)
            {
                var configErrors = _configuration.Validate();
                if (configErrors.Count > 0)
                {
                    errors.Add($"Configuration validation failed: {string.Join(", ", configErrors)}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Builds the configured NetworkSerializationBufferPool instance.
        /// </summary>
        /// <returns>Configured NetworkSerializationBufferPool service</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder is in invalid state</exception>
        public NetworkSerializationBufferPool Build()
        {
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException($"Cannot build NetworkSerializationBufferPool: {string.Join(", ", validationErrors)}");
            }

            return new NetworkSerializationBufferPool(
                _loggingService,
                _configuration,
                _poolFactory,
                _validationService);
        }

        /// <summary>
        /// Creates a new builder instance with default settings.
        /// </summary>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for creating adaptive network strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for creating high performance strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for creating dynamic size strategies</param>
        /// <returns>New NetworkSerializationBufferPoolBuilder with defaults</returns>
        public static INetworkSerializationBufferPoolBuilder CreateDefault(
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory)
        {
            return new NetworkSerializationBufferPoolBuilder(
                adaptiveNetworkStrategyFactory,
                highPerformanceStrategyFactory,
                dynamicSizeStrategyFactory).WithDefaults();
        }

        /// <summary>
        /// Creates a new builder instance with high-performance settings.
        /// </summary>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for creating adaptive network strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for creating high performance strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for creating dynamic size strategies</param>
        /// <returns>New NetworkSerializationBufferPoolBuilder with high-performance settings</returns>
        public static INetworkSerializationBufferPoolBuilder CreateHighPerformance(
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory)
        {
            return new NetworkSerializationBufferPoolBuilder(
                adaptiveNetworkStrategyFactory,
                highPerformanceStrategyFactory,
                dynamicSizeStrategyFactory).WithHighPerformance();
        }

        /// <summary>
        /// Creates a new builder instance with memory-optimized settings.
        /// </summary>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for creating adaptive network strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for creating high performance strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for creating dynamic size strategies</param>
        /// <returns>New NetworkSerializationBufferPoolBuilder with memory-optimized settings</returns>
        public static INetworkSerializationBufferPoolBuilder CreateMemoryOptimized(
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory)
        {
            return new NetworkSerializationBufferPoolBuilder(
                adaptiveNetworkStrategyFactory,
                highPerformanceStrategyFactory,
                dynamicSizeStrategyFactory).WithMemoryOptimized();
        }
    }
}