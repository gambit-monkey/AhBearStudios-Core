using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Complete implementation of IAlertServiceConfigurationBuilder that provides fluent interface for building alert service configurations.
    /// Supports all configuration options with comprehensive validation and environment-specific presets.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// </summary>
    public sealed class AlertServiceConfigurationBuilder : IAlertServiceConfigurationBuilder
    {
        #region Private Fields

        private FixedString64Bytes _serviceName = "DefaultAlertService";
        private AlertConfig _alertConfig = new AlertConfig();
        private TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(10);
        private bool _autoStart = true;
        private bool _validateOnStartup = true;
        private AlertEnvironmentType _environment = AlertEnvironmentType.Development;
        private bool _enableUnityIntegration = true;
        private bool _enableHealthReporting = true;
        private TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private bool _enableMetrics = true;
        private TimeSpan _metricsInterval = TimeSpan.FromSeconds(30);
        private bool _enableTelemetry = true;
        private readonly Dictionary<string, string> _serviceTags = new();
        private bool _autoResolveDependencies = true;
        private bool _requireAllDependencies = false;
        private TimeSpan _dependencyResolutionTimeout = TimeSpan.FromSeconds(5);
        private int _maxMemoryUsageMB = 100;
        private int _maxConcurrentOperations = 50;
        private int _maxQueuedAlerts = 1000;
        private bool _continueOnError = true;
        private int _maxConsecutiveErrors = 10;
        private TimeSpan _errorWindow = TimeSpan.FromMinutes(5);
        private bool _logErrors = true;
        private bool _escalateCriticalErrors = true;
        private bool _autoDisposeOnShutdown = true;
        private Guid _correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceConfigurationBuilder", "Initialization");

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AlertServiceConfigurationBuilder with default values.
        /// </summary>
        public AlertServiceConfigurationBuilder()
        {
        }

        #endregion

        #region IAlertServiceConfigurationBuilder Implementation

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithServiceName(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be null or whitespace.", nameof(serviceName));

            _serviceName = serviceName;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithAlertConfig(AlertConfig alertConfig)
        {
            _alertConfig = alertConfig ?? throw new ArgumentNullException(nameof(alertConfig));
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithAlertConfig(Action<IAlertConfigBuilder> configAction)
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var builder = new AlertConfigBuilder();
            configAction(builder);
            _alertConfig = builder.Build();
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithStartupTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Startup timeout must be greater than zero.");

            _startupTimeout = timeout;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithShutdownTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Shutdown timeout must be greater than zero.");

            _shutdownTimeout = timeout;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithAutoStart(bool autoStart)
        {
            _autoStart = autoStart;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithValidateOnStartup(bool validateOnStartup)
        {
            _validateOnStartup = validateOnStartup;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithEnvironment(AlertEnvironmentType environment)
        {
            if (!Enum.IsDefined(typeof(AlertEnvironmentType), environment))
                throw new ArgumentException("Invalid environment type.", nameof(environment));

            _environment = environment;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithUnityIntegration(bool enableUnityIntegration)
        {
            _enableUnityIntegration = enableUnityIntegration;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithHealthReporting(bool enableHealthReporting)
        {
            _enableHealthReporting = enableHealthReporting;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithHealthCheckInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Health check interval must be greater than zero.");

            _healthCheckInterval = interval;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithMetrics(bool enableMetrics)
        {
            _enableMetrics = enableMetrics;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithMetricsInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Metrics interval must be greater than zero.");

            _metricsInterval = interval;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithTelemetry(bool enableTelemetry)
        {
            _enableTelemetry = enableTelemetry;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithServiceTags(IDictionary<string, string> tags)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            _serviceTags.Clear();
            foreach (var kvp in tags)
            {
                _serviceTags[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithServiceTag(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Tag key cannot be null or whitespace.", nameof(key));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Tag value cannot be null or whitespace.", nameof(value));

            _serviceTags[key] = value;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithDependencyResolution(bool autoResolve, bool requireAll = false, TimeSpan? timeout = null)
        {
            _autoResolveDependencies = autoResolve;
            _requireAllDependencies = requireAll;

            if (timeout.HasValue)
            {
                if (timeout.Value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(timeout), "Dependency resolution timeout must be greater than zero.");
                _dependencyResolutionTimeout = timeout.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithResourceLimits(int maxMemoryMB, int maxConcurrentOperations, int maxQueuedAlerts)
        {
            if (maxMemoryMB <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxMemoryMB), "Max memory usage must be greater than zero.");
            if (maxConcurrentOperations <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentOperations), "Max concurrent operations must be greater than zero.");
            if (maxQueuedAlerts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxQueuedAlerts), "Max queued alerts must be greater than zero.");

            _maxMemoryUsageMB = maxMemoryMB;
            _maxConcurrentOperations = maxConcurrentOperations;
            _maxQueuedAlerts = maxQueuedAlerts;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithErrorHandling(bool continueOnError = true, int maxConsecutiveErrors = 10, TimeSpan? errorWindow = null, bool logErrors = true, bool escalateCriticalErrors = true)
        {
            if (maxConsecutiveErrors <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConsecutiveErrors), "Max consecutive errors must be greater than zero.");

            _continueOnError = continueOnError;
            _maxConsecutiveErrors = maxConsecutiveErrors;
            _logErrors = logErrors;
            _escalateCriticalErrors = escalateCriticalErrors;

            if (errorWindow.HasValue)
            {
                if (errorWindow.Value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(errorWindow), "Error window must be greater than zero.");
                _errorWindow = errorWindow.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithAutoDisposeOnShutdown(bool autoDispose)
        {
            _autoDisposeOnShutdown = autoDispose;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder WithCorrelationId(Guid correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder ForDevelopment()
        {
            return WithEnvironment(AlertEnvironmentType.Development)
                .WithServiceName("DevAlertService")
                .WithAutoStart(true)
                .WithValidateOnStartup(true)
                .WithUnityIntegration(true)
                .WithHealthReporting(true)
                .WithHealthCheckInterval(TimeSpan.FromMinutes(2))
                .WithMetrics(true)
                .WithMetricsInterval(TimeSpan.FromMinutes(1))
                .WithTelemetry(true)
                .WithResourceLimits(50, 25, 500)
                .WithErrorHandling(continueOnError: true, maxConsecutiveErrors: 5, errorWindow: TimeSpan.FromMinutes(2), logErrors: true, escalateCriticalErrors: false)
                .WithAlertConfig(builder => builder
                    .WithMinimumSeverity(AlertSeverity.Debug)
                    .WithSuppression(false)
                    .WithAsyncProcessing(true)
                    .WithUnityIntegration(true)
                    .WithMetrics(true));
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder ForProduction()
        {
            return WithEnvironment(AlertEnvironmentType.Production)
                .WithServiceName("ProdAlertService")
                .WithAutoStart(true)
                .WithValidateOnStartup(true)
                .WithUnityIntegration(true)
                .WithHealthReporting(true)
                .WithHealthCheckInterval(TimeSpan.FromMinutes(5))
                .WithMetrics(true)
                .WithMetricsInterval(TimeSpan.FromMinutes(5))
                .WithTelemetry(false) // Reduced telemetry in production
                .WithResourceLimits(200, 100, 2000)
                .WithErrorHandling(continueOnError: true, maxConsecutiveErrors: 20, errorWindow: TimeSpan.FromMinutes(10), logErrors: true, escalateCriticalErrors: true)
                .WithAlertConfig(builder => builder
                    .WithMinimumSeverity(AlertSeverity.Warning)
                    .WithSuppression(true, TimeSpan.FromMinutes(10))
                    .WithAsyncProcessing(true)
                    .WithUnityIntegration(true)
                    .WithMetrics(true));
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder ForTesting()
        {
            return WithEnvironment(AlertEnvironmentType.Testing)
                .WithServiceName("TestAlertService")
                .WithAutoStart(false) // Manual start in tests
                .WithValidateOnStartup(false) // Allow invalid configs for testing
                .WithUnityIntegration(false)
                .WithHealthReporting(false)
                .WithMetrics(false)
                .WithTelemetry(false)
                .WithStartupTimeout(TimeSpan.FromSeconds(5))
                .WithShutdownTimeout(TimeSpan.FromSeconds(2))
                .WithResourceLimits(10, 5, 50)
                .WithErrorHandling(continueOnError: false, maxConsecutiveErrors: 1, errorWindow: TimeSpan.FromSeconds(30), logErrors: false, escalateCriticalErrors: false)
                .WithAlertConfig(builder => builder
                    .WithMinimumSeverity(AlertSeverity.Debug)
                    .WithSuppression(false)
                    .WithAsyncProcessing(false) // Synchronous for predictable testing
                    .WithUnityIntegration(false)
                    .WithMetrics(false)
                    .WithMaxConcurrentAlerts(10)
                    .WithProcessingTimeout(TimeSpan.FromSeconds(5)));
        }

        /// <inheritdoc />
        public AlertServiceConfiguration Build()
        {
            var config = new AlertServiceConfiguration
            {
                ServiceName = _serviceName,
                AlertConfig = _alertConfig,
                StartupTimeout = _startupTimeout,
                ShutdownTimeout = _shutdownTimeout,
                AutoStart = _autoStart,
                ValidateOnStartup = _validateOnStartup,
                Environment = _environment,
                EnableUnityIntegration = _enableUnityIntegration,
                EnableHealthReporting = _enableHealthReporting,
                HealthCheckInterval = _healthCheckInterval,
                EnableMetrics = _enableMetrics,
                MetricsInterval = _metricsInterval,
                EnableTelemetry = _enableTelemetry,
                ServiceTags = new Dictionary<string, string>(_serviceTags),
                AutoResolveDependencies = _autoResolveDependencies,
                RequireAllDependencies = _requireAllDependencies,
                DependencyResolutionTimeout = _dependencyResolutionTimeout,
                MaxMemoryUsageMB = _maxMemoryUsageMB,
                MaxConcurrentOperations = _maxConcurrentOperations,
                MaxQueuedAlerts = _maxQueuedAlerts,
                ContinueOnError = _continueOnError,
                MaxConsecutiveErrors = _maxConsecutiveErrors,
                ErrorWindow = _errorWindow,
                LogErrors = _logErrors,
                EscalateCriticalErrors = _escalateCriticalErrors,
                AutoDisposeOnShutdown = _autoDisposeOnShutdown,
                CorrelationId = _correlationId
            };

            // Validate the configuration before returning
            config.Validate();
            return config;
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            try
            {
                Build();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public IAlertServiceConfigurationBuilder Reset()
        {
            _serviceName = "DefaultAlertService";
            _alertConfig = new AlertConfig();
            _startupTimeout = TimeSpan.FromSeconds(30);
            _shutdownTimeout = TimeSpan.FromSeconds(10);
            _autoStart = true;
            _validateOnStartup = true;
            _environment = AlertEnvironmentType.Development;
            _enableUnityIntegration = true;
            _enableHealthReporting = true;
            _healthCheckInterval = TimeSpan.FromMinutes(1);
            _enableMetrics = true;
            _metricsInterval = TimeSpan.FromSeconds(30);
            _enableTelemetry = true;
            _serviceTags.Clear();
            _autoResolveDependencies = true;
            _requireAllDependencies = false;
            _dependencyResolutionTimeout = TimeSpan.FromSeconds(5);
            _maxMemoryUsageMB = 100;
            _maxConcurrentOperations = 50;
            _maxQueuedAlerts = 1000;
            _continueOnError = true;
            _maxConsecutiveErrors = 10;
            _errorWindow = TimeSpan.FromMinutes(5);
            _logErrors = true;
            _escalateCriticalErrors = true;
            _autoDisposeOnShutdown = true;
            _correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceConfigurationBuilder.Reset", "Reset");

            return this;
        }

        #endregion
    }
}