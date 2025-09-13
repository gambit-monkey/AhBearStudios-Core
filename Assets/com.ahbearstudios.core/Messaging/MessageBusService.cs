using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Orchestrator implementation of the message bus service.
    /// Coordinates specialized messaging services following single responsibility principle.
    /// Delegates operations to focused services: Publishing, Subscription, Monitoring, Health, Retry, and Dead Letter Queue.
    /// Maintains backward compatibility while providing improved maintainability and testability.
    /// </summary>
    public sealed class MessageBusService : IMessageBusService
    {
        #region Private Fields

        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        
        // Specialized services
        private readonly IMessagePublishingService _publishingService;
        private readonly IMessageSubscriptionService _subscriptionService;
        private readonly IMessageBusMonitoringService _monitoringService;
        private readonly IMessageBusHealthService _healthService;
        private readonly IMessageRetryService _retryService;
        private readonly IDeadLetterQueueService _deadLetterQueueService;

        // State management
        private volatile bool _disposed;
        private readonly object _disposeLock = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusService class.
        /// </summary>
        /// <param name="publishingService">The message publishing service</param>
        /// <param name="subscriptionService">The message subscription service</param>
        /// <param name="monitoringService">The message bus monitoring service</param>
        /// <param name="healthService">The message bus health service</param>
        /// <param name="retryService">The message retry service</param>
        /// <param name="deadLetterQueueService">The dead letter queue service</param>
        /// <param name="logger">The logging service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageBusService(
            IMessagePublishingService publishingService,
            IMessageSubscriptionService subscriptionService,
            IMessageBusMonitoringService monitoringService,
            IMessageBusHealthService healthService,
            IMessageRetryService retryService,
            IDeadLetterQueueService deadLetterQueueService,
            ILoggingService logger,
            IProfilerService profilerService)
        {
            _publishingService = publishingService ?? throw new ArgumentNullException(nameof(publishingService));
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _deadLetterQueueService = deadLetterQueueService ?? throw new ArgumentNullException(nameof(deadLetterQueueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? NullProfilerService.Instance;

            _logger.LogInfo("MessageBusService orchestrator initialized with specialized services");
        }

        #endregion

        #region IMessageBusService Implementation - Core Publishing Operations

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.PublishMessage");
            
            try
            {
                _publishingService.PublishMessage(message);
                _logger.LogDebug($"Published message {typeof(TMessage).Name} with ID {message.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to publish message {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.PublishMessageAsync");
            
            try
            {
                await _publishingService.PublishMessageAsync(message, cancellationToken);
                _logger.LogDebug($"Published async message {typeof(TMessage).Name} with ID {message.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to publish async message {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.PublishBatch");
            
            try
            {
                _publishingService.PublishBatch(messages);
                _logger.LogDebug($"Published batch of {messages.Length} messages of type {typeof(TMessage).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to publish batch of {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.PublishBatchAsync");
            
            try
            {
                await _publishingService.PublishBatchAsync(messages, cancellationToken);
                _logger.LogDebug($"Published async batch of {messages.Length} messages of type {typeof(TMessage).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to publish async batch of {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Core Subscription Operations

        /// <inheritdoc />
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.SubscribeToMessage");
            
            try
            {
                var subscription = _subscriptionService.SubscribeToMessage(handler);
                _logger.LogDebug($"Subscribed to message type {typeof(TMessage).Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to subscribe to message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.SubscribeToMessageAsync");
            
            try
            {
                var subscription = _subscriptionService.SubscribeToMessageAsync(handler);
                _logger.LogDebug($"Subscribed to async message type {typeof(TMessage).Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to subscribe to async message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Advanced Operations

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.GetPublisher");
            
            try
            {
                return _publishingService.GetPublisher<TMessage>();
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to get publisher for message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.GetSubscriber");
            
            try
            {
                return _subscriptionService.GetSubscriber<TMessage>();
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to get subscriber for message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Filtering and Routing

        /// <inheritdoc />
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.SubscribeWithFilter");
            
            try
            {
                var subscription = _subscriptionService.SubscribeWithFilter(filter, handler);
                _logger.LogDebug($"Subscribed with filter to message type {typeof(TMessage).Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to subscribe with filter to message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.SubscribeWithFilterAsync");
            
            try
            {
                var subscription = _subscriptionService.SubscribeWithFilterAsync(filter, handler);
                _logger.LogDebug($"Subscribed with async filter to message type {typeof(TMessage).Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to subscribe with async filter to message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.SubscribeWithPriority");
            
            try
            {
                var subscription = _subscriptionService.SubscribeWithPriority(handler, minPriority);
                _logger.LogDebug($"Subscribed with priority {minPriority} to message type {typeof(TMessage).Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to subscribe with priority to message type {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Scoped Subscriptions

        /// <inheritdoc />
        public IMessageScope CreateScope()
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.CreateScope");
            
            try
            {
                var scope = _subscriptionService.CreateScope();
                _logger.LogDebug("Created message subscription scope");
                return scope;
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to create message scope", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Diagnostics and Management

        /// <inheritdoc />
        public MessageBusStatistics GetStatistics()
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.GetStatistics");
            
            try
            {
                return _monitoringService.GetStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to get message bus statistics", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public void ClearMessageHistory()
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.ClearMessageHistory");
            
            try
            {
                _monitoringService.ClearStatistics();
                _logger.LogInfo("Cleared message bus history and statistics");
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to clear message history", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public HealthStatus GetHealthStatus()
        {
            ThrowIfDisposed();

            try
            {
                return _healthService.GetOverallHealthStatus();
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to get health status", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.CheckHealthAsync");
            
            try
            {
                var healthStatus = await _healthService.CheckOverallHealthAsync(cancellationToken);
                _logger.LogDebug($"Health check completed with status: {healthStatus}");
                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to perform health check", ex);
                throw;
            }
        }

        #endregion

        #region IMessageBusService Implementation - Circuit Breaker Operations

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();

            try
            {
                return _publishingService.GetCircuitBreakerState<TMessage>();
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to get circuit breaker state for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();

            using var profilerScope = _profilerService.BeginScope("MessageBus.ResetCircuitBreaker");
            
            try
            {
                _publishingService.ResetCircuitBreaker<TMessage>();
                _logger.LogInfo($"Reset circuit breaker for message type {typeof(TMessage).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to reset circuit breaker for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws an exception if the service has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBusService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message bus service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_disposeLock)
            {
                if (_disposed) return;

                _logger.LogInfo("Disposing MessageBusService orchestrator");

                try
                {
                    // Dispose all specialized services
                    _publishingService?.Dispose();
                    _subscriptionService?.Dispose();
                    _monitoringService?.Dispose();
                    _healthService?.Dispose();
                    _retryService?.Dispose();
                    _deadLetterQueueService?.Dispose();

                    _disposed = true;
                    _logger.LogInfo("MessageBusService orchestrator disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogException("Error during MessageBusService disposal", ex);
                }
            }
        }

        #endregion
    }
}