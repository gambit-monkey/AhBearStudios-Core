using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.DependencyInjection.Bootstrap;

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

            return installer.Category == BaseBootstrapInstaller.SystemCategory.Core;
        }

        /// <summary>
        /// Determines if this installer is a development-only installer.
        /// </summary>
        public static bool IsDevelopmentInstaller(this BaseBootstrapInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));

            return installer.Category == BaseBootstrapInstaller.SystemCategory.Development;
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
    }

    /// <summary>
    /// Helper methods for working with installer collections.
    /// </summary>
    public static class InstallerCollectionExtensions
    {
        /// <summary>
        /// Orders installers by priority and dependency constraints.
        /// </summary>
        public static IOrderedEnumerable<T> OrderForInstallation<T>(this IEnumerable<T> installers) 
            where T : IBootstrapInstaller
        {
            if (installers == null)
                throw new ArgumentNullException(nameof(installers));

            return installers
                .Where(i => i.IsEnabled)
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.InstallerName);
        }

        /// <summary>
        /// Filters installers by category.
        /// </summary>
        public static IEnumerable<T> FilterByCategory<T>(this IEnumerable<T> installers, 
            BaseBootstrapInstaller.SystemCategory category) where T : BaseBootstrapInstaller
        {
            if (installers == null)
                throw new ArgumentNullException(nameof(installers));

            return installers.Where(i => i.Category == category);
        }

        /// <summary>
        /// Gets only enabled installers.
        /// </summary>
        public static IEnumerable<T> GetEnabled<T>(this IEnumerable<T> installers) where T : IBootstrapInstaller
        {
            if (installers == null)
                throw new ArgumentNullException(nameof(installers));

            return installers.Where(i => i.IsEnabled);
        }
    }
}