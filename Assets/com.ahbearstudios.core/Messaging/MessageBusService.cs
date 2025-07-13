using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Production-ready implementation of the message bus service.
    /// Provides high-performance, type-safe messaging with comprehensive monitoring and error handling.
    /// </summary>
    public sealed class MessageBusService : IMessageBusService
    {
        #region Private Fields

        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;

        private readonly ConcurrentDictionary<Type, object> _publishers;
        private readonly ConcurrentDictionary<Type, object> _subscribers;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IDisposable>> _subscriptions;
        private readonly ConcurrentDictionary<Guid, MessageScope> _scopes;

        private readonly SemaphoreSlim _publishSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private volatile bool _disposed;
        private volatile HealthStatus _currentHealthStatus;

        // Statistics tracking
        private long _totalMessagesPublished;
        private long _totalMessagesProcessed;
        private long _totalMessagesFailed;
        private long _totalMemoryAllocated;
        private readonly ConcurrentDictionary<Type, MessageTypeStatistics> _messageTypeStats;

        // Performance tracking
        private readonly Timer _statisticsTimer;
        private readonly Timer _healthCheckTimer;
        private DateTime _lastStatsReset;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusService class.
        /// </summary>
        /// <param name="config">The message bus configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
        public MessageBusService(
            MessageBusConfig config,
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService,
            IPoolingService poolingService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));

            if (!_config.IsValid())
                throw new ArgumentException("Configuration is invalid", nameof(config));

            _publishers = new ConcurrentDictionary<Type, object>();
            _subscribers = new ConcurrentDictionary<Type, object>();
            _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<IDisposable>>();
            _scopes = new ConcurrentDictionary<Guid, MessageScope>();
            _messageTypeStats = new ConcurrentDictionary<Type, MessageTypeStatistics>();

            _publishSemaphore = new SemaphoreSlim(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
            _cancellationTokenSource = new CancellationTokenSource();

            _currentHealthStatus = HealthStatus.Healthy;
            _lastStatsReset = DateTime.UtcNow;

            InstanceId = Guid.NewGuid();

            // Initialize timers for periodic operations
            _statisticsTimer = new Timer(UpdateStatistics, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            _healthCheckTimer = new Timer(PerformHealthCheck, null, _config.HealthCheckInterval, _config.HealthCheckInterval);

            _logger.LogInfo($"MessageBusService '{_config.InstanceName}' initialized with ID {InstanceId}");
        }

        #endregion

        #region IMessageBusService Implementation

        /// <inheritdoc />
        public Guid InstanceId { get; }

        /// <inheritdoc />
        public MessageBusConfig Configuration => _config;

        /// <inheritdoc />
        public bool IsOperational => !_disposed && !_cancellationTokenSource.Token.IsCancellationRequested;

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"MessageBus_Publish_{typeof(TMessage).Name}");

            try
            {
                var messageType = typeof(TMessage);
                var startTime = DateTime.UtcNow;

                // Update statistics
                Interlocked.Increment(ref _totalMessagesPublished);
                UpdateMessageTypeStatistics(messageType, true, 0);

                // Get or create publisher
                var publisher = GetOrCreatePublisher<TMessage>();

                // Publish the message
                publisher.Publish(message);

                // Track performance
                var duration = DateTime.UtcNow - startTime;
                UpdateMessageTypeStatistics(messageType, true, duration.TotalMilliseconds);

                _logger.LogInfo($"Published message {typeof(TMessage).Name} with ID {message.Id}");

                // Raise event
                MessagePublished?.Invoke(this, new MessageBusEventArgs(message));
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalMessagesFailed);
                _logger.LogException(ex, $"Failed to publish message {typeof(TMessage).Name} with ID {message.Id}");

                if (_config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Message publishing failed: {ex.Message}",
                        AlertSeverity.High,
                        "MessageBusService",
                        "Publish");
                }

                MessagePublishFailed?.Invoke(this, new MessageBusErrorEventArgs(message, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"MessageBus_PublishAsync_{typeof(TMessage).Name}");
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            try
            {
                await _publishSemaphore.WaitAsync(combinedCts.Token);

                try
                {
                    var messageType = typeof(TMessage);
                    var startTime = DateTime.UtcNow;

                    // Update statistics
                    Interlocked.Increment(ref _totalMessagesPublished);
                    UpdateMessageTypeStatistics(messageType, true, 0);

                    // Get or create publisher
                    var publisher = GetOrCreatePublisher<TMessage>();

                    // Publish the message asynchronously
                    await publisher.PublishAsync(message, combinedCts.Token);

                    // Track performance
                    var duration = DateTime.UtcNow - startTime;
                    UpdateMessageTypeStatistics(messageType, true, duration.TotalMilliseconds);

                    _logger.LogInfo($"Published async message {typeof(TMessage).Name} with ID {message.Id}");

                    // Raise event
                    MessagePublished?.Invoke(this, new MessageBusEventArgs(message));
                }
                finally
                {
                    _publishSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"Async message publishing cancelled for {typeof(TMessage).Name}");
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalMessagesFailed);
                _logger.LogException(ex, $"Failed to publish async message {typeof(TMessage).Name} with ID {message.Id}");

                if (_config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Async message publishing failed: {ex.Message}",
                        AlertSeverity.High,
                        "MessageBusService",
                        "PublishAsync");
                }

                MessagePublishFailed?.Invoke(this, new MessageBusErrorEventArgs(message, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.Subscribe(handler);
                var subscriptionId = Guid.NewGuid();

                // Track subscription
                TrackSubscription(typeof(TMessage), subscription);

                _logger.LogInfo($"Created subscription {subscriptionId} for message type {typeof(TMessage).Name}");

                SubscriptionCreated?.Invoke(this, new SubscriptionEventArgs(subscriptionId, typeof(TMessage)));

                return new TrackedSubscription(subscription, subscriptionId, typeof(TMessage), this);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to subscribe to message type {typeof(TMessage).Name}");

                if (_config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Subscription creation failed: {ex.Message}",
                        AlertSeverity.Medium,
                        "MessageBusService",
                        "Subscribe");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeAsync(handler);
                var subscriptionId = Guid.NewGuid();

                // Track subscription
                TrackSubscription(typeof(TMessage), subscription);

                _logger.LogInfo($"Created async subscription {subscriptionId} for message type {typeof(TMessage).Name}");

                SubscriptionCreated?.Invoke(this, new SubscriptionEventArgs(subscriptionId, typeof(TMessage)));

                return new TrackedSubscription(subscription, subscriptionId, typeof(TMessage), this);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to subscribe async to message type {typeof(TMessage).Name}");

                if (_config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Async subscription creation failed: {ex.Message}",
                        AlertSeverity.Medium,
                        "MessageBusService",
                        "SubscribeAsync");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreatePublisher<TMessage>();
        }

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreateSubscriber<TMessage>();
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeConditional(handler, filter);
                var subscriptionId = Guid.NewGuid();

                TrackSubscription(typeof(TMessage), subscription);

                _logger.LogInfo($"Created filtered subscription {subscriptionId} for message type {typeof(TMessage).Name}");

                SubscriptionCreated?.Invoke(this, new SubscriptionEventArgs(subscriptionId, typeof(TMessage)));

                return new TrackedSubscription(subscription, subscriptionId, typeof(TMessage), this);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to create filtered subscription for message type {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, Task<bool>> filter, Func<TMessage, Task> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeConditionalAsync(handler, filter);
                var subscriptionId = Guid.NewGuid();

                TrackSubscription(typeof(TMessage), subscription);

                _logger.LogInfo($"Created async filtered subscription {subscriptionId} for message type {typeof(TMessage).Name}");

                SubscriptionCreated?.Invoke(this, new SubscriptionEventArgs(subscriptionId, typeof(TMessage)));

                return new TrackedSubscription(subscription, subscriptionId, typeof(TMessage), this);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to create async filtered subscription for message type {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeWithPriority(handler, minPriority);
                var subscriptionId = Guid.NewGuid();

                TrackSubscription(typeof(TMessage), subscription);

                _logger.LogInfo($"Created priority subscription {subscriptionId} for message type {typeof(TMessage).Name} with min priority {minPriority}");

                SubscriptionCreated?.Invoke(this, new SubscriptionEventArgs(subscriptionId, typeof(TMessage)));

                return new TrackedSubscription(subscription, subscriptionId, typeof(TMessage), this);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to create priority subscription for message type {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IMessageScope CreateScope()
        {
            ThrowIfDisposed();

            var scope = new MessageScope(this, _logger);
            _scopes.TryAdd(scope.Id, scope);

            _logger.LogInfo($"Created message scope {scope.Id}");

            return scope;
        }

        /// <inheritdoc />
        public MessageBusStatistics GetStatistics()
        {
            var currentTime = DateTime.UtcNow.Ticks;
            var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
            var messagesPerSecond = timeSinceReset.TotalSeconds > 0 
                ? _totalMessagesProcessed / timeSinceReset.TotalSeconds 
                : 0;

            var avgProcessingTime = _messageTypeStats.Values
                .Where(s => s.ProcessedCount > 0)
                .Select(s => s.TotalProcessingTime / s.ProcessedCount)
                .DefaultIfEmpty(0)
                .Average();

            var peakProcessingTime = _messageTypeStats.Values
                .Select(s => s.PeakProcessingTime)
                .DefaultIfEmpty(0)
                .Max();

            return new MessageBusStatistics(
                _totalMessagesPublished,
                _totalMessagesProcessed,
                _totalMessagesFailed,
                GetActiveSubscriberCount(),
                GetCurrentQueueDepth(),
                avgProcessingTime,
                peakProcessingTime,
                messagesPerSecond,
                currentTime,
                GetMessagesInRetryCount(),
                GetDeadLetterQueueSize(),
                _totalMemoryAllocated,
                _config.InstanceName);
        }

        /// <inheritdoc />
        public void ClearMessageHistory()
        {
            ThrowIfDisposed();

            _logger.LogInfo("Clearing message history and resetting statistics");

            Interlocked.Exchange(ref _totalMessagesPublished, 0);
            Interlocked.Exchange(ref _totalMessagesProcessed, 0);
            Interlocked.Exchange(ref _totalMessagesFailed, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);

            _messageTypeStats.Clear();
            _lastStatsReset = DateTime.UtcNow;

            _logger.LogInfo("Message history cleared successfully");
        }

        /// <inheritdoc />
        public HealthStatus GetHealthStatus()
        {
            return _currentHealthStatus;
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MessageBusEventArgs> MessagePublished;

        /// <inheritdoc />
        public event EventHandler<MessageBusErrorEventArgs> MessagePublishFailed;

        /// <inheritdoc />
        public event EventHandler<SubscriptionEventArgs> SubscriptionCreated;

        /// <inheritdoc />
        public event EventHandler<SubscriptionEventArgs> SubscriptionDisposed;

        /// <inheritdoc />
        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        #endregion

        #region Private Methods

        private IMessagePublisher<TMessage> GetOrCreatePublisher<TMessage>() where TMessage : IMessage
        {
            return (IMessagePublisher<TMessage>)_publishers.GetOrAdd(typeof(TMessage), _ =>
            {
                var publisher = new MessagePublisher<TMessage>(_config, _logger, _profilerService, _poolingService);
                _logger.LogInfo($"Created publisher for message type {typeof(TMessage).Name}");
                return publisher;
            });
        }

        private IMessageSubscriber<TMessage> GetOrCreateSubscriber<TMessage>() where TMessage : IMessage
        {
            return (IMessageSubscriber<TMessage>)_subscribers.GetOrAdd(typeof(TMessage), _ =>
            {
                var subscriber = new MessageSubscriber<TMessage>(_config, _logger, _profilerService, this);
                _logger.LogInfo($"Created subscriber for message type {typeof(TMessage).Name}");
                return subscriber;
            });
        }

        private void TrackSubscription(Type messageType, IDisposable subscription)
        {
            _subscriptions.AddOrUpdate(messageType,
                new ConcurrentBag<IDisposable> { subscription },
                (_, existing) =>
                {
                    existing.Add(subscription);
                    return existing;
                });
        }

        private void UpdateMessageTypeStatistics(Type messageType, bool success, double processingTimeMs)
        {
            _messageTypeStats.AddOrUpdate(messageType,
                new MessageTypeStatistics(success ? 1 : 0, success ? 0 : 1, processingTimeMs, processingTimeMs),
                (_, existing) => existing.Update(success, processingTimeMs));
        }

        private int GetActiveSubscriberCount()
        {
            return _subscriptions.Values.Sum(bag => bag.Count);
        }

        private int GetCurrentQueueDepth()
        {
            // This would be implemented based on the underlying message infrastructure
            // For now, return 0 as a placeholder
            return 0;
        }

        private int GetMessagesInRetryCount()
        {
            // This would be implemented based on retry mechanism
            // For now, return 0 as a placeholder
            return 0;
        }

        private int GetDeadLetterQueueSize()
        {
            // This would be implemented based on dead letter queue mechanism
            // For now, return 0 as a placeholder
            return 0;
        }

        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Update memory allocation tracking
                var currentMemory = GC.GetTotalMemory(false);
                Interlocked.Exchange(ref _totalMemoryAllocated, currentMemory);

                // Update processed count based on successful operations
                var successfulOperations = _messageTypeStats.Values.Sum(s => s.ProcessedCount);
                Interlocked.Exchange(ref _totalMessagesProcessed, successfulOperations);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to update statistics");
            }
        }

        private void PerformHealthCheck(object state)
        {
            if (_disposed) return;

            try
            {
                var statistics = GetStatistics();
                var previousStatus = _currentHealthStatus;
                var newStatus = DetermineHealthStatus(statistics);

                if (newStatus != previousStatus)
                {
                    _currentHealthStatus = newStatus;
                    _logger.LogInfo($"Health status changed from {previousStatus} to {newStatus}");

                    HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs(
                        previousStatus, newStatus, "Periodic health check"));

                    if (_config.AlertsEnabled && newStatus != HealthStatus.Healthy)
                    {
                        _alertService.RaiseAlert(
                            $"MessageBus health status changed to {newStatus}",
                            newStatus == HealthStatus.Unhealthy ? AlertSeverity.Critical : AlertSeverity.Warning,
                            "MessageBusService",
                            "HealthCheck");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to perform health check");
            }
        }

        private HealthStatus DetermineHealthStatus(MessageBusStatistics statistics)
        {
            if (!IsOperational)
                return HealthStatus.Unhealthy;

            if (statistics.SuccessRate < 0.85)
                return HealthStatus.Unhealthy;

            if (statistics.CurrentQueueDepth > _config.MaxQueueSize * 0.9)
                return HealthStatus.Unhealthy;

            if (statistics.SuccessRate < 0.95 || 
                statistics.CurrentQueueDepth > _config.MaxQueueSize * 0.5 ||
                statistics.AverageProcessingTimeMs > 100)
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }

        internal void OnSubscriptionDisposed(Guid subscriptionId, Type messageType, string reason)
        {
            _logger.LogInfo($"Subscription {subscriptionId} disposed: {reason}");
            SubscriptionDisposed?.Invoke(this, new SubscriptionEventArgs(subscriptionId, messageType));
        }

        internal void OnScopeDisposed(Guid scopeId)
        {
            _scopes.TryRemove(scopeId, out _);
            _logger.LogInfo($"Message scope {scopeId} disposed");
        }

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

            _logger.LogInfo($"Disposing MessageBusService '{_config.InstanceName}'");

            try
            {
                // Cancel all operations
                _cancellationTokenSource.Cancel();

                // Dispose all scopes
                foreach (var scope in _scopes.Values)
                {
                    scope?.Dispose();
                }
                _scopes.Clear();

                // Dispose all subscriptions
                foreach (var subscriptionBag in _subscriptions.Values)
                {
                    foreach (var subscription in subscriptionBag)
                    {
                        subscription?.Dispose();
                    }
                }
                _subscriptions.Clear();

                // Dispose publishers and subscribers
                foreach (var publisher in _publishers.Values)
                {
                    (publisher as IDisposable)?.Dispose();
                }
                _publishers.Clear();

                foreach (var subscriber in _subscribers.Values)
                {
                    (subscriber as IDisposable)?.Dispose();
                }
                _subscribers.Clear();

                // Dispose timers
                _statisticsTimer?.Dispose();
                _healthCheckTimer?.Dispose();

                // Dispose semaphore
                _publishSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                _disposed = true;

                _logger.LogInfo($"MessageBusService '{_config.InstanceName}' disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error during MessageBusService disposal");
            }
        }

        #endregion

        #region Helper Classes

        private readonly struct MessageTypeStatistics
        {
            public readonly long ProcessedCount;
            public readonly long FailedCount;
            public readonly double TotalProcessingTime;
            public readonly double PeakProcessingTime;

            public MessageTypeStatistics(long processedCount, long failedCount, double totalProcessingTime, double peakProcessingTime)
            {
                ProcessedCount = processedCount;
                FailedCount = failedCount;
                TotalProcessingTime = totalProcessingTime;
                PeakProcessingTime = peakProcessingTime;
            }

            public MessageTypeStatistics Update(bool success, double processingTime)
            {
                return new MessageTypeStatistics(
                    success ? ProcessedCount + 1 : ProcessedCount,
                    success ? FailedCount : FailedCount + 1,
                    TotalProcessingTime + processingTime,
                    Math.Max(PeakProcessingTime, processingTime));
            }
        }

        private sealed class TrackedSubscription : IDisposable
        {
            private readonly IDisposable _innerSubscription;
            private readonly Guid _subscriptionId;
            private readonly Type _messageType;
            private readonly MessageBusService _messageBusService;
            private volatile bool _disposed;

            public TrackedSubscription(IDisposable innerSubscription, Guid subscriptionId, Type messageType, MessageBusService messageBusService)
            {
                _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
                _subscriptionId = subscriptionId;
                _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
                _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            }

            public void Dispose()
            {
                if (_disposed) return;

                _innerSubscription?.Dispose();
                _messageBusService.OnSubscriptionDisposed(_subscriptionId, _messageType, "Explicit disposal");
                _disposed = true;
            }
        }

        #endregion
    }
}