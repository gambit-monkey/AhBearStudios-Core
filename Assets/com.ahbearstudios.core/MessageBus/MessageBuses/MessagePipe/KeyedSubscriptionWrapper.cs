using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedSubscriptionWrapper that provides profiling, logging, and error handling.
    /// </summary>
    internal sealed class KeyedSubscriptionWrapper : IKeyedSubscriptionWrapper
    {
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _subscriberName;
        private readonly object _syncLock = new object();
        
        private long _totalMessagesReceived;

        /// <summary>
        /// Initializes a new instance of the KeyedSubscriptionWrapper class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        /// <param name="subscriberName">The name of the subscriber for logging purposes.</param>
        public KeyedSubscriptionWrapper(IBurstLogger logger, IProfiler profiler, string subscriberName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _subscriberName = subscriberName ?? throw new ArgumentNullException(nameof(subscriberName));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IDisposable WrapKeyedSubscription<TKey, TMessage>(
            TKey key,
            Action<TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            IKeyedSubscriptionTracker tracker)
        {
            using (_profiler.BeginSample($"{_subscriberName}.Subscribe"))
            {
                try
                {
                    var wrappedHandler = CreateKeyedHandler(key, handler);
                    var subscription = subscribe(wrappedHandler);
                    
                    _logger.Log(LogLevel.Debug,
                        $"Created subscription for key '{key}' on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    return tracker.TrackSubscription(subscription, key);
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
        public IDisposable WrapGlobalSubscription<TKey, TMessage>(
            Action<TKey, TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            IKeyedSubscriptionTracker tracker)
        {
            using (_profiler.BeginSample($"{_subscriberName}.SubscribeAll"))
            {
                try
                {
                    // For global subscriptions, we need to adapt the handler since we can't get the key
                    // from a global message subscription. This is a limitation we need to address.
                    var wrappedHandler = CreateGlobalHandler<TKey, TMessage>(handler);
                    var subscription = subscribe(wrappedHandler);
                    
                    _logger.Log(LogLevel.Debug,
                        $"Created subscription for all keys on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    return tracker.TrackSubscription(subscription);
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
        public IDisposable WrapFilteredSubscription<TKey, TMessage>(
            TKey key,
            Action<TMessage> handler,
            Func<TMessage, bool> filter,
            Func<Action<TMessage>, IDisposable> subscribe,
            IKeyedSubscriptionTracker tracker)
        {
            using (_profiler.BeginSample($"{_subscriberName}.SubscribeWithFilter"))
            {
                try
                {
                    var wrappedHandler = CreateFilteredHandler(key, handler, filter);
                    var subscription = subscribe(wrappedHandler);
                    
                    _logger.Log(LogLevel.Debug,
                        $"Created filtered subscription for key '{key}' on {_subscriberName}",
                        "MessagePipeKeyedSubscriber");

                    return tracker.TrackSubscription(subscription, key);
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

        private Action<TMessage> CreateKeyedHandler<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            return message =>
            {
                using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                {
                    try
                    {
                        handler(message);
                        IncrementMessageCount();

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
        }

        private Action<TMessage> CreateGlobalHandler<TKey, TMessage>(Action<TKey, TMessage> handler)
        {
            return message =>
            {
                using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                {
                    try
                    {
                        // Note: This is a design limitation - we can't extract the key from a global message
                        // For now, we'll pass default(TKey) but this should be addressed architecturally
                        handler(default(TKey), message);
                        IncrementMessageCount();

                        if (_logger.IsEnabled(LogLevel.Trace))
                        {
                            _logger.Log(LogLevel.Trace,
                                $"Handled global message of type {typeof(TMessage).Name}",
                                "MessagePipeKeyedSubscriber");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error,
                            $"Error in global message handler: {ex.Message}",
                            "MessagePipeKeyedSubscriber");
                        throw;
                    }
                }
            };
        }

        private Action<TMessage> CreateFilteredHandler<TKey, TMessage>(
            TKey key, 
            Action<TMessage> handler, 
            Func<TMessage, bool> filter)
        {
            return message =>
            {
                using (_profiler.BeginSample($"{_subscriberName}.HandleMessage"))
                {
                    try
                    {
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
                        IncrementMessageCount();

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
        }

        private void IncrementMessageCount()
        {
            lock (_syncLock)
            {
                _totalMessagesReceived++;
            }
        }
    }
}