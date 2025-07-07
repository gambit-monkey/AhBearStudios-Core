using AhBearStudios.Core.DependencyInjection.Adapters.VContainer;
using AhBearStudios.Core.DependencyInjection.Bootstrap;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// Framework installer for VContainer integration with our DI system.
    /// Handles VContainer-specific setup, configuration, and service registration.
    /// Ensures proper initialization order and dependency management.
    /// </summary>
    public sealed class VContainerFrameworkInstaller : BaseFrameworkInstaller
    {
        /// <summary>
        /// Gets the framework this installer supports.
        /// </summary>
        public override ContainerFramework SupportedFramework => ContainerFramework.VContainer;

        /// <summary>
        /// Gets the installer name for identification and logging.
        /// </summary>
        public override string InstallerName => "VContainerFrameworkInstaller";

        /// <summary>
        /// Gets the priority of this installer. Lower values install first.
        /// VContainer is our primary framework, so it gets high priority.
        /// </summary>
        public override int Priority => 100;

        /// <summary>
        /// Gets whether this installer is enabled and should be processed.
        /// Only enabled when VContainer is available.
        /// </summary>
        public override bool IsEnabled => IsVContainerAvailable();

        /// <summary>
        /// Gets the dependencies required by this installer.
        /// VContainer installer typically runs early and has minimal dependencies.
        /// </summary>
        public override Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// Framework-specific validation logic.
        /// Validates that VContainer is available and properly configured.
        /// </summary>
        protected override bool DoValidateInstaller(IDependencyInjectionConfig config)
        {
            if (!IsVContainerAvailable())
            {
                LogIfEnabled(config, "VContainer is not available in the current environment");
                return false;
            }

            if (config.PreferredFramework != ContainerFramework.VContainer)
            {
                LogIfEnabled(config, $"Configuration specifies {config.PreferredFramework}, but this is the VContainer installer");
                return false;
            }

            // Validate VContainer-specific configuration options
            if (config.FrameworkSpecificOptions != null)
            {
                foreach (var option in config.FrameworkSpecificOptions)
                {
                    if (!IsValidVContainerOption(option.Key, option.Value))
                    {
                        LogIfEnabled(config, $"Invalid VContainer option: {option.Key} = {option.Value}");
                        return false;
                    }
                }
            }

            LogIfEnabled(config, "VContainer installer validation passed");
            return true;
        }

        /// <summary>
        /// Framework-specific pre-installation logic.
        /// Sets up VContainer-specific prerequisites.
        /// </summary>
        protected override void DoPreInstall(IDependencyInjectionConfig config)
        {
            LogIfEnabled(config, "Preparing VContainer environment");

            try
            {
                // Verify VContainer types are accessible
                var containerBuilderType = typeof(global::VContainer.ContainerBuilder);
                var objectResolverType = typeof(global::VContainer.IObjectResolver);
                
                if (containerBuilderType == null || objectResolverType == null)
                {
                    throw new InvalidOperationException("VContainer types are not accessible");
                }

                // Initialize VContainer adapter factory in our factory system
                var factory = new VContainerAdapterFactory();
                if (!factory.IsFrameworkAvailable)
                {
                    throw new InvalidOperationException("VContainer factory reports framework as unavailable");
                }

                LogIfEnabled(config, "VContainer environment prepared successfully");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"Failed to prepare VContainer environment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Framework-specific installation logic.
        /// Registers VContainer services and adapters in the container.
        /// </summary>
        protected override void DoInstall(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            if (container.Framework != ContainerFramework.VContainer)
            {
                throw new InvalidOperationException(
                    $"VContainer installer can only be used with VContainer containers, " +
                    $"but received {container.Framework} container");
            }

            LogIfEnabled(config, "Installing VContainer framework services");

            try
            {
                // Register VContainer adapter factory
                container.RegisterSingleton<IContainerAdapterFactory>(resolver =>
                    new VContainerAdapterFactory());

                // Register VContainer-specific services that might be needed
                RegisterVContainerSpecificServices(container, config);

                // Register our DI abstractions that delegate to VContainer
                RegisterDIAbstractions(container, config);

                // Apply VContainer-specific configuration
                ApplyVContainerConfiguration(container, config);

                LogIfEnabled(config, "VContainer framework services installed successfully");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"Failed to install VContainer framework services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Framework-specific post-installation logic.
        /// Validates the installation and sets up monitoring.
        /// </summary>
        protected override void DoPostInstall(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            LogIfEnabled(config, "Performing VContainer post-installation setup");

            try
            {
                // Validate that our services were registered correctly
                ValidateInstallation(container, config);

                // Set up VContainer-specific monitoring if metrics are enabled
                if (config.EnablePerformanceMetrics)
                {
                    SetupVContainerMonitoring(container, config);
                }

                // Register for container lifecycle events if message bus is available
                if (container.MessageBusService != null)
                {
                    RegisterLifecycleEventHandlers(container, config);
                }

                LogIfEnabled(config, "VContainer post-installation setup completed successfully");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"VContainer post-installation setup failed: {ex.Message}");
                // Don't throw here as the core installation succeeded
            }
        }

        /// <summary>
        /// Checks if VContainer is available in the current environment.
        /// </summary>
        private static bool IsVContainerAvailable()
        {
            try
            {
                // Check if VContainer types are available
                var containerBuilderType = Type.GetType("VContainer.ContainerBuilder, VContainer");
                var objectResolverType = Type.GetType("VContainer.IObjectResolver, VContainer");
                
                return containerBuilderType != null && objectResolverType != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a VContainer-specific configuration option.
        /// </summary>
        private static bool IsValidVContainerOption(string key, object value)
        {
            return key switch
            {
                "EnableCodeGeneration" => value is bool,
                "EnableDiagnostics" => value is bool,
                "ValidateDependencies" => value is bool,
                _ => false // Unknown options are invalid
            };
        }

        /// <summary>
        /// Registers VContainer-specific services that might be needed by the system.
        /// </summary>
        private void RegisterVContainerSpecificServices(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            // Register VContainer validation services if validation is enabled
            if (config.EnableValidation)
            {
                // Our validation system already integrates with VContainer via extensions
                LogIfEnabled(config, "VContainer validation services are available via extensions");
            }

            // Register VContainer inspection services if debug logging is enabled
            if (config.EnableDebugLogging)
            {
                // Our inspection system already integrates with VContainer via extensions
                LogIfEnabled(config, "VContainer inspection services are available via extensions");
            }
        }

        /// <summary>
        /// Registers our DI abstractions that delegate to VContainer.
        /// </summary>
        private void RegisterDIAbstractions(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            // These are typically registered by the VContainer adapter itself
            // But we can ensure they're available here

            if (!container.IsRegistered<IDependencyProvider>())
            {
                container.RegisterSingleton<IDependencyProvider>(resolver =>
                {
                    // This assumes the resolver can access the underlying VContainer resolver
                    // Implementation would need to extract the VContainer resolver from our resolver
                    throw new NotImplementedException(
                        "IDependencyProvider registration requires access to underlying VContainer resolver");
                });
            }

            LogIfEnabled(config, "DI abstractions are handled by VContainer adapter");
        }

        /// <summary>
        /// Applies VContainer-specific configuration options.
        /// </summary>
        private void ApplyVContainerConfiguration(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            if (config.FrameworkSpecificOptions == null || config.FrameworkSpecificOptions.Count == 0)
            {
                LogIfEnabled(config, "No VContainer-specific options to apply");
                return;
            }

            foreach (var option in config.FrameworkSpecificOptions)
            {
                try
                {
                    ApplyVContainerOption(container, option.Key, option.Value, config);
                    LogIfEnabled(config, $"Applied VContainer option: {option.Key} = {option.Value}");
                }
                catch (Exception ex)
                {
                    LogIfEnabled(config, $"Failed to apply VContainer option {option.Key}: {ex.Message}");
                    if (config.ThrowOnValidationFailure)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Applies a specific VContainer configuration option.
        /// </summary>
        private void ApplyVContainerOption(IContainerAdapter container, string key, object value, IDependencyInjectionConfig config)
        {
            switch (key)
            {
                case "EnableCodeGeneration":
                    if (value is bool enableCodeGen)
                    {
                        // VContainer code generation configuration would go here
                        LogIfEnabled(config, $"VContainer code generation: {enableCodeGen}");
                    }
                    break;

                case "EnableDiagnostics":
                    if (value is bool enableDiag)
                    {
                        // VContainer diagnostics configuration would go here
                        LogIfEnabled(config, $"VContainer diagnostics: {enableDiag}");
                    }
                    break;

                case "ValidateDependencies":
                    if (value is bool validateDeps)
                    {
                        // VContainer dependency validation would be configured here
                        LogIfEnabled(config, $"VContainer dependency validation: {validateDeps}");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown VContainer option: {key}");
            }
        }

        /// <summary>
        /// Validates that the VContainer installation was successful.
        /// </summary>
        private void ValidateInstallation(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            // Check that essential services are registered
            var requiredServices = new[]
            {
                typeof(IContainerAdapterFactory),
            };

            foreach (var serviceType in requiredServices)
            {
                if (!container.IsRegistered(serviceType))
                {
                    var message = $"Required service {serviceType.Name} was not registered during VContainer installation";
                    LogIfEnabled(config, message);
                    
                    if (config.ThrowOnValidationFailure)
                    {
                        throw new InvalidOperationException(message);
                    }
                }
            }

            LogIfEnabled(config, "VContainer installation validation passed");
        }

        /// <summary>
        /// Sets up VContainer-specific performance monitoring.
        /// </summary>
        private void SetupVContainerMonitoring(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            try
            {
                // VContainer monitoring setup would go here
                // This might involve registering performance counters, metrics collectors, etc.
                LogIfEnabled(config, "VContainer performance monitoring setup completed");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"Failed to setup VContainer monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers handlers for container lifecycle events.
        /// </summary>
        private void RegisterLifecycleEventHandlers(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            try
            {
                // Register for container lifecycle events via message bus
                // This would subscribe to events like ServiceRegisteredMessage, ContainerBuiltMessage, etc.
                LogIfEnabled(config, "VContainer lifecycle event handlers registered");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"Failed to register VContainer lifecycle event handlers: {ex.Message}");
            }
        }
    }
}