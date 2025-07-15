# Logging System

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
│   ├── TargetConfig.cs                   # Target-specific settings record
│   ├── FilterConfig.cs                   # Filtering configuration record
│   └── FormatterConfig.cs               # Output formatter configuration
├── Builders/
│   ├── ILoggingConfigBuilder.cs          # Configuration builder interface
│   └── LoggingConfigBuilder.cs           # Builder implementation
├── Factories/
│   ├── ILogTargetFactory.cs              # Target creation interface
│   ├── LogTargetFactory.cs               # Target factory implementation
│   └── FormatterFactory.cs               # Formatter factory implementation
├── Services/
│   ├── LogContextService.cs              # Context management service
│   ├── LogFilterService.cs               # Log filtering service
│   ├── LogBufferService.cs               # Buffering and batching service
│   └── LogCorrelationService.cs          # Correlation ID management
├── Targets/
│   ├── ILogTarget.cs                     # Log target interface
│   ├── ConsoleLogTarget.cs               # Console output target
│   ├── FileLogTarget.cs                  # File output target
│   ├── DatabaseLogTarget.cs              # Database output target
│   ├── RemoteLogTarget.cs                # Remote service target
│   └── UnityLogTarget.cs                 # Unity console integration
├── Formatters/
│   ├── ILogFormatter.cs                  # Log formatter interface
│   ├── JsonLogFormatter.cs               # JSON output formatter
│   ├── PlainTextFormatter.cs             # Plain text formatter
│   └── StructuredFormatter.cs            # Structured data formatter
├── Models/
│   ├── LogEntry.cs                       # Log entry record
│   ├── LogContext.cs                     # Logging context record
│   ├── LogLevel.cs                       # Log level enumeration
│   ├── LogStatistics.cs                  # Logging statistics record
│   └── CorrelationInfo.cs                # Correlation tracking record
├── Filters/
│   ├── ILogFilter.cs                     # Log filter interface
│   ├── LevelFilter.cs                    # Level-based filtering
│   ├── SourceFilter.cs                   # Source-based filtering
│   └── CorrelationFilter.cs              # Correlation-based filtering
└── HealthChecks/
    └── LoggingServiceHealthCheck.cs      # Health monitoring

AhBearStudios.Unity.Logging/
├── Installers/
│   └── LoggingInstaller.cs               # Reflex bootstrap installer
├── Targets/
│   ├── UnityConsoleTarget.cs             # Unity console output
│   └── UnityFileTarget.cs                # Unity persistent file logging
└── ScriptableObjects/
    └── LoggingConfigAsset.cs             # Unity configuration asset
```
## 🔌 Key Interfaces

### ILoggingService

The primary interface for all logging operations with comprehensive correlation tracking.

```csharp
using Unity.Collections;
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Alerts;

