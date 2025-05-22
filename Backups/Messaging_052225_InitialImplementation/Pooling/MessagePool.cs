using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Object pool for message types to reduce garbage collection pressure.
    /// Provides efficient reuse of message instances.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to pool.</typeparam>
    public class MessagePool<TMessage> : IDisposable where TMessage : IMessage, new()
    {
        private readonly Stack<TMessage> _pool;
        private readonly int _initialSize;
        private readonly int _maxSize;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly object _poolLock = new object();
        private long _totalCreated;
        private long _totalReused;
        private bool _isDisposed;

        /// <summary>
        /// Gets the current size of the pool.
        /// </summary>
        public int PoolSize
        {
            get
            {
                lock (_poolLock)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>
        /// Gets the maximum allowed size of the pool.
        /// </summary>
        public int MaxPoolSize => _maxSize;

        /// <summary>
        /// Gets the total number of messages created.
        /// </summary>
        public long TotalCreated => _totalCreated;

        /// <summary>
        /// Gets the total number of messages reused from the pool.
        /// </summary>
        public long TotalReused => _totalReused;

        /// <summary>
        /// Gets the reuse ratio (reused messages / total messages).
        /// </summary>
        public double ReuseRatio
        {
            get
            {
                long total = _totalCreated + _totalReused;
                return total > 0 ? (double)_totalReused / total : 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessagePool class.
        /// </summary>
        /// <param name="initialSize">The initial number of message instances to create.</param>
        /// <param name="maxSize">The maximum number of message instances to keep in the pool.</param>
        /// <param name="logger">Optional logger for pool operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessagePool(int initialSize = 32, int maxSize = 1024, IBurstLogger logger = null, IProfiler profiler = null)
        {
            if (initialSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialSize), "Initial size cannot be negative");
            }

            if (maxSize < initialSize)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be less than initial size");
            }

            _pool = new Stack<TMessage>(initialSize);
            _initialSize = initialSize;
            _maxSize = maxSize;
            _logger = logger;
            _profiler = profiler;
            _totalCreated = 0;
            _totalReused = 0;
            _isDisposed = false;
            
            // Pre-populate the pool
            PrePopulatePool();
            
            if (_logger != null)
            {
                _logger.Info($"MessagePool<{typeof(TMessage).Name}> initialized with initial size {initialSize}, max size {maxSize}");
            }
        }

        /// <summary>
        /// Gets a message instance from the pool, or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>A message instance ready for use.</returns>
        public TMessage Get()
        {
            using (_profiler?.BeginSample("MessagePool.Get"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePool<TMessage>));
                }

                TMessage message;
                
                lock (_poolLock)
                {
                    if (_pool.Count > 0)
                    {
                        message = _pool.Pop();
                        Interlocked.Increment(ref _totalReused);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Reused message from pool, remaining: {_pool.Count}");
                        }
                    }
                    else
                    {
                        message = CreateNewMessage();
                        Interlocked.Increment(ref _totalCreated);
                        
                        if (_logger != null)
                        {
                            _logger.Debug("Created new message (pool empty)");
                        }
                    }
                }
                
                return message;
            }
        }

        /// <summary>
        /// Returns a message instance to the pool for reuse.
        /// </summary>
        /// <param name="message">The message instance to return to the pool.</param>
        public void Return(TMessage message)
        {
            using (_profiler?.BeginSample("MessagePool.Return"))
            {
                if (_isDisposed)
                {
                    // If disposed, just let the GC handle it
                    return;
                }

                if (message == null)
                {
                    return;
                }

                // Reset the message instance for reuse
                ResetMessage(message);
                
                lock (_poolLock)
                {
                    // Only add back to the pool if we're under max size
                    if (_pool.Count < _maxSize)
                    {
                        _pool.Push(message);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Returned message to pool, size: {_pool.Count}");
                        }
                    }
                    else
                    {
                        if (_logger != null)
                        {
                            _logger.Debug("Discarded message return (pool full)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the pool of all message instances.
        /// </summary>
        public void Clear()
        {
            using (_profiler?.BeginSample("MessagePool.Clear"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePool<TMessage>));
                }

                lock (_poolLock)
                {
                    int count = _pool.Count;
                    _pool.Clear();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Cleared message pool, discarded {count} messages");
                    }
                }
            }
        }

        /// <summary>
        /// Pre-populates the pool with message instances.
        /// </summary>
        private void PrePopulatePool()
        {
            using (_profiler?.BeginSample("MessagePool.PrePopulatePool"))
            {
                lock (_poolLock)
                {
                    for (int i = 0; i < _initialSize; i++)
                    {
                        _pool.Push(CreateNewMessage());
                        Interlocked.Increment(ref _totalCreated);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Pre-populated pool with {_initialSize} messages");
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new message instance.
        /// </summary>
        /// <returns>A new message instance.</returns>
        private TMessage CreateNewMessage()
        {
            return new TMessage();
        }

        /// <summary>
        /// Resets a message instance for reuse.
        /// </summary>
        /// <param name="message">The message instance to reset.</param>
        private void ResetMessage(TMessage message)
        {
            // If the message implements a reset interface, call it
            if (message is IResetable resetable)
            {
                resetable.Reset();
            }
            
            // Otherwise, let the next user of the message set its properties
            // You could also use reflection to reset properties, but that's more expensive
        }

        /// <summary>
        /// Disposes the message pool and releases all resources.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessagePool.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message pool.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Clear the pool
                lock (_poolLock)
                {
                    _pool.Clear();
                }
                
                if (_logger != null)
                {
                    _logger.Info($"MessagePool<{typeof(TMessage).Name}> disposed, " +
                                 $"total created: {_totalCreated}, total reused: {_totalReused}, " +
                                 $"reuse ratio: {ReuseRatio:P2}");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessagePool()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Interface for objects that can be reset for reuse.
    /// </summary>
    public interface IResetable
    {
        /// <summary>
        /// Resets the object to its initial state for reuse.
        /// </summary>
        void Reset();
    }
}