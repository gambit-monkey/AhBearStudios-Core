using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating network buffer pools.
    /// Responsible for creating individual buffer pools (Small, Medium, Large, Compression).
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public interface INetworkBufferPoolFactory
    {
        /// <summary>
        /// Creates a small buffer pool optimized for simple types (1KB buffers).
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <param name="strategy">Pooling strategy</param>
        /// <returns>Configured small buffer pool</returns>
        SmallBufferPool CreateSmallBufferPool(PoolConfiguration poolConfig, IPoolingStrategy strategy);

        /// <summary>
        /// Creates a medium buffer pool optimized for medium complexity objects (16KB buffers).
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <param name="strategy">Pooling strategy</param>
        /// <returns>Configured medium buffer pool</returns>
        MediumBufferPool CreateMediumBufferPool(PoolConfiguration poolConfig, IPoolingStrategy strategy);

        /// <summary>
        /// Creates a large buffer pool optimized for complex objects (64KB buffers).
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <param name="strategy">Pooling strategy</param>
        /// <returns>Configured large buffer pool</returns>
        LargeBufferPool CreateLargeBufferPool(PoolConfiguration poolConfig, IPoolingStrategy strategy);

        /// <summary>
        /// Creates a compression buffer pool optimized for compression operations (32KB buffers).
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <param name="strategy">Pooling strategy</param>
        /// <returns>Configured compression buffer pool</returns>
        CompressionBufferPool CreateCompressionBufferPool(PoolConfiguration poolConfig, IPoolingStrategy strategy);

        /// <summary>
        /// Creates all network buffer pools from a network pooling configuration.
        /// </summary>
        /// <param name="networkConfig">Network pooling configuration</param>
        /// <param name="strategy">Pooling strategy to use for all pools</param>
        /// <returns>Collection of created buffer pools</returns>
        NetworkBufferPools CreateAllBufferPools(NetworkPoolingConfig networkConfig, IPoolingStrategy strategy);
    }

    /// <summary>
    /// Container for all network buffer pools created by the factory.
    /// </summary>
    public class NetworkBufferPools
    {
        /// <summary>
        /// Small buffer pool (1KB buffers).
        /// </summary>
        public SmallBufferPool SmallBufferPool { get; init; }

        /// <summary>
        /// Medium buffer pool (16KB buffers).
        /// </summary>
        public MediumBufferPool MediumBufferPool { get; init; }

        /// <summary>
        /// Large buffer pool (64KB buffers).
        /// </summary>
        public LargeBufferPool LargeBufferPool { get; init; }

        /// <summary>
        /// Compression buffer pool (32KB buffers).
        /// </summary>
        public CompressionBufferPool CompressionBufferPool { get; init; }
    }
}