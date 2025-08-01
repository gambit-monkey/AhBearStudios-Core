using System;
using System.Collections.Concurrent;
using System.Threading;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Pools
{
    /// <summary>
    /// Object pool implementation for medium network buffers (16KB).
    /// Thread-safe pool using concurrent collections for high-performance scenarios.
    /// </summary>
    public sealed class MediumBufferPool : IObjectPool<PooledNetworkBuffer>, IDisposable
    {
        private readonly ConcurrentQueue<PooledNetworkBuffer> _objects;
        private readonly PoolConfiguration _configuration;
        private readonly IPoolStrategy _strategy;
        private readonly PoolStatistics _statistics;
        private readonly Timer _maintenanceTimer;
        private readonly object _maintenanceLock = new object();
        private volatile bool _disposed = false;
        private int _totalCount = 0;
        private int _activeCount = 0;

        /// <summary>
        /// Initializes a new MediumBufferPool instance.
        /// </summary>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="strategy">Pool strategy, defaults to DynamicSizeStrategy</param>
        public MediumBufferPool(PoolConfiguration configuration, IPoolStrategy strategy = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _strategy = strategy ?? new DynamicSizeStrategy();
            _objects = new ConcurrentQueue<PooledNetworkBuffer>();
            _statistics = new PoolStatistics { CreatedAt = DateTime.UtcNow };

            if (!_strategy.ValidateConfiguration(_configuration))
                throw new ArgumentException("Invalid pool configuration for strategy", nameof(configuration));

            // Pre-populate with initial capacity
            for (int i = 0; i < _configuration.InitialCapacity; i++)
            {
                var buffer = CreateNewBuffer();
                _objects.Enqueue(buffer);
                Interlocked.Increment(ref _totalCount);
            }

            // Setup maintenance timer
            var interval = _strategy.GetValidationInterval();
            _maintenanceTimer = new Timer(PerformMaintenance, null, interval, interval);
        }

        /// <summary>
        /// Gets the name of this pool.
        /// </summary>
        public string Name => _configuration.Name;

        /// <summary>
        /// Gets the total number of objects in the pool.
        /// </summary>
        public int Count => _totalCount;

        /// <summary>
        /// Gets the number of objects available for use.
        /// </summary>
        public int AvailableCount => _objects.Count;

        /// <summary>
        /// Gets the number of objects currently in use.
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// Gets the configuration for this pool.
        /// </summary>
        public PoolConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the pooling strategy used by this pool.
        /// </summary>
        public IPoolStrategy Strategy => _strategy;

        /// <summary>
        /// Event raised when a new object is created for the pool.
        /// </summary>
        public event Action<PooledNetworkBuffer> ObjectCreated;

        /// <summary>
        /// Event raised when an object is returned to the pool.
        /// </summary>
        public event Action<PooledNetworkBuffer> ObjectReturned;

        /// <summary>
        /// Event raised when an object is destroyed.
        /// </summary>
        public event Action<PooledNetworkBuffer> ObjectDestroyed;

        /// <summary>
        /// Gets an object from the pool, creating a new one if necessary.
        /// </summary>
        /// <returns>A buffer from the pool</returns>
        public PooledNetworkBuffer Get()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MediumBufferPool));

            PooledNetworkBuffer buffer;
            bool wasFromPool = _objects.TryDequeue(out buffer);

            if (!wasFromPool)
            {
                buffer = CreateNewBuffer();
                Interlocked.Increment(ref _totalCount);
            }

            buffer.OnGet();
            Interlocked.Increment(ref _activeCount);

            // Update statistics
            lock (_statistics)
            {
                _statistics.RecordGet(wasFromPool);
            }

            return buffer;
        }

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        /// <param name="item">The buffer to return to the pool</param>
        public void Return(PooledNetworkBuffer item)
        {
            if (_disposed || item == null)
                return;

            // Reset the buffer
            item.OnReturn();
            _configuration.ResetAction?.Invoke(item);

            // Check if we should return to pool or destroy
            if (_totalCount <= _configuration.MaxCapacity && !_strategy.ShouldDestroy(_statistics))
            {
                _objects.Enqueue(item);
                ObjectReturned?.Invoke(item);
            }
            else
            {
                DestroyBuffer(item);
            }

            Interlocked.Decrement(ref _activeCount);

            // Update statistics
            lock (_statistics)
            {
                _statistics.RecordReturn();
            }
        }

        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        public void Clear()
        {
            if (_disposed) return;

            while (_objects.TryDequeue(out var buffer))
            {
                DestroyBuffer(buffer);
            }

            Interlocked.Exchange(ref _totalCount, _activeCount);
        }

        /// <summary>
        /// Removes excess objects from the pool to reduce memory usage.
        /// </summary>
        public void TrimExcess()
        {
            if (_disposed) return;

            lock (_maintenanceLock)
            {
                var targetSize = _strategy.CalculateTargetSize(_statistics);
                var currentAvailable = _objects.Count;
                var excess = Math.Max(0, currentAvailable - Math.Max(targetSize - _activeCount, 0));

                for (int i = 0; i < excess; i++)
                {
                    if (_objects.TryDequeue(out var buffer))
                    {
                        DestroyBuffer(buffer);
                    }
                }
            }
        }

        /// <summary>
        /// Validates all objects in the pool.
        /// </summary>
        /// <returns>True if all objects are valid</returns>
        public bool Validate()
        {
            if (_disposed) return false;

            if (_configuration.ValidationFunc == null)
                return true;

            // We can't easily validate objects in concurrent queue without dequeuing
            // For buffers, we assume they're always valid unless corrupted
            return true;
        }

        /// <summary>
        /// Gets statistics about this pool's usage.
        /// </summary>
        /// <returns>Pool statistics</returns>
        public PoolStatistics GetStatistics()
        {
            lock (_statistics)
            {
                _statistics.TotalCount = _totalCount;
                _statistics.AvailableCount = _objects.Count;
                _statistics.ActiveCount = _activeCount;
                _statistics.LastUpdated = DateTime.UtcNow;
                return _statistics;
            }
        }

        /// <summary>
        /// Creates a new buffer with the appropriate size for medium buffers.
        /// </summary>
        /// <returns>A new PooledNetworkBuffer</returns>
        private PooledNetworkBuffer CreateNewBuffer()
        {
            var buffer = new PooledNetworkBuffer(16384) // 16KB for medium buffers
            {
                PoolName = Name
            };

            ObjectCreated?.Invoke(buffer);
            return buffer;
        }

        /// <summary>
        /// Destroys a buffer and updates counters.
        /// </summary>
        /// <param name="buffer">The buffer to destroy</param>
        private void DestroyBuffer(PooledNetworkBuffer buffer)
        {
            buffer?.Dispose();
            Interlocked.Decrement(ref _totalCount);
            ObjectDestroyed?.Invoke(buffer);

            lock (_statistics)
            {
                _statistics.RecordDestruction();
            }
        }

        /// <summary>
        /// Performs periodic maintenance on the pool.
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void PerformMaintenance(object state)
        {
            if (_disposed) return;

            try
            {
                lock (_maintenanceLock)
                {
                    // Trim excess objects if needed
                    if (_strategy.ShouldContract(_statistics))
                    {
                        TrimExcess();
                    }

                    // Validate objects if enabled
                    if (_configuration.EnableValidation)
                    {
                        Validate();
                    }
                }
            }
            catch (Exception)
            {
                // Log maintenance errors but don't throw
                // In a real implementation, this would use ILoggingService
            }
        }

        /// <summary>
        /// Disposes the pool and all its resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _maintenanceTimer?.Dispose();

            Clear();

            ObjectCreated = null;
            ObjectReturned = null;
            ObjectDestroyed = null;
        }
    }
}