using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Profiling;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;

namespace AhBearStudios.Core.Messaging.Subscribers;

/// <summary>
/// Production-ready implementation of a type-specific message subscriber.
/// Provides high-performance subscription management with comprehensive monitoring and error handling.
/// Follows AhBearStudios Core Development Guidelines with full core systems integration.
/// Modernized to use UniTask, IMessage integration, and simplified interface.
/// </summary>
/// <typeparam name="TMessage">The message type this subscriber handles</typeparam>
internal sealed class MessageSubscriber<TMessage> : IMessageSubscriber<TMessage> where TMessage : IMessage
{
    #region Private Fields

    private readonly MessageSubscriberConfig _config;
    private readonly ILoggingService _loggingService;
    private readonly IProfilerService _profilerService;
    private readonly IMessageBusService _messageBusService;

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

    // Profiler markers
    private readonly ProfilerMarker _processMessageMarker;
    private readonly ProfilerMarker _subscribeMarker;
    private readonly ProfilerMarker _unsubscribeMarker;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the MessageSubscriber class.
    /// </summary>
    /// <param name="config">The message subscriber configuration</param>
    /// <param name="loggingService">The logging service</param>
    /// <param name="profilerService">The profiler service</param>
    /// <param name="messageBusService">The message bus service</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public MessageSubscriber(
        MessageSubscriberConfig config,
        ILoggingService loggingService,
        IProfilerService profilerService,
        IMessageBusService messageBusService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

        // Generate correlation ID for tracking
        var correlationGuid = DeterministicIdGenerator.GenerateCorrelationId("MessageSubscriber", typeof(TMessage).Name);
        _correlationId = $"Subscriber_{typeof(TMessage).Name}_{correlationGuid:N}"[..32];

        _subscriptions = new ConcurrentDictionary<Guid, SubscriptionInfo>();
        _subscriptionSemaphore = new SemaphoreSlim(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
        _cancellationTokenSource = new CancellationTokenSource();

        _lastStatsReset = DateTime.UtcNow;
        _statisticsTimer = new Timer(UpdateStatistics, null, _config.StatisticsInterval, _config.StatisticsInterval);

        // Initialize profiler markers
        _processMessageMarker = new ProfilerMarker($"MessageSubscriber<{typeof(TMessage).Name}>.ProcessMessage");
        _subscribeMarker = new ProfilerMarker($"MessageSubscriber<{typeof(TMessage).Name}>.Subscribe");
        _unsubscribeMarker = new ProfilerMarker($"MessageSubscriber<{typeof(TMessage).Name}>.Unsubscribe");

        _loggingService.LogInfo($"[{_correlationId}] MessageSubscriber<{typeof(TMessage).Name}> initialized");

        // Publish creation message if message bus integration is enabled
        if (_config.EnableMessageBusIntegration)
        {
            PublishSubscriberCreatedMessage();
        }
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

        using (_subscribeMarker.Auto())
        {
            var subscriptionId = DeterministicIdGenerator.GenerateCorrelationId("Subscription", typeof(TMessage).Name);
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_Subscribe_{typeof(TMessage).Name}");

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Creating subscription for {typeof(TMessage).Name}");

                // Create subscription info
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.Sync,
                    handler,
                    null,
                    null,
                    null,
                    _config.DefaultMinPriority,
                    subscriptionCorrelationId,
                    SubscriptionCategory.Standard);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Successfully created subscription {subscriptionId}");

                // Publish subscription created message if enabled
                if (_config.EnableMessageBusIntegration)
                {
                    PublishSubscriptionCreatedMessage(subscriptionId, SubscriptionCategory.Standard);
                }

                return subscription;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{subscriptionCorrelationId}] Failed to create subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeAsync(Func<TMessage, UniTask> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        ThrowIfDisposed();

        using (_subscribeMarker.Auto())
        {
            var subscriptionId = DeterministicIdGenerator.GenerateCorrelationId("AsyncSubscription", typeof(TMessage).Name);
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeAsync_{typeof(TMessage).Name}");

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Creating async subscription for {typeof(TMessage).Name}");

                // Create subscription info
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.Async,
                    null,
                    handler,
                    null,
                    null,
                    _config.DefaultMinPriority,
                    subscriptionCorrelationId,
                    SubscriptionCategory.Async);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register async subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Successfully created async subscription {subscriptionId}");

                // Publish subscription created message if enabled
                if (_config.EnableMessageBusIntegration)
                {
                    PublishSubscriptionCreatedMessage(subscriptionId, SubscriptionCategory.Async);
                }

                return subscription;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{subscriptionCorrelationId}] Failed to create async subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeWithFilter(
        Action<TMessage> handler, 
        Func<TMessage, bool> filter = null, 
        MessagePriority minPriority = MessagePriority.Debug)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        ThrowIfDisposed();

