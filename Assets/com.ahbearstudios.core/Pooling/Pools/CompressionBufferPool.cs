using System;
using System.Collections.Concurrent;
using System.Threading;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Pools
{
    /// <summary>
    /// Object pool implementation for compression network buffers (32KB).
    /// Thread-safe pool using concurrent collections for high-performance scenarios.
    /// </summary>
    public sealed class CompressionBufferPool : IObjectPool<PooledNetworkBuffer>, IDisposable
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
        /// Initializes a new CompressionBufferPool instance.
        /// </summary>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="strategy">Pool strategy, defaults to DynamicSizeStrategy</param>
        public CompressionBufferPool(PoolConfiguration configuration, IPoolStrategy strategy = null)
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

        public string Name => _configuration.Name;
        public int Count => _totalCount;
        public int AvailableCount => _objects.Count;
        public int ActiveCount => _activeCount;
        public PoolConfiguration Configuration => _configuration;
        public IPoolStrategy Strategy => _strategy;

        public event Action<PooledNetworkBuffer> ObjectCreated;
        public event Action<PooledNetworkBuffer> ObjectReturned;
        public event Action<PooledNetworkBuffer> ObjectDestroyed;

        public PooledNetworkBuffer Get()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CompressionBufferPool));

            PooledNetworkBuffer buffer;
            bool wasFromPool = _objects.TryDequeue(out buffer);

            if (!wasFromPool)
            {
                buffer = CreateNewBuffer();
                Interlocked.Increment(ref _totalCount);
            }

            buffer.OnGet();
            Interlocked.Increment(ref _activeCount);

            lock (_statistics)
            {
                _statistics.RecordGet(wasFromPool);
            }

            return buffer;
        }

        public void Return(PooledNetworkBuffer item)
        {
            if (_disposed || item == null)
                return;

            item.OnReturn();
            _configuration.ResetAction?.Invoke(item);

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

            lock (_statistics)
            {
                _statistics.RecordReturn();
            }
        }

        public void Clear()
        {
            if (_disposed) return;

            while (_objects.TryDequeue(out var buffer))
            {
                DestroyBuffer(buffer);
            }

            Interlocked.Exchange(ref _totalCount, _activeCount);
        }

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

        public bool Validate()
        {
            if (_disposed) return false;
            return _configuration.ValidationFunc == null || true;
        }

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

        private PooledNetworkBuffer CreateNewBuffer()
        {
            var buffer = new PooledNetworkBuffer(32768) // 32KB for compression buffers
            {
                PoolName = Name
            };

            ObjectCreated?.Invoke(buffer);
            return buffer;
        }

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

        private void PerformMaintenance(object state)
        {
            if (_disposed) return;

            try
            {
                lock (_maintenanceLock)
                {
                    if (_strategy.ShouldContract(_statistics))
                    {
                        TrimExcess();
                    }

                    if (_configuration.EnableValidation)
                    {
                        Validate();
                    }
                }
            }
            catch (Exception)
            {
                // Log maintenance errors but don't throw
            }
        }

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