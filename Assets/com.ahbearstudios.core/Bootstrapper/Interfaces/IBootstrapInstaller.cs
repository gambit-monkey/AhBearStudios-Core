using System;
using VContainer.Unity;

namespace AhBearStudios.Core.Bootstrap.Interfaces
{
    /// <summary>
    /// Interface for bootstrap installers that can install dependencies into a DI container.
    /// Extends the basic installer with bootstrap-specific functionality.
    /// </summary>
    public interface IBootstrapInstaller : IInstaller
    {
        /// <summary>
        /// Gets the name of this installer for identification and logging purposes.
        /// </summary>
        string InstallerName { get; }
        
        /// <summary>
        /// Gets the priority of this installer. Lower values install first.
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Gets whether this installer is enabled and should be processed.
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Gets the dependencies required by this installer.
        /// These installers must be processed before this one.
        /// </summary>
        Type[] Dependencies { get; }
        
        /// <summary>
        /// Validates that this installer can be properly installed.
        /// </summary>
        /// <returns>True if the installer is valid and can be installed.</returns>
        bool ValidateInstaller();
        
        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// </summary>
        void PreInstall();
        
        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// </summary>
        void PostInstall();
    }
}