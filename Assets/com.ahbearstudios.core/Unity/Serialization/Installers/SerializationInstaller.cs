using System;
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Builders;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Factories;
using AhBearStudios.Core.Serialization.HealthChecks;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Services;
using AhBearStudios.Core.Infrastructure.DependencyInjection;
using AhBearStudios.Unity.Logging.Installers;
using AhBearStudios.Unity.Serialization.Components;
using AhBearStudios.Unity.Serialization.Formatters;
using AhBearStudios.Unity.Serialization.Jobs;
using AhBearStudios.Unity.Serialization.FishNet;
using Reflex.Core;
using UnityEngine;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Unity.Serialization.Installers
{
    /// <summary>
    /// Bootstrap installer for the Serialization System.
    /// Configures and registers all serialization services with the DI container.
    /// Follows Reflex DI patterns similar to LoggingInstaller.
    /// </summary>
    public class SerializationInstaller : BootstrapInstaller
    {
        [Header("Configuration")]
        [SerializeField]
        private SerializationFormat _defaultFormat = SerializationFormat.MemoryPack;
        
        [SerializeField]
        private CompressionLevel _compressionLevel = CompressionLevel.Optimal;
        
        [SerializeField]
        private SerializationMode _mode = SerializationMode.Production;
        
        [SerializeField]
        private bool _enableTypeValidation = true;
        
        [SerializeField]
        private bool _enablePerformanceMonitoring = true;
        
        [Header("FishNet Integration")]
        [SerializeField]
        private bool _enableFishNetSupport = false;

        #region IBootstrapInstaller Implementation

        /// <inheritdoc />
        public override string InstallerName => "SerializationInstaller";

        /// <inheritdoc />
        public override int Priority => 200; // After Logging (100) but before most other services

        /// <inheritdoc />
        public override Type[] Dependencies => new[] { typeof(LoggingInstaller) };

        #endregion

        private SerializationConfig _config;

        /// <summary>
        /// Initializes a new instance of SerializationInstaller.
        /// </summary>
        public SerializationInstaller()
        {
            // Configuration will be built from serialized fields
        }

        /// <summary>
        /// Initializes installer with custom configuration.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        public SerializationInstaller(SerializationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #region Validation and Setup

        /// <inheritdoc />
        protected override bool PerformValidation()
        {
            try
            {
                // Build configuration from serialized fields if not already set
                if (_config == null)
                {
                    _config = new SerializationConfigBuilder()
                        .WithFormat(_defaultFormat)
                        .WithCompression(_compressionLevel)
                        .WithMode(_mode)
                        .WithTypeValidation(_enableTypeValidation)
                        .WithPerformanceMonitoring(_enablePerformanceMonitoring)
                        .WithBufferPooling(true)
                        .WithVersioning(true)
                        .WithFishNetSupport(_enableFishNetSupport)
                        .Build();
                }

                // Validate configuration
                if (!_config.IsValid())
                {
                    LogError("Invalid serialization configuration");
                    return false;
                }

                LogDebug("Configuration validation passed");
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, "Configuration validation failed");
                return false;
            }
        }

        /// <inheritdoc />
        protected override void PerformPreInstall()
        {
            try
            {
                LogDebug("Pre-installation setup started");
                LogDebug($"Using format: {_config.Format}");
                LogDebug($"Compression: {_config.Compression}");
                LogDebug($"Mode: {_config.Mode}");
                
                // Register Unity formatters for MemoryPack
                if (_config.Format == SerializationFormat.MemoryPack)
                {
                    LogDebug("Registering Unity formatters for MemoryPack");
                    UnityFormatterRegistration.RegisterFormatters();
                }
                
                LogDebug("Pre-installation setup completed");
            }
            catch (Exception ex)
            {
                LogException(ex, "Pre-installation setup failed");
                throw;
            }
        }

        #endregion

        #region Reflex InstallBindings Implementation

        /// <inheritdoc />
        public override void InstallBindings(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Register configuration
            builder.AddSingleton(_config, typeof(SerializationConfig));

            // Register core services
            builder.AddSingleton(typeof(SerializationRegistry), typeof(ISerializationRegistry));
            builder.AddSingleton(typeof(VersioningService), typeof(IVersioningService));
            builder.AddSingleton(typeof(CompressionService), typeof(ICompressionService));

            // Register factory
            builder.AddSingleton(typeof(SerializerFactory), typeof(ISerializerFactory));

            // Register primary serializer using factory
            builder.AddSingleton<ISerializer>(container =>
            {
                var factory = container.Resolve<ISerializerFactory>();
                var config = container.Resolve<SerializationConfig>();
                
                // Register FishNet serializer with factory if enabled and available
                if (config.EnableFishNetSupport && IsFishNetAvailable())
                {
                    var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                    var adapter = container.Resolve<FishNetSerializationAdapter>();
                    var fishNetSerializer = new FishNetSerializer(loggingService, config, adapter);
                    
                    // Register with factory - this requires factory to support registration
                    if (factory is SerializerFactory serializerFactory)
                    {
                        serializerFactory.RegisterSerializer(SerializationFormat.FishNet, fishNetSerializer);
                    }
                }
                
                return factory.CreateSerializer(config);
            }, typeof(ISerializer));

            // Register ISerializationService implementation with all dependencies
            builder.AddSingleton<ISerializationService>(container =>
            {
                var config = container.Resolve<SerializationConfig>();
                var factory = container.Resolve<ISerializerFactory>();
                var registry = container.Resolve<ISerializationRegistry>();
                var versioningService = container.Resolve<IVersioningService>();
                var compressionService = container.Resolve<ICompressionService>();
                
                // Resolve optional services using safe resolution patterns (following LoggingInstaller pattern)
                var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                var healthCheckService = container.HasBinding(typeof(IHealthCheckService)) ? container.Resolve<IHealthCheckService>() : null;
                var alertService = container.HasBinding(typeof(IAlertService)) ? container.Resolve<IAlertService>() : null;
                var profilerService = container.HasBinding(typeof(IProfilerService)) ? container.Resolve<IProfilerService>() : null;
                var messageBusService = container.HasBinding(typeof(IMessageBusService)) ? container.Resolve<IMessageBusService>() : null;

                var serializationService = new SerializationService(
                    config,
                    factory,
                    registry,
                    versioningService,
                    compressionService,
                    loggingService,
                    healthCheckService,
                    alertService,
                    profilerService,
                    messageBusService);

                // Register with ServiceResolver for use in static extension methods
                ServiceResolver.Register<ISerializationService>(serializationService);
                
                return serializationService;
            }, typeof(ISerializationService));

            // Register Unity-specific services
            RegisterUnityServices(builder);

            // Register health checks
            builder.AddSingleton(typeof(SerializationHealthCheck));
            builder.AddSingleton<SerializationServiceHealthCheck>(container =>
            {
                var serializationService = container.Resolve<ISerializationService>();
                var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                return new SerializationServiceHealthCheck(serializationService, loggingService);
            }, typeof(SerializationServiceHealthCheck));

            // Register builder for runtime configuration
            builder.AddTransient<ISerializationConfigBuilder>(_ => SerializationConfigBuilder.Create(), typeof(ISerializationConfigBuilder));
            LogDebug("Serialization services registered successfully");
        }

        /// <summary>
        /// Registers Unity-specific serialization services following the LoggingInstaller pattern.
        /// Unity installers can register both Core and Unity services in a single installer.
        /// </summary>
        /// <param name="builder">Container builder</param>
        private void RegisterUnityServices(ContainerBuilder builder)
        {
            try
            {
                LogDebug("Registering Unity-specific serialization services");

                // Register Unity Job System services
                builder.AddSingleton(typeof(UnitySerializationJobService));
                
                // Register Unity compression job service if compression is enabled
                if (_config.Compression != CompressionLevel.None)
                {
                    builder.AddSingleton(typeof(UnityCompressionJobService));
                }

                // Register Unity-specific components and managers
                builder.AddTransient(typeof(PersistentDataManager));
                builder.AddTransient(typeof(SceneSerializationManager));
                builder.AddTransient(typeof(TransformSerializer));

                // Register FishNet services if enabled
                if (_config.EnableFishNetSupport && IsFishNetAvailable())
                {
                    LogDebug("Registering FishNet serialization services");
                    
                    builder.AddSingleton(typeof(FishNetTypeRegistry));
                    
                    // Register network buffer pool for FishNet serialization
                    builder.AddSingleton<AhBearStudios.Core.Pooling.Services.NetworkSerializationBufferPool>(container =>
                    {
                        var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                        var networkConfig = AhBearStudios.Core.Pooling.Configs.NetworkPoolingConfig.CreateDefault();
                        
                        var bufferPool = new AhBearStudios.Core.Pooling.Services.NetworkSerializationBufferPool(loggingService, networkConfig);
                        
                        // Register with ServiceResolver for use in static extension methods
                        ServiceResolver.Register<AhBearStudios.Core.Pooling.Services.NetworkSerializationBufferPool>(bufferPool);
                        
                        return bufferPool;
                    });
                    
                    builder.AddSingleton<FishNetSerializationAdapter>(container =>
                    {
                        var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                        var serializationService = container.Resolve<ISerializationService>();
                        var bufferPool = container.Resolve<AhBearStudios.Core.Pooling.Services.NetworkSerializationBufferPool>();
                        
                        return new FishNetSerializationAdapter(loggingService, serializationService, bufferPool);
                    });
                    
                    builder.AddSingleton<FishNetExtensionMethodGenerator>(container =>
                    {
                        var loggingService = container.HasBinding(typeof(ILoggingService)) ? container.Resolve<ILoggingService>() : null;
                        var typeRegistry = container.Resolve<FishNetTypeRegistry>();
                        var serializationService = container.Resolve<ISerializationService>();
                        var options = _config.FishNetOptions;
                        
                        return new FishNetExtensionMethodGenerator(loggingService, typeRegistry, serializationService, options);
                    });
                    
                    LogDebug("FishNet services registered successfully");
                }
                else if (_config.EnableFishNetSupport)
                {
                    LogWarning("FishNet support enabled but FishNet not available in project");
                }

                LogDebug("Unity-specific services registered successfully");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to register Unity-specific services");
                throw;
            }
        }

        #endregion

        #region Post-Installation

        /// <inheritdoc />
        protected override void PerformPostInstall(Container container)
        {
            try
            {
                LogDebug("Starting post-installation setup");

                // Validate serializer creation
                var serializer = container.Resolve<ISerializer>();
                if (serializer == null)
                {
                    throw new InvalidOperationException("Failed to resolve ISerializer");
                }

                var serializationService = container.Resolve<ISerializationService>();
                if (serializationService == null)
                {
                    throw new InvalidOperationException("Failed to resolve ISerializationService");
                }

                // Register health checks with health check service (if available)
                if (container.HasBinding(typeof(IHealthCheckService)))
                {
                    var healthCheckService = container.Resolve<IHealthCheckService>();
                    var serializationHealthCheck = container.Resolve<SerializationHealthCheck>();
                    var serializationServiceHealthCheck = container.Resolve<SerializationServiceHealthCheck>();
                    
                    healthCheckService.RegisterHealthCheck(serializationHealthCheck);
                    healthCheckService.RegisterHealthCheck(serializationServiceHealthCheck);
                    LogDebug("Health checks registered");
                }

                // Test basic functionality with service
                serializationService.RegisterType<string>();
                var testData = "SerializationServiceTest";
                var serialized = serializationService.Serialize(testData);
                var deserialized = serializationService.Deserialize<string>(serialized);

                if (testData != deserialized)
                {
                    throw new InvalidOperationException("SerializationService validation failed");
                }

                // Validate health check
                if (!serializationService.PerformHealthCheck())
                {
                    throw new InvalidOperationException("SerializationService health check failed");
                }

                LogDebug("SerializationService validation completed successfully");

                // Test individual serializer as well
                serializer.RegisterType<string>();
                var individualTest = "IndividualSerializerTest";
                var individualSerialized = serializer.Serialize(individualTest);
                var individualDeserialized = serializer.Deserialize<string>(individualSerialized);

                if (individualTest != individualDeserialized)
                {
                    throw new InvalidOperationException("Individual serializer validation failed");
                }

                LogDebug("Individual serializer validation completed successfully");

                // Log final statistics
                var serviceStatistics = serializationService.GetStatistics();
                var serializerStatistics = serializer.GetStatistics();
                LogDebug($"Service registered formats: {serializationService.GetRegisteredFormats().Count}");
                LogDebug($"Serializer registered types: {serializerStatistics.RegisteredTypeCount}");

                LogDebug("Post-installation completed successfully");
            }
            catch (Exception ex)
            {
                LogException(ex, "Post-installation failed");
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Creates an installer with development-optimized configuration.
        /// </summary>
        /// <returns>Development-configured installer</returns>
        public static SerializationInstaller ForDevelopment()
        {
            var config = new SerializationConfigBuilder()
                .WithFormat(SerializationFormat.MemoryPack)
                .WithCompression(CompressionLevel.Fastest)
                .WithMode(SerializationMode.Development)
                .WithTypeValidation(true)
                .WithPerformanceMonitoring(true)
                .WithBufferPooling(true, 512 * 1024) // Smaller pool for development
                .WithVersioning(true, strictMode: false)
                .Build();

            return new SerializationInstaller(config);
        }

        /// <summary>
        /// Creates an installer with production-optimized configuration.
        /// </summary>
        /// <returns>Production-configured installer</returns>
        public static SerializationInstaller ForProduction()
        {
            var config = new SerializationConfigBuilder()
                .WithFormat(SerializationFormat.MemoryPack)
                .WithCompression(CompressionLevel.Optimal)
                .WithMode(SerializationMode.Production)
                .WithTypeValidation(true)
                .WithPerformanceMonitoring(true)
                .WithBufferPooling(true, 2 * 1024 * 1024) // Larger pool for production
                .WithVersioning(true, strictMode: true)
                .WithMaxConcurrentOperations(Environment.ProcessorCount * 4)
                .Build();

            return new SerializationInstaller(config);
        }

        /// <summary>
        /// Creates an installer with debug-optimized configuration.
        /// </summary>
        /// <returns>Debug-configured installer</returns>
        public static SerializationInstaller ForDebug()
        {
            var config = new SerializationConfigBuilder()
                .WithFormat(SerializationFormat.Json) // Human-readable for debugging
                .WithCompression(CompressionLevel.None)
                .WithMode(SerializationMode.Debug)
                .WithTypeValidation(true)
                .WithPerformanceMonitoring(true)
                .WithBufferPooling(false) // Disable pooling for easier debugging
                .WithVersioning(true, strictMode: false)
                .WithMaxConcurrentOperations(1) // Single-threaded for debugging
                .Build();

            return new SerializationInstaller(config);
        }

        /// <summary>
        /// Updates the configuration for this installer.
        /// Must be called before installation.
        /// </summary>
        /// <param name="config">New configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when called after installation</exception>
        public void UpdateConfiguration(SerializationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Updates the configuration using a builder pattern.
        /// Must be called before installation.
        /// </summary>
        /// <param name="configureBuilder">Configuration builder action</param>
        /// <exception cref="ArgumentNullException">Thrown when configureBuilder is null</exception>
        public void UpdateConfiguration(Action<ISerializationConfigBuilder> configureBuilder)
        {
            if (configureBuilder == null)
                throw new ArgumentNullException(nameof(configureBuilder));

            var builder = SerializationConfigBuilder.FromConfig(_config);
            configureBuilder(builder);
            _config = builder.Build();
        }
        
        /// <summary>
        /// Checks if FishNet is available in the project by looking for FishNet assemblies.
        /// </summary>
        /// <returns>True if FishNet is available</returns>
        private static bool IsFishNetAvailable()
        {
            try
            {
                // Try to find FishNet assemblies
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.GetName().Name.Contains("FishNet"));
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Extension methods for convenient serialization service registration.
    /// Following Reflex patterns for service registration.
    /// </summary>
    public static class SerializationServiceExtensions
    {
        /// <summary>
        /// Adds serialization services to the container builder.
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="config">Serialization configuration</param>
        /// <returns>Container builder for chaining</returns>
        public static ContainerBuilder AddSerialization(this ContainerBuilder builder, SerializationConfig config = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var installer = config != null ? new SerializationInstaller(config) : new SerializationInstaller();
            
            // Call InstallBindings method to register all services
            installer.InstallBindings(builder);

            return builder;
        }

        /// <summary>
        /// Adds serialization services with custom configuration.
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="configureBuilder">Configuration builder action</param>
        /// <returns>Container builder for chaining</returns>
        public static ContainerBuilder AddSerialization(this ContainerBuilder builder, Action<ISerializationConfigBuilder> configureBuilder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (configureBuilder == null)
                throw new ArgumentNullException(nameof(configureBuilder));

            var configBuilder = SerializationConfigBuilder.Create();
            configureBuilder(configBuilder);
            var config = configBuilder.Build();

            return builder.AddSerialization(config);
        }

        /// <summary>
        /// Adds development-optimized serialization services.
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <returns>Container builder for chaining</returns>
        public static ContainerBuilder AddSerializationForDevelopment(this ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var installer = SerializationInstaller.ForDevelopment();
            installer.InstallBindings(builder);

            return builder;
        }

        /// <summary>
        /// Adds production-optimized serialization services.
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <returns>Container builder for chaining</returns>
        public static ContainerBuilder AddSerializationForProduction(this ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var installer = SerializationInstaller.ForProduction();
            installer.InstallBindings(builder);

            return builder;
        }
    }
}