/// <summary>
/// Primary logging service interface providing centralized logging
/// with correlation tracking and comprehensive system integration.
/// </summary>
public interface ILoggingService : IDisposable
{
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
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogInfo(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a warning message with correlation tracking.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogWarning(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an error message with correlation tracking.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogError(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a critical message with correlation tracking and automatic alerting.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogCritical(string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an exception with context and correlation tracking.
    /// </summary>
    /// <param name="message">Context message for the exception</param>
    /// <param name="exception">The exception to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context (typically class name)</param>
    /// <param name="properties">Additional structured properties</param>
    void LogException(string message, Exception exception, FixedString64Bytes correlationId = default, 
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a message with the specified level and full context.
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context</param>
    /// <param name="exception">Associated exception (optional)</param>
    /// <param name="properties">Structured properties</param>
    /// <param name="channel">Specific channel for the log</param>
    void Log(LogLevel level, string message, FixedString64Bytes correlationId = default, 
        string sourceContext = null, Exception exception = null, 
        IReadOnlyDictionary<string, object> properties = null, string channel = null);

    /// <summary>
    /// Begins a logging scope for hierarchical context tracking.
    /// </summary>
    /// <param name="scopeName">Name of the scope</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="sourceContext">Source context</param>
    /// <returns>Disposable logging scope</returns>
    ILoggingScope BeginScope(string scopeName, FixedString64Bytes correlationId = default, 
        string sourceContext = null);

    /// <summary>
    /// Registers a log target with the service.
    /// </summary>
    /// <param name="target">The log target to register</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Unregisters a log target from the service.
    /// </summary>
    /// <param name="targetName">Name of the target to unregister</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>True if target was unregistered</returns>
    bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets all registered log targets.
    /// </summary>
    /// <returns>Collection of registered targets</returns>
    IReadOnlyCollection<ILogTarget> GetTargets();

    /// <summary>
    /// Sets the minimum log level for filtering.
    /// </summary>
    /// <param name="level">Minimum log level</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    void SetMinimumLevel(LogLevel level, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Adds a log filter for advanced filtering.
    /// </summary>
    /// <param name="filter">Log filter to add</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    void AddFilter(ILogFilter filter, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Removes a log filter.
    /// </summary>
    /// <param name="filterName">Name of filter to remove</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>True if filter was removed</returns>
    bool RemoveFilter(string filterName, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets current logging statistics for monitoring.
    /// </summary>
    /// <returns>Current logging statistics</returns>
    LoggingStatistics GetStatistics();

    /// <summary>
    /// Flushes all buffered log entries to targets.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Task representing the flush operation</returns>
    Task FlushAsync(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates logging configuration and targets.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Clears internal caches and performs maintenance.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    void PerformMaintenance(FixedString64Bytes correlationId = default);
}
```

### ILogTarget

Interface for log output targets with correlation support.

```csharp
/// <summary>
/// Interface for log output targets with correlation tracking and performance monitoring.
/// </summary>
public interface ILogTarget : IDisposable
{
    /// <summary>
    /// Target name for identification.
    /// </summary>
    FixedString64Bytes Name { get; }

    /// <summary>
    /// Whether the target is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Minimum log level for this target.
    /// </summary>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Target-specific configuration.
    /// </summary>
    TargetConfig Configuration { get; }

    /// <summary>
    /// Log formatter for this target.
    /// </summary>
    ILogFormatter Formatter { get; set; }

    /// <summary>
    /// Writes a log entry to the target with correlation tracking.
    /// </summary>
    /// <param name="entry">Log entry to write</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Task representing the write operation</returns>
    Task WriteAsync(LogEntry entry, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Writes multiple log entries in batch for performance.
    /// </summary>
    /// <param name="entries">Log entries to write</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Task representing the batch write operation</returns>
    Task WriteBatchAsync(IReadOnlyCollection<LogEntry> entries, FixedString64Bytes correlationId = default);

    /// <summary>
    /// Flushes any buffered entries to the target.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Task representing the flush operation</returns>
    Task FlushAsync(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates the target configuration and connectivity.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Gets target statistics for monitoring.
    /// </summary>
    /// <returns>Target performance statistics</returns>
    TargetStatistics GetStatistics();

    /// <summary>
    /// Event raised when an error occurs in the target.
    /// </summary>
    event Action<ILogTarget, Exception, FixedString64Bytes> ErrorOccurred;
}
```
### ILoggingScope

Interface for hierarchical logging scopes.

```csharp
/// <summary>
/// Interface for hierarchical logging scopes with correlation tracking.
/// </summary>
public interface ILoggingScope : IDisposable
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
    ILoggingScope Parent { get; }

    /// <summary>
    /// Child scopes created within this scope.
    /// </summary>
    IReadOnlyCollection<ILoggingScope> Children { get; }

    /// <summary>
    /// Creates a child scope within this scope.
    /// </summary>
    /// <param name="childName">Name of the child scope</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Child logging scope</returns>
    ILoggingScope BeginChild(string childName, FixedString64Bytes correlationId = default);

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

    /// <summary>
    /// Event raised when the scope completes.
    /// </summary>
    event Action<ILoggingScope> ScopeCompleted;
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

Modern C# configuration using records with FixedString types.

```csharp
/// <summary>
/// Configuration record for logging system with modern C# patterns.
/// </summary>
/// <param name="MinimumLevel">Minimum log level to process</param>
/// <param name="EnableBuffering">Whether to enable log buffering</param>
/// <param name="BufferSize">Size of the log buffer</param>
/// <param name="FlushInterval">Interval for automatic buffer flushing</param>
/// <param name="EnableCorrelationTracking">Whether to enable correlation ID tracking</param>
/// <param name="EnableStructuredLogging">Whether to enable structured logging</param>
/// <param name="EnablePerformanceTracking">Whether to track logging performance</param>
/// <param name="EnableHealthChecks">Whether to enable health monitoring</param>
/// <param name="DefaultChannel">Default channel for log entries</param>
/// <param name="TargetConfigs">Configuration for log targets</param>
/// <param name="FilterConfigs">Configuration for log filters</param>
/// <param name="FormatterConfigs">Configuration for log formatters</param>
public record LoggingConfig(
    LogLevel MinimumLevel = LogLevel.Information,
    bool EnableBuffering = true,
    int BufferSize = 1000,
    TimeSpan FlushInterval = default,
    bool EnableCorrelationTracking = true,
    bool EnableStructuredLogging = true,
    bool EnablePerformanceTracking = true,
    bool EnableHealthChecks = true,
    FixedString32Bytes DefaultChannel = default,
    IReadOnlyCollection<TargetConfig> TargetConfigs = null,
    IReadOnlyCollection<FilterConfig> FilterConfigs = null,
    IReadOnlyDictionary<FixedString32Bytes, FormatterConfig> FormatterConfigs = null
)
{
    public static LoggingConfig Default => new();
    
    public LoggingConfig() : this(
        FlushInterval: TimeSpan.FromSeconds(30),
        DefaultChannel: "Default",
        TargetConfigs: CreateDefaultTargets(),
        FilterConfigs: Array.Empty<FilterConfig>(),
        FormatterConfigs: CreateDefaultFormatters()
    ) { }

    private static IReadOnlyCollection<TargetConfig> CreateDefaultTargets()
    {
        return new[]
        {
            new TargetConfig("Console", typeof(ConsoleLogTarget), LogLevel.Information, enabled: true),
            new TargetConfig("File", typeof(FileLogTarget), LogLevel.Warning, enabled: true,
                settings: new Dictionary<FixedString32Bytes, object>
                {
                    ["FilePath"] = "logs/application.log",
                    ["MaxFileSize"] = 10 * 1024 * 1024, // 10MB
                    ["MaxFiles"] = 10
                })
        };
    }

    private static IReadOnlyDictionary<FixedString32Bytes, FormatterConfig> CreateDefaultFormatters()
    {
        return new Dictionary<FixedString32Bytes, FormatterConfig>
        {
            ["Console"] = new FormatterConfig("PlainText", typeof(PlainTextFormatter)),
            ["File"] = new FormatterConfig("Json", typeof(JsonLogFormatter)),
            ["Database"] = new FormatterConfig("Structured", typeof(StructuredFormatter))
        };
    }
}

/// <summary>
/// Configuration for log targets.
/// </summary>
/// <param name="Name">Target name</param>
/// <param name="TargetType">Type of log target</param>
/// <param name="MinimumLevel">Minimum log level for this target</param>
/// <param name="Enabled">Whether target is enabled</param>
/// <param name="BufferSize">Buffer size for this target</param>
/// <param name="Settings">Target-specific settings</param>
public record TargetConfig(
    FixedString64Bytes Name,
    Type TargetType,
    LogLevel MinimumLevel = LogLevel.Information,
    bool Enabled = true,
    int BufferSize = 100,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);

/// <summary>
/// Configuration for log formatters.
/// </summary>
/// <param name="Name">Formatter name</param>
/// <param name="FormatterType">Type of formatter</param>
/// <param name="Settings">Formatter-specific settings</param>
public record FormatterConfig(
    FixedString64Bytes Name,
    Type FormatterType,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);

/// <summary>
/// Configuration for log filters.
/// </summary>
/// <param name="Name">Filter name</param>
/// <param name="FilterType">Type of filter</param>
/// <param name="Enabled">Whether filter is enabled</param>
/// <param name="Settings">Filter-specific settings</param>
public record FilterConfig(
    FixedString64Bytes Name,
    Type FilterType,
    bool Enabled = true,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);
```
## ⚙️ Configuration

### LoggingConfig Record

Modern C# configuration using records with FixedString types.

```csharp
/// <summary>
/// Configuration record for logging system with modern C# patterns.
/// </summary>
/// <param name="MinimumLevel">Minimum log level to process</param>
/// <param name="EnableBuffering">Whether to enable log buffering</param>
/// <param name="BufferSize">Size of the log buffer</param>
/// <param name="FlushInterval">Interval for automatic buffer flushing</param>
/// <param name="EnableCorrelationTracking">Whether to enable correlation ID tracking</param>
/// <param name="EnableStructuredLogging">Whether to enable structured logging</param>
/// <param name="EnablePerformanceTracking">Whether to track logging performance</param>
/// <param name="EnableHealthChecks">Whether to enable health monitoring</param>
/// <param name="DefaultChannel">Default channel for log entries</param>
/// <param name="TargetConfigs">Configuration for log targets</param>
/// <param name="FilterConfigs">Configuration for log filters</param>
/// <param name="FormatterConfigs">Configuration for log formatters</param>
public record LoggingConfig(
    LogLevel MinimumLevel = LogLevel.Information,
    bool EnableBuffering = true,
    int BufferSize = 1000,
    TimeSpan FlushInterval = default,
    bool EnableCorrelationTracking = true,
    bool EnableStructuredLogging = true,
    bool EnablePerformanceTracking = true,
    bool EnableHealthChecks = true,
    FixedString32Bytes DefaultChannel = default,
    IReadOnlyCollection<TargetConfig> TargetConfigs = null,
    IReadOnlyCollection<FilterConfig> FilterConfigs = null,
    IReadOnlyDictionary<FixedString32Bytes, FormatterConfig> FormatterConfigs = null
)
{
    public static LoggingConfig Default => new();
    
    public LoggingConfig() : this(
        FlushInterval: TimeSpan.FromSeconds(30),
        DefaultChannel: "Default",
        TargetConfigs: CreateDefaultTargets(),
        FilterConfigs: Array.Empty<FilterConfig>(),
        FormatterConfigs: CreateDefaultFormatters()
    ) { }

    private static IReadOnlyCollection<TargetConfig> CreateDefaultTargets()
    {
        return new[]
        {
            new TargetConfig("Console", typeof(ConsoleLogTarget), LogLevel.Information, enabled: true),
            new TargetConfig("File", typeof(FileLogTarget), LogLevel.Warning, enabled: true,
                settings: new Dictionary<FixedString32Bytes, object>
                {
                    ["FilePath"] = "logs/application.log",
                    ["MaxFileSize"] = 10 * 1024 * 1024, // 10MB
                    ["MaxFiles"] = 10
                })
        };
    }

    private static IReadOnlyDictionary<FixedString32Bytes, FormatterConfig> CreateDefaultFormatters()
    {
        return new Dictionary<FixedString32Bytes, FormatterConfig>
        {
            ["Console"] = new FormatterConfig("PlainText", typeof(PlainTextFormatter)),
            ["File"] = new FormatterConfig("Json", typeof(JsonLogFormatter)),
            ["Database"] = new FormatterConfig("Structured", typeof(StructuredFormatter))
        };
    }
}

/// <summary>
/// Configuration for log targets.
/// </summary>
/// <param name="Name">Target name</param>
/// <param name="TargetType">Type of log target</param>
/// <param name="MinimumLevel">Minimum log level for this target</param>
/// <param name="Enabled">Whether target is enabled</param>
/// <param name="BufferSize">Buffer size for this target</param>
/// <param name="Settings">Target-specific settings</param>
public record TargetConfig(
    FixedString64Bytes Name,
    Type TargetType,
    LogLevel MinimumLevel = LogLevel.Information,
    bool Enabled = true,
    int BufferSize = 100,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);

/// <summary>
/// Configuration for log formatters.
/// </summary>
/// <param name="Name">Formatter name</param>
/// <param name="FormatterType">Type of formatter</param>
/// <param name="Settings">Formatter-specific settings</param>
public record FormatterConfig(
    FixedString64Bytes Name,
    Type FormatterType,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);

/// <summary>
/// Configuration for log filters.
/// </summary>
/// <param name="Name">Filter name</param>
/// <param name="FilterType">Type of filter</param>
/// <param name="Enabled">Whether filter is enabled</param>
/// <param name="Settings">Filter-specific settings</param>
public record FilterConfig(
    FixedString64Bytes Name,
    Type FilterType,
    bool Enabled = true,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
);
```

### ILoggingConfigBuilder

Builder interface for fluent configuration.

```csharp
/// <summary>
/// Builder interface for creating logging configurations with fluent API.
/// </summary>
public interface ILoggingConfigBuilder
{
    /// <summary>
    /// Sets the minimum log level.
    /// </summary>
    /// <param name="level">Minimum log level</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithMinimumLevel(LogLevel level);

    /// <summary>
    /// Configures log buffering.
    /// </summary>
    /// <param name="enabled">Whether buffering is enabled</param>
    /// <param name="bufferSize">Buffer size</param>
    /// <param name="flushInterval">Flush interval</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithBuffering(bool enabled = true, int bufferSize = 1000, 
        TimeSpan flushInterval = default);

    /// <summary>
    /// Enables or disables correlation tracking.
    /// </summary>
    /// <param name="enabled">Whether correlation tracking is enabled</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithCorrelationTracking(bool enabled = true);

    /// <summary>
    /// Enables or disables structured logging.
    /// </summary>
    /// <param name="enabled">Whether structured logging is enabled</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithStructuredLogging(bool enabled = true);

    /// <summary>
    /// Configures performance tracking.
    /// </summary>
    /// <param name="enabled">Whether performance tracking is enabled</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithPerformanceTracking(bool enabled = true);

    /// <summary>
    /// Adds a log filter.
    /// </summary>
    /// <param name="filterName">Filter name</param>
    /// <param name="filterType">Type of filter</param>
    /// <param name="settings">Filter settings</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddFilter(FixedString64Bytes filterName, Type filterType,
        IReadOnlyDictionary<FixedString32Bytes, object> settings = null);

    /// <summary>
    /// Sets the default channel for log entries.
    /// </summary>
    /// <param name="channel">Default channel name</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithDefaultChannel(FixedString32Bytes channel);

    /// <summary>
    /// Builds the final configuration.
    /// </summary>
    /// <returns>Immutable logging configuration</returns>
    LoggingConfig Build();
} a console log target.
    /// </summary>
    /// <param name="minimumLevel">Minimum level for console output</param>
    /// <param name="formatter">Formatter type for console output</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddConsoleTarget(LogLevel minimumLevel = LogLevel.Information, 
        Type formatter = null);

    /// <summary>
    /// Adds a file log target.
    /// </summary>
    /// <param name="filePath">Path to log file</param>
    /// <param name="minimumLevel">Minimum level for file output</param>
    /// <param name="maxFileSize">Maximum file size before rotation</param>
    /// <param name="maxFiles">Maximum number of files to keep</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddFileTarget(string filePath, LogLevel minimumLevel = LogLevel.Warning, 
        int maxFileSize = 10485760, int maxFiles = 10);

    /// <summary>
    /// Adds a database log target.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="tableName">Table name for log entries</param>
    /// <param name="minimumLevel">Minimum level for database output</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddDatabaseTarget(string connectionString, string tableName = "LogEntries", 
        LogLevel minimumLevel = LogLevel.Error);

    /// <summary>
    /// Adds a custom log target.
    /// </summary>
    /// <typeparam name="T">Type of custom target</typeparam>
    /// <param name="name">Target name</param>
    /// <param name="minimumLevel">Minimum level for target</param>
    /// <param name="settings">Target-specific settings</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddCustomTarget<T>(FixedString64Bytes name, LogLevel minimumLevel = LogLevel.Information,
        IReadOnlyDictionary<FixedString32Bytes, object> settings = null) where T : class, ILogTarget;

    /// <summary>
    /// Adds

## 📦 Installation

### LoggingInstaller

Complete IBootstrapInstaller implementation with Reflex integration.

```csharp
using Reflex.Core;
using Unity.Collections;
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Alerts;

/// <summary>
/// Bootstrap installer for the Logging System as the foundational core system.
/// </summary>
public class LoggingInstaller : IBootstrapInstaller
{
    private readonly LoggingConfig _config;
    private FixedString64Bytes _correlationId;

    public LoggingInstaller(LoggingConfig config = null)
    {
        _config = config ?? LoggingConfig.Default;
        _correlationId = CorrelationIdGenerator.Generate("LoggingInstaller");
    }

    public FixedString32Bytes InstallerName => "LoggingInstaller";
    public int Priority => 100; // Highest priority as foundation system
    public bool IsEnabled => true;
    public Type[] Dependencies => Array.Empty<Type>(); // No dependencies as foundation

    public bool ValidateInstaller()
    {
        try
        {
            // Validate configuration
            if (_config.BufferSize <= 0)
            {
                Console.WriteLine($"[{_correlationId}] Invalid buffer size: {_config.BufferSize}. Must be positive");
                return false;
            }

            if (_config.FlushInterval <= TimeSpan.Zero)
            {
                Console.WriteLine($"[{_correlationId}] Invalid flush interval. Must be positive");
                return false;
            }

            // Validate target configurations
            foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
            {
                if (!typeof(ILogTarget).IsAssignableFrom(targetConfig.TargetType))
                {
                    Console.WriteLine($"[{_correlationId}] Invalid target type: {targetConfig.TargetType.Name}");
                    return false;
                }
            }

            Console.WriteLine($"[{_correlationId}] Logging installer validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_correlationId}] Logging installer validation failed: {ex.Message}");
            return false;
        }
    }

    public void PreInstall()
    {
        Console.WriteLine($"[{_correlationId}] Starting Logging System pre-installation");
        
        // Ensure log directories exist for file targets
        foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (targetConfig.TargetType == typeof(FileLogTarget) && 
                targetConfig.Settings?.TryGetValue("FilePath", out var filePathObj) == true &&
                filePathObj is string filePath)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[{_correlationId}] Created log directory: {directory}");
                }
            }
        }

        Console.WriteLine($"[{_correlationId}] Logging System pre-installation completed");
    }

    public void Install(ContainerBuilder builder)
    {
        Console.WriteLine($"[{_correlationId}] Installing Logging System components");

        // Register configuration
        builder.Bind<LoggingConfig>().FromInstance(_config);

        // Register builders
        builder.Bind<ILoggingConfigBuilder>().To<LoggingConfigBuilder>().AsTransient();

        // Register factories
        builder.Bind<ILogTargetFactory>().To<LogTargetFactory>().AsSingle();
        builder.Bind<FormatterFactory>().To<FormatterFactory>().AsSingle();

        // Register core service
        builder.Bind<ILoggingService>().To<LoggingService>().AsSingle();

        // Register support services
        builder.Bind<LogContextService>().To<LogContextService>().AsSingle();
        builder.Bind<LogFilterService>().To<LogFilterService>().AsSingle();
        builder.Bind<LogBufferService>().To<LogBufferService>().AsSingle();
        builder.Bind<LogCorrelationService>().To<LogCorrelationService>().AsSingle();

        // Register log targets
        RegisterLogTargets(builder);

        // Register formatters
        RegisterFormatters(builder);

        // Register filters
        RegisterFilters(builder);

        // Register health check
        builder.Bind<LoggingServiceHealthCheck>().To<LoggingServiceHealthCheck>().AsSingle();

        Console.WriteLine($"[{_correlationId}] Logging System components installed successfully");
    }

    public void PostInstall()
    {
        try
        {
            Console.WriteLine($"[{_correlationId}] Starting Logging System post-installation");

            // Initialize logging service
            var loggingService = Container.Resolve<ILoggingService>();
            
            // Register configured targets
            var factory = Container.Resolve<ILogTargetFactory>();
            foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
            {
                if (targetConfig.Enabled)
                {
                    var target = factory.CreateTarget(targetConfig, _correlationId);
                    loggingService.RegisterTarget(target, _correlationId);
                }
            }

            // Log system startup
            loggingService.LogInfo("Logging System initialized successfully", 
                correlationId: _correlationId, sourceContext: "LoggingInstaller");

            // Register health check if health service is available (optional dependency)
            if (Container.HasBinding<IHealthCheckService>())
            {
                var healthService = Container.Resolve<IHealthCheckService>();
                var healthCheck = Container.Resolve<LoggingServiceHealthCheck>();
                healthService.RegisterHealthCheck(healthCheck, _correlationId);
                
                loggingService.LogInfo("Logging System health check registered", 
                    correlationId: _correlationId, sourceContext: "LoggingInstaller");
            }

            Console.WriteLine($"[{_correlationId}] Logging System post-installation completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_correlationId}] Logging System post-installation failed: {ex.Message}");
            
            // Try to raise alert if alert service is available
            if (Container.HasBinding<IAlertService>())
            {
                var alertService = Container.Resolve<IAlertService>();
                alertService?.RaiseAlert(
                    "LoggingSystemInitializationFailed",
                    AlertSeverity.Critical,
                    "Logging system failed to initialize properly",
                    _correlationId);
            }
            throw;
        }
    }

    private void RegisterLogTargets(ContainerBuilder builder)
    {
        // Register default target types
        builder.Bind<ILogTarget>().To<ConsoleLogTarget>().AsTransient().WithId("Console");
        builder.Bind<ILogTarget>().To<FileLogTarget>().AsTransient().WithId("File");
        builder.Bind<ILogTarget>().To<DatabaseLogTarget>().AsTransient().WithId("Database");
        builder.Bind<ILogTarget>().To<RemoteLogTarget>().AsTransient().WithId("Remote");

        // Register configured custom targets
        foreach (var targetConfig in _config.TargetConfigs ?? Enumerable.Empty<TargetConfig>())
        {
            if (targetConfig.TargetType != typeof(ConsoleLogTarget) &&
                targetConfig.TargetType != typeof(FileLogTarget) &&
                targetConfig.TargetType != typeof(DatabaseLogTarget) &&
                targetConfig.TargetType != typeof(RemoteLogTarget))
            {
                builder.Bind<ILogTarget>().To(targetConfig.TargetType)
                    .AsTransient().WithId(targetConfig.Name.ToString());
            }
        }
    }

    private void RegisterFormatters(ContainerBuilder builder)
    {
        // Register default formatters
        builder.Bind<ILogFormatter>().To<JsonLogFormatter>().AsTransient().WithId("Json");
        builder.Bind<ILogFormatter>().To<PlainTextFormatter>().AsTransient().WithId("PlainText");
        builder.Bind<ILogFormatter>().To<StructuredFormatter>().AsTransient().WithId("Structured");

        // Register configured custom formatters
        foreach (var formatterConfig in _config.FormatterConfigs ?? new Dictionary<FixedString32Bytes, FormatterConfig>())
        {
            builder.Bind<ILogFormatter>().To(formatterConfig.Value.FormatterType)
                .AsTransient().WithId(formatterConfig.Key.ToString());
        }
    }

    private void RegisterFilters(ContainerBuilder builder)
    {
        // Register default filters
        builder.Bind<ILogFilter>().To<LevelFilter>().AsTransient().WithId("Level");
        builder.Bind<ILogFilter>().To<SourceFilter>().AsTransient().WithId("Source");
        builder.Bind<ILogFilter>().To<CorrelationFilter>().AsTransient().WithId("Correlation");
    }
}

/// <summary>
/// Extension methods for clean service registration.
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Adds the Logging System to the container builder.
    /// </summary>
    /// <param name="builder">Container builder</param>
    /// <param name="config">Optional logging configuration</param>
    /// <returns>Container builder for chaining</returns>
    public static ContainerBuilder AddLoggingSystem(this ContainerBuilder builder, 
        LoggingConfig config = null)
    {
        builder.Install(new LoggingInstaller(config));
        return builder;
    }
}
```
## 🚀 Usage Examples

### Basic Logging with Correlation Tracking

```csharp
/// <summary>
/// Service demonstrating basic logging usage with correlation tracking.
/// </summary>
public class UserService
{
    private readonly ILoggingService _logger;
    private readonly FixedString64Bytes _serviceCorrelationId;

    public UserService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceCorrelationId = CorrelationIdGenerator.Generate("UserService");
        
        _logger.LogInfo("UserService initialized", correlationId: _serviceCorrelationId, 
            sourceContext: nameof(UserService));
    }

    public async Task<User> CreateUserAsync(string username, string email)
    {
        var operationId = CorrelationIdGenerator.Generate("CreateUser");
        
        using var scope = _logger.BeginScope("CreateUser", operationId, nameof(UserService));
        scope.SetProperty("Username", username);
        scope.SetProperty("Email", email);
        
        try
        {
            _logger.LogInfo($"Creating user: {username}", correlationId: operationId, 
                sourceContext: nameof(UserService), properties: new Dictionary<string, object>
                {
                    ["Username"] = username,
                    ["Email"] = email,
                    ["OperationType"] = "UserCreation"
                });

            // Validate input
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("User creation failed: username is required", 
                    correlationId: operationId, sourceContext: nameof(UserService));
                throw new ArgumentException("Username is required", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                _logger.LogWarning($"User creation failed: invalid email format: {email}", 
                    correlationId: operationId, sourceContext: nameof(UserService));
                throw new ArgumentException("Valid email is required", nameof(email));
            }

            // Check if user exists
            var existingUser = await FindUserByUsernameAsync(username, operationId);
            if (existingUser != null)
            {
                _logger.LogWarning($"User creation failed: username already exists: {username}", 
                    correlationId: operationId, sourceContext: nameof(UserService));
                throw new InvalidOperationException("Username already exists");
            }

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            await SaveUserAsync(user, operationId);

            _logger.LogInfo($"User created successfully: {user.Id}", correlationId: operationId, 
                sourceContext: nameof(UserService), properties: new Dictionary<string, object>
                {
                    ["UserId"] = user.Id,
                    ["Username"] = username,
                    ["CreationTime"] = scope.Elapsed.TotalMilliseconds
                });

            scope.SetProperty("UserId", user.Id.ToString());
            scope.SetProperty("Success", true);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogException("Failed to create user", ex, correlationId: operationId, 
                sourceContext: nameof(UserService), properties: new Dictionary<string, object>
                {
                    ["Username"] = username,
                    ["Email"] = email,
                    ["ErrorType"] = ex.GetType().Name
                });

            scope.SetProperty("Success", false);
            scope.SetProperty("ErrorType", ex.GetType().Name);
            throw;
        }
    }

    private async Task<User> FindUserByUsernameAsync(string username, FixedString64Bytes correlationId)
    {
        using var scope = _logger.BeginScope("FindUserByUsername", correlationId, nameof(UserService));
        scope.SetProperty("Username", username);
        
        _logger.LogDebug($"Searching for user by username: {username}", 
            correlationId: correlationId, sourceContext: nameof(UserService));
        
        // Simulate database lookup
        await Task.Delay(50);
        
        // Return null for demo (user not found)
        scope.SetProperty("UserFound", false);
        return null;
    }

    private async Task SaveUserAsync(User user, FixedString64Bytes correlationId)
    {
        using var scope = _logger.BeginScope("SaveUser", correlationId, nameof(UserService));
        scope.SetProperty("UserId", user.Id.ToString());
        
        _logger.LogDebug($"Saving user to database: {user.Username}", 
            correlationId: correlationId, sourceContext: nameof(UserService));
        
        try
        {
            // Simulate database save
            await Task.Delay(100);
            
            _logger.LogDebug($"User saved successfully: {user.Id}", 
                correlationId: correlationId, sourceContext: nameof(UserService));
            
            scope.SetProperty("SaveSuccessful", true);
        }
        catch (Exception ex)
        {
            _logger.LogException("Failed to save user to database", ex, 
                correlationId: correlationId, sourceContext: nameof(UserService));
            
            scope.SetProperty("SaveSuccessful", false);
            throw;
        }
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains('@') && email.Contains('.'); // Simplified validation
    }
}

/// <summary>
/// Simple user model for demonstration.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Advanced Structured Logging with Multiple Targets

```csharp
/// <summary>
/// Service demonstrating advanced logging patterns with multiple targets and structured data.
/// </summary>
public class OrderProcessingService
{
    private readonly ILoggingService _logger;
    private readonly FixedString64Bytes _serviceCorrelationId;

    public OrderProcessingService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceCorrelationId = CorrelationIdGenerator.Generate("OrderProcessingService");
        
        // Configure service-specific logging
        ConfigureServiceLogging();
        
        _logger.LogInfo("OrderProcessingService initialized with advanced logging", 
            correlationId: _serviceCorrelationId, sourceContext: nameof(OrderProcessingService),
            properties: new Dictionary<string, object>
            {
                ["ServiceVersion"] = "2.1.0",
                ["LoggingMode"] = "Advanced",
                ["InitializationTime"] = DateTime.UtcNow
            });
    }

    private void ConfigureServiceLogging()
    {
        // Add order-specific log filter
        var orderFilter = new SourceFilter("OrderProcessing");
        _logger.AddFilter(orderFilter, _serviceCorrelationId);
        
        _logger.LogDebug("Service-specific logging configured", 
            correlationId: _serviceCorrelationId, sourceContext: nameof(OrderProcessingService));
    }

    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        var operationId = CorrelationIdGenerator.Generate("ProcessOrder");
        
        using var scope = _logger.BeginScope("ProcessOrder", operationId, nameof(OrderProcessingService));
        
        // Set rich contextual properties
        scope.SetProperty("OrderId", order.Id.ToString());
        scope.SetProperty("CustomerId", order.CustomerId.ToString());
        scope.SetProperty("OrderValue", order.TotalAmount);
        scope.SetProperty("ItemCount", order.Items.Count);
        scope.SetProperty("OrderType", order.Type.ToString());
        scope.SetProperty("Priority", order.Priority.ToString());
        
        try
        {
            _logger.LogInfo("Starting order processing", correlationId: operationId, 
                sourceContext: nameof(OrderProcessingService), properties: new Dictionary<string, object>
                {
                    ["OrderId"] = order.Id,
                    ["CustomerId"] = order.CustomerId,
                    ["OrderValue"] = order.TotalAmount,
                    ["ItemCount"] = order.Items.Count,
                    ["ProcessingStartTime"] = DateTime.UtcNow,
                    ["OrderType"] = order.Type.ToString(),
                    ["Priority"] = order.Priority.ToString()
                });

            // Validation phase
            var validationResult = await ValidateOrderAsync(order, operationId);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Order validation failed", correlationId: operationId, 
                    sourceContext: nameof(OrderProcessingService), properties: new Dictionary<string, object>
                    {
                        ["OrderId"] = order.Id,
                        ["ValidationErrors"] = validationResult.Errors,
                        ["ValidationTime"] = validationResult.Duration.TotalMilliseconds
                    });

                scope.SetProperty("ValidationResult", "Failed");
                scope.SetProperty("ValidationErrors", string.Join(", ", validationResult.Errors));
                
                return OrderResult.Failed(order.Id, "Validation failed", validationResult.Errors);
            }

            // Payment processing phase
            var paymentResult = await ProcessPaymentAsync(order, operationId);
            if (!paymentResult.IsSuccessful)
            {
                _logger.LogError("Payment processing failed", correlationId: operationId, 
                    sourceContext: nameof(OrderProcessingService), properties: new Dictionary<string, object>
                    {
                        ["OrderId"] = order.Id,
                        ["PaymentMethod"] = order.PaymentMethod,
                        ["PaymentAmount"] = order.TotalAmount,
                        ["PaymentError"] = paymentResult.ErrorMessage,
                        ["PaymentTransactionId"] = paymentResult.TransactionId
                    });

                scope.SetProperty("PaymentResult", "Failed");
                scope.SetProperty("PaymentError", paymentResult.ErrorMessage);
                
                return OrderResult.Failed(order.Id, "Payment failed", new[] { paymentResult.ErrorMessage });
            }

            // Fulfillment phase
            var fulfillmentResult = await CreateFulfillmentAsync(order, operationId);
            
            _logger.LogInfo("Order processed successfully", correlationId: operationId, 
                sourceContext: nameof(OrderProcessingService), properties: new Dictionary<string, object>
                {
                    ["OrderId"] = order.Id,
                    ["PaymentTransactionId"] = paymentResult.TransactionId,
                    ["FulfillmentId"] = fulfillmentResult.FulfillmentId,
                    ["EstimatedDelivery"] = fulfillmentResult.EstimatedDelivery,
                    ["TotalProcessingTime"] = scope.Elapsed.TotalMilliseconds,
                    ["ProcessingEndTime"] = DateTime.UtcNow
                });

            return OrderResult.Success(order.Id, paymentResult.TransactionId, fulfillmentResult.FulfillmentId);
        }
        catch (Exception ex)
        {
            _logger.LogException("Critical error during order processing", ex, 
                correlationId: operationId, sourceContext: nameof(OrderProcessingService), 
                properties: new Dictionary<string, object>
                {
                    ["OrderId"] = order.Id,
                    ["CustomerId"] = order.CustomerId,
                    ["OrderValue"] = order.TotalAmount,
                    ["ProcessingTime"] = scope.Elapsed.TotalMilliseconds,
                    ["ErrorType"] = ex.GetType().Name
                });
            
            throw;
        }
    }

    private async Task<ValidationResult> ValidateOrderAsync(Order order, FixedString64Bytes correlationId)
    {
        using var scope = _logger.BeginScope("ValidateOrder", correlationId, nameof(OrderProcessingService));
        
        var errors = new List<string>();
        var startTime = DateTime.UtcNow;
        
        _logger.LogDebug("Starting order validation", correlationId: correlationId, 
            sourceContext: nameof(OrderProcessingService));

        // Simulate validation logic
        await Task.Delay(50);
        
        if (order.Items == null || !order.Items.Any())
        {
            errors.Add("Order must contain at least one item");
        }
        
        if (order.TotalAmount <= 0)
        {
            errors.Add("Order total must be greater than zero");
        }
        
        if (order.CustomerId == Guid.Empty)
        {
            errors.Add("Valid customer ID is required");
        }

        var duration = DateTime.UtcNow - startTime;
        var isValid = !errors.Any();
        
        scope.SetProperty("ValidationResult", isValid ? "Success" : "Failed");
        scope.SetProperty("ErrorCount", errors.Count);

        return new ValidationResult(isValid, errors, duration);
    }

    private async Task<PaymentResult> ProcessPaymentAsync(Order order, FixedString64Bytes correlationId)
    {
        using var scope = _logger.BeginScope("ProcessPayment", correlationId, nameof(OrderProcessingService));
        
        var startTime = DateTime.UtcNow;
        
        _logger.LogInfo("Processing payment", correlationId: correlationId, 
            sourceContext: nameof(OrderProcessingService), properties: new Dictionary<string, object>
            {
                ["OrderId"] = order.Id,
                ["PaymentMethod"] = order.PaymentMethod,
                ["Amount"] = order.TotalAmount,
                ["Currency"] = "USD"
            });

        // Simulate payment processing
        await Task.Delay(200);
        
        var duration = DateTime.UtcNow - startTime;
        var transactionId = $"TXN_{Guid.NewGuid():N}";
        
        scope.SetProperty("PaymentResult", "Success");
        scope.SetProperty("TransactionId", transactionId);
        
        return new PaymentResult(true, transactionId, null, duration);
    }

    private async Task<FulfillmentResult> CreateFulfillmentAsync(Order order, FixedString64Bytes correlationId)
    {
        using var scope = _logger.BeginScope("CreateFulfillment", correlationId, nameof(OrderProcessingService));
        
        _logger.LogDebug("Creating order fulfillment", correlationId: correlationId, 
            sourceContext: nameof(OrderProcessingService));

        // Simulate fulfillment creation
        await Task.Delay(100);
        
        var fulfillmentId = Guid.NewGuid();
        var estimatedDelivery = DateTime.UtcNow.AddDays(3);
        
        scope.SetProperty("FulfillmentId", fulfillmentId.ToString());
        scope.SetProperty("EstimatedDelivery", estimatedDelivery);
        
        return new FulfillmentResult(fulfillmentId, estimatedDelivery);
    }
}

// Supporting types for the example
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public string PaymentMethod { get; set; }
    public OrderType Type { get; set; }
    public OrderPriority Priority { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderType { Standard, Express, Bulk }
public enum OrderPriority { Low, Normal, High, Critical }

public record ValidationResult(bool IsValid, List<string> Errors, TimeSpan Duration);
public record PaymentResult(bool IsSuccessful, string TransactionId, string ErrorMessage, TimeSpan Duration);
public record FulfillmentResult(Guid FulfillmentId, DateTime EstimatedDelivery);

public class OrderResult
{
    public bool IsSuccessful { get; set; }
    public Guid OrderId { get; set; }
    public string TransactionId { get; set; }
    public Guid? FulfillmentId { get; set; }
    public string ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();

    public static OrderResult Success(Guid orderId, string transactionId, Guid fulfillmentId)
    {
        return new OrderResult
        {
            IsSuccessful = true,
            OrderId = orderId,
            TransactionId = transactionId,
            FulfillmentId = fulfillmentId
        };
    }

    public static OrderResult Failed(Guid orderId, string errorMessage, IEnumerable<string> validationErrors)
    {
        return new OrderResult
        {
            IsSuccessful = false,
            OrderId = orderId,
            ErrorMessage = errorMessage,
            ValidationErrors = validationErrors.ToList()
        };
    }
}
```
## 🏥 Health Monitoring

### LoggingServiceHealthCheck

Complete health check implementation for the foundational logging system.

```csharp
using Unity.Collections;
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Alerts;

/// <summary>
/// Health check implementation for the Logging System as a foundational service.
/// </summary>
public class LoggingServiceHealthCheck : IHealthCheck
{
    private readonly ILoggingService _loggingService;
    private readonly IAlertService _alerts;
    private readonly LoggingConfig _config;

    public LoggingServiceHealthCheck(ILoggingService loggingService, 
        IAlertService alerts, LoggingConfig config)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _alerts = alerts; // Optional dependency
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public FixedString32Bytes Name => "LoggingSystemHealth";
    public FixedString64Bytes Description => "Monitors logging system health and performance as foundational service";
    public TimeSpan CheckInterval => TimeSpan.FromMinutes(5);

    public async Task<HealthCheckResult> CheckHealthAsync(FixedString64Bytes correlationId = default)
    {
        try
        {
            var checkCorrelationId = correlationId.IsEmpty ? 
                CorrelationIdGenerator.Generate("LoggingHealthCheck") : correlationId;

            _loggingService.LogDebug("Starting logging system health check", 
                correlationId: checkCorrelationId, sourceContext: nameof(LoggingServiceHealthCheck));

            var results = new List<HealthCheckResult>();
            var statistics = _loggingService.GetStatistics();

            // Check service health
            var serviceHealth = CheckServiceHealth(statistics, checkCorrelationId);
            results.Add(serviceHealth);

            // Check target health
            var targetHealth = CheckTargetHealth(checkCorrelationId);
            results.Add(targetHealth);

            // Check buffer health
            var bufferHealth = CheckBufferHealth(statistics, checkCorrelationId);
            results.Add(bufferHealth);

            