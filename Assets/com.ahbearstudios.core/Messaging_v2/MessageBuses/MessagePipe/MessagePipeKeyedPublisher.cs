using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Implementation
{
    /// <summary>
    /// MessagePipe implementation of the IKeyedMessagePublisher interface.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    internal sealed class MessagePipeKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>
    {
        private readonly IPublisher<TKey, TMessage> _publisher;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ProfilerTag _publishTag;
        
        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedPublisher class.
        /// </summary>
        /// <param name="publisher">The MessagePipe keyed publisher to wrap.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessagePipeKeyedPublisher(IPublisher<TKey, TMessage> publisher, IBurstLogger logger, IProfiler profiler)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _publishTag = new ProfilerTag(new ProfilerCategory("MessageBus"), $"PublishKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}");
        }
        
        /// <inheritdoc />
        public void Publish(TKey key, TMessage message)
        {
            using var scope = _profiler.BeginScope(_publishTag);
            
            _logger.Log(LogLevel.Debug, $"Publishing keyed message of type {typeof(TMessage).Name} with key type {typeof(TKey).Name}", "MessageBus");
            _publisher.Publish(key, message);
        }
        
        /// <inheritdoc />
        public IDisposable PublishAsync(TKey key, TMessage message)
        {
            using var scope = _profiler.BeginScope(_publishTag);
            
            _logger.Log(LogLevel.Debug, $"Publishing async keyed message of type {typeof(TMessage).Name} with key type {typeof(TKey).Name}", "MessageBus");
            return _publisher.PublishAsync(key, message);
        }
    }
}