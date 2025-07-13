using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Builders;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Factories;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Unity.Logging.Installers;

namespace AhBearStudios.Unity.Messaging.Installers
{
    /// <summary>
    /// Reflex installer for the messaging system.
    /// Integrates with the bootstrap process and registers all messaging components.
    /// </summary>
    public sealed class MessageBusInstaller : IBootstrapInstaller
    {
        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;

        /// <inheritdoc />
        public string InstallerName => "MessageBusInstaller";

        /// <inheritdoc />
        public int Priority => 200; // After logging (100) but before most other systems

        /// <inheritdoc />
        public bool IsEnabled { get; private set; } = true;

        /// <inheritdoc />
        public Type[] Dependencies => new[]
        {
            typeof(LoggingInstaller)
        };

        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class.
        /// </summary>
        /// <param name="config">Optional message bus configuration. If null, default config will be used.</param>
        /// <param name="logger">Optional logging service for installer operations</param>
        public MessageBusInstaller(MessageBusConfig config = null, ILoggingService logger = null)
        {
            _config = config ?? MessageBusConfig.Default;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool ValidateInstaller()
        {
            try
            {
                _logger?.LogInfo("Validating MessageBusInstaller dependencies");

                // Validate that required dependencies are available
                if (!Container.HasBinding<ILoggingService>())
                {
                    _logger?.LogError("ILoggingService binding not found - required dependency missing");
                    return false;
                }

                // Validate configuration
                if (!_config.IsValid())
                {
                    _logger?.LogError("MessageBus configuration is invalid");
                    return false;
                }

                // Check for optional but recommended dependencies
                if (_config.HealthChecksEnabled && !Container.HasBinding<IHealthCheckService>())
                {
                    _logger?.LogWarning("Health checks enabled but IHealthCheckService not bound - health monitoring will be disabled");
                }

                if (_config.AlertsEnabled && !Container.HasBinding<IAlertService>())
                {
                    _logger?.LogWarning("Alerts enabled but IAlertService not bound - alerting will be disabled");
                }

                if (_config.PerformanceMonitoring && !Container.HasBinding<IProfilerService>())
                {
                    _logger?.LogWarning("Performance monitoring enabled but IProfilerService not bound - profiling will be disabled");
                }

                _logger?.LogInfo("MessageBusInstaller validation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "MessageBusInstaller validation failed");
                return false;
            }
        }

        /// <inheritdoc />
        public void PreInstall()
        {
            try
            {
                _logger?.LogInfo("Starting MessageBusInstaller pre-installation");

                // Validate system state before installation
                if (!ValidateInstaller())
                {
                    IsEnabled = false;
                    _logger?.LogError("MessageBusInstaller pre-installation validation failed - installer disabled");
                    return;
                }

                // Log configuration details
                _logger?.LogInfo($"Installing MessageBus with configuration: InstanceName={_config.InstanceName}, " +
                                $"AsyncSupport={_config.AsyncSupport}, PerformanceMonitoring={_config.PerformanceMonitoring}, " +
                                $"HealthChecks={_config.HealthChecksEnabled}, Alerts={_config.AlertsEnabled}");

                _logger?.LogInfo("MessageBusInstaller pre-installation completed");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "MessageBusInstaller pre-installation failed");
                IsEnabled = false;
                throw;
            }
        }

        /// <inheritdoc />
        public void Install(ContainerBuilder builder)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("MessageBusInstaller is disabled - skipping installation");
                return;
            }

            try
            {
                _logger?.LogInfo("Installing MessageBus components");

                // Register configuration
                builder.Bind<MessageBusConfig>().FromInstance(_config).AsSingle();

                // Register builder for runtime configuration creation
                builder.Bind<MessageBusConfigBuilder>().To<MessageBusConfigBuilder>().AsTransient();

                // Register factory
                builder.Bind<IMessageBusFactory>().To<MessageBusFactory>().AsSingle();

                // Register primary service
                builder.Bind<IMessageBusService>().To<MessageBusService>().AsSingle();

                // Register health check if health checking is enabled
                if (_config.HealthChecksEnabled && Container.HasBinding<IHealthCheckService>())
                {
                    builder.Bind<MessageBusHealthCheck>().To<MessageBusHealthCheck>().AsSingle();
                }

                // Configure MessagePipe if available
                ConfigureMessagePipe(builder);

                _logger?.LogInfo("MessageBus components installed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "MessageBusInstaller installation failed");
                throw;
            }
        }

