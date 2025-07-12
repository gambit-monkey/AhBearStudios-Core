using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Interface for building logging configuration in a fluent manner.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// Provides comprehensive configuration options for all available log targets.
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
        /// Adds a Serilog target with enterprise-grade logging features.
        /// </summary>
        /// <param name="name">The name of the target (default: "Serilog")</param>
        /// <param name="minimumLevel">The minimum log level (default: Info)</param>
        /// <param name="loggerConfiguration">Optional Serilog logger configuration</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithSerilogTarget(string name = "Serilog", LogLevel minimumLevel = LogLevel.Info, object loggerConfiguration = null);

        /// <summary>
        /// Adds a null target for testing or disabled scenarios.
        /// </summary>
        /// <param name="name">The name of the target (default: "Null")</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithNullTarget(string name = "Null");

        /// <summary>
        /// Adds a standard console target (not Unity-specific).
        /// </summary>
        /// <param name="name">The name of the target (default: "StdConsole")</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <param name="useColors">Whether to use colored output (default: true)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithStandardConsoleTarget(string name = "StdConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true);

        /// <summary>
        /// Adds a Unity console target with Unity-specific features.
        /// </summary>
        /// <param name="name">The name of the target (default: "UnityConsole")</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <param name="useColors">Whether to use colored output (default: true)</param>
        /// <param name="showStackTraces">Whether to show stack traces (default: true)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithUnityConsoleTarget(string name = "UnityConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true, bool showStackTraces = true);

        /// <summary>
        /// Adds a network target for remote logging.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="endpoint">The network endpoint (URL or IP:Port)</param>
        /// <param name="minimumLevel">The minimum log level (default: Info)</param>
        /// <param name="timeoutSeconds">Network timeout in seconds (default: 30)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithNetworkTarget(string name, string endpoint, LogLevel minimumLevel = LogLevel.Info, int timeoutSeconds = 30);

        /// <summary>
        /// Adds a database target for structured log storage.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="connectionString">The database connection string</param>
        /// <param name="tableName">The table name for log storage (default: "Logs")</param>
        /// <param name="minimumLevel">The minimum log level (default: Info)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithDatabaseTarget(string name, string connectionString, string tableName = "Logs", LogLevel minimumLevel = LogLevel.Info);

        /// <summary>
        /// Adds an email target for critical alerts.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="smtpServer">The SMTP server address</param>
        /// <param name="fromEmail">The sender email address</param>
        /// <param name="toEmails">The recipient email addresses</param>
        /// <param name="minimumLevel">The minimum log level (default: Error)</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder WithEmailTarget(string name, string smtpServer, string fromEmail, string[] toEmails, LogLevel minimumLevel = LogLevel.Error);

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
        /// Configures the builder for production use with enterprise-grade logging.
        /// Sets Serilog, file logging, memory buffer, and email alerts.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForProduction();

        /// <summary>
        /// Configures the builder for development use with comprehensive debugging.
        /// Sets Unity console, standard console, memory buffer, and file logging.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForDevelopment();

        /// <summary>
        /// Configures the builder for testing scenarios with comprehensive capture.
        /// Sets large memory buffer and null target for testing.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForTesting();

        /// <summary>
        /// Configures the builder for staging environments with production-like settings.
        /// Sets Serilog, file logging, memory buffer, and Unity console for warnings.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForStaging();

        /// <summary>
        /// Configures the builder for performance testing scenarios.
        /// Minimizes logging overhead with memory-only targets.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForPerformanceTesting();

        /// <summary>
        /// Configures the builder for high-availability production environments.
        /// Sets comprehensive logging with Serilog, database, network, and email alerts.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForHighAvailability();

        /// <summary>
        /// Configures the builder for cloud deployment scenarios.
        /// Sets network logging, memory buffer, local backup, and cloud alerts.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForCloudDeployment();

        /// <summary>
        /// Configures the builder for mobile/embedded scenarios with minimal overhead.
        /// Sets minimal logging with small memory buffer and error-only file logging.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForMobile();

        /// <summary>
        /// Configures the builder for debugging specific issues with targeted logging.
        /// Sets comprehensive debugging with Unity console, file trace, and debug channel.
        /// </summary>
        /// <param name="debugChannel">The specific channel to debug</param>
        /// <returns>The builder instance for method chaining</returns>
        ILogConfigBuilder ForDebugging(string debugChannel = "Debug");

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