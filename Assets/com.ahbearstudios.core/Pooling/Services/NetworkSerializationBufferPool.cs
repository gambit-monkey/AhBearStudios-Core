using System;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Specialized buffer pool service for network serialization operations.
    /// Provides optimized buffer management for FishNet + MemoryPack integration.
    /// Acts as a facade over multiple IObjectPool instances for different buffer sizes.
    /// </summary>
    public class NetworkSerializationBufferPool : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly SmallBufferPool _smallBufferPool;
        private readonly MediumBufferPool _mediumBufferPool;
        private readonly LargeBufferPool _largeBufferPool;
        private readonly CompressionBufferPool _compressionBufferPool;
        private bool _disposed;

        // Performance statistics
        private long _smallBufferGets;
        private long _mediumBufferGets;
        private long _largeBufferGets;
        private long _compressionBufferGets;
        private long _totalBufferReturns;

        /// <summary>
        /// Initializes a new NetworkSerializationBufferPool.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="config">Network pooling configuration</param>
        public NetworkSerializationBufferPool(
            ILoggingService logger,
            NetworkPoolingConfig config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var networkConfig = config ?? NetworkPoolingConfig.CreateDefault();
            var strategy = new DynamicSizeStrategy();

            // Create individual buffer pools
            _smallBufferPool = new SmallBufferPool(CreatePoolConfig("SmallBuffer", networkConfig.SmallBufferPoolConfig), strategy);
            _mediumBufferPool = new MediumBufferPool(CreatePoolConfig("MediumBuffer", networkConfig.MediumBufferPoolConfig), strategy);
            _largeBufferPool = new LargeBufferPool(CreatePoolConfig("LargeBuffer", networkConfig.LargeBufferPoolConfig), strategy);
            _compressionBufferPool = new CompressionBufferPool(CreatePoolConfig("CompressionBuffer", networkConfig.CompressionBufferPoolConfig), strategy);

            _logger.LogInfo("NetworkSerializationBufferPool initialized with individual buffer pools", default, nameof(NetworkSerializationBufferPool));
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
                return _smallBufferPool.Get();
            }
            else if (expectedSize <= 16384)
            {
                Interlocked.Increment(ref _mediumBufferGets);
                return _mediumBufferPool.Get();
            }
            else
            {
                Interlocked.Increment(ref _largeBufferGets);
                return _largeBufferPool.Get();
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
            return _smallBufferPool.Get();
        }

        /// <summary>
        /// Gets a medium buffer optimized for moderate complexity objects.
        /// </summary>
        /// <returns>Medium pooled network buffer</returns>
        public PooledNetworkBuffer GetMediumBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _mediumBufferGets);
            return _mediumBufferPool.Get();
        }

        /// <summary>
        /// Gets a large buffer optimized for complex objects and collections.
        /// </summary>
        /// <returns>Large pooled network buffer</returns>
        public PooledNetworkBuffer GetLargeBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _largeBufferGets);
            return _largeBufferPool.Get();
        }

        /// <summary>
        /// Gets a buffer optimized for compression operations.
        /// </summary>
        /// <returns>Compression buffer</returns>
        public PooledNetworkBuffer GetCompressionBuffer()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _compressionBufferGets);
            return _compressionBufferPool.Get();
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
                        _smallBufferPool.Return(buffer);
                        break;
                    case 16384:
                        _mediumBufferPool.Return(buffer);
                        break;
                    case 32768:
                        _compressionBufferPool.Return(buffer);
                        break;
                    case 65536:
                        _largeBufferPool.Return(buffer);
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
                SmallBufferPoolStats = _smallBufferPool.GetStatistics(),
                MediumBufferPoolStats = _mediumBufferPool.GetStatistics(),
                LargeBufferPoolStats = _largeBufferPool.GetStatistics(),
                CompressionBufferPoolStats = _compressionBufferPool.GetStatistics()
            };
        }

        /// <summary>
        /// Trims excess buffers from all pools to reduce memory usage.
        /// </summary>
        public void TrimExcess()
        {
            ThrowIfDisposed();
            _smallBufferPool.TrimExcess();
            _mediumBufferPool.TrimExcess();
            _largeBufferPool.TrimExcess();
            _compressionBufferPool.TrimExcess();
            _logger.LogDebug("Network buffer pools trimmed");
        }

        /// <summary>
        /// Validates all network buffer pools.
        /// </summary>
        /// <returns>True if all pools are valid</returns>
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            var smallValid = _smallBufferPool.Validate();
            var mediumValid = _mediumBufferPool.Validate();
            var largeValid = _largeBufferPool.Validate();
            var compressionValid = _compressionBufferPool.Validate();
            return smallValid && mediumValid && largeValid && compressionValid;
        }

        private PoolConfiguration CreatePoolConfig(string name, PoolConfiguration baseConfig)
        {
            return new PoolConfiguration
            {
                Name = name,
                InitialCapacity = baseConfig?.InitialCapacity ?? 10,
                MaxCapacity = baseConfig?.MaxCapacity ?? 100,
                Factory = baseConfig?.Factory,
                ResetAction = buffer => ((PooledNetworkBuffer)buffer).Reset(),
                ValidationFunc = buffer => buffer != null && ((PooledNetworkBuffer)buffer).IsValid(),
                ValidationInterval = baseConfig?.ValidationInterval ?? TimeSpan.FromMinutes(5),
                MaxIdleTime = baseConfig?.MaxIdleTime ?? TimeSpan.FromMinutes(30),
                EnableValidation = baseConfig?.EnableValidation ?? true,
                EnableStatistics = baseConfig?.EnableStatistics ?? true,
                DisposalPolicy = baseConfig?.DisposalPolicy ?? PoolDisposalPolicy.ReturnToPool
            };
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
                _smallBufferPool?.Dispose();
                _mediumBufferPool?.Dispose();
                _largeBufferPool?.Dispose();
                _compressionBufferPool?.Dispose();
                _logger.LogInfo("NetworkSerializationBufferPool disposed");
            }
        }
    }

    /// <summary>
    /// Statistics for network buffer pool usage.
    /// </summary>
    public class NetworkBufferPoolStatistics
    {
        public long SmallBufferGets { get; init; }
        public long MediumBufferGets { get; init; }
        public long LargeBufferGets { get; init; }
        public long CompressionBufferGets { get; init; }
        public long TotalBufferGets { get; init; }
        public long TotalBufferReturns { get; init; }
        public PoolStatistics SmallBufferPoolStats { get; init; }
        public PoolStatistics MediumBufferPoolStats { get; init; }
        public PoolStatistics LargeBufferPoolStats { get; init; }
        public PoolStatistics CompressionBufferPoolStats { get; init; }

        public double BufferReturnRate => TotalBufferGets > 0 ? (double)TotalBufferReturns / TotalBufferGets : 0.0;
        public double SmallBufferUsageRate => TotalBufferGets > 0 ? (double)SmallBufferGets / TotalBufferGets : 0.0;
        public double MediumBufferUsageRate => TotalBufferGets > 0 ? (double)MediumBufferGets / TotalBufferGets : 0.0;
        public double LargeBufferUsageRate => TotalBufferGets > 0 ? (double)LargeBufferGets / TotalBufferGets : 0.0;
        public double CompressionBufferUsageRate => TotalBufferGets > 0 ? (double)CompressionBufferGets / TotalBufferGets : 0.0;
    }
}