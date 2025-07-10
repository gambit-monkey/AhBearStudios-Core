using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Models;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    /// <summary>
    /// Extension methods for BaseBootstrapInstaller to provide additional functionality.
    /// </summary>
    public static class BaseBootstrapInstallerExtensions
    {
        /// <summary>
        /// Determines if this installer has a specific dependency.
        /// </summary>
        public static bool HasDependency<T>(this BaseBootstrapInstaller installer) where T : IBootstrapInstaller
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Dependencies.Contains(typeof(T));
        }

        /// <summary>
        /// Determines if this installer has a specific dependency by type.
        /// </summary>
        public static bool HasDependency(this BaseBootstrapInstaller installer, Type dependencyType)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));
            if (dependencyType == null)
                throw new ArgumentNullException(nameof(dependencyType));

            return installer.Dependencies.Contains(dependencyType);
        }

        /// <summary>
        /// Gets the installer category as a string.
        /// </summary>
        public static string GetCategoryName(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category.ToString();
        }

        /// <summary>
        /// Determines if this installer is a core system installer.
        /// </summary>
        public static bool IsCoreInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Core;
        }

        /// <summary>
        /// Determines if this installer is an optional installer.
        /// </summary>
        public static bool IsOptionalInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Optional;
        }

        /// <summary>
        /// Determines if this installer is a development-only installer.
        /// </summary>
        public static bool IsDevelopmentInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Development;
        }

        /// <summary>
        /// Determines if this installer is a platform-specific installer.
        /// </summary>
        public static bool IsPlatformInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Platform;
        }

        /// <summary>
        /// Determines if this installer is an integration installer.
        /// </summary>
        public static bool IsIntegrationInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Integration;
        }

        /// <summary>
        /// Determines if this installer is a performance installer.
        /// </summary>
        public static bool IsPerformanceInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Performance;
        }

        /// <summary>
        /// Creates a summary string for logging and diagnostics.
        /// </summary>
        public static string GetSummary(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            var dependencyCount = installer.Dependencies.Length;
            var memoryMB = installer.EstimatedMemoryOverheadBytes / (1024.0 * 1024.0);

            return $"{installer.InstallerName} " +
                   $"[{installer.Category}] " +
                   $"(Priority: {installer.Priority}, " +
                   $"Dependencies: {dependencyCount}, " +
                   $"Memory: {memoryMB:F2}MB, " +
                   $"Enabled: {installer.IsEnabled})";
        }

        /// <summary>
        /// Validates that the installer configuration is correct.
        /// </summary>
        public static bool ValidateConfiguration(this BaseBootstrapInstaller installer, out string[] errors)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            var errorList = new List<string>();

            // Validate installer name
            if (string.IsNullOrWhiteSpace(installer.InstallerName))
            {
                errorList.Add("Installer name cannot be null or empty");
            }

            // Validate priority range
            if (installer.Priority < 0)
            {
                errorList.Add("Priority cannot be negative");
            }

            // Validate memory overhead
            if (installer.EstimatedMemoryOverheadBytes < 0)
            {
                errorList.Add("Estimated memory overhead cannot be negative");
            }

            // Validate dependencies
            if (installer.Dependencies != null)
            {
                foreach (var dependency in installer.Dependencies)
                {
                    if (dependency == null)
                    {
                        errorList.Add("Dependency types cannot be null");
                    }
                    else if (!typeof(IBootstrapInstaller).IsAssignableFrom(dependency))
                    {
                        errorList.Add($"Dependency {dependency.Name} must implement IBootstrapInstaller");
                    }
                }
            }

            // Validate category
            if (!Enum.IsDefined(typeof(SystemCategory), installer.Category))
            {
                errorList.Add($"Invalid category: {installer.Category}");
            }

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Gets the dependency names as a formatted string.
        /// </summary>
        public static string GetDependencyNames(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            if (installer.Dependencies == null || installer.Dependencies.Length == 0)
            {
                return "None";
            }

            return string.Join(", ", installer.Dependencies.Select(d => d.Name));
        }

        /// <summary>
        /// Determines if this installer can be installed in the current environment.
        /// </summary>
        public static bool CanInstall(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            if (!installer.IsEnabled)
                return false;

            var platformRequirements = installer.GetPlatformRequirements();
            if (platformRequirements?.SupportedPlatforms == null || platformRequirements.SupportedPlatforms.Length == 0)
                return true;

            var currentPlatform = UnityEngine.Application.platform;
            return Array.Exists(platformRequirements.SupportedPlatforms, p => p == currentPlatform);
        }

        /// <summary>
        /// Gets the estimated installation time in milliseconds.
        /// </summary>
        public static long GetEstimatedInstallationTimeMs(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            // Base estimation based on priority and dependencies
            long baseTime = 100; // 100ms base time
            long priorityFactor = installer.Priority * 10; // Higher priority = more time
            long dependencyFactor = installer.Dependencies.Length * 50; // Each dependency adds 50ms
            long memoryFactor = installer.EstimatedMemoryOverheadBytes / (1024 * 1024); // 1ms per MB

            // Category-specific factors
            long categoryFactor = installer.Category switch
            {
                SystemCategory.Core => 200,
                SystemCategory.Optional => 50,
                SystemCategory.Development => 25,
                SystemCategory.Platform => 150,
                SystemCategory.Integration => 300,
                SystemCategory.Performance => 100,
                _ => 100
            };

            return baseTime + priorityFactor + dependencyFactor + memoryFactor + categoryFactor;
        }

        /// <summary>
        /// Creates a detailed installer report for debugging.
        /// </summary>
        public static string CreateDetailedReport(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== Installer Report: {installer.InstallerName} ===");
            report.AppendLine($"Category: {installer.Category}");
            report.AppendLine($"Priority: {installer.Priority}");
            report.AppendLine($"Enabled: {installer.IsEnabled}");
            report.AppendLine($"Memory Overhead: {installer.EstimatedMemoryOverheadBytes / 1024.0:F2} KB");
            report.AppendLine($"Hot Reload Support: {installer.SupportsHotReload}");
            report.AppendLine($"Dependencies: {installer.GetDependencyNames()}");
            report.AppendLine($"Estimated Install Time: {installer.GetEstimatedInstallationTimeMs()}ms");
            report.AppendLine($"Can Install: {installer.CanInstall()}");

            try
            {
                var diagnostics = installer.GetDiagnostics();
                if (diagnostics != null)
                {
                    report.AppendLine($"Install Status: {(diagnostics.IsInstalled ? "Installed" : "Not Installed")}");
                    
                    if (diagnostics.Metrics != null)
                    {
                        report.AppendLine($"Services Registered: {diagnostics.Metrics.ServicesRegistered}");
                        report.AppendLine($"Error Count: {diagnostics.Metrics.ErrorCount}");
                        report.AppendLine($"Warning Count: {diagnostics.Metrics.WarningCount}");
                        
                        if (diagnostics.Metrics.TotalInstallDuration > TimeSpan.Zero)
                        {
                            report.AppendLine($"Total Install Duration: {diagnostics.Metrics.TotalInstallDuration.TotalMilliseconds:F2}ms");
                        }
                    }
                    
                    if (diagnostics.HealthStatus != null)
                    {
                        report.AppendLine($"Health Status: {(diagnostics.HealthStatus.IsHealthy ? "Healthy" : "Unhealthy")}");
                        report.AppendLine($"Health Message: {diagnostics.HealthStatus.HealthMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error generating diagnostics: {ex.Message}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Determines if this installer supports graceful degradation.
        /// </summary>
        public static bool SupportsGracefulDegradation(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == SystemCategory.Optional || 
                   installer.Category == SystemCategory.Performance ||
                   installer.Category == SystemCategory.Development;
        }

        /// <summary>
        /// Gets the installation phase order based on category and priority.
        /// </summary>
        public static int GetInstallationPhaseOrder(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            // Lower number = earlier installation
            var categoryOrder = installer.Category switch
            {
                SystemCategory.Core => 1000,
                SystemCategory.Platform => 2000,
                SystemCategory.Integration => 3000,
                SystemCategory.Performance => 4000,
                SystemCategory.Optional => 5000,
                SystemCategory.Development => 6000,
                _ => 9999
            };

            return categoryOrder + installer.Priority;
        }
    }
}