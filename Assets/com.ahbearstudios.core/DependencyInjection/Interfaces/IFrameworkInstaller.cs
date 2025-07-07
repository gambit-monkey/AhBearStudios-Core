using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for framework-specific installers that can configure containers
    /// for different DI frameworks based on configuration.
    /// </summary>
    public interface IFrameworkInstaller
    {
        /// <summary>
        /// Gets the framework this installer supports.
        /// </summary>
        ContainerFramework SupportedFramework { get; }
        
        /// <summary>
        /// Gets the installer name for identification and logging.
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
        /// Validates that this installer can be properly executed.
        /// </summary>
        /// <param name="config">The DI configuration to validate against.</param>
        /// <returns>True if the installer is valid and can be executed.</returns>
        bool ValidateInstaller(IDependencyInjectionConfig config);
        
        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// </summary>
        /// <param name="config">The DI configuration being used.</param>
        void PreInstall(IDependencyInjectionConfig config);
        
        /// <summary>
        /// Installs services into the container adapter.
        /// </summary>
        /// <param name="container">The container adapter to configure.</param>
        /// <param name="config">The DI configuration being used.</param>
        void Install(IContainerAdapter container, IDependencyInjectionConfig config);
        
        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// </summary>
        /// <param name="container">The configured container adapter.</param>
        /// <param name="config">The DI configuration being used.</param>
        void PostInstall(IContainerAdapter container, IDependencyInjectionConfig config);
    }
}