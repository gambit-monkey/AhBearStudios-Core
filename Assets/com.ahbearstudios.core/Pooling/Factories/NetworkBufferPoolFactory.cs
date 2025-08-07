using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory implementation for creating network buffer pools.
    /// Responsible for creating individual buffer pools with proper configuration and validation.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public class NetworkBufferPoolFactory : INetworkBufferPoolFactory
    {
        private readonly IPoolValidationService _validationService;
        private readonly IPooledNetworkBufferFactory _bufferFactory;
        private readonly IAdaptiveNetworkStrategyFactory _adaptiveNetworkStrategyFactory;
        private readonly IHighPerformanceStrategyFactory _highPerformanceStrategyFactory;
        private readonly IDynamicSizeStrategyFactory _dynamicSizeStrategyFactory;

        /// <summary>
        /// Initializes a new instance of the NetworkBufferPoolFactory.
        /// </summary>
        /// <param name="validationService">Service for pool validation operations</param>
        /// <param name="bufferFactory">Factory for creating individual network buffers</param>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for creating adaptive network strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for creating high performance strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for creating dynamic size strategies</param>
        public NetworkBufferPoolFactory(
            IPoolValidationService validationService,
            IPooledNetworkBufferFactory bufferFactory,
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _bufferFactory = bufferFactory ?? throw new ArgumentNullException(nameof(bufferFactory));
            _adaptiveNetworkStrategyFactory = adaptiveNetworkStrategyFactory ?? throw new ArgumentNullException(nameof(adaptiveNetworkStrategyFactory));
            _highPerformanceStrategyFactory = highPerformanceStrategyFactory ?? throw new ArgumentNullException(nameof(highPerformanceStrategyFactory));
            _dynamicSizeStrategyFactory = dynamicSizeStrategyFactory ?? throw new ArgumentNullException(nameof(dynamicSizeStrategyFactory));
        }

        /// <summary>
        /// Creates a small buffer pool optimized for simple types (1KB buffers).
        /// Uses AdaptiveNetworkStrategy for handling frequent small packet traffic.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured small buffer pool</returns>
        public SmallBufferPool CreateSmallBufferPool(PoolConfiguration poolConfig)
        {
            if (poolConfig == null)
                throw new ArgumentNullException(nameof(poolConfig));

            var config = CreatePoolConfigForSmallBuffers(poolConfig);
            return new SmallBufferPool(config, _adaptiveNetworkStrategyFactory);
        }

        /// <summary>
        /// Creates a medium buffer pool optimized for medium complexity objects (16KB buffers).
        /// Uses HighPerformanceStrategy for consistent 60+ FPS performance.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured medium buffer pool</returns>
        public MediumBufferPool CreateMediumBufferPool(PoolConfiguration poolConfig)
        {
            if (poolConfig == null)
                throw new ArgumentNullException(nameof(poolConfig));

            var config = CreatePoolConfigForMediumBuffers(poolConfig);
            return new MediumBufferPool(config, _highPerformanceStrategyFactory);
        }

        /// <summary>
        /// Creates a large buffer pool optimized for complex objects (64KB buffers).
        /// Uses DynamicSizeStrategy for memory-conscious scaling.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured large buffer pool</returns>
        public LargeBufferPool CreateLargeBufferPool(PoolConfiguration poolConfig)
        {
            if (poolConfig == null)
                throw new ArgumentNullException(nameof(poolConfig));

            var config = CreatePoolConfigForLargeBuffers(poolConfig);
            return new LargeBufferPool(config, _dynamicSizeStrategyFactory);
        }

        /// <summary>
        /// Creates a compression buffer pool optimized for compression operations (32KB buffers).
        /// Uses AdaptiveNetworkStrategy for compression workload spikes.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured compression buffer pool</returns>
        public CompressionBufferPool CreateCompressionBufferPool(PoolConfiguration poolConfig)
        {
            if (poolConfig == null)
                throw new ArgumentNullException(nameof(poolConfig));

            var config = CreatePoolConfigForCompressionBuffers(poolConfig);
            return new CompressionBufferPool(config, _adaptiveNetworkStrategyFactory);
        }

        /// <summary>
        /// Creates all network buffer pools from a network pooling configuration.
        /// Each pool uses its optimal strategy (Adaptive, HighPerformance, or Dynamic).
        /// </summary>
        /// <param name="networkConfig">Network pooling configuration</param>
        /// <returns>Collection of created buffer pools</returns>
        public NetworkBufferPools CreateAllBufferPools(NetworkPoolingConfig networkConfig)
        {
            if (networkConfig == null)
                throw new ArgumentNullException(nameof(networkConfig));

            return new NetworkBufferPools
            {
                SmallBufferPool = CreateSmallBufferPool(networkConfig.SmallBufferPoolConfig),
                MediumBufferPool = CreateMediumBufferPool(networkConfig.MediumBufferPoolConfig),
                LargeBufferPool = CreateLargeBufferPool(networkConfig.LargeBufferPoolConfig),
                CompressionBufferPool = CreateCompressionBufferPool(networkConfig.CompressionBufferPoolConfig)
            };
        }

        /// <summary>
        /// Creates pool configuration for small buffers with validation and reset actions.
        /// </summary>
        /// <param name="baseConfig">Base pool configuration</param>
        /// <returns>Configured pool configuration for small buffers</returns>
        private PoolConfiguration CreatePoolConfigForSmallBuffers(PoolConfiguration baseConfig)
        {
            return new PoolConfiguration
            {
                Name = "SmallBuffer",
                InitialCapacity = baseConfig.InitialCapacity,
                MaxCapacity = baseConfig.MaxCapacity,
                Factory = () => _bufferFactory.CreateSmallBuffer(),
                ResetAction = buffer => _validationService.ResetPooledObject(buffer),
                ValidationFunc = buffer => _validationService.ValidatePooledObject(buffer),
                ValidationInterval = baseConfig.ValidationInterval,
                MaxIdleTime = baseConfig.MaxIdleTime,
                EnableValidation = baseConfig.EnableValidation,
                EnableStatistics = baseConfig.EnableStatistics,
                DisposalPolicy = baseConfig.DisposalPolicy
            };
        }

        /// <summary>
        /// Creates pool configuration for medium buffers with validation and reset actions.
        /// </summary>
        /// <param name="baseConfig">Base pool configuration</param>
        /// <returns>Configured pool configuration for medium buffers</returns>
        private PoolConfiguration CreatePoolConfigForMediumBuffers(PoolConfiguration baseConfig)
        {
            return new PoolConfiguration
            {
                Name = "MediumBuffer",
                InitialCapacity = baseConfig.InitialCapacity,
                MaxCapacity = baseConfig.MaxCapacity,
                Factory = () => _bufferFactory.CreateMediumBuffer(),
                ResetAction = buffer => _validationService.ResetPooledObject(buffer),
                ValidationFunc = buffer => _validationService.ValidatePooledObject(buffer),
                ValidationInterval = baseConfig.ValidationInterval,
                MaxIdleTime = baseConfig.MaxIdleTime,
                EnableValidation = baseConfig.EnableValidation,
                EnableStatistics = baseConfig.EnableStatistics,
                DisposalPolicy = baseConfig.DisposalPolicy
            };
        }

        /// <summary>
        /// Creates pool configuration for large buffers with validation and reset actions.
        /// </summary>
        /// <param name="baseConfig">Base pool configuration</param>
        /// <returns>Configured pool configuration for large buffers</returns>
        private PoolConfiguration CreatePoolConfigForLargeBuffers(PoolConfiguration baseConfig)
        {
            return new PoolConfiguration
            {
                Name = "LargeBuffer",
                InitialCapacity = baseConfig.InitialCapacity,
                MaxCapacity = baseConfig.MaxCapacity,
                Factory = () => _bufferFactory.CreateLargeBuffer(),
                ResetAction = buffer => _validationService.ResetPooledObject(buffer),
                ValidationFunc = buffer => _validationService.ValidatePooledObject(buffer),
                ValidationInterval = baseConfig.ValidationInterval,
                MaxIdleTime = baseConfig.MaxIdleTime,
                EnableValidation = baseConfig.EnableValidation,
                EnableStatistics = baseConfig.EnableStatistics,
                DisposalPolicy = baseConfig.DisposalPolicy
            };
        }

        /// <summary>
        /// Creates pool configuration for compression buffers with validation and reset actions.
        /// </summary>
        /// <param name="baseConfig">Base pool configuration</param>
        /// <returns>Configured pool configuration for compression buffers</returns>
        private PoolConfiguration CreatePoolConfigForCompressionBuffers(PoolConfiguration baseConfig)
        {
            return new PoolConfiguration
            {
                Name = "CompressionBuffer",
                InitialCapacity = baseConfig.InitialCapacity,
                MaxCapacity = baseConfig.MaxCapacity,
                Factory = () => _bufferFactory.CreateCompressionBuffer(),
                ResetAction = buffer => _validationService.ResetPooledObject(buffer),
                ValidationFunc = buffer => _validationService.ValidatePooledObject(buffer),
                ValidationInterval = baseConfig.ValidationInterval,
                MaxIdleTime = baseConfig.MaxIdleTime,
                EnableValidation = baseConfig.EnableValidation,
                EnableStatistics = baseConfig.EnableStatistics,
                DisposalPolicy = baseConfig.DisposalPolicy
            };
        }
    }
}