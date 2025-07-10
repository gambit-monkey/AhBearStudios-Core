using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Interface for building logging configuration in a fluent manner.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// </summary>
    public interface ILogConfigBuilder
    {
        /// <summary>
        /// Sets the global minimum log level for all targets.
        /// </summary>
        /// <param name="logLevel">The minimum log level to set</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithGlobalMinimumLevel(LogLevel logLevel);

        /// <summary>
        /// Enables or disables logging globally.
        /// </summary>
        /// <param name="enabled">True to enable logging, false to disable</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithLoggingEnabled(bool enabled);

        /// <summary>
        /// Sets the maximum number of log messages to queue when batching is enabled.
        /// </summary>
        /// <param name="maxQueueSize">The maximum queue size</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithMaxQueueSize(int maxQueueSize);

        /// <summary>
        /// Sets the interval at which batched log messages are flushed.
        /// </summary>
        /// <param name="flushInterval">The flush interval</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithFlushInterval(TimeSpan flushInterval);

        /// <summary>
        /// Enables or disables high-performance mode with zero-allocation logging.
        /// </summary>
        /// <param name="enabled">True to enable high-performance mode</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithHighPerformanceMode(bool enabled);

        /// <summary>
        /// Enables or disables Burst compilation compatibility for native job system integration.
        /// </summary>
        /// <param name="enabled">True to enable Burst compatibility</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithBurstCompatibility(bool enabled);

        /// <summary>
        /// Enables or disables structured logging support.
        /// </summary>
        /// <param name="enabled">True to enable structured logging</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithStructuredLogging(bool enabled);

        /// <summary>
        /// Adds a log target configuration to the builder.
        /// </summary>
        /// <param name="targetConfig">The log target configuration to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithTarget(LogTargetConfig targetConfig);

        /// <summary>
        /// Adds multiple log target configurations to the builder.
        /// </summary>
        /// <param name="targetConfigs">The log target configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithTargets(params LogTargetConfig[] targetConfigs);

        /// <summary>
        /// Adds multiple log target configurations to the builder.
        /// </summary>
        /// <param name="targetConfigs">The log target configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithTargets(IEnumerable<LogTargetConfig> targetConfigs);

        /// <summary>
        /// Adds a log channel configuration to the builder.
        /// </summary>
        /// <param name="channelConfig">The log channel configuration to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithChannel(LogChannelConfig channelConfig);

        /// <summary>
        /// Adds multiple log channel configurations to the builder.
        /// </summary>
        /// <param name="channelConfigs">The log channel configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithChannels(params LogChannelConfig[] channelConfigs);

        /// <summary>
        /// Adds multiple log channel configurations to the builder.
        /// </summary>
        /// <param name="channelConfigs">The log channel configurations to add</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithChannels(IEnumerable<LogChannelConfig> channelConfigs);

        /// <summary>
        /// Sets the correlation ID format for tracking operations across system boundaries.
        /// </summary>
        /// <param name="format">The correlation ID format string</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithCorrelationIdFormat(string format);

        /// <summary>
        /// Enables or disables automatic correlation ID generation.
        /// </summary>
        /// <param name="enabled">True to enable automatic correlation ID generation</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithAutoCorrelationId(bool enabled);

        /// <summary>
        /// Sets the log message format template.
        /// </summary>
        /// <param name="template">The log message format template</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithMessageFormat(string template);

        /// <summary>
        /// Enables or disables including timestamps in log messages.
        /// </summary>
        /// <param name="enabled">True to include timestamps</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithTimestamps(bool enabled);

        /// <summary>
        /// Sets the timestamp format for log messages.
        /// </summary>
        /// <param name="format">The timestamp format string</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithTimestampFormat(string format);

        /// <summary>
        /// Validates the current configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> Validate();

        /// <summary>
        /// Builds the final LoggingConfig from the current builder state.
        /// </summary>
        /// <returns>The configured LoggingConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        LoggingConfig Build();

        /// <summary>
        /// Resets the builder to its initial state.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder Reset();
    }
}