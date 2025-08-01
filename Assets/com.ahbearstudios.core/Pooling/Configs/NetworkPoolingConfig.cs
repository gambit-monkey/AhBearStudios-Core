using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Configs
{
    /// <summary>
    /// Configuration for network-related object pools.
    /// Defines buffer sizes and pooling strategies optimized for network serialization.
    /// </summary>
    public class NetworkPoolingConfig
    {
        /// <summary>
        /// Configuration for small network buffers (up to 1KB).
        /// Used for simple types like primitives, Vector3, Quaternion.
        /// </summary>
        public PoolConfiguration SmallBufferPoolConfig { get; set; } = new PoolConfiguration
        {
            Name = "SmallNetworkBuffer",
            InitialCapacity = 100,
            MaxCapacity = 500,
            Factory = () => new PooledNetworkBuffer(1024), // 1KB buffers
            ResetAction = buffer => buffer.Reset(),
            ValidationFunc = buffer => buffer.IsValid(),
            ValidationInterval = TimeSpan.FromMinutes(5),
            MaxIdleTime = TimeSpan.FromMinutes(10),
            Strategy = new DynamicSizeStrategy()
        };

        /// <summary>
        /// Configuration for medium network buffers (up to 16KB).
        /// Used for medium complexity objects and collections.
        /// </summary>
        public PoolConfiguration MediumBufferPoolConfig { get; set; } = new PoolConfiguration
        {
            Name = "MediumNetworkBuffer",
            InitialCapacity = 50,
            MaxCapacity = 200,
            Factory = () => new PooledNetworkBuffer(16384), // 16KB buffers
            ResetAction = buffer => buffer.Reset(),
            ValidationFunc = buffer => buffer.IsValid(),
            ValidationInterval = TimeSpan.FromMinutes(5),
            MaxIdleTime = TimeSpan.FromMinutes(15),
            Strategy = new DynamicSizeStrategy()
        };

        /// <summary>
        /// Configuration for large network buffers (up to 64KB).
        /// Used for complex objects, large collections, and compressed data.
        /// </summary>
        public PoolConfiguration LargeBufferPoolConfig { get; set; } = new PoolConfiguration
        {
            Name = "LargeNetworkBuffer",
            InitialCapacity = 20,
            MaxCapacity = 100,
            Factory = () => new PooledNetworkBuffer(65536), // 64KB buffers
            ResetAction = buffer => buffer.Reset(),
            ValidationFunc = buffer => buffer.IsValid(),
            ValidationInterval = TimeSpan.FromMinutes(5),
            MaxIdleTime = TimeSpan.FromMinutes(20),
            Strategy = new DynamicSizeStrategy()
        };

        /// <summary>
        /// Configuration for compression working buffers.
        /// Used by the compression service for network payload optimization.
        /// </summary>
        public PoolConfiguration CompressionBufferPoolConfig { get; set; } = new PoolConfiguration
        {
            Name = "CompressionBuffer",
            InitialCapacity = 25,
            MaxCapacity = 100,
            Factory = () => new PooledNetworkBuffer(32768), // 32KB compression buffers
            ResetAction = buffer => buffer.Reset(),
            ValidationFunc = buffer => buffer.IsValid(),
            ValidationInterval = TimeSpan.FromMinutes(5),
            MaxIdleTime = TimeSpan.FromMinutes(10),
            Strategy = new DynamicSizeStrategy()
        };

        /// <summary>
        /// Gets the appropriate buffer pool configuration based on expected data size.
        /// </summary>
        /// <param name="expectedSize">Expected data size in bytes</param>
        /// <returns>Appropriate pool configuration</returns>
        public PoolConfiguration GetBufferPoolConfig(int expectedSize)
        {
            return expectedSize switch
            {
                <= 1024 => SmallBufferPoolConfig,
                <= 16384 => MediumBufferPoolConfig,
                _ => LargeBufferPoolConfig
            };
        }

        /// <summary>
        /// Creates a default network pooling configuration optimized for FishNet + MemoryPack.
        /// </summary>
        /// <returns>Default configuration</returns>
        public static NetworkPoolingConfig CreateDefault()
        {
            return new NetworkPoolingConfig();
        }

        /// <summary>
        /// Creates a high-performance configuration for intensive network operations.
        /// </summary>
        /// <returns>High-performance configuration</returns>
        public static NetworkPoolingConfig CreateHighPerformance()
        {
            var config = new NetworkPoolingConfig();

            // Increase pool sizes for high-throughput scenarios
            config.SmallBufferPoolConfig.InitialCapacity = 200;
            config.SmallBufferPoolConfig.MaxCapacity = 1000;

            config.MediumBufferPoolConfig.InitialCapacity = 100;
            config.MediumBufferPoolConfig.MaxCapacity = 500;

            config.LargeBufferPoolConfig.InitialCapacity = 50;
            config.LargeBufferPoolConfig.MaxCapacity = 250;

            config.CompressionBufferPoolConfig.InitialCapacity = 50;
            config.CompressionBufferPoolConfig.MaxCapacity = 200;

            return config;
        }

        /// <summary>
        /// Creates a memory-optimized configuration for resource-constrained environments.
        /// </summary>
        /// <returns>Memory-optimized configuration</returns>
        public static NetworkPoolingConfig CreateMemoryOptimized()
        {
            var config = new NetworkPoolingConfig();

            // Reduce pool sizes and use shorter idle times
            config.SmallBufferPoolConfig.InitialCapacity = 50;
            config.SmallBufferPoolConfig.MaxCapacity = 200;
            config.SmallBufferPoolConfig.MaxIdleTime = TimeSpan.FromMinutes(5);

            config.MediumBufferPoolConfig.InitialCapacity = 25;
            config.MediumBufferPoolConfig.MaxCapacity = 100;
            config.MediumBufferPoolConfig.MaxIdleTime = TimeSpan.FromMinutes(7);

            config.LargeBufferPoolConfig.InitialCapacity = 10;
            config.LargeBufferPoolConfig.MaxCapacity = 50;
            config.LargeBufferPoolConfig.MaxIdleTime = TimeSpan.FromMinutes(10);

            config.CompressionBufferPoolConfig.InitialCapacity = 10;
            config.CompressionBufferPoolConfig.MaxCapacity = 50;
            config.CompressionBufferPoolConfig.MaxIdleTime = TimeSpan.FromMinutes(5);

            return config;
        }
    }
}