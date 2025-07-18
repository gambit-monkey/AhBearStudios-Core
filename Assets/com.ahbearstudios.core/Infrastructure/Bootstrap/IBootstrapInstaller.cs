using System;
using Reflex.Core;

namespace AhBearStudios.Core.Infrastructure.Bootstrap
{
    /// <summary>
    /// Bootstrap installer contract for Reflex DI that extends the standard IInstaller interface.
    /// Provides additional metadata and lifecycle hooks while maintaining Reflex compatibility.
    /// Follows AhBearStudios Core Development Guidelines for Unity Game Development First approach.
    /// </summary>
    public interface IBootstrapInstaller : IInstaller
    {
        /// <summary>
        /// Gets the human-readable name of this installer for identification and logging.
        /// Used for debugging, diagnostics, and dependency resolution tracking.
        /// </summary>
        string InstallerName { get; }

        /// <summary>
        /// Gets the installation priority for this installer.
        /// Lower values are installed first. Typical priorities:
        /// - Infrastructure systems: 0-100
        /// - Core services: 100-200  
        /// - Application services: 200-300
        /// - UI and presentation: 300+
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether this installer is enabled and should participate in the bootstrap process.
        /// Allows conditional installation based on configuration, platform, or runtime conditions.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the types of other installers that this installer depends on.
        /// Bootstrap system will ensure dependencies are installed before this installer.
        /// Returns empty array if no dependencies.
        /// </summary>
        Type[] Dependencies { get; }

        /// <summary>
        /// Validates that all dependencies and configurations are correct before installation begins.
        /// Performs comprehensive validation of installer state, dependencies, and configuration.
        /// Called before any installation starts to catch configuration errors early.
        /// No container access available during this phase.
        /// </summary>
        /// <returns>True if validation passes and installer is ready for installation, false otherwise</returns>
        bool ValidateInstaller();

        /// <summary>
        /// Pre-installation setup and preparation phase.
        /// Prepares the environment for service registration and installation.
        /// Called after validation but before InstallBindings().
        /// Use for directory creation, resource allocation, and other setup tasks.
        /// No container access available during this phase.
        /// </summary>
        void PreInstall();

        /// <summary>
        /// Post-installation configuration and system integration phase.
        /// Completes system integration after all services have been registered and container is built.
        /// Called after InstallBindings() completes successfully with access to the built container.
        /// Use for service initialization, health check registration, and cross-system integration.
        /// </summary>
        /// <param name="container">The built Reflex container for service resolution</param>
        void PostInstall(Container container);
    }
}