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
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _publisherName;
        private readonly object _syncLock = new object();
        
        private long _totalMessagesPublished;
        private long _totalAsyncPublishes;
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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _publisherName = $"Publisher<{typeof(TMessage).Name}>";
            
            _logger.Log(LogLevel.Debug, 
                $"Created {_publisherName}", 
                "MessagePipePublisher");
        }

        /// <inheritdoc />
        public void Publish(TMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(_publisherName);

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            using (_profiler.BeginSample($"{_publisherName}.Publish"))
            {
                try
                {
                    _publisher.Publish(message);
                    
                    lock (_syncLock)
                    {
                        _totalMessagesPublished++;
                    }

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        var messageInfo = GetMessageInfo(message);
                        _logger.Log(LogLevel.Trace,
                            $"Published message of type {typeof(TMessage).Name}{messageInfo}",
                            "MessagePipePublisher");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing message of type {typeof(TMessage).Name}: {ex.Message}",
                        "MessagePipePublisher");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public IDisposable PublishAsync(TMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(_publisherName);

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            using (_profiler.BeginSample($"{_publisherName}.PublishAsync"))
            {
                try
                {
                    // Get the UniTask
                    var task = _publisher.PublishAsync(message);
            
                    // Create a dummy disposable to track the async operation
                    var asyncTracker = new AsyncPublishTracker(task, message, _logger, _publisherName);
            
                    lock (_syncLock)
                    {
                        _totalMessagesPublished++;
                        _totalAsyncPublishes++;
                    }

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        var messageInfo = GetMessageInfo(message);
                        _logger.Log(LogLevel.Trace,
                            $"Published async message of type {typeof(TMessage).Name}{messageInfo}",
                            "MessagePipePublisher");
                    }

                    // Return the tracking disposable
                    return asyncTracker;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing async message of type {typeof(TMessage).Name}: {ex.Message}",
                        "MessagePipePublisher");
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the total number of messages published by this publisher.
        /// </summary>
        public long TotalMessagesPublished
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalMessagesPublished;
                }
            }
        }

        /// <summary>
        /// Gets the total number of async publishes initiated by this publisher.
        /// </summary>
        public long TotalAsyncPublishes
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalAsyncPublishes;
                }
            }
        }

        /// <summary>
        /// Gets formatted message information for logging purposes.
        /// </summary>
        /// <param name="message">The message to get information from.</param>
        /// <returns>A formatted string with message details, or empty if no details available.</returns>
        private string GetMessageInfo(TMessage message)
        {
            if (message == null)
                return " (null)";

            // Check if the message implements IMessage for additional info
            if (message is IMessage iMessage)
            {
                return $" (ID: {iMessage.Id}, TypeCode: {iMessage.TypeCode})";
            }

            return string.Empty;
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
                    $"Disposing {_publisherName}. Total messages published: {_totalMessagesPublished}, Async publishes: {_totalAsyncPublishes}",
                    "MessagePipePublisher");

                // MessagePipe publishers typically don't need explicit disposal,
                // but we mark ourselves as disposed to prevent further use
                _disposed = true;
            }
        }

        /// <summary>
        /// Handle for async publish operations that provides completion tracking.
        /// </summary>
        private sealed class PublishAsyncHandle : IDisposable
        {
            private readonly IDisposable _innerHandle;
            private readonly TMessage _message;
            private readonly IBurstLogger _logger;
            private readonly string _publisherName;
            private readonly DateTime _startTime;
            private bool _disposed;

            public PublishAsyncHandle(
                IDisposable innerHandle, 
                TMessage message, 
                IBurstLogger logger,
                string publisherName)
            {
                _innerHandle = innerHandle ?? throw new ArgumentNullException(nameof(innerHandle));
                _message = message;
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _publisherName = publisherName;
                _startTime = DateTime.UtcNow;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _innerHandle.Dispose();
                    
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        var duration = DateTime.UtcNow - _startTime;
                        var messageType = typeof(TMessage).Name;
                        
                        _logger.Log(LogLevel.Trace,
                            $"{_publisherName}: Async publish completed for {messageType} in {duration.TotalMilliseconds:F2}ms",
                            "MessagePipePublisher");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"{_publisherName}: Error completing async publish: {ex.Message}",
                        "MessagePipePublisher");
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