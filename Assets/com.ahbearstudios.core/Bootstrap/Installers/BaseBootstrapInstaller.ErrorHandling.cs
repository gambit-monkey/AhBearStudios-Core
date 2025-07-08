using System;
using System.Collections.Generic;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.DependencyInjection.Bootstrap;
using AhBearStudios.Core.Alerts.Interfaces;
using Reflex.Core;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Error Handling Implementation

        /// <inheritdoc />
        public virtual void HandleInstallationFailure(Exception exception, IBootstrapContext context)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                lock (_metricsLock)
                {
                    _metrics.ErrorCount++;
                }

                _healthStatus.IsHealthy = false;
                _healthStatus.HealthMessage = $"Installation failed: {exception.Message}";
                _healthStatus.LastUpdateTime = DateTime.UtcNow;

                _logger?.LogException(exception, $"Installation failure in {InstallerName}");

                context.AlertService?.RaiseAlert(
                    message: $"Installation failed for {InstallerName}: {exception.Message}",
                    severity: AlertSeverity.Critical,
                    source: InstallerName,
                    tag: "InstallationFailure"
                );

                // Allow derived classes to handle failures
                OnHandleInstallationFailure(exception, context);

                // Attempt cleanup
                try
                {
                    CleanupFailedInstallation(context);
                }
                catch (Exception cleanupEx)
                {
                    _logger?.LogException(cleanupEx, $"Cleanup failed after installation failure in {InstallerName}");
                }
            }
            catch (Exception handlingEx)
            {
                // Log but don't throw - we're already in a failure state
                _logger?.LogException(handlingEx, $"Error handling installation failure in {InstallerName}");
            }
        }

        /// <inheritdoc />
        public virtual RecoveryOptions GetRecoveryOptions()
        {
            return new RecoveryOptions
            {
                InstallerName = InstallerName,
                AvailableOptions = GetAvailableRecoveryOptions(),
                DefaultOption = GetDefaultRecoveryOption(),
                SupportsGracefulDegradation = SupportsGracefulDegradation(),
                RequiresManualIntervention = RequiresManualIntervention()
            };
        }

        /// <inheritdoc />
        public virtual RecoveryResult AttemptRecovery(RecoveryOption option, Container container, 
            IBootstrapConfig config, IBootstrapContext context)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                _logger?.LogInfo($"Attempting recovery for {InstallerName} using option: {option.Name}");

                var result = OnAttemptRecovery(option, container, config, context);

                if (result.IsSuccessful)
                {
                    _healthStatus.IsHealthy = true;
                    _healthStatus.HealthMessage = $"Recovered using option: {option.Name}";
                    _healthStatus.LastUpdateTime = DateTime.UtcNow;
                    _isInstalled = true;

                    _logger?.LogInfo($"Recovery successful for {InstallerName}");
                }
                else
                {
                    _logger?.LogWarning($"Recovery failed for {InstallerName}: {result.ErrorMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Recovery attempt failed for {InstallerName}");
                return new RecoveryResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    RecoveryOption = option
                };
            }
        }

        #endregion

        #region Protected Error Handling Methods

        /// <summary>
        /// Called when installation fails to allow derived classes to implement custom failure handling.
        /// </summary>
        protected virtual void OnHandleInstallationFailure(Exception exception, IBootstrapContext context) { }

        /// <summary>
        /// Gets the recovery options available for this installer.
        /// Override in derived classes to provide custom recovery options.
        /// </summary>
        protected virtual RecoveryOption[] GetAvailableRecoveryOptions()
        {
            return new[]
            {
                new RecoveryOption
                {
                    Name = "Retry",
                    Description = "Retry the installation with the same configuration",
                    IsDefault = true
                },
                new RecoveryOption
                {
                    Name = "Minimal",
                    Description = "Install with minimal functionality",
                    IsDefault = false
                }
            };
        }

        /// <summary>
        /// Gets the default recovery option for this installer.
        /// Override in derived classes to specify a different default.
        /// </summary>
        protected virtual RecoveryOption GetDefaultRecoveryOption()
        {
            var options = GetAvailableRecoveryOptions();
            return Array.Find(options, o => o.IsDefault) ?? options[0];
        }

        /// <summary>
        /// Gets whether this installer supports graceful degradation.
        /// Override in derived classes to specify degradation support.
        /// </summary>
        protected virtual bool SupportsGracefulDegradation()
        {
            return false;
        }

        /// <summary>
        /// Gets whether recovery requires manual intervention.
        /// Override in derived classes to specify intervention requirements.
        /// </summary>
        protected virtual bool RequiresManualIntervention()
        {
            return false;
        }

        /// <summary>
        /// Attempts recovery using the specified option.
        /// Override in derived classes to implement custom recovery logic.
        /// </summary>
        protected virtual RecoveryResult OnAttemptRecovery(RecoveryOption option, Container container, 
            IBootstrapConfig config, IBootstrapContext context)
        {
            // Default implementation just retries the installation
            try
            {
                OnInstall(container, config, context);
                return new RecoveryResult
                {
                    IsSuccessful = true,
                    RecoveryOption = option
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    RecoveryOption = option
                };
            }
        }

        /// <summary>
        /// Cleans up resources after a failed installation.
        /// Override in derived classes to implement custom cleanup logic.
        /// </summary>
        protected virtual void CleanupFailedInstallation(IBootstrapContext context)
        {
            // Default implementation does nothing
        }

        #endregion
    }
}