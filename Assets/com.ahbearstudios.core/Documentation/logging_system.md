/com# Logging System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.Logging`  
**Role:** Centralized logging with multiple targets and correlation tracking  
**Status:** ✅ Core Infrastructure

The Logging System provides comprehensive centralized logging capabilities with multiple output targets, structured logging, correlation ID tracking, and performance optimization. As a foundational system, it serves all other AhBearStudios Core systems with comprehensive observability and debugging support.

## 🚀 Key Features

- **📝 Centralized Logging**: Single interface for all logging operations across systems
- **🎯 Multiple Targets**: Simultaneous output to console, file, database, and external services
- **🔗 Correlation Tracking**: Full correlation ID support for distributed tracing
- **📊 Structured Logging**: Rich contextual data with key-value properties
- **⚡ High Performance**: Optimized for minimal overhead with async operations
- **🏥 Health Integration**: Built-in health monitoring and alerting capabilities
- **📈 Performance Metrics**: Integration with IProfilerService for logging performance tracking
- **🚨 Alert Integration**: Automatic alerting for critical errors and system issues
- **🔄 Foundation Integration**: Serves as the foundation for all other core systems
- **🎛️ Configurable Filtering**: Advanced filtering by level, source, and custom criteria

## 🏗️ Architecture

### Folder Structure

```
AhBearStudios.Core.Logging/
├── ILoggingService.cs                    # Primary service interface
├── LoggingService.cs                     # Service implementation
├── Configs/
│   ├── LoggingConfig.cs                  # Logging configuration record
│   ├── ILogTargetConfig.cs               # Target configuration interface
│   ├── LogTargetConfig.cs                # Target-specific settings record
│   ├── LogChannelConfig.cs               # Channel configuration record
│   ├── FilterConfig.cs                   # Filtering configuration record
│   └── FormatterConfig.cs                # Output formatter configuration
├── Builders/
│   ├── ILogConfigBuilder.cs              # Main configuration builder interface
│   ├── LogConfigBuilder.cs               # Main builder implementation
│   ├── ILogTargetConfigBuilder.cs        # Target configuration builder interface
│   ├── LogTargetConfigBuilder.cs         # Target builder implementation
│   ├── FilterConfigBuilder.cs            # Filter builder implementation
│   └── FormatterConfigBuilder.cs         # Formatter builder implementation
├── Factories/
│   ├── ILoggingServiceFactory.cs         # Service factory interface
│   ├── LoggingServiceFactory.cs          # Service factory implementation
│   ├── ILogTargetFactory.cs              # Target creation interface
│   ├── LogTargetFactory.cs               # Target factory implementation
│   ├── ILogFormatterFactory.cs           # Formatter creation interface
│   ├── LogFormatterFactory.cs            # Formatter factory implementation
│   ├── ILogFilterFactory.cs              # Filter creation interface
│   └── LogFilterFactory.cs               # Filter factory implementation
├── Services/
│   ├── LogContextService.cs              # Context management service
│   ├── LogFilterService.cs               # Log filtering service
│   ├── LogBufferService.cs               # Buffering and batching service
│   ├── LogCorrelationService.cs          # Correlation ID management
│   ├── ILogCorrelationService.cs         # Correlation service interface
│   ├── ILogBatchingService.cs            # Batching service interface
│   ├── LogBatchingService.cs             # Batching service implementation
│   ├── ILogFormattingService.cs          # Formatting service interface
│   └── LogFormattingService.cs           # Formatting service implementation
├── Targets/
│   ├── ILogTarget.cs                     # Log target interface
│   ├── ConsoleLogTarget.cs               # Console output target
│   ├── FileLogTarget.cs                  # File output target
│   ├── MemoryLogTarget.cs                # In-memory log storage target
│   ├── SerilogTarget.cs                  # Serilog integration target
│   └── NullLogTarget.cs                  # Null target for testing
├── Formatters/
│   ├── ILogFormatter.cs                  # Log formatter interface
│   ├── JsonFormatter.cs                  # JSON output formatter
│   ├── PlainTextFormatter.cs             # Plain text formatter
│   ├── StructuredFormatter.cs            # Structured data formatter
│   ├── BinaryFormatter.cs                # Binary format for efficiency
│   ├── CefFormatter.cs                   # Common Event Format
│   ├── CsvFormatter.cs                   # CSV format for data analysis
│   ├── GelfFormatter.cs                  # Graylog Extended Log Format
│   ├── KeyValueFormatter.cs              # Key-value pair format
│   ├── MessagePackFormatter.cs           # MessagePack binary format
│   ├── ProtobufFormatter.cs              # Protocol Buffers format
│   ├── SyslogFormatter.cs                # Syslog RFC format
│   └── XmlFormatter.cs                   # XML format for enterprise
├── Models/
│   ├── LogEntry.cs                       # Log entry record
│   ├── LogContext.cs                     # Logging context record
│   ├── LogLevel.cs                       # Log level enumeration
│   ├── LogMessage.cs                     # Log message model
│   ├── LogScope.cs                       # Scope implementation
│   ├── ILogScope.cs                      # Scope interface
│   ├── LoggingStatistics.cs              # Logging statistics record
│   ├── TargetStatistics.cs               # Target-specific statistics
│   ├── CorrelationInfo.cs                # Correlation tracking record
│   ├── ILogChannel.cs                    # Channel interface
│   ├── LogChannel.cs                     # Channel implementation
│   ├── LogFormat.cs                      # Format enumeration
│   ├── LogTemplate.cs                    # Message templates
│   ├── LogTargetDefaults.cs              # Default target configurations
│   ├── LoggingScenario.cs                # Scenario configurations
│   ├── PerformanceProfile.cs             # Performance profiling data
│   └── ValidationResult.cs               # Configuration validation results
├── Filters/
│   ├── ILogFilter.cs                     # Log filter interface
│   ├── LevelFilter.cs                    # Level-based filtering
│   ├── SourceFilter.cs                   # Source-based filtering
│   ├── CorrelationFilter.cs              # Correlation-based filtering
│   ├── PatternFilter.cs                  # Pattern/regex filtering
│   ├── RateLimitFilter.cs                # Rate limiting filter
│   ├── SamplingFilter.cs                 # Statistical sampling filter
│   ├── TimeRangeFilter.cs                # Time-based filtering
│   └── FilterStatistics.cs               # Filter performance stats
├── Messages/
│   ├── LogConfigurationChangedMessage.cs  # Configuration change events
│   ├── LogScopeCompletedMessage.cs        # Scope completion events
│   ├── LogTargetErrorMessage.cs           # Target error notifications
│   └── LoggingSystemHealthMessage.cs      # Health status messages
└── HealthChecks/
    └── LoggingServiceHealthCheck.cs      # Health monitoring

AhBearStudios.Unity.Logging/
├── Installers/
│   └── LoggingInstaller.cs               # Reflex bootstrap installer
├── Targets/
│   └── UnityConsoleLogTarget.cs          # Unity console output
├── Configs/
│   └── LoggingConfig.cs                  # Unity-specific configuration
└── UnityLoggingBehaviour.cs              # Unity MonoBehaviour integration
```
## 🔌 Key Interfaces

### ILoggingService

The primary interface for all logging operations with comprehensive correlation tracking.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;

/// <summary>
/// Primary logging service interface providing centralized logging
/// with correlation tracking and comprehensive system integration.
/// Follows the AhBearStudios Core Architecture foundation system pattern.
/// Designed for Unity game development with Job System and Burst compatibility.
/// </summary>
public interface ILoggingService : IDisposable
{
    // Configuration and runtime state properties
    /// <summary>
    /// Gets the current configuration of the logging service.
    /// </summary>
    LoggingConfig Configuration { get; }

    /// <summary>
    /// Gets whether the logging service is enabled.
    /// </summary>
    bool IsEnabled { get; }

