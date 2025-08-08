using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Service responsible for managing the lifecycle of alert channels.
    /// Handles channel registration, health monitoring, configuration, and delivery orchestration.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public sealed class AlertChannelService : IDisposable
    {
        private readonly object _syncLock = new object();
        private readonly Dictionary<string, IAlertChannel> _channels = new Dictionary<string, IAlertChannel>();
        private readonly Dictionary<string, ChannelHealth> _channelHealth = new Dictionary<string, ChannelHealth>();
        private readonly Dictionary<string, ChannelMetrics> _channelMetrics = new Dictionary<string, ChannelMetrics>();
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _metricsTimer;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _metricsInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether the channel manager is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed;

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
                    return _channelHealth.Values.AsValueEnumerable().Count(h => h.IsHealthy);
                }
            }
        }


        /// <summary>
        /// Initializes a new instance of the AlertChannelService class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for internal logging</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        public AlertChannelService(ILoggingService loggingService = null, ISerializationService serializationService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            
            // Set up health check timer
            _healthCheckTimer = new Timer(PerformHealthChecks, null, _healthCheckInterval, _healthCheckInterval);
            
            // Set up metrics collection timer
            _metricsTimer = new Timer(CollectMetrics, null, _metricsInterval, _metricsInterval);
            
            LogInfo("Alert channel service initialized");
        }

        #region Channel Registration and Management

        /// <summary>
        /// Registers an alert channel with the manager.
        /// </summary>
        /// <param name="channel">Channel to register</param>
        /// <param name="config">Optional initial configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with registration result</returns>
        public async UniTask<bool> RegisterChannelAsync(IAlertChannel channel, ChannelConfig config = null, Guid correlationId = default)
        {
            if (channel == null)
                return false;

            var channelName = channel.Name.ToString();
            
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
                    _channelHealth[channelName] = new ChannelHealth
                    {
                        ChannelName = channelName,
                        IsHealthy = false,
                        LastHealthCheck = DateTime.MinValue,
                        ConsecutiveFailures = 0
                    };
                    _channelMetrics[channelName] = new ChannelMetrics
                    {
                        ChannelName = channelName,
                        RegistrationTime = DateTime.UtcNow
                    };
                }

                // Subscribe to channel events
                SubscribeToChannelEvents(channel);

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

                // Raise event
                ChannelRegistered?.Invoke(this, new ChannelEventArgs(
                    channelName, ChannelEventType.Registered, correlationId, "AlertChannelService",
                    configuration: config));

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
        /// Unregisters an alert channel from the manager.
        /// </summary>
        /// <param name="channelName">Name of channel to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with unregistration result</returns>
        public async UniTask<bool> UnregisterChannelAsync(string channelName, Guid correlationId = default)
        {
            if (string.IsNullOrEmpty(channelName))
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

                // Unsubscribe from events
                UnsubscribeFromChannelEvents(channel);

                // Flush any remaining alerts
                await channel.FlushAsync(correlationId);

                // Dispose channel
                channel.Dispose();

                // Raise event
                ChannelUnregistered?.Invoke(this, new ChannelEventArgs(
                    channelName, ChannelEventType.Unregistered, correlationId, "AlertChannelService"));

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
        public IAlertChannel GetChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
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
                return _channels.Values.ToList();
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
                return _channels.AsValueEnumerable()
                    .Where(kvp => _channelHealth.TryGetValue(kvp.Key, out var health) && health.IsHealthy)
                    .Select(kvp => kvp.Value)
                    .ToList();
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
            if (!IsEnabled || alert == null)
                return AlertDeliveryResults.Empty;

            var startTime = DateTime.UtcNow;
            var results = new AlertDeliveryResults();
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

            // Aggregate results
            results.TotalChannels = targetChannels.Count;
            results.SuccessfulDeliveries = deliveryResults.AsValueEnumerable().Count(r => r.Success);
            results.FailedDeliveries = results.TotalChannels - results.SuccessfulDeliveries;
            results.TotalDeliveryTime = DateTime.UtcNow - startTime;
            results.ChannelResults = deliveryResults.ToList();

            // Update metrics
            UpdateDeliveryMetrics(alert, results);

            LogDebug($"Alert delivered to {results.TotalChannels} channels: {results.SuccessfulDeliveries} succeeded, {results.FailedDeliveries} failed", correlationId);
            return results;
        }

        /// <summary>
        /// Delivers an alert to a specific channel.
        /// </summary>
        /// <param name="channelName">Name of target channel</param>
        /// <param name="alert">Alert to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with delivery result</returns>
        public async UniTask<bool> DeliverAlertToChannelAsync(string channelName, Alert alert, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null)
                return false;

            var result = await DeliverToChannelAsync(channel, alert, correlationId);
            return result.Success;
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
            var channels = GetAllChannels();
            var healthTasks = channels.Select(c => PerformHealthCheckForChannel(c, correlationId)).ToList();
            
            await UniTask.WhenAll(healthTasks);
            
            var healthyCount = HealthyChannelCount;
            var totalCount = ChannelCount;
            
            LogDebug($"Health checks completed: {healthyCount}/{totalCount} channels healthy", correlationId);
        }

        /// <summary>
        /// Gets health information for all channels.
        /// </summary>
        /// <returns>Collection of channel health information</returns>
        public IReadOnlyCollection<ChannelHealth> GetChannelHealthInfo()
        {
            lock (_syncLock)
            {
                return _channelHealth.Values.ToList();
            }
        }

        /// <summary>
        /// Gets health information for a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel</param>
        /// <returns>Channel health information or null if not found</returns>
        public ChannelHealth GetChannelHealth(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
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
        public async UniTask<bool> ConfigureChannelAsync(string channelName, ChannelConfig config, Guid correlationId = default)
        {
            var channel = GetChannel(channelName);
            if (channel == null || config == null)
                return false;

            try
            {
                var result = await channel.InitializeAsync(config, correlationId);
                
                if (result)
                {
                    ChannelConfigurationChanged?.Invoke(this, new ChannelEventArgs(
                        channelName, ChannelEventType.ConfigurationChanged, correlationId, 
                        "AlertChannelService", configuration: config));
                    
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
        public bool EnableChannel(string channelName, Guid correlationId = default)
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
        public bool DisableChannel(string channelName, Guid correlationId = default)
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
        /// <returns>Channel manager metrics</returns>
        public ChannelManagerMetrics GetMetrics()
        {
            lock (_syncLock)
            {
                return new ChannelManagerMetrics
                {
                    TotalChannels = _channels.Count,
                    HealthyChannels = _channelHealth.Values.AsValueEnumerable().Count(h => h.IsHealthy),
                    EnabledChannels = _channels.Values.AsValueEnumerable().Count(c => c.IsEnabled),
                    ChannelMetrics = _channelMetrics.Values.ToList(),
                    LastUpdated = DateTime.UtcNow
                };
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
                
                foreach (var metrics in _channelMetrics.Values)
                {
                    metrics.ResetCounters();
                }
            }
            
            LogInfo("Channel metrics reset", correlationId);
        }

        #endregion

        #region Private Methods

        private List<IAlertChannel> GetTargetChannelsForAlert(Alert alert)
        {
            lock (_syncLock)
            {
                return _channels.Values.AsValueEnumerable()
                    .Where(c => c.IsEnabled && 
                               c.IsHealthy && 
                               alert.Severity >= c.MinimumSeverity)
                    .ToList();
            }
        }

        private async UniTask<ChannelDeliveryResult> DeliverToChannelAsync(IAlertChannel channel, Alert alert, Guid correlationId)
        {
            var startTime = DateTime.UtcNow;
            var channelName = channel.Name.ToString();
            
            try
            {
                var success = await channel.SendAlertAsync(alert, correlationId);
                var duration = DateTime.UtcNow - startTime;
                
                // Update channel metrics
                UpdateChannelMetrics(channelName, success, duration);
                
                return new ChannelDeliveryResult
                {
                    ChannelName = channelName,
                    Success = success,
                    DeliveryTime = duration,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                
                // Update channel metrics with failure
                UpdateChannelMetrics(channelName, false, duration);
                
                // Update health status
                await UpdateChannelHealthOnError(channelName, ex);
                
                return new ChannelDeliveryResult
                {
                    ChannelName = channelName,
                    Success = false,
                    DeliveryTime = duration,
                    Error = ex.Message
                };
            }
        }

        private async UniTask PerformHealthCheckForChannel(IAlertChannel channel, Guid correlationId)
        {
            var channelName = channel.Name.ToString();
            
            try
            {
                var healthResult = await channel.TestHealthAsync(correlationId);
                var wasHealthy = GetChannelHealth(channelName)?.IsHealthy ?? false;
                
                lock (_syncLock)
                {
                    if (_channelHealth.TryGetValue(channelName, out var health))
                    {
                        health.IsHealthy = healthResult.IsHealthy;
                        health.LastHealthCheck = DateTime.UtcNow;
                        health.LastHealthMessage = healthResult.StatusMessage.ToString();
                        health.ConsecutiveFailures = healthResult.IsHealthy ? 0 : health.ConsecutiveFailures + 1;
                    }
                }
                
                // Raise event if health status changed
                if (wasHealthy != healthResult.IsHealthy)
                {
                    ChannelHealthChanged?.Invoke(this, new ChannelEventArgs(
                        channelName, ChannelEventType.HealthChanged, correlationId, 
                        "AlertChannelService", eventData: healthResult));
                }
            }
            catch (Exception ex)
            {
                await UpdateChannelHealthOnError(channelName, ex);
                LogError($"Health check failed for channel {channelName}: {ex.Message}", correlationId);
            }
        }

        private async UniTask UpdateChannelHealthOnError(string channelName, Exception ex)
        {
            var wasHealthy = false;
            
            lock (_syncLock)
            {
                if (_channelHealth.TryGetValue(channelName, out var health))
                {
                    wasHealthy = health.IsHealthy;
                    health.IsHealthy = false;
                    health.LastHealthCheck = DateTime.UtcNow;
                    health.LastHealthMessage = ex.Message;
                    health.ConsecutiveFailures++;
                }
            }
            
            // Raise event if health status changed
            if (wasHealthy)
            {
                ChannelHealthChanged?.Invoke(this, new ChannelEventArgs(
                    channelName, ChannelEventType.HealthChanged, Guid.NewGuid(), 
                    "AlertChannelService", exception: ex));
            }
        }

        private void UpdateChannelMetrics(string channelName, bool success, TimeSpan duration)
        {
            lock (_syncLock)
            {
                if (_channelMetrics.TryGetValue(channelName, out var metrics))
                {
                    metrics.TotalDeliveryAttempts++;
                    if (success)
                        metrics.SuccessfulDeliveries++;
                    else
                        metrics.FailedDeliveries++;
                    
                    metrics.TotalDeliveryTime += duration;
                    metrics.LastDeliveryAttempt = DateTime.UtcNow;
                }
            }
        }

        private void UpdateDeliveryMetrics(Alert alert, AlertDeliveryResults results)
        {
            // Update aggregate delivery metrics here
            // Implementation could track system-wide delivery statistics
        }

        private void SubscribeToChannelEvents(IAlertChannel channel)
        {
            channel.HealthChanged += OnChannelHealthChanged;
            channel.AlertDeliveryFailed += OnChannelAlertDeliveryFailed;
            channel.ConfigurationChanged += OnChannelConfigurationChanged;
        }

        private void UnsubscribeFromChannelEvents(IAlertChannel channel)
        {
            channel.HealthChanged -= OnChannelHealthChanged;
            channel.AlertDeliveryFailed -= OnChannelAlertDeliveryFailed;
            channel.ConfigurationChanged -= OnChannelConfigurationChanged;
        }

        private void OnChannelHealthChanged(object sender, ChannelHealthChangedEventArgs e)
        {
            ChannelHealthChanged?.Invoke(this, new ChannelEventArgs(
                e.ChannelName, ChannelEventType.HealthChanged, e.CorrelationId, 
                "AlertChannelService", eventData: e.HealthResult));
        }

        private void OnChannelAlertDeliveryFailed(object sender, AlertDeliveryFailedEventArgs e)
        {
            LogWarning($"Alert delivery failed for channel {e.ChannelName}: {e.Exception?.Message}", e.CorrelationId);
        }

        private void OnChannelConfigurationChanged(object sender, ChannelConfigurationChangedEventArgs e)
        {
            ChannelConfigurationChanged?.Invoke(this, new ChannelEventArgs(
                e.ChannelName, ChannelEventType.ConfigurationChanged, e.CorrelationId, 
                "AlertChannelService", configuration: e.NewConfiguration));
        }

        private void PerformHealthChecks(object state)
        {
            if (_isDisposed || DateTime.UtcNow - _lastHealthCheck < _healthCheckInterval)
                return;

            _lastHealthCheck = DateTime.UtcNow;
            _ = PerformHealthChecksAsync().Forget();
        }

        private void CollectMetrics(object state)
        {
            if (_isDisposed)
                return;

            // Collect and update metrics
            // Implementation could perform periodic metrics collection
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
        /// Disposes of the channel manager resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            _isDisposed = true;

            _healthCheckTimer?.Dispose();
            _metricsTimer?.Dispose();

            lock (_syncLock)
            {
                foreach (var channel in _channels.Values)
                {
                    try
                    {
                        UnsubscribeFromChannelEvents(channel);
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

    #region Supporting Types

    /// <summary>
    /// Health information for a channel.
    /// </summary>
    public sealed class ChannelHealth
    {
        public string ChannelName { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public string LastHealthMessage { get; set; }
        public int ConsecutiveFailures { get; set; }
    }

    /// <summary>
    /// Metrics for a channel.
    /// </summary>
    public sealed class ChannelMetrics
    {
        public string ChannelName { get; set; }
        public DateTime RegistrationTime { get; set; }
        public long TotalDeliveryAttempts { get; set; }
        public long SuccessfulDeliveries { get; set; }
        public long FailedDeliveries { get; set; }
        public TimeSpan TotalDeliveryTime { get; set; }
        public DateTime? LastDeliveryAttempt { get; set; }

        public double SuccessRate => TotalDeliveryAttempts > 0 
            ? (double)SuccessfulDeliveries / TotalDeliveryAttempts * 100 
            : 0;

        public double AverageDeliveryTimeMs => TotalDeliveryAttempts > 0 
            ? TotalDeliveryTime.TotalMilliseconds / TotalDeliveryAttempts 
            : 0;

        public void ResetCounters()
        {
            TotalDeliveryAttempts = 0;
            SuccessfulDeliveries = 0;
            FailedDeliveries = 0;
            TotalDeliveryTime = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Results of alert delivery operation.
    /// </summary>
    public sealed class AlertDeliveryResults
    {
        public int TotalChannels { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public TimeSpan TotalDeliveryTime { get; set; }
        public List<ChannelDeliveryResult> ChannelResults { get; set; } = new List<ChannelDeliveryResult>();

        public double SuccessRate => TotalChannels > 0 
            ? (double)SuccessfulDeliveries / TotalChannels * 100 
            : 0;

        public static AlertDeliveryResults Empty => new AlertDeliveryResults();
    }

    /// <summary>
    /// Result of delivery to a specific channel.
    /// </summary>
    public sealed class ChannelDeliveryResult
    {
        public string ChannelName { get; set; }
        public bool Success { get; set; }
        public TimeSpan DeliveryTime { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Comprehensive metrics for the channel manager.
    /// </summary>
    public sealed class ChannelManagerMetrics
    {
        public int TotalChannels { get; set; }
        public int HealthyChannels { get; set; }
        public int EnabledChannels { get; set; }
        public List<ChannelMetrics> ChannelMetrics { get; set; } = new List<ChannelMetrics>();
        public DateTime LastUpdated { get; set; }

        public double HealthRate => TotalChannels > 0 
            ? (double)HealthyChannels / TotalChannels * 100 
            : 0;
    }

    #endregion
}