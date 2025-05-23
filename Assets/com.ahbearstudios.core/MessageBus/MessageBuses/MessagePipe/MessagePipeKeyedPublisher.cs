using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedMessagePublisher using MessagePipe's keyed publisher.
    /// Provides efficient keyed message publishing with performance profiling and logging.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    public sealed class MessagePipeKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>, IDisposable
    {
        private readonly IAsyncPublisher<TKey, TMessage> _publisher;
        private readonly IKeyedPublisherWrapper _keyedPublisherWrapper;
        private readonly IPublishingStatistics _statistics;
        private readonly string _publisherName;
        
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedPublisher class.
        /// </summary>
        /// <param name="publisher">The underlying MessagePipe keyed publisher.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        public MessagePipeKeyedPublisher(
            IAsyncPublisher<TKey, TMessage> publisher,
            IBurstLogger logger,
            IProfiler profiler)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            
            var handlerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            var handlerProfiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            _publisherName = $"KeyedPublisher<{typeof(TKey).Name}, {typeof(TMessage).Name}>";

            _keyedPublisherWrapper = new KeyedPublisherWrapper(handlerLogger, handlerProfiler, _publisherName);
            _statistics = new PublishingStatistics(handlerLogger, _publisherName);

            handlerLogger.Log(LogLevel.Debug,
                $"Created {_publisherName}",
                "MessagePipeKeyedPublisher");
        }

        /// <inheritdoc />
        public void Publish(TKey key, TMessage message)
        {
            ThrowIfDisposed();

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            _keyedPublisherWrapper.WrapSyncPublish(
                key,
                message,
                (k, m) => _publisher.Publish(k, m),
                _statistics);
        }

        /// <inheritdoc />
        public IDisposable PublishAsync(TKey key, TMessage message)
        {
            ThrowIfDisposed();

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            return _keyedPublisherWrapper.WrapAsyncPublish(
                key,
                message,
                (k, m) => _publisher.PublishAsync(k, m),
                _statistics);
        }

        /// <summary>
        /// Gets the total number of messages published by this publisher.
        /// </summary>
        public long TotalMessagesPublished => _statistics.TotalMessagesPublished;

        /// <summary>
        /// Gets the total number of async publishes initiated by this publisher.
        /// </summary>
        public long TotalAsyncPublishes => _statistics.TotalAsyncPublishes;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _statistics.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(_publisherName);
        }
    }
}