/com# Logging System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.Logging`
**Role:** High-Performance Centralized Logging Infrastructure
**Status:** ✅ Production Ready - Core Foundation System

The Logging System is a production-ready, high-performance Unity-optimized logging infrastructure designed for game development scenarios. It provides comprehensive centralized logging with multiple output targets, structured logging, correlation ID tracking, and Burst-compatible performance optimization. As the foundational system in the AhBearStudios Core Architecture, it serves all other systems with enterprise-grade observability, debugging support, and real-time monitoring capabilities.

## 🚀 Key Features

### 🎮 Unity Game Development Optimized
- **Frame Budget Conscious**: Designed for 60+ FPS with sub-millisecond logging operations
- **Unity Job System Compatible**: Burst-compatible logging with unmanaged data structures
- **Unity Collections v2**: Zero-allocation logging using FixedString64Bytes for correlation tracking
- **Unity Console Integration**: Deep integration with Unity Editor console and profiler

### 🏗️ Production-Ready Architecture
- **📝 Centralized Service**: Single `ILoggingService` interface for all logging operations
- **🎯 Multiple Targets**: Console, File, Memory, Serilog, Unity Console, Network, Database, Email
- **🔗 Correlation Tracking**: Full correlation ID support with both Guid and FixedString overloads
- **📊 Structured Logging**: Rich contextual data with key-value properties and typed parameters

### ⚡ High-Performance Features
- **Zero-Allocation Paths**: Object pooling and Unity.Collections v2 integration
- **Batching & Buffering**: Configurable batching for high-throughput scenarios
- **Async Operations**: UniTask-based async operations for non-blocking performance
- **Caching Systems**: Intelligent message formatting caching with configurable limits

### 🔧 Advanced Capabilities
- **🏥 Health Monitoring**: Built-in health checks with automatic alerting integration
- **📈 Performance Metrics**: Deep integration with IProfilerService for real-time monitoring
- **🚨 Alert Integration**: Automatic alerting for critical errors via IAlertService
- **🎛️ Advanced Filtering**: Rate limiting, sampling, pattern matching, and time-range filters
- **🔄 Channel Management**: Hierarchical channel system for subsystem organization

## 🏗️ Production Architecture

The logging system follows the established **Builder → Config → Factory → Service** pattern for maximum testability and flexibility.

