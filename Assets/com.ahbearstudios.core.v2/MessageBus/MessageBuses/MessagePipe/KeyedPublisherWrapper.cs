using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedPublisherWrapper that provides profiling, logging, and error handling.
    /// </summary>
    internal sealed class KeyedPublisherWrapper : IKeyedPublisherWrapper
    {
        private readonly IBurstLogger _logger;
        private readonly IProfilerService _profilerService;
        private readonly string _publisherName;

        /// <summary>
        /// Initializes a new instance of the KeyedPublisherWrapper class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profilerService">The profiler for performance monitoring.</param>
        /// <param name="publisherName">The name of the publisher for logging purposes.</param>
        public KeyedPublisherWrapper(IBurstLogger logger, IProfilerService profilerService, string publisherName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _publisherName = publisherName ?? throw new ArgumentNullException(nameof(publisherName));
        }

        /// <inheritdoc />
        public void WrapSyncPublish<TKey, TMessage>(
            TKey key,
            TMessage message,
            Action<TKey, TMessage> publish,
            IPublishingStatistics statistics)
        {
            using (_profilerService.BeginSample($"{_publisherName}.Publish"))
            {
                try
                {
                    publish(key, message);
                    statistics.RecordSyncPublish();

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace,
                            $"Published message with key '{key}' of type {typeof(TMessage).Name}",
                            "MessagePipeKeyedPublisher");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing keyed message with key '{key}': {ex.Message}",
                        "MessagePipeKeyedPublisher");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable WrapAsyncPublish<TKey, TMessage>(
            TKey key,
            TMessage message,
            Func<TKey, TMessage, UniTask> publishAsync,
            IPublishingStatistics statistics)
        {
            using (_profilerService.BeginSample($"{_publisherName}.PublishAsync"))
            {
                try
                {
                    var task = publishAsync(key, message);
                    statistics.RecordAsyncPublish();

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace,
                            $"Published async message with key '{key}' of type {typeof(TMessage).Name}",
                            "MessagePipeKeyedPublisher");
                    }

                    return new AsyncPublishTracker<TMessage>(task, message, _logger, _publisherName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing async keyed message with key '{key}': {ex.Message}",
                        "MessagePipeKeyedPublisher");
                    throw;
                }
            }
        }
    }
}