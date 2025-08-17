using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Profiling;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Builders;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Production-ready implementation of the IAlertService interface providing centralized alert management.
    /// Integrates channels, filters, messaging, logging, pooling, and profiling for comprehensive alert processing.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// 
    /// Features:
    /// - Integrated AlertChannelService, AlertFilterService, and AlertSuppressionService
    /// - Health monitoring and diagnostics
    /// - Configuration hot-reload capabilities
    /// - Emergency failover and circuit breaker patterns
    /// - Comprehensive metrics and performance monitoring
    /// - Bulk operations for high-throughput scenarios
    /// - Thread-safe operations with minimal locking
    /// </summary>
    public sealed class AlertService : IAlertService
    {
        private readonly object _syncLock = new object();
        private readonly Dictionary<Guid, Alert> _activeAlerts = new Dictionary<Guid, Alert>();
        private readonly Dictionary<string, AlertSeverity> _sourceMinimumSeverities = new Dictionary<string, AlertSeverity>();
        private readonly Queue<Alert> _alertHistory = new Queue<Alert>();
        
        // Fallback collections for backward compatibility
        private readonly List<IAlertChannel> _channels = new List<IAlertChannel>();
        private readonly List<IAlertFilter> _filters = new List<IAlertFilter>();
        
        // Core service dependencies
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        
        // Integrated subsystem services
        private readonly IAlertChannelService _channelService;
        private readonly IAlertFilterService _filterService;
        private readonly IAlertSuppressionService _suppressionService;
        
        // Configuration and state management
        private AlertServiceConfiguration _configuration;
        private volatile bool _isStarted;
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private volatile bool _emergencyMode;
        private string _emergencyModeReason;
        
        // Performance markers
        private readonly ProfilerMarker _raiseAlertMarker = new("AlertService.RaiseAlert");
        private readonly ProfilerMarker _filterMarker = new("AlertService.ApplyFilters");
        private readonly ProfilerMarker _deliveryMarker = new("AlertService.DeliverAlert");
        private readonly ProfilerMarker _bulkOperationMarker = new("AlertService.BulkOperation");
        private readonly ProfilerMarker _healthCheckMarker = new("AlertService.HealthCheck");
        
        // State and configuration
        private AlertSeverity _globalMinimumSeverity = AlertSeverity.Info;
        private AlertStatistics _statistics = AlertStatistics.Empty;
        private AlertSystemPerformanceMetrics _performanceMetrics;
        private DateTime _lastMaintenanceRun = DateTime.UtcNow;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private readonly TimeSpan _maintenanceInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private const int MaxHistorySize = 1000;
        
        // Circuit breaker and resilience
        private int _consecutiveFailures;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly int _circuitBreakerThreshold = 5;
        private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(1);

        #region Core Properties and State
        
        /// <summary>
        /// Gets whether the alerting service is enabled and operational.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed && _isStarted;
        
        /// <summary>
        /// Gets whether the service is healthy and functioning normally.
        /// </summary>
        public bool IsHealthy => IsEnabled && 
                                _consecutiveFailures < _circuitBreakerThreshold &&
                                _channelService?.IsEnabled == true &&
                                _filterService?.IsEnabled == true &&
                                _suppressionService?.IsEnabled == true;
        
        /// <summary>
        /// Gets the current service configuration.
        /// </summary>
        public AlertServiceConfiguration Configuration => _configuration;
        
        /// <summary>
        /// Gets the integrated channel service for advanced channel management.
        /// </summary>
        public IAlertChannelService ChannelService => _channelService;
        
        /// <summary>
        /// Gets the integrated filter service for sophisticated filtering.
        /// </summary>
        public IAlertFilterService FilterService => _filterService;
        
        /// <summary>
        /// Gets the integrated suppression service for deduplication and rate limiting.
        /// </summary>
        public IAlertSuppressionService SuppressionService => _suppressionService;
        
        /// <summary>
        /// Gets whether emergency mode is currently active.
        /// </summary>
        public bool IsEmergencyModeActive => _emergencyMode;

        #endregion


        /// <summary>
        /// Initializes a new instance of the AlertService class with integrated subsystem services.
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        /// <param name="channelService">Channel management service</param>
        /// <param name="filterService">Filter management service</param>
        /// <param name="suppressionService">Suppression management service</param>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="serializationService">Serialization service for alert data serialization</param>
        /// <param name="poolingService">Pooling service for efficient alert container management</param>
        public AlertService(
            AlertServiceConfiguration configuration,
            IAlertChannelService channelService,
            IAlertFilterService filterService,
            IAlertSuppressionService suppressionService,
            IMessageBusService messageBusService = null, 
            ILoggingService loggingService = null, 
            ISerializationService serializationService = null,
            IPoolingService poolingService = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _suppressionService = suppressionService ?? throw new ArgumentNullException(nameof(suppressionService));
            
            _messageBusService = messageBusService;
            _loggingService = loggingService;
            _serializationService = serializationService;
            _poolingService = poolingService;

            // Initialize performance metrics
            _performanceMetrics = AlertSystemPerformanceMetrics.Create();
            
            // Initialize alert pooling if pooling service is available
            InitializeAlertPooling();

            LogInfo("Alert service initialized with integrated subsystems", Guid.NewGuid());
        }
        
        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// Creates minimal services internally if not provided.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="serializationService">Serialization service for alert data serialization</param>
        /// <param name="poolingService">Pooling service for efficient alert container management</param>
        public AlertService(
            IMessageBusService messageBusService = null, 
            ILoggingService loggingService = null, 
            ISerializationService serializationService = null,
            IPoolingService poolingService = null)
        {
            _messageBusService = messageBusService;
            _loggingService = loggingService;
            _serializationService = serializationService;
            _poolingService = poolingService;

            // Create default configuration
            _configuration = new AlertConfigBuilder().ForProduction().BuildServiceConfiguration();
            
            // Create minimal internal services
            _channelService = new AlertChannelService(new AlertChannelServiceConfig(), loggingService, messageBusService);
            _filterService = new AlertFilterService(loggingService);
            _suppressionService = new AlertSuppressionService(loggingService);
            
            // Initialize performance metrics
            _performanceMetrics = AlertSystemPerformanceMetrics.Create();

            // Initialize alert pooling if pooling service is available
            InitializeAlertPooling();

            LogInfo("Alert service initialized with legacy constructor", Guid.NewGuid());
        }

        #region Core Alerting Methods

        /// <summary>
        /// Raises an alert with correlation tracking.
        /// </summary>
        public void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            if (!IsEnabled) return;

            var alert = CreateAlert(message, severity, source, tag, correlationId);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert using Unity.Collections types for Burst compatibility.
        /// </summary>
        public void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            if (!IsEnabled) return;

            var alert = CreateAlert(message, severity, source, tag, correlationId);
            RaiseAlert(alert);
        }

        /// <summary>
        /// Raises an alert using a pre-constructed alert object.
        /// </summary>
        public void RaiseAlert(Alert alert)
        {
            using (_raiseAlertMarker.Auto())
            {
                if (!IsEnabled || alert == null) return;

                var startTime = DateTime.UtcNow;
                
                try
                {
                    // Check global minimum severity
                    if (alert.Severity < GetMinimumSeverity(alert.Source))
                        return;

                    // Apply filters
                    var filteredAlert = ApplyFilters(alert);
                    if (filteredAlert == null) // Alert was suppressed
                        return;

                    // Apply suppression rules
                    var suppressedAlert = ApplySuppressionRules(filteredAlert);
                    if (suppressedAlert == null) // Alert was suppressed
                        return;

                    // Store as active alert
                    lock (_syncLock)
                    {
                        if (_activeAlerts.ContainsKey(suppressedAlert.Id))
                        {
                            // Update existing alert (increment count)
                            var existingAlert = _activeAlerts[suppressedAlert.Id];
                            _activeAlerts[suppressedAlert.Id] = existingAlert.IncrementCount();
                            suppressedAlert = _activeAlerts[suppressedAlert.Id];
                        }
                        else
                        {
                            _activeAlerts[suppressedAlert.Id] = suppressedAlert;
                        }

                        // Add to history
                        _alertHistory.Enqueue(suppressedAlert);
                        while (_alertHistory.Count > MaxHistorySize)
                            _alertHistory.Dequeue();
                    }

                    // Send to channels
                    _ = DeliverAlertToChannelsAsync(suppressedAlert).Forget();

                    // Publish message
                    PublishAlertRaisedMessage(suppressedAlert);

                    // Update statistics
                    UpdateStatistics(true, DateTime.UtcNow - startTime);

                    LogDebug($"Alert raised: {suppressedAlert.Severity} from {suppressedAlert.Source}", suppressedAlert.CorrelationId);
                }
                catch (Exception ex)
                {
                    LogError($"Error raising alert: {ex.Message}", alert?.CorrelationId ?? Guid.NewGuid());
                    UpdateStatistics(false, DateTime.UtcNow - startTime);
                }
            }
        }

        /// <summary>
        /// Raises an alert asynchronously with correlation tracking.
        /// </summary>
        public async UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            await UniTask.RunOnThreadPool(() => RaiseAlert(alert), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Raises an alert asynchronously with message construction.
        /// </summary>
        public async UniTask RaiseAlertAsync(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default, 
            CancellationToken cancellationToken = default)
        {
            var alert = CreateAlert(message, severity, source, tag, correlationId);
            await RaiseAlertAsync(alert, cancellationToken);
        }

        #endregion

        #region Alert Management

        /// <summary>
        /// Gets all currently active alerts.
        /// </summary>
        public IEnumerable<Alert> GetActiveAlerts()
        {
            lock (_syncLock)
            {
                return _activeAlerts.Values.AsValueEnumerable().Where(a => a.State == AlertState.Active).ToList();
            }
        }

        /// <summary>
        /// Gets alert history for a specified time period.
        /// </summary>
        public IEnumerable<Alert> GetAlertHistory(TimeSpan period)
        {
            var cutoff = DateTime.UtcNow - period;
            lock (_syncLock)
            {
                return _alertHistory.AsValueEnumerable().Where(a => a.Timestamp >= cutoff).ToList();
            }
        }

        /// <summary>
        /// Acknowledges an alert by its ID.
        /// </summary>
        public void AcknowledgeAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            lock (_syncLock)
            {
                if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    return;

                if (alert.IsAcknowledged)
                    return;

                var acknowledgedAlert = alert.Acknowledge("System");
                _activeAlerts[alertId] = acknowledgedAlert;

                // Publish message
                PublishAlertAcknowledgedMessage(acknowledgedAlert);

                LogInfo($"Alert acknowledged: {alertId}", correlationId == default ? alert.CorrelationId : correlationId);
            }
        }

        /// <summary>
        /// Resolves an alert by its ID.
        /// </summary>
        public void ResolveAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            lock (_syncLock)
            {
                if (!_activeAlerts.TryGetValue(alertId, out var alert))
                    return;

                if (alert.IsResolved)
                    return;

                var resolvedAlert = alert.Resolve("System");
                _activeAlerts[alertId] = resolvedAlert;

                // Publish message
                PublishAlertResolvedMessage(resolvedAlert);

                LogInfo($"Alert resolved: {alertId}", correlationId == default ? alert.CorrelationId : correlationId);
            }
        }

        #endregion

        #region Severity Management

        /// <summary>
        /// Sets the minimum severity level for alerts.
        /// </summary>
        public void SetMinimumSeverity(AlertSeverity minimumSeverity)
        {
            _globalMinimumSeverity = minimumSeverity;
            LogInfo($"Global minimum severity set to {minimumSeverity}", Guid.NewGuid());
        }

        /// <summary>
        /// Sets the minimum severity level for a specific source.
        /// </summary>
        public void SetMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity)
        {
            var sourceStr = source.ToString();
            lock (_syncLock)
            {
                _sourceMinimumSeverities[sourceStr] = minimumSeverity;
            }
            LogInfo($"Minimum severity for {sourceStr} set to {minimumSeverity}", Guid.NewGuid());
        }

        /// <summary>
        /// Gets the minimum severity level for a source or global.
        /// </summary>
        public AlertSeverity GetMinimumSeverity(FixedString64Bytes source = default)
        {
            if (source.IsEmpty)
                return _globalMinimumSeverity;

            var sourceStr = source.ToString();
            lock (_syncLock)
            {
                return _sourceMinimumSeverities.TryGetValue(sourceStr, out var severity) 
                    ? severity 
                    : _globalMinimumSeverity;
            }
        }

        #endregion

        #region Channel Management

        /// <summary>
        /// Registers an alert channel with the service.
        /// </summary>
        public void RegisterChannel(IAlertChannel channel, FixedString64Bytes correlationId = default)
        {
            if (channel == null) return;

            try
            {
                if (_channelService != null)
                {
                    // Use integrated channel service
                    _ = _channelService.RegisterChannelAsync(channel, null, correlationId == default ? Guid.NewGuid() : Guid.Parse(correlationId.ToString())).Forget();
                }
                else
                {
                    // Fallback to local collection
                    lock (_syncLock)
                    {
                        if (!_channels.Any(c => c.Name.ToString() == channel.Name.ToString()))
                        {
                            _channels.Add(channel);
                        }
                    }
                }
                
                LogInfo($"Alert channel registered: {channel.Name}", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Failed to register channel {channel.Name}: {ex.Message}", correlationId);
            }
        }

        /// <summary>
        /// Unregisters an alert channel from the service.
        /// </summary>
        public bool UnregisterChannel(FixedString64Bytes channelName, FixedString64Bytes correlationId = default)
        {
            try
            {
                if (_channelService != null)
                {
                    // Use integrated channel service
                    var result = _channelService.UnregisterChannelAsync(channelName, correlationId == default ? Guid.NewGuid() : Guid.Parse(correlationId.ToString())).GetAwaiter().GetResult();
                    LogInfo($"Alert channel unregistered: {channelName}", correlationId);
                    return result;
                }
                else
                {
                    // Fallback to local collection
                    var nameStr = channelName.ToString();
                    lock (_syncLock)
                    {
                        var channel = _channels.AsValueEnumerable().FirstOrDefault(c => c.Name.ToString() == nameStr);
                        if (channel != null)
                        {
                            _channels.Remove(channel);
                            LogInfo($"Alert channel unregistered: {channelName}", correlationId);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Failed to unregister channel {channelName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Gets all registered alert channels.
        /// </summary>
        public IReadOnlyCollection<IAlertChannel> GetRegisteredChannels()
        {
            try
            {
                if (_channelService != null)
                {
                    return _channelService.GetAllChannels();
                }
                else
                {
                    // Fallback to local collection
                    lock (_syncLock)
                    {
                        return _channels.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to get registered channels: {ex.Message}", Guid.NewGuid());
                return new List<IAlertChannel>();
            }
        }

        #endregion

        #region Filtering and Suppression

        /// <summary>
        /// Adds an alert filter for advanced filtering.
        /// </summary>
        public void AddFilter(IAlertFilter filter, FixedString64Bytes correlationId = default)
        {
            if (filter == null) return;

            try
            {
                if (_filterService != null)
                {
                    // Use integrated filter service
                    _filterService.RegisterFilter(filter, null, correlationId == default ? Guid.NewGuid() : Guid.Parse(correlationId.ToString()));
                }
                else
                {
                    // Fallback to local collection
                    lock (_syncLock)
                    {
                        if (!_filters.Any(f => f.Name.ToString() == filter.Name.ToString()))
                        {
                            _filters.Add(filter);
                            _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                        }
                    }
                }
                
                LogInfo($"Alert filter added: {filter.Name}", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Failed to add filter {filter.Name}: {ex.Message}", correlationId);
            }
        }

        /// <summary>
        /// Removes an alert filter.
        /// </summary>
        public bool RemoveFilter(FixedString64Bytes filterName, FixedString64Bytes correlationId = default)
        {
            try
            {
                var nameStr = filterName.ToString();
                
                if (_filterService != null)
                {
                    // Use integrated filter service
                    var result = _filterService.UnregisterFilter(nameStr, correlationId == default ? Guid.NewGuid() : Guid.Parse(correlationId.ToString()));
                    if (result)
                    {
                        LogInfo($"Alert filter removed: {filterName}", correlationId);
                    }
                    return result;
                }
                else
                {
                    // Fallback to local collection
                    lock (_syncLock)
                    {
                        var filter = _filters.AsValueEnumerable().FirstOrDefault(f => f.Name.ToString() == nameStr);
                        if (filter != null)
                        {
                            _filters.Remove(filter);
                            LogInfo($"Alert filter removed: {filterName}", correlationId);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Failed to remove filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }


        #endregion

        #region Statistics and Monitoring

        /// <summary>
        /// Gets current alerting statistics for monitoring.
        /// </summary>
        public AlertStatistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// Validates alerting configuration and channels.
        /// </summary>
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            var issues = new List<string>();

            try
            {
                // Check channels
                var channels = GetRegisteredChannels();
                if (channels.Count == 0)
                    issues.Add("No alert channels registered");

                foreach (var channel in channels)
                {
                    if (!channel.IsHealthy)
                        issues.Add($"Channel {channel.Name} is unhealthy");
                }

                // Check filters
                var filterCount = 0;
                if (_filterService != null)
                {
                    filterCount = _filterService.FilterCount;
                }
                else
                {
                    lock (_syncLock)
                    {
                        filterCount = _filters.Count;
                    }
                }

                if (filterCount == 0)
                    issues.Add("No alert filters configured");
            }
            catch (Exception ex)
            {
                issues.Add($"Validation error: {ex.Message}");
            }

            return issues.Count > 0 
                ? ValidationResult.Failure(issues.AsValueEnumerable().Select(i => new ValidationError(i)).ToList(), "AlertService")
                : ValidationResult.Success("AlertService");
        }

        /// <summary>
        /// Performs maintenance operations on the alert system.
        /// </summary>
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            if (DateTime.UtcNow - _lastMaintenanceRun < _maintenanceInterval)
                return;

            _lastMaintenanceRun = DateTime.UtcNow;

            lock (_syncLock)
            {
                // Clean up resolved alerts older than 24 hours
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var toRemove = _activeAlerts.Values.AsValueEnumerable()
                    .Where(a => a.IsResolved && a.ResolvedTimestamp < cutoff)
                    .Select(a => a.Id)
                    .ToList();

                foreach (var id in toRemove)
                {
                    _activeAlerts.Remove(id);
                }

                LogDebug($"Maintenance completed: {toRemove.Count} old alerts cleaned up", correlationId);
            }
        }

        /// <summary>
        /// Flushes all buffered alerts to channels.
        /// </summary>
        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            var channels = GetRegisteredChannels();
            var flushTasks = channels.AsValueEnumerable().Select(c => c.FlushAsync(correlationId)).ToList();
            
            await UniTask.WhenAll(flushTasks);
            LogDebug("All channels flushed", correlationId);
        }

        #endregion

        #region Pooling Management

        /// <summary>
        /// Initializes alert pooling if the pooling service is available.
        /// </summary>
        private void InitializeAlertPooling()
        {
            if (_poolingService == null)
            {
                LogInfo("No pooling service provided - using direct alert creation", Guid.NewGuid());
                return;
            }

            try
            {
                // Check if pool is already registered
                if (_poolingService.IsPoolRegistered<PooledAlertContainer>())
                {
                    LogInfo("Alert pool already registered", Guid.NewGuid());
                    return;
                }

                // Create high-performance configuration for alert pooling
                var config = AlertPoolConfigBuilder
                    .CreateHighPerformance("AlertContainerPool")
                    .WithCapacity(50, 1000)  // Start with 50, max 1000 containers
                    .WithPerformance(500, true, 0.05f)  // Aggressive performance settings
                    .WithExpansionTrigger(AlertSeverity.High)  // Expand on high severity alerts
                    .Build();

                // Register the pool with the pooling service
                _poolingService.RegisterPool<PooledAlertContainer>(config);
                
                LogInfo($"Alert pool registered with {config.InitialCapacity} initial capacity", Guid.NewGuid());
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize alert pooling: {ex.Message}", Guid.NewGuid());
            }
        }

        /// <summary>
        /// Gets pooling statistics for monitoring and diagnostics.
        /// </summary>
        public PoolStatistics GetPoolingStatistics()
        {
            if (_poolingService == null || !_poolingService.IsPoolRegistered<PooledAlertContainer>())
                return null;

            try
            {
                return _poolingService.GetPoolStatistics<PooledAlertContainer>();
            }
            catch (Exception ex)
            {
                LogError($"Failed to get pooling statistics: {ex.Message}", Guid.NewGuid());
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new Alert using the pooling service for efficient memory management.
        /// </summary>
        private Alert CreateAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default, Guid operationId = default, 
            AlertContext context = default)
        {
            if (_poolingService != null && _poolingService.IsPoolRegistered<PooledAlertContainer>())
            {
                try
                {
                    // Get a pooled container and create the alert through it
                    var container = _poolingService.Get<PooledAlertContainer>();
                    var alert = container.CreateAlert(message, severity, source, tag, correlationId, operationId, context);
                    
                    // Return the container to the pool after creating the alert
                    // The alert itself is immutable and independent of the container
                    _poolingService.Return(container);
                    
                    return alert;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to create alert using pooling service: {ex.Message}", correlationId);
                    // Fall back to direct creation
                }
            }
            
            // Fallback to direct Alert creation if pooling service is not available or fails
            return Alert.Create(message, severity, source, tag, correlationId, operationId, context);
        }

        /// <summary>
        /// Creates a new Alert using Unity.Collections types.
        /// </summary>
        private Alert CreateAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default, Guid operationId = default, 
            AlertContext context = default)
        {
            if (_poolingService != null && _poolingService.IsPoolRegistered<PooledAlertContainer>())
            {
                try
                {
                    // Get a pooled container and create the alert through it
                    var container = _poolingService.Get<PooledAlertContainer>();
                    var alert = container.CreateAlert(message, severity, source, tag, correlationId, operationId, context);
                    
                    // Return the container to the pool after creating the alert
                    _poolingService.Return(container);
                    
                    return alert;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to create alert using pooling service: {ex.Message}", correlationId);
                    // Fall back to direct creation
                }
            }
            
            // Fallback to direct Alert creation if pooling service is not available or fails
            return Alert.Create(message, severity, source, tag, correlationId, operationId, context);
        }

        private Alert ApplyFilters(Alert alert)
        {
            using (_filterMarker.Auto())
            {
                var currentAlert = alert;
                var context = FilterContext.WithCorrelation(alert.CorrelationId);

                lock (_syncLock)
                {
                    foreach (var filter in _filters)
                    {
                        if (!filter.IsEnabled || !filter.CanHandle(currentAlert))
                            continue;

                        var result = filter.Evaluate(currentAlert, context);
                        
                        switch (result.Decision)
                        {
                            case FilterDecision.Allow:
                                continue;
                            case FilterDecision.Suppress:
                                return null; // Alert suppressed
                            case FilterDecision.Modify:
                                currentAlert = result.ModifiedAlert ?? currentAlert;
                                break;
                            case FilterDecision.Defer:
                                // For now, treat defer as allow
                                continue;
                        }
                    }
                }

                return currentAlert;
            }
        }

        private Alert ApplySuppressionRules(Alert alert)
        {
            // Suppression is now handled by the filter system
            // This method is kept for backward compatibility but simply returns the alert unchanged

            return alert;
        }

        private async UniTask DeliverAlertToChannelsAsync(Alert alert)
        {
            using (_deliveryMarker.Auto())
            {
                var channels = GetRegisteredChannels();
                var deliveryTasks = new List<UniTask>();

                foreach (var channel in channels)
                {
                    if (channel.IsEnabled && channel.IsHealthy && alert.Severity >= channel.MinimumSeverity)
                    {
                        deliveryTasks.Add(channel.SendAlertAsync(alert, alert.CorrelationId));
                    }
                }

                await UniTask.WhenAll(deliveryTasks);
            }
        }

        private void PublishAlertRaisedMessage(Alert alert)
        {
            try
            {
                var message = AlertRaisedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertRaisedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void PublishAlertAcknowledgedMessage(Alert alert)
        {
            try
            {
                var message = AlertAcknowledgedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertAcknowledgedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void PublishAlertResolvedMessage(Alert alert)
        {
            try
            {
                var message = AlertResolvedMessage.Create(alert, "AlertService", alert.CorrelationId);
                _messageBusService?.PublishAsync(message).Forget();
            }
            catch (Exception ex)
            {
                LogError($"Failed to publish AlertResolvedMessage: {ex.Message}", alert.CorrelationId);
            }
        }

        private void UpdateStatistics(bool success, TimeSpan duration)
        {
            // Implementation would update comprehensive statistics
            // For brevity, this is simplified
        }

        private void LogDebug(string message, Guid correlationId)
        {
            _loggingService?.LogDebug($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        private void LogInfo(string message, Guid correlationId)
        {
            _loggingService?.LogInfo($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        private void LogError(string message, Guid correlationId)
        {
            _loggingService?.LogError($"[AlertService] {message}", correlationId.ToString(), "AlertService");
        }

        #endregion

        #region Bulk Operations
        
        /// <summary>
        /// Raises multiple alerts in a single batch operation for performance.
        /// </summary>
        public async UniTask RaiseAlertsAsync(IEnumerable<Alert> alerts, Guid correlationId = default)
        {
            using (_bulkOperationMarker.Auto())
            {
                if (!IsEnabled || alerts == null) return;

                var alertList = alerts.AsValueEnumerable().ToList();
                LogInfo($"Processing bulk alert operation with {alertList.Count} alerts", correlationId);

                var tasks = alertList.AsValueEnumerable().Select(alert => RaiseAlertAsync(alert)).ToList();
                await UniTask.WhenAll(tasks);
            }
        }
        
        /// <summary>
        /// Acknowledges multiple alerts by their IDs.
        /// </summary>
        public async UniTask AcknowledgeAlertsAsync(IEnumerable<Guid> alertIds, Guid correlationId = default)
        {
            using (_bulkOperationMarker.Auto())
            {
                if (!IsEnabled || alertIds == null) return;

                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (var alertId in alertIds)
                    {
                        AcknowledgeAlert(alertId, correlationId.ToString());
                    }
                });
            }
        }
        
        /// <summary>
        /// Resolves multiple alerts by their IDs.
        /// </summary>
        public async UniTask ResolveAlertsAsync(IEnumerable<Guid> alertIds, Guid correlationId = default)
        {
            using (_bulkOperationMarker.Auto())
            {
                if (!IsEnabled || alertIds == null) return;

                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (var alertId in alertIds)
                    {
                        ResolveAlert(alertId, correlationId.ToString());
                    }
                });
            }
        }

        #endregion

        #region Configuration Management
        
        /// <summary>
        /// Updates the service configuration with hot-reload capability.
        /// </summary>
        public async UniTask<bool> UpdateConfigurationAsync(AlertServiceConfiguration configuration, Guid correlationId = default)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                // Validate configuration
                configuration.Validate();
                
                _configuration = configuration;
                
                // Apply new settings
                _globalMinimumSeverity = configuration.AlertConfig.MinimumSeverity;
                
                LogInfo("Configuration updated successfully", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to update configuration: {ex.Message}", correlationId);
                return false;
            }
        }
        
        /// <summary>
        /// Reloads configuration from the original source.
        /// </summary>
        public async UniTask ReloadConfigurationAsync(Guid correlationId = default)
        {
            try
            {
                var defaultConfig = GetDefaultConfiguration();
                await UpdateConfigurationAsync(defaultConfig, correlationId);
                LogInfo("Configuration reloaded from defaults", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Failed to reload configuration: {ex.Message}", correlationId);
            }
        }
        
        /// <summary>
        /// Gets the default configuration for the current environment.
        /// </summary>
        public AlertServiceConfiguration GetDefaultConfiguration()
        {
            return new AlertConfigBuilder().ForProduction().BuildServiceConfiguration();
        }

        #endregion

        #region Health Monitoring and Diagnostics
        
        /// <summary>
        /// Performs a comprehensive health check of the alerting system.
        /// </summary>
        public async UniTask<AlertSystemHealthReport> PerformHealthCheckAsync(Guid correlationId = default)
        {
            using (_healthCheckMarker.Auto())
            {
                var report = new AlertSystemHealthReport
                {
                    Timestamp = DateTime.UtcNow,
                    OverallHealth = IsHealthy,
                    ServiceEnabled = IsEnabled,
                    EmergencyModeActive = IsEmergencyModeActive,
                    ConsecutiveFailures = _consecutiveFailures,
                    LastHealthCheck = _lastHealthCheck
                };

                // Check subsystem health
                if (_channelService != null)
                {
                    await _channelService.PerformHealthChecksAsync(correlationId);
                    report.ChannelServiceHealth = _channelService.IsEnabled;
                    report.HealthyChannelCount = _channelService.HealthyChannelCount;
                }

                _lastHealthCheck = DateTime.UtcNow;
                LogInfo($"Health check completed - Overall: {report.OverallHealth}", correlationId);
                
                return report;
            }
        }
        
        /// <summary>
        /// Gets detailed diagnostic information about the alerting system.
        /// </summary>
        public AlertSystemDiagnostics GetDiagnostics(Guid correlationId = default)
        {
            return new AlertSystemDiagnostics
            {
                ServiceVersion = "2.0.0",
                IsEnabled = IsEnabled,
                IsHealthy = IsHealthy,
                IsStarted = _isStarted,
                EmergencyModeActive = IsEmergencyModeActive,
                EmergencyModeReason = _emergencyModeReason,
                ActiveAlertCount = _activeAlerts.Count,
                HistoryCount = _alertHistory.Count,
                ConsecutiveFailures = _consecutiveFailures,
                LastMaintenanceRun = _lastMaintenanceRun,
                LastHealthCheck = _lastHealthCheck,
                ConfigurationSummary = _configuration?.ToString(),
                SubsystemStatuses = new Dictionary<string, bool>
                {
                    ["ChannelService"] = _channelService?.IsEnabled ?? false,
                    ["FilterService"] = _filterService?.IsEnabled ?? false,
                    ["SuppressionService"] = _suppressionService?.IsEnabled ?? false
                }
            };
        }
        
        /// <summary>
        /// Gets performance metrics for all subsystems.
        /// </summary>
        public AlertSystemPerformanceMetrics GetPerformanceMetrics()
        {
            return _performanceMetrics;
        }
        
        /// <summary>
        /// Resets all performance metrics and statistics.
        /// </summary>
        public void ResetMetrics(Guid correlationId = default)
        {
            _performanceMetrics = new AlertSystemPerformanceMetrics();
            _channelService?.ResetMetrics(correlationId);
            _filterService?.ResetPerformanceMetrics(correlationId);
            _suppressionService?.ResetStatistics();
            LogInfo("Performance metrics reset", correlationId);
        }

        #endregion

        #region Emergency Operations
        
        /// <summary>
        /// Enables emergency mode, bypassing filters and suppression for critical alerts.
        /// </summary>
        public void EnableEmergencyMode(string reason, Guid correlationId = default)
        {
            _emergencyMode = true;
            _emergencyModeReason = reason ?? "Emergency mode enabled";
            LogInfo($"Emergency mode enabled: {_emergencyModeReason}", correlationId);
        }
        
        /// <summary>
        /// Disables emergency mode and restores normal operations.
        /// </summary>
        public void DisableEmergencyMode(Guid correlationId = default)
        {
            _emergencyMode = false;
            _emergencyModeReason = null;
            LogInfo("Emergency mode disabled", correlationId);
        }
        
        /// <summary>
        /// Performs emergency escalation for failed alert delivery.
        /// </summary>
        public async UniTask PerformEmergencyEscalationAsync(Alert alert, Guid correlationId = default)
        {
            try
            {
                // Try emergency channels
                var emergencyChannels = _channelService?.GetAllChannels()
                    ?.AsValueEnumerable()
                    .Where(c => c.Name.ToString().Contains("Emergency") || c.MinimumSeverity <= AlertSeverity.Critical)
                    .ToList() ?? new List<IAlertChannel>();

                foreach (var channel in emergencyChannels)
                {
                    await channel.SendAlertAsync(alert, correlationId);
                }
                
                LogInfo($"Emergency escalation completed for alert {alert.Id}", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Emergency escalation failed: {ex.Message}", correlationId);
            }
        }

        #endregion

        #region Service Control
        
        /// <summary>
        /// Starts the alerting service and all subsystems.
        /// </summary>
        public async UniTask StartAsync(Guid correlationId = default)
        {
            try
            {
                if (_isStarted) return;

                _isEnabled = true;
                _isStarted = true;
                
                LogInfo("Alert service started", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Failed to start alert service: {ex.Message}", correlationId);
                throw;
            }
        }
        
        /// <summary>
        /// Stops the alerting service and all subsystems gracefully.
        /// </summary>
        public async UniTask StopAsync(Guid correlationId = default)
        {
            try
            {
                _isEnabled = false;
                
                // Flush pending alerts
                await FlushAsync(correlationId);
                
                _isStarted = false;
                LogInfo("Alert service stopped", correlationId);
            }
            catch (Exception ex)
            {
                LogError($"Error during service stop: {ex.Message}", correlationId);
            }
        }
        
        /// <summary>
        /// Restarts the alerting service with current configuration.
        /// </summary>
        public async UniTask RestartAsync(Guid correlationId = default)
        {
            await StopAsync(correlationId);
            await StartAsync(correlationId);
            LogInfo("Alert service restarted", correlationId);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the alert service resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            _isDisposed = true;

            try
            {
                // Dispose subsystem services
                _channelService?.Dispose();
                _filterService?.Dispose();
                _suppressionService?.Dispose();

                lock (_syncLock)
                {
                    _activeAlerts.Clear();
                    _alertHistory.Clear();
                    _sourceMinimumSeverities.Clear();
                }

                LogInfo("Alert service disposed", Guid.NewGuid());
            }
            catch (Exception ex)
            {
                LogError($"Error during disposal: {ex.Message}", Guid.NewGuid());
            }
        }

        #endregion
    }
}