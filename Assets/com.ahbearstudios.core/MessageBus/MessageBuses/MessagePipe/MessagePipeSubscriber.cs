using System;
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
        private readonly ISimpleSubscriptionWrapper _subscriptionWrapper;
        private readonly ISubscriptionTracker _subscriptionTracker;
        private readonly string _subscriberName;
        
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeSubscriber class.
        /// </summary>
        /// <param name="subscriber">The underlying MessagePipe subscriber.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profilerService">The profiler for performance monitoring.</param>
        public MessagePipeSubscriber(
            ISubscriber<TMessage> subscriber,
            IBurstLogger logger,
            IProfilerService profilerService)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            
            var handlerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            var handlerProfiler = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            
            _subscriberName = $"Subscriber<{typeof(TMessage).Name}>";
            
            _subscriptionWrapper = new SimpleSubscriptionWrapper(handlerLogger, handlerProfiler, _subscriberName);
            _subscriptionTracker = new SimpleSubscriptionTracker(handlerLogger, _subscriberName);
            
            handlerLogger.Log(LogLevel.Debug, 
                $"Created {_subscriberName}", 
                "MessagePipeSubscriber");
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler)
        {
            ThrowIfDisposed();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return _subscriptionWrapper.WrapSubscription(
                handler,
                wrappedHandler => _subscriber.Subscribe(wrappedHandler),
                _subscriptionTracker);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            ThrowIfDisposed();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return _subscriptionWrapper.WrapFilteredSubscription(
                handler,
                filter,
                wrappedHandler => _subscriber.Subscribe(wrappedHandler),
                _subscriptionTracker);
        }

        /// <summary>
        /// Gets the total number of subscriptions created by this subscriber.
        /// </summary>
        public long TotalSubscriptions => _subscriptionTracker.TotalSubscriptions;

        /// <summary>
        /// Gets the total number of messages received and processed by this subscriber.
        /// </summary>
        public long TotalMessagesReceived => _subscriptionWrapper.TotalMessagesReceived;

        /// <summary>
        /// Gets the total number of messages that were filtered out.
        /// </summary>
        public long TotalMessagesFiltered => _subscriptionWrapper.TotalMessagesFiltered;

        /// <summary>
        /// Gets the number of currently active subscriptions.
        /// </summary>
        public int ActiveSubscriptionCount => _subscriptionTracker.ActiveSubscriptionCount;

        /// <summary>
        /// Gets the message processing rate (messages received / total messages including filtered).
        /// </summary>
        public double MessageProcessingRate
        {
            get
            {
                var received = TotalMessagesReceived;
                var filtered = TotalMessagesFiltered;
                var total = received + filtered;
                return total > 0 ? (double)received / total : 1.0;
            }
        }

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