### Core Package Structure (com.ahbearstudios.core)
```
Assets/com.ahbearstudios.core/Logging/
├── ILoggingService.cs                    # Primary service interface (at root)
├── LoggingService.cs                     # Main service implementation (at root)
├── Configs/
│   ├── LoggingConfig.cs                  # Main logging configuration record
│   ├── ILogTargetConfig.cs               # Target configuration interface
│   ├── LogTargetConfig.cs                # Target-specific settings
│   ├── LogChannelConfig.cs               # Channel configuration
│   ├── FilterConfig.cs                   # Filter configuration
│   └── FormatterConfig.cs                # Formatter configuration
├── Builders/
│   ├── ILogConfigBuilder.cs              # Main configuration builder interface
│   ├── LogConfigBuilder.cs               # Full builder implementation with scenarios
│   ├── ILogTargetConfigBuilder.cs        # Target configuration builder
│   ├── LogTargetConfigBuilder.cs         # Target builder implementation
│   ├── FilterConfigBuilder.cs            # Filter configuration builder
│   └── FormatterConfigBuilder.cs         # Formatter configuration builder
├── Factories/
│   ├── ILoggingServiceFactory.cs         # Service factory interface
│   ├── LoggingServiceFactory.cs          # Service factory implementation
│   ├── ILogTargetFactory.cs              # Target creation interface
│   ├── LogTargetFactory.cs               # Target factory implementation
│   ├── ILogFormatterFactory.cs           # Formatter factory interface
│   └── LogFormatterFactory.cs            # Formatter factory implementation
├── Services/
│   ├── ILogFormattingService.cs          # Formatting service interface
│   ├── LogFormattingService.cs           # Message formatting service
│   ├── ILogBatchingService.cs            # Batching service interface
│   ├── LogBatchingService.cs             # High-throughput batching
│   ├── ILogCorrelationService.cs         # Correlation service interface
│   ├── LogCorrelationService.cs          # Correlation ID management
│   ├── ILogChannelService.cs             # Channel service interface
│   ├── LogChannelService.cs              # Channel management service
│   ├── ILogTargetService.cs              # Target service interface
│   ├── LogTargetService.cs               # Target lifecycle service
│   ├── LogContextService.cs              # Context management
│   ├── LogFilterService.cs               # Filter management
│   └── LogBufferService.cs               # Buffer management
├── Targets/
│   ├── ILogTarget.cs                     # Log target interface
│   ├── ConsoleLogTarget.cs               # Standard console output
│   ├── FileLogTarget.cs                  # File-based logging with rotation
│   ├── MemoryLogTarget.cs                # In-memory circular buffer
│   ├── SerilogTarget.cs                  # Serilog enterprise integration
│   └── NullLogTarget.cs                  # Null target for testing
├── Formatters/
│   ├── ILogFormatter.cs                  # Formatter interface
│   ├── PlainTextFormatter.cs             # Human-readable text
│   ├── JsonFormatter.cs                  # JSON structured output
│   ├── StructuredFormatter.cs            # Key-value structured format
│   ├── BinaryFormatter.cs                # High-performance binary
│   ├── MessagePackFormatter.cs           # MessagePack serialization
│   ├── CefFormatter.cs                   # Common Event Format
│   ├── CsvFormatter.cs                   # CSV for data analysis
│   ├── GelfFormatter.cs                  # Graylog Extended Log Format
│   ├── KeyValueFormatter.cs              # Simple key=value format
│   ├── ProtobufFormatter.cs              # Protocol Buffers format
│   ├── SyslogFormatter.cs                # RFC 5424 Syslog format
│   └── XmlFormatter.cs                   # XML enterprise format
├── Filters/
│   ├── ILogFilter.cs                     # Filter interface
│   ├── LevelFilter.cs                    # Level-based filtering
│   ├── SourceFilter.cs                   # Source-based filtering
│   ├── CorrelationFilter.cs              # Correlation-based filtering
│   ├── PatternFilter.cs                  # Regex pattern filtering
│   ├── RateLimitFilter.cs                # Rate limiting to prevent spam
│   ├── SamplingFilter.cs                 # Statistical sampling
│   ├── TimeRangeFilter.cs                # Time-based filtering
│   ├── ILogFilterFactory.cs              # Filter factory interface
│   ├── LogFilterFactory.cs               # Filter factory implementation
│   ├── LogFilterService.cs               # Filter management
│   └── FilterStatistics.cs               # Filter performance tracking
├── Models/
│   ├── LogLevel.cs                       # Log level enumeration
│   ├── LogEntry.cs                       # Core log entry record
│   ├── LogMessage.cs                     # Message model for targets
│   ├── LogContext.cs                     # Logging context record
│   ├── ILogScope.cs                      # Scope interface
│   ├── LogScope.cs                       # Hierarchical scope implementation
│   ├── ILogChannel.cs                    # Channel interface
│   ├── LogChannel.cs                     # Channel implementation
│   ├── LogFormat.cs                      # Format enumeration
│   ├── LogTemplate.cs                    # Message templates
│   ├── LogTargetDefaults.cs              # Default configurations
│   ├── LoggingScenario.cs                # Scenario-specific configs
│   ├── PerformanceProfile.cs             # Performance profiling data
│   ├── LoggingStatistics.cs              # System statistics
│   ├── TargetStatistics.cs               # Target-specific stats
│   ├── FilterMode.cs                     # Filter mode enumeration
│   ├── LoggingSystemHealthStatus.cs      # Health status enumeration
│   ├── LogTargetErrorSeverity.cs         # Error severity levels
│   ├── LogChannelErrorSeverity.cs        # Channel error severity
│   └── LogConfigurationChangeType.cs     # Configuration change types
├── Messages/
│   ├── LoggingConfigurationChangedMessage.cs  # Configuration changes
│   ├── LoggingScopeCompletedMessage.cs         # Scope completion events
│   ├── LoggingSystemHealthMessage.cs           # System health messages
│   ├── LoggingTargetErrorMessage.cs            # Target error messages
│   ├── LoggingTargetRegisteredMessage.cs       # Target registration events
│   └── LoggingTargetUnregisteredMessage.cs     # Target removal events
└── HealthChecks/
    └── LoggingServiceHealthCheck.cs      # Comprehensive health monitoring
```

