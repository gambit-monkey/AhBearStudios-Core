using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Pooling.Pools.Async
{
    /// <summary>
    /// Implements an asynchronous object pool that supports pooling of objects 
    /// with asynchronous initialization and management.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public class AsyncObjectPool<T> : IAsyncPool<T>, IDisposable
    {
        private readonly Func<Task<T>> _asyncFactory;
        private readonly Action<T> _resetAction;
        private readonly bool _isDisposable;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        private readonly Queue<T> _inactive = new Queue<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        
        private bool _isDisposed;
        private DateTime _lastShrinkTime;
        private Timer _autoShrinkTimer;
        
        /// <summary>
        /// Gets the total number of items in the pool (active + inactive).
        /// </summary>
        public int TotalCount => CurrentActiveCount + _inactive.Count;
        
        /// <summary>
        /// Gets the number of active items currently in use.
        /// </summary>
        public int ActiveCount => CurrentActiveCount;
        
        /// <summary>
        /// Gets the number of inactive items available in the pool.
        /// </summary>
        public int InactiveCount => _inactive.Count;
        
        /// <summary>
        /// Gets the historical peak usage of the pool.
        /// </summary>
        public int PeakUsage => PeakActiveCount;
        
        /// <summary>
        /// Gets the total number of items ever created by this pool.
        /// </summary>
        public int TotalCreated => TotalCreatedCount;
        
        /// <summary>
        /// Gets a value indicating whether the pool is created and ready for use.
        /// </summary>
        public bool IsCreated { get; private set; }
        
        /// <summary>
        /// Gets a value indicating whether the pool has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Gets the type of items managed by this pool.
        /// </summary>
        public Type ItemType => typeof(T);
        
        /// <summary>
        /// Gets the name of this pool, useful for diagnostics.
        /// </summary>
        public string PoolName { get; }
        
        /// <summary>
        /// Gets the peak number of active items ever recorded.
        /// </summary>
        public int PeakActiveCount { get; private set; }
        
        /// <summary>
        /// Gets the total number of items created by this pool.
        /// </summary>
        public int TotalCreatedCount { get; private set; }
        
        /// <summary>
        /// Gets the total number of items acquired from this pool.
        /// </summary>
        public int TotalAcquiredCount { get; private set; }
        
        /// <summary>
        /// Gets the total number of items released back to this pool.
        /// </summary>
        public int TotalReleasedCount { get; private set; }
        
        /// <summary>
        /// Gets the current number of active items.
        /// </summary>
        public int CurrentActiveCount => _active.Count;
        
        /// <summary>
        /// Gets the current capacity of the pool (active + inactive items).
        /// </summary>
        public int CurrentCapacity => TotalCount;
        
        /// <summary>
        /// Gets a value indicating whether the pool supports automatic shrinking.
        /// </summary>
        public bool SupportsAutoShrink => true;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool will maintain even when shrinking.
        /// </summary>
        public int MinimumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the maximum capacity the pool will grow to.
        /// </summary>
        public int MaximumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds between automatic shrink operations.
        /// </summary>
        public float ShrinkInterval { get; set; }

        /// <summary>
        /// Gets or sets the growth factor used when expanding the pool.
        /// </summary>
        public float GrowthFactor { get; set; }

        /// <summary>
        /// Gets or sets the threshold ratio below which automatic shrinking occurs.
        /// </summary>
        public float ShrinkThreshold { get; set; }
        
        /// <summary>
        /// Gets whether automatic shrinking is currently enabled.
        /// </summary>
        public bool AutoShrinkEnabled { get; private set; }
        
        /// <summary>
        /// Gets the estimated memory usage of the pool in bytes.
        /// </summary>
        public long EstimatedMemoryUsageBytes => TotalCount * GetEstimatedItemSize();
        
        /// <summary>
        /// Gets the timestamp of the last shrink operation.
        /// </summary>
        public float LastShrinkTime => (float)(DateTime.UtcNow - _lastShrinkTime).TotalSeconds;

        /// <summary>
        /// Creates a new instance of the AsyncObjectPool class.
        /// </summary>
        /// <param name="asyncFactory">Async factory method to create new pool items</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size the pool can grow to (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <param name="prewarm">Whether to prewarm the pool with initialCapacity items</param>
        public AsyncObjectPool(
            Func<Task<T>> asyncFactory,
            int initialCapacity = 5,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null,
            bool prewarm = true)
        {
            _asyncFactory = asyncFactory ?? throw new ArgumentNullException(nameof(asyncFactory));
            _resetAction = resetAction;
            _isDisposable = typeof(IDisposable).IsAssignableFrom(typeof(T));
            
            MinimumCapacity = Math.Max(0, initialCapacity);
            MaximumCapacity = maxSize <= 0 ? int.MaxValue : maxSize;
            PoolName = string.IsNullOrEmpty(poolName) ? $"AsyncPool<{typeof(T).Name}>" : poolName;
            
            GrowthFactor = 2.0f;
            ShrinkThreshold = 0.3f;
            ShrinkInterval = 60.0f;
            
            IsCreated = true;
            _lastShrinkTime = DateTime.UtcNow;
            
            if (prewarm && initialCapacity > 0)
            {
                _ = PrewarmAsync(initialCapacity);
            }
        }

        /// <summary>
        /// Asynchronously prewarms the pool by creating the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to create</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task PrewarmAsync(int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (count <= 0)
                return;
                
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    T item = await CreateNewAsync(cancellationToken).ConfigureAwait(false);
                    _inactive.Enqueue(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Creates a new item asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the created item</returns>
        public async Task<T> CreateNewAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            
            T item = await _asyncFactory().ConfigureAwait(false);
            TotalCreatedCount++;
            InitializeItem(item);
            return item;
        }

        /// <summary>
        /// Initializes a newly created item.
        /// </summary>
        /// <param name="item">The item to initialize</param>
        public void InitializeItem(T item)
        {
            // Any initialization logic can be placed here
        }

        /// <summary>
        /// Acquires an item from the pool synchronously.
        /// </summary>
        /// <returns>An item from the pool</returns>
        public T Acquire()
        {
            ThrowIfDisposed();
            
            if (!TryAcquire(out T item))
            {
                // Create synchronously by blocking on the async operation
                // Note: This is not ideal, but necessary for the synchronous API
                item = CreateNewAsync(CancellationToken.None).GetAwaiter().GetResult();
                _active.Add(item);
            }
            
            UpdateMetricsOnAcquire();
            return item;
        }

        /// <summary>
        /// Asynchronously acquires an item from the pool.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the acquired item</returns>
        public async Task<T> AcquireAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            T item;
            
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_inactive.Count > 0)
                {
                    item = _inactive.Dequeue();
                }
                else
                {
                    // Create a new item if none available
                    if (TotalCount >= MaximumCapacity)
                    {
                        throw new InvalidOperationException($"Pool '{PoolName}' has reached its maximum capacity of {MaximumCapacity}");
                    }
                    
                    item = await CreateNewAsync(cancellationToken).ConfigureAwait(false);
                }
                
                _active.Add(item);
                UpdateMetricsOnAcquire();
            }
            finally
            {
                _semaphore.Release();
            }
            
            return item;
        }

        /// <summary>
        /// Attempts to acquire an item from the pool without waiting if none are available.
        /// </summary>
        /// <param name="item">The acquired item if successful</param>
        /// <returns>True if an item was acquired, false otherwise</returns>
        public bool TryAcquire(out T item)
        {
            ThrowIfDisposed();
            
            item = default;
            bool success = false;
            
            _semaphore.Wait();
            try
            {
                if (_inactive.Count > 0)
                {
                    item = _inactive.Dequeue();
                    _active.Add(item);
                    success = true;
                    UpdateMetricsOnAcquire();
                }
            }
            finally
            {
                _semaphore.Release();
            }
            
            return success;
        }

        /// <summary>
        /// Releases an item back to the pool.
        /// </summary>
        /// <param name="item">Item to release</param>
        public void Release(T item)
        {
            ThrowIfDisposed();
            
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            _semaphore.Wait();
            try
            {
                if (!_active.Remove(item))
                {
                    throw new InvalidOperationException($"Cannot release an item that was not acquired from this pool: {item}");
                }
                
                ResetItem(item);
                _inactive.Enqueue(item);
                TotalReleasedCount++;
                
                CheckAutoShrink();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously releases an item back to the pool.
        /// </summary>
        /// <param name="item">Item to release</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ReleaseAsync(T item)
        {
            ThrowIfDisposed();
            
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_active.Remove(item))
                {
                    throw new InvalidOperationException($"Cannot release an item that was not acquired from this pool: {item}");
                }
                
                ResetItem(item);
                _inactive.Enqueue(item);
                TotalReleasedCount++;
                
                CheckAutoShrink();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Acquires multiple items from the pool at once.
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        public List<T> AcquireMultiple(int count)
        {
            ThrowIfDisposed();
            
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            var result = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                result.Add(Acquire());
            }
            
            return result;
        }

        /// <summary>
        /// Asynchronously acquires multiple items from the pool at once.
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with a list of acquired items</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        public async Task<List<T>> AcquireMultipleAsync(int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            var result = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                result.Add(await AcquireAsync(cancellationToken).ConfigureAwait(false));
                
                if (cancellationToken.IsCancellationRequested)
                    break;
            }
            
            return result;
        }

        /// <summary>
        /// Releases multiple items back to the pool at once.
        /// </summary>
        /// <param name="items">Items to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            ThrowIfDisposed();
            
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                Release(item);
            }
        }

        /// <summary>
        /// Asynchronously releases multiple items back to the pool at once.
        /// </summary>
        /// <param name="items">Items to release</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ReleaseMultipleAsync(IEnumerable<T> items)
        {
            ThrowIfDisposed();
            
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                await ReleaseAsync(item).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Acquires an item and initializes it with a setup action.
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            ThrowIfDisposed();
            
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
                
            T item = Acquire();
            setupAction(item);
            return item;
        }

        /// <summary>
        /// Asynchronously acquires an item and initializes it with a setup action.
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the acquired and initialized item</returns>
        public async Task<T> AcquireAndSetupAsync(Action<T> setupAction, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
                
            T item = await AcquireAsync(cancellationToken).ConfigureAwait(false);
            setupAction(item);
            return item;
        }

        /// <summary>
        /// Asynchronously acquires an item, uses it, and releases it back to the pool.
        /// </summary>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="func">Function that uses the item and returns a result</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with the result</returns>
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

        /// <summary>
        /// Asynchronously acquires an item, uses it, and releases it back to the pool.
        /// </summary>
        /// <param name="action">Action that uses the item</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation</returns>
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

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            
            _semaphore.Wait();
            try
            {
                // Release all active items back to inactive
                foreach (var item in _active.ToArray())
                {
                    _active.Remove(item);
                    ResetItem(item);
                    _inactive.Enqueue(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously clears the pool, returning all active items to the inactive state.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Release all active items back to inactive
                foreach (var item in _active.ToArray())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    _active.Remove(item);
                    ResetItem(item);
                    _inactive.Enqueue(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();
            
            if (capacity <= TotalCount)
                return;
                
            // Create items synchronously
            _semaphore.Wait();
            try
            {
                int itemsToCreate = Math.Min(capacity - TotalCount, MaximumCapacity - TotalCount);
                
                for (int i = 0; i < itemsToCreate; i++)
                {
                    var item = CreateNewAsync(CancellationToken.None).GetAwaiter().GetResult();
                    _inactive.Enqueue(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously ensures the pool has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task EnsureCapacityAsync(int capacity, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (capacity <= TotalCount)
                return;
                
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                int itemsToCreate = Math.Min(capacity - TotalCount, MaximumCapacity - TotalCount);
                
                for (int i = 0; i < itemsToCreate; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var item = await CreateNewAsync(cancellationToken).ConfigureAwait(false);
                    _inactive.Enqueue(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets metrics about the pool's usage.
        /// </summary>
        /// <returns>Dictionary of metrics with string keys and object values</returns>
        public Dictionary<string, object> GetMetrics()
        {
            ThrowIfDisposed();
            
            return new Dictionary<string, object>
            {
                ["PoolName"] = PoolName,
                ["ItemType"] = ItemType.Name,
                ["TotalCreated"] = TotalCreatedCount,
                ["TotalAcquired"] = TotalAcquiredCount,
                ["TotalReleased"] = TotalReleasedCount,
                ["CurrentActive"] = CurrentActiveCount,
                ["CurrentInactive"] = InactiveCount,
                ["PeakActive"] = PeakActiveCount,
                ["MaxCapacity"] = MaximumCapacity,
                ["MinCapacity"] = MinimumCapacity,
                ["AutoShrinkEnabled"] = AutoShrinkEnabled,
                ["LastShrinkTime"] = LastShrinkTime,
                ["EstimatedMemoryUsage"] = EstimatedMemoryUsageBytes
            };
        }

        /// <summary>
        /// Resets metrics to their initial values.
        /// </summary>
        public void ResetMetrics()
        {
            ThrowIfDisposed();
            
            _semaphore.Wait();
            try
            {
                PeakActiveCount = CurrentActiveCount;
                TotalAcquiredCount = CurrentActiveCount;
                TotalReleasedCount = 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sets whether automatic shrinking is enabled.
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            ThrowIfDisposed();
            
            _semaphore.Wait();
            try
            {
                AutoShrinkEnabled = enabled;
                
                if (enabled)
                {
                    // Start the auto-shrink timer if not already started
                    if (_autoShrinkTimer == null)
                    {
                        _autoShrinkTimer = new Timer(_ => CheckAutoShrink(), null, 
                            TimeSpan.FromSeconds(ShrinkInterval), 
                            TimeSpan.FromSeconds(ShrinkInterval));
                    }
                }
                else
                {
                    // Stop the auto-shrink timer
                    _autoShrinkTimer?.Dispose();
                    _autoShrinkTimer = null;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously sets whether automatic shrinking is enabled.
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation</returns>
        public async Task SetAutoShrinkAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                AutoShrinkEnabled = enabled;
                
                if (enabled)
                {
                    // Start the auto-shrink timer if not already started
                    if (_autoShrinkTimer == null)
                    {
                        _autoShrinkTimer = new Timer(_ => CheckAutoShrink(), null, 
                            TimeSpan.FromSeconds(ShrinkInterval), 
                            TimeSpan.FromSeconds(ShrinkInterval));
                    }
                }
                else
                {
                    // Stop the auto-shrink timer
                    _autoShrinkTimer?.Dispose();
                    _autoShrinkTimer = null;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage.
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();
            
            if (threshold < 0 || threshold > 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
                
            bool didShrink = false;
            
            _semaphore.Wait();
            try
            {
                // Calculate target capacity based on threshold
                float usage = (float)CurrentActiveCount / TotalCount;
                
                if (usage <= threshold && TotalCount > MinimumCapacity)
                {
                    // Calculate new capacity
                    int targetCapacity = Math.Max(MinimumCapacity, CurrentActiveCount * 2);
                    didShrink = ShrinkTo(targetCapacity);
                }
            }
            finally
            {
                _semaphore.Release();
            }
            
            return didShrink;
        }

        /// <summary>
        /// Asynchronously attempts to shrink the pool's capacity to reduce memory usage.
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with a boolean indicating if the pool was shrunk</returns>
        public async Task<bool> TryShrinkAsync(float threshold, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (threshold < 0 || threshold > 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
                
            bool didShrink;
            
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Calculate target capacity based on threshold
                float usage = (float)CurrentActiveCount / TotalCount;
                
                if (usage <= threshold && TotalCount > MinimumCapacity)
                {
                    // Calculate new capacity
                    int targetCapacity = Math.Max(MinimumCapacity, CurrentActiveCount * 2);
                    didShrink = await ShrinkToAsync(targetCapacity, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    didShrink = false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
            
            return didShrink;
        }

        /// <summary>
        /// Shrinks the pool to the specified capacity.
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();
            
            if (targetCapacity < MinimumCapacity)
                targetCapacity = MinimumCapacity;
                
            bool didShrink = false;
            
            _semaphore.Wait();
            try
            {
                if (TotalCount <= targetCapacity)
                    return false;
                    
                // Calculate how many items to remove
                int toRemove = TotalCount - targetCapacity;
                
                // We can only remove from the inactive pool
                toRemove = Math.Min(toRemove, _inactive.Count);
                
                for (int i = 0; i < toRemove; i++)
                {
                    var item = _inactive.Dequeue();
                    DisposeItem(item);
                }
                
                _lastShrinkTime = DateTime.UtcNow;
                didShrink = toRemove > 0;
            }
            finally
            {
                _semaphore.Release();
            }
            
            return didShrink;
        }
                /// <summary>
        /// Asynchronously shrinks the pool to the specified capacity.
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with a boolean indicating if the pool was shrunk</returns>
        public async Task<bool> ShrinkToAsync(int targetCapacity, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (targetCapacity < MinimumCapacity)
                targetCapacity = MinimumCapacity;
                
            bool didShrink;
            
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (TotalCount <= targetCapacity)
                    return false;
                    
                // Calculate how many items to remove
                int toRemove = TotalCount - targetCapacity;
                
                // We can only remove from the inactive pool
                toRemove = Math.Min(toRemove, _inactive.Count);
                
                for (int i = 0; i < toRemove; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var item = _inactive.Dequeue();
                    DisposeItem(item);
                }
                
                _lastShrinkTime = DateTime.UtcNow;
                didShrink = toRemove > 0;
            }
            finally
            {
                _semaphore.Release();
            }
            
            return didShrink;
        }

        /// <summary>
        /// Throws an exception if the pool has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(PoolName ?? GetType().Name);
        }

        /// <summary>
        /// Disposes the pool and optionally disposes all contained objects.
        /// </summary>
        /// <param name="disposeObjects">Whether to dispose the pool items</param>
        protected void Dispose(bool disposeObjects)
        {
            if (_isDisposed)
                return;
                
            _autoShrinkTimer?.Dispose();
            _autoShrinkTimer = null;
            
            if (disposeObjects && _isDisposable)
            {
                // Dispose all pooled items
                foreach (var item in _active.Concat(_inactive))
                {
                    DisposeItem(item);
                }
            }
            
            _active.Clear();
            _inactive.Clear();
            
            _semaphore.Dispose();
            _isDisposed = true;
            IsCreated = false;
        }

        /// <summary>
        /// Disposes an item if it implements IDisposable.
        /// </summary>
        /// <param name="item">The item to dispose</param>
        protected void DisposeItem(T item)
        {
            if (_isDisposable && item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Resets an item before returning it to the inactive pool.
        /// </summary>
        /// <param name="item">The item to reset</param>
        protected void ResetItem(T item)
        {
            _resetAction?.Invoke(item);
        }

        /// <summary>
        /// Updates the pool metrics when an item is acquired.
        /// </summary>
        protected void UpdateMetricsOnAcquire()
        {
            TotalAcquiredCount++;
            PeakActiveCount = Math.Max(PeakActiveCount, CurrentActiveCount);
        }

        /// <summary>
        /// Checks if automatic shrinking should occur.
        /// </summary>
        protected void CheckAutoShrink()
        {
            if (!AutoShrinkEnabled)
                return;
                
            // Check if enough time has elapsed since last shrink
            float timeSinceLastShrink = LastShrinkTime;
            
            if (timeSinceLastShrink >= ShrinkInterval)
            {
                // Schedule the shrink operation to avoid blocking the current thread
                Task.Run(ShrinkPoolAsync);
            }
        }

        /// <summary>
        /// Performs an automatic shrink operation if conditions are met.
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        protected async Task ShrinkPoolAsync()
        {
            if (!AutoShrinkEnabled)
                return;
                
            await TryShrinkAsync(ShrinkThreshold).ConfigureAwait(false);
        }

        /// <summary>
        /// Estimates the memory size of a pool item. Can be overridden for more accurate estimates.
        /// </summary>
        /// <returns>Estimated size in bytes</returns>
        protected virtual long GetEstimatedItemSize()
        {
            // Default conservative estimate - can be overridden for more accurate calculations
            return 128;
        }

        /// <summary>
        /// Disposes the pool and all contained objects.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}