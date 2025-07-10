using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IPublishingStatistics that manages publishing metrics.
    /// </summary>
    internal sealed class PublishingStatistics : IPublishingStatistics
    {
        private readonly IBurstLogger _logger;
        private readonly string _publisherName;
        private readonly object _syncLock = new object();

        private long _totalMessagesPublished;
        private long _totalAsyncPublishes;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the PublishingStatistics class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="publisherName">The name of the publisher for logging purposes.</param>
        public PublishingStatistics(IBurstLogger logger, string publisherName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisherName = publisherName ?? throw new ArgumentNullException(nameof(publisherName));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void RecordSyncPublish()
        {
            lock (_syncLock)
            {
                _totalMessagesPublished++;
            }
        }

        /// <inheritdoc />
        public void RecordAsyncPublish()
        {
            lock (_syncLock)
            {
                _totalMessagesPublished++;
                _totalAsyncPublishes++;
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
                    $"Disposing {_publisherName}. Total messages published: {_totalMessagesPublished}, Async publishes: {_totalAsyncPublishes}",
                    "MessagePipeKeyedPublisher");

                _disposed = true;
            }
        }
    }
}