using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.MessageBuses.MessagePipe
{
    /// <summary>
    /// MessagePipe implementation of the IMessagePublisher interface.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    internal sealed class MessagePipePublisher<TMessage> : IMessagePublisher<TMessage>
    {
        private readonly IPublisher<TMessage> _publisher;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ProfilerTag _publishTag;
        
        /// <summary>
        /// Initializes a new instance of the MessagePipePublisher class.
        /// </summary>
        /// <param name="publisher">The MessagePipe publisher to wrap.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessagePipePublisher(IPublisher<TMessage> publisher, IBurstLogger logger, IProfiler profiler)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _publishTag = new ProfilerTag(new ProfilerCategory("MessageBus"), $"Publish_{typeof(TMessage).Name}");
        }
        
        /// <inheritdoc />
        public void Publish(TMessage message)
        {
            using var scope = _profiler.BeginScope(_publishTag);
            
            _logger.Log(LogLevel.Debug, $"Publishing message of type {typeof(TMessage).Name}", "MessageBus");
            _publisher.Publish(message);
        }
        
        /// <inheritdoc />
        public IDisposable PublishAsync(TMessage message)
        {
            using var scope = _profiler.BeginScope(_publishTag);
            
            _logger.Log(LogLevel.Debug, $"Publishing async message of type {typeof(TMessage).Name}", "MessageBus");
            return _publisher.PublishAsync(message);
        }
    }
}