using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Interface for building alert configuration in a fluent manner.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// Provides comprehensive configuration options for all available alert channels, suppression rules, and system settings.
    /// Integrates with health checking, logging, and performance monitoring systems.
    /// </summary>
    public interface IAlertConfigBuilder
    {
        /// <summary>
        /// Sets the global minimum alert severity level that will be processed by the system.
        /// Alerts below this level will be filtered out immediately across all channels.
        /// </summary>
        /// <param name="severity">The minimum alert severity level</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithMinimumSeverity(AlertSeverity severity);

        /// <summary>
        /// Enables or disables alert suppression globally to prevent alert flooding.
        /// When enabled, duplicate and rate-limited alerts will be suppressed based on configured rules.
        /// </summary>
        /// <param name="enabled">True to enable suppression, false to disable</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppression(bool enabled);

        /// <summary>
        /// Sets the default suppression window duration for duplicate alert detection.
        /// Alerts with identical content within this window will be suppressed unless overridden by specific rules.
        /// </summary>
        /// <param name="windowSize">The suppression window duration</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppressionWindow(TimeSpan windowSize);

        /// <summary>
        /// Configures suppression with both enabled state and window size in a single call.
        /// Convenience method for common suppression configuration scenarios.
        /// </summary>
        /// <param name="enabled">True to enable suppression, false to disable</param>
        /// <param name="windowSize">The suppression window duration when enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppression(bool enabled, TimeSpan windowSize);

        /// <summary>
        /// Enables or disables asynchronous alert processing for improved performance.
        /// When enabled, alerts are processed on background threads to avoid blocking the main thread.
        /// </summary>
        /// <param name="enabled">True to enable asynchronous processing</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithAsyncProcessing(bool enabled);

        /// <summary>
        /// Sets the maximum number of concurrent alerts that can be processed simultaneously.
        /// This setting prevents resource exhaustion during alert storms and maintains system stability.
        /// </summary>
        /// <param name="maxConcurrentAlerts">The maximum number of concurrent alerts</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithMaxConcurrentAlerts(int maxConcurrentAlerts);

        /// <summary>
        /// Sets the alert processing timeout for individual alert dispatch operations.
        /// Alerts that exceed this timeout will be logged as failed and potentially retried.
        /// </summary>
        /// <param name="timeout">The processing timeout duration</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithProcessingTimeout(TimeSpan timeout);

        /// <summary>
        /// Enables or disables alert history tracking for audit and analysis purposes.
        /// When enabled, all alerts are stored for the configured retention period.
        /// </summary>
        /// <param name="enabled">True to enable history tracking</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithHistory(bool enabled);

        /// <summary>
        /// Configures alert history with both enabled state and retention period.
        /// Convenience method for complete history configuration in a single call.
        /// </summary>
        /// <param name="enabled">True to enable history tracking</param>
        /// <param name="retention">The duration for which alert history is retained</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithHistory(bool enabled, TimeSpan retention);

        /// <summary>
        /// Sets the maximum number of alert history entries to retain in memory.
        /// When this limit is reached, oldest entries are removed first (FIFO).
        /// </summary>
        /// <param name="maxEntries">The maximum number of history entries</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithHistoryLimit(int maxEntries);

        /// <summary>
        /// Enables or disables alert aggregation for grouping related alerts.
        /// When enabled, similar alerts are grouped together to reduce noise and improve clarity.
        /// </summary>
        /// <param name="enabled">True to enable aggregation</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithAggregation(bool enabled);

        /// <summary>
        /// Configures alert aggregation with both enabled state and window size.
        /// Alerts within the window with similar characteristics will be grouped together.
        /// </summary>
        /// <param name="enabled">True to enable aggregation</param>
        /// <param name="window">The time window for aggregation grouping</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithAggregation(bool enabled, TimeSpan window);

        /// <summary>
        /// Sets the maximum number of alerts that can be aggregated into a single group.
        /// This prevents unbounded aggregation groups during severe alert storms.
        /// </summary>
        /// <param name="maxSize">The maximum aggregation group size</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithAggregationLimit(int maxSize);

        /// <summary>
        /// Enables or disables correlation ID tracking for alert correlation across systems.
        /// When enabled, alerts include correlation IDs for distributed tracing and debugging.
        /// </summary>
        /// <param name="enabled">True to enable correlation tracking</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithCorrelationTracking(bool enabled);

        /// <summary>
        /// Sets the buffer size for alert queue management.
        /// This determines how many alerts can be queued for processing before backpressure is applied.
        /// </summary>
        /// <param name="bufferSize">The alert buffer size</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithBufferSize(int bufferSize);

        /// <summary>
        /// Enables or disables Unity-specific integrations.
        /// When enabled, Unity console, notifications, and profiler integrations are activated.
        /// </summary>
        /// <param name="enabled">True to enable Unity integration</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithUnityIntegration(bool enabled);

        /// <summary>
        /// Enables or disables performance metrics collection for the alert system.
        /// When enabled, alert processing metrics are collected and made available for monitoring.
        /// </summary>
        /// <param name="enabled">True to enable metrics collection</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithMetrics(bool enabled);

        /// <summary>
        /// Enables or disables circuit breaker integration for system protection.
        /// When enabled, the alert system will respect circuit breaker states from the health check system.
        /// </summary>
        /// <param name="enabled">True to enable circuit breaker integration</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithCircuitBreakerIntegration(bool enabled);

        /// <summary>
        /// Adds a pre-configured channel to the alert system.
        /// Channels define how alerts are delivered (log, console, network, email, etc.).
        /// </summary>
        /// <param name="channelConfig">The channel configuration to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithChannel(ChannelConfig channelConfig);

        /// <summary>
        /// Adds multiple pre-configured channels to the alert system.
        /// Convenience method for adding multiple channels in a single call.
        /// </summary>
        /// <param name="channelConfigs">The channel configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithChannels(params ChannelConfig[] channelConfigs);

        /// <summary>
        /// Adds multiple channels from a collection.
        /// Supports any enumerable collection of channel configurations.
        /// </summary>
        /// <param name="channelConfigs">The collection of channel configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithChannels(IEnumerable<ChannelConfig> channelConfigs);

        /// <summary>
        /// Configures channels using a fluent builder pattern.
        /// Provides access to a specialized builder for complex channel configuration scenarios.
        /// </summary>
        /// <param name="channelBuilder">Action to configure channels using the specialized builder</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithChannels(Action<IChannelConfigBuilder> channelBuilder);

        /// <summary>
        /// Adds a log channel with the specified configuration.
        /// Convenience method for adding log-based alert channels.
        /// </summary>
        /// <param name="name">The channel name (optional, defaults to "Log")</param>
        /// <param name="minimumSeverity">The minimum severity level (optional, defaults to Info)</param>
        /// <param name="enabled">Whether the channel is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithLogChannel(string name = "Log", AlertSeverity minimumSeverity = AlertSeverity.Info, bool enabled = true);

        /// <summary>
        /// Adds a console channel with the specified configuration.
        /// Convenience method for adding console-based alert channels.
        /// </summary>
        /// <param name="name">The channel name (optional, defaults to "Console")</param>
        /// <param name="minimumSeverity">The minimum severity level (optional, defaults to Warning)</param>
        /// <param name="enabled">Whether the channel is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithConsoleChannel(string name = "Console", AlertSeverity minimumSeverity = AlertSeverity.Warning, bool enabled = true);

        /// <summary>
        /// Adds a network channel with the specified endpoint and configuration.
        /// Convenience method for adding webhook-based alert channels.
        /// </summary>
        /// <param name="name">The channel name</param>
        /// <param name="endpoint">The webhook endpoint URL</param>
        /// <param name="minimumSeverity">The minimum severity level (optional, defaults to Critical)</param>
        /// <param name="enabled">Whether the channel is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithNetworkChannel(string name, string endpoint, AlertSeverity minimumSeverity = AlertSeverity.Critical, bool enabled = true);

        /// <summary>
        /// Adds an email channel with the specified SMTP configuration.
        /// Convenience method for adding email-based alert channels.
        /// </summary>
        /// <param name="name">The channel name</param>
        /// <param name="smtpServer">The SMTP server address</param>
        /// <param name="fromEmail">The sender email address</param>
        /// <param name="toEmails">The recipient email addresses</param>
        /// <param name="minimumSeverity">The minimum severity level (optional, defaults to Critical)</param>
        /// <param name="enabled">Whether the channel is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithEmailChannel(string name, string smtpServer, string fromEmail, string[] toEmails, AlertSeverity minimumSeverity = AlertSeverity.Critical, bool enabled = true);

        /// <summary>
        /// Adds a Unity console channel for Unity editor/runtime console output.
        /// Convenience method for Unity-specific console integration.
        /// </summary>
        /// <param name="name">The channel name (optional, defaults to "UnityConsole")</param>
        /// <param name="minimumSeverity">The minimum severity level (optional, defaults to Warning)</param>
        /// <param name="enabled">Whether the channel is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithUnityConsoleChannel(string name = "UnityConsole", AlertSeverity minimumSeverity = AlertSeverity.Warning, bool enabled = true);

        /// <summary>
        /// Adds a suppression rule to the alert system.
        /// Suppression rules define intelligent filtering to prevent alert flooding.
        /// </summary>
        /// <param name="suppressionConfig">The suppression rule configuration to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppressionRule(SuppressionConfig suppressionConfig);

        /// <summary>
        /// Adds multiple suppression rules to the alert system.
        /// Convenience method for adding multiple rules in a single call.
        /// </summary>
        /// <param name="suppressionConfigs">The suppression rule configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppressionRules(params SuppressionConfig[] suppressionConfigs);

        /// <summary>
        /// Adds multiple suppression rules from a collection.
        /// Supports any enumerable collection of suppression rule configurations.
        /// </summary>
        /// <param name="suppressionConfigs">The collection of suppression rule configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppressionRules(IEnumerable<SuppressionConfig> suppressionConfigs);

        /// <summary>
        /// Configures suppression rules using a fluent builder pattern.
        /// Provides access to a specialized builder for complex suppression rule configuration scenarios.
        /// </summary>
        /// <param name="suppressionBuilder">Action to configure suppression rules using the specialized builder</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSuppressionRules(Action<ISuppressionConfigBuilder> suppressionBuilder);

        /// <summary>
        /// Configures alert filters using a fluent builder pattern.
        /// Provides access to a specialized builder for comprehensive filter configuration scenarios.
        /// </summary>
        /// <param name="filterBuilder">Action to configure filters using the specialized builder</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithFilters(Action<IFilterConfigBuilder> filterBuilder);

        /// <summary>
        /// Adds a severity filter with specific configuration.
        /// Convenience method for filtering alerts by severity level.
        /// </summary>
        /// <param name="name">The filter name (optional, defaults to "SeverityFilter")</param>
        /// <param name="minimumSeverity">The minimum severity level to allow</param>
        /// <param name="allowCriticalAlways">Whether to always allow critical alerts (optional, defaults to true)</param>
        /// <param name="priority">Filter priority (optional, defaults to 10)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSeverityFilter(string name = "SeverityFilter", AlertSeverity minimumSeverity = AlertSeverity.Info, bool allowCriticalAlways = true, int priority = 10);

        /// <summary>
        /// Adds a source filter with specific configuration.
        /// Convenience method for filtering alerts by source patterns.
        /// </summary>
        /// <param name="name">The filter name (optional, defaults to "SourceFilter")</param>
        /// <param name="sources">Source patterns to match</param>
        /// <param name="useWhitelist">Whether to use whitelist (true) or blacklist (false) mode (optional, defaults to true)</param>
        /// <param name="priority">Filter priority (optional, defaults to 20)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSourceFilter(string name = "SourceFilter", IEnumerable<string> sources = null, bool useWhitelist = true, int priority = 20);

        /// <summary>
        /// Adds a rate limiting filter with specific configuration.
        /// Convenience method for limiting alert rates per source pattern.
        /// </summary>
        /// <param name="name">The filter name (optional, defaults to "RateLimitFilter")</param>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute (optional, defaults to 60)</param>
        /// <param name="sourcePattern">Source pattern to match (optional, defaults to "*")</param>
        /// <param name="priority">Filter priority (optional, defaults to 30)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithRateLimitFilter(string name = "RateLimitFilter", int maxAlertsPerMinute = 60, string sourcePattern = "*", int priority = 30);

        /// <summary>
        /// Adds a default duplicate filter rule to prevent duplicate alerts.
        /// Convenience method for common duplicate suppression scenarios.
        /// </summary>
        /// <param name="name">The rule name (optional, defaults to "DuplicateFilter")</param>
        /// <param name="window">The suppression window (optional, defaults to 5 minutes)</param>
        /// <param name="enabled">Whether the rule is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithDuplicateFilter(string name = "DuplicateFilter", TimeSpan? window = null, bool enabled = true);

        /// <summary>
        /// Adds a default rate limit rule to prevent alert flooding.
        /// Convenience method for common rate limiting scenarios.
        /// </summary>
        /// <param name="name">The rule name (optional, defaults to "RateLimit")</param>
        /// <param name="maxAlerts">The maximum alerts per window (optional, defaults to 10)</param>
        /// <param name="window">The rate limit window (optional, defaults to 1 minute)</param>
        /// <param name="enabled">Whether the rule is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithRateLimit(string name = "RateLimit", int maxAlerts = 10, TimeSpan? window = null, bool enabled = true);

        /// <summary>
        /// Adds a business hours filter rule for time-based suppression.
        /// Convenience method for applying different severity thresholds during business vs. after hours.
        /// </summary>
        /// <param name="name">The rule name (optional, defaults to "BusinessHours")</param>
        /// <param name="timeZone">The time zone for business hours calculation (optional, defaults to local)</param>
        /// <param name="enabled">Whether the rule is enabled (optional, defaults to true)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithBusinessHoursFilter(string name = "BusinessHours", TimeZoneInfo timeZone = null, bool enabled = true);

        /// <summary>
        /// Sets a source-specific minimum severity override.
        /// Allows different alert sources to have different minimum severity levels.
        /// </summary>
        /// <param name="source">The alert source identifier</param>
        /// <param name="severity">The minimum severity level for this source</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSourceSeverityOverride(FixedString64Bytes source, AlertSeverity severity);

        /// <summary>
        /// Sets multiple source-specific minimum severity overrides.
        /// Convenience method for configuring multiple source overrides.
        /// </summary>
        /// <param name="overrides">Dictionary mapping sources to their minimum severity levels</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithSourceSeverityOverrides(IDictionary<FixedString64Bytes, AlertSeverity> overrides);

        /// <summary>
        /// Configures emergency escalation behavior for critical system failures.
        /// Defines fallback mechanisms when normal alert processing is compromised.
        /// </summary>
        /// <param name="escalationConfig">The emergency escalation configuration</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithEmergencyEscalation(EmergencyEscalationConfig escalationConfig);

        /// <summary>
        /// Configures emergency escalation with common parameters.
        /// Convenience method for typical emergency escalation scenarios.
        /// </summary>
        /// <param name="enabled">Whether emergency escalation is enabled</param>
        /// <param name="failureThreshold">The failure threshold for triggering escalation (0.0 to 1.0)</param>
        /// <param name="fallbackChannel">The fallback channel to use during escalation</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder WithEmergencyEscalation(bool enabled, double failureThreshold = 0.8, string fallbackChannel = "Console");

        /// <summary>
        /// Configures the builder for production use with enterprise-grade alerting.
        /// Sets appropriate channels, suppression rules, and performance settings for production environments.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForProduction();

        /// <summary>
        /// Configures the builder for development use with comprehensive alerting.
        /// Sets Unity console, standard console, and detailed logging for development environments.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForDevelopment();

        /// <summary>
        /// Configures the builder for testing scenarios with comprehensive capture.
        /// Sets up alerting for automated testing with appropriate channels and settings.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForTesting();

        /// <summary>
        /// Configures the builder for staging environments with production-like settings.
        /// Sets up alerting similar to production but with additional development-friendly features.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForStaging();

        /// <summary>
        /// Configures the builder for performance testing scenarios.
        /// Minimizes alerting overhead while maintaining essential monitoring capabilities.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForPerformanceTesting();

        /// <summary>
        /// Configures the builder for high-availability production environments.
        /// Sets comprehensive alerting with redundancy, escalation, and enterprise integrations.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForHighAvailability();

        /// <summary>
        /// Configures the builder for cloud deployment scenarios.
        /// Sets up alerting optimized for cloud environments with network channels and distributed logging.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForCloudDeployment();

        /// <summary>
        /// Configures the builder for mobile/embedded scenarios with minimal overhead.
        /// Sets up lightweight alerting suitable for resource-constrained environments.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForMobile();

        /// <summary>
        /// Configures the builder for debugging specific issues with targeted alerting.
        /// Sets up comprehensive debugging with detailed channels and enhanced monitoring.
        /// </summary>
        /// <param name="debugSource">The specific source to debug (optional)</param>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder ForDebugging(string debugSource = null);

        /// <summary>
        /// Validates the current configuration and returns any validation errors.
        /// Performs comprehensive validation of all configured channels, rules, and settings.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> Validate();

        /// <summary>
        /// Gets the current list of configured filters.
        /// Provides read-only access to the filter configurations for inspection.
        /// </summary>
        /// <returns>Read-only list of filter configurations</returns>
        IReadOnlyList<FilterConfiguration> GetFilters();

        /// <summary>
        /// Builds the final AlertConfig from the current builder state.
        /// Performs validation and constructs an immutable configuration object.
        /// </summary>
        /// <returns>The configured AlertConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        AlertConfig Build();

        /// <summary>
        /// Resets the builder to its initial state.
        /// Clears all configuration and returns to default settings.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IAlertConfigBuilder Reset();

        /// <summary>
        /// Creates a copy of the current builder state.
        /// Useful for creating variations of a base configuration.
        /// </summary>
        /// <returns>A new builder instance with the same configuration</returns>
        IAlertConfigBuilder Clone();
    }
}