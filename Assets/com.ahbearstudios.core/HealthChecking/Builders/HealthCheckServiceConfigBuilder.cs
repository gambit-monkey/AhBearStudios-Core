using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.HealthChecking.Builders
{
    /// <summary>
    /// Production-ready builder for HealthCheckServiceConfig with comprehensive configuration options
    /// </summary>
    public sealed class HealthCheckServiceConfigBuilder
    {
        private readonly ILoggingService _logger;
        private readonly List<string> _validationErrors = new();
        
        // Core configuration properties
        private TimeSpan _defaultCheckInterval = TimeSpan.FromSeconds(30);
        private int _maxConcurrentChecks = 10;
        private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private bool _enableAutomaticChecks = true;
        private bool _enableCircuitBreakers = true;
        private bool _enableGracefulDegradation = true;
        private int _maxHistoryPerCheck = 100;
        
        // Alert configuration
        private bool _enableHealthAlerts = true;
        private bool _enableCircuitBreakerAlerts = true;
        private bool _enableDegradationAlerts = true;
        private Dictionary<HealthStatus, AlertSeverity> _alertSeverities = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Low },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };
        private HashSet<FixedString64Bytes> _alertTags = new() { "HealthCheck", "SystemMonitoring" };
        
        // Threshold configurations
        private HealthThresholds _healthThresholds = new();
        private DegradationThresholds _degradationThresholds = new();
        private CircuitBreakerConfig _defaultCircuitBreakerConfig = new();
        
        // Logging and profiling
        private bool _enableHealthCheckLogging = true;
        private LogLevel _healthCheckLogLevel = LogLevel.Info;
        private bool _enableProfiling = true;
        private int _slowHealthCheckThreshold = 1000;
        
        // Build state tracking
        private bool _isBuilt = false;
        private bool _isValidated = false;

        /// <summary>
        /// Initializes a new instance of the HealthCheckServiceConfigBuilder class
        /// </summary>
        /// <param name="logger">Logging service for build operations</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public HealthCheckServiceConfigBuilder(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("HealthCheckServiceConfigBuilder initialized");
        }

        /// <summary>
        /// Sets the default interval between automatic health checks
        /// </summary>
        /// <param name="interval">Check interval (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is not positive</exception>
        public HealthCheckServiceConfigBuilder WithDefaultCheckInterval(TimeSpan interval)
        {
            ThrowIfAlreadyBuilt();
            
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Check interval must be positive");
            
            if (interval > TimeSpan.FromHours(24))
                _logger.LogWarning($"Very long check interval specified: {interval}. Consider if this is intended.");
            
            _defaultCheckInterval = interval;
            _logger.LogDebug($"Set default check interval to {interval}");
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent health checks
        /// </summary>
        /// <param name="maxConcurrent">Maximum concurrent checks (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxConcurrent is not positive</exception>
        public HealthCheckServiceConfigBuilder WithMaxConcurrentChecks(int maxConcurrent)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxConcurrent <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent checks must be positive");
            
            if (maxConcurrent > 100)
                _logger.LogWarning($"Very high concurrent check limit: {maxConcurrent}. Monitor system resources.");
            
            _maxConcurrentChecks = maxConcurrent;
            _logger.LogDebug($"Set max concurrent checks to {maxConcurrent}");
            return this;
        }

        /// <summary>
        /// Sets the default timeout for health checks
        /// </summary>
        /// <param name="timeout">Default timeout (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is not positive</exception>
        public HealthCheckServiceConfigBuilder WithDefaultTimeout(TimeSpan timeout)
        {
            ThrowIfAlreadyBuilt();
            
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive");
            
            if (timeout > TimeSpan.FromMinutes(10))
                _logger.LogWarning($"Very long timeout specified: {timeout}. This may affect system responsiveness.");
            
            _defaultTimeout = timeout;
            _logger.LogDebug($"Set default timeout to {timeout}");
            return this;
        }

        /// <summary>
        /// Enables or disables automatic health checks
        /// </summary>
        /// <param name="enabled">Whether to enable automatic checks</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithAutomaticChecks(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableAutomaticChecks = enabled;
            _logger.LogDebug($"Automatic checks {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Enables or disables circuit breaker functionality
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breakers</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithCircuitBreakers(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableCircuitBreakers = enabled;
            _logger.LogDebug($"Circuit breakers {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Enables or disables graceful degradation
        /// </summary>
        /// <param name="enabled">Whether to enable graceful degradation</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithGracefulDegradation(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableGracefulDegradation = enabled;
            _logger.LogDebug($"Graceful degradation {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Sets the maximum history to keep per health check
        /// </summary>
        /// <param name="maxHistory">Maximum history entries (must be non-negative)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHistory is negative</exception>
        public HealthCheckServiceConfigBuilder WithMaxHistoryPerCheck(int maxHistory)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxHistory < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHistory), "Max history must be non-negative");
            
            if (maxHistory > 10000)
                _logger.LogWarning($"Very large history size: {maxHistory}. This may impact memory usage.");
            
            _maxHistoryPerCheck = maxHistory;
            _logger.LogDebug($"Set max history per check to {maxHistory}");
            return this;
        }

        /// <summary>
        /// Configures health status alerting
        /// </summary>
        /// <param name="enabled">Whether to enable health alerts</param>
        /// <param name="severities">Custom alert severities for health statuses</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithHealthAlerts(
            bool enabled = true,
            Dictionary<HealthStatus, AlertSeverity> severities = null)
        {
            ThrowIfAlreadyBuilt();
            
            _enableHealthAlerts = enabled;
            
            if (severities != null)
            {
                ValidateAlertSeverities(severities);
                _alertSeverities = new Dictionary<HealthStatus, AlertSeverity>(severities);
            }
            
            _logger.LogDebug($"Health alerts {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Configures circuit breaker alerting
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithCircuitBreakerAlerts(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableCircuitBreakerAlerts = enabled;
            _logger.LogDebug($"Circuit breaker alerts {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Configures degradation alerting
        /// </summary>
        /// <param name="enabled">Whether to enable degradation alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithDegradationAlerts(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableDegradationAlerts = enabled;
            _logger.LogDebug($"Degradation alerts {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Sets custom alert tags
        /// </summary>
        /// <param name="tags">Tags to apply to alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public HealthCheckServiceConfigBuilder WithAlertTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            _alertTags = new HashSet<FixedString64Bytes>(tags);
            _logger.LogDebug($"Set {tags.Length} alert tags");
            return this;
        }

        /// <summary>
        /// Adds alert tags to existing tags
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public HealthCheckServiceConfigBuilder AddAlertTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            foreach (var tag in tags)
            {
                _alertTags.Add(tag);
            }
            
            _logger.LogDebug($"Added {tags.Length} alert tags");
            return this;
        }

        /// <summary>
        /// Sets health thresholds configuration
        /// </summary>
        /// <param name="thresholds">Health thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when thresholds is null</exception>
        public HealthCheckServiceConfigBuilder WithHealthThresholds(HealthThresholds thresholds)
        {
            ThrowIfAlreadyBuilt();
            
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));
            
            var validationErrors = thresholds.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid health thresholds: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(thresholds));
            }
            
            _healthThresholds = thresholds;
            _logger.LogDebug("Set custom health thresholds");
            return this;
        }

        /// <summary>
        /// Sets degradation thresholds configuration
        /// </summary>
        /// <param name="thresholds">Degradation thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when thresholds is null</exception>
        public HealthCheckServiceConfigBuilder WithDegradationThresholds(DegradationThresholds thresholds)
        {
            ThrowIfAlreadyBuilt();
            
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));
            
            var validationErrors = thresholds.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid degradation thresholds: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(thresholds));
            }
            
            _degradationThresholds = thresholds;
            _logger.LogDebug("Set custom degradation thresholds");
            return this;
        }

        /// <summary>
        /// Sets default circuit breaker configuration
        /// </summary>
        /// <param name="config">Default circuit breaker configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public HealthCheckServiceConfigBuilder WithDefaultCircuitBreakerConfig(CircuitBreakerConfig config)
        {
            ThrowIfAlreadyBuilt();
            
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var validationErrors = config.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid circuit breaker config: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(config));
            }
            
            _defaultCircuitBreakerConfig = config;
            _logger.LogDebug("Set custom default circuit breaker configuration");
            return this;
        }

        /// <summary>
        /// Configures health check logging
        /// </summary>
        /// <param name="enabled">Whether to enable health check logging</param>
        /// <param name="logLevel">Log level for health check operations</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder WithHealthCheckLogging(bool enabled = true, LogLevel logLevel = LogLevel.Info)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(LogLevel), logLevel))
                throw new ArgumentException($"Invalid log level: {logLevel}", nameof(logLevel));
            
            _enableHealthCheckLogging = enabled;
            _healthCheckLogLevel = logLevel;
            _logger.LogDebug($"Health check logging {(enabled ? "enabled" : "disabled")} at level {logLevel}");
            return this;
        }

        /// <summary>
        /// Configures performance profiling
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        /// <param name="slowThreshold">Threshold for slow health checks in milliseconds</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when slowThreshold is negative</exception>
        public HealthCheckServiceConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000)
        {
            ThrowIfAlreadyBuilt();
            
            if (slowThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(slowThreshold), "Slow threshold must be non-negative");
            
            _enableProfiling = enabled;
            _slowHealthCheckThreshold = slowThreshold;
            _logger.LogDebug($"Profiling {(enabled ? "enabled" : "disabled")} with slow threshold {slowThreshold}ms");
            return this;
        }

        /// <summary>
        /// Applies a preset configuration for the specified environment
        /// </summary>
        /// <param name="environment">Target environment</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder ForEnvironment(HealthCheckEnvironment environment)
        {
            ThrowIfAlreadyBuilt();
            
            switch (environment)
            {
                case HealthCheckEnvironment.Development:
                    return ApplyDevelopmentPreset();
                
                case HealthCheckEnvironment.Testing:
                    return ApplyTestingPreset();
                
                case HealthCheckEnvironment.Staging:
                    return ApplyStagingPreset();
                
                case HealthCheckEnvironment.Production:
                    return ApplyProductionPreset();
                
                default:
                    throw new ArgumentException($"Unknown environment: {environment}", nameof(environment));
            }
        }

        /// <summary>
        /// Validates the current configuration without building
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            if (_isValidated)
                return new List<string>(_validationErrors);
            
            _validationErrors.Clear();
            
            // Validate basic configuration
            if (_defaultCheckInterval <= TimeSpan.Zero)
                _validationErrors.Add("Default check interval must be positive");
            
            if (_maxConcurrentChecks <= 0)
                _validationErrors.Add("Max concurrent checks must be positive");
            
            if (_defaultTimeout <= TimeSpan.Zero)
                _validationErrors.Add("Default timeout must be positive");
            
            if (_maxHistoryPerCheck < 0)
                _validationErrors.Add("Max history per check must be non-negative");
            
            if (_slowHealthCheckThreshold < 0)
                _validationErrors.Add("Slow health check threshold must be non-negative");
            
            // Validate nested configurations
            _validationErrors.AddRange(_healthThresholds.Validate());
            _validationErrors.AddRange(_degradationThresholds.Validate());
            _validationErrors.AddRange(_defaultCircuitBreakerConfig.Validate());
            
            // Validate alert severities
            ValidateAlertSeverities(_alertSeverities);
            
            _isValidated = true;
            
            if (_validationErrors.Count > 0)
            {
                _logger.LogWarning($"Configuration validation found {_validationErrors.Count} errors");
            }
            else
            {
                _logger.LogDebug("Configuration validation passed");
            }
            
            return new List<string>(_validationErrors);
        }

        /// <summary>
        /// Builds the HealthCheckServiceConfig instance
        /// </summary>
        /// <returns>Configured HealthCheckServiceConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or already built</exception>
        public HealthCheckServiceConfig Build()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Configuration has already been built. Create a new builder instance.");
            
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Cannot build invalid configuration. Errors: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            var config = new HealthCheckServiceConfig
            {
                DefaultCheckInterval = _defaultCheckInterval,
                MaxConcurrentChecks = _maxConcurrentChecks,
                DefaultTimeout = _defaultTimeout,
                EnableAutomaticChecks = _enableAutomaticChecks,
                EnableCircuitBreakers = _enableCircuitBreakers,
                EnableGracefulDegradation = _enableGracefulDegradation,
                MaxHistoryPerCheck = _maxHistoryPerCheck,
                EnableHealthAlerts = _enableHealthAlerts,
                EnableCircuitBreakerAlerts = _enableCircuitBreakerAlerts,
                EnableDegradationAlerts = _enableDegradationAlerts,
                HealthThresholds = _healthThresholds,
                DegradationThresholds = _degradationThresholds,
                DefaultCircuitBreakerConfig = _defaultCircuitBreakerConfig,
                AlertSeverities = new Dictionary<HealthStatus, AlertSeverity>(_alertSeverities),
                AlertTags = new HashSet<FixedString64Bytes>(_alertTags),
                EnableHealthCheckLogging = _enableHealthCheckLogging,
                HealthCheckLogLevel = _healthCheckLogLevel,
                EnableProfiling = _enableProfiling,
                SlowHealthCheckThreshold = _slowHealthCheckThreshold
            };
            
            _isBuilt = true;
            _logger.LogInfo("HealthCheckServiceConfig built successfully");
            
            return config;
        }

        /// <summary>
        /// Resets the builder to allow building a new configuration
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckServiceConfigBuilder Reset()
        {
            _isBuilt = false;
            _isValidated = false;
            _validationErrors.Clear();
            _logger.LogDebug("Builder reset for new configuration");
            return this;
        }

        #region Private Methods

        /// <summary>
        /// Applies development environment preset
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckServiceConfigBuilder ApplyDevelopmentPreset()
        {
            _defaultCheckInterval = TimeSpan.FromSeconds(10);
            _maxConcurrentChecks = 5;
            _defaultTimeout = TimeSpan.FromSeconds(10);
            _enableAutomaticChecks = true;
            _enableCircuitBreakers = true;
            _enableGracefulDegradation = false;
            _maxHistoryPerCheck = 50;
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Debug;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 500;
            
            _logger.LogInfo("Applied development environment preset");
            return this;
        }

        /// <summary>
        /// Applies testing environment preset
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckServiceConfigBuilder ApplyTestingPreset()
        {
            _defaultCheckInterval = TimeSpan.FromSeconds(1);
            _maxConcurrentChecks = 1;
            _defaultTimeout = TimeSpan.FromSeconds(5);
            _enableAutomaticChecks = false;
            _enableCircuitBreakers = false;
            _enableGracefulDegradation = false;
            _maxHistoryPerCheck = 10;
            _enableHealthAlerts = false;
            _enableCircuitBreakerAlerts = false;
            _enableDegradationAlerts = false;
            _enableHealthCheckLogging = false;
            _enableProfiling = false;
            
            _logger.LogInfo("Applied testing environment preset");
            return this;
        }

        /// <summary>
        /// Applies staging environment preset
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckServiceConfigBuilder ApplyStagingPreset()
        {
            _defaultCheckInterval = TimeSpan.FromSeconds(30);
            _maxConcurrentChecks = 15;
            _defaultTimeout = TimeSpan.FromSeconds(20);
            _enableAutomaticChecks = true;
            _enableCircuitBreakers = true;
            _enableGracefulDegradation = true;
            _maxHistoryPerCheck = 150;
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 1500;
            
            _logger.LogInfo("Applied staging environment preset");
            return this;
        }

        /// <summary>
        /// Applies production environment preset
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckServiceConfigBuilder ApplyProductionPreset()
        {
            _defaultCheckInterval = TimeSpan.FromMinutes(1);
            _maxConcurrentChecks = 20;
            _defaultTimeout = TimeSpan.FromSeconds(30);
            _enableAutomaticChecks = true;
            _enableCircuitBreakers = true;
            _enableGracefulDegradation = true;
            _maxHistoryPerCheck = 200;
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 2000;
            
            _logger.LogInfo("Applied production environment preset");
            return this;
        }

        /// <summary>
        /// Validates alert severities dictionary
        /// </summary>
        /// <param name="severities">Alert severities to validate</param>
        private void ValidateAlertSeverities(Dictionary<HealthStatus, AlertSeverity> severities)
        {
            foreach (var kvp in severities)
            {
                if (!Enum.IsDefined(typeof(HealthStatus), kvp.Key))
                    _validationErrors.Add($"Invalid health status in alert severities: {kvp.Key}");
                
                if (!Enum.IsDefined(typeof(AlertSeverity), kvp.Value))
                    _validationErrors.Add($"Invalid alert severity: {kvp.Value}");
            }
        }

        /// <summary>
        /// Throws exception if configuration has already been built
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when already built</exception>
        private void ThrowIfAlreadyBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot modify configuration after it has been built. Use Reset() or create a new builder.");
        }

        #endregion
    }

    /// <summary>
    /// Environment types for health check configuration presets
    /// </summary>
    public enum HealthCheckEnvironment
    {
        /// <summary>
        /// Development environment with verbose logging and relaxed thresholds
        /// </summary>
        Development,

        /// <summary>
        /// Testing environment with minimal features for fast test execution
        /// </summary>
        Testing,

        /// <summary>
        /// Staging environment that mirrors production with some relaxed settings
        /// </summary>
        Staging,

        /// <summary>
        /// Production environment with optimal performance and monitoring
        /// </summary>
        Production
    }
}