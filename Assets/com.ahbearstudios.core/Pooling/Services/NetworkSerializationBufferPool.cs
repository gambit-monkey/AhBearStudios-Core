using System;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Specialized buffer pool service for network serialization operations.
    /// Provides optimized buffer management for FishNet + MemoryPack integration.
    /// Acts as a facade over multiple IObjectPool instances for different buffer sizes.
    /// Follows the Builder → Config → Factory → Service design pattern.
    /// </summary>
    public class NetworkSerializationBufferPool : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly NetworkPoolingConfig _configuration;
        private readonly INetworkBufferPoolFactory _poolFactory;
        private readonly IPoolValidationService _validationService;
        private readonly IPoolingStrategy _poolingStrategy;
        private readonly NetworkBufferPools _bufferPools;
        private bool _disposed;

        // Performance statistics
        private long _smallBufferGets;
        private long _mediumBufferGets;
        private long _largeBufferGets;
        private long _compressionBufferGets;
        private long _totalBufferReturns;

        /// <summary>
        /// Initializes a new NetworkSerializationBufferPool with dependency injection.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="configuration">Network pooling configuration</param>
        /// <param name="poolFactory">Factory for creating buffer pools</param>
        /// <param name="validationService">Service for pool validation operations</param>
        /// <param name="poolingStrategy">Strategy for pool management</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public NetworkSerializationBufferPool(
            ILoggingService logger,
            NetworkPoolingConfig configuration,
            INetworkBufferPoolFactory poolFactory,
            IPoolValidationService validationService,
            IPoolingStrategy poolingStrategy)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _poolFactory = poolFactory ?? throw new ArgumentNullException(nameof(poolFactory));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _poolingStrategy = poolingStrategy ?? throw new ArgumentNullException(nameof(poolingStrategy));

            // Create all buffer pools using the factory
            _bufferPools = _poolFactory.CreateAllBufferPools(_configuration, _poolingStrategy);

            _logger.LogInfo($"NetworkSerializationBufferPool initialized with {_bufferPools.SmallBufferPool?.GetType().Name}, {_bufferPools.MediumBufferPool?.GetType().Name}, {_bufferPools.LargeBufferPool?.GetType().Name}, {_bufferPools.CompressionBufferPool?.GetType().Name}", default, nameof(NetworkSerializationBufferPool));
        }

        /// <summary>
        /// Gets a buffer suitable for the expected data size.
        /// Automatically selects the most appropriate buffer pool.
        /// </summary>
        /// <param name="expectedSize">Expected data size in bytes</param>
        /// <returns>Pooled network buffer</returns>
        public PooledNetworkBuffer GetBuffer(int expectedSize = 0)
        {
            ThrowIfDisposed();

            if (expectedSize <= 1024)
            {
                Interlocked.Increment(ref _smallBufferGets);
                return _bufferPools.SmallBufferPool.Get();
            }
            else if (expectedSize <= 16384)
            {
                Interlocked.Increment(ref _mediumBufferGets);
                return _bufferPools.MediumBufferPool.Get();
            }
            else
            {
                Interlocked.Increment(ref _largeBufferGets);
                return _bufferPools.LargeBufferPool.Get();
            }
        }

        /// <summary>
        /// Gets a small buffer optimized for simple types (primitives, Vector3, etc.).
        /// </summary>
        /// <returns>Small pooled network buffer</returns>
        public PooledNetworkBuffer GetSmallBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _smallBufferGets);
            return _bufferPools.SmallBufferPool.Get();
        }

        /// <summary>
        /// Gets a medium buffer optimized for moderate complexity objects.
        /// </summary>
        /// <returns>Medium pooled network buffer</returns>
        public PooledNetworkBuffer GetMediumBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _mediumBufferGets);
            return _bufferPools.MediumBufferPool.Get();
        }

        /// <summary>
        /// Gets a large buffer optimized for complex objects and collections.
        /// </summary>
        /// <returns>Large pooled network buffer</returns>
        public PooledNetworkBuffer GetLargeBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _largeBufferGets);
            return _bufferPools.LargeBufferPool.Get();
        }

        /// <summary>
        /// Gets a buffer optimized for compression operations.
        /// </summary>
        /// <returns>Compression buffer</returns>
        public PooledNetworkBuffer GetCompressionBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _compressionBufferGets);
            return _bufferPools.CompressionBufferPool.Get();
        }

        /// <summary>
        /// Returns a buffer to the appropriate pool.
        /// </summary>
        /// <param name="buffer">Buffer to return</param>
        public void ReturnBuffer(PooledNetworkBuffer buffer)
        {
            if (buffer == null)
                return;

            ThrowIfDisposed();

            try
            {
                // Return to the appropriate pool based on buffer size
                switch (buffer.Capacity)
                {
                    case 1024:
                        _bufferPools.SmallBufferPool.Return(buffer);
                        break;
                    case 16384:
                        _bufferPools.MediumBufferPool.Return(buffer);
                        break;
                    case 32768:
                        _bufferPools.CompressionBufferPool.Return(buffer);
                        break;
                    case 65536:
                        _bufferPools.LargeBufferPool.Return(buffer);
                        break;
                    default:
                        // Unknown buffer size, dispose it
                        buffer.Dispose();
                        _logger.LogWarning($"Unknown buffer capacity {buffer.Capacity}, disposing instead of returning to pool");
                        return;
                }
                
                Interlocked.Increment(ref _totalBufferReturns);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to return network buffer to pool: {ex.Message}");
                // Dispose the buffer if it can't be returned
                buffer.Dispose();
            }
        }

        /// <summary>
        /// Gets buffer pool statistics for monitoring and diagnostics.
        /// </summary>
        /// <returns>Network buffer pool statistics</returns>
        public NetworkBufferPoolStatistics GetStatistics()
        {
            ThrowIfDisposed();

            return new NetworkBufferPoolStatistics
            {
                SmallBufferGets = _smallBufferGets,
                MediumBufferGets = _mediumBufferGets,
                LargeBufferGets = _largeBufferGets,
                CompressionBufferGets = _compressionBufferGets,
                TotalBufferReturns = _totalBufferReturns,
                TotalBufferGets = _smallBufferGets + _mediumBufferGets + _largeBufferGets + _compressionBufferGets,
                SmallBufferPoolStats = _bufferPools.SmallBufferPool.GetStatistics(),
                MediumBufferPoolStats = _bufferPools.MediumBufferPool.GetStatistics(),
                LargeBufferPoolStats = _bufferPools.LargeBufferPool.GetStatistics(),
                CompressionBufferPoolStats = _bufferPools.CompressionBufferPool.GetStatistics()
            };
        }

        /// <summary>
        /// Trims excess buffers from all pools to reduce memory usage.
        /// </summary>
        public void TrimExcess()
        {
            ThrowIfDisposed();
            _bufferPools.SmallBufferPool.TrimExcess();
            _bufferPools.MediumBufferPool.TrimExcess();
            _bufferPools.LargeBufferPool.TrimExcess();
            _bufferPools.CompressionBufferPool.TrimExcess();
            _logger.LogDebug("Network buffer pools trimmed");
        }

        /// <summary>
        /// Validates all network buffer pools.
        /// </summary>
        /// <returns>True if all pools are valid</returns>
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            var smallValid = _bufferPools.SmallBufferPool.Validate();
            var mediumValid = _bufferPools.MediumBufferPool.Validate();
            var largeValid = _bufferPools.LargeBufferPool.Validate();
            var compressionValid = _bufferPools.CompressionBufferPool.Validate();
            return smallValid && mediumValid && largeValid && compressionValid;
        }

        /// <summary>
        /// Gets the current configuration for this buffer pool service.
        /// </summary>
        /// <returns>Network pooling configuration</returns>
        public NetworkPoolingConfig GetConfiguration()
        {
            ThrowIfDisposed();
            return _configuration;
        }
        
        /// <summary>
        /// Gets the buffer pools managed by this service.
        /// </summary>
        /// <returns>Collection of buffer pools</returns>
        public NetworkBufferPools GetBufferPools()
        {
            ThrowIfDisposed();
            return _bufferPools;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NetworkSerializationBufferPool));
        }

        /// <summary>
        /// Disposes the buffer pool service.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _bufferPools?.SmallBufferPool?.Dispose();
                _bufferPools?.MediumBufferPool?.Dispose();
                _bufferPools?.LargeBufferPool?.Dispose();
                _bufferPools?.CompressionBufferPool?.Dispose();
                _logger.LogInfo("NetworkSerializationBufferPool disposed");
            }
        }
    }

}