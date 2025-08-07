using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder implementation for constructing NetworkPoolingConfig instances.
    /// Works with immutable records using with-expressions.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public class NetworkPoolingConfigBuilder : INetworkPoolingConfigBuilder
    {
        private NetworkPoolingConfig _config;
        private readonly IPooledNetworkBufferFactory _bufferFactory;

        /// <summary>
        /// Initializes a new instance of the NetworkPoolingConfigBuilder.
        /// </summary>
        /// <param name="bufferFactory">Factory for creating network buffers</param>
        public NetworkPoolingConfigBuilder(IPooledNetworkBufferFactory bufferFactory)
        {
            _bufferFactory = bufferFactory ?? throw new ArgumentNullException(nameof(bufferFactory));
            _config = NetworkPoolingConfig.CreateDefault();
        }

        /// <summary>
        /// Creates a default network pooling configuration optimized for FishNet + MemoryPack.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithDefaults()
        {
            // Configuration is already set to defaults in constructor
            return this;
        }

        /// <summary>
        /// Creates a high-performance configuration for intensive network operations.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithHighPerformance()
        {
            _config = _config with
            {
                SmallBufferPoolConfig = _config.SmallBufferPoolConfig with
                {
                    InitialCapacity = 200,
                    MaxCapacity = 1000,
                    ValidationInterval = TimeSpan.FromMinutes(1),
                },
                MediumBufferPoolConfig = _config.MediumBufferPoolConfig with
                {
                    InitialCapacity = 100,
                    MaxCapacity = 500,
                    ValidationInterval = TimeSpan.FromMinutes(2),
                },
                LargeBufferPoolConfig = _config.LargeBufferPoolConfig with
                {
                    InitialCapacity = 50,
                    MaxCapacity = 250,
                    ValidationInterval = TimeSpan.FromMinutes(3),
                },
                CompressionBufferPoolConfig = _config.CompressionBufferPoolConfig with
                {
                    InitialCapacity = 50,
                    MaxCapacity = 200,
                    ValidationInterval = TimeSpan.FromMinutes(1),
                }
            };
            return this;
        }

        /// <summary>
        /// Creates a memory-optimized configuration for resource-constrained environments.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithMemoryOptimized()
        {
            _config = _config with
            {
                SmallBufferPoolConfig = _config.SmallBufferPoolConfig with
                {
                    InitialCapacity = 50,
                    MaxCapacity = 200,
                    MaxIdleTime = TimeSpan.FromMinutes(5),
                    ValidationInterval = TimeSpan.FromMinutes(5),
                },
                MediumBufferPoolConfig = _config.MediumBufferPoolConfig with
                {
                    InitialCapacity = 25,
                    MaxCapacity = 100,
                    MaxIdleTime = TimeSpan.FromMinutes(7),
                    ValidationInterval = TimeSpan.FromMinutes(7),
                },
                LargeBufferPoolConfig = _config.LargeBufferPoolConfig with
                {
                    InitialCapacity = 10,
                    MaxCapacity = 50,
                    MaxIdleTime = TimeSpan.FromMinutes(10),
                    ValidationInterval = TimeSpan.FromMinutes(10),
                },
                CompressionBufferPoolConfig = _config.CompressionBufferPoolConfig with
                {
                    InitialCapacity = 10,
                    MaxCapacity = 50,
                    MaxIdleTime = TimeSpan.FromMinutes(5),
                    ValidationInterval = TimeSpan.FromMinutes(5),
                }
            };
            return this;
        }

        /// <summary>
        /// Configures small buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithSmallBufferPool(int initialCapacity, int maxCapacity)
        {
            _config = _config with
            {
                SmallBufferPoolConfig = _config.SmallBufferPoolConfig with
                {
                    InitialCapacity = initialCapacity,
                    MaxCapacity = maxCapacity
                }
            };
            return this;
        }

        /// <summary>
        /// Configures medium buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithMediumBufferPool(int initialCapacity, int maxCapacity)
        {
            _config = _config with
            {
                MediumBufferPoolConfig = _config.MediumBufferPoolConfig with
                {
                    InitialCapacity = initialCapacity,
                    MaxCapacity = maxCapacity
                }
            };
            return this;
        }

        /// <summary>
        /// Configures large buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithLargeBufferPool(int initialCapacity, int maxCapacity)
        {
            _config = _config with
            {
                LargeBufferPoolConfig = _config.LargeBufferPoolConfig with
                {
                    InitialCapacity = initialCapacity,
                    MaxCapacity = maxCapacity
                }
            };
            return this;
        }

        /// <summary>
        /// Configures compression buffer pool settings.
        /// </summary>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <returns>Builder instance for method chaining</returns>
        public INetworkPoolingConfigBuilder WithCompressionBufferPool(int initialCapacity, int maxCapacity)
        {
            _config = _config with
            {
                CompressionBufferPoolConfig = _config.CompressionBufferPoolConfig with
                {
                    InitialCapacity = initialCapacity,
                    MaxCapacity = maxCapacity
                }
            };
            return this;
        }

        /// <summary>
        /// Builds the configured NetworkPoolingConfig instance.
        /// </summary>
        /// <returns>Configured NetworkPoolingConfig</returns>
        public NetworkPoolingConfig Build()
        {
            // Update factory references in the configuration using with-expressions
            return _config with
            {
                SmallBufferPoolConfig = _config.SmallBufferPoolConfig with
                {
                    Factory = () => _bufferFactory.CreateSmallBuffer()
                },
                MediumBufferPoolConfig = _config.MediumBufferPoolConfig with
                {
                    Factory = () => _bufferFactory.CreateMediumBuffer()
                },
                LargeBufferPoolConfig = _config.LargeBufferPoolConfig with
                {
                    Factory = () => _bufferFactory.CreateLargeBuffer()
                },
                CompressionBufferPoolConfig = _config.CompressionBufferPoolConfig with
                {
                    Factory = () => _bufferFactory.CreateCompressionBuffer()
                }
            };
        }
    }
}