using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.MessageBuses.MessagePipe
{
    /// <summary>
    /// MessagePipe implementation of the IMessageSubscriber interface.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    internal sealed class MessagePipeSubscriber<TMessage> : IMessageSubscriber<TMessage>
    {
        private readonly ISubscriber<TMessage> _subscriber;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ProfilerTag _subscribeTag;
        
        /// <summary>
        /// Initializes a new instance of the MessagePipeSubscriber class.
        /// </summary>
        /// <param name="subscriber">The MessagePipe subscriber to wrap.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessagePipeSubscriber(ISubscriber<TMessage> subscriber, IBurstLogger logger, IProfiler profiler)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _subscribeTag = new ProfilerTag(new ProfilerCategory("MessageBus"), $"Subscribe_{typeof(TMessage).Name}");
        }
        
        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler)
        {
            using var scope = _profiler.BeginScope(_subscribeTag);
            
            _logger.Log(LogLevel.Debug, $"Subscribing to messages of type {typeof(TMessage).Name}", "MessageBus");
            return _subscriber.Subscribe(message =>
            {
                using var handlerScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"Handle_{typeof(TMessage).Name}"));
                handler(message);
            });
        }
        
        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            using var scope = _profiler.BeginScope(_subscribeTag);
            
            _logger.Log(LogLevel.Debug, $"Subscribing to messages of type {typeof(TMessage).Name} with filter", "MessageBus");
            return _subscriber.Subscribe(message =>
            {
                using var filterScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"Filter_{typeof(TMessage).Name}"));
                if (filter(message))
                {
                    using var handlerScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"Handle_{typeof(TMessage).Name}"));
                    handler(message);
                }
            });
        }
    }
}