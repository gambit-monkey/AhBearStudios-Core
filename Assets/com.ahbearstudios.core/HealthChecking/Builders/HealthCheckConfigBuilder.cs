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
    /// Production-ready builder for individual HealthCheckConfiguration with comprehensive options
    /// </summary>
    public sealed class HealthCheckConfigBuilder
    {
        private readonly ILoggingService _logger;
        private readonly List<string> _validationErrors = new();
        
        // Core configuration properties
        private FixedString64Bytes _id = GenerateId();
        private string _displayName = string.Empty;
        private string _description = string.Empty;
        private HealthCheckCategory _category = HealthCheckCategory.Custom;
        private bool _enabled = true;
        private TimeSpan _interval = TimeSpan.FromSeconds(30);
        private TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private int _priority = 100;
        
        // Circuit breaker configuration
        private bool _enableCircuitBreaker = true;
        private CircuitBreakerConfig _circuitBreakerConfig;
        
        // Health status calculation
        private bool _includeInOverallStatus = true;
        private double _overallStatusWeight = 1.0;
        
        // Alerting configuration
        private bool _enableAlerting = true;
        private Dictionary<HealthStatus, AlertSeverity> _alertSeverities = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Low },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };
        private bool _alertOnlyOnStatusChange = true;
        private TimeSpan _alertCooldown = TimeSpan.FromMinutes(5);
        
        // Metadata and organization
        private HashSet<FixedString64Bytes> _tags = new();
        private Dictionary<string, object> _metadata = new();
        private HashSet<FixedString64Bytes> _dependencies = new();
        private bool _skipOnUnhealthyDependencies = true;
        
        // History and logging
        private int _maxHistorySize = 100;
        private bool _enableDetailedLogging = false;
        private LogLevel _logLevel = LogLevel.Info;
        private bool _enableProfiling = true;
        private int _slowExecutionThreshold = 1000;
        
        // Advanced configuration
        private Dictionary<string, object> _customParameters = new();
        private RetryConfig _retryConfig = new();
        private DegradationImpactConfig _degradationImpact = new();
        private HealthCheckValidationConfig _validationConfig = new();
        private bool _allowConcurrentExecution = true;
        private ResourceLimitsConfig _resourceLimits = new();
        
        // Build state tracking
        private bool _isBuilt = false;
        private bool _isValidated = false;

        /// <summary>
        /// Initializes a new instance of the HealthCheckConfigBuilder class
        /// </summary>
        /// <param name="logger">Logging service for build operations</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public HealthCheckConfigBuilder(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("HealthCheckConfigBuilder initialized");
        }

        /// <summary>
        /// Sets the display name for the health check
        /// </summary>
        /// <param name="name">Display name (required)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
        public HealthCheckConfigBuilder WithName(string name)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(name));
            
            _displayName = name.Trim();
            _logger.LogDebug($"Set health check name to '{_displayName}'");
            return this;
        }

        /// <summary>
        /// Sets the description for the health check
        /// </summary>
        /// <param name="description">Description of what the health check validates</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckConfigBuilder WithDescription(string description)
        {
            ThrowIfAlreadyBuilt();
            
            _description = description?.Trim() ?? string.Empty;
            _logger.LogDebug($"Set health check description");
            return this;
        }

        /// <summary>
        /// Sets the category for the health check
        /// </summary>
        /// <param name="category">Health check category</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when category is invalid</exception>
        public HealthCheckConfigBuilder WithCategory(HealthCheckCategory category)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(HealthCheckCategory), category))
                throw new ArgumentException($"Invalid health check category: {category}", nameof(category));
            
            _category = category;
            _logger.LogDebug($"Set health check category to {category}");
            return this;
        }

        /// <summary>
        /// Sets whether the health check is enabled
        /// </summary>
        /// <param name="enabled">Whether the health check is enabled</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckConfigBuilder WithEnabled(bool enabled = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enabled = enabled;
            _logger.LogDebug($"Health check {(enabled ? "enabled" : "disabled")}");
            return this;
        }

        /// <summary>
        /// Sets the execution interval for the health check
        /// </summary>
        /// <param name="interval">Execution interval (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is not positive</exception>
        public HealthCheckConfigBuilder WithInterval(TimeSpan interval)
        {
            ThrowIfAlreadyBuilt();
            
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive");
            
            if (interval > TimeSpan.FromHours(24))
                _logger.LogWarning($"Very long interval specified: {interval}. Consider if this is intended.");
            
            _interval = interval;
            _logger.LogDebug($"Set health check interval to {interval}");
            return this;
        }

        /// <summary>
        /// Sets the timeout for the health check
        /// </summary>
        /// <param name="timeout">Execution timeout (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is not positive</exception>
        public HealthCheckConfigBuilder WithTimeout(TimeSpan timeout)
        {
            ThrowIfAlreadyBuilt();
            
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive");
            
            if (timeout > TimeSpan.FromMinutes(10))
                _logger.LogWarning($"Very long timeout specified: {timeout}. This may affect system responsiveness.");
            
            _timeout = timeout;
            _logger.LogDebug($"Set health check timeout to {timeout}");
            return this;
        }

        /// <summary>
        /// Sets the priority for the health check
        /// </summary>
        /// <param name="priority">Execution priority (higher numbers execute first)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when priority is negative</exception>
        public HealthCheckConfigBuilder WithPriority(int priority)
        {
            ThrowIfAlreadyBuilt();
            
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be non-negative");
            
            _priority = priority;
            _logger.LogDebug($"Set health check priority to {priority}");
            return this;
        }

        /// <summary>
        /// Configures circuit breaker for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker</param>
        /// <param name="config">Circuit breaker configuration (optional)</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckConfigBuilder WithCircuitBreaker(bool enabled = true, CircuitBreakerConfig config = null)
        {
            ThrowIfAlreadyBuilt();
            
            _enableCircuitBreaker = enabled;
            
            if (config != null)
            {
                var validationErrors = config.Validate();
                if (validationErrors.Count > 0)
                {
                    var errorMessage = $"Invalid circuit breaker config: {string.Join(", ", validationErrors)}";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage, nameof(config));
                }
                
                _circuitBreakerConfig = config;
            }
            
            _logger.LogDebug($"Circuit breaker {(enabled ? "enabled" : "disabled")} for health check");
            return this;
        }

        /// <summary>
        /// Sets whether this health check should be included in overall status calculation
        /// </summary>
        /// <param name="include">Whether to include in overall status</param>
        /// <param name="weight">Weight for overall status calculation (0.0 to 1.0)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when weight is out of range</exception>
        public HealthCheckConfigBuilder WithOverallStatusImpact(bool include = true, double weight = 1.0)
        {
            ThrowIfAlreadyBuilt();
            
            if (weight < 0.0 || weight > 1.0)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be between 0.0 and 1.0");
            
            _includeInOverallStatus = include;
            _overallStatusWeight = weight;
            _logger.LogDebug($"Overall status impact: include={include}, weight={weight}");
            return this;
        }

        /// <summary>
        /// Configures alerting for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable alerting</param>
        /// <param name="onlyOnStatusChange">Whether to alert only on status changes</param>
        /// <param name="cooldown">Minimum time between alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when cooldown is negative</exception>
        public HealthCheckConfigBuilder WithAlerting(
            bool enabled = true,
            bool onlyOnStatusChange = true,
            TimeSpan? cooldown = null)
        {
            ThrowIfAlreadyBuilt();
            
            var alertCooldown = cooldown ?? TimeSpan.FromMinutes(5);
            
            if (alertCooldown < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(cooldown), "Alert cooldown must be non-negative");
            
            _enableAlerting = enabled;
            _alertOnlyOnStatusChange = onlyOnStatusChange;
            _alertCooldown = alertCooldown;
            
            _logger.LogDebug($"Alerting: enabled={enabled}, onStatusChange={onlyOnStatusChange}, cooldown={alertCooldown}");
            return this;
        }

        /// <summary>
        /// Sets custom alert severities for different health statuses
        /// </summary>
        /// <param name="severities">Custom alert severities</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when severities is null</exception>
        public HealthCheckConfigBuilder WithAlertSeverities(Dictionary<HealthStatus, AlertSeverity> severities)
        {
            ThrowIfAlreadyBuilt();
            
            if (severities == null)
                throw new ArgumentNullException(nameof(severities));
            
            ValidateAlertSeverities(severities);
            _alertSeverities = new Dictionary<HealthStatus, AlertSeverity>(severities);
            _logger.LogDebug("Set custom alert severities");
            return this;
        }

        /// <summary>
        /// Adds tags to the health check
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public HealthCheckConfigBuilder WithTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            foreach (var tag in tags)
            {
                _tags.Add(tag);
            }
            
            _logger.LogDebug($"Added {tags.Length} tags to health check");
            return this;
        }

        /// <summary>
        /// Adds metadata to the health check
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
        public HealthCheckConfigBuilder WithMetadata(string key, object value)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
            
            _metadata[key] = value;
            _logger.LogDebug($"Added metadata: {key}");
            return this;
        }

        /// <summary>
        /// Adds multiple metadata entries to the health check
        /// </summary>
        /// <param name="metadata">Metadata dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when metadata is null</exception>
        public HealthCheckConfigBuilder WithMetadata(Dictionary<string, object> metadata)
        {
            ThrowIfAlreadyBuilt();
            
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            
            foreach (var kvp in metadata)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    throw new ArgumentException("Metadata keys cannot be null or empty");
                
                _metadata[kvp.Key] = kvp.Value;
            }
            
            _logger.LogDebug($"Added {metadata.Count} metadata entries");
            return this;
        }

        /// <summary>
        /// Sets dependencies for the health check
        /// </summary>
        /// <param name="dependencies">Health check dependencies</param>
        /// <param name="skipOnUnhealthy">Whether to skip if dependencies are unhealthy</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public HealthCheckConfigBuilder WithDependencies(
            IEnumerable<FixedString64Bytes> dependencies,
            bool skipOnUnhealthy = true)
        {
            ThrowIfAlreadyBuilt();
            
            if (dependencies == null)
                throw new ArgumentNullException(nameof(dependencies));
            
            _dependencies = new HashSet<FixedString64Bytes>(dependencies);
            _skipOnUnhealthyDependencies = skipOnUnhealthy;
            
            // Validate no self-references
            if (_dependencies.Contains(_id))
            {
                _dependencies.Remove(_id);
                _logger.LogWarning("Removed self-reference from dependencies");
            }
            
            _logger.LogDebug($"Set {_dependencies.Count} dependencies, skipOnUnhealthy={skipOnUnhealthy}");
            return this;
        }

        /// <summary>
        /// Configures history retention for the health check
        /// </summary>
        /// <param name="maxSize">Maximum number of history entries to keep</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is negative</exception>
        public HealthCheckConfigBuilder WithHistory(int maxSize)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max history size must be non-negative");
            
            if (maxSize > 10000)
                _logger.LogWarning($"Very large history size: {maxSize}. This may impact memory usage.");
            
            _maxHistorySize = maxSize;
            _logger.LogDebug($"Set max history size to {maxSize}");
            return this;
        }

        /// <summary>
        /// Configures logging for the health check
        /// </summary>
        /// <param name="enableDetailed">Whether to enable detailed logging</param>
        /// <param name="logLevel">Log level for operations</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when logLevel is invalid</exception>
        public HealthCheckConfigBuilder WithLogging(bool enableDetailed = false, LogLevel logLevel = LogLevel.Info)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(LogLevel), logLevel))
                throw new ArgumentException($"Invalid log level: {logLevel}", nameof(logLevel));
            
            _enableDetailedLogging = enableDetailed;
            _logLevel = logLevel;
            _logger.LogDebug($"Logging: detailed={enableDetailed}, level={logLevel}");
            return this;
        }

        /// <summary>
        /// Configures profiling for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        /// <param name="slowThreshold">Threshold for slow execution in milliseconds</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when slowThreshold is negative</exception>
        public HealthCheckConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000)
        {
            ThrowIfAlreadyBuilt();
            
            if (slowThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(slowThreshold), "Slow threshold must be non-negative");
            
            _enableProfiling = enabled;
            _slowExecutionThreshold = slowThreshold;
            _logger.LogDebug($"Profiling: enabled={enabled}, slowThreshold={slowThreshold}ms");
            return this;
        }

        /// <summary>
        /// Adds custom parameters for the health check implementation
        /// </summary>
        /// <param name="key">Parameter key</param>
        /// <param name="value">Parameter value</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
        public HealthCheckConfigBuilder WithCustomParameter(string key, object value)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Parameter key cannot be null or empty", nameof(key));
            
            _customParameters[key] = value;
            _logger.LogDebug($"Added custom parameter: {key}");
            return this;
        }

        /// <summary>
        /// Adds multiple custom parameters for the health check implementation
        /// </summary>
        /// <param name="parameters">Parameters dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        public HealthCheckConfigBuilder WithCustomParameters(Dictionary<string, object> parameters)
        {
            ThrowIfAlreadyBuilt();
            
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            foreach (var kvp in parameters)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    throw new ArgumentException("Parameter keys cannot be null or empty");
                
                _customParameters[kvp.Key] = kvp.Value;
            }
            
            _logger.LogDebug($"Added {parameters.Count} custom parameters");
            return this;
        }

        /// <summary>
        /// Configures retry behavior for the health check
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries</param>
        /// <param name="backoffMultiplier">Exponential backoff multiplier</param>
        /// <param name="maxRetryDelay">Maximum delay between retries</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public HealthCheckConfigBuilder WithRetry(
            int maxRetries = 0,
            TimeSpan? retryDelay = null,
            double backoffMultiplier = 1.0,
            TimeSpan? maxRetryDelay = null)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");
            
            if (maxRetries > 10)
                _logger.LogWarning($"High retry count: {maxRetries}. This may cause delays.");
            
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            var maxDelay = maxRetryDelay ?? TimeSpan.FromMinutes(1);
            
            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be non-negative");
            
            if (backoffMultiplier < 1.0)
                throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be at least 1.0");
            
            if (maxDelay < delay)
                throw new ArgumentOutOfRangeException(nameof(maxRetryDelay), "Max retry delay must be greater than or equal to retry delay");
            
            _retryConfig = new RetryConfig
            {
                MaxRetries = maxRetries,
                RetryDelay = delay,
                BackoffMultiplier = backoffMultiplier,
                MaxRetryDelay = maxDelay
            };
            
            _logger.LogDebug($"Retry config: max={maxRetries}, delay={delay}, backoff={backoffMultiplier}, maxDelay={maxDelay}");
            return this;
        }

        /// <summary>
        /// Configures degradation impact for the health check
        /// </summary>
        /// <param name="degradedImpact">Impact level when degraded</param>
        /// <param name="unhealthyImpact">Impact level when unhealthy</param>
        /// <param name="disabledFeatures">Features to disable when unhealthy</param>
        /// <param name="degradedServices">Services to degrade when unhealthy</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when impact levels are invalid</exception>
        public HealthCheckConfigBuilder WithDegradationImpact(
            DegradationLevel degradedImpact = DegradationLevel.Minor,
            DegradationLevel unhealthyImpact = DegradationLevel.Moderate,
            IEnumerable<FixedString64Bytes> disabledFeatures = null,
            IEnumerable<FixedString64Bytes> degradedServices = null)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(DegradationLevel), degradedImpact))
                throw new ArgumentException($"Invalid degraded impact level: {degradedImpact}", nameof(degradedImpact));
            
            if (!Enum.IsDefined(typeof(DegradationLevel), unhealthyImpact))
                throw new ArgumentException($"Invalid unhealthy impact level: {unhealthyImpact}", nameof(unhealthyImpact));
            
            if (degradedImpact >= unhealthyImpact)
                _logger.LogWarning("Degraded impact level should typically be less severe than unhealthy impact level");
            
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = degradedImpact,
                UnhealthyImpact = unhealthyImpact,
                DisabledFeatures = disabledFeatures != null ? new HashSet<FixedString64Bytes>(disabledFeatures) : new(),
                DegradedServices = degradedServices != null ? new HashSet<FixedString64Bytes>(degradedServices) : new()
            };
            
            _logger.LogDebug($"Degradation impact: degraded={degradedImpact}, unhealthy={unhealthyImpact}");
            return this;
        }

        /// <summary>
        /// Configures validation for health check results
        /// </summary>
        /// <param name="enabled">Whether to enable validation</param>
        /// <param name="minExecutionTime">Minimum acceptable execution time</param>
        /// <param name="maxExecutionTime">Maximum acceptable execution time</param>
        /// <param name="requiredDataFields">Required data fields in results</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when execution times are invalid</exception>
        public HealthCheckConfigBuilder WithValidation(
            bool enabled = true,
            TimeSpan? minExecutionTime = null,
            TimeSpan? maxExecutionTime = null,
            IEnumerable<string> requiredDataFields = null)
        {
            ThrowIfAlreadyBuilt();
            
            var minTime = minExecutionTime ?? TimeSpan.FromMilliseconds(1);
            var maxTime = maxExecutionTime ?? TimeSpan.FromMinutes(5);
            
            if (minTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minExecutionTime), "Min execution time must be non-negative");
            
            if (maxTime <= minTime)
                throw new ArgumentOutOfRangeException(nameof(maxExecutionTime), "Max execution time must be greater than min execution time");
            
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = enabled,
                MinExecutionTime = minTime,
                MaxExecutionTime = maxTime,
                RequiredDataFields = requiredDataFields != null ? new HashSet<string>(requiredDataFields) : new()
            };
            
            _logger.LogDebug($"Validation: enabled={enabled}, minTime={minTime}, maxTime={maxTime}");
            return this;
        }

        /// <summary>
        /// Configures resource limits for the health check
        /// </summary>
        /// <param name="maxMemoryUsage">Maximum memory usage in bytes (0 = no limit)</param>
        /// <param name="maxCpuUsage">Maximum CPU usage percentage (0 = no limit)</param>
        /// <param name="maxConcurrentExecutions">Maximum concurrent executions</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public HealthCheckConfigBuilder WithResourceLimits(
            long maxMemoryUsage = 0,
            double maxCpuUsage = 0,
            int maxConcurrentExecutions = 1)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxMemoryUsage < 0)
                throw new ArgumentOutOfRangeException(nameof(maxMemoryUsage), "Max memory usage must be non-negative");
            
            if (maxCpuUsage < 0 || maxCpuUsage > 100)
                throw new ArgumentOutOfRangeException(nameof(maxCpuUsage), "Max CPU usage must be between 0 and 100");
            
            if (maxConcurrentExecutions < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentExecutions), "Max concurrent executions must be at least 1");
            
            _resourceLimits = new ResourceLimitsConfig
            {
                MaxMemoryUsage = maxMemoryUsage,
                MaxCpuUsage = maxCpuUsage,
                MaxConcurrentExecutions = maxConcurrentExecutions
            };
            
            _allowConcurrentExecution = maxConcurrentExecutions > 1;
            
            _logger.LogDebug($"Resource limits: memory={maxMemoryUsage}, cpu={maxCpuUsage}%, concurrent={maxConcurrentExecutions}");
            return this;
        }

        /// <summary>
        /// Applies a preset configuration for the specified scenario
        /// </summary>
        /// <param name="preset">Configuration preset to apply</param>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckConfigBuilder ForScenario(HealthCheckScenario preset)
        {
            ThrowIfAlreadyBuilt();
            
            switch (preset)
            {
                case HealthCheckScenario.CriticalSystem:
                    return ApplyCriticalSystemPreset();
                
                case HealthCheckScenario.Database:
                    return ApplyDatabasePreset();
                
                case HealthCheckScenario.NetworkService:
                    return ApplyNetworkServicePreset();
                
                case HealthCheckScenario.PerformanceMonitoring:
                    return ApplyPerformanceMonitoringPreset();
                
                case HealthCheckScenario.Development:
                    return ApplyDevelopmentPreset();
                
                default:
                    throw new ArgumentException($"Unknown scenario: {preset}", nameof(preset));
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
            if (string.IsNullOrWhiteSpace(_displayName))
                _validationErrors.Add("Display name is required");
            
            if (_interval <= TimeSpan.Zero)
                _validationErrors.Add("Interval must be positive");
            
            if (_timeout <= TimeSpan.Zero)
                _validationErrors.Add("Timeout must be positive");
            
            if (_priority < 0)
                _validationErrors.Add("Priority must be non-negative");
            
            if (_overallStatusWeight < 0.0 || _overallStatusWeight > 1.0)
                _validationErrors.Add("Overall status weight must be between 0.0 and 1.0");
            
            if (_alertCooldown < TimeSpan.Zero)
                _validationErrors.Add("Alert cooldown must be non-negative");
            
            if (_maxHistorySize < 0)
                _validationErrors.Add("Max history size must be non-negative");
            
            if (_slowExecutionThreshold < 0)
                _validationErrors.Add("Slow execution threshold must be non-negative");
            
            // Validate dependencies don't include self-references
            if (_dependencies.Contains(_id))
                _validationErrors.Add("Health check cannot depend on itself");
            
            // Validate nested configurations
            _validationErrors.AddRange(_retryConfig.Validate());
            _validationErrors.AddRange(_degradationImpact.Validate());
            _validationErrors.AddRange(_validationConfig.Validate());
            _validationErrors.AddRange(_resourceLimits.Validate());
            
            if (_circuitBreakerConfig != null)
                _validationErrors.AddRange(_circuitBreakerConfig.Validate());
            
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
        /// Builds the HealthCheckConfiguration instance
        /// </summary>
        /// <returns>Configured HealthCheckConfiguration instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or already built</exception>
        public HealthCheckConfiguration Build()
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
            
            var config = new HealthCheckConfiguration
            {
                Id = _id,
                DisplayName = _displayName,
                Description = _description,
                Category = _category,
                Enabled = _enabled,
                Interval = _interval,
                Timeout = _timeout,
                Priority = _priority,
                EnableCircuitBreaker = _enableCircuitBreaker,
                CircuitBreakerConfig = _circuitBreakerConfig,
                IncludeInOverallStatus = _includeInOverallStatus,
                OverallStatusWeight = _overallStatusWeight,
                EnableAlerting = _enableAlerting,
                AlertSeverities = new Dictionary<HealthStatus, AlertSeverity>(_alertSeverities),
                AlertOnlyOnStatusChange = _alertOnlyOnStatusChange,
                AlertCooldown = _alertCooldown,
                Tags = new HashSet<FixedString64Bytes>(_tags),
                Metadata = new Dictionary<string, object>(_metadata),
                Dependencies = new HashSet<FixedString64Bytes>(_dependencies),
                SkipOnUnhealthyDependencies = _skipOnUnhealthyDependencies,
                MaxHistorySize = _maxHistorySize,
                EnableDetailedLogging = _enableDetailedLogging,
                LogLevel = _logLevel,
                EnableProfiling = _enableProfiling,
                SlowExecutionThreshold = _slowExecutionThreshold,
                CustomParameters = new Dictionary<string, object>(_customParameters),
                RetryConfig = _retryConfig,
                DegradationImpact = _degradationImpact,
                ValidationConfig = _validationConfig,
                AllowConcurrentExecution = _allowConcurrentExecution,
                ResourceLimits = _resourceLimits
            };
            
            _isBuilt = true;
            _logger.LogInfo($"HealthCheckConfiguration '{_displayName}' built successfully");
            
            return config;
        }

        /// <summary>
        /// Resets the builder to allow building a new configuration
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public HealthCheckConfigBuilder Reset()
        {
            _isBuilt = false;
            _isValidated = false;
            _validationErrors.Clear();
            _id = GenerateId();
            
            // Reset all configuration to defaults
            _displayName = string.Empty;
            _description = string.Empty;
            _category = HealthCheckCategory.Custom;
            _enabled = true;
            _interval = TimeSpan.FromSeconds(30);
            _timeout = TimeSpan.FromSeconds(30);
            _priority = 100;
            _enableCircuitBreaker = true;
            _circuitBreakerConfig = null;
            _includeInOverallStatus = true;
            _overallStatusWeight = 1.0;
            _enableAlerting = true;
            _alertSeverities = new()
            {
                { HealthStatus.Healthy, AlertSeverity.Low },
                { HealthStatus.Degraded, AlertSeverity.Warning },
                { HealthStatus.Unhealthy, AlertSeverity.Critical },
                { HealthStatus.Unknown, AlertSeverity.Warning }
            };
            _alertOnlyOnStatusChange = true;
            _alertCooldown = TimeSpan.FromMinutes(5);
            _tags = new();
            _metadata = new();
            _dependencies = new();
            _skipOnUnhealthyDependencies = true;
            _maxHistorySize = 100;
            _enableDetailedLogging = false;
            _logLevel = LogLevel.Info;
            _enableProfiling = true;
            _slowExecutionThreshold = 1000;
            _customParameters = new();
            _retryConfig = new();
            _degradationImpact = new();
            _validationConfig = new();
            _allowConcurrentExecution = true;
            _resourceLimits = new();
            
            _logger.LogDebug("Builder reset for new configuration");
            return this;
        }

        #region Private Methods

        /// <summary>
        /// Applies critical system preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckConfigBuilder ApplyCriticalSystemPreset()
        {
            _category = HealthCheckCategory.System;
            _interval = TimeSpan.FromSeconds(15);
            _timeout = TimeSpan.FromSeconds(10);
            _priority = 1000;
            _enableCircuitBreaker = true;
            _circuitBreakerConfig = CircuitBreakerConfig.ForCriticalService();
            _overallStatusWeight = 1.0;
            _alertSeverities = new Dictionary<HealthStatus, AlertSeverity>
            {
                { HealthStatus.Healthy, AlertSeverity.Low },
                { HealthStatus.Degraded, AlertSeverity.Critical },
                { HealthStatus.Unhealthy, AlertSeverity.Critical },
                { HealthStatus.Unknown, AlertSeverity.Critical }
            };
            _alertCooldown = TimeSpan.FromMinutes(1);
            _maxHistorySize = 200;
            _enableDetailedLogging = true;
            _logLevel = LogLevel.Warning;
            _slowExecutionThreshold = 500;
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = DegradationLevel.Moderate,
                UnhealthyImpact = DegradationLevel.Severe
            };
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = true,
                MinExecutionTime = TimeSpan.FromMilliseconds(1),
                MaxExecutionTime = TimeSpan.FromSeconds(10)
            };
            
            _logger.LogInfo("Applied critical system preset");
            return this;
        }

        /// <summary>
        /// Applies database preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckConfigBuilder ApplyDatabasePreset()
        {
            _category = HealthCheckCategory.Database;
            _interval = TimeSpan.FromSeconds(30);
            _timeout = TimeSpan.FromSeconds(15);
            _priority = 800;
            _enableCircuitBreaker = true;
            _circuitBreakerConfig = CircuitBreakerConfig.ForDatabase();
            _overallStatusWeight = 0.9;
            _alertCooldown = TimeSpan.FromMinutes(2);
            _maxHistorySize = 150;
            _slowExecutionThreshold = 2000;
            _retryConfig = new RetryConfig
            {
                MaxRetries = 2,
                RetryDelay = TimeSpan.FromSeconds(1),
                BackoffMultiplier = 2.0,
                MaxRetryDelay = TimeSpan.FromSeconds(10)
            };
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = DegradationLevel.Minor,
                UnhealthyImpact = DegradationLevel.Moderate
            };
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = true,
                MinExecutionTime = TimeSpan.FromMilliseconds(10),
                MaxExecutionTime = TimeSpan.FromSeconds(15),
                RequiredDataFields = new HashSet<string> { "ConnectionString", "ResponseTime" }
            };
            
            _logger.LogInfo("Applied database preset");
            return this;
        }

        /// <summary>
        /// Applies network service preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckConfigBuilder ApplyNetworkServicePreset()
        {
            _category = HealthCheckCategory.Network;
            _interval = TimeSpan.FromSeconds(45);
            _timeout = TimeSpan.FromSeconds(20);
            _priority = 600;
            _enableCircuitBreaker = true;
            _circuitBreakerConfig = CircuitBreakerConfig.ForNetworkService();
            _overallStatusWeight = 0.7;
            _alertCooldown = TimeSpan.FromMinutes(3);
            _maxHistorySize = 100;
            _slowExecutionThreshold = 3000;
            _retryConfig = new RetryConfig
            {
                MaxRetries = 3,
                RetryDelay = TimeSpan.FromSeconds(2),
                BackoffMultiplier = 1.5,
                MaxRetryDelay = TimeSpan.FromSeconds(30)
            };
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = DegradationLevel.Minor,
                UnhealthyImpact = DegradationLevel.Moderate,
                DisabledFeatures = new HashSet<FixedString64Bytes> { "ExternalIntegration" },
                DegradedServices = new HashSet<FixedString64Bytes> { "CacheService" }
            };
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = true,
                MinExecutionTime = TimeSpan.FromMilliseconds(50),
                MaxExecutionTime = TimeSpan.FromSeconds(20),
                RequiredDataFields = new HashSet<string> { "Endpoint", "StatusCode", "ResponseTime" }
            };
            
            _logger.LogInfo("Applied network service preset");
            return this;
        }

        /// <summary>
        /// Applies performance monitoring preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckConfigBuilder ApplyPerformanceMonitoringPreset()
        {
            _category = HealthCheckCategory.Performance;
            _interval = TimeSpan.FromMinutes(1);
            _timeout = TimeSpan.FromSeconds(30);
            _priority = 400;
            _enableCircuitBreaker = false;
            _overallStatusWeight = 0.5;
            _alertOnlyOnStatusChange = false;
            _alertCooldown = TimeSpan.FromMinutes(10);
            _maxHistorySize = 500;
            _enableDetailedLogging = true;
            _enableProfiling = true;
            _slowExecutionThreshold = 1000;
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = DegradationLevel.None,
                UnhealthyImpact = DegradationLevel.Minor
            };
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = true,
                MinExecutionTime = TimeSpan.FromMilliseconds(1),
                MaxExecutionTime = TimeSpan.FromSeconds(30),
                RequiredDataFields = new HashSet<string> { "CpuUsage", "MemoryUsage", "ResponseTime" }
            };
            _resourceLimits = new ResourceLimitsConfig
            {
                MaxMemoryUsage = 50_000_000, // 50MB
                MaxCpuUsage = 10.0, // 10%
                MaxConcurrentExecutions = 1
            };
            
            _logger.LogInfo("Applied performance monitoring preset");
            return this;
        }

        /// <summary>
        /// Applies development preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private HealthCheckConfigBuilder ApplyDevelopmentPreset()
        {
            _category = HealthCheckCategory.Custom;
            _interval = TimeSpan.FromSeconds(10);
            _timeout = TimeSpan.FromSeconds(5);
            _priority = 100;
            _enableCircuitBreaker = false;
            _enableAlerting = false;
            _maxHistorySize = 50;
            _enableDetailedLogging = true;
            _logLevel = LogLevel.Debug;
            _slowExecutionThreshold = 500;
            _degradationImpact = new DegradationImpactConfig
            {
                DegradedImpact = DegradationLevel.None,
                UnhealthyImpact = DegradationLevel.None
            };
            _validationConfig = new HealthCheckValidationConfig
            {
                EnableValidation = false
            };
            _resourceLimits = new ResourceLimitsConfig
            {
                MaxMemoryUsage = 0,
                MaxCpuUsage = 0,
                MaxConcurrentExecutions = 1
            };
            
            _logger.LogInfo("Applied development preset");
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

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }

        #endregion
    }
}