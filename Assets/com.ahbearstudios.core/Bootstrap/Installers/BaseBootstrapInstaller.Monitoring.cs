using System;
using System.Collections.Generic;
using AhBearStudios.Core.Bootstrap.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Monitoring Implementation

        /// <inheritdoc />
        public virtual InstallationMetrics GetInstallationMetrics()
        {
            lock (_metricsLock)
            {
                return new InstallationMetrics
                {
                    InstallerName = InstallerName,
                    PreInstallStartTime = _metrics.PreInstallStartTime,
                    PreInstallEndTime = _metrics.PreInstallEndTime,
                    PreInstallDuration = _metrics.PreInstallDuration,
                    InstallStartTime = _metrics.InstallStartTime,
                    InstallEndTime = _metrics.InstallEndTime,
                    InstallDuration = _metrics.InstallDuration,
                    PostInstallStartTime = _metrics.PostInstallStartTime,
                    PostInstallEndTime = _metrics.PostInstallEndTime,
                    PostInstallDuration = _metrics.PostInstallDuration,
                    TotalInstallDuration = _metrics.TotalInstallDuration,
                    ServicesRegistered = _metrics.ServicesRegistered,
                    MemoryUsageBefore = _metrics.MemoryUsageBefore,
                    MemoryUsageAfter = _metrics.MemoryUsageAfter,
                    ErrorCount = _metrics.ErrorCount,
                    WarningCount = _metrics.WarningCount
                };
            }
        }

        /// <inheritdoc />
        public virtual SystemHealthStatus GetHealthStatus()
        {
            return new SystemHealthStatus
            {
                InstallerName = InstallerName,
                IsHealthy = _healthStatus.IsHealthy,
                LastUpdateTime = _healthStatus.LastUpdateTime,
                HealthMessage = _healthStatus.HealthMessage,
                RegisteredServices = GetServiceRegistrations(),
                DependencyStatus = GetDependencyStatus()
            };
        }

        /// <inheritdoc />
        public virtual InstallerDiagnostics GetDiagnostics()
        {
            return new InstallerDiagnostics
            {
                InstallerName = InstallerName,
                Category = Category,
                Priority = Priority,
                IsEnabled = IsEnabled,
                IsInstalled = _isInstalled,
                Dependencies = Dependencies,
                ServiceRegistrations = GetServiceRegistrations(),
                ConfigurationRequirements = GetConfigurationRequirements(),
                PlatformRequirements = GetPlatformRequirements(),
                Metrics = GetInstallationMetrics(),
                HealthStatus = GetHealthStatus()
            };
        }

        /// <inheritdoc />
        public virtual void ResetMetrics()
        {
            lock (_metricsLock)
            {
                InitializeMetrics();
            }
        }

        #endregion

        #region Private Monitoring Methods

        private void InitializeMetrics()
        {
            _metrics = new InstallationMetrics
            {
                InstallerName = InstallerName,
                ServicesRegistered = 0,
                ErrorCount = 0,
                WarningCount = 0
            };
        }

        private void InitializeHealthStatus()
        {
            _healthStatus = new SystemHealthStatus
            {
                InstallerName = InstallerName,
                IsHealthy = false,
                LastUpdateTime = DateTime.UtcNow,
                HealthMessage = "Not installed"
            };
        }

        private Dictionary<string, bool> GetDependencyStatus()
        {
            var status = new Dictionary<string, bool>();
            foreach (var dependency in Dependencies)
            {
                // In a real implementation, you would check if the dependency is properly installed
                status[dependency.Name] = true; // Simplified for this example
            }
            return status;
        }

        #endregion
    }
}