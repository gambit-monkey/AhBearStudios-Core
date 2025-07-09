using System;
using System.Collections.Generic;
using System.Diagnostics;
using AhBearStudios.Core.Alerts;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Alerts.Interfaces;
using Reflex.Core;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Lifecycle Implementation

        /// <inheritdoc />
        public virtual BootstrapValidationResult ValidateInstaller(IBootstrapConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = new BootstrapValidationResult
                {
                    IsValid = true,
                    InstallerName = InstallerName,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };

                // Validate configuration requirements
                var configRequirements = GetConfigurationRequirements();
                if (!ValidateConfigurationRequirements(config, configRequirements, result))
                {
                    result.IsValid = false;
                }

                // Validate platform requirements
                var platformRequirements = GetPlatformRequirements();
                if (!ValidatePlatformRequirements(platformRequirements, result))
                {
                    result.IsValid = false;
                }

                // Validate service registrations
                ValidateServiceRegistrations(result);

                // Allow derived classes to add custom validation
                OnValidateInstaller(config, result);

                result.ValidationDuration = stopwatch.Elapsed;
                return result;
            }
            catch (Exception ex)
            {
                return new BootstrapValidationResult
                {
                    IsValid = false,
                    InstallerName = InstallerName,
                    Errors = new List<string> { $"Validation failed with exception: {ex.Message}" },
                    Warnings = new List<string>(),
                    ValidationDuration = stopwatch.Elapsed
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <inheritdoc />
        public virtual void PreInstall(IBootstrapConfig config, IBootstrapContext context)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                _logger = context.Logger;
                _correlationId = context.CorrelationId;

                LogPhaseStart("pre-installation");

                lock (_metricsLock)
                {
                    _metrics.PreInstallStartTime = DateTime.UtcNow;
                }

                // Allow derived classes to implement custom pre-installation logic
                OnPreInstall(config, context);

                lock (_metricsLock)
                {
                    _metrics.PreInstallEndTime = DateTime.UtcNow;
                    _metrics.PreInstallDuration = _metrics.PreInstallEndTime - _metrics.PreInstallStartTime;
                }

                LogPhaseEnd("pre-installation", _metrics.PreInstallDuration);
            }
            catch (Exception ex)
            {
                lock (_metricsLock)
                {
                    _metrics.ErrorCount++;
                }

                _logger?.LogException(ex, $"Pre-installation failed for {InstallerName}");
                context.AlertService?.RaiseAlert(
                    message: $"Pre-installation failed for {InstallerName}",
                    severity: AlertSeverity.Critical,
                    source: InstallerName,
                    tag: "PreInstallFailure"
                );
                throw;
            }
        }

        /// <inheritdoc />
        public virtual void Install(Container container, IBootstrapConfig config, IBootstrapContext context)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                using var profileScope = context.Profiler?.BeginScope($"{InstallerName}.Install");

                LogPhaseStart("installation");

                lock (_metricsLock)
                {
                    _metrics.InstallStartTime = DateTime.UtcNow;
                    _metrics.MemoryUsageBefore = GetCurrentMemoryUsage();
                }

                // Validate dependencies before installation
                if (!ValidateDependencies(container))
                {
                    throw new InvalidOperationException($"Dependencies not satisfied for {InstallerName}");
                }

                // Execute the actual installation logic
                OnInstall(container, config, context);

                lock (_metricsLock)
                {
                    _metrics.InstallEndTime = DateTime.UtcNow;
                    _metrics.InstallDuration = _metrics.InstallEndTime - _metrics.InstallStartTime;
                    _metrics.ServicesRegistered = GetServiceRegistrations().Length;
                    _metrics.MemoryUsageAfter = GetCurrentMemoryUsage();
                }

                _isInstalled = true;
                _healthStatus.IsHealthy = true;
                _healthStatus.HealthMessage = "Installation completed successfully";
                _healthStatus.LastUpdateTime = DateTime.UtcNow;

                LogPhaseEnd("installation", _metrics.InstallDuration);
            }
            catch (Exception ex)
            {
                lock (_metricsLock)
                {
                    _metrics.ErrorCount++;
                }

                _logger?.LogException(ex, $"Installation failed for {InstallerName}");
                HandleInstallationFailure(ex, context);
                throw;
            }
        }

        /// <inheritdoc />
        public virtual void PostInstall(Container container, IBootstrapConfig config, IBootstrapContext context)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                LogPhaseStart("post-installation");

                lock (_metricsLock)
                {
                    _metrics.PostInstallStartTime = DateTime.UtcNow;
                }

                // Register health checks and configure monitoring
                RegisterHealthChecks(container, context);
                ConfigureAlerting(context);

                // Allow derived classes to implement custom post-installation logic
                OnPostInstall(container, config, context);

                lock (_metricsLock)
                {
                    _metrics.PostInstallEndTime = DateTime.UtcNow;
                    _metrics.PostInstallDuration = _metrics.PostInstallEndTime - _metrics.PostInstallStartTime;
                    _metrics.TotalInstallDuration = _metrics.PostInstallEndTime - _metrics.PreInstallStartTime;
                }

                LogPhaseEnd("post-installation", _metrics.PostInstallDuration);
            }
            catch (Exception ex)
            {
                lock (_metricsLock)
                {
                    _metrics.ErrorCount++;
                    _metrics.WarningCount++;
                }

                _logger?.LogException(ex, $"Post-installation failed for {InstallerName}");
                context.AlertService?.RaiseAlert(
                    message: $"Post-installation failed for {InstallerName}",
                    severity: AlertSeverity.Warning,
                    source: InstallerName,
                    tag: "PostInstallFailure"
                );
                throw;
            }
        }

        #endregion

        #region Private Lifecycle Methods

        /// <summary>
        /// Validates service registrations for this installer.
        /// </summary>
        private void ValidateServiceRegistrations(BootstrapValidationResult result)
        {
            try
            {
                var serviceRegistrations = GetServiceRegistrations();
                if (serviceRegistrations == null)
                {
                    result.Warnings.Add("GetServiceRegistrations returned null");
                    return;
                }

                foreach (var registration in serviceRegistrations)
                {
                    if (registration == null)
                    {
                        result.Errors.Add("Service registration cannot be null");
                        continue;
                    }

                    if (registration.ServiceType == null)
                    {
                        result.Errors.Add($"Service type cannot be null for registration: {registration.ServiceName}");
                    }

                    if (registration.ImplementationType == null)
                    {
                        result.Errors.Add($"Implementation type cannot be null for registration: {registration.ServiceName}");
                    }

                    if (registration.ServiceType != null && registration.ImplementationType != null)
                    {
                        if (!registration.ServiceType.IsAssignableFrom(registration.ImplementationType))
                        {
                            result.Errors.Add($"Implementation type {registration.ImplementationType.Name} does not implement service type {registration.ServiceType.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to validate service registrations: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers health checks for this installer.
        /// </summary>
        private void RegisterHealthChecks(Container container, IBootstrapContext context)
        {
            try
            {
                var healthCheck = CreateHealthCheck();
                context.HealthCheckService?.RegisterHealthCheck(healthCheck);
                _logger?.LogDebug($"Health check registered for {InstallerName}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register health check for {InstallerName}");
            }
        }

        /// <summary>
        /// Configures alerting for this installer.
        /// </summary>
        private void ConfigureAlerting(IBootstrapContext context)
        {
            try
            {
                // Configure installer-specific alerting rules
                OnConfigureAlerting(context);
                _logger?.LogDebug($"Alerting configured for {InstallerName}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to configure alerting for {InstallerName}");
            }
        }

        /// <summary>
        /// Called to configure alerting for this installer.
        /// Override in derived classes to setup custom alerting rules.
        /// </summary>
        protected virtual void OnConfigureAlerting(IBootstrapContext context) { }

        #endregion
    }
}