using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using AhBearStudios.Pooling.Configurations;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// Thread-safe pool implementation that uses a semaphore to limit concurrent access.
    /// This implementation wraps an existing pool and limits how many items can be checked out simultaneously.
    /// </summary>
    /// <typeparam name="T">The type of object to pool</typeparam>
    public class SemaphorePool<T> : ISemaphorePool<T>, IDisposable
    {
        #region Private Fields

        private readonly IPool<T> _innerPool;
        private readonly SemaphoreSlim _semaphore;
        private readonly object _lockObject = new object();
        private readonly IPoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly IPoolMetrics _metrics;
        private readonly SemaphorePoolConfig _config;
        private readonly DateTime _creationTime;

        private int _activeCount;
        private int _peakActiveCount;
        private float _lastShrinkTime;
        private bool _isDisposed;

        #endregion

        #region IPool<T> Implementation

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        public bool IsCreated => _innerPool.IsCreated && !IsDisposed;

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => _innerPool.TotalCount;

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => _innerPool.InactiveCount;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated => _innerPool.TotalCreated;

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => _innerPool.ItemType;

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed || _innerPool.IsDisposed;

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName => $"Semaphore({_innerPool.PoolName})";

        /// <summary>
        /// Gets the unique identifier for this pool
        /// </summary>
        public Guid Id => _innerPool.Id;

        #endregion

        #region IPoolMetrics Implementation

        /// <summary>
        /// Gets the peak number of active items since the pool was created or last reset
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items created by this pool
        /// </summary>
        public int TotalCreatedCount => _metrics.TotalCreatedCount;

        /// <summary>
        /// Gets the total number of acquire operations
        /// </summary>
        public int TotalAcquiredCount => _metrics.TotalAcquiredCount;

        /// <summary>
        /// Gets the total number of release operations
        /// </summary>
        public int TotalReleasedCount => _metrics.TotalReleasedCount;

        /// <summary>
        /// Gets the current number of active items
        /// </summary>
        public int CurrentActiveCount => _activeCount;

        /// <summary>
        /// Gets the current capacity of the pool
        /// </summary>
        public int CurrentCapacity => _innerPool is IPoolMetrics metricsPool
            ? metricsPool.CurrentCapacity
            : TotalCount;

        #endregion

        #region IShrinkablePool Implementation

        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => _innerPool is IShrinkablePool;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity
        {
            get => _config.MinimumCapacity;
            set => _config.MinimumCapacity = value;
        }

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity
        {
            get => _config.MaximumCapacity;
            set => _config.MaximumCapacity = value;
        }

        /// <summary>
        /// Gets or sets the shrink interval in seconds.
        /// </summary>
        public float ShrinkInterval
        {
            get => _config.ShrinkInterval;
            set => _config.ShrinkInterval = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand.
        /// </summary>
        public float GrowthFactor
        {
            get => _config.GrowthFactor;
            set => _config.GrowthFactor = Mathf.Max(1.1f, value);
        }

        /// <summary>
        /// Gets or sets the shrink threshold.
        /// </summary>
        public float ShrinkThreshold
        {
            get => _config.ShrinkThreshold;
            set => _config.ShrinkThreshold = Mathf.Clamp(value, 0.1f, 0.9f);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SemaphorePool class
        /// </summary>
        /// <param name="innerPool">The underlying pool to wrap with a semaphore</param>
        /// <param name="maxConcurrency">The maximum number of items that can be acquired concurrently</param>
        /// <param name="serviceLocator">Optional service locator for dependency injection</param>
        public SemaphorePool(IPool<T> innerPool, int maxConcurrency, IPoolingServiceLocator serviceLocator = null)
            : this(innerPool, new SemaphorePoolConfig { InitialCount = maxConcurrency }, serviceLocator)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SemaphorePool class with a configuration
        /// </summary>
        /// <param name="innerPool">The underlying pool to wrap with a semaphore</param>
        /// <param name="config">Configuration for the semaphore pool</param>
        /// <param name="serviceLocator">Optional service locator for dependency injection</param>
        public SemaphorePool(IPool<T> innerPool, SemaphorePoolConfig config,
            IPoolingServiceLocator serviceLocator = null)
        {
            _innerPool = innerPool ?? throw new ArgumentNullException(nameof(innerPool));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (config.InitialCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(config), "Initial count must be positive");

            // Initialize service locator
            IPoolingServiceLocator locator = serviceLocator ?? DefaultPoolingServices.Instance;

            // Get services via dependency injection
            _logger = locator.GetService<IPoolLogger>();
            _diagnostics = locator.GetService<IPoolDiagnostics>();
            _metrics = locator.GetService<IPoolMetrics>() ?? 
                       new PoolMetrics(
                           PoolName,                     // pool name
                           typeof(T),                    // item type
                           GetType(),                    // pool type
                           locator,                      // service locator
                           locator.GetService<IPoolProfiler>(), // profiler
                           locator.GetService<IPoolLogger>(),   // logger
                           _config.InitialCount          // initial capacity
                       );

            // Initialize the semaphore
            _semaphore = new SemaphoreSlim(config.InitialCount, config.MaxConcurrentWaits > 0
                ? config.MaxConcurrentWaits
                : int.MaxValue);

            // Initialize timestamps
            _creationTime = DateTime.UtcNow;
            _lastShrinkTime = Time.realtimeSinceStartup;

            // Register with diagnostics
            _diagnostics?.RegisterPool(this, PoolName);

            // Log pool creation
            _logger?.LogInfoInstance(
                $"Created SemaphorePool<{typeof(T).Name}> with max concurrency {config.InitialCount}");
        }

        #endregion

        #region ISemaphorePool<T> Implementation

        /// <summary>
        /// Acquires an item from the pool, blocking if necessary
        /// </summary>
        /// <returns>The acquired item</returns>
        public T Acquire()
        {
            ThrowIfDisposed();

            _diagnostics?.RecordAcquireStart(this);
            _metrics.RecordAcquireAttempt();

            _semaphore.Wait();

            try
            {
                T item = _innerPool.Acquire();

                lock (_lockObject)
                {
                    _activeCount++;
                    UpdatePeakUsage();
                    _metrics.RecordAcquireSuccess();
                }

                _diagnostics?.RecordAcquireComplete(this, _activeCount, item);

                return item;
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                _metrics.RecordAcquireFailure();
                _logger?.LogErrorInstance($"Failed to acquire item from {PoolName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Acquires an item from the pool asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the acquired item</returns>
        public async Task<T> AcquireAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            _diagnostics?.RecordAcquireStart(this);
            _metrics.RecordAcquireAttempt();

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                T item = _innerPool.Acquire();

                lock (_lockObject)
                {
                    _activeCount++;
                    UpdatePeakUsage();
                    _metrics.RecordAcquireSuccess();
                }

                _diagnostics?.RecordAcquireComplete(this, _activeCount, item);

                return item;
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                _metrics.RecordAcquireFailure();
                _logger?.LogErrorInstance($"Failed to acquire item asynchronously from {PoolName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tries to acquire an item without blocking
        /// </summary>
        /// <param name="item">The acquired item if successful</param>
        /// <returns>True if an item was acquired, false otherwise</returns>
        public bool TryAcquire(out T item)
        {
            ThrowIfDisposed();

            _diagnostics?.RecordAcquireStart(this);
            _metrics.RecordAcquireAttempt();

            if (!_semaphore.Wait(0))
            {
                item = default;
                _metrics.RecordAcquireFailure();
                return false;
            }

            try
            {
                item = _innerPool.Acquire();

                lock (_lockObject)
                {
                    _activeCount++;
                    UpdatePeakUsage();
                    _metrics.RecordAcquireSuccess();
                }

                _diagnostics?.RecordAcquireComplete(this, _activeCount, item);

                return true;
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                item = default;
                _metrics.RecordAcquireFailure();
                _logger?.LogErrorInstance($"Failed to acquire item from {PoolName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="item">The item to release</param>
        public void Release(T item)
        {
            if (IsDisposed)
            {
                _logger?.LogWarningInstance($"Attempted to release item to disposed pool {PoolName}");
                return;
            }

            if (item == null)
            {
                _logger?.LogWarningInstance($"Attempted to release null item to {PoolName}");
                return;
            }

            _diagnostics?.RecordReleaseStart(this, item);
            _metrics.RecordReleaseAttempt();

            try
            {
                _innerPool.Release(item);

                lock (_lockObject)
                {
                    _activeCount--;
                    _metrics.RecordReleaseSuccess();
                }

                _semaphore.Release();

                _diagnostics?.RecordReleaseComplete(this, _activeCount);

                // Check if we should auto-shrink
                if (_config.EnableAutoShrink && SupportsAutoShrink)
                {
                    TryAutoShrink();
                }
            }
            catch (Exception ex)
            {
                _metrics.RecordReleaseFailure();
                _logger?.LogErrorInstance($"Failed to release item to {PoolName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Releases multiple items back to the pool
        /// </summary>
        /// <param name="items">The items to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (IsDisposed)
            {
                _logger?.LogWarningInstance($"Attempted to release items to disposed pool {PoolName}");
                return;
            }

            if (items == null)
            {
                _logger?.LogWarningInstance($"Attempted to release null items collection to {PoolName}");
                return;
            }

            int count = 0;
            foreach (var item in items)
            {
                if (item == null) continue;

                try
                {
                    _innerPool.Release(item);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Failed to release item to {PoolName}: {ex.Message}");
                }
            }

            if (count > 0)
            {
                lock (_lockObject)
                {
                    _activeCount -= count;
                    _metrics.RecordMultipleReleaseSuccess(count);
                }

                _semaphore.Release(count);

                // Check if we should auto-shrink
                if (_config.EnableAutoShrink && SupportsAutoShrink)
                {
                    TryAutoShrink();
                }
            }
        }

        /// <summary>
        /// Acquires an item from the pool and performs setup operations on it
        /// </summary>
        /// <param name="setupAction">Action to perform on the item after acquisition</param>
        /// <returns>The acquired and setup item</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        /// <exception cref="ArgumentNullException">Thrown if the setup action is null</exception>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            ThrowIfDisposed();

            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction), "Setup action cannot be null");

            _diagnostics?.RecordAcquireStart(this);
            _metrics.RecordAcquireAttempt();

            _semaphore.Wait();

            try
            {
                // First acquire the item
                T item = _innerPool.Acquire();

                lock (_lockObject)
                {
                    _activeCount++;
                    UpdatePeakUsage();
                    _metrics.RecordAcquireSuccess();
                }

                // Then perform the setup
                try
                {
                    setupAction(item);
                }
                catch (Exception ex)
                {
                    // If setup fails, release the item and propagate the exception
                    _logger?.LogErrorInstance($"Setup action failed for item from {PoolName}: {ex.Message}");

                    try
                    {
                        Release(item);
                    }
                    catch
                    {
                        // Ensure semaphore is released even if Release fails
                        _semaphore.Release();
                        throw;
                    }

                    throw;
                }

                _diagnostics?.RecordAcquireComplete(this, _activeCount, item);

                return item;
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                _metrics.RecordAcquireFailure();
                _logger?.LogErrorInstance($"Failed to acquire and setup item from {PoolName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Acquires multiple items from the pool
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool is disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than or equal to zero</exception>
        public List<T> AcquireMultiple(int count)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");

            var result = new List<T>(count);

            _diagnostics?.RecordAcquireStart(this);
            _metrics.RecordAcquireAttempt();

            try
            {
                // Try to acquire all semaphore slots at once
                _semaphore.Wait(count);

                bool success = false;

                try
                {
                    // Acquire all items
                    for (int i = 0; i < count; i++)
                    {
                        result.Add(_innerPool.Acquire());
                    }

                    // Update metrics after acquiring all items
                    lock (_lockObject)
                    {
                        _activeCount += count;
                        UpdatePeakUsage();
                        _metrics.RecordMultipleAcquireSuccess(count);
                    }

                    success = true;
                }
                finally
                {
                    // If we failed to acquire all items, release the semaphore slots for any we didn't acquire
                    if (!success)
                    {
                        int acquiredCount = result.Count;
                        if (acquiredCount < count)
                        {
                            _semaphore.Release(count - acquiredCount);
                        }

                        // Also release any items we did acquire
                        foreach (var item in result)
                        {
                            try
                            {
                                _innerPool.Release(item);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogErrorInstance(
                                    $"Failed to release item during AcquireMultiple rollback in {PoolName}: {ex.Message}");
                            }
                        }

                        // Clear the result list
                        result.Clear();
                    }
                }

                _diagnostics?.RecordAcquireComplete(this, _activeCount, result.Count > 0 ? result[0] : default);

                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordAcquireFailure();
                _logger?.LogErrorInstance($"Failed to acquire multiple items from {PoolName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the pool, returning it to its initial state
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                // Reset the semaphore
                int currentCount;
                _semaphore.CurrentCount.TryGetValue(out currentCount);

                _semaphore.Dispose();
                _semaphore = new SemaphoreSlim(_config.InitialCount, _config.MaxConcurrentWaits > 0
                    ? _config.MaxConcurrentWaits
                    : int.MaxValue);

                // Clear the inner pool
                _innerPool.Clear();

                // Reset metrics
                _activeCount = 0;
                _lastShrinkTime = Time.realtimeSinceStartup;
                ResetMetrics();

                _logger?.LogInfoInstance($"Cleared {PoolName}");
            }
        }

        /// <summary>
        /// Ensures that the pool has the specified capacity
        /// </summary>
        /// <param name="capacity">The desired capacity</param>
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();

            if (_innerPool is IShrinkablePool shrinkablePool)
            {
                lock (_lockObject)
                {
                    shrinkablePool.EnsureCapacity(capacity);
                }
            }
        }

        /// <summary>
        /// Sets the name of the pool
        /// </summary>
        /// <param name="newName">The new name for the pool</param>
        public void SetPoolName(string newName)
        {
            ThrowIfDisposed();

            if (_innerPool is IPool<T> pool)
            {
                pool.SetPoolName(newName);
            }
        }

        /// <summary>
        /// Gets the metrics for the pool
        /// </summary>
        /// <returns>Dictionary of metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PoolName"] = PoolName,
                ["PoolType"] = GetType().Name,
                ["ItemType"] = ItemType.Name,
                ["TotalCount"] = TotalCount,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["PeakUsage"] = PeakUsage,
                ["TotalCreated"] = TotalCreated,
                ["TotalAcquired"] = TotalAcquiredCount,
                ["TotalReleased"] = TotalReleasedCount,
                ["UpTime"] = (DateTime.UtcNow - _creationTime).TotalSeconds,
                ["MaxConcurrency"] = _config.InitialCount
            };

            // Add inner pool metrics if available
            if (_innerPool is IPoolMetrics innerMetrics)
            {
                var innerMetricsDict = innerMetrics.GetMetricsData()?.ToDictionary();
                if (innerMetricsDict != null)
                {
                    foreach (var kvp in innerMetricsDict)
                    {
                        metrics[$"Inner_{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            return metrics;
        }

        /// <summary>
        /// Gets the pool metrics data
        /// </summary>
        /// <returns>Pool metrics data</returns>
        public PoolMetricsData GetMetricsData()
        {
            return _metrics.GetMetricsData();
        }

        /// <summary>
        /// Gets the approximate memory usage of the pool
        /// </summary>
        /// <returns>Memory usage in bytes</returns>
        public long GetApproximateMemoryUsage()
        {
            // Calculate semaphore overhead
            long semaphoreOverhead = 150; // Approximate size of SemaphoreSlim

            // Get inner pool memory usage
            long innerPoolMemory = _innerPool.GetApproximateMemoryUsage();

            // Return total
            return semaphoreOverhead + innerPoolMemory;
        }

        /// <summary>
        /// Resets the metrics for the pool
        /// </summary>
        public void ResetMetrics()
        {
            _metrics.ResetMetrics(_id);
            _peakActiveCount = _activeCount;

            if (_innerPool is IPoolMetrics innerMetrics)
            {
                innerMetrics.ResetMetrics();
            }
        }

        #endregion

        #region IShrinkablePool Implementation Methods

        /// <summary>
        /// Tries to shrink the pool to the minimum capacity
        /// </summary>
        /// <param name="threshold">The threshold ratio below which to shrink</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();

            if (!SupportsAutoShrink || _innerPool is not IShrinkablePool shrinkablePool)
            {
                return false;
            }

            return shrinkablePool.TryShrink(threshold);
        }

        /// <summary>
        /// Tries to shrink the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();

            if (!SupportsAutoShrink || _innerPool is not IShrinkablePool shrinkablePool)
            {
                return false;
            }

            return shrinkablePool.ShrinkTo(targetCapacity);
        }

        /// <summary>
        /// Sets whether auto-shrink is enabled
        /// </summary>
        /// <param name="enabled">Whether to enable auto-shrink</param>
        public void SetAutoShrink(bool enabled)
        {
            _config.EnableAutoShrink = enabled;

            if (_innerPool is IShrinkablePool shrinkablePool)
            {
                shrinkablePool.SetAutoShrink(enabled);
            }
        }

        /// <summary>
        /// Tries to automatically shrink the pool based on the current usage
        /// </summary>
        public void TryAutoShrink()
        {
            if (!_config.EnableAutoShrink || !SupportsAutoShrink ||
                _innerPool is not IShrinkablePool shrinkablePool)
            {
                return;
            }

            float timeSinceLastShrink = Time.realtimeSinceStartup - _lastShrinkTime;

            if (timeSinceLastShrink >= _config.ShrinkInterval)
            {
                if (shrinkablePool.TryShrink(_config.ShrinkThreshold))
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the pool
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_lockObject)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                // Dispose the semaphore
                _semaphore.Dispose();

                // Dispose the inner pool if it's disposable
                if (_innerPool is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // Unregister from diagnostics
                _diagnostics?.UnregisterPool(this);

                _logger?.LogInfoInstance($"Disposed {PoolName}");
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Updates the peak usage counter
        /// </summary>
        private void UpdatePeakUsage()
        {
            if (_activeCount > _peakActiveCount)
            {
                _peakActiveCount = _activeCount;
            }
        }

        /// <summary>
        /// Throws an exception if the pool is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(PoolName);
            }
        }

        #endregion
    }
}