### Unity Package Structure (com.ahbearstudios.unity)
```
Assets/com.ahbearstudios.unity/Logging/
├── Installers/
│   └── LoggingInstaller.cs               # Production Reflex installer with Bootstrap lifecycle
├── Targets/
│   └── UnityConsoleLogTarget.cs          # Unity Editor console integration
├── ScriptableObjects/
│   ├── LoggingConfigurationAsset.cs      # Designer-friendly configuration
│   ├── LoggingScriptableObjectBase.cs    # Base ScriptableObject for logging
│   ├── LogTargetScriptableObject.cs      # Target configuration SO
│   ├── LogFormatterScriptableObject.cs   # Formatter configuration SO
│   ├── LogFilterScriptableObject.cs      # Filter configuration SO
│   ├── Targets/                          # Target-specific ScriptableObjects
│   ├── Formatters/                       # Formatter-specific ScriptableObjects
│   └── Filters/                          # Filter-specific ScriptableObjects
├── Editor/
│   ├── LoggingEditorWindow.cs            # Unity Editor logging window
│   └── LoggingConfigurationEditor.cs     # Custom Inspector for logging config
└── UnityLoggingBehaviour.cs              # Runtime MonoBehaviour integration
```
## 🔌 Key Interfaces

### ILoggingService

The primary interface for all logging operations with comprehensive correlation tracking and production-ready features.

```csharp
using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Common.Models;

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

    // Guid overloads for improved developer experience
    /// <summary>
    /// Logs a debug message with Guid correlation tracking.
    /// Convenience overload that accepts Guid directly instead of FixedString64Bytes.
    /// </summary>
    void LogDebug(string message, Guid correlationId, string sourceContext = null,
        IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an informational message with Guid correlation tracking.
    /// </summary>
    void LogInfo(string message, Guid correlationId, string sourceContext = null,
        IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a warning message with Guid correlation tracking.
    /// </summary>
    void LogWarning(string message, Guid correlationId, string sourceContext = null,
        IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an error message with Guid correlation tracking.
    /// </summary>
    void LogError(string message, Guid correlationId, string sourceContext = null,
        IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs a critical message with Guid correlation tracking and automatic alerting.
    /// </summary>
    void LogCritical(string message, Guid correlationId, string sourceContext = null,
        IReadOnlyDictionary<string, object> properties = null);

    // Unity Job System and Burst-compatible logging methods
    /// <summary>
    /// Logs a debug message with structured data for Burst compatibility.
    /// Designed for use within Unity Job System contexts.
    /// </summary>
    /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
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

    // Exception logging with dual correlation ID support
    /// <summary>
    /// Logs an exception with context and correlation tracking.
    /// </summary>
    void LogException(string message, Exception exception, FixedString64Bytes correlationId = default,
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    /// <summary>
    /// Logs an exception with context and Guid correlation tracking.
    /// Convenience overload that accepts Guid directly instead of FixedString64Bytes.
    /// </summary>
    void LogException(string message, Exception exception, Guid correlationId,
        string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

    // Advanced logging method
    /// <summary>
    /// Logs a message with the specified level and full context.
    /// </summary>
    void Log(LogLevel level, string message, FixedString64Bytes correlationId = default,
        string sourceContext = null, Exception exception = null,
        IReadOnlyDictionary<string, object> properties = null, string channel = null);

    // Hierarchical logging scopes
    /// <summary>
    /// Begins a logging scope for hierarchical context tracking.
    /// </summary>
    ILogScope BeginScope(string scopeName, FixedString64Bytes correlationId = default,
        string sourceContext = null);

    // Target management
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

    // Filter management
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

    // Channel management
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

    // Maintenance and monitoring
    /// <summary>
    /// Gets current logging statistics for monitoring.
    /// </summary>
    LoggingStatistics GetStatistics();

    /// <summary>
    /// Flushes all buffered log entries to targets (UniTask-based).
    /// </summary>
    UniTask FlushAsync(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Validates logging configuration and targets.
    /// </summary>
    ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Clears internal caches and performs maintenance.
    /// </summary>
    void PerformMaintenance(FixedString64Bytes correlationId = default);

    /// <summary>
    /// Performs a health check on the logging service and all registered targets.
    /// </summary>
    bool PerformHealthCheck();
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

## 📦 Production Installation

### Unity Reflex DI Integration

The logging system integrates seamlessly with Unity's Reflex dependency injection framework through a production-ready installer.

### LoggingInstaller - Production Reflex Integration

```csharp
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Unity.Logging.ScriptableObjects;
using Reflex.Core;

