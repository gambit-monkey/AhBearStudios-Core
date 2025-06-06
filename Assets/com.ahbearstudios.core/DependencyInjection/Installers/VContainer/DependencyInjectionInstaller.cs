using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Providers.Unity;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// VContainer installer for the AhBearStudios DependencyInjection system.
    /// Registers all core DI services and abstractions to be resolved by VContainer.
    /// Properly handles initialization order to avoid circular dependencies.
    /// </summary>
    public sealed class DependencyInjectionInstaller : IInstaller
    {
        private readonly bool _enableDebugLogging;
        private readonly bool _registerUnityProvider;

        /// <summary>
        /// Initializes a new instance of the DependencyInjectionInstaller class.
        /// </summary>
        /// <param name="enableDebugLogging">Whether to enable debug logging for the DI system.</param>
        /// <param name="registerUnityProvider">Whether to register UnityDependencyProvider.</param>
        public DependencyInjectionInstaller(bool enableDebugLogging = false, bool registerUnityProvider = true)
        {
            _enableDebugLogging = enableDebugLogging;
            _registerUnityProvider = registerUnityProvider;
        }

        /// <summary>
        /// Installs the dependency injection system services into the VContainer.
        /// </summary>
        /// <param name="builder">The VContainer builder instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public void Install(IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Starting DI system installation...");

                // Step 1: Register core MessageBus system (required foundation)
                RegisterMessageBusServices(builder);
                
                // Step 2: Register core provider interfaces that delegate to VContainer
                RegisterCoreProviderInterfaces(builder);
                
                // Step 3: Register factory services
                RegisterFactoryServices(builder);
                
                // Step 4: Register Unity integration services (optional)
                if (_registerUnityProvider)
                {
                    RegisterUnityServices(builder);
                }

                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] DI system installation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to install DI system: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers MessageBus services if not already present.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterMessageBusServices(IContainerBuilder builder)
        {
            try
            {
                // Register MessageBus implementation if not already registered
                builder.RegisterIfNotPresent<IMessageBus, MessagePipeBus>(Lifetime.Singleton);

                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Registered MessageBus services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to register MessageBus services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers core provider interfaces that adapt VContainer functionality.
        /// These allow other parts of the system to use our DI abstractions.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterCoreProviderInterfaces(IContainerBuilder builder)
        {
            try
            {
                // Register IDependencyProvider that wraps VContainer's IObjectResolver
                builder.RegisterIfNotPresent<IDependencyProvider>(resolver =>
                    new VContainerDependencyProviderAdapter(resolver), Lifetime.Singleton);

                // Register IServiceProvider for .NET compatibility
                builder.RegisterIfNotPresent<IServiceProvider>(resolver =>
                {
                    var dependencyProvider = resolver.Resolve<IDependencyProvider>();
                    return new ServiceProviderAdapter(dependencyProvider);
                }, Lifetime.Singleton);

                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Registered core provider interfaces");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to register provider interfaces: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers factory services for creating dependency containers.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterFactoryServices(IContainerBuilder builder)
        {
            try
            {
                // Register factory configuration callback
                builder.RegisterBuildCallback(container =>
                {
                    try
                    {
                        // Set the default MessageBus for the factory
                        var messageBus = container.Resolve<IMessageBus>();
                        DependencyContainerFactory.SetDefaultMessageBus(messageBus);

                        if (_enableDebugLogging)
                            Debug.Log("[DependencyInjectionInstaller] Configured DependencyContainerFactory");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DependencyInjectionInstaller] Failed to configure factory: {ex.Message}");
                    }
                });

                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Registered factory services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to register factory services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers Unity-specific services for scene integration.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterUnityServices(IContainerBuilder builder)
        {
            try
            {
                // Register UnityDependencyProvider initialization
                builder.RegisterBuildCallback(container =>
                {
                    try
                    {
                        InitializeUnityDependencyProvider(container);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DependencyInjectionInstaller] Failed to initialize UnityDependencyProvider: {ex.Message}");
                    }
                });

                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Registered Unity services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to register Unity services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes UnityDependencyProvider after the container is built.
        /// </summary>
        /// <param name="container">The built VContainer resolver.</param>
        private void InitializeUnityDependencyProvider(IObjectResolver container)
        {
            try
            {
                // Check if there's already a global UnityDependencyProvider
                var existing = UnityDependencyProvider.Global;
                if (existing != null && existing.IsInitialized)
                {
                    if (_enableDebugLogging)
                        Debug.Log("[DependencyInjectionInstaller] UnityDependencyProvider already initialized");
                    return;
                }

                // Try to find an existing provider in the scene
                var found = UnityEngine.Object.FindFirstObjectByType<UnityDependencyProvider>();
                if (found != null)
                {
                    if (!found.IsInitialized)
                    {
                        // Initialize the found provider using the regular Initialize() method
                        found.Initialize();
                        
                        if (_enableDebugLogging)
                            Debug.Log("[DependencyInjectionInstaller] Initialized existing UnityDependencyProvider");
                    }
                    return;
                }

                // Create new UnityDependencyProvider if none exists
                var go = new GameObject("[UnityDependencyProvider]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                var provider = go.AddComponent<UnityDependencyProvider>();
                
                // Initialize using the standard method
                provider.Initialize();
                
                if (_enableDebugLogging)
                    Debug.Log("[DependencyInjectionInstaller] Created and initialized new UnityDependencyProvider");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyInjectionInstaller] Failed to initialize UnityDependencyProvider: {ex.Message}");
            }
        }
    }
}