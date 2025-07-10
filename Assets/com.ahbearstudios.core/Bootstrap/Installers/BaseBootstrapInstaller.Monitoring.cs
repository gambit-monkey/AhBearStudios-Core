using System;
using System.Collections.Generic;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Models;

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
                WarningCount = 0,
                PreInstallStartTime = DateTime.MinValue,
                PreInstallEndTime = DateTime.MinValue,
                InstallStartTime = DateTime.MinValue,
                InstallEndTime = DateTime.MinValue,
                PostInstallStartTime = DateTime.MinValue,
                PostInstallEndTime = DateTime.MinValue,
                PreInstallDuration = TimeSpan.Zero,
                InstallDuration = TimeSpan.Zero,
                PostInstallDuration = TimeSpan.Zero,
                TotalInstallDuration = TimeSpan.Zero,
                MemoryUsageBefore = 0,
                MemoryUsageAfter = 0
            };
        }

        private void InitializeHealthStatus()
        {
            _healthStatus = new SystemHealthStatus
            {
                InstallerName = InstallerName,
                IsHealthy = false,
                LastUpdateTime = DateTime.UtcNow,
                HealthMessage = "Not installed",
                RegisteredServices = Array.Empty<ServiceRegistrationInfo>(),
                DependencyStatus = new Dictionary<string, bool>()
            };
        }

        private Dictionary<string, bool> GetDependencyStatus()
        {
            var status = new Dictionary<string, bool>();
            
            if (Dependencies != null)
            {
                foreach (var dependency in Dependencies)
                {
                    if (dependency == null)
                    {
                        status[$"null_dependency_{Guid.NewGuid()}"] = false;
                        continue;
                    }

                    // In a real implementation, you would check if the dependency is properly installed
                    // For now, we'll mark all dependencies as healthy if they're defined
                    status[dependency.Name] = true;
                }
            }
            
            return status;
        }

        /// <summary>
        /// Updates the health status of this installer.
        /// </summary>
        protected void UpdateHealthStatus(bool isHealthy, string message = null)
        {
            _healthStatus.IsHealthy = isHealthy;
            _healthStatus.HealthMessage = message ?? (isHealthy ? "Healthy" : "Unhealthy");
            _healthStatus.LastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Increments the error count in metrics.
        /// </summary>
        protected void IncrementErrorCount()
        {
            lock (_metricsLock)
            {
                _metrics.ErrorCount++;
            }
        }

        /// <summary>
        /// Increments the warning count in metrics.
        /// </summary>
        protected void IncrementWarningCount()
        {
            lock (_metricsLock)
            {
                _metrics.WarningCount++;
            }
        }

        /// <summary>
        /// Records the number of services registered by this installer.
        /// </summary>
        protected void RecordServicesRegistered(int count)
        {
            lock (_metricsLock)
            {
                _metrics.ServicesRegistered = count;
            }
        }

        #endregion
    }
}