using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedMessageSubscriber using MessagePipe's keyed subscriber.
    /// Provides efficient keyed message subscription with filtering, performance profiling, and logging.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    public sealed class MessagePipeKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>, IDisposable
    {
        private readonly ISubscriber<TKey, TMessage> _keyedSubscriber;
        private readonly ISubscriber<TMessage> _globalSubscriber;
        private readonly IKeyedSubscriptionWrapper _keyedSubscriptionWrapper;
        private readonly IKeyedSubscriptionTracker _subscriptionTracker;
        private readonly string _subscriberName;
        
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedSubscriber class.
        /// </summary>
        /// <param name="keyedSubscriber">The underlying MessagePipe keyed subscriber.</param>
        /// <param name="globalSubscriber">The underlying MessagePipe global subscriber for all-key subscriptions.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profilerService">The profiler for performance monitoring.</param>
        public MessagePipeKeyedSubscriber(
            ISubscriber<TKey, TMessage> keyedSubscriber,
            ISubscriber<TMessage> globalSubscriber,
            IBurstLogger logger,
            IProfilerService profilerService)
        {
            _keyedSubscriber = keyedSubscriber ?? throw new ArgumentNullException(nameof(keyedSubscriber));
            _globalSubscriber = globalSubscriber ?? throw new ArgumentNullException(nameof(globalSubscriber));
            
            var handlerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            var handlerProfiler = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            
            _subscriberName = $"KeyedSubscriber<{typeof(TKey).Name}, {typeof(TMessage).Name}>";
            
            _keyedSubscriptionWrapper = new KeyedSubscriptionWrapper(handlerLogger, handlerProfiler, _subscriberName);
            _subscriptionTracker = new KeyedSubscriptionTracker(handlerLogger, _subscriberName);
            
            handlerLogger.Log(LogLevel.Debug, 
                $"Created {_subscriberName}", 
                "MessagePipeKeyedSubscriber");
        }

        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler)
        {
            ThrowIfDisposed();
            
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return _keyedSubscriptionWrapper.WrapKeyedSubscription(
                key,
                handler,
                wrappedHandler => _keyedSubscriber.Subscribe(key, wrappedHandler),
                _subscriptionTracker);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TKey, TMessage> handler)
        {
            ThrowIfDisposed();
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return _keyedSubscriptionWrapper.WrapGlobalSubscription(
                handler,
                wrappedHandler => _globalSubscriber.Subscribe(wrappedHandler),
                _subscriptionTracker);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            ThrowIfDisposed();
            
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return _keyedSubscriptionWrapper.WrapFilteredSubscription(
                key,
                handler,
                filter,
                wrappedHandler => _keyedSubscriber.Subscribe(key, wrappedHandler),
                _subscriptionTracker);
        }

        /// <summary>
        /// Gets the total number of subscriptions created by this subscriber.
        /// </summary>
        public long TotalSubscriptions => _subscriptionTracker.TotalSubscriptions;

        /// <summary>
        /// Gets the total number of messages received by this subscriber.
        /// </summary>
        public long TotalMessagesReceived => _keyedSubscriptionWrapper.TotalMessagesReceived;

        /// <summary>
        /// Gets the number of currently active subscriptions.
        /// </summary>
        public int ActiveSubscriptionCount => _subscriptionTracker.ActiveSubscriptionCount;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _subscriptionTracker.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(_subscriberName);
        }
    }
}