using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Factory interface for creating container adapters for different DI frameworks.
    /// </summary>
    public interface IContainerAdapterFactory
    {
        /// <summary>
        /// Gets the framework this factory supports.
        /// </summary>
        ContainerFramework SupportedFramework { get; }
        
        /// <summary>
        /// Gets whether this framework is available in the current environment.
        /// </summary>
        bool IsFrameworkAvailable { get; }
        
        /// <summary>
        /// Creates a new container adapter with the specified configuration.
        /// </summary>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter instance.</returns>
        IContainerAdapter CreateContainer(
            IDependencyInjectionConfig config,
            string containerName = null,
            IMessageBusService messageBusService = null);
        
        /// <summary>
        /// Creates a container adapter from an existing framework-specific builder.
        /// </summary>
        /// <param name="frameworkBuilder">The framework-specific builder object.</param>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter instance.</returns>
        IContainerAdapter CreateFromBuilder(
            object frameworkBuilder,
            IDependencyInjectionConfig config,
            string containerName = null,
            IMessageBusService messageBusService = null);
    }
}