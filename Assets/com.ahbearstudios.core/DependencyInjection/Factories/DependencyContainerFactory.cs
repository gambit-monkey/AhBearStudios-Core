using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Builders;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses;
using AhBearStudios.Core.MessageBus.Unity;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Factory for creating dependency injection containers.
    /// Provides abstraction over different DI container implementations with proper lifecycle management.
    /// </summary>
    public static class DependencyContainerFactory
    {
        /// <summary>
        /// The current container implementation being used.
        /// </summary>
        public static ContainerImplementation CurrentImplementation { get; private set; } = ContainerImplementation.VContainer;

        /// <summary>
        /// The default message bus instance to use for containers.
        /// </summary>
        private static IMessageBusService _defaultMessageBusService;

        /// <summary>
        /// Gets or sets the default message bus used by containers.
        /// If not set, containers will resolve MessageBusService from themselves or create a fallback.
        /// </summary>
        public static IMessageBusService DefaultMessageBusService
        {
            get => _defaultMessageBusService;
            set => _defaultMessageBusService = value;
        }

        /// <summary>
        /// Creates a new dependency container using the current implementation.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new dependency container instance.</returns>
        public static IDependencyContainer Create(string containerName = null)
        {
            return CurrentImplementation switch
            {
                ContainerImplementation.VContainer => CreateVContainer(containerName),
                _ => throw new NotSupportedException($"Container implementation '{CurrentImplementation}' is not supported")
            };
        }

        /// <summary>
        /// Creates a new VContainer-based dependency container.
        /// MessageBusService will be resolved from the container or use default if available.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new VContainer-based dependency container.</returns>
        public static IDependencyContainer CreateVContainer(string containerName = null)
        {
            var builder = new ContainerBuilder();
            
            // Register default MessageBusService if available
            if (_defaultMessageBusService != null)
            {
                VContainer.ContainerBuilderExtensions.RegisterInstance(builder, _defaultMessageBusService).As<IMessageBusService>();
            }
            else
            {
                // Register a MessageBusService that will be resolved later
                RegisterDefaultMessageBus(builder);
            }
            
            return new VContainerAdapter(builder, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new dependency container wrapping the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IDependencyContainer FromVContainerBuilder(IContainerBuilder builder, string containerName = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            
            // Ensure MessageBusService is registered
            EnsureMessageBusRegistered(builder);
            
            return new VContainerAdapter(builder, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer resolver.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus instance.</param>
        /// <returns>A new dependency container wrapping the resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static IDependencyContainer FromVContainerResolver(IObjectResolver resolver, string containerName = null, IMessageBusService messageBusService = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            
            // Use provided MessageBusService or try to resolve from container
            var effectiveMessageBus = messageBusService;
            if (effectiveMessageBus == null)
            {
                // Try to resolve from VContainer resolver
                if (!resolver.TryResolve<IMessageBusService>(out effectiveMessageBus))
                {
                    effectiveMessageBus = _defaultMessageBusService;
                }
            }
            
            return new VContainerAdapter(resolver, effectiveMessageBus, containerName);
        }

        
        /// <summary>
        /// Creates a pre-configured container with common registrations.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <returns>A configured dependency container.</returns>
        public static IDependencyContainer CreateConfigured(
            string containerName = null, 
            Action<IDependencyContainer> configureContainer = null)
        {
            var container = Create(containerName);
    
            // Apply custom configuration first
            configureContainer?.Invoke(container);
    
            // Register common framework dependencies
            RegisterCommonDependencies(container);
    
            return container;
        }

        /// <summary>
        /// Creates a pre-configured container with common registrations and a specific message bus.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <param name="messageBusService">The message bus to use for the container.</param>
        /// <returns>A configured dependency container.</returns>
        public static IDependencyContainer CreateConfigured(
            string containerName, 
            Action<IDependencyContainer> configureContainer,
            IMessageBusService messageBusService)
        {
            // Temporarily set the message bus for this container creation
            var previousMessageBus = _defaultMessageBusService;
            _defaultMessageBusService = messageBusService;
    
            try
            {
                var container = Create(containerName);
        
                // Apply custom configuration first
                configureContainer?.Invoke(container);
        
                // Register common framework dependencies
                RegisterCommonDependencies(container);
        
                return container;
            }
            finally
            {
                // Restore the previous default message bus
                _defaultMessageBusService = previousMessageBus;
            }
        }

        /// <summary>
        /// Creates a fully built and ready-to-use container.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <returns>A built and ready dependency container.</returns>
        public static IDependencyContainer CreateAndBuild(
            string containerName = null, 
            Action<IDependencyContainer> configureContainer = null)
        {
            var container = CreateConfigured(containerName, configureContainer);
            
            // Build the container if it's a VContainerAdapter
            if (container is VContainerAdapter adapter)
            {
                return adapter.Build();
            }
            
            return container;
        }

        /// <summary>
        /// Sets the default container implementation to use.
        /// </summary>
        /// <param name="implementation">The container implementation to use.</param>
        public static void SetDefaultImplementation(ContainerImplementation implementation)
        {
            CurrentImplementation = implementation;
        }

        /// <summary>
        /// Sets the default message bus instance for all containers.
        /// </summary>
        /// <param name="messageBusService">The message bus instance to use as default.</param>
        public static void SetDefaultMessageBus(IMessageBusService messageBusService)
        {
            _defaultMessageBusService = messageBusService;
        }

        /// <summary>
        /// Registers common framework dependencies in the container.
        /// </summary>
        /// <param name="container">The container to register dependencies in.</param>
        private static void RegisterCommonDependencies(IDependencyContainer container)
        {
            if (container == null) return;

            try
            {
                // Register the container as IDependencyProvider (self-registration)
                if (!container.IsRegistered<IDependencyProvider>())
                {
                    container.RegisterInstance<IDependencyProvider>(container);
                }

                // Register IServiceProvider adapter for .NET compatibility
                if (!container.IsRegistered<IServiceProvider>())
                {
                    container.RegisterSingleton<IServiceProvider>(provider => 
                        new ServiceProviderAdapter(provider));
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DependencyContainerFactory] Failed to register common dependencies: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures MessageBusService is registered in the builder if not already present.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private static void EnsureMessageBusRegistered(IContainerBuilder builder)
        {
            if (builder == null) return;

            try
            {
                // Check if MessageBusService is already registered
                if (!VContainerInspectionExtensions.IsRegistered(builder, typeof(IMessageBusService)))
                {
                    RegisterDefaultMessageBus(builder);
                }
            }
            catch (Exception)
            {
                // If we can't check registration, try to register anyway
                RegisterDefaultMessageBus(builder);
            }
        }

        /// <summary>
        /// Registers a default MessageBusService implementation in the builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private static void RegisterDefaultMessageBus(IContainerBuilder builder)
        {
            if (builder == null) return;

            try
            {
                if (_defaultMessageBusService != null)
                {
                    VContainer.ContainerBuilderExtensions.RegisterInstance(builder, _defaultMessageBusService).As<IMessageBusService>();
                }
                else
                {
                    // Register a factory that creates MessageBusService from Unity provider
                    VContainer.ContainerBuilderExtensions.Register<IMessageBusService>(builder, resolver =>
                    {
                        try
                        {
                            // Try to get from MessageBusProvider
                            var provider = MessageBusProvider.Instance;
                            if (provider != null && provider.IsInitialized)
                            {
                                return provider.MessageBusService;
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"[DependencyContainerFactory] Failed to get MessageBusService from provider: {ex.Message}");
                        }

                        // Fallback: create a null message bus to avoid breaking the container
                        return new NullMessageBusService();
                    }, Lifetime.Singleton);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DependencyContainerFactory] Failed to register MessageBusService: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a container builder that can be configured before building.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A container builder that can be configured.</returns>
        public static IDependencyContainerBuilder CreateBuilder(string containerName = null)
        {
            return new DependencyContainerBuilder(containerName);
        }
    }

    /// <summary>
    /// Enumeration of supported container implementations.
    /// </summary>
    public enum ContainerImplementation
    {
        /// <summary>
        /// VContainer implementation (Unity-focused DI container).
        /// </summary>
        VContainer,

        /// <summary>
        /// Future support for other container implementations.
        /// </summary>
        // Zenject,
        // Microsoft,
        // Custom
    }
}