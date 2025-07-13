using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Production-ready implementation of a type-specific message subscriber.
    /// Provides high-performance subscription management with comprehensive monitoring and error handling.
    /// Follows AhBearStudios Core Development Guidelines with full core systems integration.
    /// </summary>
    /// <typeparam name="TMessage">The message type this subscriber handles</typeparam>
    internal sealed class MessageSubscriber<TMessage> : IMessageSubscriber<TMessage> where TMessage : IMessage
    {
        #region Private Fields

        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        private readonly MessageBusService _messageBusService;

        // Subscription management
        private readonly ConcurrentDictionary<Guid, SubscriptionInfo> _subscriptions;
        private readonly SemaphoreSlim _subscriptionSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // State management
        private volatile bool _disposed;

        // Statistics tracking
        private long _totalReceived;
        private long _successfullyProcessed;
        private long _failedProcessing;
        private long _filteredOut;
        private long _lastProcessedTicks;
        private double _totalProcessingTime;
        private double _peakProcessingTime;

        // Performance monitoring
        private readonly Timer _statisticsTimer;
        private DateTime _lastStatsReset;

        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageSubscriber class.
        /// </summary>
        /// <param name="config">The message bus configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="messageBusService">The parent message bus service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageSubscriber(
            MessageBusConfig config,
            ILoggingService logger,
            IProfilerService profilerService,
            MessageBusService messageBusService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            // Generate correlation ID for tracking
            _correlationId = $"Subscriber_{typeof(TMessage).Name}_{Guid.NewGuid():N}"[..32];

            _subscriptions = new ConcurrentDictionary<Guid, SubscriptionInfo>();
            _subscriptionSemaphore = new SemaphoreSlim(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
            _cancellationTokenSource = new CancellationTokenSource();

            _lastStatsReset = DateTime.UtcNow;
            _statisticsTimer = new Timer(UpdateStatistics, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInfo($"[{_correlationId}] MessageSubscriber<{typeof(TMessage).Name}> initialized");
        }

        #endregion

        #region IMessageSubscriber<TMessage> Implementation

        /// <inheritdoc />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc />
        public bool IsOperational => !_disposed && !_cancellationTokenSource.Token.IsCancellationRequested;

        /// <inheritdoc />
        public int ActiveSubscriptions => _subscriptions.Count;

        /// <inheritdoc />
        public IDisposable Subscribe(Action<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            var subscriptionId = Guid.NewGuid();
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_Subscribe_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{subscriptionCorrelationId}] Creating subscription for {typeof(TMessage).Name}");

                // Create subscription info
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.Sync,
                    handler,
                    null,
                    null,
                    null,
                    MessagePriority.Normal,
                    subscriptionCorrelationId);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _logger.LogInfo($"[{subscriptionCorrelationId}] Successfully created subscription {subscriptionId}");

                // Raise event
                SubscriptionCreated?.Invoke(this, new SubscriptionCreatedEventArgs(subscriptionId, typeof(TMessage)));

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{subscriptionCorrelationId}] Failed to create subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync(Func<TMessage, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            var subscriptionId = Guid.NewGuid();
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeAsync_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{subscriptionCorrelationId}] Creating async subscription for {typeof(TMessage).Name}");

                // Create subscription info
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.Async,
                    null,
                    handler,
                    null,
                    null,
                    MessagePriority.Normal,
                    subscriptionCorrelationId);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register async subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _logger.LogInfo($"[{subscriptionCorrelationId}] Successfully created async subscription {subscriptionId}");

                // Raise event
                SubscriptionCreated?.Invoke(this, new SubscriptionCreatedEventArgs(subscriptionId, typeof(TMessage)));

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{subscriptionCorrelationId}] Failed to create async subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority(Action<TMessage> handler, MessagePriority minPriority)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            var subscriptionId = Guid.NewGuid();
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribePriority_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{subscriptionCorrelationId}] Creating priority subscription for {typeof(TMessage).Name} with min priority {minPriority}");

                // Create subscription info with priority filter
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.SyncWithPriority,
                    handler,
                    null,
                    null,
                    null,
                    minPriority,
                    subscriptionCorrelationId);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register priority subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _logger.LogInfo($"[{subscriptionCorrelationId}] Successfully created priority subscription {subscriptionId}");

                // Raise event
                SubscriptionCreated?.Invoke(this, new SubscriptionCreatedEventArgs(subscriptionId, typeof(TMessage)));

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{subscriptionCorrelationId}] Failed to create priority subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeConditional(Action<TMessage> handler, Func<TMessage, bool> condition)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            ThrowIfDisposed();

            var subscriptionId = Guid.NewGuid();
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeConditional_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{subscriptionCorrelationId}] Creating conditional subscription for {typeof(TMessage).Name}");

                // Create subscription info with condition
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.SyncWithCondition,
                    handler,
                    null,
                    condition,
                    null,
                    MessagePriority.Normal,
                    subscriptionCorrelationId);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register conditional subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _logger.LogInfo($"[{subscriptionCorrelationId}] Successfully created conditional subscription {subscriptionId}");

                // Raise event
                SubscriptionCreated?.Invoke(this, new SubscriptionCreatedEventArgs(subscriptionId, typeof(TMessage)));

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{subscriptionCorrelationId}] Failed to create conditional subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeConditionalAsync(Func<TMessage, Task> handler, Func<TMessage, Task<bool>> condition)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            ThrowIfDisposed();

            var subscriptionId = Guid.NewGuid();
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeConditionalAsync_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{subscriptionCorrelationId}] Creating async conditional subscription for {typeof(TMessage).Name}");

                // Create subscription info with async condition
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.AsyncWithCondition,
                    null,
                    handler,
                    null,
                    condition,
                    MessagePriority.Normal,
                    subscriptionCorrelationId);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register async conditional subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _logger.LogInfo($"[{subscriptionCorrelationId}] Successfully created async conditional subscription {subscriptionId}");

                // Raise event
                SubscriptionCreated?.Invoke(this, new SubscriptionCreatedEventArgs(subscriptionId, typeof(TMessage)));

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{subscriptionCorrelationId}] Failed to create async conditional subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeFromSource(Action<TMessage> handler, string source)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            // Create a condition filter for the specified source
            return SubscribeConditional(handler, message => message.Source.ToString() == source);
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithCorrelation(Action<TMessage> handler, Guid correlationId)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (correlationId == Guid.Empty)
                throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

            // Create a condition filter for the specified correlation ID
            return SubscribeConditional(handler, message => message.CorrelationId == correlationId);
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithErrorHandling(Action<TMessage> handler, Action<Exception, TMessage> errorHandler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (errorHandler == null)
                throw new ArgumentNullException(nameof(errorHandler));

            // Create a wrapped handler with error handling
            Action<TMessage> wrappedHandler = message =>
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    try
                    {
                        errorHandler(ex, message);
                    }
                    catch (Exception errorEx)
                    {
                        _logger.LogException(errorEx, $"[{_correlationId}] Error in error handler for message {message.Id}");
                    }
                }
            };

            return Subscribe(wrappedHandler);
        }

        /// <inheritdoc />
        public void UnsubscribeAll()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogInfo($"[{_correlationId}] Unsubscribing all subscriptions for {typeof(TMessage).Name}");

                var subscriptionIds = _subscriptions.Keys;
                foreach (var subscriptionId in subscriptionIds)
                {
                    try
                    {
                        RemoveSubscription(subscriptionId, "UnsubscribeAll");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, $"[{_correlationId}] Failed to remove subscription {subscriptionId}");
                    }
                }

                _logger.LogInfo($"[{_correlationId}] All subscriptions unsubscribed for {typeof(TMessage).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to unsubscribe all for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public SubscriberStatistics GetStatistics()
        {
            var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
            var processingRate = timeSinceReset.TotalSeconds > 0 
                ? _successfullyProcessed / timeSinceReset.TotalSeconds 
                : 0;

            var avgProcessingTime = _successfullyProcessed > 0 
                ? _totalProcessingTime / _successfullyProcessed 
                : 0;

            return new SubscriberStatistics(
                _totalReceived,
                _successfullyProcessed,
                _failedProcessing,
                _filteredOut,
                avgProcessingTime,
                processingRate,
                _lastProcessedTicks,
                ActiveSubscriptions);
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MessageProcessedEventArgs> MessageProcessed;

        /// <inheritdoc />
        public event EventHandler<MessageProcessingFailedEventArgs> MessageProcessingFailed;

        /// <inheritdoc />
        public event EventHandler<SubscriptionCreatedEventArgs> SubscriptionCreated;

        /// <inheritdoc />
        public event EventHandler<SubscriptionDisposedEventArgs> SubscriptionDisposed;

        #endregion

        #region Internal Methods

        /// <summary>
        /// Processes a message for all applicable subscriptions.
        /// This would be called by the underlying message infrastructure (e.g., MessagePipe).
        /// </summary>
        /// <param name="message">The message to process</param>
        internal async Task ProcessMessageAsync(TMessage message)
        {
            if (message == null || _disposed)
                return;

            Interlocked.Increment(ref _totalReceived);

            var messageCorrelationId = $"{_correlationId}_{message.Id:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_ProcessMessage_{typeof(TMessage).Name}");

                _logger.LogInfo($"[{messageCorrelationId}] Processing message {message.Id} for {_subscriptions.Count} subscriptions");

                var processedCount = 0;
                var filteredCount = 0;

                // Process all subscriptions concurrently
                var processingTasks = new List<Task>();

                foreach (var subscription in _subscriptions.Values)
                {
                    processingTasks.Add(ProcessSubscriptionAsync(message, subscription, messageCorrelationId));
                }

                await Task.WhenAll(processingTasks);

                _logger.LogInfo($"[{messageCorrelationId}] Completed processing message {message.Id}: {processedCount} processed, {filteredCount} filtered");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{messageCorrelationId}] Failed to process message {message.Id}");
            }
        }

        /// <summary>
        /// Processes a message for a specific subscription.
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <param name="subscription">The subscription info</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        private async Task ProcessSubscriptionAsync(TMessage message, SubscriptionInfo subscription, FixedString128Bytes correlationId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Check priority filter
                if (subscription.Type == SubscriptionType.SyncWithPriority && message.Priority < subscription.MinPriority)
                {
                    Interlocked.Increment(ref _filteredOut);
                    return;
                }

                // Check sync condition
                if (subscription.Type == SubscriptionType.SyncWithCondition && subscription.SyncCondition != null)
                {
                    if (!subscription.SyncCondition(message))
                    {
                        Interlocked.Increment(ref _filteredOut);
                        return;
                    }
                }

                // Check async condition
                if (subscription.Type == SubscriptionType.AsyncWithCondition && subscription.AsyncCondition != null)
                {
                    if (!await subscription.AsyncCondition(message))
                    {
                        Interlocked.Increment(ref _filteredOut);
                        return;
                    }
                }

                // Acquire semaphore for concurrency control
                await _subscriptionSemaphore.WaitAsync(_cancellationTokenSource.Token);

                try
                {
                    // Execute handler based on type
                    switch (subscription.Type)
                    {
                        case SubscriptionType.Sync:
                        case SubscriptionType.SyncWithPriority:
                        case SubscriptionType.SyncWithCondition:
                            subscription.SyncHandler?.Invoke(message);
                            break;

                        case SubscriptionType.Async:
                        case SubscriptionType.AsyncWithCondition:
                            if (subscription.AsyncHandler != null)
                            {
                                await subscription.AsyncHandler(message);
                            }
                            break;
                    }

                    // Track successful processing
                    stopwatch.Stop();
                    var processingTime = stopwatch.Elapsed.TotalMilliseconds;

                    Interlocked.Increment(ref _successfullyProcessed);
                    Interlocked.Exchange(ref _lastProcessedTicks, DateTime.UtcNow.Ticks);
                    UpdateProcessingTimeStatistics(processingTime);

                    _logger.LogInfo($"[{correlationId}] Subscription {subscription.Id} processed message {message.Id} in {processingTime:F2}ms");

                    // Raise success event
                    MessageProcessed?.Invoke(this, new MessageProcessedEventArgs(message, stopwatch.Elapsed));
                }
                finally
                {
                    _subscriptionSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Interlocked.Increment(ref _failedProcessing);

                _logger.LogException(ex, $"[{correlationId}] Subscription {subscription.Id} failed to process message {message.Id}");

                // Raise failure event
                MessageProcessingFailed?.Invoke(this, new MessageProcessingFailedEventArgs(message, ex, subscription.Id));
            }
        }

        /// <summary>
        /// Removes a subscription from tracking.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID to remove</param>
        /// <param name="reason">The reason for removal</param>
        internal void RemoveSubscription(Guid subscriptionId, string reason)
        {
            if (_subscriptions.TryRemove(subscriptionId, out var subscriptionInfo))
            {
                _logger.LogInfo($"[{subscriptionInfo.CorrelationId}] Removed subscription {subscriptionId}: {reason}");

                // Raise disposal event
                SubscriptionDisposed?.Invoke(this, new SubscriptionDisposedEventArgs(subscriptionId, reason));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates processing time statistics in a thread-safe manner.
        /// </summary>
        /// <param name="processingTime">The processing time in milliseconds</param>
        private void UpdateProcessingTimeStatistics(double processingTime)
        {
            // Update total processing time
            var currentTotal = _totalProcessingTime;
            var newTotal = currentTotal + processingTime;
            while (Interlocked.CompareExchange(ref _totalProcessingTime, newTotal, currentTotal) != currentTotal)
            {
                currentTotal = _totalProcessingTime;
                newTotal = currentTotal + processingTime;
            }

            // Update peak processing time
            var currentPeak = _peakProcessingTime;
            if (processingTime > currentPeak)
            {
                Interlocked.CompareExchange(ref _peakProcessingTime, processingTime, currentPeak);
            }
        }

        /// <summary>
        /// Updates statistics periodically (timer callback).
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Statistics are updated in real-time by other methods
                // This timer could be used for periodic cleanup or aggregation
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to update statistics for subscriber {typeof(TMessage).Name}");
            }
        }

        /// <summary>
        /// Throws an exception if the subscriber has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the subscriber is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"MessageSubscriber<{typeof(TMessage).Name}>");
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message subscriber and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessageSubscriber<{typeof(TMessage).Name}>");

            try
            {
                _disposed = true;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Unsubscribe all
                UnsubscribeAll();

                // Dispose resources
                _statisticsTimer?.Dispose();
                _subscriptionSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageSubscriber<{typeof(TMessage).Name}> disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Error during MessageSubscriber<{typeof(TMessage).Name}> disposal");
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Subscription type enumeration.
        /// </summary>
        private enum SubscriptionType
        {
            Sync,
            Async,
            SyncWithPriority,
            SyncWithCondition,
            AsyncWithCondition
        }

        /// <summary>
        /// Subscription information structure.
        /// </summary>
        private sealed class SubscriptionInfo
        {
            public Guid Id { get; }
            public SubscriptionType Type { get; }
            public Action<TMessage> SyncHandler { get; }
            public Func<TMessage, Task> AsyncHandler { get; }
            public Func<TMessage, bool> SyncCondition { get; }
            public Func<TMessage, Task<bool>> AsyncCondition { get; }
            public MessagePriority MinPriority { get; }
            public FixedString128Bytes CorrelationId { get; }

            public SubscriptionInfo(
                Guid id,
                SubscriptionType type,
                Action<TMessage> syncHandler,
                Func<TMessage, Task> asyncHandler,
                Func<TMessage, bool> syncCondition,
                Func<TMessage, Task<bool>> asyncCondition,
                MessagePriority minPriority,
                FixedString128Bytes correlationId)
            {
                Id = id;
                Type = type;
                SyncHandler = syncHandler;
                AsyncHandler = asyncHandler;
                SyncCondition = syncCondition;
                AsyncCondition = asyncCondition;
                MinPriority = minPriority;
                CorrelationId = correlationId;
            }
        }

        /// <summary>
        /// Managed subscription wrapper that notifies the subscriber when disposed.
        /// </summary>
        private sealed class ManagedSubscription : IDisposable
        {
            private readonly Guid _subscriptionId;
            private readonly MessageSubscriber<TMessage> _subscriber;
            private readonly FixedString128Bytes _correlationId;
            private volatile bool _disposed;

            public ManagedSubscription(Guid subscriptionId, MessageSubscriber<TMessage> subscriber, FixedString128Bytes correlationId)
            {
                _subscriptionId = subscriptionId;
                _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
                _correlationId = correlationId;
            }

            public void Dispose()
            {
                if (_disposed) return;

                try
                {
                    _subscriber.RemoveSubscription(_subscriptionId, "Explicit disposal");
                }
                catch (Exception ex)
                {
                    _subscriber._logger.LogException(ex, $"[{_correlationId}] Error disposing managed subscription {_subscriptionId}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }
}