        /// <inheritdoc />
        public void PostInstall()
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("MessageBusInstaller is disabled - skipping post-installation");
                return;
            }

            try
            {
                _logger?.LogInfo("Starting MessageBusInstaller post-installation");

                // Resolve and validate the message bus service
                var messageBusService = Container.Resolve<IMessageBusService>();
                if (messageBusService == null)
                {
                    throw new InvalidOperationException("Failed to resolve IMessageBusService after installation");
                }

                // Register health check with the health check service
                if (_config.HealthChecksEnabled && Container.HasBinding<IHealthCheckService>())
                {
                    RegisterHealthCheck();
                }

                // Validate the service is operational
                if (!messageBusService.IsOperational)
                {
                    _logger?.LogWarning("MessageBus service is not operational after installation");
                    
                    if (_config.AlertsEnabled && Container.HasBinding<IAlertService>())
                    {
                        var alertService = Container.Resolve<IAlertService>();
                        alertService?.RaiseAlert(
                            "MessageBus service is not operational after installation",
                            AlertSeverity.Critical,
                            "MessageBusInstaller",
                            "PostInstall");
                    }
                }
                else
                {
                    _logger?.LogInfo($"MessageBus service '{_config.InstanceName}' is operational");
                }

                // Log successful installation
                var statistics = messageBusService.GetStatistics();
                _logger?.LogInfo($"MessageBus installation completed: {statistics}");

                _logger?.LogInfo("MessageBusInstaller post-installation completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "MessageBusInstaller post-installation failed");
                
                if (_config.AlertsEnabled && Container.HasBinding<IAlertService>())
                {
                    try
                    {
                        var alertService = Container.Resolve<IAlertService>();
                        alertService?.RaiseAlert(
                            $"MessageBusInstaller post-installation failed: {ex.Message}",
                            AlertSeverity.Critical,
                            "MessageBusInstaller",
                            "PostInstall");
                    }
                    catch
                    {
                        // Ignore alerting failures during error handling
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Configures MessagePipe integration if available.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void ConfigureMessagePipe(ContainerBuilder builder)
        {
            try
            {
                _logger?.LogInfo("Configuring MessagePipe integration");

                // This would be implemented when MessagePipe is available
                // For now, we'll use a placeholder implementation
                
                // Example MessagePipe configuration:
                // builder.RegisterMessagePipe(options =>
                // {
                //     options.InstanceLifetime = InstanceLifetime.Singleton;
                //     options.RequestHandlerLifetime = InstanceLifetime.Singleton;
                //     options.EnableCaptureStackTrace = false; // Performance optimization
                // });

                _logger?.LogInfo("MessagePipe integration configured");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to configure MessagePipe - using fallback implementation");
                // Continue without MessagePipe if not available
            }
        }

        /// <summary>
        /// Registers the health check with the health check service.
        /// </summary>
        private void RegisterHealthCheck()
        {
            try
            {
                _logger?.LogInfo("Registering MessageBus health check");

                var healthCheckService = Container.Resolve<IHealthCheckService>();
                var messageBusService = Container.Resolve<IMessageBusService>();
                var healthCheck = new MessageBusHealthCheck(messageBusService, _config, _logger);

                healthCheckService.RegisterHealthCheck(healthCheck);

                _logger?.LogInfo("MessageBus health check registered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to register MessageBus health check");
                
                if (_config.AlertsEnabled && Container.HasBinding<IAlertService>())
                {
                    try
                    {
                        var alertService = Container.Resolve<IAlertService>();
                        alertService?.RaiseAlert(
                            $"Failed to register MessageBus health check: {ex.Message}",
                            AlertSeverity.Warning,
                            "MessageBusInstaller",
                            "HealthCheck");
                    }
                    catch
                    {
                        // Ignore alerting failures during error handling
                    }
                }
            }
        }

        /// <summary>
        /// Creates a MessageBusInstaller with high-performance configuration.
        /// </summary>
        /// <param name="logger">Optional logging service</param>
        /// <returns>Installer configured for high performance</returns>
        public static MessageBusInstaller ForHighPerformance(ILoggingService logger = null)
        {
            return new MessageBusInstaller(MessageBusConfig.HighPerformance, logger);
        }

        /// <summary>
        /// Creates a MessageBusInstaller with reliable configuration.
        /// </summary>
        /// <param name="logger">Optional logging service</param>
        /// <returns>Installer configured for reliability</returns>
        public static MessageBusInstaller ForReliability(ILoggingService logger = null)
        {
            return new MessageBusInstaller(MessageBusConfig.Reliable, logger);
        }

        /// <summary>
        /// Creates a MessageBusInstaller with custom configuration.
        /// </summary>
        /// <param name="configBuilder">Configuration builder function</param>
        /// <param name="logger">Optional logging service</param>
        /// <returns>Installer with custom configuration</returns>
        public static MessageBusInstaller WithCustomConfig(
            Func<MessageBusConfigBuilder, MessageBusConfigBuilder> configBuilder,
            ILoggingService logger = null)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            var builder = new MessageBusConfigBuilder();
            var config = configBuilder(builder).Build();
            
            return new MessageBusInstaller(config, logger);
        }
    }

    /// <summary>
    /// Bootstrap installer interface for system integration.
    /// </summary>
    public interface IBootstrapInstaller : IInstaller
    {
        /// <summary>
        /// Gets the name of this installer.
        /// </summary>
        string InstallerName { get; }

        /// <summary>
        /// Gets the installation priority (lower values install first).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether this installer is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the types this installer depends on.
        /// </summary>
        Type[] Dependencies { get; }

        /// <summary>
        /// Validates that this installer can be installed.
        /// </summary>
        /// <returns>True if installer is valid, false otherwise</returns>
        bool ValidateInstaller();

        /// <summary>
        /// Performs pre-installation setup.
        /// </summary>
        void PreInstall();

        /// <summary>
        /// Performs post-installation validation and configuration.
        /// </summary>
        void PostInstall();
    }

    /// <summary>
    /// Base installer interface.
    /// </summary>
    public interface IInstaller
    {
        /// <summary>
        /// Installs bindings into the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        void Install(ContainerBuilder builder);
    }

    /// <summary>
    /// Placeholder container builder interface.
    /// </summary>
    public interface ContainerBuilder
    {
        /// <summary>
        /// Binds an interface to an implementation.
        /// </summary>
        /// <typeparam name="TInterface">The interface type</typeparam>
        /// <returns>Binding configurator</returns>
        IBindingConfigurator<TInterface> Bind<TInterface>();
    }

    /// <summary>
    /// Placeholder binding configurator interface.
    /// </summary>
    /// <typeparam name="T">The bound type</typeparam>
    public interface IBindingConfigurator<T>
    {
        /// <summary>
        /// Specifies the implementation type.
        /// </summary>
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <returns>Lifetime configurator</returns>
        ILifetimeConfigurator<T> To<TImplementation>() where TImplementation : class, T;

        /// <summary>
        /// Binds to a specific instance.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>Lifetime configurator</returns>
        ILifetimeConfigurator<T> FromInstance(T instance);
    }

    /// <summary>
    /// Placeholder lifetime configurator interface.
    /// </summary>
    /// <typeparam name="T">The bound type</typeparam>
    public interface ILifetimeConfigurator<T>
    {
        /// <summary>
        /// Configures as singleton lifetime.
        /// </summary>
        /// <returns>The configurator</returns>
        ILifetimeConfigurator<T> AsSingle();

        /// <summary>
        /// Configures as transient lifetime.
        /// </summary>
        /// <returns>The configurator</returns>
        ILifetimeConfigurator<T> AsTransient();
    }

    /// <summary>
    /// Placeholder container interface.
    /// </summary>
    public static class Container
    {
        /// <summary>
        /// Checks if a binding exists for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>True if binding exists, false otherwise</returns>
        public static bool HasBinding<T>() => true; // Placeholder implementation

        /// <summary>
        /// Resolves an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to resolve</typeparam>
        /// <returns>The resolved instance</returns>
        public static T Resolve<T>() => default(T); // Placeholder implementation
    }
}