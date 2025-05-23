using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Implementation of IMessageHandlerContext that provides logging and profiling utilities.
    /// </summary>
    internal sealed class MessageHandlerContext : IMessageHandlerContext
    {
        private readonly ProfilerTag _handlerTag;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessageHandlerContext class.
        /// </summary>
        /// <param name="messageBus">The message bus instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="profiler">The profiler instance.</param>
        /// <param name="handlerType">The type of the handler for profiling purposes.</param>
        public MessageHandlerContext(IMessageBus messageBus, IBurstLogger logger, IProfiler profiler, Type handlerType)
        {
            MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _handlerTag = new ProfilerTag(new ProfilerCategory("MessageHandler"), handlerType.Name);
            
            Logger.Log(LogLevel.Debug, 
                $"Registered message handler {handlerType.Name}", 
                "MessageHandler");
        }

        /// <inheritdoc />
        public IMessageBus MessageBus { get; }
        
        /// <inheritdoc />
        public IBurstLogger Logger { get; }
        
        /// <inheritdoc />
        public IProfiler Profiler { get; }

        /// <inheritdoc />
        public void ExecuteWithProfiling(string sampleName, Action action)
        {
            using var scope = Profiler.BeginScope(_handlerTag);
            action();
        }

        /// <inheritdoc />
        public IDisposable SubscribeKeyed<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            // Use extension methods or direct access to keyed subscriber
            // This assumes the MessageBus has extension methods for keyed subscriptions
            return MessageBus.Subscribe<TKey, TMessage>(key, handler);
        }

        /// <inheritdoc />
        public IDisposable SubscribeKeyedAll<TKey, TMessage>(Action<TKey, TMessage> handler)
        {
            // Use extension methods or direct access to keyed subscriber  
            // This assumes the MessageBus has extension methods for keyed subscriptions
            return MessageBus.Subscribe<TKey, TMessage>(handler);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            Logger.Log(LogLevel.Debug, 
                $"Disposed message handler context", 
                "MessageHandler");
            
            _disposed = true;
        }
    }
}