/// <summary>
/// Production-ready Reflex DI installer for the logging system.
/// Follows standard Reflex IInstaller pattern with enhanced Bootstrap lifecycle management.
///
/// Reflex Compliance:
/// - Implements IInstaller interface via IBootstrapInstaller inheritance
/// - Uses standard InstallBindings(ContainerBuilder) method for dependency registration
/// - Follows Reflex patterns for singleton/transient service registration
/// - Implements safe optional dependency resolution using container.HasBinding()
/// - Maintains proper service lifecycle management through Bootstrap phases
/// </summary>
[DefaultExecutionOrder(-1000)] // Execute early in bootstrap process
public class LoggingInstaller : BootstrapInstaller
{
    [Header("Configuration")]
    [SerializeField] private LoggingConfigurationAsset _configAsset;
    [SerializeField] private bool _createDefaultConfigIfMissing = true;

    [Header("Override Settings")]
    [SerializeField] private bool _overrideGlobalMinimumLevel = false;
    [SerializeField] private LogLevel _overrideMinimumLevel = LogLevel.Info;

    #region IBootstrapInstaller Implementation

    /// <inheritdoc />
    public override string InstallerName => "LoggingInstaller";

    /// <inheritdoc />
    public override int Priority => 50; // Very high priority - logging is foundational

    /// <inheritdoc />
    public override Type[] Dependencies => Array.Empty<Type>(); // No dependencies

    #endregion

    /// <summary>
    /// Core Reflex installer method that registers all logging system dependencies.
    /// Follows the standard Reflex pattern for dependency registration.
    /// </summary>
    public override void InstallBindings(ContainerBuilder builder)
    {
        try
        {
            LogDebug("Starting Reflex logging system installation");

            // Core Reflex registration pattern: Configuration → Factories → Services → Targets
            RegisterConfiguration(builder);
            RegisterFactories(builder);
            RegisterSupportingServices(builder);
            RegisterTargets(builder);
            RegisterMainService(builder);
            RegisterHealthChecks(builder);

            LogDebug("Reflex logging system installation completed successfully");
        }
        catch (Exception ex)
        {
            LogException(ex, "Failed to install logging system via Reflex");
            throw;
        }
    }

    /// <summary>
    /// Registers the main logging service with the container using Reflex factory pattern.
    /// </summary>
    private void RegisterMainService(ContainerBuilder builder)
    {
        // Register main service with proper dependency injection
        builder.AddSingleton<ILoggingService>(container =>
        {
            var config = container.Resolve<LoggingConfig>();
            var formattingService = container.Resolve<LogFormattingService>();

            // Resolve optional services using safe resolution patterns
            var batchingService = config.BatchingEnabled ? container.Resolve<LogBatchingService>() : null;
            var healthCheckService = TryResolveOptionalService<IHealthCheckService>(container, true);
            var alertService = TryResolveOptionalService<IAlertService>(container, true);
            var profilerService = TryResolveOptionalService<IProfilerService>(container, true);
            var messageBusService = TryResolveOptionalService<IMessageBusService>(container, true) ?? NullMessageBusService.Instance;

            return new LoggingService(
                config,
                targets: null, // Targets registered in PostInstall phase
                formattingService,
                batchingService,
                healthCheckService,
                alertService,
                profilerService,
                messageBusService);
        }, typeof(ILoggingService));

        LogDebug("Registered main logging service with Reflex factory pattern");
    }

