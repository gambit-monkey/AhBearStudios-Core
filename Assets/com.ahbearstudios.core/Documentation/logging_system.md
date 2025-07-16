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
/// <param name="Name">Target identifier</param>
/// <param name="TargetType">Implementation type for the target</param>
/// <param name="MinimumLevel">Minimum log level for this target</param>
/// <param name="Enabled">Whether the target is enabled</param>
/// <param name="Settings">Target-specific configuration</param>
public record TargetConfig(
    FixedString64Bytes Name,
    Type TargetType,
    LogLevel MinimumLevel = LogLevel.Information,
    bool Enabled = true,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
)
{
    public TargetConfig() : this("Default", typeof(ConsoleLogTarget)) { }
}

/// <summary>
/// Configuration for log filtering.
/// </summary>
/// <param name="Name">Filter identifier</param>
/// <param name="FilterType">Implementation type for the filter</param>
/// <param name="Enabled">Whether the filter is enabled</param>
/// <param name="Settings">Filter-specific configuration</param>
public record FilterConfig(
    FixedString64Bytes Name,
    Type FilterType,
    bool Enabled = true,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
)
{
    public FilterConfig() : this("Default", typeof(LevelFilter)) { }
}

/// <summary>
/// Configuration for log formatters.
/// </summary>
/// <param name="Name">Formatter identifier</param>
/// <param name="FormatterType">Implementation type for the formatter</param>
/// <param name="Settings">Formatter-specific configuration</param>
public record FormatterConfig(
    FixedString64Bytes Name,
    Type FormatterType,
    IReadOnlyDictionary<FixedString32Bytes, object> Settings = null
)
{
    public FormatterConfig() : this("Default", typeof(PlainTextFormatter)) { }
}
```

### LoggingConfigBuilder

Fluent builder pattern for configuration creation.

```csharp
/// <summary>
/// Builder interface for creating logging configurations with fluent syntax.
/// </summary>
public interface ILoggingConfigBuilder
{
    /// <summary>
    /// Sets the minimum log level.
    /// </summary>
    /// <param name="level">Minimum log level to process</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithMinimumLevel(LogLevel level);

    /// <summary>
    /// Configures log buffering.
    /// </summary>
    /// <param name="enabled">Whether buffering is enabled</param>
    /// <param name="bufferSize">Size of the buffer</param>
    /// <param name="flushInterval">Automatic flush interval</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder WithBuffering(bool enabled = true, int bufferSize = 1000, 
        TimeSpan flushInterval = default);

    /// <summary>
    /// Adds a console log target.
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
    /// <param name="name">Target name</param>
    /// <param name="targetType">Target implementation type</param>
    /// <param name="minimumLevel">Minimum log level</param>
    /// <param name="settings">Target-specific settings</param>
    /// <returns>Builder instance for chaining</returns>
    ILoggingConfigBuilder AddCustomTarget(FixedString64Bytes name, Type targetType, 
        LogLevel minimumLevel = LogLevel.Information,
        IReadOnlyDictionary<FixedString32Bytes, object> settings = null);

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
}
```

### Configuration Usage Examples

```csharp
// Basic configuration with default settings
var basicConfig = new LoggingConfigBuilder()
    .WithMinimumLevel(LogLevel.Information)
    .AddConsoleTarget()
    .AddFileTarget("logs/app.log")
    .WithCorrelationTracking()
    .Build();

// Advanced configuration with custom targets and filtering
var advancedConfig = new LoggingConfigBuilder()
    .WithMinimumLevel(LogLevel.Debug)
    .WithBuffering(enabled: true, bufferSize: 5000, TimeSpan.FromSeconds(10))
    .AddConsoleTarget(LogLevel.Information, typeof(ColoredConsoleFormatter))
    .AddFileTarget("logs/detailed.log", LogLevel.Debug, maxFileSize: 50_000_000)
    .AddDatabaseTarget("Server=localhost;Database=Logs", "AppLogs", LogLevel.Error)
    .AddFilter("SensitiveDataFilter", typeof(SensitiveDataFilter))
    .WithStructuredLogging(true)
    .WithPerformanceTracking(true)
    .WithDefaultChannel("Application")
    .Build();

// Production configuration with external services
var productionConfig = new LoggingConfigBuilder()
    .WithMinimumLevel(LogLevel.Warning)
    .WithBuffering(enabled: true, bufferSize: 10000, TimeSpan.FromMinutes(1))
    .AddFileTarget("logs/production.log", LogLevel.Warning)
    .AddCustomTarget("ElasticSearch", typeof(ElasticSearchTarget), LogLevel.Error,
        new Dictionary<FixedString32Bytes, object>
        {
            ["Endpoint"] = "https://elastic.company.com",
            ["Index"] = "application-logs",
            ["ApiKey"] = Environment.GetEnvironmentVariable("ELASTIC_API_KEY")
        })
    .AddFilter("PerformanceFilter", typeof(PerformanceFilter))
    .WithCorrelationTracking(true)
    .WithStructuredLogging(true)
    .WithPerformanceTracking(true)
    .Build();
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