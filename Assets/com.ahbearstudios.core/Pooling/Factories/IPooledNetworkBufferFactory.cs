using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating pooled network buffers.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public interface IPooledNetworkBufferFactory
    {
        /// <summary>
        /// Creates a small buffer optimized for simple types (1KB capacity).
        /// </summary>
        /// <returns>Configured small network buffer</returns>
        PooledNetworkBuffer CreateSmallBuffer();

        /// <summary>
        /// Creates a medium buffer optimized for medium complexity objects (16KB capacity).
        /// </summary>
        /// <returns>Configured medium network buffer</returns>
        PooledNetworkBuffer CreateMediumBuffer();

        /// <summary>
        /// Creates a large buffer optimized for complex objects (64KB capacity).
        /// </summary>
        /// <returns>Configured large network buffer</returns>
        PooledNetworkBuffer CreateLargeBuffer();

        /// <summary>
        /// Creates a compression buffer optimized for compression operations (32KB capacity).
        /// </summary>
        /// <returns>Configured compression network buffer</returns>
        PooledNetworkBuffer CreateCompressionBuffer();

        /// <summary>
        /// Creates a buffer with custom capacity and pool name.
        /// </summary>
        /// <param name="capacity">Buffer capacity in bytes</param>
        /// <param name="poolName">Name of the pool this buffer belongs to</param>
        /// <returns>Configured network buffer</returns>
        PooledNetworkBuffer CreateCustomBuffer(int capacity, string poolName);
    }
}