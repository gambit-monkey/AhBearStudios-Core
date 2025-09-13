using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Collections;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging.Messages;
using ZLinq;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service for managing log targets in the logging system.
    /// Handles target registration, configuration, health monitoring, and lifecycle management.
    /// Follows the AhBearStudios Core Architecture patterns for service decomposition.
    /// </summary>
    public sealed class LogTargetService : ILogTargetService, IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<string, ILogTarget> _targets;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly object _lock = new object();
        
        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LogTargetService.
        /// </summary>
        /// <param name="profilerService">Service for performance monitoring</param>
        /// <param name="alertService">Service for critical notifications</param>
        /// <param name="messageBusService">Service for loose coupling through events</param>
        public LogTargetService(
            IProfilerService profilerService = null,
            IAlertService alertService = null,
            IMessageBusService messageBusService = null)
        {
            _targets = new ConcurrentDictionary<string, ILogTarget>();
            _profilerService = profilerService ?? NullProfilerService.Instance;
            _alertService = alertService;
            _messageBusService = messageBusService;
        }

        #endregion

        #region Target Registration and Management

        /// <inheritdoc />
        public void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogTargetService));

            using var scope = _profilerService.BeginScope("LogTargetService.RegisterTarget");
            
            lock (_lock)
            {
                if (_targets.TryAdd(target.Name, target))
                {
                    PublishTargetRegisteredMessage(target.Name, correlationId);
                }
                else
                {
                    TriggerTargetWarning($"Target with name '{target.Name}' is already registered", correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            if (_disposed) return false;

            using var scope = _profilerService.BeginScope("LogTargetService.UnregisterTarget");
            
            lock (_lock)
            {
                if (_targets.TryRemove(targetName, out var target))
                {
                    try
                    {
                        target.Dispose();
                        PublishTargetUnregisteredMessage(targetName, correlationId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        TriggerTargetError($"Error disposing target '{targetName}': {ex.Message}", ex, correlationId);
                        return false;
                    }
                }
                
                return false;
            }
        }

        /// <inheritdoc />
        public ILogTarget GetTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;
            
            _targets.TryGetValue(targetName, out var target);
            return target;
        }

        /// <inheritdoc />
        public bool HasTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            return _targets.ContainsKey(targetName);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ILogTarget> GetTargets()
        {
            return _targets.Values.AsValueEnumerable().ToList().AsReadOnly();
        }

        #endregion

        #region Target Configuration

        /// <inheritdoc />
        public void SetMinimumLevel(LogLevel minimumLevel, FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogTargetService.SetMinimumLevel");

            foreach (var target in _targets.Values)
            {
                try
                {
                    target.MinimumLevel = minimumLevel;
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Failed to set minimum level for target '{target.Name}': {ex.Message}", ex, correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool SetMinimumLevel(string targetName, LogLevel minimumLevel, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(targetName)) return false;

            using var scope = _profilerService.BeginScope("LogTargetService.SetMinimumLevelSpecific");
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.MinimumLevel = minimumLevel;
                    return true;
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Failed to set minimum level for target '{targetName}': {ex.Message}", ex, correlationId);
                    return false;
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public void SetEnabled(bool enabled, FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogTargetService.SetEnabled");

            foreach (var target in _targets.Values)
            {
                try
                {
                    target.IsEnabled = enabled;
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Failed to set enabled state for target '{target.Name}': {ex.Message}", ex, correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool SetEnabled(string targetName, bool enabled, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(targetName)) return false;

            using var scope = _profilerService.BeginScope("LogTargetService.SetEnabledSpecific");
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.IsEnabled = enabled;
                    return true;
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Failed to set enabled state for target '{targetName}': {ex.Message}", ex, correlationId);
                    return false;
                }
            }
            
            return false;
        }

        #endregion

        #region Target Operations

        /// <inheritdoc />
        public void WriteToTargets(LogMessage logMessage)
        {
            using var scope = _profilerService.BeginScope("LogTargetService.WriteToTargets");

            foreach (var target in _targets.Values)
            {
                try
                {
                    if (target.IsEnabled && target.ShouldProcessMessage(logMessage))
                    {
                        target.Write(logMessage);
                    }
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Error writing to target '{target.Name}': {ex.Message}", ex);
                }
            }
        }

        /// <inheritdoc />
        public void FlushAll(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogTargetService.FlushAll");

            foreach (var target in _targets.Values)
            {
                try
                {
                    target.Flush();
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Flush error for target '{target.Name}': {ex.Message}", ex, correlationId);
                }
            }
        }

        /// <inheritdoc />
        public bool Flush(string targetName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(targetName)) return false;

            using var scope = _profilerService.BeginScope("LogTargetService.FlushSpecific");
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Flush error for target '{targetName}': {ex.Message}", ex, correlationId);
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
            using var scope = _profilerService.BeginScope("LogTargetService.PerformHealthCheck");

            if (_disposed) return false;
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (!target.PerformHealthCheck())
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    TriggerTargetError($"Health check error for target '{target.Name}': {ex.Message}", ex);
                    return false;
                }
            }
            
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            using var scope = _profilerService.BeginScope("LogTargetService.GetHealthStatus");

            var healthStatus = new Dictionary<string, bool>();
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    healthStatus[target.Name] = target.PerformHealthCheck();
                }
                catch (Exception ex)
                {
                    healthStatus[target.Name] = false;
                    TriggerTargetError($"Health check failed for target '{target.Name}': {ex.Message}", ex);
                }
            }
            
            return new ReadOnlyDictionary<string, bool>(healthStatus);
        }

        /// <inheritdoc />
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope("LogTargetService.ValidateConfiguration");

            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            // Validate targets
            if (_targets.Count == 0)
            {
                warnings.Add(new ValidationWarning("No log targets registered", "Targets"));
            }
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (!target.PerformHealthCheck())
                    {
                        warnings.Add(new ValidationWarning($"Target '{target.Name}' failed health check", $"Target.{target.Name}"));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError($"Target '{target.Name}' validation error: {ex.Message}", $"Target.{target.Name}"));
                }
            }
            
            // Return appropriate result
            if (errors.Count == 0)
            {
                return ValidationResult.Success(
                    component: "LogTargetService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["TargetCount"] = _targets.Count,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
            else
            {
                return ValidationResult.Failure(
                    errors: errors,
                    component: "LogTargetService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["TargetCount"] = _targets.Count,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Publishes a target registered message through the message bus.
        /// </summary>
        private void PublishTargetRegisteredMessage(string targetName, FixedString64Bytes correlationId)
        {
            if (_messageBusService != null)
            {
                var message = LoggingTargetRegisteredMessage.Create(
                    targetName,
                    source: "LogTargetService",
                    correlationId: correlationId.IsEmpty ? default : new Guid(correlationId.ToString()));
                
                _messageBusService.PublishMessage(message);
            }
        }

        /// <summary>
        /// Publishes a target unregistered message through the message bus.
        /// </summary>
        private void PublishTargetUnregisteredMessage(string targetName, FixedString64Bytes correlationId)
        {
            if (_messageBusService != null)
            {
                var message = LoggingTargetUnregisteredMessage.Create(
                    targetName,
                    source: "LogTargetService",
                    correlationId: correlationId.IsEmpty ? default : new Guid(correlationId.ToString()));
                
                _messageBusService.PublishMessage(message);
            }
        }

        /// <summary>
        /// Triggers a warning for target operations.
        /// </summary>
        private void TriggerTargetWarning(string message, FixedString64Bytes correlationId = default)
        {
            // Could log warning or publish message - for now, just suppress
        }

        /// <summary>
        /// Triggers an error alert for critical target failures.
        /// </summary>
        private void TriggerTargetError(string message, Exception exception = null, FixedString64Bytes correlationId = default)
        {
            try
            {
                // Publish error message through message bus
                if (_messageBusService != null)
                {
                    var errorMessage = LoggingTargetErrorMessage.Create(
                        "LogTargetService",
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
                        new FixedString64Bytes("LogTargetService"),
                        new FixedString32Bytes("TargetError"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger target error alert: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the log target service and all registered targets.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;

            using var scope = _profilerService.BeginScope("LogTargetService.Dispose");
            
            // Flush all targets before disposal
            FlushAll();
            
            // Dispose all targets
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LogTargetService disposal error for target '{target.Name}': {ex.Message}");
                }
            }
            
            _targets.Clear();
        }

        #endregion
    }
}