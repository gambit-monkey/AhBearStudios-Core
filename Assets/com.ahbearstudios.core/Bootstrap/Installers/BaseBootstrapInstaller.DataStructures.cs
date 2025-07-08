using System;
using System.Collections.Generic;
using AhBearStudios.Core.Bootstrap.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Data Structures

        /// <summary>
        /// System categories for organizing installers.
        /// </summary>
        public enum SystemCategory
        {
            Core = 0,
            Infrastructure = 1,
            Framework = 2,
            Game = 3,
            Development = 4,
            Platform = 5
        }

        /// <summary>
        /// Bootstrap validation result containing success status and detailed information.
        /// </summary>
        public class BootstrapValidationResult
        {
            public bool IsValid { get; set; }
            public string InstallerName { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public TimeSpan ValidationDuration { get; set; }
        }

        /// <summary>
        /// Installation metrics for performance monitoring and analysis.
        /// </summary>
        public class InstallationMetrics
        {
            public string InstallerName { get; set; }
            public DateTime PreInstallStartTime { get; set; }
            public DateTime PreInstallEndTime { get; set; }
            public TimeSpan PreInstallDuration { get; set; }
            public DateTime InstallStartTime { get; set; }
            public DateTime InstallEndTime { get; set; }
            public TimeSpan InstallDuration { get; set; }
            public DateTime PostInstallStartTime { get; set; }
            public DateTime PostInstallEndTime { get; set; }
            public TimeSpan PostInstallDuration { get; set; }
            public TimeSpan TotalInstallDuration { get; set; }
            public int ServicesRegistered { get; set; }
            public long MemoryUsageBefore { get; set; }
            public long MemoryUsageAfter { get; set; }
            public int ErrorCount { get; set; }
            public int WarningCount { get; set; }
        }

        /// <summary>
        /// System health status information.
        /// </summary>
        public class SystemHealthStatus
        {
            public string InstallerName { get; set; }
            public bool IsHealthy { get; set; }
            public DateTime LastUpdateTime { get; set; }
            public string HealthMessage { get; set; }
            public ServiceRegistrationInfo[] RegisteredServices { get; set; }
            public Dictionary<string, bool> DependencyStatus { get; set; }
        }

        /// <summary>
        /// Installer diagnostic information for troubleshooting.
        /// </summary>
        public class InstallerDiagnostics
        {
            public string InstallerName { get; set; }
            public SystemCategory Category { get; set; }
            public int Priority { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsInstalled { get; set; }
            public Type[] Dependencies { get; set; }
            public ServiceRegistrationInfo[] ServiceRegistrations { get; set; }
            public ConfigurationRequirements ConfigurationRequirements { get; set; }
            public PlatformRequirements PlatformRequirements { get; set; }
            public InstallationMetrics Metrics { get; set; }
            public SystemHealthStatus HealthStatus { get; set; }
        }

        /// <summary>
        /// Configuration requirements for installer validation.
        /// </summary>
        public class ConfigurationRequirements
        {
            public string InstallerName { get; set; }
            public string[] RequiredSections { get; set; }
            public string[] OptionalSections { get; set; }
            public ConfigurationValidationRule[] ValidationRules { get; set; }
        }

        /// <summary>
        /// Configuration validation rule definition.
        /// </summary>
        public class ConfigurationValidationRule
        {
            public string RuleName { get; set; }
            public string Description { get; set; }
            public Func<IBootstrapConfig, bool> ValidationFunction { get; set; }
        }

        /// <summary>
        /// Service registration information for dependency analysis.
        /// </summary>
        public class ServiceRegistrationInfo
        {
            public string ServiceName { get; set; }
            public Type ServiceType { get; set; }
            public Type ImplementationType { get; set; }
            public string Lifecycle { get; set; }
            public bool IsRequired { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// Platform requirements and constraints.
        /// </summary>
        public class PlatformRequirements
        {
            public string InstallerName { get; set; }
            public UnityEngine.RuntimePlatform[] SupportedPlatforms { get; set; }
            public string[] RequiredFeatures { get; set; }
            public long MinimumMemoryMB { get; set; }
            public string RequiredUnityVersion { get; set; }
        }

        /// <summary>
        /// Recovery options for failed installations.
        /// </summary>
        public class RecoveryOptions
        {
            public string InstallerName { get; set; }
            public RecoveryOption[] AvailableOptions { get; set; }
            public RecoveryOption DefaultOption { get; set; }
            public bool SupportsGracefulDegradation { get; set; }
            public bool RequiresManualIntervention { get; set; }
        }

        /// <summary>
        /// Individual recovery option definition.
        /// </summary>
        public class RecoveryOption
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsDefault { get; set; }
            public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        }

        /// <summary>
        /// Result of a recovery attempt.
        /// </summary>
        public class RecoveryResult
        {
            public bool IsSuccessful { get; set; }
            public string ErrorMessage { get; set; }
            public RecoveryOption RecoveryOption { get; set; }
            public Dictionary<string, object> RecoveryData { get; set; } = new Dictionary<string, object>();
        }

        #endregion
    }
}