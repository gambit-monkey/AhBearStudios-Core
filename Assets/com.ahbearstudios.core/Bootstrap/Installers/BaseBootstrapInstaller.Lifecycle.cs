using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerts;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.DependencyInjection.Bootstrap;
using AhBearStudios.Core.Alerts.Interfaces;
using Reflex.Core;
using Unity.Collections;

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

                // Allow derived classes to add custom validation
                OnValidateInstaller(config, result);

                return result;
            }
            catch (Exception ex)
            {
                return new BootstrapValidationResult
                {
                    IsValid = false,
                    InstallerName = InstallerName,
                    Errors = new List<string> { $"Validation failed with exception: {ex.Message}" },
                    Warnings = new List<string>()
                };
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

                _logger.LogInfo($"Starting pre-installation for {InstallerName} (CorrelationId: {_correlationId})");

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

                _logger.LogInfo($"Pre-installation completed for {InstallerName} in {_metrics.PreInstallDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
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

                _logger.LogInfo($"Starting installation for {InstallerName}");

                lock (_metricsLock)
                {
                    _metrics.InstallStartTime = DateTime.UtcNow;
                }

                // Execute the actual installation logic
                OnInstall(container, config, context);

                lock (_metricsLock)
                {
                    _metrics.InstallEndTime = DateTime.UtcNow;
                    _metrics.InstallDuration = _metrics.InstallEndTime - _metrics.InstallStartTime;
                    _metrics.ServicesRegistered = GetServiceRegistrations().Length;
                }

                _isInstalled = true;
                _healthStatus.IsHealthy = true;
                _healthStatus.LastUpdateTime = DateTime.UtcNow;

                _logger.LogInfo($"Installation completed for {InstallerName} in {_metrics.InstallDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
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
                _logger.LogInfo($"Starting post-installation for {InstallerName}");

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

                _logger.LogInfo($"Post-installation completed for {InstallerName} in {_metrics.PostInstallDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Post-installation failed for {InstallerName}");
                context.AlertService?.RaiseAlert(
                    message: $"Post-installation failed for {InstallerName}",
                    severity: AlertSeverity.Critical,
                    source: InstallerName,
                    tag: "PostInstallFailure"
                );
                throw;
            }
        }

        /// <inheritdoc />
        public virtual void Uninstall(Container container, IBootstrapContext context)
        {
            if (!SupportsHotReload)
                return;

            try
            {
                _logger?.LogInfo($"Starting uninstallation for {InstallerName}");

                // Allow derived classes to implement custom uninstallation logic
                OnUninstall(container, context);

                _isInstalled = false;
                _healthStatus.IsHealthy = false;
                _healthStatus.LastUpdateTime = DateTime.UtcNow;

                _logger?.LogInfo($"Uninstallation completed for {InstallerName}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Uninstallation failed for {InstallerName}");
                // Don't rethrow during uninstall - log and continue
            }
        }

        #endregion

        #region Abstract Methods for Derived Classes

        /// <summary>
        /// Called during validation to allow derived classes to add custom validation logic.
        /// </summary>
        protected virtual void OnValidateInstaller(IBootstrapConfig config, BootstrapValidationResult result) { }

        /// <summary>
        /// Called during pre-installation to allow derived classes to implement custom logic.
        /// </summary>
        protected virtual void OnPreInstall(IBootstrapConfig config, IBootstrapContext context) { }

        /// <summary>
        /// Called during installation to register services with the container.
        /// Derived classes must implement this method.
        /// </summary>
        protected abstract void OnInstall(Container container, IBootstrapConfig config, IBootstrapContext context);

        /// <summary>
        /// Called during post-installation to allow derived classes to implement custom logic.
        /// </summary>
        protected virtual void OnPostInstall(Container container, IBootstrapConfig config, IBootstrapContext context) { }

        /// <summary>
        /// Called during uninstallation to allow derived classes to implement custom cleanup logic.
        /// </summary>
        protected virtual void OnUninstall(Container container, IBootstrapContext context) { }

        #endregion
    }
}