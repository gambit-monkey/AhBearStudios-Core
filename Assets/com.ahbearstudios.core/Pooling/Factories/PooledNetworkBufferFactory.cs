using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory implementation for creating pooled network buffers.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public class PooledNetworkBufferFactory : IPooledNetworkBufferFactory
    {
        /// <summary>
        /// Creates a small buffer optimized for simple types (1KB capacity).
        /// </summary>
        /// <returns>Configured small network buffer</returns>
        public PooledNetworkBuffer CreateSmallBuffer()
        {
            return new PooledNetworkBuffer(1024) // 1KB buffers
            {
                PoolName = "SmallNetworkBuffer"
            };
        }

        /// <summary>
        /// Creates a medium buffer optimized for medium complexity objects (16KB capacity).
        /// </summary>
        /// <returns>Configured medium network buffer</returns>
        public PooledNetworkBuffer CreateMediumBuffer()
        {
            return new PooledNetworkBuffer(16384) // 16KB buffers
            {
                PoolName = "MediumNetworkBuffer"
            };
        }

        /// <summary>
        /// Creates a large buffer optimized for complex objects (64KB capacity).
        /// </summary>
        /// <returns>Configured large network buffer</returns>
        public PooledNetworkBuffer CreateLargeBuffer()
        {
            return new PooledNetworkBuffer(65536) // 64KB buffers
            {
                PoolName = "LargeNetworkBuffer"
            };
        }

        /// <summary>
        /// Creates a compression buffer optimized for compression operations (32KB capacity).
        /// </summary>
        /// <returns>Configured compression network buffer</returns>
        public PooledNetworkBuffer CreateCompressionBuffer()
        {
            return new PooledNetworkBuffer(32768) // 32KB compression buffers
            {
                PoolName = "CompressionBuffer"
            };
        }

        /// <summary>
        /// Creates a buffer with custom capacity and pool name.
        /// </summary>
        /// <param name="capacity">Buffer capacity in bytes</param>
        /// <param name="poolName">Name of the pool this buffer belongs to</param>
        /// <returns>Configured network buffer</returns>
        public PooledNetworkBuffer CreateCustomBuffer(int capacity, string poolName)
        {
            return new PooledNetworkBuffer(capacity)
            {
                PoolName = poolName ?? "CustomNetworkBuffer"
            };
        }
    }
}