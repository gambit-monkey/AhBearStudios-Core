using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Builders;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.DependencyInjection.Utilities;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Enhanced factory for creating dependency injection containers with multiple framework support.
    /// Provides configuration-driven container selection and automatic fallback mechanisms.
    /// </summary>
    public static class DependencyContainerFactory
    {
        private static readonly Dictionary<ContainerFramework, IContainerAdapterFactory> RegisteredFactories 
            = new Dictionary<ContainerFramework, IContainerAdapterFactory>();
        
        private static readonly object FactoryLock = new object();
        private static IDependencyInjectionConfig _defaultConfig;
        private static IMessageBusService _defaultMessageBusService;
        private static IConfigurationLoader _configurationLoader;
        
        /// <summary>
        /// Gets or sets the default configuration for new containers.
        /// </summary>
        public static IDependencyInjectionConfig DefaultConfiguration
        {
            get => _defaultConfig ??= new DependencyInjectionConfig();
            set => _defaultConfig = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        /// <summary>
        /// Gets or sets the default message bus service for new containers.
        /// </summary>
        public static IMessageBusService DefaultMessageBusService
        {
            get => _defaultMessageBusService;
            set => _defaultMessageBusService = value;
        }
        
        /// <summary>
        /// Gets or sets the configuration loader for loading configs from files.
        /// </summary>
        public static IConfigurationLoader ConfigurationLoader
        {
            get => _configurationLoader ??= new ConfigurationLoader();
            set => _configurationLoader = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        /// <summary>
        /// Gets the registered framework factories.
        /// </summary>
        public static IReadOnlyDictionary<ContainerFramework, IContainerAdapterFactory> RegisteredFactories => 
            new Dictionary<ContainerFramework, IContainerAdapterFactory>(RegisteredFactories);
        
        static DependencyContainerFactory()
        {
            // Initialize with built-in factories
            InitializeBuiltInFactories();
        }
        
        /// <summary>
        /// Registers a container adapter factory for a specific framework.
        /// </summary>
        /// <param name="factory">The factory to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
        /// <exception cref="ArgumentException">Thrown when framework is already registered.</exception>
        public static void RegisterFactory(IContainerAdapterFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            lock (FactoryLock)
            {
                if (RegisteredFactories.ContainsKey(factory.SupportedFramework))
                    throw new ArgumentException($"Factory for framework {factory.SupportedFramework} is already registered");
                
                RegisteredFactories[factory.SupportedFramework] = factory;
            }
        }
        
        /// <summary>
        /// Unregisters a container adapter factory for a specific framework.
        /// </summary>
        /// <param name="framework">The framework to unregister.</param>
        /// <returns>True if the factory was removed, false if it wasn't registered.</returns>
        public static bool UnregisterFactory(ContainerFramework framework)
        {
            lock (FactoryLock)
            {
                return RegisteredFactories.Remove(framework);
            }
        }
        
        /// <summary>
        /// Creates a new dependency container using the default configuration.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new container adapter.</returns>
        public static IContainerAdapter Create(string containerName = null)
        {
            return Create(DefaultConfiguration, containerName, DefaultMessageBusService);
        }
        
        /// <summary>
        /// Creates a new dependency container with the specified configuration.
        /// </summary>
        /// <param name="config">Configuration for the container.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when no suitable framework is available.</exception>
        public static IContainerAdapter Create(
            IDependencyInjectionConfig config,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            // Try to create with preferred framework
            var factory = GetFactory(config.PreferredFramework);
            if (factory != null && factory.IsFrameworkAvailable)
            {
                return factory.CreateContainer(config, containerName, messageBusService);
            }
            
            // Fallback to any available framework
            factory = GetFirstAvailableFactory();
            if (factory != null)
            {
                // Create a modified config with the available framework
                var fallbackConfig = CreateFallbackConfig(config, factory.SupportedFramework);
                return factory.CreateContainer(fallbackConfig, containerName, messageBusService);
            }
            
            throw new NotSupportedException(
                $"No suitable DI framework available. Preferred: {config.PreferredFramework}, " +
                $"Registered: [{string.Join(", ", RegisteredFactories.Keys)}]");
        }
        
        /// <summary>
        /// Creates a container from a configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter.</returns>
        public static IContainerAdapter CreateFromFile(
            string configFilePath,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            var config = ConfigurationLoader.LoadFromFile(configFilePath);
            return Create(config, containerName, messageBusService);
        }
        
        /// <summary>
        /// Creates a container with configuration based on environment.
        /// </summary>
        /// <param name="environment">Environment name (Development, Production, Testing).</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter.</returns>
        public static IContainerAdapter CreateForEnvironment(
            string environment,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            var config = environment?.ToLowerInvariant() switch
            {
                "development" => DependencyInjectionConfig.Development,
                "production" => DependencyInjectionConfig.Production,
                "testing" => DependencyInjectionConfig.Testing,
                _ => DefaultConfiguration
            };
            
            return Create(config, containerName, messageBusService);
        }
        
        /// <summary>
        /// Creates a container adapter from an existing framework-specific builder.
        /// </summary>
        /// <param name="frameworkBuilder">The framework-specific builder object.</param>
        /// <param name="framework">The framework the builder belongs to.</param>
        /// <param name="config">Optional configuration (uses default if null).</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new container adapter.</returns>
        public static IContainerAdapter CreateFromBuilder(
            object frameworkBuilder,
            ContainerFramework framework,
            IDependencyInjectionConfig config = null,
            string containerName = null,
            IMessageBusService messageBusService = null)
        {
            if (frameworkBuilder == null)
                throw new ArgumentNullException(nameof(frameworkBuilder));
            
            var factory = GetFactory(framework);
            if (factory == null)
                throw new NotSupportedException($"No factory registered for framework {framework}");
            
            config ??= DefaultConfiguration;
            return factory.CreateFromBuilder(frameworkBuilder, config, containerName, messageBusService);
        }
        
        /// <summary>
        /// Gets available frameworks in order of preference.
        /// </summary>
        /// <returns>List of available frameworks.</returns>
        public static IReadOnlyList<ContainerFramework> GetAvailableFrameworks()
        {
            lock (FactoryLock)
            {
                return RegisteredFactories.Values
                    .Where(f => f.IsFrameworkAvailable)
                    .Select(f => f.SupportedFramework)
                    .OrderBy(f => (int)f) // Prefer VContainer first
                    .ToList();
            }
        }
        
        /// <summary>
        /// Checks if a specific framework is available.
        /// </summary>
        /// <param name="framework">The framework to check.</param>
        /// <returns>True if the framework is available, false otherwise.</returns>
        public static bool IsFrameworkAvailable(ContainerFramework framework)
        {
            var factory = GetFactory(framework);
            return factory?.IsFrameworkAvailable == true;
        }
        
        /// <summary>
        /// Creates a container builder for fluent configuration.
        /// </summary>
        /// <param name="framework">Optional preferred framework.</param>
        /// <returns>A new container builder.</returns>
        public static IEnhancedContainerBuilder CreateBuilder(ContainerFramework? framework = null)
        {
            var config = framework.HasValue 
                ? new DependencyInjectionConfigBuilder().WithFramework(framework.Value).Build()
                : DefaultConfiguration;
            
            return new EnhancedContainerBuilder(config);
        }
        
        /// <summary>
        /// Gets the factory for a specific framework.
        /// </summary>
        private static IContainerAdapterFactory GetFactory(ContainerFramework framework)
        {
            lock (FactoryLock)
            {
                RegisteredFactories.TryGetValue(framework, out var factory);
                return factory;
            }
        }
        
        /// <summary>
        /// Gets the first available factory.
        /// </summary>
        private static IContainerAdapterFactory GetFirstAvailableFactory()
        {
            lock (FactoryLock)
            {
                return RegisteredFactories.Values
                    .Where(f => f.IsFrameworkAvailable)
                    .OrderBy(f => (int)f.SupportedFramework)
                    .FirstOrDefault();
            }
        }
        
        /// <summary>
        /// Creates a fallback configuration for a different framework.
        /// </summary>
        private static IDependencyInjectionConfig CreateFallbackConfig(
            IDependencyInjectionConfig originalConfig,
            ContainerFramework fallbackFramework)
        {
            return new DependencyInjectionConfigBuilder()
                .WithFramework(fallbackFramework)
                .WithValidation(originalConfig.EnableValidation)
                .WithDebugLogging(originalConfig.EnableDebugLogging)
                .WithPerformanceMetrics(originalConfig.EnablePerformanceMetrics)
                .WithThrowOnValidationFailure(originalConfig.ThrowOnValidationFailure)
                .WithMaxBuildTimeWarning(originalConfig.MaxBuildTimeWarningMs)
                .WithScoping(originalConfig.EnableScoping)
                .WithNamedServices(originalConfig.EnableNamedServices)
                .Build();
        }
        
        /// <summary>
        /// Initializes built-in framework factories.
        /// </summary>
        private static void InitializeBuiltInFactories()
        {
            try
            {
                // Register VContainer factory if available
                var vcontainerFactory = CreateVContainerFactory();
                if (vcontainerFactory != null)
                {
                    RegisteredFactories[ContainerFramework.VContainer] = vcontainerFactory;
                }
            }
            catch
            {
                // VContainer not available, continue without it
            }
            
            try
            {
                // Register Reflex factory if available
                var reflexFactory = CreateReflexFactory();
                if (reflexFactory != null)
                {
                    RegisteredFactories[ContainerFramework.Reflex] = reflexFactory;
                }
            }
            catch
            {
                // Reflex not available, continue without it
            }
        }
        
        /// <summary>
        /// Creates VContainer factory if VContainer is available.
        /// </summary>
        private static IContainerAdapterFactory CreateVContainerFactory()
        {
            // Check if VContainer is available through reflection
            var vcontainerType = Type.GetType("VContainer.ContainerBuilder, VContainer");
            if (vcontainerType != null)
            {
                // VContainer is available, create factory
                var factoryType = Type.GetType(
                    "AhBearStudios.Core.DependencyInjection.Adapters.VContainer.VContainerAdapterFactory, " +
                    "AhBearStudios.Core");
                
                if (factoryType != null)
                {
                    return (IContainerAdapterFactory)Activator.CreateInstance(factoryType);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates Reflex factory if Reflex is available.
        /// </summary>
        private static IContainerAdapterFactory CreateReflexFactory()
        {
            // Check if Reflex is available through reflection
            var reflexType = Type.GetType("Reflex.Core.ContainerBuilder, Reflex");
            if (reflexType != null)
            {
                // Reflex is available, create factory
                var factoryType = Type.GetType(
                    "AhBearStudios.Core.DependencyInjection.Adapters.Reflex.ReflexAdapterFactory, " +
                    "AhBearStudios.Core");
                
                if (factoryType != null)
                {
                    return (IContainerAdapterFactory)Activator.CreateInstance(factoryType);
                }
            }
            
            return null;
        }
    }
}