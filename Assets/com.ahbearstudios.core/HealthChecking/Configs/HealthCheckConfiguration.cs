using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Simplified configuration for individual health checks focused on game development.
/// Designed for Unity with performance-first approach and 60+ FPS targets.
/// Follows CLAUDE.md patterns with static factory methods and no field initializers.
/// </summary>
public sealed record HealthCheckConfiguration
{
    #region Core Properties
    
    /// <summary>
    /// Unique identifier for this health check configuration
    /// </summary>
    public FixedString64Bytes Id { get; init; }
    
    /// <summary>
    /// Unique name for this health check (used for registration)
    /// </summary>
    public FixedString64Bytes Name { get; init; }
    
    /// <summary>
    /// Display name for this health check
    /// </summary>
    public string DisplayName { get; init; }
    
    /// <summary>
    /// Category of this health check for organization
    /// </summary>
    public HealthCheckCategory Category { get; init; }
    
    /// <summary>
    /// Whether this health check is enabled
    /// </summary>
    public bool Enabled { get; init; }
    
    /// <summary>
    /// Interval between automatic executions of this health check
    /// </summary>
    public TimeSpan Interval { get; init; }
    
    /// <summary>
    /// Maximum time this health check is allowed to run
    /// </summary>
    public TimeSpan Timeout { get; init; }
    
    /// <summary>
    /// Priority of this health check (higher numbers execute first)
    /// </summary>
    public int Priority { get; init; }
    
    #endregion

    #region Resilience Configuration
    
    /// <summary>
    /// Circuit breaker configuration for this health check
    /// </summary>
    public CircuitBreakerConfig CircuitBreaker { get; init; }
    
    /// <summary>
    /// Retry configuration for failed health checks
    /// </summary>
    public RetryConfig Retry { get; init; }
    
    /// <summary>
    /// Degradation configuration for this health check
    /// </summary>
    public DegradationConfig Degradation { get; init; }
    
    /// <summary>
    /// Performance configuration for this health check
    /// </summary>
    public PerformanceConfig Performance { get; init; }
    
    #endregion

    #region Monitoring Settings
    
    /// <summary>
    /// Whether to enable alerting for this health check
    /// </summary>
    public bool EnableAlerting { get; init; }
    
    /// <summary>
    /// Whether to enable performance profiling for this health check
    /// </summary>
    public bool EnableProfiling { get; init; }
    
    /// <summary>
    /// Whether to enable detailed logging for this health check
    /// </summary>
    public bool EnableDetailedLogging { get; init; }
    
    /// <summary>
    /// Log level for this health check operations
    /// </summary>
    public LogLevel LogLevel { get; init; }
    
