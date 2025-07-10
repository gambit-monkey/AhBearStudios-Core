using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Pooling.Diagnostics;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Async
{
    /// <summary>
    /// Adapter class that wraps a synchronous pool with asynchronous capabilities.
    /// Implements both synchronous and asynchronous pool interfaces for seamless integration.
    /// </summary>
    /// <typeparam name="T">Type of objects in the pool</typeparam>
    public class AsyncPoolAdapter<T> : IAsyncPool<T>, IDisposable where T : class
    {
        private readonly IPool<T> _basePool;
        private readonly Func<CancellationToken, Task<T>> _asyncFactory;
        private readonly bool _ownsPool;
        private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);
        private readonly PoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly PoolLogger _logger;
        private bool _autoShrinkEnabled;
        private float _lastShrinkTime;
        private bool _disposed;

        /// <inheritdoc />
        public int TotalCount => CheckDisposed() ? 0 : _basePool.TotalCount;

        /// <inheritdoc />
        public int ActiveCount => CheckDisposed() ? 0 : _basePool.ActiveCount;

        /// <inheritdoc />
        public int InactiveCount => CheckDisposed() ? 0 : _basePool.InactiveCount;

        /// <inheritdoc />
        public int PeakUsage => CheckDisposed() ? 0 : _basePool.PeakUsage;

        /// <inheritdoc />
        public int TotalCreated => CheckDisposed() ? 0 : _basePool.TotalCreated;

        /// <inheritdoc />
        public bool IsCreated => !_disposed && _basePool != null && _basePool.IsCreated;

        /// <inheritdoc />
        public bool IsDisposed => _disposed;

        /// <inheritdoc />
        public Type ItemType => _basePool?.ItemType ?? typeof(T);

        /// <inheritdoc />
        public string PoolName => _basePool?.PoolName ?? $"AsyncPoolAdapter<{typeof(T).Name}>";

        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadSafe;

        /// <summary>
        /// Gets whether this pool supports asynchronous creation of items
        /// </summary>
        public bool SupportsAsyncCreation => _asyncFactory != null;

        /// <inheritdoc />
        public int PeakActiveCount => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.PeakActiveCount ?? PeakUsage;

        /// <inheritdoc />
        public int TotalCreatedCount => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.TotalCreatedCount ?? TotalCreated;

        /// <inheritdoc />
        public int TotalAcquiredCount => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.TotalAcquiredCount ?? 0;

        /// <inheritdoc />
        public int TotalReleasedCount => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.TotalReleasedCount ?? 0;

        /// <inheritdoc />
        public int CurrentActiveCount => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.CurrentActiveCount ?? ActiveCount;

        /// <inheritdoc />
        public int CurrentCapacity => CheckDisposed() ? 0 : (_basePool as IPoolMetrics)?.CurrentCapacity ?? TotalCount;

        /// <inheritdoc />
        public bool AutoShrinkEnabled => _autoShrinkEnabled;

        /// <inheritdoc />
        public float LastShrinkTime => _lastShrinkTime;

        /// <inheritdoc />
        public bool SupportsAutoShrink => _basePool is IShrinkablePool;

        /// <inheritdoc />
        public int MinimumCapacity
        {
            get => CheckDisposed() ? 0 : (_basePool as IShrinkablePool)?.MinimumCapacity ?? 0;
            set
            {
                ThrowIfDisposed();
                if (_basePool is IShrinkablePool shrinkable)
                {
                    shrinkable.MinimumCapacity = value;
                }
            }
        }

        /// <inheritdoc />
        public int MaximumCapacity
        {
            get => CheckDisposed() ? 0 : (_basePool as IShrinkablePool)?.MaximumCapacity ?? 0;
            set
            {
                ThrowIfDisposed();
                if (_basePool is IShrinkablePool shrinkable)
                {
                    shrinkable.MaximumCapacity = value;
                }
            }
        }

        /// <inheritdoc />
        public float ShrinkInterval
        {
            get => CheckDisposed() ? 0 : (_basePool as IShrinkablePool)?.ShrinkInterval ?? 0;
            set
            {
                ThrowIfDisposed();
                if (_basePool is IShrinkablePool shrinkable)
                {
                    shrinkable.ShrinkInterval = value;
                }
            }
        }

        /// <inheritdoc />
        public float GrowthFactor
        {
            get => CheckDisposed() ? 0 : (_basePool as IShrinkablePool)?.GrowthFactor ?? 0;
            set
            {
                ThrowIfDisposed();
                if (_basePool is IShrinkablePool shrinkable)
                {
                    shrinkable.GrowthFactor = value;
                }
            }
        }

        /// <inheritdoc />
        public float ShrinkThreshold
        {
            get => CheckDisposed() ? 0 : (_basePool as IShrinkablePool)?.ShrinkThreshold ?? 0;
            set
            {
                ThrowIfDisposed();
                if (_basePool is IShrinkablePool shrinkable)
                {
                    shrinkable.ShrinkThreshold = value;
                }
            }
        }

        /// <inheritdoc />
        public long EstimatedMemoryUsageBytes
        {
            get
            {
                if (CheckDisposed())
                    return 0;

                // If the base pool provides metrics, use them, otherwise estimate
                if (_basePool is IPoolMetrics metrics)
                {
                    var result = metrics.GetMetrics();
                    if (result.TryGetValue("EstimatedMemoryUsageBytes", out object memObj) && memObj is long mem)
                    {
                        return mem;
                    }
                }

                // Fallback: rough estimate based on total count
                return TotalCount * 100; // Assuming ~100 bytes per object as a general estimate
            }
        }

        /// <summary>
        /// Creates a new asynchronous pool adapter wrapping the specified base pool
        /// </summary>
        /// <param name="basePool">Underlying synchronous pool to wrap</param>
        /// <param name="ownsPool">Whether this adapter owns and should dispose the base pool</param>
        public AsyncPoolAdapter(IPool<T> basePool, bool ownsPool = false)
        {
            _basePool = basePool ?? throw new ArgumentNullException(nameof(basePool));
            _ownsPool = ownsPool;
            _asyncFactory = null;

            // Get services if available
            _profiler = PoolingServices.TryGetService<PoolProfiler>(out var profiler) ? profiler : null;
            _diagnostics = PoolingServices.TryGetService<IPoolDiagnostics>(out var diagnostics) ? diagnostics : null;
            _logger = PoolingServices.TryGetService<PoolLogger>(out var logger) ? logger : null;

            // Register with diagnostics
            _diagnostics?.RegisterPool(this, $"AsyncAdapter:{basePool.PoolName}");
            
            // Initialize shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            // Copy auto-shrink settings from base pool if applicable
            if (basePool is IShrinkablePool shrinkablePool)
            {
                _autoShrinkEnabled = shrinkablePool.SupportsAutoShrink;
            }
        }

        /// <summary>
        /// Creates a new asynchronous pool adapter with a custom async factory
        /// </summary>
        /// <param name="basePool">Underlying synchronous pool to wrap</param>
        /// <param name="asyncFactory">Custom factory function for asynchronous item creation</param>
        /// <param name="ownsPool">Whether this adapter owns and should dispose the base pool</param>
        public AsyncPoolAdapter(IPool<T> basePool, Func<CancellationToken, Task<T>> asyncFactory = null, bool ownsPool = false)
            : this(basePool, ownsPool)
        {
            _asyncFactory = asyncFactory;
        }

        /// <inheritdoc />
        public T Acquire()
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("Acquire", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                T item = _basePool.Acquire();
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                return item;
            }
            finally
            {
                _profiler?.EndSample("Acquire", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            if (item == null || CheckDisposed())
                return;

            _profiler?.BeginSample("Release", PoolName);

            try
            {
                _basePool.Release(item);
                _diagnostics?.RecordRelease(this, ActiveCount, item);
            }
            finally
            {
                _profiler?.EndSample("Release", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public List<T> AcquireMultiple(int count)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

            _profiler?.BeginSample("AcquireMultiple", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                List<T> items = _basePool.AcquireMultiple(count);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, null);
                return items;
            }
            finally
            {
                _profiler?.EndSample("AcquireMultiple", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null || CheckDisposed())
                return;

            _profiler?.BeginSample("ReleaseMultiple", PoolName);

            try
            {
                _basePool.ReleaseMultiple(items);
                _diagnostics?.RecordRelease(this, ActiveCount);
            }
            finally
            {
                _profiler?.EndSample("ReleaseMultiple", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public T AcquireAndSetup(Action<T> setupAction)
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("AcquireAndSetup", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                T item = _basePool.AcquireAndSetup(setupAction);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                return item;
            }
            finally
            {
                _profiler?.EndSample("AcquireAndSetup", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (CheckDisposed())
                return;

            _profiler?.BeginSample("Clear", PoolName);

            try
            {
                _basePool.Clear();
            }
            finally
            {
                _profiler?.EndSample("Clear", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("EnsureCapacity", PoolName);

            try
            {
                _basePool.EnsureCapacity(capacity);
            }
            finally
            {
                _profiler?.EndSample("EnsureCapacity", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Creates a new item asynchronously, using the async factory if available
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the created item</returns>
        protected virtual async Task<T> CreateItemAsync(CancellationToken cancellationToken)
        {
            if (_asyncFactory != null)
            {
                // Use the provided async factory
                return await _asyncFactory(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Default implementation: wrap the synchronous creation in a task
                return await Task.Run(() => _basePool.Acquire(), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<T> AcquireAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("AcquireAsync", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                // Try to get an item synchronously first for better performance
                if (TryAcquire(out T item))
                {
                    _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                    return item;
                }

                // If no item is available or TryAcquire is not supported, create one asynchronously
                item = await CreateItemAsync(cancellationToken).ConfigureAwait(false);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                return item;
            }
            finally
            {
                _profiler?.EndSample("AcquireAsync", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public async Task PrewarmAsync(int count, int maxParallelism = 4, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

            if (maxParallelism <= 0)
                maxParallelism = Math.Max(1, Environment.ProcessorCount - 1);

            _profiler?.BeginSample("PrewarmAsync", PoolName);

            try
            {
                // Use semaphore to limit parallelism
                using (SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelism))
                {
                    var tasks = new List<Task>(count);

                    for (int i = 0; i < count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                // Create item, then immediately release it
                                T item = await CreateItemAsync(cancellationToken).ConfigureAwait(false);
                                await ReleaseAsync(item).ConfigureAwait(false);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, cancellationToken));
                    }

                    // Wait for all tasks to complete
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            finally
            {
                _profiler?.EndSample("PrewarmAsync", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public Task ReleaseAsync(T item)
        {
            // Most pool implementations can release synchronously without blocking
            Release(item);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<List<T>> AcquireMultipleAsync(int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

            _profiler?.BeginSample("AcquireMultipleAsync", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                // For small counts, try synchronous acquisition first
                if (count <= 3)
                {
                    try
                    {
                        var items = AcquireMultiple(count);
                        _diagnostics?.RecordAcquireComplete(this, ActiveCount, null);
                        return items;
                    }
                    catch
                    {
                        // Fall back to async acquisition if synchronous fails
                    }
                }

                // Acquire items in parallel
                var tasks = new Task<T>[count];
                for (int i = 0; i < count; i++)
                {
                    tasks[i] = AcquireAsync(cancellationToken);
                }

                // Wait for all acquisition tasks to complete
                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Collect results
                var result = new List<T>(count);
                foreach (var task in tasks)
                {
                    result.Add(task.Result);
                }

                _diagnostics?.RecordAcquireComplete(this, ActiveCount, null);
                return result;
            }
            finally
            {
                _profiler?.EndSample("AcquireMultipleAsync", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public async Task ReleaseMultipleAsync(IEnumerable<T> items)
        {
            if (items == null || CheckDisposed())
                return;

            _profiler?.BeginSample("ReleaseMultipleAsync", PoolName);

            try
            {
                // We can process a few items at a time to balance between creating too many tasks
                // and avoiding blocking for too long
                const int batchSize = 8;
                var pendingItems = new List<T>();

                foreach (var item in items)
                {
                    if (item == null)
                        continue;

                    pendingItems.Add(item);

                    if (pendingItems.Count >= batchSize)
                    {
                        await Task.Run(() => _basePool.ReleaseMultiple(pendingItems)).ConfigureAwait(false);
                        _diagnostics?.RecordRelease(this, ActiveCount);
                        pendingItems.Clear();
                    }
                }

                // Release any remaining items
                if (pendingItems.Count > 0)
                {
                    await Task.Run(() => _basePool.ReleaseMultiple(pendingItems)).ConfigureAwait(false);
                    _diagnostics?.RecordRelease(this, ActiveCount);
                }
            }
            finally
            {
                _profiler?.EndSample("ReleaseMultipleAsync", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public async Task<T> AcquireAndSetupAsync(Action<T> setupAction, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("AcquireAndSetupAsync", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                T item = await AcquireAsync(cancellationToken).ConfigureAwait(false);
                setupAction?.Invoke(item);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                return item;
            }
            finally
            {
                _profiler?.EndSample("AcquireAndSetupAsync", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public Task EnsureCapacityAsync(int capacity, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return Task.Run(() => EnsureCapacity(capacity), cancellationToken);
        }

        /// <inheritdoc />
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            if (CheckDisposed())
                return Task.CompletedTask;

            return Task.Run(() => Clear(), cancellationToken);
        }

        /// <inheritdoc />
        public bool TryAcquire(out T item)
        {
            ThrowIfDisposed();

            _profiler?.BeginSample("TryAcquire", PoolName);
            _diagnostics?.RecordAcquireStart(this);

            try
            {
                // Check if the base pool supports TryAcquire directly
                if (_basePool is IHasTryAcquire<T> tryAcquirePool)
                {
                    bool result = tryAcquirePool.TryAcquire(out item);
                    if (result)
                    {
                        _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                    }
                    return result;
                }

                // Fallback for pools that don't support TryAcquire:
                // Check if there are inactive items
                if (InactiveCount > 0)
                {
                    item = _basePool.Acquire();
                    _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                    return true;
                }

                item = null;
                return false;
            }
            finally
            {
                _profiler?.EndSample("TryAcquire", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public async Task<TResult> UseItemAsync<TResult>(Func<T, Task<TResult>> func, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (func == null)
                throw new ArgumentNullException(nameof(func));

            T item = await AcquireAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await func(item).ConfigureAwait(false);
            }
            finally
            {
                await ReleaseAsync(item).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UseItemAsync(Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            T item = await AcquireAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await action(item).ConfigureAwait(false);
            }
            finally
            {
                await ReleaseAsync(item).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetrics()
        {
            if (CheckDisposed())
                return new Dictionary<string, object>();

            // Start with metrics from the base pool if available
            Dictionary<string, object> metrics;
            if (_basePool is IPoolMetrics baseMetrics)
            {
                metrics = baseMetrics.GetMetrics();
            }
            else
            {
                metrics = new Dictionary<string, object>
                {
                    ["TotalCount"] = TotalCount,
                    ["ActiveCount"] = ActiveCount,
                    ["InactiveCount"] = InactiveCount,
                    ["PeakUsage"] = PeakUsage,
                    ["TotalCreated"] = TotalCreated
                };
            }

            // Add adapter-specific metrics
            metrics["PoolName"] = PoolName;
            metrics["IsAsyncAdapter"] = true;
            metrics["SupportsAsyncCreation"] = SupportsAsyncCreation;
            metrics["HasCustomAsyncFactory"] = _asyncFactory != null;
            metrics["AutoShrinkEnabled"] = _autoShrinkEnabled;
            metrics["LastShrinkTime"] = _lastShrinkTime;
            metrics["EstimatedMemoryUsageBytes"] = EstimatedMemoryUsageBytes;

            return metrics;
        }

        /// <inheritdoc />
        public void ResetMetrics()
        {
            if (CheckDisposed())
                return;

            // Forward to base pool if it supports metrics
            if (_basePool is IPoolMetrics baseMetrics)
            {
                baseMetrics.ResetMetrics();
            }
        }

        /// <inheritdoc />
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();

            // Forward to base pool if it supports shrinking
            if (_basePool is IShrinkablePool shrinkablePool)
            {
                bool result = shrinkablePool.TryShrink(threshold);
                if (result)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                }
                return result;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> TryShrinkAsync(float threshold, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Ensure only one shrink operation runs at a time
            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await Task.Run(() => TryShrink(threshold), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        /// <inheritdoc />
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();

            // Forward to base pool if it supports shrinking
            if (_basePool is IShrinkablePool shrinkablePool)
            {
                bool result = shrinkablePool.ShrinkTo(targetCapacity);
                if (result)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                }
                return result;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> ShrinkToAsync(int targetCapacity, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Ensure only one shrink operation runs at a time
            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await Task.Run(() => ShrinkTo(targetCapacity), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        /// <inheritdoc />
        public void SetAutoShrink(bool enabled)
        {
            ThrowIfDisposed();

            _autoShrinkEnabled = enabled;

            // Forward to base pool if it supports shrinking
            if (_basePool is IShrinkablePool shrinkablePool)
            {
                shrinkablePool.SetAutoShrink(enabled);
            }
        }

        /// <inheritdoc />
        public async Task SetAutoShrinkAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            await Task.Run(() => SetAutoShrink(enabled), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks if this instance has been disposed
        /// </summary>
        /// <returns>True if this instance has been disposed</returns>
        protected bool CheckDisposed()
        {
            return _disposed;
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed</exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncPoolAdapter<T>));
            }
        }

        /// <summary>
        /// Disposes resources used by this instance
        /// </summary>
        /// <param name="disposing">Whether this call is from Dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _asyncLock.Dispose();

                    if (_ownsPool && _basePool is IDisposable disposablePool)
                    {
                        disposablePool.Dispose();
                    }

                    // Unregister from diagnostics
                    _diagnostics?.UnregisterPool(this);
                }

                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Interface for pools that support try-acquire operations without throwing exceptions
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public interface IHasTryAcquire<T>
    {
        /// <summary>
        /// Attempts to acquire an item from the pool without waiting if none are available
        /// </summary>
        /// <param name="item">The acquired item if successful</param>
        /// <returns>True if an item was acquired, false otherwise</returns>
        bool TryAcquire(out T item);
    }
}