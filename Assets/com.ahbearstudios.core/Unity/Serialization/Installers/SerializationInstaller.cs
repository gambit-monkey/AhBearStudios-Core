using System;
using AhBearStudios.Core.Bootstrap;
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Builders;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Factories;
using AhBearStudios.Core.Serialization.HealthChecks;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Services;
using Reflex.Core;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Unity.Serialization.Installers
{
    /// <summary>
    /// Bootstrap installer for the Serialization System.
    /// Configures and registers all serialization services with the DI container.
    /// </summary>
    public class SerializationInstaller : IBootstrapInstaller
    {
        /// <inheritdoc />
        public string InstallerName => "SerializationInstaller";

        /// <inheritdoc />
        public int Priority => 200; // After Logging (100) but before most other services

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public Type[] Dependencies => new[] { typeof(LoggingInstaller) };

        private SerializationConfig _config;

        /// <summary>
        /// Initializes a new instance of SerializationInstaller.
        /// </summary>
        public SerializationInstaller()
        {
            // Create default configuration
            _config = new SerializationConfigBuilder()
                .WithFormat(SerializationFormat.MemoryPack)
                .WithCompression(CompressionLevel.Optimal)
                .WithMode(SerializationMode.Production)
                .WithTypeValidation(true)
                .WithPerformanceMonitoring(true)
                .WithBufferPooling(true)
                .WithVersioning(true)
                .Build();
        }

        /// <summary>
        /// Initializes installer with custom configuration.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        public SerializationInstaller(SerializationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public bool ValidateInstaller()
        {
            // Validate that required dependencies are available
            if (!Container.HasBinding<ILoggingService>())
            {
                UnityEngine.Debug.LogError($"[{InstallerName}] Missing required dependency: ILoggingService");
                return false;
            }

            // Validate configuration
            if (!_config.IsValid())
            {
                UnityEngine.Debug.LogError($"[{InstallerName}] Invalid serialization configuration");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public void PreInstall()
        {
            var logger = Container.Resolve<ILoggingService>();
            logger?.LogInfo($"[{InstallerName}] Pre-installation started", GetCorrelationId());

            // Log configuration details
            logger?.LogInfo($"[{InstallerName}] Using format: {_config.Format}", GetCorrelationId());
            logger?.LogInfo($"[{InstallerName}] Compression: {_config.Compression}", GetCorrelationId());
            logger?.LogInfo($"[{InstallerName}] Mode: {_config.Mode}", GetCorrelationId());
        }

        /// <inheritdoc />
        public void Install(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Register configuration
            builder.Bind<SerializationConfig>().FromInstance(_config);

            // Register core services
            builder.Bind<ISerializationRegistry>().To<SerializationRegistry>().AsSingle();
            builder.Bind<IVersioningService>().To<VersioningService>().AsSingle();
            builder.Bind<ICompressionService>().To<CompressionService>().AsSingle();

            // Register factory
            builder.Bind<ISerializerFactory>().To<SerializerFactory>().AsSingle();

            // Register primary serializer using factory
            builder.Bind<ISerializer>().FromFunction(container =>
            {
                var factory = container.Resolve<ISerializerFactory>();
                var config = container.Resolve<SerializationConfig>();
                return factory.CreateSerializer(config);
            }).AsSingle();

            // Register health check
            builder.Bind<SerializationHealthCheck>().To<SerializationHealthCheck>().AsSingle();

            // Register builder for runtime configuration
            builder.Bind<ISerializationConfigBuilder>().FromFunction(_ => SerializationConfigBuilder.Create()).AsTransient();

            var logger = Container.Resolve<ILoggingService>();
            logger?.LogInfo($"[{InstallerName}] Services registered successfully", GetCorrelationId());
        }

        /// <inheritdoc />
        public void PostInstall()
        {
            var logger = Container.Resolve<ILoggingService>();
            var correlationId = GetCorrelationId();

            logger?.LogInfo($"[{InstallerName}] Post-installation started", correlationId);

            try
            {
                // Register health check with health check service
                if (Container.HasBinding<IHealthCheckService>())
                {
                    var healthCheckService = Container.Resolve<IHealthCheckService>();
                    var serializationHealthCheck = Container.Resolve<SerializationHealthCheck>();
                    
                    healthCheckService.RegisterHealthCheck(serializationHealthCheck);
                    logger?.LogInfo($"[{InstallerName}] Health check registered", correlationId);
                }
                else
                {
                    logger?.LogWarning($"[{InstallerName}] HealthCheckService not available, skipping health check registration", correlationId);
                }

                // Validate serializer creation
                var serializer = Container.Resolve<ISerializer>();
                if (serializer == null)
                {
                    throw new InvalidOperationException("Failed to resolve ISerializer");
                }

                // Test basic functionality
                serializer.RegisterType<string>();
                var testData = "SerializationTest";
                var serialized = serializer.Serialize(testData);
                var deserialized = serializer.Deserialize<string>(serialized);

                if (testData != deserialized)
                {
                    throw new InvalidOperationException("Serialization validation failed");
                }

                logger?.LogInfo($"[{InstallerName}] Serializer validation completed successfully", correlationId);

                // Log final statistics
                var statistics = serializer.GetStatistics();
                logger?.LogInfo($"[{InstallerName}] Registered types: {statistics.RegisteredTypeCount}", correlationId);

                logger?.LogInfo($"[{InstallerName}] Installation completed successfully", correlationId);
            }
            catch (Exception ex)
            {
                logger?.LogException($"[{InstallerName}] Post-installation failed", ex, correlationId);
                throw;
            }
        }

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
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
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

        private Unity.Collections.FixedString64Bytes GetCorrelationId()
        {
            return new Unity.Collections.FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }

    /// <summary>
    /// Extension methods for convenient serialization service registration.
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
            installer.Install(builder);

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
            installer.Install(builder);

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
            installer.Install(builder);

            return builder;
        }
    }
}