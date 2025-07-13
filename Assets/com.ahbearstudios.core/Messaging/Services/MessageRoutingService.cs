using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Production-ready implementation of message routing service.
    /// Provides intelligent routing, filtering, and delivery of messages based on complex criteria.
    /// Follows AhBearStudios Core Development Guidelines with full core systems integration.
    /// </summary>
    public sealed class MessageRoutingService : IMessageRoutingService
    {
        #region Private Fields

        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        private readonly IMessageRegistry _messageRegistry;

        // Routing infrastructure
        private readonly ConcurrentDictionary<Guid, RoutingRule> _routingRules;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<Guid>> _typeToRulesMap;
        private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> _categoryToRulesMap;
        private readonly ConcurrentDictionary<MessagePriority, ConcurrentBag<Guid>> _priorityToRulesMap;

        // Route execution
        private readonly ConcurrentDictionary<Guid, RouteHandler> _routeHandlers;
        private readonly ConcurrentQueue<RoutedMessage> _routingQueue;
        private readonly SemaphoreSlim _routingSemaphore;

        // Performance optimization
        private readonly ConcurrentDictionary<Type, RouteCache> _routeCache;
        private readonly ReaderWriterLockSlim _routingLock;

        // State management
        private volatile bool _disposed;
        private volatile bool _initialized;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Statistics tracking
        private long _totalMessagesRouted;
        private long _totalRulesEvaluated;
        private long _totalRoutingFailures;
        private long _cacheHits;
        private long _cacheMisses;

        // Performance monitoring
        private readonly Timer _statisticsTimer;
        private readonly Timer _cacheCleanupTimer;
        private readonly Timer _queueProcessingTimer;
        private DateTime _lastStatsReset;

        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        // Configuration
        private readonly MessageRoutingConfig _config;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageRoutingService class.
        /// </summary>
        /// <param name="messageRegistry">The message registry service</param>
        /// <param name="logger">The logging service</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <param name="config">Optional routing configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageRoutingService(
            IMessageRegistry messageRegistry,
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService,
            IPoolingService poolingService,
            MessageRoutingConfig config = null)
        {
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _config = config ?? MessageRoutingConfig.Default;

            // Generate correlation ID for tracking
            _correlationId = $"MessageRouter_{Guid.NewGuid():N}"[..32];

            try
            {
                using var initScope = _profilerService.BeginScope("MessageRouting_Initialize");

                _logger.LogInfo($"[{_correlationId}] Initializing MessageRoutingService");

                // Initialize collections
                _routingRules = new ConcurrentDictionary<Guid, RoutingRule>();
                _typeToRulesMap = new ConcurrentDictionary<Type, ConcurrentBag<Guid>>();
                _categoryToRulesMap = new ConcurrentDictionary<string, ConcurrentBag<Guid>>();
                _priorityToRulesMap = new ConcurrentDictionary<MessagePriority, ConcurrentBag<Guid>>();
                _routeHandlers = new ConcurrentDictionary<Guid, RouteHandler>();
                _routingQueue = new ConcurrentQueue<RoutedMessage>();
                _routeCache = new ConcurrentDictionary<Type, RouteCache>();

                // Initialize synchronization
                _routingLock = new ReaderWriterLockSlim();
                _routingSemaphore = new SemaphoreSlim(_config.MaxConcurrentRoutes, _config.MaxConcurrentRoutes);
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize timers
                _lastStatsReset = DateTime.UtcNow;
                _statisticsTimer = new Timer(UpdateStatistics, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                _cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
                _queueProcessingTimer = new Timer(ProcessQueuedMessages, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

                // Register default routing rules
                RegisterDefaultRoutes();

                _initialized = true;

                _logger.LogInfo($"[{_correlationId}] MessageRoutingService initialized with {_routingRules.Count} routing rules");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to initialize MessageRoutingService");

                if (_alertService != null)
                {
                    _alertService.RaiseAlert(
                        $"MessageRoutingService initialization failed: {ex.Message}",
                        AlertSeverity.Critical,
                        "MessageRoutingService",
                        "Initialization");
                }

                throw;
            }
        }

        #endregion

        #region IMessageRoutingService Implementation

        /// <inheritdoc />
        public bool IsInitialized => _initialized && !_disposed;

        /// <inheritdoc />
        public int ActiveRuleCount => _routingRules.Count;

        /// <inheritdoc />
        public Guid AddRoutingRule(RoutingRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            ThrowIfDisposed();

            var ruleId = Guid.NewGuid();
            var ruleCorrelationId = $"{_correlationId}_{ruleId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"MessageRouting_AddRule_{rule.Name}");

                _logger.LogInfo($"[{ruleCorrelationId}] Adding routing rule '{rule.Name}'");

                // Validate rule
                ValidateRoutingRule(rule, ruleCorrelationId);

                _routingLock.EnterWriteLock();
                try
                {
                    // Create rule with ID
                    var ruleWithId = rule with { Id = ruleId };

                    // Add to main collection
                    _routingRules[ruleId] = ruleWithId;

                    // Update mapping indexes
                    UpdateRuleMappings(ruleWithId, true);

                    // Invalidate relevant caches
                    InvalidateCache(rule.MessageType);

                    _logger.LogInfo($"[{ruleCorrelationId}] Successfully added routing rule '{rule.Name}' with ID {ruleId}");

                    // Raise event
                    RoutingRuleAdded?.Invoke(this, new RoutingRuleEventArgs(ruleWithId, RoutingRuleOperation.Added));

                    return ruleId;
                }
                finally
                {
                    _routingLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{ruleCorrelationId}] Failed to add routing rule '{rule.Name}'");

                if (_alertService != null)
                {
                    _alertService.RaiseAlert(
                        $"Failed to add routing rule: {ex.Message}",
                        AlertSeverity.High,
                        "MessageRoutingService",
                        "RuleAddition");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public bool RemoveRoutingRule(Guid ruleId)
        {
            ThrowIfDisposed();

            var removalCorrelationId = $"{_correlationId}_{ruleId:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope("MessageRouting_RemoveRule");

                _logger.LogInfo($"[{removalCorrelationId}] Removing routing rule {ruleId}");

                _routingLock.EnterWriteLock();
                try
                {
                    if (!_routingRules.TryRemove(ruleId, out var removedRule))
                    {
                        _logger.LogWarning($"[{removalCorrelationId}] Routing rule {ruleId} not found");
                        return false;
                    }

                    // Update mapping indexes
                    UpdateRuleMappings(removedRule, false);

                    // Invalidate relevant caches
                    InvalidateCache(removedRule.MessageType);

                    _logger.LogInfo($"[{removalCorrelationId}] Successfully removed routing rule '{removedRule.Name}'");

                    // Raise event
                    RoutingRuleRemoved?.Invoke(this, new RoutingRuleEventArgs(removedRule, RoutingRuleOperation.Removed));

                    return true;
                }
                finally
                {
                    _routingLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{removalCorrelationId}] Failed to remove routing rule {ruleId}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<RouteResult> RouteMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            var routingCorrelationId = $"{_correlationId}_{message.Id:N}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"MessageRouting_Route_{typeof(TMessage).Name}");
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

                _logger.LogInfo($"[{routingCorrelationId}] Routing message {message.Id} of type {typeof(TMessage).Name}");

                Interlocked.Increment(ref _totalMessagesRouted);

                // Get applicable routing rules
                var applicableRules = await GetApplicableRulesAsync(message, combinedCts.Token);

                if (applicableRules.Count == 0)
                {
                    _logger.LogInfo($"[{routingCorrelationId}] No routing rules found for message {message.Id}");
                    return RouteResult.NoRoutes(message.Id);
                }

                // Execute routing rules
                var routeResults = new List<RouteExecution>();
                var routingTasks = new List<Task<RouteExecution>>();

                await _routingSemaphore.WaitAsync(combinedCts.Token);
                try
                {
                    foreach (var rule in applicableRules)
                    {
                        routingTasks.Add(ExecuteRoutingRuleAsync(message, rule, routingCorrelationId, combinedCts.Token));
                    }

                    var completedResults = await Task.WhenAll(routingTasks);
                    routeResults.AddRange(completedResults);
                }
                finally
                {
                    _routingSemaphore.Release();
                }

                // Analyze results
                var successfulRoutes = routeResults.Where(r => r.IsSuccess).ToArray();
                var failedRoutes = routeResults.Where(r => !r.IsSuccess).ToArray();

                if (failedRoutes.Length > 0)
                {
                    Interlocked.Add(ref _totalRoutingFailures, failedRoutes.Length);

                    _logger.LogWarning($"[{routingCorrelationId}] {failedRoutes.Length} routing rules failed for message {message.Id}");

                    if (_config.AlertOnRoutingFailures)
                    {
                        _alertService.RaiseAlert(
                            $"Message routing failures: {failedRoutes.Length} rules failed",
                            AlertSeverity.Medium,
                            "MessageRoutingService",
                            "RoutingFailure");
                    }
                }

                var result = new RouteResult(
                    message.Id,
                    successfulRoutes.Length,
                    failedRoutes.Length,
                    routeResults.ToArray(),
                    DateTime.UtcNow);

                _logger.LogInfo($"[{routingCorrelationId}] Completed routing for message {message.Id}: {successfulRoutes.Length} successful, {failedRoutes.Length} failed");

                // Raise event
                MessageRouted?.Invoke(this, new MessageRoutedEventArgs(message, result));

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"[{routingCorrelationId}] Message routing cancelled for {message.Id}");
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalRoutingFailures);
                _logger.LogException(ex, $"[{routingCorrelationId}] Failed to route message {message.Id}");

                if (_config.AlertOnRoutingFailures)
                {
                    _alertService.RaiseAlert(
                        $"Message routing failed: {ex.Message}",
                        AlertSeverity.High,
                        "MessageRoutingService",
                        "RoutingError");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public void RouteMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            // For synchronous routing, queue the message for background processing
            var routedMessage = new RoutedMessage(message, typeof(TMessage), DateTime.UtcNow);
            _routingQueue.Enqueue(routedMessage);

            _logger.LogInfo($"[{_correlationId}] Queued message {message.Id} for background routing");
        }

        /// <inheritdoc />
        public IEnumerable<RoutingRule> GetRoutingRules()
        {
            ThrowIfDisposed();

            _routingLock.EnterReadLock();
            try
            {
                return _routingRules.Values.ToArray();
            }
            finally
            {
                _routingLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IEnumerable<RoutingRule> GetRoutingRulesForType<TMessage>() where TMessage : IMessage
        {
            return GetRoutingRulesForType(typeof(TMessage));
        }

        /// <inheritdoc />
        public IEnumerable<RoutingRule> GetRoutingRulesForType(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            ThrowIfDisposed();

            var rules = new List<RoutingRule>();

            _routingLock.EnterReadLock();
            try
            {
                // Get rules by exact type match
                if (_typeToRulesMap.TryGetValue(messageType, out var ruleIds))
                {
                    foreach (var ruleId in ruleIds)
                    {
                        if (_routingRules.TryGetValue(ruleId, out var rule))
                        {
                            rules.Add(rule);
                        }
                    }
                }

                // Get rules by category if message registry is available
                if (_messageRegistry.IsRegistered(messageType))
                {
                    var typeInfo = _messageRegistry.GetMessageTypeInfo(messageType);
                    if (!string.IsNullOrEmpty(typeInfo.Category) && _categoryToRulesMap.TryGetValue(typeInfo.Category, out var categoryRuleIds))
                    {
                        foreach (var ruleId in categoryRuleIds)
                        {
                            if (_routingRules.TryGetValue(ruleId, out var rule) && !rules.Contains(rule))
                            {
                                rules.Add(rule);
                            }
                        }
                    }
                }

                return rules.ToArray();
            }
            finally
            {
                _routingLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public Guid RegisterRouteHandler(string name, Func<IMessage, CancellationToken, Task<bool>> handler)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Handler name cannot be null or empty", nameof(name));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            var handlerId = Guid.NewGuid();
            var handlerCorrelationId = $"{_correlationId}_{handlerId:N}"[..32];

            try
            {
                _logger.LogInfo($"[{handlerCorrelationId}] Registering route handler '{name}'");

                var routeHandler = new RouteHandler(handlerId, name, handler, DateTime.UtcNow);
                _routeHandlers[handlerId] = routeHandler;

                _logger.LogInfo($"[{handlerCorrelationId}] Successfully registered route handler '{name}' with ID {handlerId}");

                // Raise event
                RouteHandlerRegistered?.Invoke(this, new RouteHandlerEventArgs(routeHandler, RouteHandlerOperation.Registered));

                return handlerId;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{handlerCorrelationId}] Failed to register route handler '{name}'");
                throw;
            }
        }

        /// <inheritdoc />
        public bool UnregisterRouteHandler(Guid handlerId)
        {
            ThrowIfDisposed();

            var unregistrationCorrelationId = $"{_correlationId}_{handlerId:N}"[..32];

            try
            {
                _logger.LogInfo($"[{unregistrationCorrelationId}] Unregistering route handler {handlerId}");

                if (_routeHandlers.TryRemove(handlerId, out var removedHandler))
                {
                    _logger.LogInfo($"[{unregistrationCorrelationId}] Successfully unregistered route handler '{removedHandler.Name}'");

                    // Raise event
                    RouteHandlerUnregistered?.Invoke(this, new RouteHandlerEventArgs(removedHandler, RouteHandlerOperation.Unregistered));

                    return true;
                }

                _logger.LogWarning($"[{unregistrationCorrelationId}] Route handler {handlerId} not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{unregistrationCorrelationId}] Failed to unregister route handler {handlerId}");
                throw;
            }
        }

        /// <inheritdoc />
        public MessageRoutingStatistics GetStatistics()
        {
            try
            {
                var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
                var routingRate = timeSinceReset.TotalSeconds > 0 
                    ? _totalMessagesRouted / timeSinceReset.TotalSeconds 
                    : 0;

                var cacheHitRate = (_cacheHits + _cacheMisses) > 0 
                    ? (double)_cacheHits / (_cacheHits + _cacheMisses) 
                    : 0;

                var averageRulesPerMessage = _totalMessagesRouted > 0 
                    ? (double)_totalRulesEvaluated / _totalMessagesRouted 
                    : 0;

                return new MessageRoutingStatistics(
                    _totalMessagesRouted,
                    _totalRulesEvaluated,
                    _totalRoutingFailures,
                    _cacheHits,
                    _cacheMisses,
                    cacheHitRate,
                    routingRate,
                    averageRulesPerMessage,
                    ActiveRuleCount,
                    _routeHandlers.Count,
                    _routingQueue.Count,
                    DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to get routing statistics");
                return MessageRoutingStatistics.Empty;
            }
        }

        /// <inheritdoc />
        public void ClearRoutes()
        {
            ThrowIfDisposed();

            try
            {
                using var profilerScope = _profilerService?.BeginScope("MessageRouting_ClearRoutes");

                _logger.LogInfo($"[{_correlationId}] Clearing all routing rules and handlers");

                _routingLock.EnterWriteLock();
                try
                {
                    var removedRules = _routingRules.Values.ToArray();
                    var removedHandlers = _routeHandlers.Values.ToArray();

                    _routingRules.Clear();
                    _typeToRulesMap.Clear();
                    _categoryToRulesMap.Clear();
                    _priorityToRulesMap.Clear();
                    _routeHandlers.Clear();
                    _routeCache.Clear();

                    // Clear routing queue
                    while (_routingQueue.TryDequeue(out _))
                    {
                        // Clear all queued messages
                    }

                    _logger.LogInfo($"[{_correlationId}] Cleared {removedRules.Length} routing rules and {removedHandlers.Length} handlers");

                    // Raise events
                    RoutesCleared?.Invoke(this, new RoutesClearedEventArgs(removedRules, removedHandlers));
                }
                finally
                {
                    _routingLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to clear routes");
                throw;
            }
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MessageRoutedEventArgs> MessageRouted;

        /// <inheritdoc />
        public event EventHandler<RoutingRuleEventArgs> RoutingRuleAdded;

        /// <inheritdoc />
        public event EventHandler<RoutingRuleEventArgs> RoutingRuleRemoved;

        /// <inheritdoc />
        public event EventHandler<RouteHandlerEventArgs> RouteHandlerRegistered;

        /// <inheritdoc />
        public event EventHandler<RouteHandlerEventArgs> RouteHandlerUnregistered;

        /// <inheritdoc />
        public event EventHandler<RoutesClearedEventArgs> RoutesCleared;

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers default routing rules for system operations.
        /// </summary>
        private void RegisterDefaultRoutes()
        {
            try
            {
                _logger.LogInfo($"[{_correlationId}] Registering default routing rules");

                // Default rule for all messages (lowest priority)
                var defaultRule = new RoutingRule(
                    Guid.Empty,
                    "Default",
                    "Routes all messages to default handler",
                    typeof(IMessage),
                    null,
                    null,
                    MessagePriority.Low,
                    0, // Lowest priority
                    true,
                    DateTime.UtcNow);

                AddRoutingRule(defaultRule);

                _logger.LogInfo($"[{_correlationId}] Default routing rules registered");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to register default routing rules");
            }
        }

        /// <summary>
        /// Validates a routing rule for correctness.
        /// </summary>
        /// <param name="rule">The rule to validate</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <exception cref="ArgumentException">Thrown when rule is invalid</exception>
        private void ValidateRoutingRule(RoutingRule rule, FixedString128Bytes correlationId)
        {
            if (string.IsNullOrEmpty(rule.Name))
            {
                var errorMessage = "Routing rule name cannot be null or empty";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage);
            }

            if (rule.MessageType == null)
            {
                var errorMessage = "Routing rule message type cannot be null";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage);
            }

            if (!typeof(IMessage).IsAssignableFrom(rule.MessageType))
            {
                var errorMessage = $"Routing rule message type {rule.MessageType.Name} must implement IMessage";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage);
            }

            if (rule.Priority < 0)
            {
                var errorMessage = "Routing rule priority cannot be negative";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage);
            }
        }

        /// <summary>
        /// Updates the rule mapping indexes when adding or removing rules.
        /// </summary>
        /// <param name="rule">The routing rule</param>
        /// <param name="isAdding">True if adding, false if removing</param>
        private void UpdateRuleMappings(RoutingRule rule, bool isAdding)
        {
            if (isAdding)
            {
                // Add to type mapping
                _typeToRulesMap.AddOrUpdate(rule.MessageType,
                    new ConcurrentBag<Guid> { rule.Id },
                    (_, existing) =>
                    {
                        existing.Add(rule.Id);
                        return existing;
                    });

                // Add to category mapping if applicable
                if (!string.IsNullOrEmpty(rule.Category))
                {
                    _categoryToRulesMap.AddOrUpdate(rule.Category,
                        new ConcurrentBag<Guid> { rule.Id },
                        (_, existing) =>
                        {
                            existing.Add(rule.Id);
                            return existing;
                        });
                }

                // Add to priority mapping
                _priorityToRulesMap.AddOrUpdate(rule.MinPriority,
                    new ConcurrentBag<Guid> { rule.Id },
                    (_, existing) =>
                    {
                        existing.Add(rule.Id);
                        return existing;
                    });
            }
            else
            {
                // Note: ConcurrentBag doesn't support removal, so we'll rebuild on cleanup
                // For now, we'll mark the entries as stale and clean them up periodically
                InvalidateCache(rule.MessageType);
            }
        }

        /// <summary>
        /// Gets applicable routing rules for a message.
        /// </summary>
        /// <param name="message">The message to route</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of applicable routing rules</returns>
        private async Task<List<RoutingRule>> GetApplicableRulesAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : IMessage
        {
            var messageType = typeof(TMessage);

            // Check cache first
            if (_routeCache.TryGetValue(messageType, out var cachedRoutes) && !cachedRoutes.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return await FilterRulesAsync(cachedRoutes.Rules, message, cancellationToken);
            }

            Interlocked.Increment(ref _cacheMisses);

            _routingLock.EnterReadLock();
            try
            {
                var applicableRules = new List<RoutingRule>();

                // Get rules by exact type match
                if (_typeToRulesMap.TryGetValue(messageType, out var typeRuleIds))
                {
                    foreach (var ruleId in typeRuleIds)
                    {
                        if (_routingRules.TryGetValue(ruleId, out var rule))
                        {
                            applicableRules.Add(rule);
                        }
                    }
                }

                // Get rules by message inheritance hierarchy
                if (_messageRegistry.IsRegistered(messageType))
                {
                    var derivedTypes = _messageRegistry.GetDerivedTypes(messageType);
                    foreach (var derivedType in derivedTypes)
                    {
                        if (_typeToRulesMap.TryGetValue(derivedType, out var derivedRuleIds))
                        {
                            foreach (var ruleId in derivedRuleIds)
                            {
                                if (_routingRules.TryGetValue(ruleId, out var rule) && !applicableRules.Contains(rule))
                                {
                                    applicableRules.Add(rule);
                                }
                            }
                        }
                    }

                    // Get rules by category
                    var typeInfo = _messageRegistry.GetMessageTypeInfo(messageType);
                    if (!string.IsNullOrEmpty(typeInfo.Category) && _categoryToRulesMap.TryGetValue(typeInfo.Category, out var categoryRuleIds))
                    {
                        foreach (var ruleId in categoryRuleIds)
                        {
                            if (_routingRules.TryGetValue(ruleId, out var rule) && !applicableRules.Contains(rule))
                            {
                                applicableRules.Add(rule);
                            }
                        }
                    }
                }

                // Sort by priority (highest first)
                applicableRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                // Update cache
                var cacheEntry = new RouteCache(applicableRules.ToArray(), DateTime.UtcNow.Add(_config.CacheDuration));
                _routeCache[messageType] = cacheEntry;

                Interlocked.Add(ref _totalRulesEvaluated, applicableRules.Count);

                return await FilterRulesAsync(applicableRules.ToArray(), message, cancellationToken);
            }
            finally
            {
                _routingLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Filters routing rules based on message content and conditions.
        /// </summary>
        /// <param name="rules">The rules to filter</param>
        /// <param name="message">The message to evaluate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Filtered list of applicable rules</returns>
        private async Task<List<RoutingRule>> FilterRulesAsync<TMessage>(RoutingRule[] rules, TMessage message, CancellationToken cancellationToken) where TMessage : IMessage
        {
            var filteredRules = new List<RoutingRule>();

            foreach (var rule in rules)
            {
                try
                {
                    // Check if rule is enabled
                    if (!rule.IsEnabled)
                        continue;

                    // Check priority filter
                    if (message.Priority < rule.MinPriority)
                        continue;

                    // Check source filter if specified
                    if (!string.IsNullOrEmpty(rule.SourceFilter) && message.Source.ToString() != rule.SourceFilter)
                        continue;

                    // Check custom condition if specified
                    if (rule.Condition != null)
                    {
                        var conditionResult = await EvaluateConditionAsync(rule.Condition, message, cancellationToken);
                        if (!conditionResult)
                            continue;
                    }

                    filteredRules.Add(rule);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"[{_correlationId}] Failed to evaluate routing rule '{rule.Name}' for message {message.Id}");
                }
            }

            return filteredRules;
        }

        /// <summary>
        /// Evaluates a custom routing condition.
        /// </summary>
        /// <param name="condition">The condition function</param>
        /// <param name="message">The message to evaluate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if condition passes, false otherwise</returns>
        private async Task<bool> EvaluateConditionAsync(Func<IMessage, CancellationToken, Task<bool>> condition, IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                return await condition(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Custom routing condition failed for message {message.Id}");
                return false;
            }
        }

        /// <summary>
        /// Executes a routing rule for a message.
        /// </summary>
        /// <param name="message">The message to route</param>
        /// <param name="rule">The routing rule to execute</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Route execution result</returns>
        private async Task<RouteExecution> ExecuteRoutingRuleAsync<TMessage>(TMessage message, RoutingRule rule, FixedString128Bytes correlationId, CancellationToken cancellationToken) where TMessage : IMessage
        {
            var executionId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"[{correlationId}] Executing routing rule '{rule.Name}' for message {message.Id}");

                // Get route handlers for this rule
                var handlers = GetRouteHandlersForRule(rule);

                if (handlers.Length == 0)
                {
                    _logger.LogWarning($"[{correlationId}] No route handlers found for rule '{rule.Name}'");
                    return RouteExecution.Failed(executionId, rule.Id, rule.Name, "No handlers available", DateTime.UtcNow - startTime);
                }

                var handlerResults = new List<HandlerExecutionResult>();

                // Execute all handlers for this rule
                foreach (var handler in handlers)
                {
                    try
                    {
                        var handlerStartTime = DateTime.UtcNow;
                        var success = await handler.Handler(message, cancellationToken);
                        var handlerDuration = DateTime.UtcNow - handlerStartTime;

                        handlerResults.Add(new HandlerExecutionResult(
                            handler.Id,
                            handler.Name,
                            success,
                            null,
                            handlerDuration));

                        _logger.LogInfo($"[{correlationId}] Handler '{handler.Name}' executed {(success ? "successfully" : "with failure")} in {handlerDuration.TotalMilliseconds:F2}ms");
                    }
                    catch (Exception handlerEx)
                    {
                        var handlerDuration = DateTime.UtcNow - DateTime.UtcNow; // Reset for error case
                        handlerResults.Add(new HandlerExecutionResult(
                            handler.Id,
                            handler.Name,
                            false,
                            handlerEx.Message,
                            handlerDuration));

                        _logger.LogException(handlerEx, $"[{correlationId}] Handler '{handler.Name}' failed for rule '{rule.Name}'");
                    }
                }

                var overallSuccess = handlerResults.All(r => r.Success);
                var duration = DateTime.UtcNow - startTime;

                if (overallSuccess)
                {
                    _logger.LogInfo($"[{correlationId}] Successfully executed routing rule '{rule.Name}' in {duration.TotalMilliseconds:F2}ms");
                    return RouteExecution.Success(executionId, rule.Id, rule.Name, handlerResults.ToArray(), duration);
                }
                else
                {
                    var failedHandlers = handlerResults.Where(r => !r.Success).Count();
                    _logger.LogWarning($"[{correlationId}] Routing rule '{rule.Name}' completed with {failedHandlers} handler failures");
                    return RouteExecution.PartialSuccess(executionId, rule.Id, rule.Name, handlerResults.ToArray(), duration);
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogException(ex, $"[{correlationId}] Failed to execute routing rule '{rule.Name}'");
                return RouteExecution.Failed(executionId, rule.Id, rule.Name, ex.Message, duration);
            }
        }

        /// <summary>
        /// Gets route handlers for a specific routing rule.
        /// </summary>
        /// <param name="rule">The routing rule</param>
        /// <returns>Array of applicable route handlers</returns>
        private RouteHandler[] GetRouteHandlersForRule(RoutingRule rule)
        {
            var handlers = new List<RouteHandler>();

            foreach (var handler in _routeHandlers.Values)
            {
                // For now, all handlers handle all rules
                // This could be extended with more sophisticated matching
                handlers.Add(handler);
            }

            return handlers.ToArray();
        }

        /// <summary>
        /// Invalidates cache entries for a specific message type.
        /// </summary>
        /// <param name="messageType">The message type to invalidate</param>
        private void InvalidateCache(Type messageType)
        {
            _routeCache.TryRemove(messageType, out _);

            // Also invalidate related types
            if (_messageRegistry.IsRegistered(messageType))
            {
                var derivedTypes = _messageRegistry.GetDerivedTypes(messageType);
                foreach (var derivedType in derivedTypes)
                {
                    _routeCache.TryRemove(derivedType, out _);
                }
            }
        }

        /// <summary>
        /// Processes queued messages for background routing (timer callback).
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private async void ProcessQueuedMessages(object state)
        {
            if (_disposed) return;

            try
            {
                var processedCount = 0;
                var maxProcessing = Math.Min(_config.MaxConcurrentRoutes, _routingQueue.Count);

                var processingTasks = new List<Task>();

                for (int i = 0; i < maxProcessing && _routingQueue.TryDequeue(out var routedMessage); i++)
                {
                    processingTasks.Add(ProcessQueuedMessageAsync(routedMessage));
                    processedCount++;
                }

                if (processingTasks.Count > 0)
                {
                    await Task.WhenAll(processingTasks);
                    _logger.LogInfo($"[{_correlationId}] Processed {processedCount} queued messages");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to process queued messages");
            }
        }

        /// <summary>
        /// Processes a single queued message.
        /// </summary>
        /// <param name="routedMessage">The message to process</param>
        private async Task ProcessQueuedMessageAsync(RoutedMessage routedMessage)
        {
            try
            {
                // Use reflection to call the generic RouteMessageAsync method
                var method = typeof(MessageRoutingService).GetMethod(nameof(RouteMessageAsync));
                var genericMethod = method.MakeGenericMethod(routedMessage.MessageType);
                
                var task = (Task<RouteResult>)genericMethod.Invoke(this, new object[] { routedMessage.Message, CancellationToken.None });
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to process queued message {routedMessage.Message.Id}");
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
                _logger.LogException(ex, $"[{_correlationId}] Failed to update routing statistics");
            }
        }

        /// <summary>
        /// Cleans up expired cache entries (timer callback).
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void CleanupCache(object state)
        {
            if (_disposed) return;

            try
            {
                var expiredKeys = new List<Type>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _routeCache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _routeCache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogInfo($"[{_correlationId}] Cleaned up {expiredKeys.Count} expired cache entries");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to cleanup cache");
            }
        }

        /// <summary>
        /// Throws an exception if the service has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when service is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageRoutingService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message routing service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessageRoutingService");

            try
            {
                _disposed = true;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _statisticsTimer?.Dispose();
                _cacheCleanupTimer?.Dispose();
                _queueProcessingTimer?.Dispose();

                // Clear all collections
                _routingLock?.EnterWriteLock();
                try
                {
                    _routingRules.Clear();
                    _typeToRulesMap.Clear();
                    _categoryToRulesMap.Clear();
                    _priorityToRulesMap.Clear();
                    _routeHandlers.Clear();
                    _routeCache.Clear();

                    // Clear routing queue
                    while (_routingQueue.TryDequeue(out _))
                    {
                        // Clear all queued messages
                    }
                }
                finally
                {
                    _routingLock?.ExitWriteLock();
                }

                // Dispose synchronization objects
                _routingLock?.Dispose();
                _routingSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageRoutingService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Error during MessageRoutingService disposal");
            }
        }

        #endregion

        #region Helper Classes and Records

        /// <summary>
        /// Represents a cached route entry with expiration.
        /// </summary>
        private readonly record struct RouteCache(RoutingRule[] Rules, DateTime ExpiresAt)
        {
            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }

        /// <summary>
        /// Represents a queued message for background routing.
        /// </summary>
        private readonly record struct RoutedMessage(IMessage Message, Type MessageType, DateTime QueuedAt);

        /// <summary>
        /// Configuration for message routing service.
        /// </summary>
        public sealed record MessageRoutingConfig
        {
            public int MaxConcurrentRoutes { get; init; } = 100;
            public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
            public bool AlertOnRoutingFailures { get; init; } = true;
            public int MaxQueueSize { get; init; } = 10000;

            public static MessageRoutingConfig Default => new();
        }

        #endregion
    }
}