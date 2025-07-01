using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using AhBearStudios.Core.MessageBus.Services;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Factories
{
    /// <summary>
    /// Default implementation of IMessageDeliveryServiceFactory that creates delivery services
    /// with message bus integration for communication instead of traditional events.
    /// </summary>
    public sealed class MessageDeliveryServiceFactory : IMessageDeliveryServiceFactory
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _logger;
        private readonly IProfiler _profiler;
        private readonly DeliveryServiceConfiguration _configuration;
        
        /// <summary>
        /// Initializes a new instance of the MessageDeliveryServiceFactory class.
        /// </summary>
        /// <param name="messageBusService">The message bus service to use for message delivery and communication.</param>
        /// <param name="logger">The logging service to use for operational logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring and metrics.</param>
        /// <param name="configuration">Configuration for delivery services. If null, default configuration is used.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public MessageDeliveryServiceFactory(
            IMessageBusService messageBusService, 
            ILoggingService logger, 
            IProfiler profiler,
            DeliveryServiceConfiguration configuration = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _configuration = configuration ?? CreateDefaultConfiguration();
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateReliableDeliveryService()
        {
            using var scope = _profiler.BeginSample("CreateReliableDeliveryService");
            
            _logger.LogInfo("Creating reliable message delivery service");
            
            var service = new ReliableMessageDeliveryService(
                _messageBusService, 
                _logger, 
                _profiler, 
                _configuration);
                
            // Publish service creation message
            PublishServiceCreatedMessage("ReliableDeliveryService", service);
            
            return service;
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateFireAndForgetService()
        {
            using var scope = _profiler.BeginSample("CreateFireAndForgetService");
            
            _logger.LogInfo("Creating fire-and-forget message delivery service");
            
            var service = new FireAndForgetDeliveryService(
                _messageBusService, 
                _logger, 
                _profiler, 
                _configuration);
                
            // Publish service creation message
            PublishServiceCreatedMessage("FireAndForgetService", service);
            
            return service;
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateBatchOptimizedService()
        {
            using var scope = _profiler.BeginSample("CreateBatchOptimizedService");
            
            _logger.LogInfo("Creating batch-optimized message delivery service");
            
            var batchConfig = CreateBatchOptimizedConfiguration();
            
            var service = new BatchOptimizedDeliveryService(
                _messageBusService, 
                _logger, 
                _profiler, 
                batchConfig);
                
            // Publish service creation message
            PublishServiceCreatedMessage("BatchOptimizedService", service);
            
            return service;
        }
        
        /// <summary>
        /// Creates the default configuration for delivery services.
        /// </summary>
        /// <returns>A new default delivery service configuration.</returns>
        private static DeliveryServiceConfiguration CreateDefaultConfiguration()
        {
            return new DeliveryServiceConfiguration
            {
                MaxConcurrentDeliveries = 100,
                ProcessingInterval = TimeSpan.FromMilliseconds(50),
                DefaultTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttempts = 3,
                RetryBackoffMultiplier = 2.0f,
                EnableMetrics = true,
                EnableLogging = true,
                EnableBatching = true,
                BatchSize = 10
            };
        }
        
        /// <summary>
        /// Creates the batch-optimized configuration based on the base configuration.
        /// </summary>
        /// <returns>A new batch-optimized configuration.</returns>
        private BatchOptimizedConfiguration CreateBatchOptimizedConfiguration()
        {
            return new BatchOptimizedConfiguration
            {
                MaxBatchSize = _configuration.MaxConcurrentDeliveries,
                BatchInterval = _configuration.ProcessingInterval,
                FlushInterval = TimeSpan.FromMilliseconds(_configuration.ProcessingInterval.TotalMilliseconds * 4),
                ConfirmationTimeout = _configuration.DefaultTimeout,
                ImmediateProcessingForReliable = true,
                EnableAdaptiveBatching = true,
                TargetThroughput = 1000,
                MaxQueueDepth = _configuration.MaxConcurrentDeliveries * 2,
                EnableCompression = false,
                CompressionThreshold = 1024
            };
        }
        
        /// <summary>
        /// Publishes a service creation message to notify other components.
        /// </summary>
        /// <param name="serviceTypeName">The type name of the created service.</param>
        /// <param name="service">The created delivery service instance.</param>
        private void PublishServiceCreatedMessage(string serviceTypeName, IMessageDeliveryService service)
        {
            try
            {
                var creationMessage = new DeliveryServiceCreatedMessage
                {
                    Id = Guid.NewGuid(),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = GetMessageTypeCode<DeliveryServiceCreatedMessage>(),
                    ServiceId = Guid.NewGuid(), // In a real implementation, this would come from the service
                    ServiceTypeName = serviceTypeName,
                    ServiceName = service.Name,
                    IsActive = service.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    Configuration = _configuration
                };
                
                _messageBusService.PublishMessage(creationMessage);
                
                _logger.LogDebug($"Published service creation message for {serviceTypeName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to publish service creation message for {serviceTypeName}: {ex.Message}");
                // Don't throw - service creation should not fail due to messaging issues
            }
        }
        
        /// <summary>
        /// Gets the type code for a message type from the message registry.
        /// </summary>
        /// <typeparam name="TMessage">The message type to get the code for.</typeparam>
        /// <returns>The type code for the message type.</returns>
        private ushort GetMessageTypeCode<TMessage>() where TMessage : IMessage
        {
            try
            {
                var registry = _messageBusService.GetMessageRegistry();
                return registry.GetTypeCode<TMessage>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get type code for {typeof(TMessage).Name}: {ex.Message}");
                return 0; // Default type code if registry lookup fails
            }
        }
    }
}