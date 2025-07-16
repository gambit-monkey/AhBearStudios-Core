using System.Collections.Generic;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Pooling.Pools
{
    /// <summary>
    /// Manages pooled storage for managed log data (exceptions, properties, scopes)
    /// that cannot be stored in native collections but need to be associated with log entries.
    /// </summary>
    public sealed class ManagedLogDataPool : IDisposable
    {
        private readonly IPoolingService _poolingService;
        private readonly IMessageBusService _messageBus;
        private readonly Dictionary<Guid, ManagedLogData> _dataStore;
        private readonly object _storeLock = new object();
        private volatile bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ManagedLogDataPool.
        /// </summary>
        /// <param name="poolingService">The pooling service for managing object lifecycle</param>
        /// <param name="messageBus">The message bus service for notifications</param>
        public ManagedLogDataPool(IPoolingService poolingService, IMessageBusService messageBus)
        {
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _dataStore = new Dictionary<Guid, ManagedLogData>();

            RegisterPools();
        }

        /// <summary>
        /// Stores managed data for a log entry and returns the storage ID.
        /// </summary>
        /// <param name="exception">The exception to store</param>
        /// <param name="properties">The properties to store</param>
        /// <param name="scope">The scope to store</param>
        /// <returns>The unique ID for retrieving the stored data</returns>
        public Guid StoreData(Exception exception, IReadOnlyDictionary<string, object> properties, object scope)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ManagedLogDataPool));

            var id = Guid.NewGuid();
            var data = _poolingService.Get<ManagedLogData>();
            
            data.Exception = exception;
            data.Properties = properties;
            data.Scope = scope;
            data.StorageId = id;
            data.CreatedAt = DateTime.UtcNow;

            lock (_storeLock)
            {
                _dataStore[id] = data;
            }

            return id;
        }

        /// <summary>
        /// Retrieves managed data by storage ID.
        /// </summary>
        /// <param name="storageId">The storage ID</param>
        /// <returns>The managed data, or null if not found</returns>
        public ManagedLogData GetData(Guid storageId)
        {
            if (_disposed) return null;

            lock (_storeLock)
            {
                return _dataStore.TryGetValue(storageId, out var data) ? data : null;
            }
        }

        /// <summary>
        /// Releases managed data by storage ID and returns it to the pool.
        /// </summary>
        /// <param name="storageId">The storage ID</param>
        /// <returns>True if data was found and released</returns>
        public bool ReleaseData(Guid storageId)
        {
            if (_disposed) return false;

            ManagedLogData data;
            lock (_storeLock)
            {
                if (!_dataStore.TryGetValue(storageId, out data))
                    return false;

                _dataStore.Remove(storageId);
            }

            // Reset the data before returning to pool
            data.Reset();
            _poolingService.Return(data);

            return true;
        }

        /// <summary>
        /// Gets the current number of stored data items.
        /// </summary>
        public int StoredCount
        {
            get
            {
                lock (_storeLock)
                {
                    return _dataStore.Count;
                }
            }
        }

        /// <summary>
        /// Clears all stored data and returns items to the pool.
        /// </summary>
        public void ClearAll()
        {
            if (_disposed) return;

            List<ManagedLogData> dataToReturn;
            lock (_storeLock)
            {
                dataToReturn = new List<ManagedLogData>(_dataStore.Values);
                _dataStore.Clear();
            }

            foreach (var data in dataToReturn)
            {
                data.Reset();
                _poolingService.Return(data);
            }
        }

        /// <summary>
        /// Performs maintenance by removing expired data entries.
        /// </summary>
        /// <param name="maxAge">The maximum age of data to keep</param>
        /// <returns>The number of expired entries removed</returns>
        public int RemoveExpiredData(TimeSpan maxAge)
        {
            if (_disposed) return 0;

            var cutoffTime = DateTime.UtcNow - maxAge;
            var expiredIds = new List<Guid>();

            lock (_storeLock)
            {
                foreach (var kvp in _dataStore)
                {
                    if (kvp.Value.CreatedAt < cutoffTime)
                    {
                        expiredIds.Add(kvp.Key);
                    }
                }
            }

            foreach (var id in expiredIds)
            {
                ReleaseData(id);
            }

            return expiredIds.Count;
        }

        private void RegisterPools()
        {
            // Register the ManagedLogData pool
            var poolConfig = new PoolConfiguration
            {
                InitialCapacity = 100,
                MaxCapacity = 1000,
                Factory = () => new ManagedLogData(),
                ResetAction = data => data.Reset(),
                ValidationFunc = data => data != null
            };

            _poolingService.RegisterPool<ManagedLogData>(poolConfig);

            // Register pools for common property dictionary types
            _poolingService.RegisterPool<Dictionary<string, object>>(new PoolConfiguration
            {
                InitialCapacity = 50,
                MaxCapacity = 200,
                Factory = () => new Dictionary<string, object>(),
                ResetAction = dict => dict.Clear(),
                ValidationFunc = dict => dict != null
            });
        }

        /// <summary>
        /// Disposes the pool and releases all stored data.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            ClearAll();
        }
    }

    /// <summary>
    /// Represents managed data that cannot be stored in native collections.
    /// </summary>
    public sealed class ManagedLogData : IPooledObject
    {
        /// <summary>
        /// The exception associated with the log entry.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The structured properties associated with the log entry.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// The scope context associated with the log entry.
        /// </summary>
        public object Scope { get; set; }

        /// <summary>
        /// The unique storage ID for this data.
        /// </summary>
        public Guid StorageId { get; set; }

        /// <summary>
        /// The timestamp when this data was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets the pool name for this object.
        /// </summary>
        public string PoolName { get; set; }

        /// <summary>
        /// Gets the timestamp when this object was last used.
        /// </summary>
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        public void OnGet()
        {
            LastUsed = DateTime.UtcNow;
        }

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public void OnReturn()
        {
            // Reset is called separately, so we don't need to do anything here
        }

        /// <summary>
        /// Resets the object to its initial state for reuse.
        /// </summary>
        public void Reset()
        {
            Exception = null;
            Properties = null;
            Scope = null;
            StorageId = Guid.Empty;
            CreatedAt = DateTime.MinValue;
        }

        /// <summary>
        /// Validates that the object is in a valid state.
        /// </summary>
        /// <returns>True if the object is valid</returns>
        public bool IsValid()
        {
            return true; // ManagedLogData is always valid
        }
    }
}