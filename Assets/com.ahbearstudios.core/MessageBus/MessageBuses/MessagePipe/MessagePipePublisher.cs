using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IMessagePublisher using MessagePipe's publisher.
    /// Provides efficient message publishing with performance profiling and logging.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    public sealed class MessagePipePublisher<TMessage> : IMessagePublisher<TMessage>, IDisposable
    {
        private readonly IAsyncPublisher<TMessage> _publisher;
        private readonly ISimplePublisherWrapper _publisherWrapper;
        private readonly IPublishingStatistics _statistics;
        private readonly string _publisherName;
        
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipePublisher class.
        /// </summary>
        /// <param name="publisher">The underlying MessagePipe publisher.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        public MessagePipePublisher(
            IAsyncPublisher<TMessage> publisher,
            IBurstLogger logger,
            IProfiler profiler)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            
            var handlerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            var handlerProfiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _publisherName = $"Publisher<{typeof(TMessage).Name}>";

            _publisherWrapper = new SimplePublisherWrapper(handlerLogger, handlerProfiler, _publisherName);
            _statistics = new PublishingStatistics(handlerLogger, _publisherName);
            
            handlerLogger.Log(LogLevel.Debug, 
                $"Created {_publisherName}", 
                "MessagePipePublisher");
        }

        /// <inheritdoc />
        public void Publish(TMessage message)
        {
            ThrowIfDisposed();

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            _publisherWrapper.WrapSyncPublish(
                message,
                m => _publisher.Publish(m),
                _statistics);
        }

        /// <inheritdoc />
        public IDisposable PublishAsync(TMessage message)
        {
            ThrowIfDisposed();

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            return _publisherWrapper.WrapAsyncPublish(
                message,
                m => _publisher.PublishAsync(m),
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