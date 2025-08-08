using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Alert channel implementation that sends alerts to the logging system.
    /// Maps alert severities to appropriate log levels and includes rich context.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public sealed class LogAlertChannel : BaseAlertChannel
    {
        private readonly ILoggingService _loggingService;
        private bool _includeContext = true;
        private bool _includeStackTrace = false;
        private string _logPrefix = "[ALERT]";
        
        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        public override FixedString64Bytes Name => "LogChannel";

        /// <summary>
        /// Initializes a new instance of the LogAlertChannel class.
        /// </summary>
        /// <param name="loggingService">The logging service to use</param>
        /// <param name="messageBusService">Message bus service for publishing channel events</param>
        public LogAlertChannel(ILoggingService loggingService, IMessageBusService messageBusService) : base(messageBusService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Core implementation for sending an alert synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if successful</returns>
        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            if (_loggingService == null || !_loggingService.IsEnabled)
                return false;

            var message = FormatAlertMessage(alert);
            var properties = BuildLogProperties(alert);
            var correlationIdString = correlationId == default ? alert.CorrelationId.ToString() : correlationId.ToString();
            var sourceContext = alert.Source.ToString();

            // Map alert severity to appropriate log level
            switch (alert.Severity)
            {
                case AlertSeverity.Debug:
                    _loggingService.LogDebug(message, correlationIdString, sourceContext, properties);
                    break;
                    
                case AlertSeverity.Info:
                    _loggingService.LogInfo(message, correlationIdString, sourceContext, properties);
                    break;
                    
                case AlertSeverity.Low:
                case AlertSeverity.Medium:
                case AlertSeverity.High:
                    _loggingService.LogInfo(message, correlationIdString, sourceContext, properties);
                    break;
                    
                case AlertSeverity.Warning:
                    _loggingService.LogWarning(message, correlationIdString, sourceContext, properties);
                    break;
                    
                case AlertSeverity.Critical:
                    _loggingService.LogError(message, correlationIdString, sourceContext, properties);
                    break;
                    
                case AlertSeverity.Emergency:
                    _loggingService.LogCritical(message, correlationIdString, sourceContext, properties);
                    break;
                    
                default:
                    _loggingService.LogInfo(message, correlationIdString, sourceContext, properties);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Core implementation for sending an alert asynchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with result</returns>
        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            // Logging is typically synchronous, so we'll run it on a background thread
            return await UniTask.RunOnThreadPool(() => SendAlertCore(alert, correlationId), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Core implementation for health testing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health result</returns>
        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            await UniTask.Yield(cancellationToken);
            
            if (_loggingService == null)
            {
                return ChannelHealthResult.Unhealthy("Logging service is not available");
            }

            if (!_loggingService.IsEnabled)
            {
                return ChannelHealthResult.Unhealthy("Logging service is disabled");
            }

            try
            {
                // Test logging with a simple message
                _loggingService.LogDebug($"{_logPrefix} Health check from alert channel", correlationId.ToString(), Name.ToString());
                return ChannelHealthResult.Healthy("Logging channel is operational");
            }
            catch (Exception ex)
            {
                return ChannelHealthResult.Unhealthy($"Failed to write to log: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Core implementation for channel initialization.
        /// </summary>
        /// <param name="config">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with initialization result</returns>
        protected override async UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId)
        {
            await UniTask.Yield();
            
            if (config?.Settings != null)
            {
                if (config.Settings.TryGetValue("IncludeContext", out var includeContextStr) && 
                    bool.TryParse(includeContextStr, out var includeContext))
                    _includeContext = includeContext;
                
                if (config.Settings.TryGetValue("IncludeStackTrace", out var includeStackStr) && 
                    bool.TryParse(includeStackStr, out var includeStack))
                    _includeStackTrace = includeStack;
                
                if (config.Settings.TryGetValue("LogPrefix", out var prefix) && 
                    !string.IsNullOrWhiteSpace(prefix))
                    _logPrefix = prefix;
            }

            _loggingService.LogInfo($"{_logPrefix} Alert logging channel initialized", correlationId.ToString(), Name.ToString());
            return true;
        }

        /// <summary>
        /// Creates the default configuration for this channel.
        /// </summary>
        /// <returns>Default channel configuration</returns>
        protected override ChannelConfig CreateDefaultConfiguration()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = AlertChannelType.Log,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                Settings = new Dictionary<string, string>
                {
                    ["IncludeContext"] = "true",
                    ["IncludeStackTrace"] = "false",
                    ["LogPrefix"] = "[ALERT]",
                    ["MaxAlertsPerSecond"] = "1000"
                }
            };
        }

        /// <summary>
        /// Formats an alert into a log message string.
        /// </summary>
        /// <param name="alert">Alert to format</param>
        /// <returns>Formatted message</returns>
        private string FormatAlertMessage(Alert alert)
        {
            var state = alert.State switch
            {
                AlertState.Active => "",
                AlertState.Acknowledged => " [ACKNOWLEDGED]",
                AlertState.Resolved => " [RESOLVED]",
                AlertState.Suppressed => " [SUPPRESSED]",
                _ => ""
            };

            var tag = alert.Tag.IsEmpty ? "" : $" [{alert.Tag.ToString()}]";
            var count = alert.Count > 1 ? $" (Count: {alert.Count})" : "";
            
            return $"{_logPrefix} [{alert.Severity}]{state}{tag} {alert.Message.ToString()}{count}";
        }

        /// <summary>
        /// Builds structured log properties from an alert.
        /// </summary>
        /// <param name="alert">Alert to extract properties from</param>
        /// <returns>Dictionary of log properties</returns>
        private Dictionary<string, object> BuildLogProperties(Alert alert)
        {
            var properties = new Dictionary<string, object>
            {
                ["AlertId"] = alert.Id,
                ["AlertSeverity"] = alert.Severity.ToString(),
                ["AlertSource"] = alert.Source.ToString(),
                ["AlertState"] = alert.State.ToString(),
                ["AlertTimestamp"] = alert.Timestamp,
                ["AlertCount"] = alert.Count
            };

            if (!alert.Tag.IsEmpty)
                properties["AlertTag"] = alert.Tag.ToString();

            if (alert.OperationId != default)
                properties["OperationId"] = alert.OperationId;

            if (alert.CorrelationId != default)
                properties["AlertCorrelationId"] = alert.CorrelationId;

            if (alert.IsAcknowledged)
            {
                properties["AcknowledgedBy"] = alert.AcknowledgedBy.ToString();
                properties["AcknowledgedAt"] = alert.AcknowledgedTimestamp;
            }

            if (alert.IsResolved)
            {
                properties["ResolvedBy"] = alert.ResolvedBy.ToString();
                properties["ResolvedAt"] = alert.ResolvedTimestamp;
            }

            // Include context if configured
            if (_includeContext && alert.Context != null)
            {
                properties["HasContext"] = true;
                
                if (alert.Context.Exception != null)
                {
                    properties["ExceptionType"] = alert.Context.Exception.TypeName.ToString();
                    properties["ExceptionMessage"] = alert.Context.Exception.Message.ToString();
                    
                    if (_includeStackTrace && !alert.Context.Exception.StackTrace.IsEmpty)
                    {
                        properties["StackTrace"] = alert.Context.Exception.StackTrace.ToString();
                    }
                }

                if (alert.Context.Performance != null)
                {
                    properties["DurationMs"] = alert.Context.Performance.Duration.TotalMilliseconds;
                    properties["MemoryUsageBytes"] = alert.Context.Performance.MemoryUsageBytes;
                    properties["CpuUsagePercent"] = alert.Context.Performance.CpuUsagePercent;
                }

                if (alert.Context.System != null)
                {
                    properties["MachineName"] = alert.Context.System.MachineName.ToString();
                    properties["ProcessId"] = alert.Context.System.ProcessId;
                    properties["ThreadId"] = alert.Context.System.ThreadId;
                }

                if (alert.Context.User != null)
                {
                    properties["UserId"] = alert.Context.User.UserId.ToString();
                    properties["SessionId"] = alert.Context.User.SessionId.ToString();
                }

                if (alert.Context.Network != null)
                {
                    properties["RequestUrl"] = alert.Context.Network.RequestUrl.ToString();
                    properties["HttpStatusCode"] = alert.Context.Network.HttpStatusCode;
                    properties["RequestDurationMs"] = alert.Context.Network.RequestDurationMs;
                }

                // Add custom properties
                if (alert.Context.Properties != null)
                {
                    foreach (var kvp in alert.Context.Properties)
                    {
                        properties[$"Context.{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Validates channel configuration.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        protected override ValidationResult ValidateConfiguration(ChannelConfig config)
        {
            var baseResult = base.ValidateConfiguration(config);
            if (!baseResult.IsValid)
                return baseResult;

            if (_loggingService == null)
            {
                return ValidationResult.Failure("Logging service is required", Name.ToString());
            }

            var warnings = new List<ValidationWarning>();
            
            if (config.Settings != null)
            {
                if (config.Settings.TryGetValue("MaxAlertsPerSecond", out var maxStr) && 
                    int.TryParse(maxStr, out var maxAlerts) && maxAlerts > 1000)
                {
                    warnings.Add(new ValidationWarning(
                        "High alert rate may impact logging performance",
                        "MaxAlertsPerSecond"));
                }

                if (config.Settings.TryGetValue("IncludeStackTrace", out var stackStr) && 
                    bool.TryParse(stackStr, out var includeStack) && includeStack)
                {
                    warnings.Add(new ValidationWarning(
                        "Including stack traces will increase log size significantly",
                        "IncludeStackTrace"));
                }
            }

            return warnings.Count > 0 
                ? ValidationResult.Success(Name.ToString(), warnings) 
                : ValidationResult.Success(Name.ToString());
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        protected override void DisposeCore()
        {
            // Log channel disposal
            try
            {
                _loggingService?.LogInfo($"{_logPrefix} Alert logging channel disposed", null, Name.ToString());
            }
            catch
            {
                // Ignore disposal logging errors
            }
        }
    }
}