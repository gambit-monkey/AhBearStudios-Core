using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of ISimpleSubscriptionWrapper that provides profiling, logging, and error handling.
    /// </summary>
    internal sealed class SimpleSubscriptionWrapper : ISimpleSubscriptionWrapper
    {
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _subscriberName;
        private readonly object _syncLock = new object();
        
        private long _totalMessagesReceived;
        private long _totalMessagesFiltered;

        /// <summary>
        /// Initializes a new instance of the SimpleSubscriptionWrapper class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        /// <param name="subscriberName">The name of the subscriber for logging purposes.</param>
        public SimpleSubscriptionWrapper(IBurstLogger logger, IProfiler profiler, string subscriberName)
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
        
        /// <inheritdoc />
        public IDisposable WrapSubscription<TMessage>(
            Action<TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker)
        {
            using (_profiler.BeginSample($"{_subscriberName}.Subscribe"))
            {
                try
                {
                    var wrappedHandler = CreateHandler(handler);
                    var subscription = subscribe(wrappedHandler);
                    
                    _logger.Log(LogLevel.Debug,
                        $"Created subscription on {_subscriberName}",
                        "MessagePipeSubscriber");

                    return tracker.TrackSubscription(subscription);
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
        public IDisposable WrapFilteredSubscription<TMessage>(
            Action<TMessage> handler,
            Func<TMessage, bool> filter,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker)
        {
            using (_profiler.BeginSample($"{_subscriberName}.SubscribeWithFilter"))
            {
                try
                {
                    var wrappedHandler = CreateFilteredHandler(handler, filter);
                    var subscription = subscribe(wrappedHandler);
                    
                    _logger.Log(LogLevel.Debug,
                        $"Created filtered subscription on {_subscriberName}",
                        "MessagePipeSubscriber");

                    return tracker.TrackSubscription(subscription, isFiltered: true);
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

        private Action<TMessage> CreateHandler<TMessage>(Action<TMessage> handler)
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
        }

        private Action<TMessage> CreateFilteredHandler<TMessage>(
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
                            IncrementFilteredCount();
                            return;
                        }

                        if (!shouldHandle)
                        {
                            IncrementFilteredCount();
                            
                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.Log(LogLevel.Trace,
                                    $"Message of type {typeof(TMessage).Name} filtered out",
                                    "MessagePipeSubscriber");
                            }
                            return;
                        }

                        handler(message);
                        IncrementMessageCount();

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
        }

        private void IncrementMessageCount()
        {
            lock (_syncLock)
            {
                _totalMessagesReceived++;
            }
        }

        private void IncrementFilteredCount()
        {
            lock (_syncLock)
            {
                _totalMessagesFiltered++;
            }
        }
    }
}