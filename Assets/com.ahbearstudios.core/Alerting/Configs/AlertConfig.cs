using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Primary configuration class for the Alert System.
    /// Provides comprehensive settings for alert dispatching, filtering, history management, and performance optimization.
    /// Follows immutable design pattern for thread safety and production reliability.
    /// </summary>
    public record AlertConfig
    {
        /// <summary>
        /// Gets the minimum alert severity level that will be processed by the system.
        /// Alerts below this level will be filtered out immediately.
        /// </summary>
        [Required]
        public AlertSeverity MinimumSeverity { get; init; } = AlertSeverity.Warning;

        /// <summary>
        /// Gets whether alert suppression is enabled to prevent alert flooding.
        /// When enabled, duplicate and rate-limited alerts will be suppressed based on suppression rules.
        /// </summary>
        public bool EnableSuppression { get; init; } = true;

        /// <summary>
        /// Gets the default suppression window duration for duplicate alert detection.
        /// Alerts with identical content within this window will be suppressed.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan SuppressionWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets whether asynchronous alert processing is enabled for improved performance.
        /// When enabled, alerts are processed on background threads to avoid blocking the main thread.
        /// </summary>
        public bool EnableAsyncProcessing { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of concurrent alerts that can be processed simultaneously.
        /// This setting prevents resource exhaustion during alert storms.
        /// </summary>
        [Range(1, 1000)]
        public int MaxConcurrentAlerts { get; init; } = 100;

        /// <summary>
        /// Gets the alert processing timeout for individual alert dispatch operations.
        /// Alerts that exceed this timeout will be logged as failed and potentially retried.
        /// </summary>
        [Range(1, 300)]
        public TimeSpan ProcessingTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether alert history tracking is enabled for audit and analysis purposes.
        /// When enabled, all alerts are stored for the configured retention period.
        /// </summary>
        public bool EnableHistory { get; init; } = true;

        /// <summary>
        /// Gets the duration for which alert history is retained before automatic cleanup.
        /// Older alerts are automatically purged to prevent unbounded memory growth.
        /// </summary>
        [Range(1, 8760)] // 1 hour to 1 year
        public TimeSpan HistoryRetention { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets the maximum number of alert history entries to retain in memory.
        /// When this limit is reached, oldest entries are removed first (FIFO).
        /// </summary>
        [Range(100, 100000)]
        public int MaxHistoryEntries { get; init; } = 10000;

        /// <summary>
        /// Gets whether alert aggregation is enabled for grouping related alerts.
        /// When enabled, similar alerts are grouped together to reduce noise.
        /// </summary>
        public bool EnableAggregation { get; init; } = true;

        /// <summary>
        /// Gets the time window for alert aggregation grouping.
        /// Alerts within this window with similar characteristics will be grouped together.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan AggregationWindow { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets the maximum number of alerts that can be aggregated into a single group.
        /// This prevents unbounded aggregation groups during severe alert storms.
        /// </summary>
        [Range(2, 1000)]
        public int MaxAggregationSize { get; init; } = 50;

        /// <summary>
        /// Gets whether correlation ID tracking is enabled for alert correlation across systems.
        /// When enabled, alerts include correlation IDs for distributed tracing and debugging.
        /// </summary>
        public bool EnableCorrelationTracking { get; init; } = true;

        /// <summary>
        /// Gets the collection of configured alert channels.
        /// Each channel defines how alerts are delivered (log, console, network, etc.).
        /// </summary>
        [Required]
        public IReadOnlyList<ChannelConfig> Channels { get; init; } = Array.Empty<ChannelConfig>();

        /// <summary>
        /// Gets the collection of suppression rules for intelligent alert filtering.
        /// Rules are evaluated in order of priority to determine alert processing.
        /// </summary>
        public IReadOnlyList<SuppressionConfig> SuppressionRules { get; init; } = Array.Empty<SuppressionConfig>();

        /// <summary>
        /// Gets the buffer size for alert queue management.
        /// This determines how many alerts can be queued for processing before backpressure is applied.
        /// </summary>
        [Range(100, 10000)]
        public int AlertBufferSize { get; init; } = 1000;

        /// <summary>
        /// Gets whether Unity-specific integrations are enabled.
        /// When enabled, Unity console, notifications, and profiler integrations are activated.
        /// </summary>
        public bool EnableUnityIntegration { get; init; } = true;

        /// <summary>
        /// Gets whether performance metrics collection is enabled for the alert system.
        /// When enabled, alert processing metrics are collected and made available for monitoring.
        /// </summary>
        public bool EnableMetrics { get; init; } = true;

        /// <summary>
        /// Gets the collection of source-specific minimum severity overrides.
        /// Allows different alert sources to have different minimum severity levels.
        /// </summary>
        public IReadOnlyDictionary<FixedString64Bytes, AlertSeverity> SourceSeverityOverrides { get; init; } 
            = new Dictionary<FixedString64Bytes, AlertSeverity>();

        /// <summary>
        /// Gets whether circuit breaker integration is enabled for system protection.
        /// When enabled, the alert system will respect circuit breaker states from the health check system.
        /// </summary>
        public bool EnableCircuitBreakerIntegration { get; init; } = true;

        /// <summary>
        /// Gets the emergency escalation configuration for critical system failures.
        /// Defines behavior when normal alert processing fails or system health is severely degraded.
        /// </summary>
        public EmergencyEscalationConfig EmergencyEscalation { get; init; } = EmergencyEscalationConfig.Default;

        /// <summary>
        /// Validates the configuration for correctness and consistency.
        /// Throws <see cref="InvalidOperationException"/> if configuration is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails.</exception>
        public void Validate()
        {
            if (Channels.Count == 0)
                throw new InvalidOperationException("At least one alert channel must be configured.");

            if (SuppressionWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Suppression window must be greater than zero.");

            if (ProcessingTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Processing timeout must be greater than zero.");

            if (HistoryRetention <= TimeSpan.Zero)
                throw new InvalidOperationException("History retention must be greater than zero.");

            if (AggregationWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Aggregation window must be greater than zero.");

            // Validate all channels
            foreach (var channel in Channels)
            {
                channel.Validate();
            }

            // Validate all suppression rules
            foreach (var rule in SuppressionRules)
            {
                rule.Validate();
            }

            // Validate emergency escalation
            EmergencyEscalation.Validate();
        }

        /// <summary>
        /// Gets whether the specified alert severity meets the minimum threshold for processing.
        /// Takes into account source-specific overrides if configured.
        /// </summary>
        /// <param name="severity">The alert severity to check.</param>
        /// <param name="source">The optional alert source for source-specific overrides.</param>
        /// <returns>True if the severity meets the minimum threshold; otherwise, false.</returns>
        public bool MeetsMinimumSeverity(AlertSeverity severity, FixedString64Bytes source = default)
        {
            var minimumSeverity = source.IsEmpty || !SourceSeverityOverrides.ContainsKey(source)
                ? MinimumSeverity
                : SourceSeverityOverrides[source];

            return severity >= minimumSeverity;
        }

        /// <summary>
        /// Creates a default production-ready configuration with sensible defaults.
        /// Suitable for most production environments with basic alerting requirements.
        /// </summary>
        /// <returns>A default alert configuration.</returns>
        public static AlertConfig CreateDefault()
        {
            return new AlertConfig
            {
                MinimumSeverity = AlertSeverity.Warning,
                EnableSuppression = true,
                SuppressionWindow = TimeSpan.FromMinutes(5),
                EnableAsyncProcessing = true,
                MaxConcurrentAlerts = 100,
                ProcessingTimeout = TimeSpan.FromSeconds(30),
                EnableHistory = true,
                HistoryRetention = TimeSpan.FromHours(24),
                MaxHistoryEntries = 10000,
                EnableAggregation = true,
                AggregationWindow = TimeSpan.FromMinutes(2),
                MaxAggregationSize = 50,
                EnableCorrelationTracking = true,
                AlertBufferSize = 1000,
                EnableUnityIntegration = true,
                EnableMetrics = true,
                EnableCircuitBreakerIntegration = true,
                Channels = new[]
                {
                    ChannelConfig.CreateLogChannel(),
                    ChannelConfig.CreateConsoleChannel()
                },
                SuppressionRules = new[]
                {
                    SuppressionConfig.CreateDefaultDuplicateFilter(),
                    SuppressionConfig.CreateDefaultRateLimit()
                },
                EmergencyEscalation = EmergencyEscalationConfig.Default
            };
        }
    }

    /// <summary>
    /// Configuration for emergency escalation behavior during critical system failures.
    /// Defines fallback mechanisms when normal alert processing is compromised.
    /// </summary>
    public sealed record EmergencyEscalationConfig
    {
        /// <summary>
        /// Gets whether emergency escalation is enabled.
        /// When enabled, critical alerts will use fallback mechanisms if normal processing fails.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the threshold for triggering emergency escalation based on alert processing failures.
        /// When this percentage of alerts fail to process, emergency mode is activated.
        /// </summary>
        [Range(0.1, 1.0)]
        public double FailureThreshold { get; init; } = 0.8; // 80% failure rate

        /// <summary>
        /// Gets the time window for calculating failure rates.
        /// Failure rates are calculated over this rolling window.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the fallback channel to use during emergency escalation.
        /// This should be the most reliable channel available (typically console or log).
        /// </summary>
        [Required]
        public FixedString64Bytes FallbackChannel { get; init; } = "Console";

        /// <summary>
        /// Gets the minimum delay between emergency escalation attempts.
        /// Prevents rapid-fire emergency escalations that could overwhelm fallback systems.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan EscalationCooldown { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Validates the emergency escalation configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (IsEnabled && FallbackChannel.IsEmpty)
                throw new InvalidOperationException("Fallback channel must be specified when emergency escalation is enabled.");

            if (FailureThreshold <= 0 || FailureThreshold > 1)
                throw new InvalidOperationException("Failure threshold must be between 0 and 1.");

            if (EvaluationWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Evaluation window must be greater than zero.");

            if (EscalationCooldown <= TimeSpan.Zero)
                throw new InvalidOperationException("Escalation cooldown must be greater than zero.");
        }

        /// <summary>
        /// Gets the default emergency escalation configuration.
        /// </summary>
        public static EmergencyEscalationConfig Default => new()
        {
            IsEnabled = true,
            FailureThreshold = 0.8,
            EvaluationWindow = TimeSpan.FromMinutes(5),
            FallbackChannel = "Console",
            EscalationCooldown = TimeSpan.FromMinutes(1)
        };
    }
}