    /// <summary>
    /// Post-installation configuration and integration.
    /// </summary>
    protected override void PerformPostInstall(Container container)
    {
        try
        {
            LogDebug("Starting post-installation setup");

            // Initialize and validate the logging service
            var loggingService = container.Resolve<ILoggingService>();

            // Register health check with the health check service (if available)
            if (container.HasBinding(typeof(IHealthCheckService)))
            {
                RegisterWithHealthCheckService(container, loggingService);
            }

            // Perform initial health check
            ValidateServiceHealth(loggingService);

            // Register targets from configuration
            RegisterConfiguredTargets(container, loggingService);

            // Configure performance monitoring (if available)
            if (container.HasBinding(typeof(IProfilerService)))
            {
                ConfigurePerformanceMonitoring(container);
            }

            // Log successful initialization
            loggingService.LogInfo("Logging system successfully initialized and validated", "Bootstrap", "LoggingInstaller");

            var targetCount = loggingService.GetTargets().Count;
            LogDebug($"Post-installation complete. {targetCount} targets registered");
        }
        catch (Exception ex)
        {
            LogException(ex, "Post-installation failed");
            throw;
        }
}
```

### ScriptableObject Configuration

The Unity package provides designer-friendly ScriptableObject-based configuration.

```csharp
[CreateAssetMenu(fileName = "LoggingConfig", menuName = "AhBearStudios/Logging Configuration")]
public class LoggingConfigurationAsset : ScriptableObject
{
    [Header("Global Settings")]
    public LogLevel GlobalMinimumLevel = LogLevel.Info;
    public bool IsLoggingEnabled = true;

    [Header("Performance")]
    public bool HighPerformanceMode = true;
    public bool BurstCompatibility = true;
    public int MaxQueueSize = 1000;

    [Header("Targets")]
    public bool EnableUnityConsole = true;
    public bool EnableFileLogging = true;
    public string LogFilePath = "Logs/game.log";

    [Header("Channels")]
    public LogChannelConfig[] ChannelConfigurations = new[]
    {
        new LogChannelConfig { Name = "Gameplay", MinimumLevel = LogLevel.Debug },
        new LogChannelConfig { Name = "Networking", MinimumLevel = LogLevel.Info },
        new LogChannelConfig { Name = "UI", MinimumLevel = LogLevel.Warning }
    };

    public LoggingConfig ToRuntimeConfig()
    {
        return new LoggingConfig
        {
            GlobalMinimumLevel = GlobalMinimumLevel,
            IsLoggingEnabled = IsLoggingEnabled,
            HighPerformanceMode = HighPerformanceMode,
            BurstCompatibility = BurstCompatibility,
            MaxQueueSize = MaxQueueSize,
            // Convert Unity-specific settings to runtime config
        };
    }
}
```

## 🎮 Unity Integration Examples

### Basic Game Development Setup

```csharp
public class GameManager : MonoBehaviour
{
    private ILoggingService _loggingService;

