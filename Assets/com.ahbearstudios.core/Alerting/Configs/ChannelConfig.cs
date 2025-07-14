using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration class for individual alert channels.
    /// Defines channel-specific settings including delivery mechanisms, formatting, retry policies, and health monitoring.
    /// Supports multiple channel types: Log, Console, Network, Email, Unity Notifications.
    /// </summary>
    public record ChannelConfig
    {
        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// Must be unique across all configured channels in the alert system.
        /// </summary>
        [Required]
        [StringLength(64, MinimumLength = 1)]
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Gets the channel type identifier that determines the implementation to use.
        /// Supported types: Log, Console, Network, Email, UnityConsole, UnityNotification.
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 1)]
        public FixedString32Bytes ChannelType { get; init; }

        /// <summary>
        /// Gets whether this channel is enabled for alert processing.
        /// Disabled channels are skipped during alert dispatch but remain configured.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the minimum alert severity level that this channel will process.
        /// Alerts below this level are filtered out before reaching this channel.
        /// </summary>
        [Required]
        public AlertSeverity MinimumSeverity { get; init; } = AlertSeverity.Info;

        /// <summary>
        /// Gets the maximum alert severity level that this channel will process.
        /// Allows channels to handle only specific severity ranges.
        /// </summary>
        public AlertSeverity MaximumSeverity { get; init; } = AlertSeverity.Emergency;

        /// <summary>
        /// Gets the message format template for this channel.
        /// Supports placeholders: {Timestamp}, {Severity}, {Source}, {Message}, {Tag}, {CorrelationId}.
        /// </summary>
        [StringLength(512)]
        public string MessageFormat { get; init; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}";

        /// <summary>
        /// Gets the timestamp format string used in message formatting.
        /// Standard .NET DateTime format strings are supported.
        /// </summary>
        [StringLength(64)]
        public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Gets whether batch processing is enabled for this channel.
        /// When enabled, multiple alerts can be sent together to improve efficiency.
        /// </summary>
        public bool EnableBatching { get; init; } = false;

        /// <summary>
        /// Gets the maximum number of alerts to include in a single batch.
        /// Only applicable when batch processing is enabled.
        /// </summary>
        [Range(1, 1000)]
        public int BatchSize { get; init; } = 10;

        /// <summary>
        /// Gets the maximum time to wait before flushing a partial batch.
        /// Ensures alerts are not delayed indefinitely waiting for a full batch.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan BatchFlushInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the retry policy configuration for failed alert deliveries.
        /// Defines how many times and with what delays failed alerts should be retried.
        /// </summary>
        [Required]
        public RetryPolicyConfig RetryPolicy { get; init; } = RetryPolicyConfig.Default;

        /// <summary>
        /// Gets the timeout for individual alert send operations.
        /// Operations exceeding this timeout are considered failed and may be retried.
        /// </summary>
        [Range(1, 300)]
        public TimeSpan SendTimeout { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets whether health monitoring is enabled for this channel.
        /// When enabled, channel health is reported to the health check system.
        /// </summary>
        public bool EnableHealthMonitoring { get; init; } = true;

        /// <summary>
        /// Gets the interval for performing channel health checks.
        /// Health checks verify that the channel can successfully deliver alerts.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the collection of channel-specific configuration settings.
        /// Settings are interpreted by the specific channel implementation.
        /// </summary>
        public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the collection of tags used for alert filtering and routing.
        /// Channels can be configured to only process alerts with specific tags.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> AllowedTags { get; init; } = Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Gets the collection of sources that this channel should ignore.
        /// Alerts from these sources will be filtered out before processing.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> IgnoredSources { get; init; } = Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Gets the rate limiting configuration for this channel.
        /// Prevents overwhelming the channel with too many alerts in a short time period.
        /// </summary>
        public RateLimitConfig RateLimit { get; init; } = RateLimitConfig.Default;

        /// <summary>
        /// Gets the priority level for this channel during alert processing.
        /// Higher priority channels are processed first. Range: 1 (highest) to 1000 (lowest).
        /// </summary>
        [Range(1, 1000)]
        public int Priority { get; init; } = 500;

        /// <summary>
        /// Gets whether this channel should participate in emergency escalation.
        /// Emergency escalation channels are used when normal alert processing fails.
        /// </summary>
        public bool IsEmergencyChannel { get; init; } = false;

        /// <summary>
        /// Validates the channel configuration for correctness and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails.</exception>
        public void Validate()
        {
            if (Name.IsEmpty)
                throw new InvalidOperationException("Channel name cannot be empty.");

            if (ChannelType.IsEmpty)
                throw new InvalidOperationException("Channel type cannot be empty.");

            if (MinimumSeverity > MaximumSeverity)
                throw new InvalidOperationException("Minimum severity cannot be greater than maximum severity.");

            if (string.IsNullOrWhiteSpace(MessageFormat))
                throw new InvalidOperationException("Message format cannot be empty or whitespace.");

            if (EnableBatching && BatchSize <= 0)
                throw new InvalidOperationException("Batch size must be greater than zero when batching is enabled.");

            if (SendTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Send timeout must be greater than zero.");

            if (HealthCheckInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Health check interval must be greater than zero.");

            RetryPolicy.Validate();
            RateLimit.Validate();
        }

        /// <summary>
        /// Determines whether this channel should process the specified alert based on its configuration.
        /// </summary>
        /// <param name="alert">The alert to evaluate.</param>
        /// <returns>True if the channel should process the alert; otherwise, false.</returns>
        public bool ShouldProcessAlert(Alert alert)
        {
            if (!IsEnabled)
                return false;

            if (alert.Severity < MinimumSeverity || alert.Severity > MaximumSeverity)
                return false;

            if (IgnoredSources.Count > 0 && IgnoredSources.Contains(alert.Source))
                return false;

            if (AllowedTags.Count > 0 && !AllowedTags.Contains(alert.Tag))
                return false;

            return true;
        }

        /// <summary>
        /// Creates a default log channel configuration.
        /// Suitable for logging alerts to the application log system.
        /// </summary>
        /// <param name="name">Optional custom name for the channel.</param>
        /// <returns>A configured log channel.</returns>
        public static ChannelConfig CreateLogChannel(string name = "Log")
        {
            return new ChannelConfig
            {
                Name = name,
                ChannelType = "Log",
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(5),
                SendTimeout = TimeSpan.FromSeconds(5),
                Priority = 100,
                IsEmergencyChannel = true
            };
        }

        /// <summary>
        /// Creates a default console channel configuration.
        /// Suitable for displaying alerts in the application console.
        /// </summary>
        /// <param name="name">Optional custom name for the channel.</param>
        /// <returns>A configured console channel.</returns>
        public static ChannelConfig CreateConsoleChannel(string name = "Console")
        {
            var settings = new Dictionary<string, string>
            {
                ["EnableColors"] = "true",
                ["ColorMap"] = "Info:#00ff00,Warning:#ffff00,Critical:#ff0000,Emergency:#800000"
            };

            return new ChannelConfig
            {
                Name = name,
                ChannelType = "Console",
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Warning,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Timestamp:HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromSeconds(2),
                Settings = settings,
                Priority = 200,
                IsEmergencyChannel = true
            };
        }

        /// <summary>
        /// Creates a network channel configuration for webhook-based alerts.
        /// Suitable for sending alerts to external monitoring systems.
        /// </summary>
        /// <param name="name">The channel name.</param>
        /// <param name="endpoint">The webhook endpoint URL.</param>
        /// <param name="minimumSeverity">The minimum severity level.</param>
        /// <returns>A configured network channel.</returns>
        public static ChannelConfig CreateNetworkChannel(string name, string endpoint, AlertSeverity minimumSeverity = AlertSeverity.Critical)
        {
            var settings = new Dictionary<string, string>
            {
                ["Endpoint"] = endpoint,
                ["Method"] = "POST",
                ["ContentType"] = "application/json",
                ["UserAgent"] = "AhBearStudios-AlertSystem/2.0"
            };

            return new ChannelConfig
            {
                Name = name,
                ChannelType = "Network",
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "{{\"timestamp\":\"{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\",\"severity\":\"{Severity}\",\"source\":\"{Source}\",\"message\":\"{Message}\",\"tag\":\"{Tag}\",\"correlationId\":\"{CorrelationId}\"}}",
                EnableBatching = true,
                BatchSize = 10,
                BatchFlushInterval = TimeSpan.FromMinutes(2),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(3),
                SendTimeout = TimeSpan.FromSeconds(30),
                Settings = settings,
                RetryPolicy = new RetryPolicyConfig
                {
                    MaxAttempts = 3,
                    BaseDelay = TimeSpan.FromSeconds(5),
                    MaxDelay = TimeSpan.FromMinutes(2),
                    BackoffMultiplier = 2.0,
                    JitterEnabled = true
                },
                Priority = 300
            };
        }

        /// <summary>
        /// Creates an email channel configuration for email-based alerts.
        /// Suitable for sending critical alerts to system administrators.
        /// </summary>
        /// <param name="name">The channel name.</param>
        /// <param name="smtpServer">The SMTP server address.</param>
        /// <param name="fromEmail">The sender email address.</param>
        /// <param name="toEmails">The recipient email addresses.</param>
        /// <returns>A configured email channel.</returns>
        public static ChannelConfig CreateEmailChannel(string name, string smtpServer, string fromEmail, params string[] toEmails)
        {
            var settings = new Dictionary<string, string>
            {
                ["SmtpServer"] = smtpServer,
                ["SmtpPort"] = "587",
                ["EnableSsl"] = "true",
                ["FromEmail"] = fromEmail,
                ["ToEmails"] = string.Join(",", toEmails),
                ["Subject"] = "[ALERT] {Severity} - {Source}",
                ["UseHtml"] = "true"
            };

            return new ChannelConfig
            {
                Name = name,
                ChannelType = "Email",
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Critical,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "<h3>Alert Details</h3><p><strong>Timestamp:</strong> {Timestamp:yyyy-MM-dd HH:mm:ss}</p><p><strong>Severity:</strong> {Severity}</p><p><strong>Source:</strong> {Source}</p><p><strong>Message:</strong> {Message}</p><p><strong>Correlation ID:</strong> {CorrelationId}</p>",
                EnableBatching = true,
                BatchSize = 5,
                BatchFlushInterval = TimeSpan.FromMinutes(5),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(15),
                SendTimeout = TimeSpan.FromMinutes(1),
                Settings = settings,
                RetryPolicy = new RetryPolicyConfig
                {
                    MaxAttempts = 2,
                    BaseDelay = TimeSpan.FromMinutes(1),
                    MaxDelay = TimeSpan.FromMinutes(10),
                    BackoffMultiplier = 3.0,
                    JitterEnabled = false
                },
                Priority = 400
            };
        }

        /// <summary>
        /// Creates a Unity console channel configuration for Unity editor/runtime console output.
        /// </summary>
        /// <param name="name">Optional custom name for the channel.</param>
        /// <returns>A configured Unity console channel.</returns>
        public static ChannelConfig CreateUnityConsoleChannel(string name = "UnityConsole")
        {
            var settings = new Dictionary<string, string>
            {
                ["UseUnityLog"] = "true",
                ["EnableStackTrace"] = "true",
                ["LogTypeMapping"] = "Info:Log,Warning:Warning,Critical:Error,Emergency:Error"
            };

            return new ChannelConfig
            {
                Name = name,
                ChannelType = "UnityConsole",
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Warning,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Source}] {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromSeconds(1),
                Settings = settings,
                Priority = 150,
                IsEmergencyChannel = true
            };
        }
    }

    /// <summary>
    /// Configuration for retry policies when alert delivery fails.
    /// Implements exponential backoff with optional jitter for robust failure handling.
    /// </summary>
    public sealed record RetryPolicyConfig
    {
        /// <summary>
        /// Gets the maximum number of retry attempts for failed alert deliveries.
        /// Set to 0 to disable retries entirely.
        /// </summary>
        [Range(0, 10)]
        public int MaxAttempts { get; init; } = 3;

        /// <summary>
        /// Gets the base delay between retry attempts.
        /// Actual delay may be modified by backoff multiplier and jitter.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets the maximum delay between retry attempts.
        /// Prevents exponential backoff from creating excessively long delays.
        /// </summary>
        [Range(1, 7200)]
        public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the multiplier applied to the delay after each failed attempt.
        /// Values greater than 1.0 implement exponential backoff to reduce system load during failures.
        /// </summary>
        [Range(1.0, 10.0)]
        public double BackoffMultiplier { get; init; } = 2.0;

        /// <summary>
        /// Gets whether random jitter is added to retry delays.
        /// Jitter helps prevent thundering herd problems when multiple alerts fail simultaneously.
        /// </summary>
        public bool JitterEnabled { get; init; } = true;

        /// <summary>
        /// Gets the maximum jitter percentage to apply to retry delays.
        /// Jitter is applied as a random percentage of the calculated delay.
        /// </summary>
        [Range(0.0, 0.5)]
        public double JitterMaxPercentage { get; init; } = 0.1; // 10% jitter

        /// <summary>
        /// Validates the retry policy configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (MaxAttempts < 0)
                throw new InvalidOperationException("Max attempts cannot be negative.");

            if (BaseDelay <= TimeSpan.Zero)
                throw new InvalidOperationException("Base delay must be greater than zero.");

            if (MaxDelay <= TimeSpan.Zero)
                throw new InvalidOperationException("Max delay must be greater than zero.");

            if (MaxDelay < BaseDelay)
                throw new InvalidOperationException("Max delay cannot be less than base delay.");

            if (BackoffMultiplier < 1.0)
                throw new InvalidOperationException("Backoff multiplier must be at least 1.0.");

            if (JitterMaxPercentage < 0.0 || JitterMaxPercentage > 0.5)
                throw new InvalidOperationException("Jitter max percentage must be between 0.0 and 0.5.");
        }

        /// <summary>
        /// Gets the default retry policy configuration.
        /// </summary>
        public static RetryPolicyConfig Default => new()
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(5),
            BackoffMultiplier = 2.0,
            JitterEnabled = true,
            JitterMaxPercentage = 0.1
        };

        /// <summary>
        /// Gets a retry policy configuration with no retries.
        /// </summary>
        public static RetryPolicyConfig NoRetry => new()
        {
            MaxAttempts = 0,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffMultiplier = 1.0,
            JitterEnabled = false,
            JitterMaxPercentage = 0.0
        };
    }

    /// <summary>
    /// Configuration for rate limiting alert processing to prevent channel overload.
    /// Implements token bucket algorithm for smooth rate limiting with burst capacity.
    /// </summary>
    public sealed record RateLimitConfig
    {
        /// <summary>
        /// Gets whether rate limiting is enabled for this channel.
        /// When disabled, no rate limiting is applied regardless of other settings.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of alerts allowed per time window.
        /// This represents the sustained rate that the channel can handle.
        /// </summary>
        [Range(1, 10000)]
        public int MaxAlertsPerWindow { get; init; } = 100;

        /// <summary>
        /// Gets the time window for rate limit calculations.
        /// Rate limits are enforced over rolling windows of this duration.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan TimeWindow { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets the burst capacity for handling alert spikes.
        /// Allows temporary bursts above the sustained rate up to this limit.
        /// </summary>
        [Range(1, 10000)]
        public int BurstCapacity { get; init; } = 20;

        /// <summary>
        /// Gets the action to take when rate limits are exceeded.
        /// Options: Drop (discard), Queue (delay), or Escalate (send to emergency channel).
        /// </summary>
        public RateLimitAction ExceededAction { get; init; } = RateLimitAction.Queue;

        /// <summary>
        /// Gets the maximum queue size for delayed alerts when using Queue action.
        /// When queue is full, oldest alerts are dropped to make room for new ones.
        /// </summary>
        [Range(1, 1000)]
        public int MaxQueueSize { get; init; } = 50;

        /// <summary>
        /// Gets the maximum delay for queued alerts before they are dropped.
        /// Prevents indefinite queuing of alerts that may become stale.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan MaxQueueDelay { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Validates the rate limit configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (MaxAlertsPerWindow <= 0)
                throw new InvalidOperationException("Max alerts per window must be greater than zero.");

            if (TimeWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Time window must be greater than zero.");

            if (BurstCapacity <= 0)
                throw new InvalidOperationException("Burst capacity must be greater than zero.");

            if (MaxQueueSize <= 0)
                throw new InvalidOperationException("Max queue size must be greater than zero.");

            if (MaxQueueDelay <= TimeSpan.Zero)
                throw new InvalidOperationException("Max queue delay must be greater than zero.");
        }

        /// <summary>
        /// Gets the default rate limit configuration.
        /// </summary>
        public static RateLimitConfig Default => new()
        {
            IsEnabled = true,
            MaxAlertsPerWindow = 100,
            TimeWindow = TimeSpan.FromMinutes(1),
            BurstCapacity = 20,
            ExceededAction = RateLimitAction.Queue,
            MaxQueueSize = 50,
            MaxQueueDelay = TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// Gets a rate limit configuration with no limits.
        /// </summary>
        public static RateLimitConfig Unlimited => new()
        {
            IsEnabled = false,
            MaxAlertsPerWindow = int.MaxValue,
            TimeWindow = TimeSpan.FromHours(1),
            BurstCapacity = int.MaxValue,
            ExceededAction = RateLimitAction.Drop,
            MaxQueueSize = 1,
            MaxQueueDelay = TimeSpan.FromSeconds(1)
        };
    }

    /// <summary>
    /// Defines actions to take when rate limits are exceeded.
    /// </summary>
    public enum RateLimitAction
    {
        /// <summary>
        /// Drop the alert silently when rate limit is exceeded.
        /// </summary>
        Drop,

        /// <summary>
        /// Queue the alert for later delivery when rate limit allows.
        /// </summary>
        Queue,

        /// <summary>
        /// Escalate the alert to an emergency channel that bypasses rate limits.
        /// </summary>
        Escalate
    }
}