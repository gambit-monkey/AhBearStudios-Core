using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Alerting.Models;
using ZLinq;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service for managing log channels in the logging system.
    /// Handles channel registration, routing, filtering, and lifecycle management.
    /// Follows the AhBearStudios Core Architecture patterns for service decomposition.
    /// </summary>
    public sealed class LogChannelService : ILogChannelService, IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<string, ILogChannel> _channels;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly object _lock = new object();
        
        private volatile ILogChannel _defaultChannel;
        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LogChannelService.
        /// </summary>
        /// <param name="profilerService">Service for performance monitoring</param>
        /// <param name="alertService">Service for critical notifications</param>
        /// <param name="messageBusService">Service for loose coupling through events</param>
        public LogChannelService(
            IProfilerService profilerService = null,
            IAlertService alertService = null,
            IMessageBusService messageBusService = null)
        {
            _channels = new ConcurrentDictionary<string, ILogChannel>();
            _profilerService = profilerService ?? NullProfilerService.Instance;
            _alertService = alertService;
            _messageBusService = messageBusService;
        }

        #endregion

        #region Channel Registration and Management

        /// <inheritdoc />
        public void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogChannelService));

            using var scope = _profilerService.BeginScope("LogChannelService.RegisterChannel");
            
            lock (_lock)
            {
                if (_channels.TryAdd(channel.Name, channel))
                {
                    PublishChannelRegisteredMessage(channel.Name, correlationId);
                }
                else
                {
                    TriggerChannelWarning($"Channel with name '{channel.Name}' is already registered", correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(channelName)) return false;
            if (_disposed) return false;

            using var scope = _profilerService.BeginScope("LogChannelService.UnregisterChannel");
            
            lock (_lock)
            {
                if (_channels.TryRemove(channelName, out var channel))
                {
                    try
                    {
                        // Clear default channel if it's the one being removed
                        if (_defaultChannel == channel)
                        {
                            _defaultChannel = null;
                        }
                        
                        PublishChannelUnregisteredMessage(channelName, correlationId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        TriggerChannelError($"Error unregistering channel '{channelName}': {ex.Message}", ex, correlationId);
                        return false;
                    }
                }
                
                return false;
            }
        }

        /// <inheritdoc />
        public ILogChannel GetChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return null;
            
            _channels.TryGetValue(channelName, out var channel);
            return channel;
        }

        /// <inheritdoc />
        public bool HasChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return false;
            return _channels.ContainsKey(channelName);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ILogChannel> GetChannels()
        {
            return _channels.Values.AsValueEnumerable().ToList().AsReadOnly();
        }

        #endregion

        #region Channel Routing and Filtering

        /// <inheritdoc />
        public ILogChannel RouteMessage(LogMessage logMessage)
        {
            using var scope = _profilerService.BeginScope("LogChannelService.RouteMessage");

            // Check if message specifies a channel
            if (!string.IsNullOrEmpty(logMessage.Channel.ToString()) && _channels.TryGetValue(logMessage.Channel.ToString(), out var specificChannel))
            {
                return specificChannel;
            }

            // Route based on log level or other criteria
            // This is a simple implementation - can be enhanced with more sophisticated routing rules
            foreach (var channel in _channels.Values)
            {
                if (channel.ShouldProcessMessage(logMessage))
                {
                    return channel;
                }
            }

            // Return default channel if no specific routing applies
            return _defaultChannel;
        }

        /// <inheritdoc />
        public ILogChannel GetDefaultChannel()
        {
            return _defaultChannel;
        }

        /// <inheritdoc />
        public bool SetDefaultChannel(string channelName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                _defaultChannel = null;
                return true;
            }

            using var scope = _profilerService.BeginScope("LogChannelService.SetDefaultChannel");
            
            if (_channels.TryGetValue(channelName, out var channel))
            {
                _defaultChannel = channel;
                PublishDefaultChannelChangedMessage(channelName, correlationId);
                return true;
            }
            
            return false;
        }

        #endregion

        #region Channel Configuration

        /// <inheritdoc />
        public void SetEnabled(bool enabled, FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogChannelService.SetEnabled");

            foreach (var channel in _channels.Values)
            {
                try
                {
                    channel.IsEnabled = enabled;
                }
                catch (Exception ex)
                {
                    TriggerChannelError($"Failed to set enabled state for channel '{channel.Name}': {ex.Message}", ex, correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool SetEnabled(string channelName, bool enabled, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(channelName)) return false;

            using var scope = _profilerService.BeginScope("LogChannelService.SetEnabledSpecific");
            
            if (_channels.TryGetValue(channelName, out var channel))
            {
                try
                {
                    channel.IsEnabled = enabled;
                    return true;
                }
                catch (Exception ex)
                {
                    TriggerChannelError($"Failed to set enabled state for channel '{channelName}': {ex.Message}", ex, correlationId);
                    return false;
                }
            }
            
            return false;
        }

        #endregion

        #region Health Monitoring

        /// <inheritdoc />
        public bool PerformHealthCheck()
        {
            using var scope = _profilerService.BeginScope("LogChannelService.PerformHealthCheck");

            if (_disposed) return false;
            
            foreach (var channel in _channels.Values)
            {
                try
                {
                    if (!channel.IsHealthy)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    TriggerChannelError($"Health check error for channel '{channel.Name}': {ex.Message}", ex);
                    return false;
                }
            }
            
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            using var scope = _profilerService.BeginScope("LogChannelService.GetHealthStatus");

            var healthStatus = new Dictionary<string, bool>();
            
            foreach (var channel in _channels.Values)
            {
                try
                {
                    healthStatus[channel.Name] = channel.IsHealthy;
                }
                catch (Exception ex)
                {
                    healthStatus[channel.Name] = false;
                    TriggerChannelError($"Health check failed for channel '{channel.Name}': {ex.Message}", ex);
                }
            }
            
            return new ReadOnlyDictionary<string, bool>(healthStatus);
        }

        /// <inheritdoc />
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogChannelService.ValidateConfiguration");

            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            // Validate channels
            if (_channels.Count == 0)
            {
                warnings.Add(new ValidationWarning("No log channels registered", "Channels"));
            }
            
            foreach (var channel in _channels.Values)
            {
                try
                {
                    if (!channel.IsHealthy)
                    {
                        warnings.Add(new ValidationWarning($"Channel '{channel.Name}' is not healthy", $"Channel.{channel.Name}"));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError($"Channel '{channel.Name}' validation error: {ex.Message}", $"Channel.{channel.Name}"));
                }
            }
            
            // Return appropriate result
            if (errors.Count == 0)
            {
                return ValidationResult.Success(
                    component: "LogChannelService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["ChannelCount"] = _channels.Count,
                        ["HasDefaultChannel"] = _defaultChannel != null,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
            else
            {
                return ValidationResult.Failure(
                    errors: errors,
                    component: "LogChannelService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["ChannelCount"] = _channels.Count,
                        ["HasDefaultChannel"] = _defaultChannel != null,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Publishes a channel registered message through the message bus.
        /// </summary>
        private void PublishChannelRegisteredMessage(string channelName, FixedString64Bytes correlationId)
        {
            // Simple logging configuration changed message for now - can be enhanced later
            if (_messageBusService != null)
            {
                var message = LoggingConfigurationChangedMessage.Create(
                    LogConfigurationChangeType.ChannelAdded,
                    "LogChannelService",
                    "RegisteredChannels",
                    previousValue: "Channel registration",
                    newValue: $"Channel '{channelName}' registered",
                    changedBy: "LogChannelService",
                    changeReason: "Channel registration");
                
                _messageBusService.PublishMessage(message);
            }
        }

        /// <summary>
        /// Publishes a channel unregistered message through the message bus.
        /// </summary>
        private void PublishChannelUnregisteredMessage(string channelName, FixedString64Bytes correlationId)
        {
            // Simple logging configuration changed message for now - can be enhanced later
            if (_messageBusService != null)
            {
                var message = LoggingConfigurationChangedMessage.Create(
                    LogConfigurationChangeType.ChannelRemoved,
                    "LogChannelService",
                    "RegisteredChannels",
                    previousValue: $"Channel '{channelName}' was registered",
                    newValue: "Channel unregistered",
                    changedBy: "LogChannelService",
                    changeReason: "Channel unregistration");
                
                _messageBusService.PublishMessage(message);
            }
        }

        /// <summary>
        /// Publishes a default channel changed message through the message bus.
        /// </summary>
        private void PublishDefaultChannelChangedMessage(string channelName, FixedString64Bytes correlationId)
        {
            // Simple logging configuration changed message for now - can be enhanced later
            if (_messageBusService != null)
            {
                var message = LoggingConfigurationChangedMessage.Create(
                    LogConfigurationChangeType.DefaultChannelChanged,
                    "LogChannelService",
                    "DefaultChannel",
                    previousValue: "Previous default channel",
                    newValue: $"Default channel set to '{channelName}'",
                    changedBy: "LogChannelService",
                    changeReason: "Default channel configuration");
                
                _messageBusService.PublishMessage(message);
            }
        }

        /// <summary>
        /// Triggers a warning for channel operations.
        /// </summary>
        private void TriggerChannelWarning(string message, FixedString64Bytes correlationId = default)
        {
            // Could log warning or publish message - for now, just suppress
        }

        /// <summary>
        /// Triggers an error alert for critical channel failures.
        /// </summary>
        private void TriggerChannelError(string message, Exception exception = null, FixedString64Bytes correlationId = default)
        {
            try
            {
                // Use existing LoggingTargetErrorMessage pattern for channels too
                if (_messageBusService != null)
                {
                    var errorMessage = LoggingTargetErrorMessage.Create(
                        "LogChannelService",
                        message,
                        severity: LogTargetErrorSeverity.Error);
                    
                    _messageBusService.PublishMessage(errorMessage);
                }
                
                // Trigger alert for backward compatibility
                if (_alertService != null)
                {
                    var alertMessage = exception != null ? $"{message} - {exception.Message}" : message;
                    // Truncate message to fit in FixedString512Bytes
                    if (alertMessage.Length > 511)
                        alertMessage = alertMessage.Substring(0, 511);
                        
                    _alertService.RaiseAlert(
                        new FixedString512Bytes(alertMessage),
                        AlertSeverity.Medium,
                        new FixedString64Bytes("LogChannelService"),
                        new FixedString32Bytes("ChannelError"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger channel error alert: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the log channel service and all registered channels.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;

            using var scope = _profilerService.BeginScope("LogChannelService.Dispose");
            
            // Clear default channel reference
            _defaultChannel = null;
            
            // Clear all channels (channels are not disposed as they may be shared)
            _channels.Clear();
        }

        #endregion
    }
}