    // Core logging methods with Unity.Collections v2 correlation tracking
    /// <summary>
    /// Logs a debug message with correlation tracking.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogDebug(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an informational message with correlation tracking.
    /// </summary>
    void LogInfo(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a warning message with correlation tracking.
    /// </summary>
    void LogWarning(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an error message with correlation tracking.
    /// </summary>
    void LogError(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a critical message with correlation tracking and automatic alerting.
    /// </summary>
    void LogCritical(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    // Unity Job System and Burst-compatible logging methods
    /// <summary>
    /// Logs a debug message with structured data using generic type constraints for Burst compatibility.
    /// Designed for use within Unity Job System contexts.
    /// </summary>
    /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
    /// <param name="message">The message to log</param>
    /// <param name="data">The structured data to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    void LogDebug<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

    /// <summary>
    /// Logs an informational message with structured data for Burst compatibility.
    /// </summary>
    void LogInfo<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

    /// <summary>
    /// Logs a warning message with structured data for Burst compatibility.
    /// </summary>
    void LogWarning<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

    /// <summary>
    /// Logs an error message with structured data for Burst compatibility.
    /// </summary>
    void LogError<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

    /// <summary>
    /// Logs a critical message with structured data for Burst compatibility.
    /// </summary>
    void LogCritical<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

    /// <summary>
    /// Logs an exception with context and correlation tracking.
    /// </summary>
    void LogException(string message, Exception exception, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a message with the specified level and full context.
    /// </summary>
    void Log(LogLevel level, string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, Exception exception = null, 
        IReadOnlyDictionary<string, object> properties = null, string channel = null);

    // Hierarchical logging scopes with correlation tracking
    /// <summary>
    /// Begins a logging scope for hierarchical context tracking.
    /// </summary>
    /// <param name="scopeName">Name of the scope</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context</param>
    /// <returns>Disposable logging scope</returns>
    ILogScope BeginScope(string scopeName, FixedString64Bytes correlationId = default, 
        string sourceContext = null);

    // Target management with correlation tracking
    /// <summary>
    /// Registers a log target with the service.
    /// </summary>
    void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Unregisters a log target from the service.
    /// </summary>
    bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets all registered log targets.
    /// </summary>
    IReadOnlyCollection<ILogTarget> GetTargets();

    /// <summary>
    /// Sets the minimum log level for filtering.
    /// </summary>
    void SetMinimumLevel(LogLevel level, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Adds a log filter for advanced filtering.
    /// </summary>
    void AddFilter(ILogFilter filter, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Removes a log filter.
    /// </summary>
    bool RemoveFilter(string filterName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets current logging statistics for monitoring.
    /// </summary>
    LoggingStatistics GetStatistics();

    /// <summary>
    /// Flushes all buffered log entries to targets.
    /// </summary>
    Task FlushAsync(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates logging configuration and targets.
    /// </summary>
    ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Clears internal caches and performs maintenance.
    /// </summary>
    void PerformMaintenance(FixedString64Bytes correlationId = default);

    // Channel management methods
    /// <summary>
    /// Registers a log channel with the service.
    /// </summary>
    void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Unregisters a log channel from the service.
    /// </summary>
    bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets all registered log channels.
    /// </summary>
    IReadOnlyCollection<ILogChannel> GetChannels();

    /// <summary>
    /// Gets a registered log channel by name.
    /// </summary>
    ILogChannel GetChannel(string channelName);

    /// <summary>
    /// Determines whether a log channel is registered.
    /// </summary>
    bool HasChannel(string channelName);
}
```

### ILogTarget

Interface for log output targets with streamlined synchronous operations.

```csharp
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

/// <summary>
/// Interface for log targets that define where log messages are written.
/// Supports multiple output destinations including console, file, network, and custom targets.
/// </summary>
public interface ILogTarget : IDisposable
{
    /// <summary>
    /// Gets the unique name of this log target.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the minimum log level that this target will process.
    /// Messages below this level will be ignored.
    /// </summary>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Gets or sets whether this target is enabled and should process log messages.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets whether this target is currently healthy and operational.
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Gets the list of channels this target listens to.
    /// Empty list means it listens to all channels.
    /// </summary>
    IReadOnlyList<string> Channels { get; }

    /// <summary>
    /// Writes a log message to this target.
    /// </summary>
    /// <param name="logMessage">The log message to write</param>
    void Write(in LogMessage logMessage);

    /// <summary>
    /// Writes multiple log messages to this target in a batch operation.
    /// </summary>
    /// <param name="logMessages">The log messages to write</param>
    void WriteBatch(IReadOnlyList<LogMessage> logMessages);

    /// <summary>
    /// Determines whether this target should process the given log message.
    /// </summary>
    /// <param name="logMessage">The log message to evaluate</param>
    /// <returns>True if the message should be processed, false otherwise</returns>
    bool ShouldProcessMessage(in LogMessage logMessage);

    /// <summary>
    /// Flushes any buffered log messages to the target destination.
    /// </summary>
    void Flush();

    /// <summary>
    /// Performs a health check on this target.
    /// </summary>
    /// <returns>True if the target is healthy, false otherwise</returns>
    bool PerformHealthCheck();
}
```
### ILogScope

Interface for hierarchical logging scopes with integrated logging methods.

```csharp
using System;
using System.Collections.Generic;
using Unity.Collections;

/// <summary>
/// Interface for log scopes that provide contextual logging boundaries.
/// Scopes automatically add context to all log messages within their lifetime.
/// </summary>
public interface ILogScope : IDisposable
{
    /// <summary>
    /// Scope name for identification.
    /// </summary>
    FixedString64Bytes Name { get; }

    /// <summary>
    /// Correlation ID for this scope.
    /// </summary>
    FixedString64Bytes CorrelationId { get; }

    /// <summary>
    /// Source context for this scope.
    /// </summary>
    string SourceContext { get; }

    /// <summary>
    /// Elapsed time since scope creation.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Whether the scope is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Parent scope (if any).
    /// </summary>
    ILogScope Parent { get; }

    /// <summary>
    /// Child scopes created within this scope.
    /// </summary>
    IReadOnlyCollection<ILogScope> Children { get; }

    /// <summary>
    /// Creates a child scope within this scope.
    /// </summary>
    /// <param name="childName">Name of the child scope</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Child logging scope</returns>
    ILogScope BeginChild(string childName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Adds a property to this scope's context.
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    void SetProperty(string key, object value);

    /// <summary>
    /// Gets a property from this scope's context.
    /// </summary>
    /// <param name="key">Property key</param>
    /// <returns>Property value or null if not found</returns>
    object GetProperty(string key);

    /// <summary>
    /// Gets all properties in this scope's context.
    /// </summary>
    /// <returns>Read-only dictionary of properties</returns>
    IReadOnlyDictionary<string, object> GetAllProperties();

    // Direct logging methods within the scope context
    /// <summary>
    /// Logs a debug message within this scope.
    /// </summary>
    /// <param name="message">The message to log</param>
    void LogDebug(string message);

    /// <summary>
    /// Logs an informational message within this scope.
    /// </summary>
    /// <param name="message">The message to log</param>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning message within this scope.
    /// </summary>
    /// <param name="message">The message to log</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message within this scope.
    /// </summary>
    /// <param name="message">The message to log</param>
    void LogError(string message);

    /// <summary>
    /// Logs a critical message within this scope.
    /// </summary>
    /// <param name="message">The message to log</param>
    void LogCritical(string message);

    /// <summary>
    /// Logs an exception within this scope.
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="message">Additional context message</param>
    void LogException(Exception exception, string message = null);
}
```

### ILogFormatter

Interface for log message formatting.

```csharp
/// <summary>
/// Interface for log message formatting with correlation support.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Formatter name for identification.
    /// </summary>
    FixedString64Bytes Name { get; }

    /// <summary>
    /// Supported output format.
    /// </summary>
    LogFormat Format { get; }

    /// <summary>
    /// Formats a log entry for output.
    /// </summary>
    /// <param name="entry">Log entry to format</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Formatted log message</returns>
    string Format(LogEntry entry, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Formats multiple log entries for batch output.
    /// </summary>
    /// <param name="entries">Log entries to format</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Formatted log messages</returns>
    IEnumerable<string> FormatBatch(IReadOnlyCollection<LogEntry> entries, 
        FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates the formatter configuration.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(FixedString64Bytes correlationId = default);
}
```

### ILogFilter

Interface for log filtering with correlation support.

```csharp
/// <summary>
/// Interface for log filtering with correlation tracking.
/// </summary>
public interface ILogFilter
{
    /// <summary>
    /// Filter name for identification.
    /// </summary>
    FixedString64Bytes Name { get; }

    /// <summary>
    /// Whether the filter is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Priority for filter execution order.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if a log entry should be processed.
    /// </summary>
    /// <param name="entry">Log entry to filter</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>True if the entry should be processed</returns>
    bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates the filter configuration.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets filter statistics for monitoring.
    /// </summary>
    /// <returns>Filter performance statistics</returns>
    FilterStatistics GetStatistics();
}
```
## ⚙️ Configuration

### LoggingConfig Record

Modern C# configuration using records with performance-optimized settings for Unity game development.

```csharp
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

/// <summary>
/// Configuration record for the logging system.
/// Contains all settings required to configure the high-performance logging infrastructure.
/// </summary>
public sealed record LoggingConfig
{
    /// <summary>
    /// Gets the default logging configuration for production use.
    /// </summary>
    public static LoggingConfig Default => new LoggingConfig
    {
        GlobalMinimumLevel = LogLevel.Info,
        IsLoggingEnabled = true,
        MaxQueueSize = 1000,
        FlushInterval = TimeSpan.FromMilliseconds(100),
        HighPerformanceMode = true,
        BurstCompatibility = true,
        StructuredLogging = true,
        BatchingEnabled = true,
        BatchSize = 100,
        CorrelationIdFormat = "{0:N}",
        AutoCorrelationId = true,
        MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}",
        IncludeTimestamps = true,
        TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
        CachingEnabled = true,
        MaxCacheSize = 1000,
        TargetConfigs = new List<LogTargetConfig>().AsReadOnly(),
        ChannelConfigs = new List<LogChannelConfig> 
        { 
            new LogChannelConfig 
            { 
                Name = "Default", 
                MinimumLevel = LogLevel.Debug, 
                IsEnabled = true 
            } 
        }.AsReadOnly()
    };

    /// <summary>
    /// Gets or sets the global minimum log level. Messages below this level will be filtered out.
    /// </summary>
    public LogLevel GlobalMinimumLevel { get; init; } = LogLevel.Info;

    /// <summary>
    /// Gets or sets whether logging is enabled globally.
    /// </summary>
    public bool IsLoggingEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum number of log messages to queue when batching is enabled.
    /// </summary>
    public int MaxQueueSize { get; init; } = 1000;

    /// <summary>
    /// Gets or sets the interval at which batched log messages are flushed.
    /// </summary>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets whether high-performance mode is enabled for zero-allocation logging.
    /// When enabled, uses Unity.Collections v2 and object pooling for optimal performance.
    /// </summary>
    public bool HighPerformanceMode { get; init; } = true;

    /// <summary>
    /// Gets or sets whether Burst compilation compatibility is enabled for native job system integration.
    /// When enabled, uses native-compatible data structures and algorithms.
    /// </summary>
    public bool BurstCompatibility { get; init; } = true;

    /// <summary>
    /// Gets or sets whether structured logging is enabled for rich contextual data.
    /// </summary>
    public bool StructuredLogging { get; init; } = true;

    /// <summary>
    /// Gets or sets whether batching is enabled for high-throughput scenarios.
    /// </summary>
    public bool BatchingEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of messages to batch before flushing.
    /// </summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Gets or sets the format string for correlation IDs used to track operations across system boundaries.
    /// </summary>
    public string CorrelationIdFormat { get; init; } = "{0:N}";

    /// <summary>
    /// Gets or sets whether automatic correlation ID generation is enabled.
    /// </summary>
    public bool AutoCorrelationId { get; init; } = true;

    /// <summary>
    /// Gets or sets the message format template for log output.
    /// </summary>
    public string MessageFormat { get; init; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";

    /// <summary>
    /// Gets or sets whether timestamps are included in log messages.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = true;

    /// <summary>
    /// Gets or sets the format string for timestamps in log messages.
    /// </summary>
    public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Gets or sets whether message formatting caching is enabled for performance.
    /// </summary>
    public bool CachingEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum cache size for formatted messages.
    /// </summary>
    public int MaxCacheSize { get; init; } = 1000;

    /// <summary>
    /// Gets or sets the collection of log target configurations.
    /// </summary>
    public IReadOnlyList<LogTargetConfig> TargetConfigs { get; init; } = new List<LogTargetConfig>().AsReadOnly();

    /// <summary>
    /// Gets or sets the collection of log channel configurations.
    /// </summary>
    public IReadOnlyList<LogChannelConfig> ChannelConfigs { get; init; } = new List<LogChannelConfig>().AsReadOnly();

    /// <summary>
    /// Validates the configuration and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (MaxQueueSize <= 0)
            errors.Add("Max queue size must be greater than zero.");

        if (FlushInterval <= TimeSpan.Zero)
            errors.Add("Flush interval must be greater than zero.");

        if (BatchingEnabled && BatchSize <= 0)
            errors.Add("Batch size must be greater than zero when batching is enabled.");

        if (string.IsNullOrWhiteSpace(CorrelationIdFormat))
            errors.Add("Correlation ID format cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(MessageFormat))
            errors.Add("Message format template cannot be null or empty.");

        if (IncludeTimestamps && string.IsNullOrWhiteSpace(TimestampFormat))
            errors.Add("Timestamp format cannot be null or empty when timestamps are enabled.");

        if (CachingEnabled && MaxCacheSize <= 0)
            errors.Add("Max cache size must be greater than zero when caching is enabled.");

        // Validate nested configurations
        foreach (var targetConfig in TargetConfigs)
        {
            var targetErrors = targetConfig.Validate();
            errors.AddRange(targetErrors);
        }

        foreach (var channelConfig in ChannelConfigs)
        {
            var channelErrors = channelConfig.Validate();
            errors.AddRange(channelErrors);
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Creates a copy of this configuration with the specified modifications.
    /// </summary>
    /// <param name="modifications">Action to apply modifications to the copy</param>
    /// <returns>A new LoggingConfig instance with the modifications applied</returns>
    public LoggingConfig WithModifications(Action<LoggingConfig> modifications)
    {
        if (modifications == null)
            throw new ArgumentNullException(nameof(modifications));

        var copy = this with { };
        modifications(copy);
        return copy;
    }
}
```

### ILogTargetConfig Interface

Interface for game-optimized log target configuration.

```csharp
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

/// <summary>
/// Interface for log target configuration with strongly-typed properties.
/// Provides game-optimized configuration options for Unity development.
/// </summary>
public interface ILogTargetConfig
{
    /// <summary>
    /// Gets the unique name of the log target.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of the log target (e.g., "Console", "File", "Network").
    /// </summary>
    string TargetType { get; }

    /// <summary>
    /// Gets the minimum log level for this target.
    /// </summary>
    LogLevel MinimumLevel { get; }

    /// <summary>
    /// Gets whether this target is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the maximum number of messages to buffer for this target.
    /// </summary>
    int BufferSize { get; }

    /// <summary>
    /// Gets the flush interval for this target.
    /// </summary>
    TimeSpan FlushInterval { get; }

    /// <summary>
    /// Gets whether this target should use asynchronous writing.
    /// </summary>
    bool UseAsyncWrite { get; }

    /// <summary>
    /// Gets target-specific configuration properties.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets the message format template specific to this target.
    /// If null or empty, the global message format will be used.
    /// </summary>
    string MessageFormat { get; }

    /// <summary>
    /// Gets the list of channels this target should listen to.
    /// If empty, the target will listen to all channels.
    /// </summary>
    IReadOnlyList<string> Channels { get; }

    /// <summary>
    /// Gets whether this target should include stack traces in error messages.
    /// </summary>
    bool IncludeStackTrace { get; }

    /// <summary>
    /// Gets whether this target should include correlation IDs in messages.
    /// </summary>
    bool IncludeCorrelationId { get; }

    // Game-specific performance monitoring configuration
    
    /// <summary>
    /// Gets the error rate threshold (0.0 to 1.0) that triggers alerts.
    /// Default: 0.1 (10% error rate)
    /// </summary>
    double ErrorRateThreshold { get; }

    /// <summary>
    /// Gets the frame budget threshold in milliseconds per write operation.
    /// Operations exceeding this threshold will trigger performance alerts.
    /// Default: 0.5ms for 60 FPS games (16.67ms frame budget)
    /// </summary>
    double FrameBudgetThresholdMs { get; }

    /// <summary>
    /// Gets the alert suppression interval in minutes.
    /// Prevents alert spam by suppressing duplicate alerts within this timeframe.
    /// Default: 5 minutes
    /// </summary>
    int AlertSuppressionIntervalMinutes { get; }

    /// <summary>
    /// Gets the maximum concurrent async operations for this target.
    /// Limits memory usage and prevents thread pool exhaustion.
    /// Default: 10 concurrent operations
    /// </summary>
    int MaxConcurrentAsyncOperations { get; }

    /// <summary>
    /// Gets whether Unity Profiler integration is enabled.
    /// When enabled, operations will be tracked in Unity Profiler.
    /// Default: true in development builds, false in production
    /// </summary>
    bool EnableUnityProfilerIntegration { get; }

    /// <summary>
    /// Gets whether performance metrics should be tracked and reported.
    /// Default: true
    /// </summary>
    bool EnablePerformanceMetrics { get; }

    /// <summary>
    /// Gets the health check interval in seconds.
    /// More frequent checks for game development scenarios.
    /// Default: 30 seconds
    /// </summary>
    int HealthCheckIntervalSeconds { get; }

    /// <summary>
    /// Validates the target configuration and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    IReadOnlyList<string> Validate();
}
```

### ILogConfigBuilder

Comprehensive builder interface with scenario-based configurations.

```csharp
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

/// <summary>
/// Interface for building logging configuration in a fluent manner.
/// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
/// Provides comprehensive configuration options for all available log targets.
/// </summary>
public interface ILogConfigBuilder
{
    // Basic configuration methods
    ILogConfigBuilder WithGlobalMinimumLevel(LogLevel logLevel);
    ILogConfigBuilder WithLoggingEnabled(bool enabled);
    ILogConfigBuilder WithMaxQueueSize(int maxQueueSize);
    ILogConfigBuilder WithFlushInterval(TimeSpan flushInterval);
    ILogConfigBuilder WithHighPerformanceMode(bool enabled);
    ILogConfigBuilder WithBurstCompatibility(bool enabled);
    ILogConfigBuilder WithStructuredLogging(bool enabled);
    ILogConfigBuilder WithBatching(bool enabled, int batchSize = 100);
    ILogConfigBuilder WithCaching(bool enabled, int maxCacheSize = 1000);

    // Target configuration methods
    ILogConfigBuilder WithTarget(LogTargetConfig targetConfig);
    ILogConfigBuilder WithTargets(params LogTargetConfig[] targetConfigs);
    ILogConfigBuilder WithTargets(IEnumerable<LogTargetConfig> targetConfigs);
    ILogConfigBuilder WithConsoleTarget(string name = "Console", LogLevel minimumLevel = LogLevel.Debug);
    ILogConfigBuilder WithFileTarget(string name, string filePath, LogLevel minimumLevel = LogLevel.Info, int bufferSize = 100);
    ILogConfigBuilder WithMemoryTarget(string name = "Memory", int maxEntries = 1000, LogLevel minimumLevel = LogLevel.Debug);
    ILogConfigBuilder WithSerilogTarget(string name = "Serilog", LogLevel minimumLevel = LogLevel.Info, object loggerConfiguration = null);
    ILogConfigBuilder WithNullTarget(string name = "Null");
    ILogConfigBuilder WithStandardConsoleTarget(string name = "StdConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true);
    ILogConfigBuilder WithUnityConsoleTarget(string name = "UnityConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true, bool showStackTraces = true);
    ILogConfigBuilder WithNetworkTarget(string name, string endpoint, LogLevel minimumLevel = LogLevel.Info, int timeoutSeconds = 30);
    ILogConfigBuilder WithDatabaseTarget(string name, string connectionString, string tableName = "Logs", LogLevel minimumLevel = LogLevel.Info);
    ILogConfigBuilder WithEmailTarget(string name, string smtpServer, string fromEmail, string[] toEmails, LogLevel minimumLevel = LogLevel.Error);

    // Channel configuration methods
    ILogConfigBuilder WithChannel(LogChannelConfig channelConfig);
    ILogConfigBuilder WithChannels(params LogChannelConfig[] channelConfigs);
    ILogConfigBuilder WithChannels(IEnumerable<LogChannelConfig> channelConfigs);
    ILogConfigBuilder WithChannel(string name, LogLevel minimumLevel = LogLevel.Debug, bool enabled = true);

    // Message formatting methods
    ILogConfigBuilder WithCorrelationIdFormat(string format);
    ILogConfigBuilder WithAutoCorrelationId(bool enabled);
    ILogConfigBuilder WithMessageFormat(string template);
    ILogConfigBuilder WithTimestamps(bool enabled);
    ILogConfigBuilder WithTimestampFormat(string format);

    // Scenario-based configuration methods
    ILogConfigBuilder ForProduction();
    ILogConfigBuilder ForDevelopment();
    ILogConfigBuilder ForTesting();
    ILogConfigBuilder ForStaging();
    ILogConfigBuilder ForPerformanceTesting();
    ILogConfigBuilder ForHighAvailability();
    ILogConfigBuilder ForCloudDeployment();
    ILogConfigBuilder ForMobile();
    ILogConfigBuilder ForDebugging(string debugChannel = "Debug");

    // Validation and build methods
    IReadOnlyList<string> Validate();
    LoggingConfig Build();
    ILogConfigBuilder Reset();
}
```

### Configuration Usage Examples

```csharp
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Models;

// Basic configuration with default settings
var basicConfig = new LogConfigBuilder()
    .WithGlobalMinimumLevel(LogLevel.Info)
    .WithConsoleTarget()
    .WithFileTarget("AppLog", "logs/app.log")
    .WithAutoCorrelationId(true)
    .Build();

// Development configuration with comprehensive debugging
var devConfig = new LogConfigBuilder()
    .ForDevelopment()  // Sets up Unity console, standard console, memory buffer, and file logging
    .WithChannel("GameLogic", LogLevel.Debug)
    .WithChannel("Networking", LogLevel.Debug)
    .WithChannel("Physics", LogLevel.Info)
    .Build();

// Production configuration with enterprise features
var productionConfig = new LogConfigBuilder()
    .ForProduction()  // Sets up Serilog, file logging, memory buffer, and email alerts
    .WithHighPerformanceMode(true)
    .WithBurstCompatibility(true)
    .WithBatching(true, batchSize: 500)
    .WithCaching(true, maxCacheSize: 5000)
    .Build();

// High-availability configuration with multiple redundant targets
var haConfig = new LogConfigBuilder()
    .ForHighAvailability()  // Sets up comprehensive logging with Serilog, database, network, and email alerts
    .WithNetworkTarget("PrimaryLog", "https://logs.primary.com", LogLevel.Info)
    .WithNetworkTarget("BackupLog", "https://logs.backup.com", LogLevel.Warning)
    .WithDatabaseTarget("LogDB", "Server=db1;Database=Logs;")
    .Build();

// Mobile optimized configuration with minimal overhead
var mobileConfig = new LogConfigBuilder()
    .ForMobile()  // Minimal logging with small memory buffer and error-only file logging
    .WithMaxQueueSize(100)  // Small queue for mobile
    .WithFlushInterval(TimeSpan.FromSeconds(60))  // Less frequent flushes
    .Build();

// Testing configuration with comprehensive capture
var testConfig = new LogConfigBuilder()
    .ForTesting()  // Large memory buffer and null target for unit tests
    .WithMemoryTarget("TestCapture", maxEntries: 10000, LogLevel.Trace)
    .WithStructuredLogging(true)
    .Build();

// Performance testing configuration
var perfConfig = new LogConfigBuilder()
    .ForPerformanceTesting()  // Memory-only targets for minimal overhead
    .WithHighPerformanceMode(true)
    .WithBurstCompatibility(true)
    .WithBatching(false)  // Disable batching for accurate timing
    .Build();

// Advanced custom configuration
var customConfig = new LogConfigBuilder()
    .WithGlobalMinimumLevel(LogLevel.Debug)
    .WithHighPerformanceMode(true)
    .WithBurstCompatibility(true)
    .WithStructuredLogging(true)
    .WithBatching(true, batchSize: 200)
    .WithCaching(true, maxCacheSize: 2000)
    // Add multiple targets with different configurations
    .WithUnityConsoleTarget(minimumLevel: LogLevel.Info, showStackTraces: true)
    .WithFileTarget("DetailedLog", "logs/detailed.log", LogLevel.Debug)
    .WithFileTarget("ErrorLog", "logs/errors.log", LogLevel.Error)
    .WithSerilogTarget("Serilog", LogLevel.Info)
    .WithMemoryTarget("Buffer", maxEntries: 5000)
    // Add channels for different subsystems
    .WithChannel("UI", LogLevel.Info)
    .WithChannel("GameLogic", LogLevel.Debug)
    .WithChannel("Networking", LogLevel.Debug)
    .WithChannel("Audio", LogLevel.Warning)
    .WithChannel("Performance", LogLevel.Info)
    // Configure message formatting
    .WithMessageFormat("[{Timestamp:HH:mm:ss.fff}] [{Level,-5}] [{Channel,-12}] {Message}")
    .WithTimestampFormat("HH:mm:ss.fff")
    .WithCorrelationIdFormat("{0:D}")
    .WithAutoCorrelationId(true)
    .Build();
```

## 🔧 Advanced Features

### Log Filters

The logging system provides comprehensive filtering capabilities to control which log messages are processed.

#### Available Filters

- **LevelFilter**: Filters messages based on log level
- **SourceFilter**: Filters messages based on source context or namespace
- **CorrelationFilter**: Filters messages based on correlation ID patterns
- **PatternFilter**: Filters messages using regex patterns on message content
- **RateLimitFilter**: Prevents log flooding by limiting message rates
- **SamplingFilter**: Statistical sampling for high-volume scenarios
- **TimeRangeFilter**: Filters messages based on time windows

#### Filter Usage Example

```csharp
using AhBearStudios.Core.Logging.Filters;

// Create rate limit filter to prevent spam
var rateLimitFilter = new RateLimitFilter
{
    Name = "RateLimit",
    MaxMessagesPerSecond = 100,
    BurstCapacity = 200
};

// Create pattern filter to exclude sensitive data
var sensitiveDataFilter = new PatternFilter
{
    Name = "SensitiveData",
    ExcludePatterns = new[] { @"password\s*=\s*\S+", @"\b\d{4}-\d{4}-\d{4}-\d{4}\b" },
    IsExclusive = true  // Exclude matching messages
};

// Create sampling filter for performance metrics
var performanceFilter = new SamplingFilter
{
    Name = "PerformanceSampling",
    SampleRate = 0.1,  // Sample 10% of messages
    ChannelFilter = "Performance"
};

// Apply filters to logging service
_loggingService.AddFilter(rateLimitFilter);
_loggingService.AddFilter(sensitiveDataFilter);
_loggingService.AddFilter(performanceFilter);
```

### Log Formatters

The system supports multiple output formats for different use cases.

#### Available Formatters

- **PlainTextFormatter**: Human-readable plain text format
- **JsonFormatter**: Structured JSON format for log aggregation
- **StructuredFormatter**: Key-value structured format
- **BinaryFormatter**: Efficient binary format for performance
- **CefFormatter**: Common Event Format for security tools
- **CsvFormatter**: CSV format for data analysis
- **GelfFormatter**: Graylog Extended Log Format
- **KeyValueFormatter**: Simple key=value format
- **MessagePackFormatter**: High-performance binary serialization
- **ProtobufFormatter**: Protocol Buffers format for cross-platform
- **SyslogFormatter**: RFC 5424 Syslog format
- **XmlFormatter**: XML format for enterprise systems

#### Formatter Configuration Example

```csharp
// Configure JSON formatter for file target
var jsonFormatter = new JsonFormatter
{
    PrettyPrint = false,
    IncludeStackTrace = true,
    IncludeCorrelationId = true,
    DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fffZ"
};

// Configure CEF formatter for security logging
var cefFormatter = new CefFormatter
{
    DeviceVendor = "AhBearStudios",
    DeviceProduct = "GameEngine",
    DeviceVersion = "1.0.0",
    IncludeExtensions = true
};

// Configure binary formatter for high-performance scenarios
var binaryFormatter = new BinaryFormatter
{
    CompressionLevel = CompressionLevel.Fastest,
    IncludeMetadata = false
};
```

### Log Targets

Comprehensive set of output targets for different deployment scenarios.

#### Available Targets

- **ConsoleLogTarget**: Standard console output
- **UnityConsoleLogTarget**: Unity Editor console integration
- **FileLogTarget**: File-based logging with rotation
- **MemoryLogTarget**: In-memory circular buffer
- **SerilogTarget**: Serilog integration for enterprise logging
- **NullLogTarget**: No-op target for testing

#### Target Configuration Example

```csharp
// File target with rotation
var fileTarget = new FileLogTarget
{
    Name = "MainLog",
    FilePath = "logs/game.log",
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxFiles = 5,
    BufferSize = 1000,
    FlushInterval = TimeSpan.FromSeconds(10),
    IncludeTimestamp = true,
    Formatter = new JsonFormatter()
};

// Memory target for recent log capture
var memoryTarget = new MemoryLogTarget
{
    Name = "RecentLogs",
    MaxEntries = 1000,
    CircularBuffer = true,
    MinimumLevel = LogLevel.Debug
};

// Serilog target for production
var serilogTarget = new SerilogTarget
{
    Name = "Serilog",
    MinimumLevel = LogLevel.Info,
    WriteTo = new[] { "Seq", "ElasticSearch", "ApplicationInsights" },
    EnrichWithCorrelationId = true,
    EnrichWithMachineName = true
};
```

### Channels and Scopes

Organize logs by subsystem and create hierarchical contexts.

```csharp
// Register channels for different subsystems
_loggingService.RegisterChannel(new LogChannel
{
    Name = "Gameplay",
    MinimumLevel = LogLevel.Debug,
    Targets = new[] { "Console", "GameplayLog" }
});

_loggingService.RegisterChannel(new LogChannel
{
    Name = "Networking",
    MinimumLevel = LogLevel.Info,
    Targets = new[] { "Console", "NetworkLog", "Serilog" }
});

// Use scopes for hierarchical context
using (var gameScope = _loggingService.BeginScope("GameSession"))
{
    gameScope.SetProperty("SessionId", sessionId);
    gameScope.SetProperty("PlayerId", playerId);
    
    gameScope.LogInfo("Game session started");
    
    using (var levelScope = gameScope.BeginChild("Level1"))
    {
        levelScope.SetProperty("LevelName", "Tutorial");
        levelScope.LogDebug("Loading level assets");
        
        // All logs within this scope include session and level context
        levelScope.LogInfo("Level loaded successfully");
    }
}
```

### Performance Monitoring Integration

Track logging system performance metrics.

```csharp
// Get logging statistics
var stats = _loggingService.GetStatistics();
Console.WriteLine($"Total messages: {stats.TotalMessages}");
Console.WriteLine($"Messages per second: {stats.MessagesPerSecond}");
Console.WriteLine($"Average processing time: {stats.AverageProcessingTime}ms");
Console.WriteLine($"Buffer utilization: {stats.BufferUtilization:P}");

// Monitor individual targets
foreach (var target in _loggingService.GetTargets())
{
    if (target is ILogTarget logTarget)
    {
        var targetStats = logTarget.GetStatistics();
        Console.WriteLine($"{logTarget.Name}: {targetStats.ProcessedMessages} messages, " +
                        $"{targetStats.FailedMessages} failures");
    }
}
```

## 📦 Installation

### LoggingInstaller Bootstrap Integration

Complete implementation of IBootstrapInstaller for Reflex DI integration.

```csharp
/// <summary>
/// Bootstrap installer for the logging system with comprehensive integration.
/// </summary>
public class LoggingInstaller : IBootstrapInstaller
{
    private readonly ILoggingService _logger;
    private readonly LoggingConfig _config;
    private bool _isValidated;

    public string InstallerName => "LoggingInstaller";
    public int Priority => 0; // Highest priority - foundation system
    public bool IsEnabled => true;
    public Type[] Dependencies => Array.Empty<Type>(); // No dependencies

    /// <summary>
    /// Initializes the logging installer.
    /// </summary>
    /// <param name="config">Logging configuration</param>
    public LoggingInstaller(LoggingConfig config = null)
    {
        _config = config ?? LoggingConfig.Default;
        
        // Bootstrap logger for installer operations
        _logger = new BasicLogger(); // Temporary logger until full service is available
    }

    /// <summary>
    /// Validates the installer configuration and dependencies.
    /// </summary>
    /// <returns>True if validation passes</returns>
    public bool ValidateInstaller()
    {
        var correlationId = CorrelationId.Generate();
        
        try
        {
            _logger.LogInfo("Validating LoggingInstaller configuration", correlationId);

            // Validate configuration
            if (_config == null)
            {
                _logger.LogError("LoggingConfig is null", correlationId);
                return false;
            }

            // Validate buffer settings
            if (_config.EnableBuffering && _config.BufferSize <= 0)
            {
                _logger.LogError($"Invalid buffer size: {_config.BufferSize}", correlationId);
                return false;
            }

            // Validate targets
            if (_config.TargetConfigs?.Any() != true)
            {
                _logger.LogWarning("No log targets configured, using default console target", correlationId);
            }

            // Validate target types
            foreach (var target in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
            {
                if (target.TargetType == null || !typeof(ILogTarget).IsAssignableFrom(target.TargetType))
                {
                    _logger.LogError($"Invalid target type for '{target.Name}': {target.TargetType?.Name}", correlationId);
                    return false;
                }
            }

            // Validate formatter types
            foreach (var formatter in _config.FormatterConfigs?.Values ?? Enumerable.Empty<FormatterConfig>())
            {
                if (formatter.FormatterType == null || !typeof(ILogFormatter).IsAssignableFrom(formatter.FormatterType))
                {
                    _logger.LogError($"Invalid formatter type for '{formatter.Name}': {formatter.FormatterType?.Name}", correlationId);
                    return false;
                }
            }

            _isValidated = true;
            _logger.LogInfo("LoggingInstaller validation completed successfully", correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException("LoggingInstaller validation failed", ex, correlationId);
            return false;
        }
    }

    /// <summary>
    /// Pre-installation setup operations.
    /// </summary>
    public void PreInstall()
    {
        var correlationId = CorrelationId.Generate();
        
        try
        {
            _logger.LogInfo("Starting LoggingInstaller pre-installation", correlationId);

            if (!_isValidated)
            {
                throw new InvalidOperationException("Installer must be validated before installation");
            }

            // Create log directories if file targets are configured
            CreateLogDirectories(correlationId);

            // Initialize any required external connections
            InitializeExternalConnections(correlationId);

            _logger.LogInfo("LoggingInstaller pre-installation completed", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogException("LoggingInstaller pre-installation failed", ex, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Installs the logging system into the Reflex container.
    /// </summary>
    /// <param name="builder">Container builder</param>
    public void Install(ContainerBuilder builder)
    {
        var correlationId = CorrelationId.Generate();
        
        try
        {
            _logger.LogInfo("Installing logging system components", correlationId);

            // Register configuration
            RegisterConfiguration(builder, correlationId);

            // Register core interfaces and implementations
            RegisterCoreServices(builder, correlationId);

            // Register targets
            RegisterTargets(builder, correlationId);

            // Register formatters
            RegisterFormatters(builder, correlationId);

            // Register filters
            RegisterFilters(builder, correlationId);

            // Register factories
            RegisterFactories(builder, correlationId);

            // Register health checks
            RegisterHealthChecks(builder, correlationId);

            _logger.LogInfo("Logging system installation completed successfully", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogException("Logging system installation failed", ex, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Post-installation configuration and integration.
    /// </summary>
    public void PostInstall()
    {
        var correlationId = CorrelationId.Generate();
        
        try
        {
            _logger.LogInfo("Starting LoggingInstaller post-installation", correlationId);

            var container = Container.Current;

            // Initialize the main logging service
            var loggingService = container.Resolve<ILoggingService>();
            
            // Register with health check service if available
            if (container.HasBinding<IHealthCheckService>())
            {
                var healthService = container.Resolve<IHealthCheckService>();
                var healthCheck = container.Resolve<LoggingServiceHealthCheck>();
                healthService.RegisterHealthCheck(healthCheck);
                
                _logger.LogInfo("Registered logging health check", correlationId);
            }

            // Initialize performance monitoring if profiler service is available
            if (container.HasBinding<IProfilerService>())
            {
                var profilerService = container.Resolve<IProfilerService>();
                profilerService.RegisterMetricAlert("logging_performance", 
                    threshold: 100, // 100ms threshold for logging operations
                    alertLevel: AlertSeverity.Warning);
                
                _logger.LogInfo("Registered logging performance monitoring", correlationId);
            }

            // Configure alert integration if alert service is available
            if (container.HasBinding<IAlertService>())
            {
                var alertService = container.Resolve<IAlertService>();
                
                // Configure automatic alerts for critical log events
                ConfigureAutomaticAlerts(alertService, correlationId);
                
                _logger.LogInfo("Configured logging alert integration", correlationId);
            }

            // Replace bootstrap logger with full service
            ReplaceBootstrapLogger(loggingService, correlationId);

            _logger.LogInfo("LoggingInstaller post-installation completed", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogException("LoggingInstaller post-installation failed", ex, correlationId);
            throw;
        }
    }

    #region Private Registration Methods

    private void RegisterConfiguration(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        builder.Bind<LoggingConfig>().FromInstance(_config);
        _logger.LogDebug("Registered LoggingConfig", correlationId);
    }

    private void RegisterCoreServices(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        // Register main logging service
        builder.Bind<ILoggingService>().To<LoggingService>().AsSingle();
        
        // Register supporting services
        builder.Bind<ILogFormattingService>().To<LogFormattingService>().AsSingle();
        builder.Bind<ILogBatchingService>().To<LogBatchingService>().AsSingle();
        builder.Bind<ILogCorrelationService>().To<LogCorrelationService>().AsSingle();
        
        _logger.LogDebug("Registered core logging services", correlationId);
    }

    private void RegisterTargets(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        // Register default targets
        builder.Bind<ILogTarget>().To<ConsoleLogTarget>().WithId("Console");
        builder.Bind<ILogTarget>().To<FileLogTarget>().WithId("File");
        builder.Bind<ILogTarget>().To<DatabaseLogTarget>().WithId("Database");

        // Register custom targets from configuration
        foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (targetConfig.Enabled && targetConfig.TargetType != null)
            {
                builder.Bind<ILogTarget>().To(targetConfig.TargetType).WithId(targetConfig.Name.ToString());
            }
        }
        
        _logger.LogDebug($"Registered {_config.TargetConfigs?.Count ?? 0} log targets", correlationId);
    }

    private void RegisterFormatters(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        // Register default formatters
        builder.Bind<ILogFormatter>().To<PlainTextFormatter>().WithId("PlainText");
        builder.Bind<ILogFormatter>().To<JsonLogFormatter>().WithId("Json");
        builder.Bind<ILogFormatter>().To<StructuredFormatter>().WithId("Structured");

        // Register custom formatters from configuration
        foreach (var formatterConfig in _config.FormatterConfigs?.Values ?? Enumerable.Empty<FormatterConfig>())
        {
            if (formatterConfig.FormatterType != null)
            {
                builder.Bind<ILogFormatter>().To(formatterConfig.FormatterType).WithId(formatterConfig.Name.ToString());
            }
        }
        
        _logger.LogDebug($"Registered {_config.FormatterConfigs?.Count ?? 0} log formatters", correlationId);
    }

    private void RegisterFilters(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        // Register default filters
        builder.Bind<ILogFilter>().To<LevelFilter>().WithId("Level");
        builder.Bind<ILogFilter>().To<SourceFilter>().WithId("Source");

        // Register custom filters from configuration
        foreach (var filterConfig in _config.FilterConfigs ?? Enumerable.Empty<FilterConfig>())
        {
            if (filterConfig.Enabled && filterConfig.FilterType != null)
            {
                builder.Bind<ILogFilter>().To(filterConfig.FilterType).WithId(filterConfig.Name.ToString());
            }
        }
        
        _logger.LogDebug($"Registered {_config.FilterConfigs?.Count ?? 0} log filters", correlationId);
    }

    private void RegisterFactories(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        builder.Bind<ILoggingServiceFactory>().To<LoggingServiceFactory>().AsSingle();
        builder.Bind<ILogTargetFactory>().To<LogTargetFactory>().AsSingle();
        builder.Bind<ILogFormatterFactory>().To<LogFormatterFactory>().AsSingle();
        
        _logger.LogDebug("Registered logging factories", correlationId);
    }

    private void RegisterHealthChecks(ContainerBuilder builder, FixedString64Bytes correlationId)
    {
        builder.Bind<LoggingServiceHealthCheck>().AsSingle();
        _logger.LogDebug("Registered logging health checks", correlationId);
    }

    #endregion

    #region Private Helper Methods

    private void CreateLogDirectories(FixedString64Bytes correlationId)
    {
        foreach (var target in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (target.TargetType == typeof(FileLogTarget) && 
                target.Settings?.TryGetValue("FilePath", out var filePathObj) == true &&
                filePathObj is string filePath)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogDebug($"Created log directory: {directory}", correlationId);
                }
            }
        }
    }

    private void InitializeExternalConnections(FixedString64Bytes correlationId)
    {
        // Initialize database connections, external services, etc.
        foreach (var target in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (target.TargetType == typeof(DatabaseLogTarget))
            {
                // Test database connectivity
                _logger.LogDebug($"Testing database connection for target: {target.Name}", correlationId);
            }
        }
    }

    private void ConfigureAutomaticAlerts(IAlertService alertService, FixedString64Bytes correlationId)
    {
        // Configure alerts for critical logging events
        var alertConfig = new AlertConfig
        {
            Name = "LoggingSystemAlert",
            Severity = AlertSeverity.Critical,
            Description = "Critical logging system events",
            AutoResolve = true,
            ResolutionTimeout = TimeSpan.FromMinutes(5)
        };

        // This would be handled by the alert service integration
        _logger.LogDebug("Configured automatic alert integration", correlationId);
    }

    private void ReplaceBootstrapLogger(ILoggingService fullService, FixedString64Bytes correlationId)
    {
        // Replace temporary bootstrap logger with full service
        // This would involve updating any global logger references
        _logger.LogInfo("Replaced bootstrap logger with full logging service", correlationId);
    }

    #endregion
}
```

### Extension Methods for Clean Registration

```csharp
/// <summary>
/// Extension methods for clean Reflex container registration.
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Adds the complete logging system to the container.
    /// </summary>
    /// <param name="builder">Container builder</param>
    /// <param name="config">Optional logging configuration</param>
    /// <returns>Container builder for chaining</returns>
    public static ContainerBuilder AddLoggingSystem(this ContainerBuilder builder, LoggingConfig config = null)
    {
        var installer = new LoggingInstaller(config);
        
        if (!installer.ValidateInstaller())
        {
            throw new InvalidOperationException("LoggingInstaller validation failed");
        }
        
        installer.PreInstall();
        installer.Install(builder);
        
        return builder;
    }

    /// <summary>
    /// Adds basic console and file logging.
    /// </summary>
    /// <param name="builder">Container builder</param>
    /// <param name="minimumLevel">Minimum log level</param>
    /// <param name="logFilePath">Path to log file (optional)</param>
    /// <returns>Container builder for chaining</returns>
    public static ContainerBuilder AddBasicLogging(this ContainerBuilder builder, 
        LogLevel minimumLevel = LogLevel.Information, string logFilePath = null)
    {
        var configBuilder = new LoggingConfigBuilder()
            .WithMinimumLevel(minimumLevel)
            .AddConsoleTarget(minimumLevel)
            .WithCorrelationTracking()
            .WithStructuredLogging();

        if (!string.IsNullOrEmpty(logFilePath))
        {
            configBuilder.AddFileTarget(logFilePath, LogLevel.Warning);
        }

        var config = configBuilder.Build();
        return builder.AddLoggingSystem(config);
    }

    /// <summary>
    /// Adds high-performance logging optimized for production.
    /// </summary>
    /// <param name="builder">Container builder</param>
    /// <param name="logDirectory">Directory for log files</param>
    /// <returns>Container builder for chaining</returns>
    public static ContainerBuilder AddHighPerformanceLogging(this ContainerBuilder builder, 
        string logDirectory = "logs")
    {
        var config = new LoggingConfigBuilder()
            .WithMinimumLevel(LogLevel.Warning)
            .WithBuffering(enabled: true, bufferSize: 10000, TimeSpan.FromMinutes(1))
            .AddFileTarget(Path.Combine(logDirectory, "application.log"), LogLevel.Warning)
            .AddFileTarget(Path.Combine(logDirectory, "errors.log"), LogLevel.Error)
            .WithCorrelationTracking(true)
            .WithStructuredLogging(true)
            .WithPerformanceTracking(true)
            .Build();

        return builder.AddLoggingSystem(config);
    }
}
```

## 🎮 Unity Integration

### Unity-Specific Components

The logging system provides seamless Unity integration with specialized components.

#### UnityLoggingBehaviour

MonoBehaviour component for runtime log visualization and control.

```csharp
using AhBearStudios.Core.Unity.Logging;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private UnityLoggingBehaviour _loggingBehaviour;
    
    void Start()
    {
        // Add logging behaviour to GameObject
        _loggingBehaviour = gameObject.AddComponent<UnityLoggingBehaviour>();
        
        // Configure Unity-specific settings
        _loggingBehaviour.ShowInGameConsole = true;
        _loggingBehaviour.MaxVisibleLogs = 50;
        _loggingBehaviour.LogLevelColors = new Dictionary<LogLevel, Color>
        {
            [LogLevel.Debug] = Color.gray,
            [LogLevel.Info] = Color.white,
            [LogLevel.Warning] = Color.yellow,
            [LogLevel.Error] = Color.red,
            [LogLevel.Critical] = Color.magenta
        };
    }
}
```

#### Unity Console Integration

The UnityConsoleLogTarget provides deep integration with Unity's console.

```csharp
// Configure Unity console target with stack trace support
var unityTarget = new UnityConsoleLogTarget
{
    Name = "UnityEditor",
    MinimumLevel = LogLevel.Debug,
    UseColors = true,
    ShowStackTraces = true,
    StackTraceLogLevel = LogLevel.Error,
    GroupByContext = true,
    CollapseIdenticalLogs = true
};

// Unity-specific log context
_logger.LogInfo("Player spawned", sourceContext: "GameplaySystem", 
    properties: new Dictionary<string, object>
    {
        ["Position"] = transform.position,
        ["Health"] = playerHealth,
        ["Level"] = currentLevel
    });
```

### Unity Job System Integration

Burst-compatible logging for high-performance scenarios.

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Logging;

[BurstCompile]
public struct PhysicsCalculationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [WriteOnly] public NativeArray<float3> velocities;
    
    // Burst-compatible logging data
    [ReadOnly] public FixedString64Bytes correlationId;
    [WriteOnly] public NativeQueue<LogMessage>.ParallelWriter logQueue;
    
    public void Execute(int index)
    {
        // Perform physics calculations
        var velocity = CalculateVelocity(positions[index]);
        velocities[index] = velocity;
        
        // Log significant events (Burst-compatible)
        if (math.length(velocity) > 100f)
        {
            var logData = new BurstLogData
            {
                Level = LogLevel.Warning,
                Message = "High velocity detected",
                Index = index,
                Value = math.length(velocity)
            };
            
            // Queue log for main thread processing
            logQueue.Enqueue(new LogMessage
            {
                Level = LogLevel.Warning,
                CorrelationId = correlationId,
                Data = logData
            });
        }
    }
}

// Process queued logs on main thread
void ProcessJobLogs(NativeQueue<LogMessage> logQueue)
{
    while (logQueue.TryDequeue(out var logMessage))
    {
        _logger.LogWarning<BurstLogData>(
            logMessage.Message, 
            logMessage.Data, 
            logMessage.CorrelationId
        );
    }
}
```

### Unity Profiler Integration

Track logging performance in Unity Profiler.

```csharp
using Unity.Profiling;

public class ProfilingExample : MonoBehaviour
{
    private static readonly ProfilerMarker s_LogMarker = new ProfilerMarker("Logging.Write");
    private static readonly ProfilerMarker s_FormatMarker = new ProfilerMarker("Logging.Format");
    
    void PerformLogging()
    {
        using (s_LogMarker.Auto())
        {
            // Logging operations are automatically profiled
            _logger.LogInfo("Game state updated", 
                sourceContext: "GameLoop",
                properties: GetGameStateProperties());
        }
    }
}
```

### ScriptableObject Configuration

Use ScriptableObjects for runtime configuration changes.

```csharp
using UnityEngine;
using AhBearStudios.Core.Unity.Logging.Configs;

[CreateAssetMenu(fileName = "LoggingConfig", menuName = "AhBearStudios/Logging Configuration")]
public class LoggingConfigAsset : ScriptableObject
{
    [Header("Global Settings")]
    public LogLevel globalMinimumLevel = LogLevel.Info;
    public bool enableLogging = true;
    
    [Header("Performance")]
    public bool highPerformanceMode = true;
    public bool burstCompatibility = true;
    public int maxQueueSize = 1000;
    
    [Header("Targets")]
    public bool enableUnityConsole = true;
    public bool enableFileLogging = true;
    public string logFilePath = "Logs/game.log";
    
    [Header("Channels")]
    public ChannelConfig[] channels = new[]
    {
        new ChannelConfig { name = "Gameplay", minimumLevel = LogLevel.Debug },
        new ChannelConfig { name = "Networking", minimumLevel = LogLevel.Info },
        new ChannelConfig { name = "UI", minimumLevel = LogLevel.Warning }
    };
    
    public LoggingConfig ToRuntimeConfig()
    {
        return new LoggingConfig
        {
            GlobalMinimumLevel = globalMinimumLevel,
            IsLoggingEnabled = enableLogging,
            HighPerformanceMode = highPerformanceMode,
            BurstCompatibility = burstCompatibility,
            MaxQueueSize = maxQueueSize,
            // Convert Unity-specific settings to runtime config
        };
    }
}
```

### Unity Event System Integration

Integrate with Unity's event system for reactive logging.

```csharp
using UnityEngine;
using UnityEngine.Events;

public class LoggingEventBridge : MonoBehaviour
{
    [System.Serializable]
    public class LogEvent : UnityEvent<LogLevel, string> { }
    
    public LogEvent onLogReceived;
    public LogEvent onErrorOccurred;
    
    private ILoggingService _logger;
    
    void Start()
    {
        _logger = Container.Resolve<ILoggingService>();
        
        // Subscribe to specific log levels
        var memoryTarget = _logger.GetTargets()
            .OfType<MemoryLogTarget>()
            .FirstOrDefault();
            
        if (memoryTarget != null)
        {
            memoryTarget.OnMessageLogged += HandleLogMessage;
        }
    }
    
    void HandleLogMessage(LogMessage message)
    {
        onLogReceived?.Invoke(message.Level, message.FormattedMessage);
        
        if (message.Level >= LogLevel.Error)
        {
            onErrorOccurred?.Invoke(message.Level, message.FormattedMessage);
        }
    }
}
```

### Unity Addressables Integration

Log asset loading and resource management.

```csharp
using UnityEngine.AddressableAssets;

public class AssetLoader : MonoBehaviour
{
    private readonly ILoggingService _logger;
    private readonly FixedString32Bytes _channel = "AssetLoading";
    
    async Task LoadGameAssets()
    {
        var correlationId = FixedString64Bytes.FromString(Guid.NewGuid().ToString());
        
        using (var scope = _logger.BeginScope("AssetLoading", correlationId))
        {
            scope.SetProperty("LoadType", "Addressables");
            scope.SetProperty("Platform", Application.platform.ToString());
            
            try
            {
                scope.LogInfo("Starting asset load");
                
                var handle = Addressables.LoadAssetsAsync<GameObject>(
                    "GameplayAssets", 
                    null);
                    
                await handle.Task;
                
                scope.SetProperty("LoadedCount", handle.Result.Count);
                scope.LogInfo($"Loaded {handle.Result.Count} assets successfully");
            }
            catch (Exception ex)
            {
                scope.LogException(ex, "Asset loading failed");
                throw;
            }
        }
    }
}
```

## 🚀 Usage Examples

### Basic Logging Operations

Modern C# patterns with comprehensive system integration.

```csharp
/// <summary>
/// Example service demonstrating proper logging integration.
/// </summary>
public class UserService : IUserService
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IProfilerService _profiler;
    private readonly FixedString32Bytes _serviceContext = "UserService";

    public UserService(ILoggingService logger, IAlertService alertService, IProfilerService profiler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService;
        _profiler = profiler;
    }

    /// <summary>
    /// Creates a new user with comprehensive logging and monitoring.
    /// </summary>
    public async Task<UserResult> CreateUserAsync(CreateUserRequest request)
    {
        var correlationId = CorrelationId.Generate();
        
        // Use profiler scope for performance monitoring
        using var scope = _profiler?.BeginScope("UserService.CreateUser", correlationId) ?? Disposable.Empty;
        
        try
        {
            _logger.LogInfo("Starting user creation process", correlationId, _serviceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["Email"] = request.Email,
                    ["RequestSource"] = request.Source
                });

            // Validation with detailed logging
            var validationResult = await ValidateUserRequestAsync(request, correlationId);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("User creation validation failed", correlationId, _serviceContext.ToString(),
                    new Dictionary<string, object>
                    {
                        ["UserId"] = request.UserId,
                        ["ValidationErrors"] = validationResult.Errors,
                        ["FailureReason"] = "InvalidInput"
                    });

                return UserResult.Failure("Validation failed", validationResult.Errors);
            }

            // Business logic with structured logging
            var user = await CreateUserInternalAsync(request, correlationId);

            _logger.LogInfo("User created successfully", correlationId, _serviceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["UserId"] = user.Id,
                    ["Email"] = user.Email,
                    ["CreatedAt"] = user.CreatedAt,
                    ["Duration"] = scope?.ElapsedMilliseconds ?? 0
                });

            return UserResult.Success(user);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("User creation validation exception", correlationId, _serviceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["ValidationMessage"] = ex.Message,
                    ["ValidationErrors"] = ex.Errors
                });

            return UserResult.Failure("Validation failed", ex.Errors);
        }
        catch (DuplicateUserException ex)
        {
            _logger.LogWarning("Duplicate user creation attempt", correlationId, _serviceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["Email"] = request.Email,
                    ["ExistingUserId"] = ex.ExistingUserId
                });

            return UserResult.Failure("User already exists", new[] { "EMAIL_ALREADY_EXISTS" });
        }
        catch (Exception ex)
        {
            _logger.LogException("Critical error during user creation", ex, correlationId, _serviceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["Email"] = request.Email,
                    ["ErrorType"] = ex.GetType().Name,
                    ["Duration"] = scope?.ElapsedMilliseconds ?? 0
                });

            // Trigger critical alert for unexpected errors
            if (_alertService != null)
            {
                await _alertService.RaiseAlert(
                    "UserCreationCriticalError",
                    AlertSeverity.Critical,
                    $"Critical error in user creation: {ex.Message}",
                    correlationId,
                    new Dictionary<FixedString32Bytes, object>
                    {
                        ["UserId"] = request.UserId.ToString(),
                        ["ErrorType"] = ex.GetType().Name
                    });
            }

            return UserResult.Failure("Internal error occurred");
        }
    }

    /// <summary>
    /// Example of correlation ID propagation across method calls.
    /// </summary>
    private async Task<ValidationResult> ValidateUserRequestAsync(CreateUserRequest request, FixedString64Bytes correlationId)
    {
        using var validationScope = _logger.BeginScope("UserValidation", correlationId);

        _logger.LogDebug("Validating user request", correlationId, _serviceContext.ToString(),
            new Dictionary<string, object>
            {
                ["UserId"] = request.UserId,
                ["Email"] = request.Email,
                ["ValidationRules"] = new[] { "EmailFormat", "PasswordStrength", "UserIdUniqueness" }
            });

        var errors = new List<string>();

        // Email validation
        if (!IsValidEmail(request.Email))
        {
            errors.Add("INVALID_EMAIL_FORMAT");
            _logger.LogDebug("Email validation failed", correlationId, _serviceContext.ToString(),
                new Dictionary<string, object> { ["Email"] = request.Email });
        }

        // Password validation
        if (!IsValidPassword(request.Password))
        {
            errors.Add("WEAK_PASSWORD");
            _logger.LogDebug("Password validation failed", correlationId, _serviceContext.ToString());
        }

        var result = new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };

        _logger.LogDebug("User validation completed", correlationId, _serviceContext.ToString(),
            new Dictionary<string, object>
            {
                ["IsValid"] = result.IsValid,
                ["ErrorCount"] = errors.Count,
                ["ValidationDuration"] = validationScope.ElapsedMilliseconds
            });

        return result;
    }
}
```

### Structured Logging with Context

```csharp
/// <summary>
/// Advanced structured logging with rich context and correlation.
/// </summary>
public class OrderProcessor : IOrderProcessor
{
    private readonly ILoggingService _logger;
    private readonly IMessageBusService _messageBus;
    private readonly FixedString32Bytes _serviceContext = "OrderProcessor";

    public OrderProcessor(ILoggingService logger, IMessageBusService messageBus)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    /// <summary>
    /// Processes an order with comprehensive logging and event correlation.
    /// </summary>
    public async Task<ProcessOrderResult> ProcessOrderAsync(Order order, FixedString64Bytes? parentCorrelationId = null)
    {
        var correlationId = parentCorrelationId ?? CorrelationId.Generate();
        
        // Create rich structured context
        var orderContext = new Dictionary<string, object>
        {
            ["OrderId"] = order.Id,
            ["CustomerId"] = order.CustomerId,
            ["OrderValue"] = order.TotalAmount,
            ["ItemCount"] = order.Items.Count,
            ["OrderType"] = order.Type.ToString(),
            ["ProcessingNode"] = Environment.MachineName,
            ["ProcessingVersion"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        };

        using var processingScope = _logger.BeginScope("OrderProcessing", correlationId, orderContext);

        try
        {
            _logger.LogInfo("Order processing started", correlationId, _serviceContext.ToString(), orderContext);

            // Step 1: Inventory validation
            var inventoryResult = await ValidateInventoryAsync(order, correlationId);
            if (!inventoryResult.IsValid)
            {
                var failureContext = orderContext.ToDictionary(x => x.Key, x => x.Value);
                failureContext["FailureReason"] = "InsufficientInventory";
                failureContext["UnavailableItems"] = inventoryResult.UnavailableItems;

                _logger.LogWarning("Order processing failed - insufficient inventory", correlationId, 
                    _serviceContext.ToString(), failureContext);

                return ProcessOrderResult.Failure("Insufficient inventory", inventoryResult.UnavailableItems);
            }

            // Step 2: Payment processing
            var paymentResult = await ProcessPaymentAsync(order, correlationId);
            if (!paymentResult.IsSuccessful)
            {
                var failureContext = orderContext.ToDictionary(x => x.Key, x => x.Value);
                failureContext["FailureReason"] = "PaymentFailed";
                failureContext["PaymentError"] = paymentResult.ErrorCode;
                failureContext["PaymentTransactionId"] = paymentResult.TransactionId;

                _logger.LogError("Order processing failed - payment declined", correlationId,
                    _serviceContext.ToString(), failureContext);

                return ProcessOrderResult.Failure("Payment failed", paymentResult.ErrorCode);
            }

            // Step 3: Order fulfillment
            var fulfillmentResult = await CreateFulfillmentAsync(order, correlationId);
            
            // Success logging with complete context
            var successContext = orderContext.ToDictionary(x => x.Key, x => x.Value);
            successContext["PaymentTransactionId"] = paymentResult.TransactionId;
            successContext["FulfillmentId"] = fulfillmentResult.FulfillmentId;
            successContext["ProcessingDurationMs"] = processingScope.ElapsedMilliseconds;
            successContext["EstimatedDelivery"] = fulfillmentResult.EstimatedDeliveryDate;

            _logger.LogInfo("Order processing completed successfully", correlationId, 
                _serviceContext.ToString(), successContext);

            // Publish success event with correlation
            await _messageBus.PublishMessage(new OrderProcessedEvent
            {
                OrderId = order.Id,
                CorrelationId = correlationId,
                ProcessedAt = DateTime.UtcNow,
                FulfillmentId = fulfillmentResult.FulfillmentId
            });

            return ProcessOrderResult.Success(fulfillmentResult);
        }
        catch (Exception ex)
        {
            var errorContext = orderContext.ToDictionary(x => x.Key, x => x.Value);
            errorContext["ErrorType"] = ex.GetType().Name;
            errorContext["ProcessingDurationMs"] = processingScope.ElapsedMilliseconds;

            _logger.LogException("Critical error during order processing", ex, correlationId,
                _serviceContext.ToString(), errorContext);

            // Publish failure event
            await _messageBus.PublishMessage(new OrderProcessingFailedEvent
            {
                OrderId = order.Id,
                CorrelationId = correlationId,
                FailedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name
            });

            throw;
        }
    }
}
```

### High-Performance Logging Patterns

```csharp
/// <summary>
/// Performance-optimized logging for high-throughput scenarios.
/// </summary>
public class HighThroughputEventProcessor : IEventProcessor
{
    private readonly ILoggingService _logger;
    private readonly IPoolingService _pooling;
    private readonly IProfilerService _profiler;
    
    // Pre-allocated context for performance
    private readonly FixedString32Bytes _serviceContext = "EventProcessor";
    private readonly FixedString32Bytes _performanceChannel = "Performance";

    public HighThroughputEventProcessor(ILoggingService logger, IPoolingService pooling, IProfilerService profiler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pooling = pooling ?? throw new ArgumentNullException(nameof(pooling));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
    }

    /// <summary>
    /// Processes events with minimal logging overhead.
    /// </summary>
    public async Task ProcessEventsAsync(IReadOnlyList<Event> events, FixedString64Bytes correlationId)
    {
        using var batchScope = _profiler.BeginScope("EventBatchProcessing", correlationId);

        // Use pooled objects for repeated operations
        using var contextPool = _pooling.GetService<DictionaryPool<string, object>>();
        var sharedContext = contextPool.Get();

        try
        {
            // Log batch start with minimal context
            sharedContext["BatchSize"] = events.Count;
            sharedContext["ProcessingNode"] = Environment.MachineName;

            _logger.LogInfo("Event batch processing started", correlationId, 
                _serviceContext.ToString(), sharedContext, _performanceChannel.ToString());

            var processedCount = 0;
            var errorCount = 0;

            foreach (var evt in events)
            {
                try
                {
                    using var eventScope = _profiler.BeginScope("EventProcessing");
                    
                    await ProcessSingleEventAsync(evt, correlationId);
                    processedCount++;

                    // Log only every 100th event for performance
                    if (processedCount % 100 == 0)
                    {
                        sharedContext.Clear();
                        sharedContext["ProcessedCount"] = processedCount;
                        sharedContext["BatchProgress"] = (double)processedCount / events.Count;

                        _logger.LogDebug("Event processing progress", correlationId,
                            _serviceContext.ToString(), sharedContext, _performanceChannel.ToString());
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;

                    // Log errors immediately but with minimal context
                    sharedContext.Clear();
                    sharedContext["EventId"] = evt.Id;
                    sharedContext["EventType"] = evt.Type;
                    sharedContext["ErrorCount"] = errorCount;

                    _logger.LogError($"Event processing failed: {ex.Message}", correlationId,
                        _serviceContext.ToString(), sharedContext);
                }
            }

            // Final batch summary
            sharedContext.Clear();
            sharedContext["TotalProcessed"] = processedCount;
            sharedContext["TotalErrors"] = errorCount;
            sharedContext["SuccessRate"] = (double)processedCount / events.Count;
            sharedContext["ProcessingDurationMs"] = batchScope.ElapsedMilliseconds;
            sharedContext["EventsPerSecond"] = events.Count / (batchScope.ElapsedMilliseconds / 1000.0);

            _logger.LogInfo("Event batch processing completed", correlationId,
                _serviceContext.ToString(), sharedContext, _performanceChannel.ToString());
        }
        finally
        {
            // Return pooled object
            sharedContext.Clear();
            contextPool.Return(sharedContext);
        }
    }
}
```

### Error Handling and Alert Integration

```csharp
/// <summary>
/// Comprehensive error handling with logging and alerting integration.
/// </summary>
public class CriticalSystemService : ICriticalSystemService
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IHealthCheckService _healthCheck;
    private readonly FixedString32Bytes _serviceContext = "CriticalSystem";

    public CriticalSystemService(ILoggingService logger, IAlertService alertService, IHealthCheckService healthCheck)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
    }

    /// <summary>
    /// Executes critical operations with comprehensive error handling.
    /// </summary>
    public async Task<OperationResult> ExecuteCriticalOperationAsync(CriticalRequest request)
    {
        var correlationId = CorrelationId.Generate();
        var operationContext = new Dictionary<string, object>
        {
            ["OperationId"] = request.OperationId,
            ["RequestType"] = request.Type.ToString(),
            ["Priority"] = request.Priority.ToString(),
            ["RequestedBy"] = request.UserId
        };

        try
        {
            _logger.LogInfo("Critical operation started", correlationId, _serviceContext.ToString(), operationContext);

            // Execute with different error handling patterns
            var result = request.Type switch
            {
                OperationType.DataMigration => await ExecuteDataMigrationAsync(request, correlationId),
                OperationType.SystemBackup => await ExecuteSystemBackupAsync(request, correlationId),
                OperationType.SecurityUpdate => await ExecuteSecurityUpdateAsync(request, correlationId),
                _ => throw new NotSupportedException($"Operation type {request.Type} is not supported")
            };

            operationContext["Duration"] = result.Duration.TotalMilliseconds;
            operationContext["Success"] = result.IsSuccess;

            _logger.LogInfo("Critical operation completed", correlationId, _serviceContext.ToString(), operationContext);

            return result;
        }
        catch (ValidationException ex)
        {
            return await HandleValidationErrorAsync(ex, correlationId, operationContext);
        }
        catch (TimeoutException ex)
        {
            return await HandleTimeoutErrorAsync(ex, correlationId, operationContext);
        }
        catch (SecurityException ex)
        {
            return await HandleSecurityErrorAsync(ex, correlationId, operationContext);
        }
        catch (Exception ex)
        {
            return await HandleCriticalErrorAsync(ex, correlationId, operationContext);
        }
    }

    private async Task<OperationResult> HandleValidationErrorAsync(ValidationException ex, 
        FixedString64Bytes correlationId, Dictionary<string, object> context)
    {
        context["ErrorType"] = "Validation";
        context["ValidationErrors"] = ex.Errors;

        _logger.LogWarning("Critical operation validation failed", correlationId, 
            _serviceContext.ToString(), context);

        return OperationResult.ValidationFailure(ex.Errors);
    }

    private async Task<OperationResult> HandleTimeoutErrorAsync(TimeoutException ex,
        FixedString64Bytes correlationId, Dictionary<string, object> context)
    {
        context["ErrorType"] = "Timeout";
        context["TimeoutDuration"] = ex.Data["Duration"];

        _logger.LogError("Critical operation timed out", correlationId, 
            _serviceContext.ToString(), context);

        // Trigger timeout alert
        await _alertService.RaiseAlert(
            "CriticalOperationTimeout",
            AlertSeverity.High,
            $"Critical operation timed out: {ex.Message}",
            correlationId,
            context.ToDictionary(x => (FixedString32Bytes)x.Key, x => x.Value));

        return OperationResult.TimeoutFailure();
    }

    private async Task<OperationResult> HandleSecurityErrorAsync(SecurityException ex,
        FixedString64Bytes correlationId, Dictionary<string, object> context)
    {
        context["ErrorType"] = "Security";
        context["SecurityViolationType"] = ex.Data["ViolationType"];

        _logger.LogError("Critical operation security violation", correlationId,
            _serviceContext.ToString(), context);

        // Immediate critical alert for security issues
        await _alertService.RaiseAlert(
            "SecurityViolation",
            AlertSeverity.Critical,
            $"Security violation in critical operation: {ex.Message}",
            correlationId,
            context.ToDictionary(x => (FixedString32Bytes)x.Key, x => x.Value));

        return OperationResult.SecurityFailure();
    }

    private async Task<OperationResult> HandleCriticalErrorAsync(Exception ex,
        FixedString64Bytes correlationId, Dictionary<string, object> context)
    {
        context["ErrorType"] = "Critical";
        context["ExceptionType"] = ex.GetType().Name;
        context["StackTrace"] = ex.StackTrace;

        _logger.LogException("Critical operation failed with unexpected error", ex, correlationId,
            _serviceContext.ToString(), context);

        // Critical system alert
        await _alertService.RaiseAlert(
            "CriticalOperationFailure",
            AlertSeverity.Critical,
            $"Critical operation failed: {ex.Message}",
            correlationId,
            context.ToDictionary(x => (FixedString32Bytes)x.Key, x => x.Value));

        // Update system health status
        await _healthCheck.ReportUnhealthyAsync("CriticalSystemService", 
            $"Critical operation failed: {ex.Message}", correlationId);

        return OperationResult.CriticalFailure(ex.Message);
    }
}
```
## 🏥 Health Monitoring

### LoggingServiceHealthCheck Implementation

Complete health check implementation with modern C# patterns and comprehensive monitoring.

```csharp
/// <summary>
/// Health check implementation for the logging system with comprehensive monitoring.
/// </summary>
public class LoggingServiceHealthCheck : IHealthCheck
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly LoggingConfig _config;
    private readonly IProfilerService _profiler;
    private readonly FixedString64Bytes _healthCheckName = "LoggingService";
    private readonly FixedString32Bytes _sourceContext = "LoggingHealthCheck";

    private readonly HealthCheckStatistics _statistics = new();
    private DateTime _lastSuccessfulCheck = DateTime.UtcNow;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly object _statisticsLock = new();

    public FixedString64Bytes Name => _healthCheckName;
    public string Description => "Monitors logging system health, performance, and target availability";
    public TimeSpan CheckInterval => TimeSpan.FromMinutes(1);
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public HealthCheckConfig Config { get; }

    public LoggingServiceHealthCheck(ILoggingService logger, IAlertService alertService, 
        LoggingConfig config, IProfilerService profiler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _profiler = profiler;

        Config = new HealthCheckConfig
        {
            Name = _healthCheckName,
            Description = Description,
            CheckInterval = CheckInterval,
            Timeout = Timeout,
            CriticalityLevel = CriticalityLevel.Critical,
            EnableRetries = true,
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>
    /// Executes comprehensive health check of the logging system.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(FixedString64Bytes correlationId = default)
    {
        if (correlationId.IsEmpty)
            correlationId = CorrelationId.Generate();

        using var healthCheckScope = _profiler?.BeginScope("LoggingHealthCheck", correlationId) ?? Disposable.Empty;

        try
        {
            var checkStartTime = DateTime.UtcNow;
            var results = new List<ComponentHealthResult>();

            // Check 1: Core logging service availability
            var coreServiceResult = await CheckCoreServiceHealthAsync(correlationId);
            results.Add(coreServiceResult);

            // Check 2: Log targets health
            var targetsResult = await CheckLogTargetsHealthAsync(correlationId);
            results.Add(targetsResult);

            // Check 3: Buffer and performance health
            var performanceResult = await CheckPerformanceHealthAsync(correlationId);
            results.Add(performanceResult);

            // Check 4: Correlation tracking health
            var correlationResult = await CheckCorrelationTrackingHealthAsync(correlationId);
            results.Add(correlationResult);

            // Check 5: System integration health
            var integrationResult = await CheckSystemIntegrationHealthAsync(correlationId);
            results.Add(integrationResult);

            // Determine overall health status
            var overallStatus = DetermineOverallHealth(results);
            var checkDuration = DateTime.UtcNow - checkStartTime;

            // Update statistics
            UpdateStatistics(overallStatus, checkDuration);

            // Create result with comprehensive data
            var healthResult = new HealthCheckResult
            {
                Name = _healthCheckName,
                Status = overallStatus,
                Description = GetHealthDescription(overallStatus, results),
                CheckedAt = checkStartTime,
                Duration = checkDuration,
                Data = CreateHealthData(results),
                ComponentResults = results
            };

            // Log health check completion
            _logger.LogDebug("Logging health check completed", correlationId, _sourceContext.ToString(),
                new Dictionary<string, object>
                {
                    ["Status"] = overallStatus.ToString(),
                    ["Duration"] = checkDuration.TotalMilliseconds,
                    ["ComponentCount"] = results.Count,
                    ["FailedComponents"] = results.Count(r => r.Status != HealthStatus.Healthy)
                });

            // Handle health status changes
            await HandleHealthStatusChangeAsync(overallStatus, healthResult, correlationId);

            return healthResult;
        }
        catch (Exception ex)
        {
            var errorResult = new HealthCheckResult
            {
                Name = _healthCheckName,
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed with exception: {ex.Message}",
                CheckedAt = DateTime.UtcNow,
                Duration = healthCheckScope?.Elapsed ?? TimeSpan.Zero,
                Exception = ex
            };

            _logger.LogException("Logging health check failed", ex, correlationId, _sourceContext.ToString());

            UpdateStatistics(HealthStatus.Unhealthy, healthCheckScope?.Elapsed ?? TimeSpan.Zero);

            return errorResult;
        }
    }

    /// <summary>
    /// Checks the health of the core logging service.
    /// </summary>
    private async Task<ComponentHealthResult> CheckCoreServiceHealthAsync(FixedString64Bytes correlationId)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // Test basic logging functionality
            var testMessage = $"Health check test message - {correlationId}";
            var testCorrelationId = CorrelationId.Generate();

            // Attempt to log a test message
            _logger.LogDebug(testMessage, testCorrelationId, _sourceContext.ToString(),
                new Dictionary<string, object> { ["HealthCheckTest"] = true });

            var duration = DateTime.UtcNow - startTime;

            return new ComponentHealthResult
            {
                ComponentName = "CoreLoggingService",
                Status = HealthStatus.Healthy,
                Description = "Core logging service is responsive",
                CheckDuration = duration,
                Data = new Dictionary<string, object>
                {
                    ["ResponseTime"] = duration.TotalMilliseconds,
                    ["TestCorrelationId"] = testCorrelationId.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthResult
            {
                ComponentName = "CoreLoggingService",
                Status = HealthStatus.Unhealthy,
                Description = $"Core logging service failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Checks the health of all configured log targets.
    /// </summary>
    private async Task<ComponentHealthResult> CheckLogTargetsHealthAsync(FixedString64Bytes correlationId)
    {
        var targetResults = new List<TargetHealthResult>();
        var overallTargetStatus = HealthStatus.Healthy;

        foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (!targetConfig.Enabled)
                continue;

            try
            {
                var targetResult = await CheckIndividualTargetHealthAsync(targetConfig, correlationId);
                targetResults.Add(targetResult);

                // Downgrade overall status if any target is unhealthy
                if (targetResult.Status == HealthStatus.Unhealthy)
                    overallTargetStatus = HealthStatus.Unhealthy;
                else if (targetResult.Status == HealthStatus.Degraded && overallTargetStatus == HealthStatus.Healthy)
                    overallTargetStatus = HealthStatus.Degraded;
            }
            catch (Exception ex)
            {
                targetResults.Add(new TargetHealthResult
                {
                    TargetName = targetConfig.Name.ToString(),
                    Status = HealthStatus.Unhealthy,
                    Description = $"Target health check failed: {ex.Message}",
                    Exception = ex
                });
                overallTargetStatus = HealthStatus.Unhealthy;
            }
        }

        return new ComponentHealthResult
        {
            ComponentName = "LogTargets",
            Status = overallTargetStatus,
            Description = GetTargetsHealthDescription(targetResults),
            Data = new Dictionary<string, object>
            {
                ["TotalTargets"] = targetResults.Count,
                ["HealthyTargets"] = targetResults.Count(t => t.Status == HealthStatus.Healthy),
                ["DegradedTargets"] = targetResults.Count(t => t.Status == HealthStatus.Degraded),
                ["UnhealthyTargets"] = targetResults.Count(t => t.Status == HealthStatus.Unhealthy),
                ["TargetDetails"] = targetResults.ToDictionary(t => t.TargetName, t => new
                {
                    Status = t.Status.ToString(),
                    Description = t.Description,
                    ResponseTime = t.ResponseTime?.TotalMilliseconds
                })
            }
        };
    }

    /// <summary>
    /// Checks the health of an individual log target.
    /// </summary>
    private async Task<TargetHealthResult> CheckIndividualTargetHealthAsync(TargetConfig targetConfig, FixedString64Bytes correlationId)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Target-specific health checks
            var result = targetConfig.TargetType.Name switch
            {
                nameof(ConsoleLogTarget) => await CheckConsoleTargetHealthAsync(targetConfig, correlationId),
                nameof(FileLogTarget) => await CheckFileTargetHealthAsync(targetConfig, correlationId),
                nameof(DatabaseLogTarget) => await CheckDatabaseTargetHealthAsync(targetConfig, correlationId),
                _ => await CheckGenericTargetHealthAsync(targetConfig, correlationId)
            };

            result.ResponseTime = DateTime.UtcNow - startTime;
            return result;
        }
        catch (Exception ex)
        {
            return new TargetHealthResult
            {
                TargetName = targetConfig.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = $"Target health check exception: {ex.Message}",
                ResponseTime = DateTime.UtcNow - startTime,
                Exception = ex
            };
        }
    }

    private async Task<TargetHealthResult> CheckConsoleTargetHealthAsync(TargetConfig config, FixedString64Bytes correlationId)
    {
        // Console is typically always available
        return new TargetHealthResult
        {
            TargetName = config.Name.ToString(),
            Status = HealthStatus.Healthy,
            Description = "Console target is available"
        };
    }

    private async Task<TargetHealthResult> CheckFileTargetHealthAsync(TargetConfig config, FixedString64Bytes correlationId)
    {
        try
        {
            if (config.Settings?.TryGetValue("FilePath", out var filePathObj) == true &&
                filePathObj is string filePath)
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);

                // Check directory accessibility
                if (!Directory.Exists(directory))
                {
                    return new TargetHealthResult
                    {
                        TargetName = config.Name.ToString(),
                        Status = HealthStatus.Unhealthy,
                        Description = $"Log directory does not exist: {directory}"
                    };
                }

                // Check write permissions
                var testFile = Path.Combine(directory, $"health_check_{correlationId}.tmp");
                await File.WriteAllTextAsync(testFile, "health check");
                File.Delete(testFile);

                // Check disk space
                var drive = new DriveInfo(Path.GetPathRoot(filePath));
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                var status = freeSpaceGB < 1.0 ? HealthStatus.Degraded : HealthStatus.Healthy;
                var description = freeSpaceGB < 1.0 
                    ? $"Low disk space: {freeSpaceGB:F2} GB remaining"
                    : $"File target healthy, {freeSpaceGB:F2} GB available";

                return new TargetHealthResult
                {
                    TargetName = config.Name.ToString(),
                    Status = status,
                    Description = description,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["FilePath"] = filePath,
                        ["FreeSpaceGB"] = freeSpaceGB,
                        ["DirectoryExists"] = true,
                        ["WritePermissions"] = true
                    }
                };
            }

            return new TargetHealthResult
            {
                TargetName = config.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = "File target configuration missing or invalid"
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new TargetHealthResult
            {
                TargetName = config.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = "Insufficient permissions to write to log file"
            };
        }
        catch (Exception ex)
        {
            return new TargetHealthResult
            {
                TargetName = config.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = $"File target error: {ex.Message}",
                Exception = ex
            };
        }
    }

    private async Task<TargetHealthResult> CheckDatabaseTargetHealthAsync(TargetConfig config, FixedString64Bytes correlationId)
    {
        try
        {
            if (config.Settings?.TryGetValue("ConnectionString", out var connectionStringObj) == true &&
                connectionStringObj is string connectionString)
            {
                // Test database connectivity (implementation would depend on database type)
                var connectionTest = await TestDatabaseConnectionAsync(connectionString);
                
                return new TargetHealthResult
                {
                    TargetName = config.Name.ToString(),
                    Status = connectionTest.IsSuccessful ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    Description = connectionTest.IsSuccessful 
                        ? "Database connection successful"
                        : $"Database connection failed: {connectionTest.ErrorMessage}",
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["ConnectionSuccessful"] = connectionTest.IsSuccessful,
                        ["ResponseTimeMs"] = connectionTest.ResponseTime.TotalMilliseconds
                    }
                };
            }

            return new TargetHealthResult
            {
                TargetName = config.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = "Database target configuration missing connection string"
            };
        }
        catch (Exception ex)
        {
            return new TargetHealthResult
            {
                TargetName = config.Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Description = $"Database target error: {ex.Message}",
                Exception = ex
            };
        }
    }

    private async Task<TargetHealthResult> CheckGenericTargetHealthAsync(TargetConfig config, FixedString64Bytes correlationId)
    {
        // Generic health check for custom targets
        return new TargetHealthResult
        {
            TargetName = config.Name.ToString(),
            Status = HealthStatus.Healthy,
            Description = $"Generic target {config.TargetType.Name} assumed healthy"
        };
    }

    /// <summary>
    /// Checks the performance and buffer health of the logging system.
    /// </summary>
    private async Task<ComponentHealthResult> CheckPerformanceHealthAsync(FixedString64Bytes correlationId)
    {
        try
        {
            var performanceData = new Dictionary<string, object>();
            var status = HealthStatus.Healthy;
            var issues = new List<string>();

            // Check buffer health if buffering is enabled
            if (_config.EnableBuffering)
            {
                var bufferStats = await GetBufferStatisticsAsync();
                performanceData["BufferUtilization"] = bufferStats.UtilizationPercentage;
                performanceData["BufferSize"] = bufferStats.TotalSize;
                performanceData["BufferUsed"] = bufferStats.UsedSize;
                performanceData["PendingFlushes"] = bufferStats.PendingFlushes;

                if (bufferStats.UtilizationPercentage > 90)
                {
                    status = HealthStatus.Degraded;
                    issues.Add($"Buffer utilization high: {bufferStats.UtilizationPercentage:F1}%");
                }

                if (bufferStats.PendingFlushes > 10)
                {
                    status = HealthStatus.Degraded;
                    issues.Add($"High pending flushes: {bufferStats.PendingFlushes}");
                }
            }

            // Check logging performance metrics
            if (_config.EnablePerformanceTracking)
            {
                var perfStats = await GetPerformanceStatisticsAsync();
                performanceData["AverageLogTime"] = perfStats.AverageLogTimeMs;
                performanceData["LogsPerSecond"] = perfStats.LogsPerSecond;
                performanceData["SlowLogCount"] = perfStats.SlowLogCount;

                if (perfStats.AverageLogTimeMs > 50) // 50ms threshold
                {
                    status = HealthStatus.Degraded;
                    issues.Add($"Slow logging performance: {perfStats.AverageLogTimeMs:F1}ms average");
                }

                if (perfStats.SlowLogCount > 100)
                {
                    status = HealthStatus.Degraded;
                    issues.Add($"High slow log count: {perfStats.SlowLogCount}");
                }
            }

            // Check memory usage
            var memoryUsage = GC.GetTotalMemory(false);
            performanceData["MemoryUsageBytes"] = memoryUsage;
            performanceData["MemoryUsageMB"] = memoryUsage / (1024.0 * 1024.0);

            var description = issues.Any() 
                ? $"Performance issues detected: {string.Join(", ", issues)}"
                : "Logging performance is healthy";

            return new ComponentHealthResult
            {
                ComponentName = "Performance",
                Status = status,
                Description = description,
                Data = performanceData
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthResult
            {
                ComponentName = "Performance",
                Status = HealthStatus.Unhealthy,
                Description = $"Performance check failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Checks the health of correlation tracking functionality.
    /// </summary>
    private async Task<ComponentHealthResult> CheckCorrelationTrackingHealthAsync(FixedString64Bytes correlationId)
    {
        try
        {
            if (!_config.EnableCorrelationTracking)
            {
                return new ComponentHealthResult
                {
                    ComponentName = "CorrelationTracking",
                    Status = HealthStatus.Healthy,
                    Description = "Correlation tracking is disabled",
                    Data = new Dictionary<string, object> { ["Enabled"] = false }
                };
            }

            // Test correlation ID generation and tracking
            var testCorrelationId = CorrelationId.Generate();
            var trackingData = new Dictionary<string, object>
            {
                ["TestCorrelationId"] = testCorrelationId.ToString(),
                ["ParentCorrelationId"] = correlationId.ToString(),
                ["TrackingEnabled"] = true
            };

            // Verify correlation context is working
            using var scope = _logger.BeginScope("CorrelationHealthTest", testCorrelationId);
            
            return new ComponentHealthResult
            {
                ComponentName = "CorrelationTracking",
                Status = HealthStatus.Healthy,
                Description = "Correlation tracking is functional",
                Data = trackingData
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthResult
            {
                ComponentName = "CorrelationTracking",
                Status = HealthStatus.Unhealthy,
                Description = $"Correlation tracking failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Checks the health of system integrations (alerts, health checks, profiler).
    /// </summary>
    private async Task<ComponentHealthResult> CheckSystemIntegrationHealthAsync(FixedString64Bytes correlationId)
    {
        var integrationData = new Dictionary<string, object>();
        var integrationIssues = new List<string>();
        var status = HealthStatus.Healthy;

        try
        {
            // Check alert service integration
            if (_alertService != null)
            {
                var alertsHealthy = await TestAlertServiceIntegrationAsync(correlationId);
                integrationData["AlertServiceAvailable"] = alertsHealthy;
                if (!alertsHealthy)
                {
                    integrationIssues.Add("Alert service integration failed");
                    status = HealthStatus.Degraded;
                }
            }
            else
            {
                integrationData["AlertServiceAvailable"] = false;
                integrationIssues.Add("Alert service not available");
            }

            // Check profiler integration
            if (_profiler != null)
            {
                var profilerHealthy = await TestProfilerServiceIntegrationAsync(correlationId);
                integrationData["ProfilerServiceAvailable"] = profilerHealthy;
                if (!profilerHealthy)
                {
                    integrationIssues.Add("Profiler service integration failed");
                    status = HealthStatus.Degraded;
                }
            }
            else
            {
                integrationData["ProfilerServiceAvailable"] = false;
            }

            var description = integrationIssues.Any()
                ? $"Integration issues: {string.Join(", ", integrationIssues)}"
                : "All system integrations are healthy";

            return new ComponentHealthResult
            {
                ComponentName = "SystemIntegration",
                Status = status,
                Description = description,
                Data = integrationData
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthResult
            {
                ComponentName = "SystemIntegration",
                Status = HealthStatus.Unhealthy,
                Description = $"System integration check failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    #region Private Helper Methods

    private HealthStatus DetermineOverallHealth(List<ComponentHealthResult> results)
    {
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;
        
        if (results.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;
        
        return HealthStatus.Healthy;
    }

    private string GetHealthDescription(HealthStatus status, List<ComponentHealthResult> results)
    {
        var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
        var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
        var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);

        return status switch
        {
            HealthStatus.Healthy => $"All {results.Count} components are healthy",
            HealthStatus.Degraded => $"System degraded: {degradedCount} degraded, {unhealthyCount} failed of {results.Count} components",
            HealthStatus.Unhealthy => $"System unhealthy: {unhealthyCount} failed, {degradedCount} degraded of {results.Count} components",
            _ => "Unknown health status"
        };
    }

    private Dictionary<string, object> CreateHealthData(List<ComponentHealthResult> results)
    {
        lock (_statisticsLock)
        {
            return new Dictionary<string, object>
            {
                ["TotalComponents"] = results.Count,
                ["HealthyComponents"] = results.Count(r => r.Status == HealthStatus.Healthy),
                ["DegradedComponents"] = results.Count(r => r.Status == HealthStatus.Degraded),
                ["UnhealthyComponents"] = results.Count(r => r.Status == HealthStatus.Unhealthy),
                ["LastSuccessfulCheck"] = _lastSuccessfulCheck,
                ["LastFailureTime"] = _lastFailureTime == DateTime.MinValue ? null : _lastFailureTime,
                ["TotalChecks"] = _statistics.TotalChecks,
                ["SuccessfulChecks"] = _statistics.SuccessfulChecks,
                ["FailedChecks"] = _statistics.FailedChecks,
                ["AverageCheckDuration"] = _statistics.AverageCheckDuration.TotalMilliseconds,
                ["ComponentDetails"] = results.ToDictionary(r => r.ComponentName, r => new
                {
                    Status = r.Status.ToString(),
                    Description = r.Description,
                    Duration = r.CheckDuration?.TotalMilliseconds,
                    Data = r.Data
                })
            };
        }
    }

    private void UpdateStatistics(HealthStatus status, TimeSpan duration)
    {
        lock (_statisticsLock)
        {
            _statistics.TotalChecks++;
            _statistics.TotalCheckDuration = _statistics.TotalCheckDuration.Add(duration);
            _statistics.AverageCheckDuration = TimeSpan.FromTicks(_statistics.TotalCheckDuration.Ticks / _statistics.TotalChecks);

            if (status == HealthStatus.Healthy)
            {
                _statistics.SuccessfulChecks++;
                _lastSuccessfulCheck = DateTime.UtcNow;
            }
            else
            {
                _statistics.FailedChecks++;
                _lastFailureTime = DateTime.UtcNow;
            }
        }
    }

    private async Task HandleHealthStatusChangeAsync(HealthStatus currentStatus, HealthCheckResult result, FixedString64Bytes correlationId)
    {
        if (_alertService == null) return;

        // Trigger alerts based on status changes and severity
        switch (currentStatus)
        {
            case HealthStatus.Unhealthy:
                await _alertService.RaiseAlert(
                    "LoggingSystemUnhealthy",
                    AlertSeverity.Critical,
                    $"Logging system is unhealthy: {result.Description}",
                    correlationId,
                    result.Data?.ToDictionary(x => (FixedString32Bytes)x.Key, x => x.Value) ?? new Dictionary<FixedString32Bytes, object>());
                break;

            case HealthStatus.Degraded:
                await _alertService.RaiseAlert(
                    "LoggingSystemDegraded",
                    AlertSeverity.Warning,
                    $"Logging system performance degraded: {result.Description}",
                    correlationId,
                    result.Data?.ToDictionary(x => (FixedString32Bytes)x.Key, x => x.Value) ?? new Dictionary<FixedString32Bytes, object>());
                break;
        }
    }

    // Additional helper methods for specific checks
    private async Task<BufferStatistics> GetBufferStatisticsAsync() => new BufferStatistics();
    private async Task<PerformanceStatistics> GetPerformanceStatisticsAsync() => new PerformanceStatistics();
    private async Task<DatabaseConnectionResult> TestDatabaseConnectionAsync(string connectionString) => new DatabaseConnectionResult();
    private async Task<bool> TestAlertServiceIntegrationAsync(FixedString64Bytes correlationId) => true;
    private async Task<bool> TestProfilerServiceIntegrationAsync(FixedString64Bytes correlationId) => true;

    private string GetTargetsHealthDescription(List<TargetHealthResult> results)
    {
        var healthy = results.Count(r => r.Status == HealthStatus.Healthy);
        var total = results.Count;
        return $"{healthy}/{total} targets healthy";
    }

    #endregion
}

/// <summary>
/// Health check statistics for the logging system.
/// </summary>
public class HealthCheckStatistics
{
    public int TotalChecks { get; set; }
    public int SuccessfulChecks { get; set; }
    public int FailedChecks { get; set; }
    public TimeSpan TotalCheckDuration { get; set; }
    public TimeSpan AverageCheckDuration { get; set; }
    
    public double SuccessRate => TotalChecks > 0 ? (double)SuccessfulChecks / TotalChecks : 0;
}

/// <summary>
/// Result of a target health check.
/// </summary>
public class TargetHealthResult
{
    public string TargetName { get; set; }
    public HealthStatus Status { get; set; }
    public string Description { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public Exception Exception { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Buffer statistics for monitoring.
/// </summary>
public class BufferStatistics
{
    public int TotalSize { get; set; } = 1000;
    public int UsedSize { get; set; } = 150;
    public int PendingFlushes { get; set; } = 2;
    public double UtilizationPercentage => TotalSize > 0 ? (double)UsedSize / TotalSize * 100 : 0;
}

/// <summary>
/// Performance statistics for monitoring.
/// </summary>
public class PerformanceStatistics
{
    public double AverageLogTimeMs { get; set; } = 5.2;
    public double LogsPerSecond { get; set; } = 1500;
    public int SlowLogCount { get; set; } = 15;
}

/// <summary>
/// Database connection test result.
/// </summary>
public class DatabaseConnectionResult
{
    public bool IsSuccessful { get; set; } = true;
    public string ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; } = TimeSpan.FromMilliseconds(25);
}
```
## 📚 Additional Resources

- [Log Target Development Guide](LOGGING_TARGETS.md)
- [Custom Formatter Creation](LOGGING_FORMATTERS.md)
- [Performance Optimization Guide](LOGGING_PERFORMANCE.md)
- [Correlation Tracking Guide](LOGGING_CORRELATION.md)
- [Integration Guide](LOGGING_INTEGRATION.md)
- [Troubleshooting Guide](LOGGING_TROUBLESHOOTING.md)
- [Security Best Practices](LOGGING_SECURITY.md)
- [Testing Strategies](LOGGING_TESTING.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the Logging System.

## 📄 Dependencies

- **Direct**: None (Foundation system)
- **Integration**: Reflex (Dependency Injection)
- **Optional**: HealthCheck (for health monitoring), Alerts (for critical event alerting), Profiling (for performance monitoring), Messaging (for distributed logging events), Pooling (for high-performance object management)
- **Dependents**: All systems require logging capabilities

---

*The Logging System serves as the foundational observability layer for all AhBearStudios Core systems, providing comprehensive logging, monitoring, and debugging capabilities.*