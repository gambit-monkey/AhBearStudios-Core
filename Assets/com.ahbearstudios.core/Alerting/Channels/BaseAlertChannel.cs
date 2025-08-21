using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Alerting.Configs;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Abstract base class for alert channel implementations.
    /// Provides common functionality for channel health monitoring, statistics tracking, and configuration management.
    /// Designed for Unity game development with zero-allocation patterns and async UniTask operations.
    /// </summary>
    public abstract class BaseAlertChannel : IAlertChannel
    {
        private readonly object _syncLock = new object();
        private readonly IMessageBusService _messageBusService;
        private volatile bool _isEnabled = true;
        private volatile bool _isHealthy = true;
        private volatile bool _isDisposed;
        private ChannelConfig _configuration;
        private ChannelStatistics _statistics = ChannelStatistics.Empty;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        
        // Rate limiting
        private DateTime _rateLimitWindowStart = DateTime.UtcNow;
        private int _alertsInCurrentWindow = 0;

        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        public abstract FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets whether this channel is currently enabled and operational.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed;

        /// <summary>
        /// Gets whether this channel is currently healthy and can deliver alerts.
        /// </summary>
        public bool IsHealthy => _isHealthy && !_isDisposed;

        /// <summary>
        /// Gets or sets the minimum severity level this channel will process.
        /// </summary>
        public AlertSeverity MinimumSeverity { get; set; } = AlertSeverity.Info;

        /// <summary>
        /// Gets or sets the maximum number of alerts this channel can process per second.
        /// </summary>
        public int MaxAlertsPerSecond { get; set; } = 100;

        /// <summary>
        /// Gets the current configuration for this channel.
        /// </summary>
        public ChannelConfig Configuration => _configuration;

        /// <summary>
        /// Gets performance statistics for this channel.
        /// </summary>
        public ChannelStatistics Statistics => _statistics;


        /// <summary>
        /// Initializes a new instance of the BaseAlertChannel class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing channel events</param>
        protected BaseAlertChannel(IMessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _configuration = CreateDefaultConfiguration();
        }

        /// <summary>
        /// Sends an alert through this channel synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if the alert was sent successfully</returns>
        public virtual bool SendAlert(Alert alert, Guid correlationId = default)
        {
            if (!CanSendAlert(alert))
                return false;

            if (!CheckRateLimit())
            {
                UpdateStatistics(alert, false, TimeSpan.Zero, "Rate limit exceeded");
                return false;
            }

            var startTime = DateTime.UtcNow;
            try
            {
                var result = SendAlertCore(alert, correlationId);
                var duration = DateTime.UtcNow - startTime;
                UpdateStatistics(alert, result, duration);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HandleDeliveryFailure(alert, ex, correlationId);
                UpdateStatistics(alert, false, duration, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends an alert through this channel asynchronously using UniTask.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with send result</returns>
        public virtual async UniTask<bool> SendAlertAsync(Alert alert, Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            if (!CanSendAlert(alert))
                return false;

            if (!CheckRateLimit())
            {
                UpdateStatistics(alert, false, TimeSpan.Zero, "Rate limit exceeded");
                return false;
            }

            var startTime = DateTime.UtcNow;
            try
            {
                var result = await SendAlertAsyncCore(alert, correlationId, cancellationToken);
                var duration = DateTime.UtcNow - startTime;
                UpdateStatistics(alert, result, duration);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HandleDeliveryFailure(alert, ex, correlationId);
                UpdateStatistics(alert, false, duration, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends multiple alerts as a batch for efficiency.
        /// </summary>
        /// <param name="alerts">Collection of alerts to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with number of successfully sent alerts</returns>
        public virtual async UniTask<int> SendAlertBatchAsync(IEnumerable<Alert> alerts, Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || !IsHealthy)
                return 0;

            var alertList = alerts.AsValueEnumerable().ToList();
            if (alertList.Count == 0)
                return 0;

            // Filter alerts by minimum severity
            var eligibleAlerts = alertList.AsValueEnumerable()
                .Where(a => a.Severity >= MinimumSeverity)
                .ToList();

            if (eligibleAlerts.Count == 0)
                return 0;

            return await SendAlertBatchAsyncCore(eligibleAlerts, correlationId, cancellationToken);
        }

        /// <summary>
        /// Tests the channel connectivity and health.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health check result</returns>
        public virtual async UniTask<ChannelHealthResult> TestHealthAsync(Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Perform channel-specific health check
                var result = await TestHealthAsyncCore(correlationId, cancellationToken);
                
                var duration = DateTime.UtcNow - startTime;
                _lastHealthCheck = DateTime.UtcNow;
                
                // Update health status if changed
                if (result.IsHealthy != _isHealthy)
                {
                    var previousHealth = _isHealthy;
                    _isHealthy = result.IsHealthy;
                    
                    var healthChangedMessage = AlertChannelHealthChangedMessage.Create(
                        Name,
                        previousHealth,
                        _isHealthy,
                        result,
                        Name,
                        correlationId);
                    
                    _messageBusService.PublishMessage(healthChangedMessage);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _lastHealthCheck = DateTime.UtcNow;
                
                if (_isHealthy)
                {
                    _isHealthy = false;
                    var result = ChannelHealthResult.Unhealthy($"Health check failed: {ex.Message}", ex, duration);
                    
                    var healthChangedMessage = AlertChannelHealthChangedMessage.Create(
                        Name,
                        true,
                        false,
                        result,
                        Name,
                        correlationId);
                    
                    _messageBusService.PublishMessage(healthChangedMessage);
                    
                    return result;
                }
                
                return ChannelHealthResult.Unhealthy($"Health check failed: {ex.Message}", ex, duration);
            }
        }

        /// <summary>
        /// Initializes the channel with the provided configuration.
        /// </summary>
        /// <param name="config">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with initialization result</returns>
        public virtual async UniTask<bool> InitializeAsync(ChannelConfig config, Guid correlationId = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name.ToString());

            try
            {
                // Validate configuration
                var validationResult = ValidateConfiguration(config);
                if (!validationResult.IsValid)
                {
                    return false;
                }

                // Store previous configuration
                var previousConfig = _configuration;
                _configuration = config;

                // Apply configuration settings
                if (config.Settings != null)
                {
                    if (config.Settings.TryGetValue("MinimumSeverity", out var minSeverityStr) && 
                        Enum.TryParse<AlertSeverity>(minSeverityStr, out var minSeverity))
                        MinimumSeverity = minSeverity;
                    
                    if (config.Settings.TryGetValue("MaxAlertsPerSecond", out var maxAlertsStr) && 
                        int.TryParse(maxAlertsStr, out var maxAlerts))
                        MaxAlertsPerSecond = maxAlerts;
                }

                // Perform channel-specific initialization
                var result = await InitializeAsyncCore(config, correlationId);
                
                if (result)
                {
                    _isEnabled = true;
                    
                    var configChangedMessage = AlertChannelConfigurationChangedMessage.Create(
                        Name,
                        previousConfig,
                        config,
                        Name,
                        correlationId);
                    
                    _messageBusService.PublishMessage(configChangedMessage);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _isHealthy = false;
                throw new InvalidOperationException($"Failed to initialize channel {Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enables the channel for alert processing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public virtual void Enable(Guid correlationId = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name.ToString());

            _isEnabled = true;
            OnEnabled(correlationId);
        }

        /// <summary>
        /// Disables the channel temporarily without disposing resources.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public virtual void Disable(Guid correlationId = default)
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            OnDisabled(correlationId);
        }

        /// <summary>
        /// Flushes any buffered alerts to ensure delivery.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask representing the flush operation</returns>
        public virtual async UniTask FlushAsync(Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                return;

            await FlushAsyncCore(correlationId, cancellationToken);
        }

        /// <summary>
        /// Resets channel statistics and error counters.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public virtual void ResetStatistics(Guid correlationId = default)
        {
            lock (_syncLock)
            {
                _statistics = ChannelStatistics.Empty;
                _rateLimitWindowStart = DateTime.UtcNow;
                _alertsInCurrentWindow = 0;
            }
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _isEnabled = false;
                _isHealthy = false;
                DisposeCore();
            }

            _isDisposed = true;
        }

        #region Abstract Methods

        /// <summary>
        /// Core implementation for sending an alert synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if successful</returns>
        protected abstract bool SendAlertCore(Alert alert, Guid correlationId);

        /// <summary>
        /// Core implementation for sending an alert asynchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with result</returns>
        protected abstract UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Core implementation for batch sending alerts.
        /// </summary>
        /// <param name="alerts">Alerts to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with count of successful sends</returns>
        protected virtual async UniTask<int> SendAlertBatchAsyncCore(List<Alert> alerts, Guid correlationId, CancellationToken cancellationToken)
        {
            // Default implementation sends alerts one by one
            var successCount = 0;
            foreach (var alert in alerts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                if (await SendAlertAsync(alert, correlationId, cancellationToken))
                    successCount++;
            }
            return successCount;
        }

        /// <summary>
        /// Core implementation for health testing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health result</returns>
        protected abstract UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Core implementation for channel initialization.
        /// </summary>
        /// <param name="config">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with initialization result</returns>
        protected abstract UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId);

        /// <summary>
        /// Core implementation for flushing buffered alerts.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask representing the flush operation</returns>
        protected virtual UniTask FlushAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            // Default implementation does nothing (no buffering)
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Core implementation for resource disposal.
        /// </summary>
        protected virtual void DisposeCore()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Creates the default configuration for this channel.
        /// </summary>
        /// <returns>Default channel configuration</returns>
        protected abstract ChannelConfig CreateDefaultConfiguration();

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Called when the channel is enabled.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        protected virtual void OnEnabled(Guid correlationId)
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Called when the channel is disabled.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        protected virtual void OnDisabled(Guid correlationId)
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Validates channel configuration.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        protected virtual ValidationResult ValidateConfiguration(ChannelConfig config)
        {
            if (config == null)
                return ValidationResult.Failure("Configuration cannot be null", Name.ToString());

            if (config.Name.IsEmpty)
                return ValidationResult.Failure("Channel name is required", Name.ToString());

            return ValidationResult.Success(Name.ToString());
        }

        /// <summary>
        /// Checks if an alert can be sent through this channel.
        /// </summary>
        /// <param name="alert">Alert to check</param>
        /// <returns>True if alert can be sent</returns>
        protected virtual bool CanSendAlert(Alert alert)
        {
            if (alert == null)
                return false;

            if (!IsEnabled || !IsHealthy)
                return false;

            if (alert.Severity < MinimumSeverity)
                return false;

            // Check if health check is needed
            if (DateTime.UtcNow - _lastHealthCheck > _healthCheckInterval)
            {
                TestHealthAsync().Forget();
            }

            return true;
        }

        /// <summary>
        /// Checks rate limiting for alert sending.
        /// </summary>
        /// <returns>True if within rate limits</returns>
        protected bool CheckRateLimit()
        {
            lock (_syncLock)
            {
                var now = DateTime.UtcNow;
                var windowDuration = TimeSpan.FromSeconds(1);
                
                if (now - _rateLimitWindowStart > windowDuration)
                {
                    _rateLimitWindowStart = now;
                    _alertsInCurrentWindow = 0;
                }
                
                if (_alertsInCurrentWindow >= MaxAlertsPerSecond)
                    return false;
                
                _alertsInCurrentWindow++;
                return true;
            }
        }

        /// <summary>
        /// Updates channel statistics after an alert send attempt.
        /// </summary>
        /// <param name="alert">The alert that was sent</param>
        /// <param name="success">Whether the send was successful</param>
        /// <param name="duration">Duration of the send operation</param>
        /// <param name="errorMessage">Error message if failed</param>
        protected void UpdateStatistics(Alert alert, bool success, TimeSpan duration, string errorMessage = null)
        {
            lock (_syncLock)
            {
                var totalProcessed = _statistics.TotalAlertsProcessed + 1;
                var successCount = success ? _statistics.SuccessfulDeliveries + 1 : _statistics.SuccessfulDeliveries;
                var failureCount = !success ? _statistics.FailedDeliveries + 1 : _statistics.FailedDeliveries;
                
                var avgTime = (_statistics.AverageDeliveryTimeMs * _statistics.TotalAlertsProcessed + duration.TotalMilliseconds) / totalProcessed;
                var maxTime = Math.Max(_statistics.MaxDeliveryTimeMs, duration.TotalMilliseconds);
                
                _statistics = new ChannelStatistics
                {
                    TotalAlertsProcessed = totalProcessed,
                    SuccessfulDeliveries = successCount,
                    FailedDeliveries = failureCount,
                    AverageDeliveryTimeMs = avgTime,
                    MaxDeliveryTimeMs = maxTime,
                    CurrentDeliveryRate = _alertsInCurrentWindow,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Handles alert delivery failure.
        /// </summary>
        /// <param name="alert">The alert that failed to deliver</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        protected virtual void HandleDeliveryFailure(Alert alert, Exception exception, Guid correlationId)
        {
            var deliveryFailedMessage = AlertDeliveryFailedMessage.Create(
                Name,
                alert,
                exception,
                0,
                true,
                Name,
                correlationId);
            
            _messageBusService.PublishMessage(deliveryFailedMessage);
        }

        #endregion
    }
}