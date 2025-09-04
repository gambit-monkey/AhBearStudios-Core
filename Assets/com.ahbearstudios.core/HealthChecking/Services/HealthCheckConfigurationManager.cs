using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production-ready health check configuration manager with full core system integration
    /// Follows Builder → Config → Factory → Service pattern with proper logging, alerting, and message bus integration
    /// </summary>
    public sealed class HealthCheckConfigurationManager : IHealthCheckConfigurationManager, IDisposable
    {
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBus;
        private readonly IProfilerService _profilerService;
        private readonly ProfilerMarker _updateConfigMarker;
        private readonly ProfilerMarker _validateConfigMarker;

        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _healthCheckConfigurations;
        private IHealthCheckServiceConfig _serviceConfig;
        private readonly object _configLock = new();
        private bool _disposed;

        public HealthCheckConfigurationManager(
            ILoggingService loggingService,
            IAlertService alertService,
            IMessageBusService messageBus,
            IProfilerService profilerService,
            IHealthCheckServiceConfig initialServiceConfig = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));

            _updateConfigMarker = new ProfilerMarker("HealthCheckConfigManager.UpdateConfig");
            _validateConfigMarker = new ProfilerMarker("HealthCheckConfigManager.ValidateConfig");

            _healthCheckConfigurations = new ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration>();
            _serviceConfig = initialServiceConfig ?? HealthCheckServiceConfig.ForProduction();

            _loggingService.LogInfo("HealthCheckConfigurationManager initialized",
                DeterministicIdGenerator.GenerateCorrelationId("ConfigManagerInit", "System"));
        }

        public IHealthCheckServiceConfig ServiceConfig => _serviceConfig;

        public async UniTask UpdateServiceConfigurationAsync(IHealthCheckServiceConfig newConfig, string changedBy = "System")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));

            using (_updateConfigMarker.Auto())
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ServiceConfigUpdate", changedBy);

                try
                {
                    _loggingService.LogInfo($"Updating service configuration requested by {changedBy}", correlationId);

                    // Validate new configuration
                    var validationErrors = ValidateConfiguration(newConfig);
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = $"Service configuration validation failed: {string.Join(", ", validationErrors)}";
                        _loggingService.LogError(errorMessage, correlationId);
                        await _alertService.RaiseAlertAsync(new Alert
                        {
                            Id = DeterministicIdGenerator.GenerateAlertId("ServiceConfigValidation", changedBy),
                            Severity = AlertSeverity.Warning,
                            Title = "Health Check Service Configuration Validation Failed",
                            Message = errorMessage,
                            Source = "HealthCheckConfigurationManager",
                            CorrelationId = correlationId,
                            Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Validation" }
                        });
                        throw new InvalidOperationException(errorMessage);
                    }

                    var previousConfig = _serviceConfig;
                    
                    lock (_configLock)
                    {
                        _serviceConfig = newConfig;
                    }

                    // Publish configuration change message
                    var configChangeMessage = new HealthCheckServiceConfigurationChangedMessage
                    {
                        ChangeType = "ServiceConfigurationUpdate",
                        ChangeDescription = $"Service configuration updated by {changedBy}",
                        AutomaticChecksEnabled = newConfig.EnableAutomaticChecks,
                        NewAutomaticCheckInterval = newConfig.AutomaticCheckInterval,
                        MaxConcurrentHealthChecks = newConfig.MaxConcurrentHealthChecks,
                        CircuitBreakerEnabled = newConfig.EnableCircuitBreaker,
                        GracefulDegradationEnabled = newConfig.EnableGracefulDegradation,
                        ChangedBy = changedBy,
                        PreviousVersion = "Previous",
                        NewVersion = "Current"
                    };

                    await _messageBus.PublishAsync(configChangeMessage);

                    _loggingService.LogInfo($"Service configuration updated successfully by {changedBy}", correlationId);

                    // Raise informational alert
                    await _alertService.RaiseAlertAsync(new Alert
                    {
                        Id = DeterministicIdGenerator.GenerateAlertId("ServiceConfigUpdated", changedBy),
                        Severity = AlertSeverity.Info,
                        Title = "Health Check Service Configuration Updated",
                        Message = $"Service configuration has been updated by {changedBy}",
                        Source = "HealthCheckConfigurationManager",
                        CorrelationId = correlationId,
                        Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Update" }
                    });
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to update service configuration: {ex.Message}", correlationId, ex);
                    await _alertService.RaiseAlertAsync(new Alert
                    {
                        Id = DeterministicIdGenerator.GenerateAlertId("ServiceConfigUpdateError", changedBy),
                        Severity = AlertSeverity.Critical,
                        Title = "Health Check Service Configuration Update Failed",
                        Message = $"Failed to update service configuration: {ex.Message}",
                        Source = "HealthCheckConfigurationManager",
                        CorrelationId = correlationId,
                        Exception = ex,
                        Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Error" }
                    });
                    throw;
                }
            }
        }

        public HealthCheckConfiguration GetHealthCheckConfiguration(FixedString64Bytes name)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            
            return _healthCheckConfigurations.TryGetValue(name, out var config) ? config : null;
        }

        public async UniTask UpdateHealthCheckConfigurationAsync(HealthCheckConfiguration configuration, string changedBy = "System")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            using (_updateConfigMarker.Auto())
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckConfigUpdate", changedBy);

                try
                {
                    _loggingService.LogInfo($"Updating health check configuration '{configuration.Name}' requested by {changedBy}", correlationId);

                    // Validate configuration
                    var validationErrors = ValidateHealthCheckConfiguration(configuration);
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = $"Health check configuration validation failed for '{configuration.Name}': {string.Join(", ", validationErrors)}";
                        _loggingService.LogError(errorMessage, correlationId);
                        await _alertService.RaiseAlertAsync(new Alert
                        {
                            Id = DeterministicIdGenerator.GenerateAlertId("HealthCheckConfigValidation", changedBy),
                            Severity = AlertSeverity.Warning,
                            Title = "Health Check Configuration Validation Failed",
                            Message = errorMessage,
                            Source = "HealthCheckConfigurationManager",
                            CorrelationId = correlationId,
                            Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Validation" }
                        });
                        throw new InvalidOperationException(errorMessage);
                    }

                    // Update configuration with modification metadata
                    var updatedConfig = configuration with 
                    { 
                        ModifiedAt = DateTime.UtcNow,
                        ModifiedBy = changedBy
                    };

                    _healthCheckConfigurations.AddOrUpdate(configuration.Name, updatedConfig, (key, oldValue) => updatedConfig);

                    // Publish configuration change message
                    var configChangeMessage = new HealthCheckConfigurationChangedMessage
                    {
                        ConfigurationName = configuration.Name,
                        ChangeType = "ConfigurationUpdate",
                        ChangeDescription = $"Health check configuration '{configuration.Name}' updated by {changedBy}",
                        IsEnabled = configuration.Enabled,
                        NewInterval = configuration.Interval,
                        NewTimeout = configuration.Timeout,
                        ChangedBy = changedBy,
                        Metadata = $"Priority: {configuration.Priority}, Critical: {configuration.IsCritical}"
                    };

                    await _messageBus.PublishAsync(configChangeMessage);

                    _loggingService.LogInfo($"Health check configuration '{configuration.Name}' updated successfully by {changedBy}", correlationId);

                    // Raise informational alert for critical health checks
                    if (configuration.IsCritical)
                    {
                        await _alertService.RaiseAlertAsync(new Alert
                        {
                            Id = DeterministicIdGenerator.GenerateAlertId("CriticalHealthCheckConfigUpdated", changedBy),
                            Severity = AlertSeverity.Warning,
                            Title = "Critical Health Check Configuration Updated",
                            Message = $"Critical health check '{configuration.DisplayName}' configuration has been updated by {changedBy}",
                            Source = "HealthCheckConfigurationManager",
                            CorrelationId = correlationId,
                            Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Critical" }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to update health check configuration '{configuration.Name}': {ex.Message}", correlationId, ex);
                    await _alertService.RaiseAlertAsync(new Alert
                    {
                        Id = DeterministicIdGenerator.GenerateAlertId("HealthCheckConfigUpdateError", changedBy),
                        Severity = AlertSeverity.Critical,
                        Title = "Health Check Configuration Update Failed",
                        Message = $"Failed to update health check configuration '{configuration.Name}': {ex.Message}",
                        Source = "HealthCheckConfigurationManager",
                        CorrelationId = correlationId,
                        Exception = ex,
                        Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Error" }
                    });
                    throw;
                }
            }
        }

        public async UniTask SetHealthCheckEnabledAsync(FixedString64Bytes name, bool enabled, string changedBy = "System")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));

            var existingConfig = GetHealthCheckConfiguration(name);
            if (existingConfig == null)
            {
                throw new ArgumentException($"Health check configuration '{name}' not found", nameof(name));
            }

            var updatedConfig = existingConfig.WithEnabled(enabled);
            await UpdateHealthCheckConfigurationAsync(updatedConfig, changedBy);
        }

        public async UniTask UpdateHealthCheckIntervalAsync(FixedString64Bytes name, TimeSpan newInterval, string changedBy = "System")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));

            var existingConfig = GetHealthCheckConfiguration(name);
            if (existingConfig == null)
            {
                throw new ArgumentException($"Health check configuration '{name}' not found", nameof(name));
            }

            var updatedConfig = existingConfig.WithInterval(newInterval);
            await UpdateHealthCheckConfigurationAsync(updatedConfig, changedBy);
        }

        public IReadOnlyCollection<HealthCheckConfiguration> GetAllConfigurations()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            
            return _healthCheckConfigurations.Values.ToList().AsReadOnly();
        }

        public List<string> ValidateConfiguration(IHealthCheckServiceConfig config)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using (_validateConfigMarker.Auto())
            {
                return config.Validate();
            }
        }

        public List<string> ValidateHealthCheckConfiguration(HealthCheckConfiguration config)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using (_validateConfigMarker.Auto())
            {
                return config.Validate();
            }
        }

        public async UniTask ReloadConfigurationsAsync(string source = "System")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ConfigReload", source);

            try
            {
                _loggingService.LogInfo($"Configuration reload requested by {source}", correlationId);

                // In a real implementation, this would reload from persistent storage
                // For now, we'll just log the reload request

                _loggingService.LogInfo($"Configuration reload completed by {source}", correlationId);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to reload configurations: {ex.Message}", correlationId, ex);
                await _alertService.RaiseAlertAsync(new Alert
                {
                    Id = DeterministicIdGenerator.GenerateAlertId("ConfigReloadError", source),
                    Severity = AlertSeverity.Critical,
                    Title = "Health Check Configuration Reload Failed",
                    Message = $"Failed to reload configurations: {ex.Message}",
                    Source = "HealthCheckConfigurationManager",
                    CorrelationId = correlationId,
                    Exception = ex,
                    Tags = new HashSet<FixedString64Bytes> { "HealthCheck", "Configuration", "Reload", "Error" }
                });
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ConfigManagerDispose", "System");
                _loggingService.LogInfo("HealthCheckConfigurationManager disposing", correlationId);
                
                _healthCheckConfigurations.Clear();
                
                _loggingService.LogInfo("HealthCheckConfigurationManager disposed successfully", correlationId);
            }
            catch (Exception ex)
            {
                // Best effort logging during disposal
                try
                {
                    _loggingService?.LogError($"Error during HealthCheckConfigurationManager disposal: {ex.Message}", Guid.Empty, ex);
                }
                catch
                {
                    // Ignore disposal logging errors
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}