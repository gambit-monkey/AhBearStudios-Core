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
        /// Uses AdaptiveNetworkStrategy for handling frequent small packet traffic.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured small buffer pool</returns>
        SmallBufferPool CreateSmallBufferPool(PoolConfiguration poolConfig);

        /// <summary>
        /// Creates a medium buffer pool optimized for medium complexity objects (16KB buffers).
        /// Uses HighPerformanceStrategy for consistent 60+ FPS performance.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured medium buffer pool</returns>
        MediumBufferPool CreateMediumBufferPool(PoolConfiguration poolConfig);

        /// <summary>
        /// Creates a large buffer pool optimized for complex objects (64KB buffers).
        /// Uses DynamicSizeStrategy for memory-conscious scaling.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured large buffer pool</returns>
        LargeBufferPool CreateLargeBufferPool(PoolConfiguration poolConfig);

        /// <summary>
        /// Creates a compression buffer pool optimized for compression operations (32KB buffers).
        /// Uses AdaptiveNetworkStrategy for compression workload spikes.
        /// </summary>
        /// <param name="poolConfig">Pool configuration</param>
        /// <returns>Configured compression buffer pool</returns>
        CompressionBufferPool CreateCompressionBufferPool(PoolConfiguration poolConfig);

        /// <summary>
        /// Creates all network buffer pools from a network pooling configuration.
        /// Each pool uses its optimal strategy (Adaptive, HighPerformance, or Dynamic).
        /// </summary>
        /// <param name="networkConfig">Network pooling configuration</param>
        /// <returns>Collection of created buffer pools</returns>
        NetworkBufferPools CreateAllBufferPools(NetworkPoolingConfig networkConfig);
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