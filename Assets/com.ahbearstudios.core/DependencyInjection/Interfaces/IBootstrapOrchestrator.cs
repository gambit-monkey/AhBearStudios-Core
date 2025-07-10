using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Bootstrap;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for orchestrating the bootstrap process across multiple installers.
    /// Handles dependency ordering, validation, and error recovery.
    /// </summary>
    public interface IBootstrapOrchestrator
    {
        /// <summary>
        /// Registers a framework installer.
        /// </summary>
        /// <param name="installer">The installer to register.</param>
        void RegisterInstaller(IFrameworkInstaller installer);
        
        /// <summary>
        /// Registers multiple framework installers.
        /// </summary>
        /// <param name="installers">The installers to register.</param>
        void RegisterInstallers(params IFrameworkInstaller[] installers);
        
        /// <summary>
        /// Executes the bootstrap process for a container.
        /// </summary>
        /// <param name="container">The container to bootstrap.</param>
        /// <param name="config">The DI configuration to use.</param>
        /// <returns>Bootstrap result with success/failure information.</returns>
        BootstrapResult Execute(IContainerAdapter container, IDependencyInjectionConfig config);
        
        /// <summary>
        /// Validates all registered installers for the given configuration.
        /// </summary>
        /// <param name="config">The DI configuration to validate against.</param>
        /// <returns>Validation result with detailed information.</returns>
        BootstrapValidationResult Validate(IDependencyInjectionConfig config);
        
        /// <summary>
        /// Gets all registered installers ordered by priority and dependencies.
        /// </summary>
        /// <param name="framework">Optional framework filter.</param>
        /// <returns>Ordered list of installers.</returns>
        IReadOnlyList<IFrameworkInstaller> GetOrderedInstallers(ContainerFramework? framework = null);
        
        /// <summary>
        /// Clears all registered installers.
        /// </summary>
        void Clear();
    }
}