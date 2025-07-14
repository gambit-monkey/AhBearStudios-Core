using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Comprehensive configuration for individual health checks with runtime state management
    /// </summary>
    public sealed record HealthCheckConfiguration
    {
        #region Identity and Basic Settings

        /// <summary>
        /// Unique identifier for this health check configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; } = GenerateId();

        /// <summary>
        /// Unique name for this health check (used for registration)
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Display name for this health check
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Detailed description of what this health check validates
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Category of this health check for organization
        /// </summary>
        public HealthCheckCategory Category { get; init; } = HealthCheckCategory.Custom;

        /// <summary>
        /// Version of this health check configuration
        /// </summary>
        public string Version { get; init; } = "1.0.0";

        #endregion

        #region Execution Settings

        /// <summary>
        /// Whether this health check is enabled (can be changed at runtime)
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Interval between automatic executions of this health check (1 second to 1 hour)
        /// </summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum time this health check is allowed to run (1 second to 10 minutes)
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Priority of this health check (higher numbers execute first)
        /// </summary>
        [Range(0, 10000)]
        public int Priority { get; init; } = 100;

        /// <summary>
        /// Whether this health check can run concurrently with others
        /// </summary>
        public bool AllowConcurrentExecution { get; init; } = true;

        /// <summary>
        /// Maximum number of concurrent executions allowed for this health check
        /// </summary>
        [Range(1, 100)]
        public int MaxConcurrentExecutions { get; init; } = 1;

        /// <summary>
        /// Whether to enable execution timeout enforcement
        /// </summary>
        public bool EnableExecutionTimeout { get; init; } = true;

        #endregion

        #region Circuit Breaker Settings

        /// <summary>
        /// Whether to enable circuit breaker protection for this health check
        /// </summary>
        public bool EnableCircuitBreaker { get; init; } = true;

        /// <summary>
        /// Circuit breaker configuration specific to this health check
        /// </summary>
        public CircuitBreakerConfig CircuitBreakerConfig { get; init; } = new();

        /// <summary>
        /// Whether to use circuit breaker fallback results when circuit is open
        /// </summary>
        public bool UseCircuitBreakerFallback { get; init; } = true;

        /// <summary>
        /// Custom fallback result when circuit breaker is open
        /// </summary>
        public HealthCheckResult FallbackResult { get; init; }

        /// <summary>
        /// Whether to automatically reset circuit breaker on successful execution
        /// </summary>
        public bool AutoResetCircuitBreaker { get; init; } = true;

        #endregion

        #region Health Status and Aggregation

        /// <summary>
        /// Whether to include this health check in overall health status calculation
        /// </summary>
        public bool IncludeInOverallStatus { get; init; } = true;

        /// <summary>
        /// Weight of this health check in overall status calculation (0.0 to 1.0)
        /// </summary>
        [Range(0.0f, 1.0f)]
        public double OverallStatusWeight { get; init; } = 1.0;

        /// <summary>
        /// Whether this health check is critical (failure causes system to be unhealthy)
        /// </summary>
        public bool IsCritical { get; init; } = false;

        /// <summary>
        /// Custom health status mappings for different result conditions
        /// </summary>
        public Dictionary<string, HealthStatus> CustomStatusMappings { get; init; } = new();

        /// <summary>
        /// Minimum status level required for this check to be considered healthy
        /// </summary>
        public HealthStatus MinimumHealthyStatus { get; init; } = HealthStatus.Healthy;

        #endregion

        #region Alerting and Notifications

        /// <summary>
        /// Whether to enable alerting for this health check
        /// </summary>
        public bool EnableAlerting { get; init; } = true;

        /// <summary>
        /// Custom alert severities for different health statuses
        /// </summary>
        public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; } = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };

        /// <summary>
        /// Whether to alert only on status changes or on every execution
        /// </summary>
        public bool AlertOnlyOnStatusChange { get; init; } = true;

        /// <summary>
        /// Number of consecutive failures before triggering an alert
        /// </summary>
        [Range(1, 100)]
        public int AlertFailureThreshold { get; init; } = 3;

        /// <summary>
        /// Minimum time between alerts for the same status (1 second to 24 hours)
        /// </summary>
        public TimeSpan AlertCooldown { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable circuit breaker state change alerts
        /// </summary>
        public bool EnableCircuitBreakerAlerts { get; init; } = true;

        #endregion

        #region Dependencies and Relationships

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public HashSet<FixedString64Bytes> Dependencies { get; init; } = new();

        /// <summary>
        /// Whether to skip this health check if dependencies are unhealthy
        /// </summary>
        public bool SkipOnUnhealthyDependencies { get; init; } = true;

        /// <summary>
        /// Whether to validate dependency health before execution
        /// </summary>
        public bool ValidateDependencies { get; init; } = true;

        /// <summary>
        /// Maximum depth for dependency validation
        /// </summary>
        [Range(1, 10)]
        public int MaxDependencyDepth { get; init; } = 3;

        /// <summary>
        /// Health checks that depend on this one
        /// </summary>
        public HashSet<FixedString64Bytes> Dependents { get; init; } = new();

        #endregion

        #region History and Statistics

        /// <summary>
        /// Maximum number of historical results to keep for this health check
        /// </summary>
        [Range(10, 10000)]
        public int MaxHistorySize { get; init; } = 100;

        /// <summary>
        /// Whether to enable historical result tracking
        /// </summary>
        public bool EnableHistoryTracking { get; init; } = true;

        /// <summary>
        /// Maximum age of historical results to keep (1 minute to 7 days)
        /// </summary>
        public TimeSpan MaxHistoryAge { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Whether to enable statistics collection for this health check
        /// </summary>
        public bool EnableStatisticsCollection { get; init; } = true;

        /// <summary>
        /// Whether to track execution performance metrics
        /// </summary>
        public bool EnablePerformanceTracking { get; init; } = true;

        #endregion

        #region Logging and Profiling

        /// <summary>
        /// Whether to enable detailed logging for this health check
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;

        /// <summary>
        /// Log level for this health check operations
        /// </summary>
        public LogLevel LogLevel { get; init; } = LogLevel.Info;

        /// <summary>
        /// Whether to enable performance profiling for this health check
        /// </summary>
        public bool EnableProfiling { get; init; } = true;

        /// <summary>
        /// Threshold for considering this health check slow (in milliseconds)
        /// </summary>
        [Range(100, 300000)]
        public int SlowExecutionThreshold { get; init; } = 1000;

        /// <summary>
        /// Whether to log slow execution warnings
        /// </summary>
        public bool LogSlowExecutions { get; init; } = true;

        /// <summary>
        /// Whether to enable correlation ID tracking
        /// </summary>
        public bool EnableCorrelationIds { get; init; } = true;

        #endregion

        #region Metadata and Tagging

        /// <summary>
        /// Custom tags for this health check
        /// </summary>
        public HashSet<FixedString64Bytes> Tags { get; init; } = new();

        /// <summary>
        /// Custom metadata for this health check
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Custom configuration parameters specific to the health check implementation
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; init; } = new();

        /// <summary>
        /// Environment-specific settings
        /// </summary>
        public Dictionary<string, object> EnvironmentSettings { get; init; } = new();

        #endregion

        #region Advanced Configuration

        /// <summary>
        /// Retry configuration for failed health checks
        /// </summary>
        public RetryConfig RetryConfig { get; init; } = new();

        /// <summary>
        /// Degradation impact configuration
        /// </summary>
        public DegradationImpactConfig DegradationImpact { get; init; } = new();

        /// <summary>
        /// Validation rules for health check results
        /// </summary>
        public HealthCheckValidationConfig ValidationConfig { get; init; } = new();

        /// <summary>
        /// Resource limits for this health check execution
        /// </summary>
        public ResourceLimitsConfig ResourceLimits { get; init; } = new();

        /// <summary>
        /// Caching configuration for health check results
        /// </summary>
        public CachingConfig CachingConfig { get; init; } = new();

        /// <summary>
        /// Schedule configuration for advanced scheduling patterns
        /// </summary>
        public ScheduleConfig ScheduleConfig { get; init; } = new();

        #endregion

        #region Runtime State Support

        /// <summary>
        /// Whether this configuration supports runtime modifications
        /// </summary>
        public bool AllowRuntimeModification { get; init; } = true;

        /// <summary>
        /// Timestamp when this configuration was created
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when this configuration was last modified
        /// </summary>
        public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// User or system that created this configuration
        /// </summary>
        public string CreatedBy { get; init; } = "System";

        /// <summary>
        /// User or system that last modified this configuration
        /// </summary>
        public string ModifiedBy { get; init; } = "System";

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates this configuration and returns any validation errors
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Basic validation
            if (Name.IsEmpty)
                errors.Add("Name cannot be empty");

            if (string.IsNullOrWhiteSpace(DisplayName))
                errors.Add("DisplayName cannot be null or empty");

            // TimeSpan validation
            if (Interval <= TimeSpan.Zero)
                errors.Add("Interval must be greater than zero");

            if (Interval > TimeSpan.FromHours(1))
                errors.Add("Interval should not exceed 1 hour");

            if (Timeout <= TimeSpan.Zero)
                errors.Add("Timeout must be greater than zero");

            if (Timeout > TimeSpan.FromMinutes(10))
                errors.Add("Timeout should not exceed 10 minutes for stability");

            if (AlertCooldown < TimeSpan.Zero)
                errors.Add("AlertCooldown must be non-negative");

            if (AlertCooldown > TimeSpan.FromHours(24))
                errors.Add("AlertCooldown should not exceed 24 hours");

            if (MaxHistoryAge <= TimeSpan.Zero)
                errors.Add("MaxHistoryAge must be greater than zero");

            if (MaxHistoryAge > TimeSpan.FromDays(7))
                errors.Add("MaxHistoryAge should not exceed 7 days");

            // Numeric validation
            if (Priority < 0)
                errors.Add("Priority must be non-negative");

            if (OverallStatusWeight < 0.0 || OverallStatusWeight > 1.0)
                errors.Add("OverallStatusWeight must be between 0.0 and 1.0");

            if (MaxHistorySize < 0)
                errors.Add("MaxHistorySize must be non-negative");

            if (SlowExecutionThreshold < 0)
                errors.Add("SlowExecutionThreshold must be non-negative");

            if (MaxConcurrentExecutions < 1)
                errors.Add("MaxConcurrentExecutions must be at least 1");

            if (AlertFailureThreshold < 1)
                errors.Add("AlertFailureThreshold must be at least 1");

            if (MaxDependencyDepth < 1)
                errors.Add("MaxDependencyDepth must be at least 1");

            // Validate nested configurations
            if (CircuitBreakerConfig != null)
                errors.AddRange(CircuitBreakerConfig.Validate());

            if (RetryConfig != null)
                errors.AddRange(RetryConfig.Validate());

            if (DegradationImpact != null)
                errors.AddRange(DegradationImpact.Validate());

            if (ValidationConfig != null)
                errors.AddRange(ValidationConfig.Validate());

            if (ResourceLimits != null)
                errors.AddRange(ResourceLimits.Validate());

            if (CachingConfig != null)
                errors.AddRange(CachingConfig.Validate());

            if (ScheduleConfig != null)
                errors.AddRange(ScheduleConfig.Validate());

            // Validate alert severities
            foreach (var severity in AlertSeverities.Values)
            {
                if (!Enum.IsDefined(typeof(AlertSeverity), severity))
                    errors.Add($"Invalid alert severity: {severity}");
            }

            // Validate dependencies don't include self-references
            if (Dependencies.Contains(Name))
                errors.Add("Health check cannot depend on itself");

            // Validate dependents don't include self-references
            if (Dependents.Contains(Name))
                errors.Add("Health check cannot be dependent on itself");

            // Validate no circular dependencies
            if (Dependencies.Any(dep => Dependents.Contains(dep)))
                errors.Add("Circular dependency detected");

            // Validate timeout is reasonable compared to interval
            if (Timeout >= Interval)
                errors.Add("Timeout should be less than execution interval");

            // Validate custom status mappings
            foreach (var mapping in CustomStatusMappings)
            {
                if (!Enum.IsDefined(typeof(HealthStatus), mapping.Value))
                    errors.Add($"Invalid health status in custom mapping: {mapping.Value}");
            }

            return errors;
        }

        /// <summary>
        /// Validates configuration and throws exception if invalid
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public void ValidateAndThrow()
        {
            var errors = Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Invalid HealthCheckConfiguration: {string.Join(", ", errors)}");
            }
        }

        #endregion

        #region Configuration Helpers

        /// <summary>
        /// Creates a deep copy of this configuration with optional modifications
        /// </summary>
        /// <param name="modifications">Optional modifications to apply</param>
        /// <returns>New configuration instance with modifications</returns>
        public HealthCheckConfiguration WithModifications(Action<HealthCheckConfigBuilder> modifications = null)
        {
            var builder = new HealthCheckConfigBuilder(this);
            modifications?.Invoke(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a configuration suitable for runtime enable/disable operations
        /// </summary>
        /// <param name="enabled">Whether to enable the health check</param>
        /// <returns>New configuration with enabled state modified</returns>
        public HealthCheckConfiguration WithEnabled(bool enabled)
        {
            return this with { Enabled = enabled, ModifiedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Creates a configuration with modified interval
        /// </summary>
        /// <param name="interval">New execution interval</param>
        /// <returns>New configuration with modified interval</returns>
        public HealthCheckConfiguration WithInterval(TimeSpan interval)
        {
            return this with { Interval = interval, ModifiedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Creates a configuration with modified timeout
        /// </summary>
        /// <param name="timeout">New execution timeout</param>
        /// <returns>New configuration with modified timeout</returns>
        public HealthCheckConfiguration WithTimeout(TimeSpan timeout)
        {
            return this with { Timeout = timeout, ModifiedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Creates a configuration with additional metadata
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>New configuration with added metadata</returns>
        public HealthCheckConfiguration WithMetadata(string key, object value)
        {
            var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
            return this with { Metadata = newMetadata, ModifiedAt = DateTime.UtcNow };
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a configuration optimized for critical system checks
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the health check</param>
        /// <returns>Critical system check configuration</returns>
        public static HealthCheckConfiguration ForCriticalSystem(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Critical system check: {displayName}",
                Category = HealthCheckCategory.System,
                Interval = TimeSpan.FromSeconds(15),
                Timeout = TimeSpan.FromSeconds(10),
                Priority = 1000,
                IsCritical = true,
                EnableCircuitBreaker = true,
                CircuitBreakerConfig = CircuitBreakerConfig.ForCriticalService(),
                OverallStatusWeight = 1.0,
                AlertSeverities = new Dictionary<HealthStatus, AlertSeverity>
                {
                    { HealthStatus.Healthy, AlertSeverity.Info },
                    { HealthStatus.Warning, AlertSeverity.Critical },
                    { HealthStatus.Degraded, AlertSeverity.Critical },
                    { HealthStatus.Unhealthy, AlertSeverity.Emergency },
                    { HealthStatus.Critical, AlertSeverity.Emergency },
                    { HealthStatus.Offline, AlertSeverity.Emergency },
                    { HealthStatus.Unknown, AlertSeverity.Critical }
                },
                AlertFailureThreshold = 1,
                AlertCooldown = TimeSpan.FromMinutes(1),
                MaxHistorySize = 200,
                EnableDetailedLogging = true,
                LogLevel = LogLevel.Warning,
                SlowExecutionThreshold = 500,
                EnableCorrelationIds = true,
                EnablePerformanceTracking = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for database health checks
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the database check</param>
        /// <returns>Database health check configuration</returns>
        public static HealthCheckConfiguration ForDatabase(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Database connectivity check: {displayName}",
                Category = HealthCheckCategory.Database,
                Interval = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(15),
                Priority = 800,
                EnableCircuitBreaker = true,
                CircuitBreakerConfig = CircuitBreakerConfig.ForDatabase(),
                OverallStatusWeight = 0.9,
                AlertCooldown = TimeSpan.FromMinutes(2),
                MaxHistorySize = 150,
                SlowExecutionThreshold = 2000,
                RetryConfig = new RetryConfig
                {
                    MaxRetries = 2,
                    RetryDelay = TimeSpan.FromSeconds(1),
                    BackoffMultiplier = 2.0
                },
                EnableCorrelationIds = true,
                EnablePerformanceTracking = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for network service checks
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the network check</param>
        /// <returns>Network service health check configuration</returns>
        public static HealthCheckConfiguration ForNetworkService(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Network service check: {displayName}",
                Category = HealthCheckCategory.Network,
                Interval = TimeSpan.FromSeconds(45),
                Timeout = TimeSpan.FromSeconds(20),
                Priority = 600,
                EnableCircuitBreaker = true,
                CircuitBreakerConfig = CircuitBreakerConfig.ForNetworkService(),
                OverallStatusWeight = 0.7,
                AlertCooldown = TimeSpan.FromMinutes(3),
                MaxHistorySize = 100,
                SlowExecutionThreshold = 3000,
                RetryConfig = new RetryConfig
                {
                    MaxRetries = 3,
                    RetryDelay = TimeSpan.FromSeconds(2),
                    BackoffMultiplier = 1.5
                },
                EnableCorrelationIds = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for performance monitoring
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the performance check</param>
        /// <returns>Performance monitoring configuration</returns>
        public static HealthCheckConfiguration ForPerformanceMonitoring(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Performance monitoring: {displayName}",
                Category = HealthCheckCategory.Performance,
                Interval = TimeSpan.FromMinutes(1),
                Timeout = TimeSpan.FromSeconds(30),
                Priority = 400,
                EnableCircuitBreaker = false,
                OverallStatusWeight = 0.5,
                AlertOnlyOnStatusChange = false,
                AlertCooldown = TimeSpan.FromMinutes(10),
                MaxHistorySize = 500,
                EnableDetailedLogging = true,
                EnableProfiling = true,
                SlowExecutionThreshold = 1000,
                EnablePerformanceTracking = true,
                EnableStatisticsCollection = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for development/testing
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the health check</param>
        /// <returns>Development/testing configuration</returns>
        public static HealthCheckConfiguration ForDevelopment(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Development check: {displayName}",
                Category = HealthCheckCategory.Custom,
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                Priority = 100,
                EnableCircuitBreaker = false,
                EnableAlerting = false,
                MaxHistorySize = 50,
                EnableDetailedLogging = true,
                LogLevel = LogLevel.Debug,
                SlowExecutionThreshold = 500,
                EnableCorrelationIds = false,
                EnablePerformanceTracking = false,
                AllowRuntimeModification = true
            };
        }

        /// <summary>
        /// Creates a minimal configuration for lightweight health checks
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Description of the health check</param>
        /// <returns>Minimal configuration</returns>
        public static HealthCheckConfiguration Minimal(FixedString64Bytes name, string displayName, string description = null)
        {
            return new HealthCheckConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = description ?? $"Minimal check: {displayName}",
                Category = HealthCheckCategory.Custom,
                Interval = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromSeconds(10),
                Priority = 100,
                EnableCircuitBreaker = false,
                EnableAlerting = false,
                MaxHistorySize = 20,
                EnableDetailedLogging = false,
                EnableProfiling = false,
                EnableStatisticsCollection = false,
                EnablePerformanceTracking = false,
                EnableCorrelationIds = false,
                AllowRuntimeModification = false
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }

        /// <summary>
        /// Determines if two configurations are functionally equivalent
        /// </summary>
        /// <param name="other">Configuration to compare</param>
        /// <returns>True if functionally equivalent</returns>
        public bool IsFunctionallyEquivalent(HealthCheckConfiguration other)
        {
            if (other == null) return false;
            
            return Name.Equals(other.Name) &&
                   Enabled == other.Enabled &&
                   Interval == other.Interval &&
                   Timeout == other.Timeout &&
                   Priority == other.Priority &&
                   EnableCircuitBreaker == other.EnableCircuitBreaker &&
                   IncludeInOverallStatus == other.IncludeInOverallStatus &&
                   OverallStatusWeight.Equals(other.OverallStatusWeight);
        }

        #endregion
    }
}