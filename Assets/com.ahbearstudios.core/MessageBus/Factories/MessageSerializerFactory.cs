using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Serialization;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Factories
{
    /// <summary>
    /// Default implementation of IMessageSerializerFactory.
    /// </summary>
    public sealed class MessageSerializerFactory : IMessageSerializerFactory
    {
        private readonly ILoggingService _logger;
        private readonly IMessageRegistry _messageRegistry;
        private readonly ISerializerMetrics _metrics;
        
        /// <summary>
        /// Initializes a new instance of the MessageSerializerFactory class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="messageRegistry">The message registry to use for type resolution.</param>
        public MessageSerializerFactory(ILoggingService logger, IMessageRegistry messageRegistry, ISerializerMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }
        
        /// <inheritdoc />
        public IMessageSerializer CreateDefaultSerializer()
        {
            return new MemoryPackMessageSerializer(_logger, _messageRegistry, _metrics);
        }
        
        /// <inheritdoc />
        public IMessageSerializer CreateBurstSerializer()
        {
            return new BurstMessageSerializer(_logger, _messageRegistry, _metrics);
        }
        
        /// <inheritdoc />
        public IMessageSerializer CreateNetworkSerializer()
        {
            // For network scenarios, prioritize MemoryPack for managed types
            // and Burst serializer for blittable types
            return CreateCompositeSerializer();
        }
        
        /// <inheritdoc />
        public IMessageSerializer CreateCompositeSerializer()
        {
            var memoryPackSerializer = new MemoryPackMessageSerializer(_logger, _messageRegistry, _metrics);
            var burstSerializer = new BurstMessageSerializer(_logger, _messageRegistry, _metrics);
            
            // Prioritize MemoryPack for managed types, Burst for blittable types
            return new CompositeMessageSerializer(_logger, memoryPackSerializer, burstSerializer);
        }
    }
}