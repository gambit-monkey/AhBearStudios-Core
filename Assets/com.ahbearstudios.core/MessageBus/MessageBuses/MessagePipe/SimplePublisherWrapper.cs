using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of ISimplePublisherWrapper that provides profiling, logging, and error handling.
    /// </summary>
    internal sealed class SimplePublisherWrapper : ISimplePublisherWrapper
    {
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _publisherName;

        /// <summary>
        /// Initializes a new instance of the SimplePublisherWrapper class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        /// <param name="publisherName">The name of the publisher for logging purposes.</param>
        public SimplePublisherWrapper(IBurstLogger logger, IProfiler profiler, string publisherName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _publisherName = publisherName ?? throw new ArgumentNullException(nameof(publisherName));
        }

        /// <inheritdoc />
        public void WrapSyncPublish<TMessage>(
            TMessage message,
            Action<TMessage> publish,
            IPublishingStatistics statistics)
        {
            using (_profiler.BeginSample($"{_publisherName}.Publish"))
            {
                try
                {
                    publish(message);
                    statistics.RecordSyncPublish();

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
        public IDisposable WrapAsyncPublish<TMessage>(
            TMessage message,
            Func<TMessage, UniTask> publishAsync,
            IPublishingStatistics statistics)
        {
            using (_profiler.BeginSample($"{_publisherName}.PublishAsync"))
            {
                try
                {
                    var task = publishAsync(message);
                    statistics.RecordAsyncPublish();

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        var messageInfo = GetMessageInfo(message);
                        _logger.Log(LogLevel.Trace,
                            $"Published async message of type {typeof(TMessage).Name}{messageInfo}",
                            "MessagePipePublisher");
                    }

                    return new AsyncPublishTracker<TMessage>(task, message, _logger, _publisherName);
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
        /// Gets formatted message information for logging purposes.
        /// </summary>
        /// <param name="message">The message to get information from.</param>
        /// <returns>A formatted string with message details, or empty if no details available.</returns>
        private string GetMessageInfo<TMessage>(TMessage message)
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
    }
}