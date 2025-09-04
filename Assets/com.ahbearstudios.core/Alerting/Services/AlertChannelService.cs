using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Profiling;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Service responsible for managing the lifecycle of alert channels.
    /// Handles channel registration, health monitoring, configuration, and delivery orchestration.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// Uses IMessageBusService for events and UniTask for async operations.
    /// </summary>
    public sealed class AlertChannelService : IAlertChannelService
    {
        private readonly object _syncLock = new object();
        private readonly Dictionary<FixedString64Bytes, IAlertChannel> _channels = new();
        private readonly Dictionary<FixedString64Bytes, ChannelHealthInfo> _channelHealth = new();
        private readonly Dictionary<FixedString64Bytes, ChannelMetrics> _channelMetrics = new();
        private readonly AlertChannelServiceConfig _config;
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBus;
        private readonly DateTime _serviceStartTime;
        
        private volatile bool _isDisposed;
        private CancellationTokenSource _cancellationTokenSource;
        
        // Performance monitoring
        private readonly ProfilerMarker _deliverAlertMarker = new("AlertChannelService.DeliverAlert");
        private readonly ProfilerMarker _healthCheckMarker = new("AlertChannelService.HealthCheck");
        private readonly ProfilerMarker _metricsMarker = new("AlertChannelService.Metrics");

        /// <summary>
        /// Gets whether the channel service is enabled.
        /// </summary>
        public bool IsEnabled => _config.IsEnabled && !_isDisposed;

        /// <summary>
        /// Gets the count of registered channels.
        /// </summary>
        public int ChannelCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _channels.Count;
                }
            }
        }

        /// <summary>
        /// Gets the count of healthy channels.
        /// </summary>
        public int HealthyChannelCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _channelHealth.Values.AsValueEnumerable().Where(h => h.IsHealthy).Count();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the AlertChannelService class.
        /// </summary>
        /// <param name="config">Service configuration</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="messageBus">Message bus for event distribution</param>
        public AlertChannelService(
            AlertChannelServiceConfig config,
            ILoggingService loggingService,
            IMessageBusService messageBus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _loggingService = loggingService;
            _messageBus = messageBus;
            _serviceStartTime = DateTime.UtcNow;
            _cancellationTokenSource = new CancellationTokenSource();
            
            LogInfo("Alert channel service initialized");
        }
        
        /// <summary>
        /// Initializes the service and starts background tasks.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            // Start background tasks for health checks and metrics
            if (_config.EnableAutoHealthChecks)
            {
               RunHealthCheckLoopAsync(_cancellationTokenSource.Token).Forget();
            }
            
            if (_config.EnableMetricsCollection)
            {
                RunMetricsCollectionLoopAsync(_cancellationTokenSource.Token).Forget();
            }
            
            await UniTask.CompletedTask;
        }

        #region Channel Registration and Management

        /// <summary>
        /// Registers an alert channel with the service.
        /// </summary>
        /// <param name="channel">Channel to register</param>
        /// <param name="config">Optional initial configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with registration result</returns>
        public async UniTask<bool> RegisterChannelAsync(IAlertChannel channel, ChannelConfig config = null, Guid correlationId = default)
        {
            if (channel == null)
                return false;

            var channelName = channel.Name;
            
            try
            {
                lock (_syncLock)
                {
                    if (_channels.ContainsKey(channelName))
                    {
                        LogWarning($"Channel already registered: {channelName}", correlationId);
                        return false;
                    }

                    _channels[channelName] = channel;
                    _channelHealth[channelName] = ChannelHealthInfo.Unhealthy(channelName, "Channel registered, awaiting health check", 0);
                    _channelMetrics[channelName] = ChannelMetrics.CreateEmpty(channelName);
                }

                // Initialize channel if configuration provided
                if (config != null)
                {
                    var initResult = await channel.InitializeAsync(config, correlationId);
                    if (!initResult)
                    {
                        await UnregisterChannelAsync(channelName, correlationId);
                        return false;
                    }
                }

                // Perform initial health check
                await PerformHealthCheckForChannel(channel, correlationId);

                // Publish message
                var message = AlertChannelRegisteredMessage.Create(channelName, config, new FixedString64Bytes("AlertChannelService"), correlationId);
                await _messageBus.PublishMessageAsync(message);

                LogInfo($"Channel registered successfully: {channelName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to register channel {channelName}: {ex.Message}", correlationId);
                
                // Cleanup on failure
                await UnregisterChannelAsync(channelName, correlationId);
                return false;
            }
        }

        /// <summary>
        /// Unregisters an alert channel from the service.
        /// </summary>
        /// <param name="channelName">Name of channel to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with unregistration result</returns>
        public async UniTask<bool> UnregisterChannelAsync(FixedString64Bytes channelName, Guid correlationId = default)
        {
            if (channelName.IsEmpty)
                return false;

            IAlertChannel channel = null;
            
            try
            {
                lock (_syncLock)
                {
                    if (!_channels.TryGetValue(channelName, out channel))
                        return false;

                    _channels.Remove(channelName);
                    _channelHealth.Remove(channelName);
                    _channelMetrics.Remove(channelName);
                }

                // Flush any remaining alerts
                await channel.FlushAsync(correlationId);

                // Dispose channel
                channel.Dispose();

                // Publish message
                var message = AlertChannelUnregisteredMessage.Create(channelName, new FixedString64Bytes("AlertChannelService"), correlationId, new FixedString512Bytes("Manual unregistration"));
                await _messageBus.PublishMessageAsync(message);

                LogInfo($"Channel unregistered: {channelName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to unregister channel {channelName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Gets a registered channel by name.
        /// </summary>
        /// <param name="channelName">Name of channel to retrieve</param>
        /// <returns>Channel instance or null if not found</returns>
        public IAlertChannel GetChannel(FixedString64Bytes channelName)
        {
            if (channelName.IsEmpty)
                return null;

            lock (_syncLock)
            {
                return _channels.TryGetValue(channelName, out var channel) ? channel : null;
            }
        }

        /// <summary>
        /// Gets all registered channels.
        /// </summary>
        /// <returns>Collection of registered channels</returns>
        public IReadOnlyCollection<IAlertChannel> GetAllChannels()
        {
            lock (_syncLock)
            {
                return _channels.Values.AsValueEnumerable().ToList();
            }
        }

        /// <summary>
        /// Gets healthy channels only.
        /// </summary>
        /// <returns>Collection of healthy channels</returns>
        public IReadOnlyCollection<IAlertChannel> GetHealthyChannels()
        {
            lock (_syncLock)
            {
                var healthyChannels = new List<IAlertChannel>();
                foreach (var kvp in _channels)
                {
                    if (_channelHealth.TryGetValue(kvp.Key, out var health) && health.IsHealthy)
                    {
                        healthyChannels.Add(kvp.Value);
                    }
                }
                return healthyChannels;
            }
        }

        #endregion

        #region Alert Delivery

        /// <summary>
        /// Delivers an alert to all appropriate channels.
        /// </summary>
        /// <param name="alert">Alert to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with delivery results</returns>
        public async UniTask<AlertDeliveryResults> DeliverAlertAsync(Alert alert, Guid correlationId = default)
        {
            using (_deliverAlertMarker.Auto())
            {
                if (!IsEnabled || alert == null)
                    return AlertDeliveryResults.Empty;

                var deliveryTasks = new List<UniTask<ChannelDeliveryResult>>();

                // Get channels that should receive this alert
                var targetChannels = GetTargetChannelsForAlert(alert);

                // Create delivery tasks for each channel
                foreach (var channel in targetChannels)
                {
                    deliveryTasks.Add(DeliverToChannelAsync(channel, alert, correlationId));
                }

                // Wait for all deliveries
                var deliveryResults = await UniTask.WhenAll(deliveryTasks);

                // Create results from channel results
                var results = AlertDeliveryResults.FromChannelResults(deliveryResults);

                LogDebug($"Alert delivered to {results.TotalChannels} channels: {results.SuccessfulDeliveries} succeeded, {results.FailedDeliveries} failed", correlationId);
                return results;
            }
        }

        /// <summary>
        /// Delivers an alert to a specific channel.
        /// </summary>
        /// <param name="channelName">Name of target channel</param>
        /// <param name="alert">Alert to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with delivery result</returns>
        public async UniTask<bool> DeliverAlertToChannelAsync(FixedString64Bytes channelName, Alert alert, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null)
                return false;

            var task = DeliverToChannelAsync(channel, alert, correlationId);
            ChannelDeliveryResult result = await task;
             return result.IsSuccess; 
        }

        #endregion

        #region Channel Health Management

        /// <summary>
        /// Performs health checks on all registered channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the health check operation</returns>
        public async UniTask PerformHealthChecksAsync(Guid correlationId = default)
        {
            using (_healthCheckMarker.Auto())
            {
                var channels = GetAllChannels();
                var healthTasks = new List<UniTask>();
                foreach (var channel in channels)
                {
                    healthTasks.Add(PerformHealthCheckForChannel(channel, correlationId));
                }
                
                await UniTask.WhenAll(healthTasks);
                
                var healthyCount = HealthyChannelCount;
                var totalCount = ChannelCount;
                
                LogDebug($"Health checks completed: {healthyCount}/{totalCount} channels healthy", correlationId);
            }
        }

        /// <summary>
        /// Gets health information for all channels.
        /// </summary>
        /// <returns>Collection of channel health information</returns>
        public IReadOnlyCollection<ChannelHealthInfo> GetChannelHealthInfo()
        {
            lock (_syncLock)
            {
                return _channelHealth.Values.AsValueEnumerable().ToList();
            }
        }

        /// <summary>
        /// Gets health information for a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel</param>
        /// <returns>Channel health information or null if not found</returns>
        public ChannelHealthInfo GetChannelHealth(FixedString64Bytes channelName)
        {
            if (channelName.IsEmpty)
                return null;

            lock (_syncLock)
            {
                return _channelHealth.TryGetValue(channelName, out var health) ? health : null;
            }
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Updates configuration for a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to configure</param>
        /// <param name="config">New configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with configuration result</returns>
        public async UniTask<bool> ConfigureChannelAsync(FixedString64Bytes channelName, ChannelConfig config, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null || config == null)
                return false;

            try
            {
                var result = await channel.InitializeAsync(config, correlationId);
                
                if (result)
                {
                    // Publish configuration changed message
                    var message = AlertChannelConfigurationChangedMessage.Create(channelName, null, config, new FixedString64Bytes("AlertChannelService"), correlationId);
                    await _messageBus.PublishMessageAsync(message);
                    
                    LogInfo($"Channel configuration updated: {channelName}", correlationId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to configure channel {channelName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Enables a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to enable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if channel was enabled</returns>
        public bool EnableChannel(FixedString64Bytes channelName, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null)
                return false;

            try
            {
                channel.Enable(correlationId);
                LogInfo($"Channel enabled: {channelName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to enable channel {channelName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Disables a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to disable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if channel was disabled</returns>
        public bool DisableChannel(FixedString64Bytes channelName, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null)
                return false;

            try
            {
                channel.Disable(correlationId);
                LogInfo($"Channel disabled: {channelName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to disable channel {channelName}: {ex.Message}", correlationId);
                return false;
            }
        }

        #endregion

        #region Metrics and Statistics

        /// <summary>
        /// Gets comprehensive metrics for all channels.
        /// </summary>
        /// <returns>Channel service metrics</returns>
        public ChannelServiceMetrics GetMetrics()
        {
            using (_metricsMarker.Auto())
            {
                lock (_syncLock)
                {
                    return ChannelServiceMetrics.FromChannelState(
                        _channelMetrics.Values.AsValueEnumerable().ToList(),
                        _channelHealth.Values.AsValueEnumerable().Where(h => h.IsHealthy).Count(),
                        _channels.Values.AsValueEnumerable().Where(c => c.IsEnabled).Count(),
                        _serviceStartTime
                    );
                }
            }
        }

        /// <summary>
        /// Resets metrics for all channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public void ResetMetrics(Guid correlationId = default)
        {
            lock (_syncLock)
            {
                foreach (var channel in _channels.Values)
                {
                    channel.ResetStatistics(correlationId);
                }
                
                var resetChannelMetrics = new Dictionary<FixedString64Bytes, ChannelMetrics>();
                foreach (var kvp in _channelMetrics)
                {
                    resetChannelMetrics[kvp.Key] = kvp.Value.Reset();
                }
                _channelMetrics.Clear();
                foreach (var kvp in resetChannelMetrics)
                {
                    _channelMetrics[kvp.Key] = kvp.Value;
                }
            }
            
            LogInfo("Channel metrics reset", correlationId);
        }

        #endregion

        #region Private Methods

        private async UniTask RunHealthCheckLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    await UniTask.Delay(_config.HealthCheckInterval, DelayType.DeltaTime, cancellationToken: cancellationToken);
                    await PerformHealthChecksAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogError($"Error in health check loop: {ex.Message}");
                }
            }
        }

        private async UniTask RunMetricsCollectionLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    await UniTask.Delay(_config.MetricsCollectionInterval, DelayType.DeltaTime, cancellationToken: cancellationToken);
                    // Collect metrics - implementation could perform periodic metrics collection
                    GetMetrics(); // This triggers metrics collection
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogError($"Error in metrics collection loop: {ex.Message}");
                }
            }
        }

        private List<IAlertChannel> GetTargetChannelsForAlert(Alert alert)
        {
            lock (_syncLock)
            {
                var targetChannels = new List<IAlertChannel>();
                foreach (var channel in _channels.Values)
                {
                    if (channel.IsEnabled && 
                        channel.IsHealthy && 
                        alert.Severity >= channel.MinimumSeverity)
                    {
                        targetChannels.Add(channel);
                    }
                }
                return targetChannels;
            }
        }

        private async UniTask<ChannelDeliveryResult> DeliverToChannelAsync(IAlertChannel channel, Alert alert, Guid correlationId)
        {
            var startTime = DateTime.UtcNow;
            var channelName = channel.Name;
            
            try
            {
                var success = await channel.SendAlertAsync(alert, correlationId);
                var duration = DateTime.UtcNow - startTime;
                
                // Update channel metrics
                UpdateChannelMetrics(channelName, success, duration);
                
                if (success)
                {
                    return ChannelDeliveryResult.Success(channelName, duration);
                }
                else
                {
                    return ChannelDeliveryResult.Failure(channelName, "Alert delivery failed", duration);
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                
                // Update channel metrics with failure
                UpdateChannelMetrics(channelName, false, duration);
                
                // Update health status
                UpdateChannelHealthOnError(channelName, ex);
                
                return ChannelDeliveryResult.FromException(channelName, ex, duration);
            }
        }

        private async UniTask PerformHealthCheckForChannel(IAlertChannel channel, Guid correlationId)
        {
            var channelName = channel.Name;
            
            try
            {
                var healthResult = await channel.TestHealthAsync(correlationId);
                var previousHealth = GetChannelHealth(channelName);
                var wasHealthy = previousHealth?.IsHealthy ?? false;
                
                lock (_syncLock)
                {
                    if (healthResult.IsHealthy)
                    {
                        _channelHealth[channelName] = ChannelHealthInfo.Healthy(channelName, healthResult.StatusMessage.ToString());
                    }
                    else
                    {
                        var consecutiveFailures = (previousHealth?.ConsecutiveFailures ?? 0) + 1;
                        _channelHealth[channelName] = ChannelHealthInfo.Unhealthy(channelName, healthResult.StatusMessage.ToString(), consecutiveFailures);
                    }
                }
                
                // Publish health changed message if status changed
                if (wasHealthy != healthResult.IsHealthy)
                {
                    var message = AlertChannelHealthChangedMessage.Create(
                        channelName, 
                        wasHealthy, 
                        healthResult.IsHealthy, 
                        healthResult, 
                        new FixedString64Bytes("AlertChannelService"), 
                        correlationId);
                    await _messageBus.PublishMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                UpdateChannelHealthOnError(channelName, ex);
                LogError($"Health check failed for channel {channelName}: {ex.Message}", correlationId);
            }
        }

        private void UpdateChannelHealthOnError(FixedString64Bytes channelName, Exception ex)
        {
            lock (_syncLock)
            {
                var previousHealth = _channelHealth.TryGetValue(channelName, out var health) ? health : null;
                var consecutiveFailures = (previousHealth?.ConsecutiveFailures ?? 0) + 1;
                
                _channelHealth[channelName] = ChannelHealthInfo.Unhealthy(channelName, ex.Message, consecutiveFailures);
            }
        }

        private void UpdateChannelMetrics(FixedString64Bytes channelName, bool success, TimeSpan duration)
        {
            lock (_syncLock)
            {
                if (_channelMetrics.TryGetValue(channelName, out var metrics))
                {
                    _channelMetrics[channelName] = metrics.WithDelivery(success, duration);
                }
            }
        }

        private void LogDebug(string message, Guid correlationId = default)
        {
            _loggingService?.LogDebug($"[AlertChannelService] {message}", correlationId.ToString(), "AlertChannelService");
        }

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertChannelService] {message}", correlationId.ToString(), "AlertChannelService");
        }

        private void LogWarning(string message, Guid correlationId = default)
        {
            _loggingService?.LogWarning($"[AlertChannelService] {message}", correlationId.ToString(), "AlertChannelService");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertChannelService] {message}", correlationId.ToString(), "AlertChannelService");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the channel service resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Cancel background tasks
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            lock (_syncLock)
            {
                foreach (var channel in _channels.Values)
                {
                    try
                    {
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error disposing channel {channel.Name}: {ex.Message}");
                    }
                }

                _channels.Clear();
                _channelHealth.Clear();
                _channelMetrics.Clear();
            }

            LogInfo("Alert channel service disposed");
        }

        #endregion
    }
}