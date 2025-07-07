using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters.VContainer
{
    /// <summary>
    /// Factory for creating VContainer adapter instances.
    /// Handles VContainer availability detection and provides consistent container creation.
    /// Fixed to use correct VContainer API for child containers.
    /// </summary>
    public sealed class VContainerAdapterFactory : IContainerAdapterFactory
    {
        private static readonly Lazy<bool> IsVContainerAvailable = new Lazy<bool>(CheckVContainerAvailability);

        /// <summary>
        /// Gets the framework this factory supports.
        /// </summary>
        public ContainerFramework SupportedFramework => ContainerFramework.VContainer;

        /// <summary>
        /// Gets whether VContainer is available in the current environment.
        /// </summary>
        public bool IsFrameworkAvailable => IsVContainerAvailable.Value;

        /// <summary>
        /// Creates a new VContainer adapter with the specified configuration.
        /// </summary>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainer adapter instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when VContainer is not available.</exception>
        public IContainerAdapter CreateContainer(
            IDependencyInjectionConfig config,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!IsFrameworkAvailable)
                throw new NotSupportedException("VContainer is not available in the current environment");

            try
            {
                // Create a new VContainer builder (correct API: no parent in constructor)
                var builder = new ContainerBuilder();

                // Apply VContainer-specific configuration options
                ApplyVContainerConfiguration(builder, config);

                // Create the adapter (no parent resolver for root container)
                var adapter = new VContainerAdapter(
                    builder,
                    containerName,
                    config,
                    messageBusService);

                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.Log($"[VContainerAdapterFactory] Created new VContainer adapter '{adapter.ContainerName}'");
                }

                return adapter;
            }
            catch (Exception ex)
            {
                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.LogError($"[VContainerAdapterFactory] Failed to create VContainer adapter: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Creates a VContainer adapter from an existing VContainer builder.
        /// </summary>
        /// <param name="frameworkBuilder">The VContainer builder object.</param>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainer adapter instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when frameworkBuilder or config is null.</exception>
        /// <exception cref="ArgumentException">Thrown when frameworkBuilder is not a VContainer builder.</exception>
        /// <exception cref="NotSupportedException">Thrown when VContainer is not available.</exception>
        public IContainerAdapter CreateFromBuilder(
            object frameworkBuilder,
            IDependencyInjectionConfig config,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (frameworkBuilder == null)
                throw new ArgumentNullException(nameof(frameworkBuilder));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!IsFrameworkAvailable)
                throw new NotSupportedException("VContainer is not available in the current environment");

            // Validate that the builder is a VContainer builder
            if (!(frameworkBuilder is IContainerBuilder vcontainerBuilder))
            {
                throw new ArgumentException(
                    $"Expected VContainer IContainerBuilder, but received {frameworkBuilder.GetType().FullName}",
                    nameof(frameworkBuilder));
            }

            try
            {
                // Apply VContainer-specific configuration to the existing builder
                ApplyVContainerConfiguration(vcontainerBuilder, config);

                // Create the adapter with the existing builder (no parent resolver)
                var adapter = new VContainerAdapter(
                    vcontainerBuilder,
                    containerName,
                    config,
                    messageBusService);

                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.Log($"[VContainerAdapterFactory] Created VContainer adapter '{adapter.ContainerName}' from existing builder");
                }

                return adapter;
            }
            catch (Exception ex)
            {
                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.LogError($"[VContainerAdapterFactory] Failed to create VContainer adapter from builder: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Creates a VContainer adapter for Unity integration scenarios.
        /// Fixed to use correct VContainer API - parent relationship handled at Build() time.
        /// </summary>
        /// <param name="parentContainer">Optional parent container for hierarchical DI.</param>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainer adapter configured for Unity integration.</returns>
        public IContainerAdapter CreateForUnity(
            IObjectResolver parentContainer = null,
            IDependencyInjectionConfig config = null,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (!IsFrameworkAvailable)
                throw new NotSupportedException("VContainer is not available in the current environment");

            try
            {
                // Create builder using correct VContainer API (no parent in constructor)
                var builder = new ContainerBuilder();

                config ??= new Configuration.DependencyInjectionConfig();

                // Apply Unity-specific VContainer configuration
                ApplyUnityConfiguration(builder, config);

                // Create adapter and pass parent resolver for Build() phase
                var adapter = new VContainerAdapter(
                    builder,
                    containerName ?? "UnityVContainer",
                    config,
                    messageBusService,
                    null, // validator
                    parentContainer); // Parent resolver for child container support

                if (config.EnableDebugLogging)
                {
                    var parentInfo = parentContainer != null ? " with parent container" : "";
                    UnityEngine.Debug.Log($"[VContainerAdapterFactory] Created Unity-integrated VContainer adapter '{adapter.ContainerName}'{parentInfo}");
                }

                return adapter;
            }
            catch (Exception ex)
            {
                var enableLogging = config?.EnableDebugLogging ?? false;
                if (enableLogging)
                {
                    UnityEngine.Debug.LogError($"[VContainerAdapterFactory] Failed to create Unity VContainer adapter: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Creates a child VContainer adapter from a parent resolver.
        /// Uses correct VContainer API for child container creation.
        /// </summary>
        /// <param name="parentResolver">The parent VContainer resolver.</param>
        /// <param name="config">Configuration for the child container.</param>
        /// <param name="containerName">Optional name for the child container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new child VContainer adapter.</returns>
        public IContainerAdapter CreateChild(
            IObjectResolver parentResolver,
            IDependencyInjectionConfig config = null,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (parentResolver == null)
                throw new ArgumentNullException(nameof(parentResolver));

            if (!IsFrameworkAvailable)
                throw new NotSupportedException("VContainer is not available in the current environment");

            try
            {
                // Create new builder using correct VContainer API
                var childBuilder = new ContainerBuilder();

                config ??= new Configuration.DependencyInjectionConfig();

                // Create child adapter with parent resolver reference
                var childAdapter = new VContainerAdapter(
                    childBuilder,
                    containerName ?? $"VContainer_Child_{Guid.NewGuid():N}",
                    config,
                    messageBusService,
                    null, // validator
                    parentResolver); // Parent resolver for Build() phase

                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.Log($"[VContainerAdapterFactory] Created child VContainer adapter '{childAdapter.ContainerName}'");
                }

                return childAdapter;
            }
            catch (Exception ex)
            {
                var enableLogging = config?.EnableDebugLogging ?? false;
                if (enableLogging)
                {
                    UnityEngine.Debug.LogError($"[VContainerAdapterFactory] Failed to create child VContainer adapter: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Applies VContainer-specific configuration options to the builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to configure.</param>
        /// <param name="config">The DI configuration.</param>
        private void ApplyVContainerConfiguration(IContainerBuilder builder, IDependencyInjectionConfig config)
        {
            if (config.FrameworkSpecificOptions == null)
                return;

            try
            {
                // Apply VContainer-specific options
                if (config.FrameworkSpecificOptions.TryGetValue("EnableCodeGeneration", out var enableCodeGen) &&
                    enableCodeGen is bool codeGenEnabled && codeGenEnabled)
                {
                    // VContainer code generation would be applied here
                    // This is a placeholder for when VContainer supports code generation configuration
                }

                if (config.FrameworkSpecificOptions.TryGetValue("EnableDiagnostics", out var enableDiag) &&
                    enableDiag is bool diagEnabled && diagEnabled)
                {
                    // VContainer diagnostics would be enabled here
                    // This is a placeholder for VContainer diagnostic configuration
                }

                if (config.FrameworkSpecificOptions.TryGetValue("ValidateDependencies", out var validateDeps) &&
                    validateDeps is bool validateEnabled && validateEnabled)
                {
                    // VContainer dependency validation would be configured here
                    // This leverages our existing validation extensions
                }

                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.Log("[VContainerAdapterFactory] Applied VContainer-specific configuration options");
                }
            }
            catch (Exception ex)
            {
                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.LogWarning($"[VContainerAdapterFactory] Failed to apply some VContainer configuration options: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if VContainer is available in the current environment.
        /// </summary>
        /// <returns>True if VContainer is available, false otherwise.</returns>
        private static bool CheckVContainerAvailability()
        {
            try
            {
                // Check if VContainer types are available
                var containerBuilderType = typeof(ContainerBuilder);
                var objectResolverType = typeof(IObjectResolver);
                var lifetimeType = typeof(Lifetime);

                // If we can access these types without exception, VContainer is available
                return containerBuilderType != null && 
                       objectResolverType != null && 
                       lifetimeType != null;
            }
            catch (Exception)
            {
                // If any exception occurs, assume VContainer is not available
                return false;
            }
        }

        /// <summary>
        /// Applies Unity-specific configuration to the VContainer builder.
        /// </summary>
        /// <param name="builder">The VContainer builder.</param>
        /// <param name="config">The DI configuration.</param>
        private void ApplyUnityConfiguration(IContainerBuilder builder, IDependencyInjectionConfig config)
        {
            try
            {
                // Register Unity-specific services that are commonly needed
                // This could include Unity-specific logging, coroutine managers, etc.
                
                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.Log("[VContainerAdapterFactory] Applied Unity-specific VContainer configuration");
                }
            }
            catch (Exception ex)
            {
                if (config.EnableDebugLogging)
                {
                    UnityEngine.Debug.LogWarning($"[VContainerAdapterFactory] Failed to apply Unity configuration: {ex.Message}");
                }
            }
        }
    }
}