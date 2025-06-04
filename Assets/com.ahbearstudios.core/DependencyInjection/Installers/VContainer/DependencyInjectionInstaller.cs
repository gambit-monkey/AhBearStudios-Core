
using AhBearStudios.Core.DependencyInjection.Extensions;
using VContainer;
using VContainer.Unity;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Unity;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses;
using AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe;
using UnityEngine;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// VContainer installer for the AhBearStudios DependencyInjection system.
    /// Registers all core DI services and abstractions to be resolved by VContainer.
    /// </summary>
    public class DependencyInjectionInstaller : IInstaller
    {
        /// <summary>
        /// Installs the dependency injection system services into the VContainer.
        /// </summary>
        /// <param name="builder">The VContainer builder instance.</param>
        public void Install(IContainerBuilder builder)
        {
            // Register the core MessageBus system first (required by DI system)
            RegisterMessageBusServices(builder);
            
            // Register the main dependency container
            RegisterDependencyContainer(builder);
            
            // Register provider interfaces
            RegisterProviderInterfaces(builder);
            
            // Register Unity-specific services
            RegisterUnityServices(builder);
            
            // Register factory
            RegisterFactory(builder);
        }

        private void RegisterMessageBusServices(IContainerBuilder builder)
        {
            // Register MessageBus if not already registered
            builder.RegisterIfNotPresent<IMessageBus, MessagePipeBus>(Lifetime.Singleton);
            builder.RegisterIfNotPresent<IMessageBus, NullMessageBus>(Lifetime.Singleton);
        }

        private void RegisterDependencyContainer(IContainerBuilder builder)
        {
            // Register the main dependency container as singleton
            // This will be the primary container that wraps VContainer
            builder.Register<IDependencyContainer>(resolver =>
            {
                var messageBus = resolver.Resolve<IMessageBus>();
                
                // Create a VContainer-based dependency container
                var container = DependencyContainerFactory.CreateConfigured(
                    "VContainer_Primary",
                    containerBuilder => {
                        // Register any additional services needed for the primary container
                        // This is where you can add default registrations
                    },
                    messageBus
                );
                
                return container;
            }, Lifetime.Singleton);
        }

        private void RegisterProviderInterfaces(IContainerBuilder builder)
        {
            // Register provider interfaces that delegate to the main container
            builder.Register<IDependencyProvider>(resolver =>
            {
                return resolver.Resolve<IDependencyContainer>();
            }, Lifetime.Singleton);

            builder.Register<IDependencyInjector>(resolver =>
            {
                return resolver.Resolve<IDependencyContainer>();
            }, Lifetime.Singleton);

            builder.Register<IServiceProvider>(resolver =>
            {
                var container = resolver.Resolve<IDependencyContainer>();
                
                // Create an adapter that implements System.IServiceProvider interface
                return new ServiceProviderAdapter(container);
            }, Lifetime.Singleton);
        }

        private void RegisterUnityServices(IContainerBuilder builder)
        {
            // Register UnityDependencyProvider if it exists in the scene
            builder.Register<UnityDependencyProvider>(resolver =>
            {
                var existing = UnityDependencyProvider.Global;
                if (existing != null && existing.IsInitialized)
                {
                    return existing;
                }

                // If no global instance exists, try to find one in the scene
                var found = Object.FindObjectOfType<UnityDependencyProvider>();
                if (found != null)
                {
                    if (!found.IsInitialized)
                    {
                        // Initialize with our container and message bus
                        var container = resolver.Resolve<IDependencyContainer>();
                        var messageBus = resolver.Resolve<IMessageBus>();
                        found.Initialize(container, messageBus);
                    }
                    return found;
                }

                // If none found, create a new GameObject with UnityDependencyProvider
                var go = new GameObject("Unity Dependency Provider");
                Object.DontDestroyOnLoad(go);
                var provider = go.AddComponent<UnityDependencyProvider>();
                
                var providerContainer = resolver.Resolve<IDependencyContainer>();
                var providerMessageBus = resolver.Resolve<IMessageBus>();
                provider.Initialize(providerContainer, providerMessageBus);
                
                return provider;
            }, Lifetime.Singleton);
        }

        private void RegisterFactory(IContainerBuilder builder)
        {
            // Register the factory as a singleton so it can be used throughout the application
            builder.Register<DependencyContainerFactory>(resolver =>
            {
                // The factory is static, so we just return an instance for DI purposes
                return new DependencyContainerFactory();
            }, Lifetime.Singleton);
        }
    }

    /// <summary>
    /// Adapter class to bridge IDependencyProvider to System.IServiceProvider
    /// </summary>
    internal class ServiceProviderAdapter : AhBearStudios.Core.DependencyInjection.Interfaces.IServiceProvider
    {
        private readonly IDependencyProvider _provider;

        public ServiceProviderAdapter(IDependencyProvider provider)
        {
            _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
        }

        public object GetService(System.Type serviceType)
        {
            if (_provider is IDependencyContainer container)
            {
                if (container.IsRegistered(serviceType))
                {
                    try
                    {
                        // Use reflection to call the generic Resolve method
                        var resolveMethod = typeof(IDependencyProvider).GetMethod(nameof(IDependencyProvider.Resolve));
                        var genericResolveMethod = resolveMethod.MakeGenericMethod(serviceType);
                        return genericResolveMethod.Invoke(_provider, null);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }
    }
}