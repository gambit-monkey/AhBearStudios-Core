using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Interface for building logging configuration in a fluent manner.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// Provides comprehensive configuration options for the logging system.
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
        /// Enables or disables batching for high-performance logging scenarios.
        /// </summary>
        /// <param name="enabled">True to enable batching</param>
        /// <param name="batchSize">The size of each batch (default: 100)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithBatching(bool enabled, int batchSize = 100);

        /// <summary>
        /// Enables or disables message caching for performance optimization.
        /// </summary>
        /// <param name="enabled">True to enable caching</param>
        /// <param name="maxCacheSize">The maximum cache size (default: 1000)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithCaching(bool enabled, int maxCacheSize = 1000);

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
        /// Adds a console target with the specified configuration.
        /// </summary>
        /// <param name="name">The name of the target (default: "Console")</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithConsoleTarget(string name = "Console", LogLevel minimumLevel = LogLevel.Debug);

        /// <summary>
        /// Adds a file target with the specified configuration.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="filePath">The path to the log file</param>
        /// <param name="minimumLevel">The minimum log level (default: Info)</param>
        /// <param name="bufferSize">The buffer size for file writing (default: 100)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithFileTarget(string name, string filePath, LogLevel minimumLevel = LogLevel.Info, int bufferSize = 100);

        /// <summary>
        /// Adds a memory target for in-memory log storage.
        /// </summary>
        /// <param name="name">The name of the target (default: "Memory")</param>
        /// <param name="maxEntries">The maximum number of entries to store (default: 1000)</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithMemoryTarget(string name = "Memory", int maxEntries = 1000, LogLevel minimumLevel = LogLevel.Debug);

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
        /// Adds a channel with the specified configuration.
        /// </summary>
        /// <param name="name">The channel name</param>
        /// <param name="minimumLevel">The minimum log level for the channel</param>
        /// <param name="enabled">Whether the channel is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithChannel(string name, LogLevel minimumLevel = LogLevel.Debug, bool enabled = true);

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
        /// Configures the builder for production use with optimized settings.
        /// Sets high-performance mode, burst compatibility, batching, and info-level logging.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForProduction();

        /// <summary>
        /// Configures the builder for development use with debugging-friendly settings.
        /// Sets debug-level logging, console and memory targets, and disables batching.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForDevelopment();

        /// <summary>
        /// Configures the builder for testing scenarios.
        /// Sets debug-level logging with memory target optimized for testing.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForTesting();

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