using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Configs
{
    /// <summary>
    /// Configuration for individual health checks with comprehensive settings
    /// </summary>
    public sealed record HealthCheckConfiguration
    {
        /// <summary>
        /// Unique identifier for this health check configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; } = GenerateId();

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
        /// Whether this health check is enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Interval between automatic executions of this health check
        /// </summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum time this health check is allowed to run
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Priority of this health check (higher numbers execute first)
        /// </summary>
        public int Priority { get; init; } = 100;

        /// <summary>
        /// Whether to enable circuit breaker protection for this health check
        /// </summary>
        public bool EnableCircuitBreaker { get; init; } = true;

        /// <summary>
        /// Circuit breaker configuration specific to this health check
        /// </summary>
        public CircuitBreakerConfig CircuitBreakerConfig { get; init; }

        /// <summary>
        /// Whether to include this health check in overall health status calculation
        /// </summary>
        public bool IncludeInOverallStatus { get; init; } = true;

        /// <summary>
        /// Weight of this health check in overall status calculation (0.0 to 1.0)
        /// </summary>
        public double OverallStatusWeight { get; init; } = 1.0;

        /// <summary>
        /// Whether to enable alerting for this health check
        /// </summary>
        public bool EnableAlerting { get; init; } = true;

        /// <summary>
        /// Custom alert severities for different health statuses
        /// </summary>
        public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; } = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Low },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };

        /// <summary>
        /// Whether to alert only on status changes or on every execution
        /// </summary>
        public bool AlertOnlyOnStatusChange { get; init; } = true;

        /// <summary>
        /// Minimum time between alerts for the same status
        /// </summary>
        public TimeSpan AlertCooldown { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Custom tags for this health check
        /// </summary>
        public HashSet<FixedString64Bytes> Tags { get; init; } = new();

        /// <summary>
        /// Custom metadata for this health check
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public HashSet<FixedString64Bytes> Dependencies { get; init; } = new();

        /// <summary>
        /// Whether to skip this health check if dependencies are unhealthy
        /// </summary>
        public bool SkipOnUnhealthyDependencies { get; init; } = true;

        /// <summary>
        /// Maximum number of historical results to keep for this health check
        /// </summary>
        public int MaxHistorySize { get; init; } = 100;

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
        public int SlowExecutionThreshold { get; init; } = 1000;

        /// <summary>
        /// Custom configuration parameters specific to the health check implementation
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; init; } = new();

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
        /// Whether this health check can run concurrently with others
        /// </summary>
        public bool AllowConcurrentExecution { get; init; } = true;

        /// <summary>
        /// Resource limits for this health check execution
        /// </summary>
        public ResourceLimitsConfig ResourceLimits { get; init; } = new();

        /// <summary>
        /// Validates this configuration and returns any validation errors
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(DisplayName))
                errors.Add("DisplayName cannot be null or empty");

            if (Interval <= TimeSpan.Zero)
                errors.Add("Interval must be greater than zero");

            if (Timeout <= TimeSpan.Zero)
                errors.Add("Timeout must be greater than zero");

            if (Timeout > TimeSpan.FromMinutes(10))
                errors.Add("Timeout should not exceed 10 minutes for stability");

            if (Priority < 0)
                errors.Add("Priority must be non-negative");

            if (OverallStatusWeight < 0.0 || OverallStatusWeight > 1.0)
                errors.Add("OverallStatusWeight must be between 0.0 and 1.0");

            if (AlertCooldown < TimeSpan.Zero)
                errors.Add("AlertCooldown must be non-negative");

            if (MaxHistorySize < 0)
                errors.Add("MaxHistorySize must be non-negative");

            if (SlowExecutionThreshold < 0)
                errors.Add("SlowExecutionThreshold must be non-negative");

            // Validate nested configurations
            if (CircuitBreakerConfig != null)
                errors.AddRange(CircuitBreakerConfig.Validate());

            errors.AddRange(RetryConfig.Validate());
            errors.AddRange(DegradationImpact.Validate());
            errors.AddRange(ValidationConfig.Validate());
            errors.AddRange(ResourceLimits.Validate());

            // Validate alert severities
            foreach (var severity in AlertSeverities.Values)
            {
                if (!Enum.IsDefined(typeof(AlertSeverity), severity))
                    errors.Add($"Invalid alert severity: {severity}");
            }

            // Validate dependencies don't include self-references
            if (Dependencies.Contains(Id))
                errors.Add("Health check cannot depend on itself");

            return errors;
        }

        /// <summary>
        /// Creates a configuration optimized for critical system checks
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="description">Description of the health check</param>
        /// <returns>Critical system check configuration</returns>
        public static HealthCheckConfiguration ForCriticalSystem(string name, string description = null)
        {
            return new HealthCheckConfiguration
            {
                DisplayName = name,
                Description = description ?? $"Critical system check: {name}",
                Category = HealthCheckCategory.System,
                Interval = TimeSpan.FromSeconds(15),
                Timeout = TimeSpan.FromSeconds(10),
                Priority = 1000,
                EnableCircuitBreaker = true,
                CircuitBreakerConfig = CircuitBreakerConfig.ForCriticalService(),
                OverallStatusWeight = 1.0,
                AlertSeverities = new Dictionary<HealthStatus, AlertSeverity>
                {
                    { HealthStatus.Healthy, AlertSeverity.Low },
                    { HealthStatus.Degraded, AlertSeverity.Critical },
                    { HealthStatus.Unhealthy, AlertSeverity.Critical },
                    { HealthStatus.Unknown, AlertSeverity.Critical }
                },
                AlertCooldown = TimeSpan.FromMinutes(1),
                MaxHistorySize = 200,
                EnableDetailedLogging = true,
                LogLevel = LogLevel.Warning,
                SlowExecutionThreshold = 500
            };
        }

        /// <summary>
        /// Creates a configuration optimized for database health checks
        /// </summary>
        /// <param name="name">Name of the database</param>
        /// <param name="description">Description of the database check</param>
        /// <returns>Database health check configuration</returns>
        public static HealthCheckConfiguration ForDatabase(string name, string description = null)
        {
            return new HealthCheckConfiguration
            {
                DisplayName = name,
                Description = description ?? $"Database connectivity check: {name}",
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
                }
            };
        }

        /// <summary>
        /// Creates a configuration optimized for network service checks
        /// </summary>
        /// <param name="name">Name of the network service</param>
        /// <param name="description">Description of the network check</param>
        /// <returns>Network service health check configuration</returns>
        public static HealthCheckConfiguration ForNetworkService(string name, string description = null)
        {
            return new HealthCheckConfiguration
            {
                DisplayName = name,
                Description = description ?? $"Network service check: {name}",
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
                }
            };
        }

        /// <summary>
        /// Creates a configuration optimized for performance monitoring
        /// </summary>
        /// <param name="name">Name of the performance metric</param>
        /// <param name="description">Description of the performance check</param>
        /// <returns>Performance monitoring configuration</returns>
        public static HealthCheckConfiguration ForPerformanceMonitoring(string name, string description = null)
        {
            return new HealthCheckConfiguration
            {
                DisplayName = name,
                Description = description ?? $"Performance monitoring: {name}",
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
                SlowExecutionThreshold = 1000
            };
        }

        /// <summary>
        /// Creates a configuration optimized for development/testing
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="description">Description of the health check</param>
        /// <returns>Development/testing configuration</returns>
        public static HealthCheckConfiguration ForDevelopment(string name, string description = null)
        {
            return new HealthCheckConfiguration
            {
                DisplayName = name,
                Description = description ?? $"Development check: {name}",
                Category = HealthCheckCategory.Custom,
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                Priority = 100,
                EnableCircuitBreaker = false,
                EnableAlerting = false,
                MaxHistorySize = 50,
                EnableDetailedLogging = true,
                LogLevel = LogLevel.Debug,
                SlowExecutionThreshold = 500
            };
        }

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }
    }
}