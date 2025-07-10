using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        /// <summary>
        /// Default health check implementation for bootstrap installers.
        /// Monitors the installation status and health of the installer.
        /// </summary>
        private class BaseInstallerHealthCheck : IHealthCheck
        {
            private readonly BaseBootstrapInstaller _installer;

            public FixedString64Bytes Name { get; }

            public BaseInstallerHealthCheck(BaseBootstrapInstaller installer)
            {
                _installer = installer ?? throw new ArgumentNullException(nameof(installer));
                var name = $"{installer.InstallerName}Health";
                Name = new FixedString64Bytes(name.Length > 64 ? name.Substring(0, 64) : name);
            }

            public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    var healthStatus = _installer.GetHealthStatus();
                    
                    if (!_installer._isInstalled)
                    {
                        return HealthCheckResult.Unhealthy(
                            $"Installer {_installer.InstallerName} is not installed");
                    }

                    if (!healthStatus.IsHealthy)
                    {
                        return HealthCheckResult.Unhealthy(
                            $"Installer {_installer.InstallerName} is unhealthy: {healthStatus.HealthMessage}");
                    }

                    // Check if dependencies are healthy
                    if (healthStatus.DependencyStatus != null)
                    {
                        foreach (var dependency in healthStatus.DependencyStatus)
                        {
                            if (!dependency.Value)
                            {
                                return HealthCheckResult.Degraded(
                                    $"Dependency {dependency.Key} is unhealthy for installer {_installer.InstallerName}");
                            }
                        }
                    }

                    var metrics = _installer.GetInstallationMetrics();
                    var healthData = new Dictionary<string, object>
                    {
                        ["IsInstalled"] = _installer._isInstalled,
                        ["InstallDuration"] = metrics.InstallDuration.TotalMilliseconds,
                        ["ServicesRegistered"] = metrics.ServicesRegistered,
                        ["ErrorCount"] = metrics.ErrorCount,
                        ["WarningCount"] = metrics.WarningCount,
                        ["LastUpdateTime"] = healthStatus.LastUpdateTime,
                        ["Category"] = _installer.Category.ToString(),
                        ["Priority"] = _installer.Priority,
                        ["MemoryUsage"] = metrics.MemoryUsageAfter - metrics.MemoryUsageBefore
                    };

                    return HealthCheckResult.Healthy(
                        $"Installer {_installer.InstallerName} is healthy", 
                        healthData);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Health check failed for installer {_installer.InstallerName}: {ex.Message}",
                        ex);
                }
            }
        }

        /// <summary>
        /// Creates a health check for this installer.
        /// </summary>
        protected virtual IHealthCheck CreateHealthCheck()
        {
            return new BaseInstallerHealthCheck(this);
        }
    }
}