using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Builders
{
    /// <summary>
    /// Builder pattern implementation for creating HealthCheckServiceConfig instances
    /// with validation, preset configurations, and environment-specific settings
    /// </summary>
    /// <remarks>
    /// Provides a fluent interface for building health check service configurations
    /// with comprehensive validation and support for different deployment environments
    /// </remarks>
    public sealed class HealthCheckServiceConfigBuilder : IHealthCheckServiceConfigBuilder
    {
        private readonly ILoggingService _logger;
        private bool _isBuilt;

        #region Core Health Check Settings
        
        private TimeSpan _automaticCheckInterval = TimeSpan.FromSeconds(30);
        private int _maxConcurrentHealthChecks = 10;
        private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private bool _enableAutomaticChecks = true;
        private int _maxHistorySize = 100;
        private int _maxRetries = 3;
        private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);

        #endregion

        #region Circuit Breaker Settings
        
        private bool _enableCircuitBreaker = true;
        private CircuitBreakerConfig _defaultCircuitBreakerConfig = new();
        private bool _enableCircuitBreakerAlerts = true;
        private int _defaultFailureThreshold = 5;
        private TimeSpan _defaultCircuitBreakerTimeout = TimeSpan.FromSeconds(30);

        #endregion

        #region Graceful Degradation Settings
        
        private bool _enableGracefulDegradation = true;
        private DegradationThresholds _degradationThresholds = new();
        private bool _enableDegradationAlerts = true;
        private bool _enableAutomaticDegradation = true;

        #endregion

        #region Alert and Notification Settings
        
        private bool _enableHealthAlerts = true;
        private Dictionary<HealthStatus, AlertSeverity> _alertSeverities = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };
        private HashSet<FixedString64Bytes> _alertTags = new() { "HealthCheck", "SystemMonitoring" };
        private int _alertFailureThreshold = 3;

        #endregion

        #region Logging and Profiling Settings
        
        private bool _enableHealthCheckLogging = true;
        private LogLevel _healthCheckLogLevel = LogLevel.Info;
        private bool _enableProfiling = true;
        private int _slowHealthCheckThreshold = 1000;
        private bool _enableDetailedLogging = false;

        #endregion

        #region Performance and Resource Settings
        
        private int _maxMemoryUsageMB = 50;
        private TimeSpan _historyCleanupInterval = TimeSpan.FromMinutes(30);
        private TimeSpan _maxHistoryAge = TimeSpan.FromHours(24);
        private System.Threading.ThreadPriority _healthCheckThreadPriority = System.Threading.ThreadPriority.Normal;

        #endregion

        #region Health Status Thresholds
        
        private HealthThresholds _healthThresholds = new();
        private double _unhealthyThreshold = 0.25;
        private double _warningThreshold = 0.5;

        #endregion

        #region Advanced Settings
        
        private bool _enableDependencyValidation = true;
        private bool _enableResultCaching = true;
        private TimeSpan _resultCacheDuration = TimeSpan.FromSeconds(10);
        private bool _enableExecutionTimeouts = true;
        private bool _enableCorrelationIds = true;
        private Dictionary<string, object> _defaultMetadata = new();

        #endregion

        /// <summary>
        /// Initializes a new instance of the HealthCheckServiceConfigBuilder
        /// </summary>
        /// <param name="logger">Logging service for builder operations</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public HealthCheckServiceConfigBuilder(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isBuilt = false;
            
            _logger.LogDebug("HealthCheckServiceConfigBuilder initialized");
        }

        #region Core Health Check Configuration Methods

        /// <summary>
        /// Sets the automatic health check interval
        /// </summary>
        /// <param name="interval">Interval between automatic health checks</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithAutomaticCheckInterval(TimeSpan interval)
        {
            ThrowIfAlreadyBuilt();
            
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero");
            
            if (interval > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval should not exceed 1 hour");
            
            _automaticCheckInterval = interval;
            _logger.LogDebug($"Automatic check interval set to: {interval}");
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent health checks
        /// </summary>
        /// <param name="maxConcurrent">Maximum concurrent health checks</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithMaxConcurrentHealthChecks(int maxConcurrent)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxConcurrent < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent checks must be at least 1");
            
            if (maxConcurrent > 100)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent checks should not exceed 100");
            
            _maxConcurrentHealthChecks = maxConcurrent;
            _logger.LogDebug($"Max concurrent health checks set to: {maxConcurrent}");
            return this;
        }

        /// <summary>
        /// Sets the default timeout for health checks
        /// </summary>
        /// <param name="timeout">Default health check timeout</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithDefaultTimeout(TimeSpan timeout)
        {
            ThrowIfAlreadyBuilt();
            
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");
            
            if (timeout > TimeSpan.FromMinutes(5))
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout should not exceed 5 minutes");
            
            _defaultTimeout = timeout;
            _logger.LogDebug($"Default timeout set to: {timeout}");
            return this;
        }

        /// <summary>
        /// Configures automatic health checks
        /// </summary>
        /// <param name="enabled">Whether to enable automatic checks</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithAutomaticChecks(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableAutomaticChecks = enabled;
            _logger.LogDebug($"Automatic checks enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets the maximum history size for health check results
        /// </summary>
        /// <param name="maxHistory">Maximum number of results to keep in history</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithMaxHistorySize(int maxHistory)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxHistory < 10)
                throw new ArgumentOutOfRangeException(nameof(maxHistory), "Max history size must be at least 10");
            
            if (maxHistory > 10000)
                throw new ArgumentOutOfRangeException(nameof(maxHistory), "Max history size should not exceed 10000");
            
            _maxHistorySize = maxHistory;
            _logger.LogDebug($"Max history size set to: {maxHistory}");
            return this;
        }

        /// <summary>
        /// Configures retry settings for failed health checks
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when values are invalid</exception>
        public IHealthCheckServiceConfigBuilder WithRetrySettings(int maxRetries, TimeSpan retryDelay)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");
            
            if (maxRetries > 10)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries should not exceed 10");
            
            if (retryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be non-negative");
            
            if (retryDelay > TimeSpan.FromMinutes(1))
                throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay should not exceed 1 minute");
            
            _maxRetries = maxRetries;
            _retryDelay = retryDelay;
            _logger.LogDebug($"Retry settings configured: {maxRetries} retries with {retryDelay} delay");
            return this;
        }

        #endregion

        #region Circuit Breaker Configuration Methods

        /// <summary>
        /// Configures circuit breaker functionality
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breakers</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithCircuitBreaker(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableCircuitBreaker = enabled;
            _logger.LogDebug($"Circuit breaker enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets the default circuit breaker configuration
        /// </summary>
        /// <param name="config">Circuit breaker configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        public IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerConfig(CircuitBreakerConfig config)
        {
            ThrowIfAlreadyBuilt();
            
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var validationErrors = config.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException($"Invalid circuit breaker configuration: {string.Join(", ", validationErrors)}");
            }
            
            _defaultCircuitBreakerConfig = config;
            _logger.LogDebug("Default circuit breaker configSo set");
            return this;
        }

        /// <summary>
        /// Configures circuit breaker alerts
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithCircuitBreakerAlerts(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableCircuitBreakerAlerts = enabled;
            _logger.LogDebug($"Circuit breaker alerts enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets the default failure threshold for circuit breakers
        /// </summary>
        /// <param name="threshold">Failure threshold</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithDefaultFailureThreshold(int threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold < 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Failure threshold must be at least 1");
            
            if (threshold > 100)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Failure threshold should not exceed 100");
            
            _defaultFailureThreshold = threshold;
            _logger.LogDebug($"Default failure threshold set to: {threshold}");
            return this;
        }

        /// <summary>
        /// Sets the default circuit breaker timeout
        /// </summary>
        /// <param name="timeout">Circuit breaker timeout</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerTimeout(TimeSpan timeout)
        {
            ThrowIfAlreadyBuilt();
            
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");
            
            if (timeout > TimeSpan.FromMinutes(10))
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout should not exceed 10 minutes");
            
            _defaultCircuitBreakerTimeout = timeout;
            _logger.LogDebug($"Default circuit breaker timeout set to: {timeout}");
            return this;
        }

        #endregion

        #region Graceful Degradation Configuration Methods

        /// <summary>
        /// Configures graceful degradation functionality
        /// </summary>
        /// <param name="enabled">Whether to enable graceful degradation</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithGracefulDegradation(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableGracefulDegradation = enabled;
            _logger.LogDebug($"Graceful degradation enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets degradation thresholds
        /// </summary>
        /// <param name="thresholds">Degradation thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when thresholds is null</exception>
        public IHealthCheckServiceConfigBuilder WithDegradationThresholds(DegradationThresholds thresholds)
        {
            ThrowIfAlreadyBuilt();
            
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));
            
            var validationErrors = thresholds.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException($"Invalid degradation thresholds: {string.Join(", ", validationErrors)}");
            }
            
            _degradationThresholds = thresholds;
            _logger.LogDebug("Degradation thresholds set");
            return this;
        }

        /// <summary>
        /// Configures degradation alerts
        /// </summary>
        /// <param name="enabled">Whether to enable degradation alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithDegradationAlerts(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableDegradationAlerts = enabled;
            _logger.LogDebug($"Degradation alerts enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Configures automatic degradation
        /// </summary>
        /// <param name="enabled">Whether to enable automatic degradation</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithAutomaticDegradation(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableAutomaticDegradation = enabled;
            _logger.LogDebug($"Automatic degradation enabled: {enabled}");
            return this;
        }

        #endregion

        #region Alert and Notification Configuration Methods

        /// <summary>
        /// Configures health alerts
        /// </summary>
        /// <param name="enabled">Whether to enable health alerts</param>
        /// <param name="severities">Custom alert severities for different health statuses</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithHealthAlerts(
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
            
            _logger.LogDebug($"Health alerts enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets alert tags for health check alerts
        /// </summary>
        /// <param name="tags">Alert tags to set</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public IHealthCheckServiceConfigBuilder WithAlertTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            _alertTags = new HashSet<FixedString64Bytes>(tags);
            _logger.LogDebug($"Alert tags set: {string.Join(", ", tags)}");
            return this;
        }

        /// <summary>
        /// Adds alert tags to existing tags
        /// </summary>
        /// <param name="tags">Alert tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public IHealthCheckServiceConfigBuilder AddAlertTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            foreach (var tag in tags)
            {
                _alertTags.Add(tag);
            }
            
            _logger.LogDebug($"Alert tags added: {string.Join(", ", tags)}");
            return this;
        }

        /// <summary>
        /// Sets the alert failure threshold
        /// </summary>
        /// <param name="threshold">Number of consecutive failures before triggering alert</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithAlertFailureThreshold(int threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold < 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Alert failure threshold must be at least 1");
            
            if (threshold > 10)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Alert failure threshold should not exceed 10");
            
            _alertFailureThreshold = threshold;
            _logger.LogDebug($"Alert failure threshold set to: {threshold}");
            return this;
        }

        #endregion

        #region Logging and Profiling Configuration Methods

        /// <summary>
        /// Configures health check logging
        /// </summary>
        /// <param name="enabled">Whether to enable health check logging</param>
        /// <param name="logLevel">Log level for health check operations</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithHealthCheckLogging(bool enabled = true, LogLevel logLevel = LogLevel.Info)
        {
            ThrowIfAlreadyBuilt();
            
            _enableHealthCheckLogging = enabled;
            _healthCheckLogLevel = logLevel;
            _logger.LogDebug($"Health check logging enabled: {enabled}, level: {logLevel}");
            return this;
        }

        /// <summary>
        /// Configures performance profiling
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        /// <param name="slowThreshold">Threshold for slow health check logging (milliseconds)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when slowThreshold is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000)
        {
            ThrowIfAlreadyBuilt();
            
            if (slowThreshold < 100)
                throw new ArgumentOutOfRangeException(nameof(slowThreshold), "Slow threshold must be at least 100ms");
            
            if (slowThreshold > 60000)
                throw new ArgumentOutOfRangeException(nameof(slowThreshold), "Slow threshold should not exceed 60 seconds");
            
            _enableProfiling = enabled;
            _slowHealthCheckThreshold = slowThreshold;
            _logger.LogDebug($"Profiling enabled: {enabled}, slow threshold: {slowThreshold}ms");
            return this;
        }

        /// <summary>
        /// Configures detailed logging
        /// </summary>
        /// <param name="enabled">Whether to enable detailed logging</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithDetailedLogging(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableDetailedLogging = enabled;
            _logger.LogDebug($"Detailed logging enabled: {enabled}");
            return this;
        }

        #endregion

        #region Performance and Resource Configuration Methods

        /// <summary>
        /// Configures memory and cleanup settings
        /// </summary>
        /// <param name="maxMemoryUsageMB">Maximum memory usage in MB</param>
        /// <param name="historyCleanupInterval">How often to clean up history</param>
        /// <param name="maxHistoryAge">Maximum age of history to keep</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when values are invalid</exception>
        public IHealthCheckServiceConfigBuilder WithMemorySettings(
            int maxMemoryUsageMB,
            TimeSpan historyCleanupInterval,
            TimeSpan maxHistoryAge)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxMemoryUsageMB < 10)
                throw new ArgumentOutOfRangeException(nameof(maxMemoryUsageMB), "Max memory usage must be at least 10MB");
            
            if (maxMemoryUsageMB > 1000)
                throw new ArgumentOutOfRangeException(nameof(maxMemoryUsageMB), "Max memory usage should not exceed 1000MB");
            
            if (historyCleanupInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(historyCleanupInterval), "History cleanup interval must be greater than zero");
            
            if (maxHistoryAge <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxHistoryAge), "Max history age must be greater than zero");
            
            _maxMemoryUsageMB = maxMemoryUsageMB;
            _historyCleanupInterval = historyCleanupInterval;
            _maxHistoryAge = maxHistoryAge;
            _logger.LogDebug($"Memory settings configured: {maxMemoryUsageMB}MB, cleanup: {historyCleanupInterval}, age: {maxHistoryAge}");
            return this;
        }

        /// <summary>
        /// Sets the thread priority for health check execution
        /// </summary>
        /// <param name="priority">Thread priority</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithThreadPriority(System.Threading.ThreadPriority priority)
        {
            ThrowIfAlreadyBuilt();
            _healthCheckThreadPriority = priority;
            _logger.LogDebug($"Thread priority set to: {priority}");
            return this;
        }

        #endregion

        #region Health Status Threshold Configuration Methods

        /// <summary>
        /// Sets health thresholds
        /// </summary>
        /// <param name="thresholds">Health thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when thresholds is null</exception>
        public IHealthCheckServiceConfigBuilder WithHealthThresholds(HealthThresholds thresholds)
        {
            ThrowIfAlreadyBuilt();
            
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));
            
            var validationErrors = thresholds.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException($"Invalid health thresholds: {string.Join(", ", validationErrors)}");
            }
            
            _healthThresholds = thresholds;
            _logger.LogDebug("Health thresholds set");
            return this;
        }

        /// <summary>
        /// Sets the unhealthy threshold
        /// </summary>
        /// <param name="threshold">Percentage of unhealthy checks that triggers overall unhealthy status</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithUnhealthyThreshold(double threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold < 0.0 || threshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Unhealthy threshold must be between 0.0 and 1.0");
            
            _unhealthyThreshold = threshold;
            _logger.LogDebug($"Unhealthy threshold set to: {threshold}");
            return this;
        }

        /// <summary>
        /// Sets the warning threshold
        /// </summary>
        /// <param name="threshold">Percentage of warning checks that triggers overall warning status</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithWarningThreshold(double threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold < 0.0 || threshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Warning threshold must be between 0.0 and 1.0");
            
            _warningThreshold = threshold;
            _logger.LogDebug($"Warning threshold set to: {threshold}");
            return this;
        }

        #endregion

        #region Advanced Configuration Methods

        /// <summary>
        /// Configures dependency validation
        /// </summary>
        /// <param name="enabled">Whether to enable dependency validation</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithDependencyValidation(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableDependencyValidation = enabled;
            _logger.LogDebug($"Dependency validation enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Configures result caching
        /// </summary>
        /// <param name="enabled">Whether to enable result caching</param>
        /// <param name="cacheDuration">How long to cache results</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheDuration is invalid</exception>
        public IHealthCheckServiceConfigBuilder WithResultCaching(bool enabled, TimeSpan cacheDuration)
        {
            ThrowIfAlreadyBuilt();
            
            if (cacheDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Cache duration must be greater than zero");
            
            if (cacheDuration > TimeSpan.FromMinutes(5))
                throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Cache duration should not exceed 5 minutes");
            
            _enableResultCaching = enabled;
            _resultCacheDuration = cacheDuration;
            _logger.LogDebug($"Result caching enabled: {enabled}, duration: {cacheDuration}");
            return this;
        }

        /// <summary>
        /// Configures execution timeouts
        /// </summary>
        /// <param name="enabled">Whether to enable execution timeouts</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithExecutionTimeouts(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableExecutionTimeouts = enabled;
            _logger.LogDebug($"Execution timeouts enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Configures correlation IDs
        /// </summary>
        /// <param name="enabled">Whether to enable correlation IDs</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder WithCorrelationIds(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            _enableCorrelationIds = enabled;
            _logger.LogDebug($"Correlation IDs enabled: {enabled}");
            return this;
        }

        /// <summary>
        /// Sets default metadata for all health check results
        /// </summary>
        /// <param name="metadata">Default metadata dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when metadata is null</exception>
        public IHealthCheckServiceConfigBuilder WithDefaultMetadata(Dictionary<string, object> metadata)
        {
            ThrowIfAlreadyBuilt();
            
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            
            _defaultMetadata = new Dictionary<string, object>(metadata);
            _logger.LogDebug($"Default metadata set with {metadata.Count} entries");
            return this;
        }

        /// <summary>
        /// Adds a single metadata entry
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
        public IHealthCheckServiceConfigBuilder AddMetadata(string key, object value)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            
            _defaultMetadata[key] = value;
            _logger.LogDebug($"Metadata added: {key} = {value}");
            return this;
        }

        #endregion

        #region Environment and Preset Configuration Methods

        /// <summary>
        /// Configures the builder for a specific environment
        /// </summary>
        /// <param name="environment">Target environment</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder ForEnvironment(HealthCheckEnvironment environment)
        {
            ThrowIfAlreadyBuilt();
            
            return environment switch
            {
                HealthCheckEnvironment.Development => ApplyDevelopmentPreset(),
                HealthCheckEnvironment.Testing => ApplyTestingPreset(),
                HealthCheckEnvironment.Staging => ApplyStagingPreset(),
                HealthCheckEnvironment.Production => ApplyProductionPreset(),
                _ => throw new ArgumentException($"Unknown environment: {environment}")
            };
        }

        /// <summary>
        /// Applies development environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder ApplyDevelopmentPreset()
        {
            ThrowIfAlreadyBuilt();
            
            _automaticCheckInterval = TimeSpan.FromSeconds(10);
            _maxConcurrentHealthChecks = 5;
            _defaultTimeout = TimeSpan.FromSeconds(10);
            _enableAutomaticChecks = true;
            _enableCircuitBreaker = true;
            _enableGracefulDegradation = false;
            _maxHistorySize = 50;
            _maxRetries = 1;
            _retryDelay = TimeSpan.FromSeconds(1);
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Debug;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 500;
            _enableDetailedLogging = true;
            _enableResultCaching = false;
            _resultCacheDuration = TimeSpan.FromSeconds(5);
            _maxMemoryUsageMB = 20;
            _historyCleanupInterval = TimeSpan.FromMinutes(10);
            _maxHistoryAge = TimeSpan.FromHours(2);
            
            _logger.LogDebug("Development preset applied");
            return this;
        }

        /// <summary>
        /// Applies testing environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder ApplyTestingPreset()
        {
            ThrowIfAlreadyBuilt();
            
            _automaticCheckInterval = TimeSpan.FromSeconds(1);
            _maxConcurrentHealthChecks = 1;
            _defaultTimeout = TimeSpan.FromSeconds(5);
            _enableAutomaticChecks = false;
            _enableCircuitBreaker = false;
            _enableGracefulDegradation = false;
            _maxHistorySize = 10;
            _maxRetries = 0;
            _retryDelay = TimeSpan.FromMilliseconds(100);
            _enableHealthAlerts = false;
            _enableCircuitBreakerAlerts = false;
            _enableDegradationAlerts = false;
            _enableHealthCheckLogging = false;
            _enableProfiling = false;
            _enableDetailedLogging = false;
            _enableResultCaching = false;
            _resultCacheDuration = TimeSpan.FromSeconds(1);
            _maxMemoryUsageMB = 5;
            _historyCleanupInterval = TimeSpan.FromMinutes(1);
            _maxHistoryAge = TimeSpan.FromMinutes(10);
            _enableDependencyValidation = false;
            _enableExecutionTimeouts = false;
            _enableCorrelationIds = false;
            
            _logger.LogDebug("Testing preset applied");
            return this;
        }

        /// <summary>
        /// Applies staging environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder ApplyStagingPreset()
        {
            ThrowIfAlreadyBuilt();
            
            _automaticCheckInterval = TimeSpan.FromSeconds(30);
            _maxConcurrentHealthChecks = 15;
            _defaultTimeout = TimeSpan.FromSeconds(25);
            _enableAutomaticChecks = true;
            _enableCircuitBreaker = true;
            _enableGracefulDegradation = true;
            _maxHistorySize = 200;
            _maxRetries = 2;
            _retryDelay = TimeSpan.FromSeconds(1);
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 1500;
            _enableDetailedLogging = true;
            _enableResultCaching = true;
            _resultCacheDuration = TimeSpan.FromSeconds(15);
            _maxMemoryUsageMB = 75;
            _historyCleanupInterval = TimeSpan.FromMinutes(20);
            _maxHistoryAge = TimeSpan.FromHours(12);
            _enableDependencyValidation = true;
            _enableExecutionTimeouts = true;
            _enableCorrelationIds = true;
            
            _logger.LogDebug("Staging preset applied");
            return this;
        }

        /// <summary>
        /// Applies production environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        public IHealthCheckServiceConfigBuilder ApplyProductionPreset()
        {
            ThrowIfAlreadyBuilt();
            
            _automaticCheckInterval = TimeSpan.FromMinutes(1);
            _maxConcurrentHealthChecks = 20;
            _defaultTimeout = TimeSpan.FromSeconds(30);
            _enableAutomaticChecks = true;
            _enableCircuitBreaker = true;
            _enableGracefulDegradation = true;
            _maxHistorySize = 500;
            _maxRetries = 3;
            _retryDelay = TimeSpan.FromSeconds(2);
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 2000;
            _enableDetailedLogging = false;
            _enableResultCaching = true;
            _resultCacheDuration = TimeSpan.FromSeconds(30);
            _maxMemoryUsageMB = 100;
            _historyCleanupInterval = TimeSpan.FromMinutes(30);
            _maxHistoryAge = TimeSpan.FromHours(24);
            _enableDependencyValidation = true;
            _enableExecutionTimeouts = true;
            _enableCorrelationIds = true;
            
            _logger.LogDebug("Production preset applied");
            return this;
        }

        #endregion

        #region Validation and Build Methods

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            
            // Core settings validation
            if (_automaticCheckInterval <= TimeSpan.Zero)
                errors.Add("AutomaticCheckInterval must be greater than zero");
            
            if (_maxConcurrentHealthChecks <= 0)
                errors.Add("MaxConcurrentHealthChecks must be greater than zero");
            
            if (_defaultTimeout <= TimeSpan.Zero)
                errors.Add("DefaultTimeout must be greater than zero");
            
            if (_maxHistorySize < 0)
                errors.Add("MaxHistorySize must be non-negative");
            
            if (_slowHealthCheckThreshold < 0)
                errors.Add("SlowHealthCheckThreshold must be non-negative");
            
            if (_maxRetries < 0)
                errors.Add("MaxRetries must be non-negative");
            
            if (_retryDelay < TimeSpan.Zero)
                errors.Add("RetryDelay must be non-negative");
            
            // Circuit breaker validation
            if (_enableCircuitBreaker)
            {
                if (_defaultFailureThreshold <= 0)
                    errors.Add("DefaultFailureThreshold must be greater than zero");
                
                if (_defaultCircuitBreakerTimeout <= TimeSpan.Zero)
                    errors.Add("DefaultCircuitBreakerTimeout must be greater than zero");
                
                var cbErrors = _defaultCircuitBreakerConfig.Validate();
                errors.AddRange(cbErrors);
            }
            
            // Degradation validation
            if (_enableGracefulDegradation)
            {
                var degradationErrors = _degradationThresholds.Validate();
                errors.AddRange(degradationErrors);
            }
            
            // Performance validation
            if (_maxMemoryUsageMB <= 0)
                errors.Add("MaxMemoryUsageMB must be greater than zero");
            
            if (_historyCleanupInterval <= TimeSpan.Zero)
                errors.Add("HistoryCleanupInterval must be greater than zero");
            
            if (_maxHistoryAge <= TimeSpan.Zero)
                errors.Add("MaxHistoryAge must be greater than zero");
            
            // Threshold validation
            if (_unhealthyThreshold < 0.0 || _unhealthyThreshold > 1.0)
                errors.Add("UnhealthyThreshold must be between 0.0 and 1.0");
            
            if (_warningThreshold < 0.0 || _warningThreshold > 1.0)
                errors.Add("WarningThreshold must be between 0.0 and 1.0");
            
            if (_resultCacheDuration <= TimeSpan.Zero)
                errors.Add("ResultCacheDuration must be greater than zero");
            
            var healthThresholdErrors = _healthThresholds.Validate();
            errors.AddRange(healthThresholdErrors);
            
            // Alert validation
            if (_alertFailureThreshold < 1)
                errors.Add("AlertFailureThreshold must be at least 1");
            
            return errors;
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
                AutomaticCheckInterval = _automaticCheckInterval,
                MaxConcurrentHealthChecks = _maxConcurrentHealthChecks,
                DefaultTimeout = _defaultTimeout,
                EnableAutomaticChecks = _enableAutomaticChecks,
                MaxHistorySize = _maxHistorySize,
                MaxRetries = _maxRetries,
                RetryDelay = _retryDelay,
                EnableCircuitBreaker = _enableCircuitBreaker,
                DefaultCircuitBreakerConfig = _defaultCircuitBreakerConfig,
                EnableCircuitBreakerAlerts = _enableCircuitBreakerAlerts,
                DefaultFailureThreshold = _defaultFailureThreshold,
                DefaultCircuitBreakerTimeout = _defaultCircuitBreakerTimeout,
                EnableGracefulDegradation = _enableGracefulDegradation,
                DegradationThresholds = _degradationThresholds,
                EnableDegradationAlerts = _enableDegradationAlerts,
                EnableAutomaticDegradation = _enableAutomaticDegradation,
                EnableHealthAlerts = _enableHealthAlerts,
                AlertSeverities = new Dictionary<HealthStatus, AlertSeverity>(_alertSeverities),
                AlertTags = new HashSet<FixedString64Bytes>(_alertTags),
                AlertFailureThreshold = _alertFailureThreshold,
                EnableHealthCheckLogging = _enableHealthCheckLogging,
                HealthCheckLogLevel = _healthCheckLogLevel,
                EnableProfiling = _enableProfiling,
                SlowHealthCheckThreshold = _slowHealthCheckThreshold,
                EnableDetailedLogging = _enableDetailedLogging,
                MaxMemoryUsageMB = _maxMemoryUsageMB,
                HistoryCleanupInterval = _historyCleanupInterval,
                MaxHistoryAge = _maxHistoryAge,
                HealthCheckThreadPriority = _healthCheckThreadPriority,
                HealthThresholds = _healthThresholds,
                UnhealthyThreshold = _unhealthyThreshold,
                WarningThreshold = _warningThreshold,
                EnableDependencyValidation = _enableDependencyValidation,
                EnableResultCaching = _enableResultCaching,
                ResultCacheDuration = _resultCacheDuration,
                EnableExecutionTimeouts = _enableExecutionTimeouts,
                EnableCorrelationIds = _enableCorrelationIds,
                DefaultMetadata = new Dictionary<string, object>(_defaultMetadata)
            };
            
            _isBuilt = true;
            _logger.LogInfo("HealthCheckServiceConfig built successfully");
            
            return config;
        }

        /// <summary>
        /// Resets the builder to its initial state
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public IHealthCheckServiceConfigBuilder Reset()
        {
            _isBuilt = false;
            
            // Reset all fields to their default values
            _automaticCheckInterval = TimeSpan.FromSeconds(30);
            _maxConcurrentHealthChecks = 10;
            _defaultTimeout = TimeSpan.FromSeconds(30);
            _enableAutomaticChecks = true;
            _maxHistorySize = 100;
            _maxRetries = 3;
            _retryDelay = TimeSpan.FromSeconds(1);
            _enableCircuitBreaker = true;
            _defaultCircuitBreakerConfig = new CircuitBreakerConfig();
            _enableCircuitBreakerAlerts = true;
            _defaultFailureThreshold = 5;
            _defaultCircuitBreakerTimeout = TimeSpan.FromSeconds(30);
            _enableGracefulDegradation = true;
            _degradationThresholds = new DegradationThresholds();
            _enableDegradationAlerts = true;
            _enableAutomaticDegradation = true;
            _enableHealthAlerts = true;
            _alertSeverities = new Dictionary<HealthStatus, AlertSeverity>
            {
                { HealthStatus.Healthy, AlertSeverity.Info },
                { HealthStatus.Warning, AlertSeverity.Warning },
                { HealthStatus.Degraded, AlertSeverity.Warning },
                { HealthStatus.Unhealthy, AlertSeverity.Critical },
                { HealthStatus.Critical, AlertSeverity.Critical },
                { HealthStatus.Offline, AlertSeverity.Emergency },
                { HealthStatus.Unknown, AlertSeverity.Warning }
            };
            _alertTags = new HashSet<FixedString64Bytes> { "HealthCheck", "SystemMonitoring" };
            _alertFailureThreshold = 3;
            _enableHealthCheckLogging = true;
            _healthCheckLogLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowHealthCheckThreshold = 1000;
            _enableDetailedLogging = false;
            _maxMemoryUsageMB = 50;
            _historyCleanupInterval = TimeSpan.FromMinutes(30);
            _maxHistoryAge = TimeSpan.FromHours(24);
            _healthCheckThreadPriority = System.Threading.ThreadPriority.Normal;
            _healthThresholds = new HealthThresholds();
            _unhealthyThreshold = 0.25;
            _warningThreshold = 0.5;
            _enableDependencyValidation = true;
            _enableResultCaching = true;
            _resultCacheDuration = TimeSpan.FromSeconds(10);
            _enableExecutionTimeouts = true;
            _enableCorrelationIds = true;
            _defaultMetadata = new Dictionary<string, object>();
            
            _logger.LogDebug("Builder reset to initial state");
            return this;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates alert severities dictionary
        /// </summary>
        /// <param name="severities">Alert severities to validate</param>
        /// <exception cref="ArgumentException">Thrown when severities are invalid</exception>
        private void ValidateAlertSeverities(Dictionary<HealthStatus, AlertSeverity> severities)
        {
            foreach (var severity in severities.Values)
            {
                if (!Enum.IsDefined(typeof(AlertSeverity), severity))
                {
                    throw new ArgumentException($"Invalid alert severity: {severity}");
                }
            }
        }

        /// <summary>
        /// Throws an exception if the builder has already been built
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
        private void ThrowIfAlreadyBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot modify builder after Build() has been called. Create a new builder instance.");
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of health check environments
    /// </summary>
    public enum HealthCheckEnvironment
    {
        /// <summary>
        /// Development environment
        /// </summary>
        Development,
        
        /// <summary>
        /// Testing environment
        /// </summary>
        Testing,
        
        /// <summary>
        /// Staging environment
        /// </summary>
        Staging,
        
        /// <summary>
        /// Production environment
        /// </summary>
        Production
    }
}