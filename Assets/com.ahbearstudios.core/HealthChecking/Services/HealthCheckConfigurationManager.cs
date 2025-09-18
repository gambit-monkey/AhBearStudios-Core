using System;
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
                DeterministicIdGenerator.GenerateCorrelationId("ConfigManagerInit", "System"),
                sourceContext: nameof(HealthCheckConfigurationManager));
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
                    _loggingService.LogInfo($"Updating service configuration requested by {changedBy}", correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));

                    // Validate new configuration
                    var validationErrors = ValidateConfiguration(newConfig);
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = $"Service configuration validation failed: {string.Join(", ", validationErrors)}";
                        _loggingService.LogError(errorMessage, correlationId,
                            sourceContext: nameof(HealthCheckConfigurationManager));
                        var validationAlert = Alert.Create(
                            message: "Health Check Service Configuration Validation Failed",
                            severity: AlertSeverity.Warning,
                            source: "HealthCheckConfigurationManager",
                            tag: "Configuration",
                            correlationId: correlationId
                        ) with {
                            Id = DeterministicIdGenerator.GenerateAlertId("Warning", "HealthCheckConfigurationManager", $"ServiceConfigValidation_{changedBy}")
                        };
                        await _alertService.RaiseAlertAsync(validationAlert);
                        throw new InvalidOperationException(errorMessage);
                    }

                    var previousConfig = _serviceConfig;
                    
                    lock (_configLock)
                    {
                        _serviceConfig = newConfig;
                    }

                    // Publish configuration change message
                    var configChangeMessage = HealthCheckServiceConfigurationChangedMessage.Create(
                        changeType: "ServiceConfigurationUpdate",
                        changeDescription: $"Service configuration updated by {changedBy}",
                        automaticChecksEnabled: newConfig.EnableAutomaticChecks,
                        maxConcurrentHealthChecks: newConfig.MaxConcurrentHealthChecks,
                        circuitBreakerEnabled: newConfig.EnableCircuitBreaker,
                        gracefulDegradationEnabled: newConfig.EnableGracefulDegradation,
                        newAutomaticCheckInterval: newConfig.AutomaticCheckInterval,
                        changedBy: changedBy,
                        previousVersion: "Previous",
                        newVersion: "Current",
                        source: "HealthCheckConfigurationManager",
                        correlationId: correlationId
                    );

                    await _messageBus.PublishMessageAsync(configChangeMessage);

                    _loggingService.LogInfo($"Service configuration updated successfully by {changedBy}", correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));

                    // Raise informational alert
                    var updateAlert = Alert.Create(
                        message: $"Service configuration has been updated by {changedBy}",
                        severity: AlertSeverity.Info,
                        source: "HealthCheckConfigurationManager",
                        tag: "Configuration",
                        correlationId: correlationId
                    ) with {
                        Id = DeterministicIdGenerator.GenerateAlertId("Info", "HealthCheckConfigurationManager", $"ServiceConfigUpdated_{changedBy}")
                    };
                    await _alertService.RaiseAlertAsync(updateAlert);
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to update service configuration: {ex.Message}", ex, correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));
                    var errorAlert = Alert.Create(
                        message: $"Failed to update service configuration: {ex.Message}",
                        severity: AlertSeverity.Critical,
                        source: "HealthCheckConfigurationManager",
                        tag: "Error",
                        correlationId: correlationId,
                        context: AlertContext.WithException(ex)
                    ) with {
                        Id = DeterministicIdGenerator.GenerateAlertId("Critical", "HealthCheckConfigurationManager", $"ServiceConfigUpdateError_{changedBy}")
                    };
                    await _alertService.RaiseAlertAsync(errorAlert);
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
                    _loggingService.LogInfo($"Updating health check configuration '{configuration.Name}' requested by {changedBy}", correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));

                    // Validate configuration
                    var validationErrors = ValidateHealthCheckConfiguration(configuration);
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = $"Health check configuration validation failed for '{configuration.Name}': {string.Join(", ", validationErrors)}";
                        _loggingService.LogError(errorMessage, correlationId,
                            sourceContext: nameof(HealthCheckConfigurationManager));
                        var healthCheckValidationAlert = Alert.Create(
                            message: "Health Check Configuration Validation Failed",
                            severity: AlertSeverity.Warning,
                            source: "HealthCheckConfigurationManager",
                            tag: "Validation",
                            correlationId: correlationId
                        ) with {
                            Id = DeterministicIdGenerator.GenerateAlertId("Warning", "HealthCheckConfigurationManager", $"HealthCheckConfigValidation_{changedBy}")
                        };
                        await _alertService.RaiseAlertAsync(healthCheckValidationAlert);
                        throw new InvalidOperationException(errorMessage);
                    }

                    // Update configuration in the store
                    _healthCheckConfigurations.AddOrUpdate(configuration.Name, configuration, (key, oldValue) => configuration);

                    // Publish configuration change message
                    var configChangeMessage = HealthCheckConfigurationChangedMessage.Create(
                        configurationName: configuration.Name,
                        changeType: "ConfigurationUpdate",
                        changeDescription: $"Health check configuration '{configuration.Name}' updated by {changedBy}",
                        isEnabled: configuration.Enabled,
                        newInterval: configuration.Interval,
                        newTimeout: configuration.Timeout,
                        changedBy: changedBy,
                        metadata: $"Priority: {configuration.Priority}, Critical: {configuration.IsCritical}",
                        source: "HealthCheckConfigurationManager",
                        correlationId: correlationId
                    );

                    await _messageBus.PublishMessageAsync(configChangeMessage);

                    _loggingService.LogInfo($"Health check configuration '{configuration.Name}' updated successfully by {changedBy}", correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));

                    // Raise informational alert for critical health checks
                    if (configuration.IsCritical)
                    {
                        var criticalUpdateAlert = Alert.Create(
                            message: $"Critical health check '{configuration.DisplayName}' configuration has been updated by {changedBy}",
                            severity: AlertSeverity.Warning,
                            source: "HealthCheckConfigurationManager",
                            tag: "Critical",
                            correlationId: correlationId
                        ) with {
                            Id = DeterministicIdGenerator.GenerateAlertId("Warning", "HealthCheckConfigurationManager", $"CriticalHealthCheckConfigUpdated_{changedBy}")
                        };
                        await _alertService.RaiseAlertAsync(criticalUpdateAlert);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to update health check configuration '{configuration.Name}': {ex.Message}", ex, correlationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));
                    var configUpdateErrorAlert = Alert.Create(
                        message: $"Failed to update health check configuration '{configuration.Name}': {ex.Message}",
                        severity: AlertSeverity.Critical,
                        source: "HealthCheckConfigurationManager",
                        tag: "Error",
                        correlationId: correlationId,
                        context: AlertContext.WithException(ex)
                    ) with {
                        Id = DeterministicIdGenerator.GenerateAlertId("Critical", "HealthCheckConfigurationManager", $"HealthCheckConfigUpdateError_{changedBy}")
                    };
                    await _alertService.RaiseAlertAsync(configUpdateErrorAlert);
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

            var updatedConfig = existingConfig with { Enabled = enabled };
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

            var updatedConfig = existingConfig with { Interval = newInterval };
            await UpdateHealthCheckConfigurationAsync(updatedConfig, changedBy);
        }

        public IReadOnlyCollection<HealthCheckConfiguration> GetAllConfigurations()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HealthCheckConfigurationManager));
            
            return _healthCheckConfigurations.Values.AsEnumerable().ToList().AsReadOnly();
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
                _loggingService.LogInfo($"Configuration reload requested by {source}", correlationId,
                    sourceContext: nameof(HealthCheckConfigurationManager));

                // In a real implementation, this would reload from persistent storage
                // For now, we'll just log the reload request

                _loggingService.LogInfo($"Configuration reload completed by {source}", correlationId,
                    sourceContext: nameof(HealthCheckConfigurationManager));
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to reload configurations: {ex.Message}", ex, correlationId,
                    sourceContext: nameof(HealthCheckConfigurationManager));
                var reloadErrorAlert = Alert.Create(
                    message: $"Failed to reload configurations: {ex.Message}",
                    severity: AlertSeverity.Critical,
                    source: "HealthCheckConfigurationManager",
                    tag: "Error",
                    correlationId: correlationId,
                    context: AlertContext.WithException(ex)
                ) with {
                    Id = DeterministicIdGenerator.GenerateAlertId("Critical", "HealthCheckConfigurationManager", $"ConfigReloadError_{source}")
                };
                await _alertService.RaiseAlertAsync(reloadErrorAlert);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ConfigManagerDispose", "System");
                _loggingService.LogInfo("HealthCheckConfigurationManager disposing", correlationId,
                    sourceContext: nameof(HealthCheckConfigurationManager));
                
                _healthCheckConfigurations.Clear();
                
                _loggingService.LogInfo("HealthCheckConfigurationManager disposed successfully", correlationId,
                    sourceContext: nameof(HealthCheckConfigurationManager));
            }
            catch (Exception ex)
            {
                // Best effort logging during disposal
                try
                {
                    var disposalCorrelationId = DeterministicIdGenerator.GenerateCorrelationId("ConfigManagerDispose", "System");
                    _loggingService?.LogException($"Error during HealthCheckConfigurationManager disposal: {ex.Message}", ex, disposalCorrelationId,
                        sourceContext: nameof(HealthCheckConfigurationManager));
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