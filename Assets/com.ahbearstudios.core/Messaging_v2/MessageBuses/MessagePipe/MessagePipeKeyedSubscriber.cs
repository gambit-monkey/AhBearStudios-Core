// MessagePipeKeyedSubscriber.cs
using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.MessageBuses.MessagePipe
{
    /// <summary>
    /// MessagePipe implementation of the IKeyedMessageSubscriber interface.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    internal sealed class MessagePipeKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>
    {
        private readonly ISubscriber<TKey, TMessage> _subscriber;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ProfilerTag _subscribeTag;
        
        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedSubscriber class.
        /// </summary>
        /// <param name="subscriber">The MessagePipe keyed subscriber to wrap.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessagePipeKeyedSubscriber(ISubscriber<TKey, TMessage> subscriber, IBurstLogger logger, IProfiler profiler)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _subscribeTag = new ProfilerTag(new ProfilerCategory("MessageBus"), $"SubscribeKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}");
        }
        
        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler)
        {
            using var scope = _profiler.BeginScope(_subscribeTag);
            
            _logger.Log(LogLevel.Debug, $"Subscribing to keyed messages of type {typeof(TMessage).Name} with key {key}", "MessageBus");
            return _subscriber.Subscribe(key, message =>
            {
                using var handlerScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"HandleKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}"));
                handler(message);
            });
        }
        
        /// <inheritdoc />
        public IDisposable Subscribe(Action<TKey, TMessage> handler)
        {
            using var scope = _profiler.BeginScope(_subscribeTag);
            
            _logger.Log(LogLevel.Debug, $"Subscribing to all keyed messages of type {typeof(TMessage).Name}", "MessageBus");
            return _subscriber.Subscribe((key, message) =>
            {
                using var handlerScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"HandleAllKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}"));
                handler(key, message);
            });
        }
        
        /// <inheritdoc />
        public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            using var scope = _profiler.BeginScope(_subscribeTag);
            
            _logger.Log(LogLevel.Debug, $"Subscribing to keyed messages of type {typeof(TMessage).Name} with key {key} and filter", "MessageBus");
            return _subscriber.Subscribe(key, message =>
            {
                using var filterScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"FilterKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}"));
                if (filter(message))
                {
                    using var handlerScope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBus"), $"HandleKeyed_{typeof(TKey).Name}_{typeof(TMessage).Name}"));
                    handler(message);
                }
            });
        }
    }
}