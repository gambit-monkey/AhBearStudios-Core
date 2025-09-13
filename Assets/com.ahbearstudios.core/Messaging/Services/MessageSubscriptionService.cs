using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Filters;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Service for handling message subscription operations.
    /// Focused solely on subscription responsibilities with comprehensive error handling and monitoring.
    /// </summary>
    public sealed class MessageSubscriptionService : IMessageSubscriptionService
    {
        #region Private Fields

        private readonly MessageSubscriptionConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        private readonly IMessageBusAdapter _messageBusAdapter;
        
        // Core collections
        private readonly ConcurrentDictionary<Type, object> _subscribers;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IDisposable>> _subscriptions;
        private readonly ConcurrentDictionary<Guid, IMessageScope> _scopes;
        private readonly ConcurrentDictionary<Type, SubscriberStatistics> _messageTypeStats;
        private readonly ConcurrentDictionary<Guid, ScopeStatistics> _scopeStats;
        
        // Threading and synchronization
        private readonly SemaphoreSlim _subscriptionSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ReaderWriterLockSlim _healthStatusLock;
        
        // State management
        private volatile bool _disposed;
        private volatile HealthStatus _currentHealthStatus;
        
        // Statistics tracking
        private long _totalSubscriptionsCreated;
        private long _totalSubscriptionsDisposed;
        private long _totalMessagesProcessed;
        private long _totalMessagesFailedToProcess;
        private long _totalMemoryAllocated;
        private DateTime _lastStatsReset;
        private DateTime _lastHealthCheck;
        
        // Performance tracking
        private readonly Timer _statisticsTimer;
        private readonly Timer _healthCheckTimer;
        
        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        #endregion

        #region Events

        /// <inheritdoc />
        public event Action<Type, string> SubscriptionCreated;

        /// <inheritdoc />
        public event Action<Type, string> SubscriptionDisposed;

        /// <inheritdoc />
        public event Action<Type, string, TimeSpan> MessageProcessed;

        /// <inheritdoc />
        public event Action<Type, string, Exception> MessageProcessingFailed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageSubscriptionService class.
        /// </summary>
        /// <param name="config">The subscription configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="messageBusAdapter">The message bus adapter (optional)</param>
        /// <param name="alertService">The alert service (optional)</param>
        /// <param name="profilerService">The profiler service (optional)</param>
        /// <param name="poolingService">The pooling service (optional)</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageSubscriptionService(
            MessageSubscriptionConfig config,
            ILoggingService logger,
            IMessageBusAdapter messageBusAdapter = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolingService poolingService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBusAdapter = messageBusAdapter;
            _alertService = alertService;
            _profilerService = profilerService;
            _poolingService = poolingService;

            // Validate configuration
            if (!_config.IsValid())
                throw new ArgumentException("Invalid subscription configuration", nameof(config));

            // Generate correlation ID for tracking
            _correlationId = new FixedString128Bytes($"Subscription-{DeterministicIdGenerator.GenerateCorrelationId("MessageSubscriptionService", "Instance"):N}");

            // Initialize collections
            var initialCapacity = _config.InitialSubscriberCapacity;
            _subscribers = new ConcurrentDictionary<Type, object>(Environment.ProcessorCount, initialCapacity);
            _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<IDisposable>>(Environment.ProcessorCount, initialCapacity);
            _scopes = new ConcurrentDictionary<Guid, IMessageScope>(Environment.ProcessorCount, 64);
            _messageTypeStats = new ConcurrentDictionary<Type, SubscriberStatistics>(Environment.ProcessorCount, _config.MaxTrackedMessageTypes);
            _scopeStats = new ConcurrentDictionary<Guid, ScopeStatistics>(Environment.ProcessorCount, _config.MaxTrackedScopes);

            // Initialize synchronization primitives
            _subscriptionSemaphore = new SemaphoreSlim(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
            _cancellationTokenSource = new CancellationTokenSource();
            _healthStatusLock = new ReaderWriterLockSlim();

            // Initialize timers
            if (_config.PerformanceMonitoringEnabled)
            {
                _statisticsTimer = new Timer(UpdateStatistics, null, 
                    _config.StatisticsUpdateInterval, _config.StatisticsUpdateInterval);
            }

            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                _config.HealthCheckInterval, _config.HealthCheckInterval);

            // Set initial state
            _currentHealthStatus = HealthStatus.Healthy;
            _lastStatsReset = DateTime.UtcNow;
            _lastHealthCheck = DateTime.UtcNow;

            _logger.LogInfo($"[{_correlationId}] MessageSubscriptionService initialized");
        }

        #endregion

        #region Core Subscription Operations

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
                
                // Track subscription
                TrackSubscription(typeof(TMessage), subscription);
                
                _logger.LogInfo($"[{_correlationId}] Created subscription for {typeof(TMessage).Name}");
                
                // Fire event
                SubscriptionCreated?.Invoke(typeof(TMessage), typeof(TMessage).Name);
                
                return new WrappedSubscription(subscription, _logger, typeof(TMessage), new Guid(_correlationId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to create subscription for {typeof(TMessage).Name}", ex);
                
                // Fire event
                MessageProcessingFailed?.Invoke(typeof(TMessage), typeof(TMessage).Name, ex);
                
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            if (!_config.AsyncHandlingEnabled)
            {
                // Convert async handler to sync (not recommended, but fallback)
                return SubscribeToMessage<TMessage>(msg => handler(msg).Forget());
            }

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeAsync(handler);
                
                // Track subscription
                TrackSubscription(typeof(TMessage), subscription);
                
                _logger.LogInfo($"[{_correlationId}] Created async subscription for {typeof(TMessage).Name}");
                
                // Fire event
                SubscriptionCreated?.Invoke(typeof(TMessage), typeof(TMessage).Name);
                
                return new WrappedSubscription(subscription, _logger, typeof(TMessage), new Guid(_correlationId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to create async subscription for {typeof(TMessage).Name}", ex);
                
                // Fire event
                MessageProcessingFailed?.Invoke(typeof(TMessage), typeof(TMessage).Name, ex);
                
                throw;
            }
        }

        #endregion

        #region Filtering and Routing

        /// <inheritdoc />
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            if (!_config.FilteringEnabled)
            {
                // If filtering is disabled, just subscribe normally
                return SubscribeToMessage(handler);
            }

            if (_messageBusAdapter == null)
            {
                // Fallback to manual filtering
                return SubscribeToMessage<TMessage>(msg =>
                {
                    if (filter(msg))
                        handler(msg);
                });
            }

            // Use MessagePipe filter
            var customFilter = new CustomPredicateFilter<TMessage>(filter, _logger);
            var subscription = _messageBusAdapter.Subscribe(handler, customFilter);
            
            TrackSubscription(typeof(TMessage), subscription);
            SubscriptionCreated?.Invoke(typeof(TMessage), $"Filtered-{typeof(TMessage).Name}");
            
            return new WrappedSubscription(subscription, _logger, typeof(TMessage), new Guid(_correlationId.ToString()));
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            if (!_config.FilteringEnabled)
            {
                // If filtering is disabled, just subscribe normally
                return SubscribeToMessageAsync(handler);
            }

            if (!_config.AsyncHandlingEnabled)
            {
                // Convert to sync version
                return SubscribeWithFilter<TMessage>(filter, msg => handler(msg).Forget());
            }

            if (_messageBusAdapter == null)
            {
                // Fallback to manual filtering
                return SubscribeToMessageAsync<TMessage>(async msg =>
                {
                    if (filter(msg))
                        await handler(msg);
                });
            }

            // Use MessagePipe async filter
            var customFilter = new AsyncCustomPredicateFilter<TMessage>(filter, _logger);
            var subscription = _messageBusAdapter.SubscribeAsync(handler, customFilter);
            
            TrackSubscription(typeof(TMessage), subscription);
            SubscriptionCreated?.Invoke(typeof(TMessage), $"AsyncFiltered-{typeof(TMessage).Name}");
            
            return new WrappedSubscription(subscription, _logger, typeof(TMessage), new Guid(_correlationId.ToString()));
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            if (!_config.PriorityRoutingEnabled)
            {
                // If priority routing is disabled, just subscribe normally
                return SubscribeToMessage(handler);
            }

            if (_messageBusAdapter == null)
            {
                // Fallback to manual priority filtering
                return SubscribeToMessage<TMessage>(msg =>
                {
                    if (msg.Priority >= minPriority)
                        handler(msg);
                });
            }

            // Use MessagePipe priority filter
            var priorityFilter = new MessagePriorityFilter<TMessage>(minPriority, _logger);
            var subscription = _messageBusAdapter.Subscribe(handler, priorityFilter);
            
            TrackSubscription(typeof(TMessage), subscription);
            SubscriptionCreated?.Invoke(typeof(TMessage), $"Priority-{typeof(TMessage).Name}");
            
            return new WrappedSubscription(subscription, _logger, typeof(TMessage), new Guid(_correlationId.ToString()));
        }

        #endregion

        #region Scoped Subscriptions

        /// <inheritdoc />
        public IMessageScope CreateScope()
        {
            ThrowIfDisposed();

            var scope = new MessageScope(this, _logger);
            _scopes.TryAdd(scope.Id, scope);
            
            // Track scope statistics if enabled
            if (_config.TrackScopeStatistics && _scopeStats.Count < _config.MaxTrackedScopes)
            {
                _scopeStats.TryAdd(scope.Id, new ScopeStatistics
                {
                    ScopeId = scope.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    SubscriptionCount = 0,
                    MessagesProcessed = 0,
                    ProcessingFailures = 0
                });
            }
            
            _logger.LogInfo($"[{_correlationId}] Created message scope {scope.Id}");
            
            return scope;
        }

        #endregion

        #region Subscriber Management

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreateSubscriber<TMessage>();
        }

        /// <inheritdoc />
        public int GetSubscriberCount<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            
            if (_subscriptions.TryGetValue(typeof(TMessage), out var subscriptionBag))
            {
                return subscriptionBag.Count;
            }
            
            return 0;
        }

        /// <inheritdoc />
        public int GetTotalSubscriberCount()
        {
            ThrowIfDisposed();
            return _subscriptions.Values.AsValueEnumerable().Sum(bag => bag.Count);
        }

        #endregion

        #region Statistics and Diagnostics

        /// <inheritdoc />
        public MessageSubscriptionStatistics GetStatistics()
        {
            ThrowIfDisposed();

            _healthStatusLock.EnterReadLock();
            try
            {
                return new MessageSubscriptionStatistics
                {
                    TotalSubscriptionsCreated = Interlocked.Read(ref _totalSubscriptionsCreated),
                    TotalSubscriptionsDisposed = Interlocked.Read(ref _totalSubscriptionsDisposed),
                    TotalMessagesProcessed = Interlocked.Read(ref _totalMessagesProcessed),
                    TotalMessagesFailedToProcess = Interlocked.Read(ref _totalMessagesFailedToProcess),
                    ActiveSubscriptions = GetTotalSubscriberCount(),
                    ActiveScopes = _scopes.Count,
                    AverageProcessingTimeMs = CalculateAverageProcessingTime(),
                    PeakProcessingTimeMs = CalculatePeakProcessingTime(),
                    MessagesPerSecond = CalculateMessagesPerSecond(),
                    PeakMessagesPerSecond = 0, // TODO: Track peak MPS
                    ErrorRate = CalculateErrorRate(),
                    MemoryUsageBytes = Interlocked.Read(ref _totalMemoryAllocated),
                    CapturedAt = DateTime.UtcNow,
                    LastResetAt = _lastStatsReset,
                    MessageTypeStatistics = _messageTypeStats.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    ScopeStatistics = _scopeStats.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            }
            finally
            {
                _healthStatusLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public void ClearStatistics()
        {
            ThrowIfDisposed();

            _logger.LogInfo($"[{_correlationId}] Clearing subscription statistics");

            // Reset counters
            Interlocked.Exchange(ref _totalSubscriptionsCreated, 0);
            Interlocked.Exchange(ref _totalSubscriptionsDisposed, 0);
            Interlocked.Exchange(ref _totalMessagesProcessed, 0);
            Interlocked.Exchange(ref _totalMessagesFailedToProcess, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);
            
            // Clear per-type and scope statistics
            _messageTypeStats.Clear();
            _scopeStats.Clear();
            _lastStatsReset = DateTime.UtcNow;

            _logger.LogInfo($"[{_correlationId}] Subscription statistics cleared");
        }

        #endregion

        #region Health and Status

        /// <inheritdoc />
        public HealthStatus GetHealthStatus()
        {
            _healthStatusLock.EnterReadLock();
            try
            {
                return _currentHealthStatus;
            }
            finally
            {
                _healthStatusLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            try
            {
                var statistics = GetStatistics();
                var newStatus = DetermineHealthStatus(statistics);
                
                UpdateHealthStatus(newStatus, "Manual health check");
                
                return newStatus;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"[{_correlationId}] Health check cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Health check failed", ex);
                UpdateHealthStatus(HealthStatus.Unhealthy, $"Health check exception: {ex.Message}");
                return HealthStatus.Unhealthy;
            }
        }

        #endregion

        #region Private Implementation

        private IMessageSubscriber<TMessage> GetOrCreateSubscriber<TMessage>() where TMessage : IMessage
        {
            return (IMessageSubscriber<TMessage>)_subscribers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating subscriber for {typeof(TMessage).Name}");
                
                // Create MessageSubscriberConfig from MessageSubscriptionConfig
                var subscriberConfig = new MessageSubscriberConfig(
                    maxConcurrentHandlers: _config.MaxConcurrentHandlers,
                    processingTimeout: _config.ProcessingTimeout,
                    enableProfiling: _config.PerformanceMonitoringEnabled,
                    enableMessageBusIntegration: _config.MessageBusIntegrationEnabled,
                    statisticsInterval: _config.StatisticsUpdateInterval,
                    enableCircuitBreaker: _config.CircuitBreakerEnabled,
                    enableErrorRetry: _config.RetryFailedMessages,
                    maxRetryAttempts: _config.MaxRetryAttempts,
                    retryDelay: _config.RetryDelay,
                    correlationId: _correlationId);
                
                // Create subscriber directly - pooling not suitable for subscribers with dependencies
                return new MessageSubscriber<TMessage>(subscriberConfig, _logger, _profilerService, null);
            });
        }

        private void TrackSubscription(Type messageType, IDisposable subscription)
        {
            var subscriptions = _subscriptions.GetOrAdd(messageType, _ => new ConcurrentBag<IDisposable>());
            subscriptions.Add(subscription);
            
            Interlocked.Increment(ref _totalSubscriptionsCreated);
        }

        private double CalculateErrorRate()
        {
            var totalMessages = _totalMessagesProcessed + _totalMessagesFailedToProcess;
            return totalMessages > 0 ? (double)_totalMessagesFailedToProcess / totalMessages : 0.0;
        }

        private double CalculateAverageProcessingTime()
        {
            var allStats = _messageTypeStats.Values;
            if (!allStats.AsValueEnumerable().Any()) return 0.0;
            
            return allStats.AsValueEnumerable().Average(s => s.AverageProcessingTimeMs);
        }

        private double CalculatePeakProcessingTime()
        {
            var allStats = _messageTypeStats.Values;
            if (!allStats.AsValueEnumerable().Any()) return 0.0;
            
            return allStats.AsValueEnumerable().Max(s => s.AverageProcessingTimeMs);
        }

        private double CalculateMessagesPerSecond()
        {
            var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
            if (timeSinceReset.TotalSeconds <= 0) return 0.0;
            
            return _totalMessagesProcessed / timeSinceReset.TotalSeconds;
        }

        private HealthStatus DetermineHealthStatus(MessageSubscriptionStatistics statistics)
        {
            // Check critical conditions
            if (statistics.ErrorRate > _config.CriticalErrorRateThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.AverageProcessingTimeMs > _config.CriticalProcessingTimeThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.ActiveSubscriptions > _config.CriticalActiveSubscriptionsThreshold)
                return HealthStatus.Unhealthy;
            
            // Check degraded conditions
            if (statistics.ErrorRate > _config.WarningErrorRateThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.AverageProcessingTimeMs > _config.WarningProcessingTimeThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.ActiveSubscriptions > _config.WarningActiveSubscriptionsThreshold)
                return HealthStatus.Degraded;
            
            return HealthStatus.Healthy;
        }

        private void UpdateHealthStatus(HealthStatus newStatus, string reason)
        {
            _healthStatusLock.EnterWriteLock();
            try
            {
                var oldStatus = _currentHealthStatus;
                if (oldStatus != newStatus)
                {
                    _currentHealthStatus = newStatus;
                    _lastHealthCheck = DateTime.UtcNow;
                    
                    _logger.LogInfo($"[{_correlationId}] Subscription health status changed from {oldStatus} to {newStatus}: {reason}");
                    
                    // Raise alert for unhealthy status
                    if (_alertService != null && newStatus == HealthStatus.Unhealthy)
                    {
                        _alertService.RaiseAlert(
                            $"MessageSubscriptionService health status changed to {newStatus}",
                            AlertSeverity.Critical,
                            source: "MessageSubscriptionService",
                            tag: "HealthChange",
                            correlationId: new Guid(_correlationId.ToString()));
                    }
                }
            }
            finally
            {
                _healthStatusLock.ExitWriteLock();
            }
        }

        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Update memory allocation tracking
                var currentMemory = GC.GetTotalMemory(false);
                Interlocked.Exchange(ref _totalMemoryAllocated, currentMemory);

                // Force GC if memory pressure is too high
                if (_config.ForceGCOnHighMemoryPressure && currentMemory > _config.MaxMemoryPressure)
                {
                    _logger.LogWarning($"[{_correlationId}] High memory pressure detected: {currentMemory / 1024 / 1024}MB, forcing GC");
                    GC.Collect(2, GCCollectionMode.Forced);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to update subscription statistics", ex);
            }
        }

        private void PerformHealthCheck(object state)
        {
            if (_disposed) return;

            try
            {
                var statistics = GetStatistics();
                var newStatus = DetermineHealthStatus(statistics);
                UpdateHealthStatus(newStatus, "Periodic health check");
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Health check failed", ex);
                UpdateHealthStatus(HealthStatus.Unhealthy, $"Health check exception: {ex.Message}");
            }
        }

        internal void OnScopeDisposed(Guid scopeId)
        {
            _scopes.TryRemove(scopeId, out _);
            
            // Update scope statistics if tracking
            if (_scopeStats.TryGetValue(scopeId, out var scopeStat))
            {
                scopeStat.IsActive = false;
                _logger.LogInfo($"[{_correlationId}] Scope {scopeId} removed from tracking");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageSubscriptionService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message subscription service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessageSubscriptionService");

            try
            {
                _disposed = true;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _statisticsTimer?.Dispose();
                _healthCheckTimer?.Dispose();

                // Dispose all subscribers
                foreach (var subscriber in _subscribers.Values.AsValueEnumerable().OfType<IDisposable>())
                {
                    try
                    {
                        subscriber?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"[{_correlationId}] Error disposing subscriber", ex);
                    }
                }

                // Dispose all scopes
                foreach (var scope in _scopes.Values)
                {
                    try
                    {
                        scope?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"[{_correlationId}] Error disposing scope", ex);
                    }
                }

                // Dispose synchronization primitives
                _subscriptionSemaphore?.Dispose();
                _healthStatusLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageSubscriptionService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Error during MessageSubscriptionService disposal", ex);
            }
        }

        #endregion
    }
}