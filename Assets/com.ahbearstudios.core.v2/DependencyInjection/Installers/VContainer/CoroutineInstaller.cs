using System;
using AhBearStudios.Core.Coroutine.Factories;
using AhBearStudios.Core.Coroutine.Interfaces;
using AhBearStudios.Core.Coroutine.Unity;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AhBearStudios.Core.Coroutine.Installers.VContainer
{
    /// <summary>
    /// VContainer installer for the coroutine management system.
    /// Registers all coroutine-related services with proper dependency injection patterns.
    /// </summary>
    public sealed class CoroutineInstaller : IInstaller
    {
        private readonly bool _enableDebugLogging;
        private readonly bool _autoInitialize;

        /// <summary>
        /// Initializes a new instance of the CoroutineInstaller class.
        /// </summary>
        /// <param name="enableDebugLogging">Whether to enable debug logging for the coroutine system.</param>
        /// <param name="autoInitialize">Whether to automatically initialize the coroutine manager after registration.</param>
        public CoroutineInstaller(bool enableDebugLogging = false, bool autoInitialize = true)
        {
            _enableDebugLogging = enableDebugLogging;
            _autoInitialize = autoInitialize;
        }

        /// <summary>
        /// Installs the coroutine system services into the VContainer.
        /// </summary>
        /// <param name="builder">The VContainer builder instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public void Install(IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Starting coroutine system installation...");

                // Register core coroutine services
                RegisterCoroutineServices(builder);
                
                // Register factory services
                RegisterFactoryServices(builder);
                
                // Setup initialization callback
                if (_autoInitialize)
                {
                    RegisterInitializationCallback(builder);
                }

                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Coroutine system installation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoroutineInstaller] Failed to install coroutine system: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers core coroutine services.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterCoroutineServices(IContainerBuilder builder)
        {
            try
            {
                // Register the coroutine manager as singleton
                // This will create the instance and register it for both interfaces
                builder.RegisterIfNotPresent<CoreCoroutineManager>(Lifetime.Singleton)
                    .AsImplementedInterfaces() // Registers as ICoroutineManager
                    .AsSelf(); // Also registers as CoreCoroutineManager

                // Register the default runner factory method
                builder.RegisterIfNotPresent<ICoroutineRunner>(resolver =>
                {
                    var manager = resolver.Resolve<ICoroutineManager>();
                    return manager.DefaultRunner;
                }, Lifetime.Singleton);

                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Registered core coroutine services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoroutineInstaller] Failed to register coroutine services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers factory services for creating coroutine runners.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterFactoryServices(IContainerBuilder builder)
        {
            try
            {
                // Register factory for creating coroutine runners
                builder.RegisterIfNotPresent<ICoroutineRunnerFactory, CoroutineRunnerFactory>(Lifetime.Singleton);

                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Registered factory services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoroutineInstaller] Failed to register factory services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers initialization callback to setup the coroutine system after container is built.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterInitializationCallback(IContainerBuilder builder)
        {
            try
            {
                builder.RegisterBuildCallback(container =>
                {
                    try
                    {
                        InitializeCoroutineSystem(container);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CoroutineInstaller] Failed to initialize coroutine system: {ex.Message}");
                    }
                });

                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Registered initialization callback");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoroutineInstaller] Failed to register initialization callback: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes the coroutine system after the container is built.
        /// </summary>
        /// <param name="container">The built VContainer resolver.</param>
        private void InitializeCoroutineSystem(IObjectResolver container)
        {
            try
            {
                // Get the coroutine manager and initialize it
                var manager = container.Resolve<CoreCoroutineManager>();
                
                // Initialize without dependency provider since services are already registered
                manager.Initialize();

                if (_enableDebugLogging)
                    Debug.Log("[CoroutineInstaller] Coroutine system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoroutineInstaller] Failed to initialize coroutine system: {ex.Message}");
            }
        }
    }
}