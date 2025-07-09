using System;
using System.Collections.Generic;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Models;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Data Structures

        /// <summary>
        /// Bootstrap validation result containing success status and detailed information.
        /// </summary>
        public class BootstrapValidationResult
        {
            public bool IsValid { get; set; } = true;
            public string InstallerName { get; set; } = string.Empty;
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public TimeSpan ValidationDuration { get; set; } = TimeSpan.Zero;
        }

        /// <summary>
        /// Installation metrics for performance monitoring and analysis.
        /// </summary>
        public class InstallationMetrics
        {
            public string InstallerName { get; set; } = string.Empty;
            public DateTime PreInstallStartTime { get; set; } = DateTime.MinValue;
            public DateTime PreInstallEndTime { get; set; } = DateTime.MinValue;
            public TimeSpan PreInstallDuration { get; set; } = TimeSpan.Zero;
            public DateTime InstallStartTime { get; set; } = DateTime.MinValue;
            public DateTime InstallEndTime { get; set; } = DateTime.MinValue;
            public TimeSpan InstallDuration { get; set; } = TimeSpan.Zero;
            public DateTime PostInstallStartTime { get; set; } = DateTime.MinValue;
            public DateTime PostInstallEndTime { get; set; } = DateTime.MinValue;
            public TimeSpan PostInstallDuration { get; set; } = TimeSpan.Zero;
            public TimeSpan TotalInstallDuration { get; set; } = TimeSpan.Zero;
            public int ServicesRegistered { get; set; } = 0;
            public long MemoryUsageBefore { get; set; } = 0;
            public long MemoryUsageAfter { get; set; } = 0;
            public int ErrorCount { get; set; } = 0;
            public int WarningCount { get; set; } = 0;
        }

        /// <summary>
        /// System health status information.
        /// </summary>
        public class SystemHealthStatus
        {
            public string InstallerName { get; set; } = string.Empty;
            public bool IsHealthy { get; set; } = false;
            public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
            public string HealthMessage { get; set; } = "Not installed";
            public ServiceRegistrationInfo[] RegisteredServices { get; set; } = Array.Empty<ServiceRegistrationInfo>();
            public Dictionary<string, bool> DependencyStatus { get; set; } = new Dictionary<string, bool>();
        }

        /// <summary>
        /// Installer diagnostic information for troubleshooting.
        /// </summary>
        public class InstallerDiagnostics
        {
            public string InstallerName { get; set; } = string.Empty;
            public SystemCategory Category { get; set; } = SystemCategory.Core;
            public int Priority { get; set; } = 0;
            public bool IsEnabled { get; set; } = false;
            public bool IsInstalled { get; set; } = false;
            public Type[] Dependencies { get; set; } = Array.Empty<Type>();
            public ServiceRegistrationInfo[] ServiceRegistrations { get; set; } = Array.Empty<ServiceRegistrationInfo>();
            public ConfigurationRequirements ConfigurationRequirements { get; set; } = new ConfigurationRequirements();
            public PlatformRequirements PlatformRequirements { get; set; } = new PlatformRequirements();
            public InstallationMetrics Metrics { get; set; } = new InstallationMetrics();
            public SystemHealthStatus HealthStatus { get; set; } = new SystemHealthStatus();
        }

        /// <summary>
        /// Configuration requirements for installer validation.
        /// </summary>
        public class ConfigurationRequirements
        {
            public string InstallerName { get; set; } = string.Empty;
            public string[] RequiredSections { get; set; } = Array.Empty<string>();
            public string[] OptionalSections { get; set; } = Array.Empty<string>();
            public ConfigurationValidationRule[] ValidationRules { get; set; } = Array.Empty<ConfigurationValidationRule>();
        }

        /// <summary>
        /// Configuration validation rule definition.
        /// </summary>
        public class ConfigurationValidationRule
        {
            public string RuleName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Func<IBootstrapConfig, bool> ValidationFunction { get; set; } = _ => true;
        }

        /// <summary>
        /// Service registration information for dependency analysis.
        /// </summary>
        public class ServiceRegistrationInfo
        {
            public string ServiceName { get; set; } = string.Empty;
            public Type ServiceType { get; set; } = typeof(object);
            public Type ImplementationType { get; set; } = typeof(object);
            public string Lifecycle { get; set; } = "Transient";
            public bool IsRequired { get; set; } = true;
            public string Description { get; set; } = string.Empty;
        }

        /// <summary>
        /// Platform requirements and constraints.
        /// </summary>
        public class PlatformRequirements
        {
            public string InstallerName { get; set; } = string.Empty;
            public UnityEngine.RuntimePlatform[] SupportedPlatforms { get; set; } = Array.Empty<UnityEngine.RuntimePlatform>();
            public string[] RequiredFeatures { get; set; } = Array.Empty<string>();
            public long MinimumMemoryMB { get; set; } = 0;
            public string RequiredUnityVersion { get; set; } = "2022.3.0f1";
        }

        /// <summary>
        /// Recovery options for failed installations.
        /// </summary>
        public class RecoveryOptions
        {
            public string InstallerName { get; set; } = string.Empty;
            public RecoveryOption[] AvailableOptions { get; set; } = Array.Empty<RecoveryOption>();
            public RecoveryOption DefaultOption { get; set; } = new RecoveryOption();
            public bool SupportsGracefulDegradation { get; set; } = false;
            public bool RequiresManualIntervention { get; set; } = false;
        }

        /// <summary>
        /// Individual recovery option.
        /// </summary>
        public class RecoveryOption
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsDefault { get; set; } = false;
        }

        /// <summary>
        /// Result of a recovery attempt.
        /// </summary>
        public class RecoveryResult
        {
            public bool IsSuccessful { get; set; } = false;
            public string ErrorMessage { get; set; } = string.Empty;
            public RecoveryOption RecoveryOption { get; set; } = new RecoveryOption();
        }

        #endregion
    }
}