    /// <summary>
    /// Custom alert severities for different health statuses
    /// </summary>
    public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; }
    
    /// <summary>
    /// Whether this health check is critical (failure causes system to be unhealthy)
    /// </summary>
    public bool IsCritical { get; init; }
    
    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a health check configuration with proper validation and defaults
    /// </summary>
    /// <param name="name">Unique name for the health check</param>
    /// <param name="displayName">Display name for the health check</param>
    /// <param name="category">Category of the health check</param>
    /// <returns>New HealthCheckConfiguration instance</returns>
    public static HealthCheckConfiguration Create(
        string name, 
        string displayName = null, 
        HealthCheckCategory category = HealthCheckCategory.Custom)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentException("Name cannot be null or empty", nameof(name));

        return new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("HealthCheck", name).ToString("N")[..16]),
            Name = name,
            DisplayName = displayName ?? name,
            Category = category,
            Enabled = true,
            Interval = TimeSpan.FromSeconds(30),
            Timeout = TimeSpan.FromSeconds(30),
            Priority = 100,
            CircuitBreaker = CircuitBreakerConfig.Create($"CB_{name}"),
            Retry = new RetryConfig(),
            Degradation = new DegradationConfig(),
            Performance = PerformanceConfig.ForProduction(),
            EnableAlerting = true,
            EnableProfiling = true,
            EnableDetailedLogging = false,
            LogLevel = LogLevel.Info,
            AlertSeverities = GetDefaultAlertSeverities(),
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates a configuration optimized for critical game systems
    /// </summary>
    /// <param name="name">Name of the health check</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Critical system check configuration</returns>
    public static HealthCheckConfiguration ForCriticalSystem(string name, string displayName = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentException("Name cannot be null or empty", nameof(name));

        return new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("CriticalHealthCheck", name).ToString("N")[..16]),
            Name = name,
            DisplayName = displayName ?? $"Critical: {name}",
            Category = HealthCheckCategory.System,
            Enabled = true,
            Interval = TimeSpan.FromSeconds(15),
            Timeout = TimeSpan.FromSeconds(10),
            Priority = 1000,
            CircuitBreaker = CircuitBreakerConfig.ForCriticalService(),
            Retry = new RetryConfig { MaxRetries = 1, RetryDelay = TimeSpan.FromSeconds(1) },
            Degradation = DegradationConfig.ForCriticalSystem(),
            Performance = PerformanceConfig.ForHighPerformanceGames(),
            EnableAlerting = true,
            EnableProfiling = true,
            EnableDetailedLogging = true,
            LogLevel = LogLevel.Warning,
            AlertSeverities = GetCriticalAlertSeverities(),
            IsCritical = true
        };
    }

    /// <summary>
    /// Creates a configuration optimized for database health checks
    /// </summary>
    /// <param name="name">Name of the health check</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Database health check configuration</returns>
    public static HealthCheckConfiguration ForDatabase(string name, string displayName = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentException("Name cannot be null or empty", nameof(name));

        return new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("DatabaseHealthCheck", name).ToString("N")[..16]),
            Name = name,
            DisplayName = displayName ?? $"Database: {name}",
            Category = HealthCheckCategory.Database,
            Enabled = true,
            Interval = TimeSpan.FromSeconds(30),
            Timeout = TimeSpan.FromSeconds(15),
            Priority = 800,
            CircuitBreaker = CircuitBreakerConfig.ForDatabase(),
            Retry = new RetryConfig { MaxRetries = 2, RetryDelay = TimeSpan.FromSeconds(1), BackoffMultiplier = 2.0 },
            Degradation = new DegradationConfig(),
            Performance = PerformanceConfig.ForProduction(),
            EnableAlerting = true,
            EnableProfiling = true,
            EnableDetailedLogging = false,
            LogLevel = LogLevel.Info,
            AlertSeverities = GetDefaultAlertSeverities(),
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates a configuration optimized for network service checks
    /// </summary>
    /// <param name="name">Name of the health check</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Network service health check configuration</returns>
    public static HealthCheckConfiguration ForNetwork(string name, string displayName = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentException("Name cannot be null or empty", nameof(name));

        return new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("NetworkHealthCheck", name).ToString("N")[..16]),
            Name = name,
            DisplayName = displayName ?? $"Network: {name}",
            Category = HealthCheckCategory.Network,
            Enabled = true,
            Interval = TimeSpan.FromSeconds(45),
            Timeout = TimeSpan.FromSeconds(20),
            Priority = 600,
            CircuitBreaker = CircuitBreakerConfig.ForNetworkService(),
            Retry = new RetryConfig { MaxRetries = 3, RetryDelay = TimeSpan.FromSeconds(2), BackoffMultiplier = 1.5 },
            Degradation = new DegradationConfig(),
            Performance = PerformanceConfig.ForProduction(),
            EnableAlerting = true,
            EnableProfiling = true,
            EnableDetailedLogging = false,
            LogLevel = LogLevel.Info,
            AlertSeverities = GetDefaultAlertSeverities(),
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates a minimal configuration for development
    /// </summary>
    /// <param name="name">Name of the health check</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Development configuration</returns>
    public static HealthCheckConfiguration ForDevelopment(string name, string displayName = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentException("Name cannot be null or empty", nameof(name));

        return new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("DevHealthCheck", name).ToString("N")[..16]),
            Name = name,
            DisplayName = displayName ?? $"Dev: {name}",
            Category = HealthCheckCategory.Custom,
            Enabled = true,
            Interval = TimeSpan.FromSeconds(10),
            Timeout = TimeSpan.FromSeconds(5),
            Priority = 100,
            CircuitBreaker = CircuitBreakerConfig.ForDevelopment(),
            Retry = new RetryConfig(),
            Degradation = DegradationConfig.ForDevelopment(),
            Performance = PerformanceConfig.ForDevelopment(),
            EnableAlerting = false,
            EnableProfiling = false,
            EnableDetailedLogging = true,
            LogLevel = LogLevel.Debug,
            AlertSeverities = GetDefaultAlertSeverities(),
            IsCritical = false
        };
    }

    #endregion

    #region Validation

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

        if (Timeout <= TimeSpan.Zero)
            errors.Add("Timeout must be greater than zero");

        if (Timeout >= Interval)
            errors.Add("Timeout should be less than execution interval");

        // Unity game development specific validations
        if (Timeout > TimeSpan.FromSeconds(30))
            errors.Add("Timeout should not exceed 30 seconds for game performance");

        // Numeric validation
        if (Priority < 0)
            errors.Add("Priority must be non-negative");

        // Validate nested configurations
        if (CircuitBreaker != null)
            errors.AddRange(CircuitBreaker.Validate());

        if (Retry != null)
            errors.AddRange(Retry.Validate());

        if (Degradation != null)
            errors.AddRange(Degradation.Validate());

        if (Performance != null)
            errors.AddRange(Performance.Validate());

        return errors;
    }

    /// <summary>
    /// Validates configuration and throws exception if invalid
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when configuration is invalid</exception>
    public void ValidateAndThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new System.InvalidOperationException($"Invalid HealthCheckConfiguration: {string.Join(", ", errors)}");
        }
    }

    #endregion

    #region Helper Methods

    private static Dictionary<HealthStatus, AlertSeverity> GetDefaultAlertSeverities()
    {
        return new Dictionary<HealthStatus, AlertSeverity>
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };
    }

    private static Dictionary<HealthStatus, AlertSeverity> GetCriticalAlertSeverities()
    {
        return new Dictionary<HealthStatus, AlertSeverity>
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Critical },
            { HealthStatus.Degraded, AlertSeverity.Critical },
            { HealthStatus.Unhealthy, AlertSeverity.Emergency },
            { HealthStatus.Critical, AlertSeverity.Emergency },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Critical }
        };
    }

    #endregion
}