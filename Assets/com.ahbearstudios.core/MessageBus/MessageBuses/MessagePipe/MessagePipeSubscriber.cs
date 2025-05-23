using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IMessageSubscriber using MessagePipe's subscriber.
    /// Provides efficient message subscription with filtering, performance profiling, and logging.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    public sealed class MessagePipeSubscriber<TMessage> : IMessageSubscriber<TMessage>, IDisposable
    {
        private readonly ISubscriber<TMessage> _subscriber;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _subscriberName;
        private readonly object _syncLock = new object();
        private readonly List<IDisposable> _activeSubscriptions = new List<IDisposable>();
        
        private long _totalSubscriptions;
        private long _totalMessagesReceived;
        private long _totalMessagesFiltered;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeSubscriber class.
        /// </summary>
        /// <param name="subscriber">The underlying MessagePipe subscriber.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        public MessagePipeSubscriber(
            ISubscriber<TMessage> subscriber,
            IBurstLogger logger,
            IProfiler profiler)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _subscriberName = $"Subscriber<{typeof(TMessage).Name}>";
            
            _logger.Log(LogLevel.Debug, 
                $"Created {_subscriberName}", 
                "MessagePipeSubscriber");
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler)
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);

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
                                        $"Handled message of type {typeof(TMessage).Name}",
                                        "MessagePipeSubscriber");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error,
                                    $"Error in message handler for {typeof(TMessage).Name}: {ex.Message}",
                                    "MessagePipeSubscriber");
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
                        $"Created subscription on {_subscriberName}. Total active: {_activeSubscriptions.Count}",
                        "MessagePipeSubscriber");

                    // Return a wrapper that removes from tracking when disposed
                    return new SubscriptionHandle(
                        subscription, 
                        () => RemoveSubscription(subscription), 
                        _logger, 
                        _subscriberName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error creating subscription: {ex.Message}",
                        "MessagePipeSubscriber");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);

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
                                    using (_profiler.BeginSample($"{_subscriberName}.Filter"))
                                    {
                                        shouldHandle = filter(message);
                                    }
                                }
                                catch (Exception filterEx)
                                {
                                    _logger.Log(LogLevel.Error,
                                        $"Error in message filter for {typeof(TMessage).Name}: {filterEx.Message}",
                                        "MessagePipeSubscriber");
                                    // Don't process the message if filter fails
                                    lock (_syncLock)
                                    {
                                        _totalMessagesFiltered++;
                                    }
                                    return;
                                }

                                if (!shouldHandle)
                                {
                                    lock (_syncLock)
                                    {
                                        _totalMessagesFiltered++;
                                    }
                                    
                                    if (_logger.IsEnabled(LogLevel.Trace))
                                    {
                                        _logger.Log(LogLevel.Trace,
                                            $"Message of type {typeof(TMessage).Name} filtered out",
                                            "MessagePipeSubscriber");
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
                                        $"Handled filtered message of type {typeof(TMessage).Name}",
                                        "MessagePipeSubscriber");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error,
                                    $"Error in message handler for {typeof(TMessage).Name}: {ex.Message}",
                                    "MessagePipeSubscriber");
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
                        $"Created filtered subscription on {_subscriberName}. Total active: {_activeSubscriptions.Count}",
                        "MessagePipeSubscriber");

                    // Return a wrapper that removes from tracking when disposed
                    return new SubscriptionHandle(
                        subscription, 
                        () => RemoveSubscription(subscription), 
                        _logger, 
                        _subscriberName,
                        isFiltered: true);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error creating filtered subscription: {ex.Message}",
                        "MessagePipeSubscriber");
                    throw;
                }
            }
        }

        /// <summary>
        /// Removes a subscription from the active subscriptions list.
        /// </summary>
        private void RemoveSubscription(IDisposable subscription)
        {
            lock (_syncLock)
            {
                _activeSubscriptions.Remove(subscription);
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.Log(LogLevel.Debug,
                        $"Removed subscription from {_subscriberName}. Remaining active: {_activeSubscriptions.Count}",
                        "MessagePipeSubscriber");
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
        /// Gets the total number of messages received and processed by this subscriber.
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
        /// Gets the total number of messages that were filtered out.
        /// </summary>
        public long TotalMessagesFiltered
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalMessagesFiltered;
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

        /// <summary>
        /// Gets the message processing rate (messages received / total messages including filtered).
        /// </summary>
        public double MessageProcessingRate
        {
            get
            {
                lock (_syncLock)
                {
                    var total = _totalMessagesReceived + _totalMessagesFiltered;
                    return total > 0 ? (double)_totalMessagesReceived / total : 1.0;
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
                    $"Disposing {_subscriberName}. Statistics - Total subscriptions: {_totalSubscriptions}, " +
                    $"Messages received: {_totalMessagesReceived}, Messages filtered: {_totalMessagesFiltered}, " +
                    $"Active subscriptions: {_activeSubscriptions.Count}, Processing rate: {MessageProcessingRate:P2}",
                    "MessagePipeSubscriber");

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
                            "MessagePipeSubscriber");
                    }
                }

                _activeSubscriptions.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Handle for subscription operations that provides disposal tracking and lifecycle management.
        /// </summary>
        private sealed class SubscriptionHandle : IDisposable
        {
            private readonly IDisposable _innerSubscription;
            private readonly Action _onDispose;
            private readonly IBurstLogger _logger;
            private readonly string _subscriberName;
            private readonly bool _isFiltered;
            private readonly DateTime _createdAt;
            private bool _disposed;

            public SubscriptionHandle(
                IDisposable innerSubscription,
                Action onDispose,
                IBurstLogger logger,
                string subscriberName,
                bool isFiltered = false)
            {
                _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
                _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _subscriberName = subscriberName;
                _isFiltered = isFiltered;
                _createdAt = DateTime.UtcNow;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _innerSubscription.Dispose();
                    _onDispose();
                    
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var duration = DateTime.UtcNow - _createdAt;
                        var subscriptionType = _isFiltered ? "filtered subscription" : "subscription";
                        _logger.Log(LogLevel.Debug,
                            $"{_subscriberName}: Disposed {subscriptionType} after {duration.TotalSeconds:F2} seconds",
                            "MessagePipeSubscriber");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"{_subscriberName}: Error disposing subscription: {ex.Message}",
                        "MessagePipeSubscriber");
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