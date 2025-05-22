using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.Messaging.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedMessageSubscriber using MessagePipe's keyed subscriber.
    /// Provides efficient keyed message subscription with filtering, performance profiling, and logging.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    public sealed class MessagePipeKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>, IDisposable
    {
        private readonly ISubscriber<TKey, TMessage> _subscriber;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _subscriberName;
        private readonly object _syncLock = new object();
        private readonly List<IDisposable> _activeSubscriptions = new List<IDisposable>();
        
        private long _totalSubscriptions;
        private long _totalMessagesReceived;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedSubscriber class.
        /// </summary>
        /// <param name="subscriber">The underlying MessagePipe keyed subscriber.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        public MessagePipeKeyedSubscriber(
            ISubscriber<TKey, TMessage> subscriber,
            IBurstLogger logger,
            IProfiler profiler)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _subscriberName = $"KeyedSubscriber<{typeof(TKey).Name}, {typeof(TMessage).Name}>";
            
            _logger.Log(LogLevel.Debug, 
                $"Created {_subscriberName}", 
                "MessagePipeKeyedSubscriber");
        }

        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler)
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            using (_profiler.BeginSample($"{_subscriberName}.Subscribe"))
            {
                try
                {
                    // Wrap the handler to add profiling and logging
                    Action<TMessage> wrappedHandler = message =>
                    {
                        using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                        {
                            try
                            {
                                handler(message);
                                
                                lock (_syncLock)
                                {
                                    _totalMessagesReceived++;
                                }

                                if (_logger.IsEnabled(LogLevel.Trace))
                                {
                                    _logger.Log(LogLevel.Trace,
                                        $"Handled message with key '{key}' of type {typeof(TMessage).Name}",
                                        "MessagePipeKeyedSubscriber");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error,
                                    $"Error in message handler for key '{key}': {ex.Message}",
                                    "MessagePipeKeyedSubscriber");
                                throw;
                            }
                        }
                    };

                    var subscription = _subscriber.Subscribe(key, wrappedHandler);
                    
                    lock (_syncLock)
                    {
                        _totalSubscriptions++;
                        _activeSubscriptions.Add(subscription);
                    }

                    _logger.Log(LogLevel.Debug,
                        $"Created subscription for key '{key}' on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    // Return a wrapper that removes from tracking when disposed
                    return new SubscriptionHandle(subscription, () =>
                    {
                        lock (_syncLock)
                        {
                            _activeSubscriptions.Remove(subscription);
                        }
                    }, key, _logger, _subscriberName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error creating subscription for key '{key}': {ex.Message}",
                        "MessagePipeKeyedSubscriber");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TKey, TMessage> handler)
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            using (_profiler.BeginSample($"{_subscriberName}.SubscribeAll"))
            {
                try
                {
                    // Wrap the handler to add profiling and logging
                    Action<TKey, TMessage> wrappedHandler = (key, message) =>
                    {
                        using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                        {
                            try
                            {
                                handler(key, message);
                                
                                lock (_syncLock)
                                {
                                    _totalMessagesReceived++;
                                }

                                if (_logger.IsEnabled(LogLevel.Trace))
                                {
                                    _logger.Log(LogLevel.Trace,
                                        $"Handled message with key '{key}' of type {typeof(TMessage).Name}",
                                        "MessagePipeKeyedSubscriber");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error,
                                    $"Error in message handler: {ex.Message}",
                                    "MessagePipeKeyedSubscriber");
                                throw;
                            }
                        }
                    };

                    var subscription = _subscriber.Subscribe(wrappedHandler);
                    
                    lock (_syncLock)
                    {
                        _totalSubscriptions++;
                        _activeSubscriptions.Add(subscription);
                    }

                    _logger.Log(LogLevel.Debug,
                        $"Created subscription for all keys on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    // Return a wrapper that removes from tracking when disposed
                    return new SubscriptionHandle(subscription, () =>
                    {
                        lock (_syncLock)
                        {
                            _activeSubscriptions.Remove(subscription);
                        }
                    }, null, _logger, _subscriberName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error creating subscription for all keys: {ex.Message}",
                        "MessagePipeKeyedSubscriber");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            using (_profiler.BeginSample($"{_subscriberName}.SubscribeWithFilter"))
            {
                try
                {
                    // Wrap the handler to add filtering, profiling and logging
                    Action<TMessage> wrappedHandler = message =>
                    {
                        using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                        {
                            try
                            {
                                // Apply filter first
                                bool shouldHandle;
                                try
                                {
                                    shouldHandle = filter(message);
                                }
                                catch (Exception filterEx)
                                {
                                    _logger.Log(LogLevel.Error,
                                        $"Error in message filter for key '{key}': {filterEx.Message}",
                                        "MessagePipeKeyedSubscriber");
                                    // Don't process the message if filter fails
                                    return;
                                }

                                if (!shouldHandle)
                                {
                                    if (_logger.IsEnabled(LogLevel.Trace))
                                    {
                                        _logger.Log(LogLevel.Trace,
                                            $"Message filtered out for key '{key}'",
                                            "MessagePipeKeyedSubscriber");
                                    }
                                    return;
                                }

                                handler(message);
                                
                                lock (_syncLock)
                                {
                                    _totalMessagesReceived++;
                                }

                                if (_logger.IsEnabled(LogLevel.Trace))
                                {
                                    _logger.Log(LogLevel.Trace,
                                        $"Handled filtered message with key '{key}' of type {typeof(TMessage).Name}",
                                        "MessagePipeKeyedSubscriber");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error,
                                    $"Error in message handler for key '{key}': {ex.Message}",
                                    "MessagePipeKeyedSubscriber");
                                throw;
                            }
                        }
                    };

                    var subscription = _subscriber.Subscribe(key, wrappedHandler);
                    
                    lock (_syncLock)
                    {
                        _totalSubscriptions++;
                        _activeSubscriptions.Add(subscription);
                    }

                    _logger.Log(LogLevel.Debug,
                        $"Created filtered subscription for key '{key}' on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    // Return a wrapper that removes from tracking when disposed
                    return new SubscriptionHandle(subscription, () =>
                    {
                        lock (_syncLock)
                        {
                            _activeSubscriptions.Remove(subscription);
                        }
                    }, key, _logger, _subscriberName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error creating filtered subscription for key '{key}': {ex.Message}",
                        "MessagePipeKeyedSubscriber");
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the total number of subscriptions created by this subscriber.
        /// </summary>
        public long TotalSubscriptions
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalSubscriptions;
                }
            }
        }

        /// <summary>
        /// Gets the total number of messages received by this subscriber.
        /// </summary>
        public long TotalMessagesReceived
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalMessagesReceived;
                }
            }
        }

        /// <summary>
        /// Gets the number of currently active subscriptions.
        /// </summary>
        public int ActiveSubscriptionCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _activeSubscriptions.Count;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                if (_disposed)
                    return;

                _logger.Log(LogLevel.Debug,
                    $"Disposing {_subscriberName}. Total subscriptions: {_totalSubscriptions}, " +
                    $"Messages received: {_totalMessagesReceived}, Active subscriptions: {_activeSubscriptions.Count}",
                    "MessagePipeKeyedSubscriber");

                // Dispose all active subscriptions
                foreach (var subscription in _activeSubscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Warning,
                            $"Error disposing subscription: {ex.Message}",
                            "MessagePipeKeyedSubscriber");
                    }
                }

                _activeSubscriptions.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Handle for subscription operations that provides disposal tracking.
        /// </summary>
        private sealed class SubscriptionHandle : IDisposable
        {
            private readonly IDisposable _innerSubscription;
            private readonly Action _onDispose;
            private readonly TKey _key;
            private readonly IBurstLogger _logger;
            private readonly string _subscriberName;
            private bool _disposed;

            public SubscriptionHandle(
                IDisposable innerSubscription,
                Action onDispose,
                TKey key,
                IBurstLogger logger,
                string subscriberName)
            {
                _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
                _onDispose = onDispose;
                _key = key;
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _subscriberName = subscriberName;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _innerSubscription.Dispose();
                    _onDispose?.Invoke();
                    
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var keyInfo = _key != null ? $"key '{_key}'" : "all keys";
                        _logger.Log(LogLevel.Debug,
                            $"{_subscriberName}: Disposed subscription for {keyInfo}",
                            "MessagePipeKeyedSubscriber");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"{_subscriberName}: Error disposing subscription: {ex.Message}",
                        "MessagePipeKeyedSubscriber");
                    throw;
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}