        using (_subscribeMarker.Auto())
        {
            var subscriptionId = DeterministicIdGenerator.GenerateCorrelationId("FilteredSubscription", typeof(TMessage).Name);
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeWithFilter_{typeof(TMessage).Name}");

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Creating filtered subscription for {typeof(TMessage).Name} with min priority {minPriority}");

                // Create subscription info with filter
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    filter != null ? SubscriptionType.SyncWithCondition : SubscriptionType.SyncWithPriority,
                    handler,
                    null,
                    filter,
                    null,
                    minPriority,
                    subscriptionCorrelationId,
                    filter != null ? SubscriptionCategory.Filtered : SubscriptionCategory.Priority);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register filtered subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Successfully created filtered subscription {subscriptionId}");

                // Publish subscription created message if enabled
                if (_config.EnableMessageBusIntegration)
                {
                    var category = filter != null ? SubscriptionCategory.Filtered : SubscriptionCategory.Priority;
                    PublishSubscriptionCreatedMessage(subscriptionId, category);
                }

                return subscription;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{subscriptionCorrelationId}] Failed to create filtered subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeAsyncWithFilter(
        Func<TMessage, UniTask> handler, 
        Func<TMessage, UniTask<bool>> filter = null, 
        MessagePriority minPriority = MessagePriority.Debug)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        ThrowIfDisposed();

        using (_subscribeMarker.Auto())
        {
            var subscriptionId = DeterministicIdGenerator.GenerateCorrelationId("AsyncFilteredSubscription", typeof(TMessage).Name);
            var subscriptionCorrelationId = $"{_correlationId}_{subscriptionId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_SubscribeAsyncWithFilter_{typeof(TMessage).Name}");

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Creating async filtered subscription for {typeof(TMessage).Name}");

                // Create subscription info with async filter
                var subscriptionInfo = new SubscriptionInfo(
                    subscriptionId,
                    SubscriptionType.AsyncWithCondition,
                    null,
                    handler,
                    null,
                    filter,
                    minPriority,
                    subscriptionCorrelationId,
                    SubscriptionCategory.AsyncFiltered);

                // Add to tracking
                if (!_subscriptions.TryAdd(subscriptionId, subscriptionInfo))
                {
                    throw new InvalidOperationException($"Failed to register async filtered subscription {subscriptionId}");
                }

                // Create disposable wrapper
                var subscription = new ManagedSubscription(subscriptionId, this, subscriptionCorrelationId);

                _loggingService.LogInfo($"[{subscriptionCorrelationId}] Successfully created async filtered subscription {subscriptionId}");

                // Publish subscription created message if enabled
                if (_config.EnableMessageBusIntegration)
                {
                    PublishSubscriptionCreatedMessage(subscriptionId, SubscriptionCategory.AsyncFiltered);
                }

                return subscription;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{subscriptionCorrelationId}] Failed to create async filtered subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public void UnsubscribeAll()
    {
        ThrowIfDisposed();

        using (_unsubscribeMarker.Auto())
        {
            try
            {
                _loggingService.LogInfo($"[{_correlationId}] Unsubscribing all subscriptions for {typeof(TMessage).Name}");

                var subscriptionIds = _subscriptions.Keys.AsValueEnumerable().ToList();
                foreach (var subscriptionId in subscriptionIds)
                {
                    try
                    {
                        RemoveSubscription(subscriptionId, "UnsubscribeAll");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"[{_correlationId}] Failed to remove subscription {subscriptionId}", ex);
                    }
                }

                _loggingService.LogInfo($"[{_correlationId}] All subscriptions unsubscribed for {typeof(TMessage).Name}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{_correlationId}] Failed to unsubscribe all for {typeof(TMessage).Name}", ex);
                throw;
            }
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

    #region Internal Methods

    /// <summary>
    /// Processes a message for all applicable subscriptions.
    /// This would be called by the underlying message infrastructure (e.g., MessagePipe).
    /// </summary>
    /// <param name="message">The message to process</param>
    internal async UniTask ProcessMessageAsync(TMessage message)
    {
        if (message == null || _disposed)
            return;

        Interlocked.Increment(ref _totalReceived);

        var messageCorrelationId = $"{_correlationId}_{message.Id:N}"[..32];

        using (_processMessageMarker.Auto())
        {
            try
            {
                using var profilerScope = _profilerService?.BeginScope($"Subscriber_ProcessMessage_{typeof(TMessage).Name}");

                _loggingService.LogInfo($"[{messageCorrelationId}] Processing message {message.Id} for {_subscriptions.Count} subscriptions");

                // Process all subscriptions concurrently using UniTask
                var processingTasks = _subscriptions.Values
                    .AsValueEnumerable()
                    .Select(subscription => ProcessSubscriptionAsync(message, subscription, messageCorrelationId))
                    .ToList();

                await UniTask.WhenAll(processingTasks);

                _loggingService.LogInfo($"[{messageCorrelationId}] Completed processing message {message.Id}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"[{messageCorrelationId}] Failed to process message {message.Id}", ex);
            }
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
            _loggingService.LogInfo($"[{subscriptionInfo.CorrelationId}] Removed subscription {subscriptionId}: {reason}");

            // Publish subscription disposed message if enabled
            if (_config.EnableMessageBusIntegration)
            {
                PublishSubscriptionDisposedMessage(subscriptionId, reason);
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Processes a message for a specific subscription.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="subscription">The subscription info</param>
    /// <param name="correlationId">Correlation ID for logging</param>
    private async UniTask ProcessSubscriptionAsync(TMessage message, SubscriptionInfo subscription, FixedString128Bytes correlationId)
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

                _loggingService.LogInfo($"[{correlationId}] Subscription {subscription.Id} processed message {message.Id} in {processingTime:F2}ms");

                // Publish success message if enabled
                if (_config.EnableMessageBusIntegration)
                {
                    PublishSubscriptionProcessedMessage(subscription.Id, message, stopwatch.Elapsed, subscription.Category);
                }
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

            _loggingService.LogException($"[{correlationId}] Subscription {subscription.Id} failed to process message {message.Id}", ex);

            // Publish failure message if enabled
            if (_config.EnableMessageBusIntegration)
            {
                PublishSubscriptionFailedMessage(subscription.Id, message, ex, stopwatch.Elapsed, subscription.Category);
            }
        }
    }

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
            _loggingService.LogException($"[{_correlationId}] Failed to update statistics for subscriber {typeof(TMessage).Name}", ex);
        }
    }

    /// <summary>
    /// Publishes a subscriber created message to the message bus.
    /// </summary>
    private void PublishSubscriberCreatedMessage()
    {
        try
        {
            var subscriberId = DeterministicIdGenerator.GenerateCorrelationId("Subscriber", typeof(TMessage).Name);
            var message = MessageBusSubscriberCreatedMessage.Create(
                subscriberId,
                typeof(TMessage),
                nameof(SubscriptionCategory.Standard));
            _messageBusService.PublishMessage(message);
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Failed to publish subscriber created message", ex);
        }
    }

    /// <summary>
    /// Publishes a subscription created message to the message bus.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="category">The subscription category</param>
    private void PublishSubscriptionCreatedMessage(Guid subscriptionId, SubscriptionCategory category)
    {
        try
        {
            var message = MessageBusSubscriberCreatedMessage.Create(
                subscriptionId,
                typeof(TMessage),
                nameof(category));
            _messageBusService.PublishMessage(message);
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Failed to publish subscription created message for {subscriptionId}", ex);
        }
    }

    /// <summary>
    /// Publishes a subscription disposed message to the message bus.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="reason">The disposal reason</param>
    private void PublishSubscriptionDisposedMessage(Guid subscriptionId, string reason)
    {
        try
        {
            var message = MessageBusSubscriberDisposedMessage.Create(
                subscriptionId,
                typeof(TMessage),
                "Standard",
                $"Subscriber_{typeof(TMessage).Name}",
                reason ?? "Unknown");
            _messageBusService.PublishMessage(message);
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Failed to publish subscription disposed message for {subscriptionId}", ex);
        }
    }

    /// <summary>
    /// Publishes a subscription processed message to the message bus.
    /// </summary>
    private void PublishSubscriptionProcessedMessage(Guid subscriptionId, TMessage message, TimeSpan processingTime, SubscriptionCategory category)
    {
        try
        {
            var processedMessage = MessageBusSubscriptionProcessedMessage.Create(
                subscriptionId,
                typeof(TMessage),
                $"Subscriber_{typeof(TMessage).Name}",
                processingTime,
                true); // isSuccessful = true
            _messageBusService.PublishMessage(processedMessage);
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Failed to publish subscription processed message for {subscriptionId}", ex);
        }
    }

    /// <summary>
    /// Publishes a subscription failed message to the message bus.
    /// </summary>
    private void PublishSubscriptionFailedMessage(Guid subscriptionId, TMessage message, Exception exception, TimeSpan processingTime, SubscriptionCategory category)
    {
        try
        {
            var failedMessage = MessageBusSubscriptionFailedMessage.Create(
                subscriptionId,
                typeof(TMessage),
                $"Subscriber_{typeof(TMessage).Name}",
                exception); // Exception parameter
            _messageBusService.PublishMessage(failedMessage);
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Failed to publish subscription failed message for {subscriptionId}", ex);
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

        _loggingService.LogInfo($"[{_correlationId}] Disposing MessageSubscriber<{typeof(TMessage).Name}>");

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

            // Publish disposed message if enabled
            if (_config.EnableMessageBusIntegration)
            {
                try
                {
                    var subscriberId = DeterministicIdGenerator.GenerateCorrelationId("SubscriberDisposal", typeof(TMessage).Name);
                    var message = MessageBusSubscriberDisposedMessage.Create(
                        subscriberId,
                        typeof(TMessage),
                        "Standard",
                        $"Subscriber_{typeof(TMessage).Name}",
                        "Subscriber disposal");
                    _messageBusService.PublishMessage(message);
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"[{_correlationId}] Failed to publish subscriber disposed message", ex);
                }
            }

            _loggingService.LogInfo($"[{_correlationId}] MessageSubscriber<{typeof(TMessage).Name}> disposed successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{_correlationId}] Error during MessageSubscriber<{typeof(TMessage).Name}> disposal", ex);
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
        public Func<TMessage, UniTask> AsyncHandler { get; }
        public Func<TMessage, bool> SyncCondition { get; }
        public Func<TMessage, UniTask<bool>> AsyncCondition { get; }
        public MessagePriority MinPriority { get; }
        public FixedString128Bytes CorrelationId { get; }
        public SubscriptionCategory Category { get; }

        public SubscriptionInfo(
            Guid id,
            SubscriptionType type,
            Action<TMessage> syncHandler,
            Func<TMessage, UniTask> asyncHandler,
            Func<TMessage, bool> syncCondition,
            Func<TMessage, UniTask<bool>> asyncCondition,
            MessagePriority minPriority,
            FixedString128Bytes correlationId,
            SubscriptionCategory category)
        {
            Id = id;
            Type = type;
            SyncHandler = syncHandler;
            AsyncHandler = asyncHandler;
            SyncCondition = syncCondition;
            AsyncCondition = asyncCondition;
            MinPriority = minPriority;
            CorrelationId = correlationId;
            Category = category;
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
                _subscriber._loggingService.LogException($"[{_correlationId}] Error disposing managed subscription {_subscriptionId}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #endregion
}