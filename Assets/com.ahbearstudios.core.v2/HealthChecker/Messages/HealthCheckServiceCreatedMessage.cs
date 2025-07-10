using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Messages;

/// <summary>
/// Message published when a health check service is successfully created and initialized.
/// Provides comprehensive information about service configuration, capabilities, and initial state
/// for monitoring, auditing, and integration purposes.
/// </summary>
/// <param name="Id">Unique identifier for this message instance.</param>
/// <param name="TimestampTicks">UTC timestamp when the service was created, in ticks since Unix epoch.</param>
/// <param name="TypeCode">Message type code for efficient routing and filtering.</param>
/// <param name="ServiceId">Unique identifier for the health check service instance.</param>
/// <param name="ServiceName">Human-readable name of the health check service.</param>
/// <param name="ServiceVersion">Version of the health check service implementation.</param>
/// <param name="ConfigurationId">Identifier of the configuration used to create the service.</param>
/// <param name="InitializationDurationMs">Time taken to initialize the service in milliseconds.</param>
/// <param name="RegisteredHealthCheckCount">Number of health checks registered during service creation.</param>
/// <param name="EnabledFeatures">Comma-separated list of enabled service features.</param>
/// <param name="ServiceEndpoint">Network endpoint where the service is accessible (if applicable).</param>
/// <param name="AutoDiscoveryEnabled">Whether automatic health check discovery is enabled.</param>
/// <param name="DefaultExecutionIntervalMs">Default execution interval for health checks in milliseconds.</param>
/// <param name="MaxConcurrentHealthChecks">Maximum number of health checks that can run concurrently.</param>
/// <param name="RetentionPolicyDays">Number of days to retain health check history.</param>
/// <param name="AlertingEnabled">Whether alerting is enabled for this service instance.</param>
/// <param name="RemediationEnabled">Whether automatic remediation is enabled for this service instance.</param>
/// <param name="EnvironmentName">Name of the environment where the service is running.</param>
/// <param name="CreatedBy">Identity of the user or system that created the service.</param>
/// <param name="CorrelationId">Correlation identifier for tracking related operations.</param>
public readonly record struct HealthCheckServiceCreatedMessage(
    Guid Id,
    long TimestampTicks,
    ushort TypeCode,
    FixedString128Bytes ServiceId,
    FixedString64Bytes ServiceName,
    FixedString32Bytes ServiceVersion,
    FixedString128Bytes ConfigurationId,
    float InitializationDurationMs,
    int RegisteredHealthCheckCount,
    FixedString512Bytes EnabledFeatures,
    FixedString128Bytes ServiceEndpoint,
    bool AutoDiscoveryEnabled,
    int DefaultExecutionIntervalMs,
    int MaxConcurrentHealthChecks,
    int RetentionPolicyDays,
    bool AlertingEnabled,
    bool RemediationEnabled,
    FixedString64Bytes EnvironmentName,
    FixedString64Bytes CreatedBy,
    FixedString64Bytes CorrelationId
) : IMessage
{
    /// <summary>
    /// Gets whether the service was initialized quickly (under 1000ms).
    /// </summary>
    public bool IsFastInitialization => InitializationDurationMs < 1000f;

    /// <summary>
    /// Gets whether the service has health checks registered.
    /// </summary>
    public bool HasHealthChecks => RegisteredHealthCheckCount > 0;

    /// <summary>
    /// Gets whether the service has enterprise features enabled.
    /// </summary>
    public bool HasEnterpriseFeatures => AlertingEnabled || RemediationEnabled;

    /// <summary>
    /// Gets whether the service is configured for high availability.
    /// </summary>
    public bool IsHighAvailabilityConfiguration => MaxConcurrentHealthChecks > 10 && RetentionPolicyDays >= 7;

    /// <summary>
    /// Gets whether the service has a network endpoint configured.
    /// </summary>
    public bool HasNetworkEndpoint => !ServiceEndpoint.IsEmpty && ServiceEndpoint.Length > 0;

    /// <summary>
    /// Gets the initialization performance category.
    /// </summary>
    public ServiceInitializationPerformance GetInitializationPerformance()
    {
        return InitializationDurationMs switch
        {
            < 100f => ServiceInitializationPerformance.Excellent,
            < 500f => ServiceInitializationPerformance.Good,
            < 1000f => ServiceInitializationPerformance.Acceptable,
            < 5000f => ServiceInitializationPerformance.Slow,
            _ => ServiceInitializationPerformance.Poor
        };
    }

    /// <summary>
    /// Gets the service scale category based on registered health checks.
    /// </summary>
    public ServiceScale GetServiceScale()
    {
        return RegisteredHealthCheckCount switch
        {
            0 => ServiceScale.Empty,
            <= 5 => ServiceScale.Small,
            <= 20 => ServiceScale.Medium,
            <= 50 => ServiceScale.Large,
            _ => ServiceScale.Enterprise
        };
    }

    /// <summary>
    /// Gets a formatted summary of the service creation.
    /// </summary>
    /// <returns>A human-readable summary of the service creation event.</returns>
    public string GetFormattedSummary()
    {
        var performance = GetInitializationPerformance();
        var scale = GetServiceScale();
        var features = HasEnterpriseFeatures ? " with enterprise features" : "";
        var endpoint = HasNetworkEndpoint ? $" at {ServiceEndpoint}" : "";
        
        return $"Health Check Service '{ServiceName}' v{ServiceVersion} created successfully " +
               $"({scale} scale, {performance} performance){features}{endpoint}. " +
               $"Initialized {RegisteredHealthCheckCount} health checks in {InitializationDurationMs:F1}ms.";
    }

    /// <summary>
    /// Gets the enabled features as a parsed list.
    /// </summary>
    /// <returns>A list of enabled feature names.</returns>
    public IReadOnlyList<string> GetEnabledFeaturesList()
    {
        if (EnabledFeatures.IsEmpty)
            return Array.Empty<string>();

        return EnabledFeatures.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
    }

    /// <summary>
    /// Gets a dictionary representation of key service metrics.
    /// </summary>
    /// <returns>Dictionary containing key metrics for monitoring and reporting.</returns>
    public IReadOnlyDictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            ["service_id"] = ServiceId.ToString(),
            ["service_name"] = ServiceName.ToString(),
            ["service_version"] = ServiceVersion.ToString(),
            ["initialization_duration_ms"] = InitializationDurationMs,
            ["registered_health_check_count"] = RegisteredHealthCheckCount,
            ["auto_discovery_enabled"] = AutoDiscoveryEnabled,
            ["default_execution_interval_ms"] = DefaultExecutionIntervalMs,
            ["max_concurrent_health_checks"] = MaxConcurrentHealthChecks,
            ["retention_policy_days"] = RetentionPolicyDays,
            ["alerting_enabled"] = AlertingEnabled,
            ["remediation_enabled"] = RemediationEnabled,
            ["environment_name"] = EnvironmentName.ToString(),
            ["has_network_endpoint"] = HasNetworkEndpoint,
            ["initialization_performance"] = GetInitializationPerformance().ToString(),
            ["service_scale"] = GetServiceScale().ToString(),
            ["has_enterprise_features"] = HasEnterpriseFeatures,
            ["timestamp"] = DateTimeOffset.FromUnixTimeSeconds(TimestampTicks / TimeSpan.TicksPerSecond).ToString("O")
        };
    }

    /// <summary>
    /// Validates that all required fields are properly set and within acceptable ranges.
    /// </summary>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return Id != Guid.Empty
               && TimestampTicks > 0
               && !ServiceId.IsEmpty
               && !ServiceName.IsEmpty
               && !ServiceVersion.IsEmpty
               && !ConfigurationId.IsEmpty
               && InitializationDurationMs >= 0
               && RegisteredHealthCheckCount >= 0
               && DefaultExecutionIntervalMs > 0
               && MaxConcurrentHealthChecks > 0
               && RetentionPolicyDays > 0
               && !EnvironmentName.IsEmpty
               && !CorrelationId.IsEmpty;
    }

    /// <summary>
    /// Gets the DateTime representation of the TimestampTicks value.
    /// </summary>
    public DateTime GetTimestamp() => DateTimeOffset.FromUnixTimeSeconds(TimestampTicks / TimeSpan.TicksPerSecond).DateTime;

    /// <summary>
    /// Creates a new message with an updated correlation ID.
    /// </summary>
    /// <param name="correlationId">The new correlation ID.</param>
    /// <returns>A new message with the updated correlation ID.</returns>
    public HealthCheckServiceCreatedMessage WithCorrelationId(FixedString64Bytes correlationId)
    {
        return this with { CorrelationId = correlationId };
    }

    /// <summary>
    /// Creates a condensed version of this message with only essential information.
    /// Useful for high-frequency logging or storage-constrained scenarios.
    /// </summary>
    /// <returns>A condensed message containing only essential fields.</returns>
    public HealthCheckServiceCreatedMessage GetCondensedVersion()
    {
        return this with 
        { 
            EnabledFeatures = new FixedString512Bytes(""), // Clear large text fields
            ServiceEndpoint = new FixedString128Bytes(""),
            CreatedBy = new FixedString64Bytes("")
        };
    }
}