    [Inject]
    public void Initialize(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    private void Start()
    {
        // Game development optimized logging with correlation tracking
        using var gameStartScope = _loggingService.BeginScope("GameStart", "GameManager");
        gameStartScope.LogInfo("Game initialization started");

        try
        {
            InitializeGameSystems();
            gameStartScope.LogInfo("Game systems initialized successfully");
        }
        catch (Exception ex)
        {
            gameStartScope.LogException("Game initialization failed", ex);
            throw;
        }
    }

    private void Update()
    {
        // Frame budget conscious logging - only log critical information
        if (Time.frameCount % 600 == 0) // Every 10 seconds at 60 FPS
        {
            _loggingService.LogDebug($"Game running - Frame: {Time.frameCount}, FPS: {1.0f / Time.unscaledDeltaTime:F1}",
                sourceContext: "GameManager");
        }
    }
}
```

### Unity Job System Integration

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct PhysicsCalculationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [WriteOnly] public NativeArray<float3> velocities;
    [ReadOnly] public FixedString64Bytes correlationId;

    public void Execute(int index)
    {
        // Burst-compatible high-performance calculations
        var velocity = CalculateVelocity(positions[index]);
        velocities[index] = velocity;

        // Burst-compatible logging for critical events
        if (math.length(velocity) > 100f)
        {
            // Use structured data logging for Burst compatibility
            var logData = new BurstLogData
            {
                EntityIndex = index,
                VelocityMagnitude = math.length(velocity),
                Position = positions[index]
            };

            // Log will be processed on main thread
            _loggingService.LogWarning<BurstLogData>(
                "High velocity detected in physics calculation",
                logData,
                correlationId);
        }
    }
}
```

### Unity Profiler Integration

```csharp
public class PerformanceManager : MonoBehaviour
{
    private ILoggingService _loggingService;
    private IProfilerService _profilerService;

    [Inject]
    public void Initialize(ILoggingService loggingService, IProfilerService profilerService)
    {
        _loggingService = loggingService;
        _profilerService = profilerService;
    }

    private void Update()
    {
        using var scope = _profilerService.BeginScope("GameLoop.Update");

        // Game logic with integrated performance monitoring
        ProcessGameLogic();

        // Automatic logging integration with profiler
        if (scope.ElapsedMilliseconds > 16.67f) // Frame budget exceeded
        {
            _loggingService.LogWarning(
                $"Frame budget exceeded: {scope.ElapsedMilliseconds:F2}ms",
                sourceContext: "PerformanceManager");
        }
    }
}
```

## 📊 Production Monitoring

### Health Check Integration

```csharp
// Health check is automatically registered with the health check service
var healthResult = _loggingService.PerformHealthCheck();
if (!healthResult)
{
    // Automatic alerting is triggered for critical logging failures
    _loggingService.LogCritical("Logging system health check failed - investigating targets",
        "HealthMonitor", "LoggingSystem");
}
```

### Performance Statistics

```csharp
// Get comprehensive logging statistics for monitoring
var stats = _loggingService.GetStatistics();

_loggingService.LogInfo($"Logging Performance Report:", "MonitoringService", "PerformanceReporter",
    properties: new Dictionary<string, object>
    {
        ["TotalMessages"] = stats.TotalMessages,
        ["MessagesPerSecond"] = stats.MessagesPerSecond,
        ["AverageProcessingTime"] = stats.AverageProcessingTime,
        ["BufferUtilization"] = stats.BufferUtilization,
        ["FailedTargets"] = stats.FailedTargetCount,
        ["ActiveFilters"] = stats.ActiveFilterCount
    });
```

## 🚀 Quick Start Guide

### 1. Install and Configure

1. Add the LoggingInstaller to your bootstrap sequence
2. Create a LoggingConfigurationAsset via **Create > AhBearStudios > Logging Configuration**
3. Configure targets, channels, and performance settings in the Inspector

### 2. Basic Usage

```csharp
// Inject the service
[Inject] private ILoggingService _logger;

// Basic logging with automatic correlation
_logger.LogInfo("Player spawned", "GameManager");

// Structured logging with context
_logger.LogWarning("Low health detected", "CombatSystem",
    properties: new Dictionary<string, object>
    {
        ["PlayerId"] = playerId,
        ["CurrentHealth"] = health,
        ["MaxHealth"] = maxHealth
    });

// Exception logging with full context
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    _logger.LogException("Risky operation failed", ex, "SystemName");
}
```

### 3. Advanced Scenarios

```csharp
// Use builders for complex configurations
var config = new LogConfigBuilder()
    .ForDevelopment()                    // Pre-configured development scenario
    .WithChannel("MyGame", LogLevel.Debug)
    .WithFileTarget("GameLog", "logs/game.log")
    .Build();

// Hierarchical scopes for complex operations
using var gameScope = _logger.BeginScope("GameSession", "GameManager");
gameScope.SetProperty("SessionId", sessionId);

using var levelScope = gameScope.BeginChild("LoadLevel");
levelScope.LogInfo("Loading level assets");
// All logs in this scope include session and level context
```

## 📋 Summary

The AhBearStudios Core Logging System is a production-ready, high-performance logging infrastructure specifically designed for Unity game development. It provides:

- **Unity-Optimized Performance**: Frame budget conscious with Burst compatibility
- **Production-Ready Features**: Comprehensive health monitoring, alerting, and statistics
- **Developer Experience**: Intuitive APIs with both Guid and FixedString64Bytes correlation support
- **Flexible Configuration**: Scenario-based builders and designer-friendly ScriptableObjects
- **Enterprise Integration**: Serilog, database, network, and email target support
- **Advanced Filtering**: Rate limiting, sampling, and pattern-based filtering
- **Reflex DI Integration**: Full dependency injection support with proper lifecycle management

The system serves as the foundational logging infrastructure for all other AhBearStudios Core systems, providing comprehensive observability and debugging capabilities for Unity game development at scale.
