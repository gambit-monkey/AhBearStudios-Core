# Logging Interfaces

## Overview

The AhBearStudios.Core Logging Interfaces provide the foundation for a robust, configurable logging system designed for high-performance environments, including those leveraging Unity's Burst compiler. These interfaces define contracts for creating, formatting, intercepting, processing, and outputting log messages through a flexible pipeline architecture.

The system is designed with composability in mind, allowing developers to easily customize logging behavior through interchangeable components following the principle of separation of concerns.

## Key Interfaces

The logging system is built around seven core interfaces:

1. **IBurstLogger**: A Burst-compatible logging interface optimized for performance in Unity environments
2. **ILogFormatter**: Transforms log entries into formatted output strings
3. **ILoggerConfig**: Provides configuration options for logger behavior
4. **ILoggerFactory**: Creates and configures logger instances
5. **ILogInterceptor**: Intercepts log messages before they enter the processing pipeline
6. **ILogMiddleware**: Processes and potentially transforms log events in a middleware pipeline
7. **ILogTarget**: Represents the destination for log messages after processing

## Interface Relationships

```
┌───────────────────┐     creates    ┌───────────────┐
│  ILoggerFactory   │─────────────────▶  IBurstLogger │
└────────┬──────────┘                └───────┬───────┘
         │                                   │
         │ configures                        │ uses
         ▼                                   ▼
┌────────────────────┐              ┌────────────────────┐
│   ILoggerConfig    │              │   ILogInterceptor  │
└────────────────────┘              └─────────┬──────────┘
                                              │
                                              │ passes to
                                              ▼
┌────────────────────┐  formats    ┌────────────────────┐
│   ILogFormatter    │◀────────────│   ILogMiddleware   │
└────────┬───────────┘             └─────────┬──────────┘
         │                                   │
         │ outputs to                        │ outputs to
         ▼                                   ▼
┌────────────────────┐              ┌────────────────────┐
│     ILogTarget     │◀─────────────┤     ILogTarget     │
└────────────────────┘              └────────────────────┘
```

## Detailed Interface Descriptions

### IBurstLogger

The `IBurstLogger` interface defines a high-performance logging API that is compatible with Unity's Burst compiler, allowing for efficient logging in performance-critical code.

```csharp
public interface IBurstLogger
{
    void Log(LogType logType, string message);
    void LogFormat(LogType logType, string format, params object[] args);
    bool IsLogTypeAllowed(LogType logType);
}
```

#### Key Capabilities:
- Burst-compatible logging methods
- Support for formatted log messages
- Runtime filtering of log messages by type

### ILogFormatter

The `ILogFormatter` interface is responsible for transforming log data into formatted strings suitable for output.

```csharp
public interface ILogFormatter
{
    string Format(LogType logType, string message, object context);
    string FormatException(Exception exception);
}
```

#### Key Capabilities:
- Format log messages with appropriate decoration and context
- Special handling for exception formatting

### ILoggerConfig

The `ILoggerConfig` interface provides configuration options for controlling logger behavior.

```csharp
public interface ILoggerConfig
{
    bool IncludeTimestamps { get; set; }
    bool IncludeStackTrace { get; set; }
    LogLevel MinimumLogLevel { get; set; }
    Dictionary<string, LogLevel> CategoryLogLevels { get; }
    
    bool IsLogLevelAllowed(LogLevel logLevel, string category = null);
}
```

#### Key Capabilities:
- Control inclusion of timestamps and stack traces
- Set minimum log level thresholds globally and per category
- Check if specific log levels are allowed

### ILoggerFactory

The `ILoggerFactory` interface is responsible for creating and configuring logger instances.

```csharp
public interface ILoggerFactory
{
    IBurstLogger CreateLogger(string category = null);
    ILoggerFactory AddTarget(ILogTarget target);
    ILoggerFactory AddMiddleware(ILogMiddleware middleware);
    ILoggerFactory AddInterceptor(ILogInterceptor interceptor);
    ILoggerConfig Config { get; }
}
```

#### Key Capabilities:
- Create logger instances for specific categories
- Add log targets, middleware, and interceptors
- Access logger configuration

### ILogInterceptor

The `ILogInterceptor` interface defines a contract for components that can intercept log messages before they enter the middleware pipeline.

```csharp
public interface ILogInterceptor
{
    bool Intercept(ref LogType logType, ref string message, ref object context);
}
```

#### Key Capabilities:
- Inspect and potentially modify log data
- Control whether the log message continues through the pipeline

### ILogMiddleware

The `ILogMiddleware` interface defines a contract for components that process log messages in a middleware pipeline.

```csharp
public interface ILogMiddleware
{
    void Process(LogType logType, string message, object context, ILogTarget target);
}
```

#### Key Capabilities:
- Process log data before it reaches a target
- Apply transformations, enrichment, or filtering
- Forward logs to the appropriate target

### ILogTarget

The `ILogTarget` interface represents a destination for log messages after they have been processed.

```csharp
public interface ILogTarget
{
    void Write(LogType logType, string message, object context = null);
    void Flush();
}
```

#### Key Capabilities:
- Write log messages to a specific destination
- Flush any buffered log messages

## Integration with the Logging System

### Creating and Configuring Loggers

```csharp
// Create a logger factory
var loggerFactory = new LoggerFactory();

// Configure the factory
loggerFactory.Config.MinimumLogLevel = LogLevel.Info;
loggerFactory.Config.IncludeTimestamps = true;
loggerFactory.Config.IncludeStackTrace = false;

// Set category-specific log levels
loggerFactory.Config.CategoryLogLevels["Network"] = LogLevel.Debug;
loggerFactory.Config.CategoryLogLevels["AI"] = LogLevel.Warning;

// Add targets, middleware, and interceptors
loggerFactory
    .AddTarget(new ConsoleLogTarget())
    .AddTarget(new FileLogTarget("app.log"))
    .AddMiddleware(new TimestampMiddleware())
    .AddInterceptor(new SensitiveDataInterceptor());

// Create a logger
var logger = loggerFactory.CreateLogger("GameLogic");
var networkLogger = loggerFactory.CreateLogger("Network");
```

### Basic Logging

```csharp
// Log messages at different levels
logger.Log(LogType.Log, "Game initialized");
logger.Log(LogType.Warning, "Low memory detected");
logger.Log(LogType.Error, "Failed to load level data");

// Using format strings
logger.LogFormat(LogType.Log, "Player {0} connected from {1}", "Player1", "192.168.1.100");

// Check if log type is allowed before expensive operations
if (logger.IsLogTypeAllowed(LogType.Log)) {
    var detailedStats = GenerateDetailedStats(); // Expensive operation
    logger.Log(LogType.Log, "Detailed stats: " + detailedStats);
}
```

### Custom Log Formatter

```csharp
public class JsonLogFormatter : ILogFormatter
{
    public string Format(LogType logType, string message, object context)
    {
        var logObject = new {
            timestamp = DateTime.UtcNow.ToString("o"),
            type = logType.ToString(),
            message = message,
            context = context
        };
        
        return JsonUtility.ToJson(logObject);
    }
    
    public string FormatException(Exception exception)
    {
        var exceptionObject = new {
            type = exception.GetType().Name,
            message = exception.Message,
            stackTrace = exception.StackTrace
        };
        
        return JsonUtility.ToJson(exceptionObject);
    }
}
```

### Custom Log Target for Unity

```csharp
public class UnityDebugLogTarget : ILogTarget
{
    private readonly ILogFormatter _formatter;
    
    public UnityDebugLogTarget(ILogFormatter formatter = null)
    {
        _formatter = formatter ?? new DefaultLogFormatter();
    }
    
    public void Write(LogType logType, string message, object context = null)
    {
        string formattedMessage = _formatter.Format(logType, message, context);
        
        switch (logType)
        {
            case LogType.Log:
                UnityEngine.Debug.Log(formattedMessage);
                break;
            case LogType.Warning:
                UnityEngine.Debug.LogWarning(formattedMessage);
                break;
            case LogType.Error:
            case LogType.Exception:
                UnityEngine.Debug.LogError(formattedMessage);
                break;
            case LogType.Assert:
                UnityEngine.Debug.LogAssertion(formattedMessage);
                break;
        }
    }
    
    public void Flush()
    {
        // Unity Debug immediately displays logs, no need to flush
    }
}
```

### Custom Log Middleware

```csharp
public class EnrichmentMiddleware : ILogMiddleware
{
    private readonly string _applicationVersion;
    private readonly string _deviceId;
    
    public EnrichmentMiddleware(string applicationVersion, string deviceId)
    {
        _applicationVersion = applicationVersion;
        _deviceId = deviceId;
    }
    
    public void Process(LogType logType, string message, object context, ILogTarget target)
    {
        // Create enriched context
        var enrichedContext = new Dictionary<string, object>();
        
        // Add existing context if available
        if (context != null)
        {
            if (context is IDictionary<string, object> contextDict)
            {
                foreach (var kvp in contextDict)
                {
                    enrichedContext[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Add the entire context object if it's not a dictionary
                enrichedContext["originalContext"] = context;
            }
        }
        
        // Add environment information
        enrichedContext["appVersion"] = _applicationVersion;
        enrichedContext["deviceId"] = _deviceId;
        enrichedContext["timestamp"] = DateTime.UtcNow;
        
        // Forward to target with enriched context
        target.Write(logType, message, enrichedContext);
    }
}
```

### Custom Log Interceptor

```csharp
public class SensitiveDataInterceptor : ILogInterceptor
{
    private readonly Regex _creditCardRegex = new Regex(@"\b(?:\d{4}[-\s]?){3}\d{4}\b");
    private readonly Regex _emailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
    
    public bool Intercept(ref LogType logType, ref string message, ref object context)
    {
        // Redact sensitive information
        if (message != null)
        {
            message = _creditCardRegex.Replace(message, "XXXX-XXXX-XXXX-XXXX");
            message = _emailRegex.Replace(message, "EMAIL@REDACTED");
        }
        
        // Continue processing the log
        return true;
    }
}
```

## Integration with Unity's Burst Compiler

The `IBurstLogger` interface is specifically designed to work with Unity's Burst compiler:

```csharp
[BurstCompile]
public struct BurstCompatibleSystem : ISystem
{
    [ReadOnly] public IBurstLogger Logger;
    
    public void OnUpdate(ref SystemState state)
    {
        // This logging call is compatible with Burst compilation
        Logger.Log(LogType.Log, "System updated");
        
        // Check log type before expensive operations
        if (Logger.IsLogTypeAllowed(LogType.Log))
        {
            // Only generate detailed reports when logging is enabled
            string report = GenerateDetailedReport();
            Logger.Log(LogType.Log, report);
        }
    }
    
    private string GenerateDetailedReport()
    {
        // Generate a detailed performance report...
        return "Detailed system performance report...";
    }
}
```

## Advanced Usage Patterns

### Hierarchical Logging

You can implement hierarchical logging based on categories:

```csharp
// Configure log levels hierarchically
var factory = new LoggerFactory();
factory.Config.CategoryLogLevels["Game"] = LogLevel.Warning;
factory.Config.CategoryLogLevels["Game.UI"] = LogLevel.Info;
factory.Config.CategoryLogLevels["Game.Physics"] = LogLevel.Error;

// Create loggers with hierarchical categories
var gameLogger = factory.CreateLogger("Game");
var uiLogger = factory.CreateLogger("Game.UI");
var physicsLogger = factory.CreateLogger("Game.Physics");

// Each logger will respect its specific category log level
```

### Log Batching

You can implement batching to improve performance when logging high-frequency events:

```csharp
public class BatchingLogTarget : ILogTarget
{
    private readonly ILogTarget _innerTarget;
    private readonly List<(LogType Type, string Message, object Context)> _batchedLogs;
    private readonly int _batchSize;
    private readonly object _lockObject = new object();
    
    public BatchingLogTarget(ILogTarget innerTarget, int batchSize = 100)
    {
        _innerTarget = innerTarget;
        _batchedLogs = new List<(LogType, string, object)>();
        _batchSize = batchSize;
    }
    
    public void Write(LogType logType, string message, object context = null)
    {
        lock (_lockObject)
        {
            _batchedLogs.Add((logType, message, context));
            
            if (_batchedLogs.Count >= _batchSize)
            {
                Flush();
            }
        }
    }
    
    public void Flush()
    {
        lock (_lockObject)
        {
            foreach (var (type, message, context) in _batchedLogs)
            {
                _innerTarget.Write(type, message, context);
            }
            
            _batchedLogs.Clear();
            _innerTarget.Flush();
        }
    }
}

// Usage
loggerFactory.AddTarget(new BatchingLogTarget(new FileLogTarget("app.log"), 50));
```

### Async Logging

For better performance in the main thread, implement asynchronous logging:

```csharp
public class AsyncLogTarget : ILogTarget
{
    private readonly ILogTarget _innerTarget;
    private readonly ConcurrentQueue<(LogType Type, string Message, object Context)> _logQueue;
    private readonly Thread _loggingThread;
    private readonly AutoResetEvent _logEvent = new AutoResetEvent(false);
    private volatile bool _isRunning = true;
    
    public AsyncLogTarget(ILogTarget innerTarget)
    {
        _innerTarget = innerTarget;
        _logQueue = new ConcurrentQueue<(LogType, string, object)>();
        
        _loggingThread = new Thread(ProcessLogs);
        _loggingThread.IsBackground = true;
        _loggingThread.Start();
    }
    
    public void Write(LogType logType, string message, object context = null)
    {
        _logQueue.Enqueue((logType, message, context));
        _logEvent.Set();
    }
    
    public void Flush()
    {
        _logEvent.Set();
        
        // Wait for queue to drain
        while (!_logQueue.IsEmpty)
        {
            Thread.Sleep(10);
        }
        
        _innerTarget.Flush();
    }
    
    private void ProcessLogs()
    {
        while (_isRunning)
        {
            _logEvent.WaitOne();
            
            while (_logQueue.TryDequeue(out var logEntry))
            {
                _innerTarget.Write(logEntry.Type, logEntry.Message, logEntry.Context);
            }
        }
    }
    
    public void Dispose()
    {
        _isRunning = false;
        _logEvent.Set();
        _loggingThread.Join(1000);
    }
}

// Usage
loggerFactory.AddTarget(new AsyncLogTarget(new FileLogTarget("app.log")));
```

## Testing with Interfaces

The interface-based design makes testing much easier:

```csharp
public class TestLogTarget : ILogTarget
{
    public List<(LogType Type, string Message, object Context)> Logs { get; } = 
        new List<(LogType, string, object)>();
    
    public void Write(LogType logType, string message, object context = null)
    {
        Logs.Add((logType, message, context));
    }
    
    public void Flush()
    {
        // Nothing to flush in test target
    }
    
    // Helper methods for testing
    public bool ContainsLog(LogType logType, string messageSubstring)
    {
        return Logs.Any(log => 
            log.Type == logType && 
            log.Message.Contains(messageSubstring));
    }
    
    public void Clear()
    {
        Logs.Clear();
    }
}

// Testing example
[Test]
public void GameController_StartGame_LogsGameStarted()
{
    // Arrange
    var testTarget = new TestLogTarget();
    var loggerFactory = new LoggerFactory();
    loggerFactory.AddTarget(testTarget);
    
    var logger = loggerFactory.CreateLogger("GameController");
    var gameController = new GameController(logger);
    
    // Act
    gameController.StartGame();
    
    // Assert
    Assert.IsTrue(testTarget.ContainsLog(LogType.Log, "Game started"));
}
```

## Best Practices

1. **Use Category-Specific Loggers**: Create loggers with specific categories to enable fine-grained control over log levels.

2. **Leverage Middleware for Cross-Cutting Concerns**: Use middleware for adding timestamps, correlation IDs, and other context that should be applied consistently.

3. **Be Mindful of Performance in Burst Code**: In Burst-compiled code, check `IsLogTypeAllowed` before constructing expensive log messages.

4. **Follow Log Type Guidelines**:
   - **Error/Exception**: For errors that affect application functionality
   - **Warning**: For non-critical issues that should be monitored
   - **Log**: For general operational information
   - **Assert**: For runtime assertion failures

5. **Configure Log Levels Appropriately**: Set stricter log levels in production environments to reduce noise and performance impact.

6. **Use Interceptors for Sensitive Data**: Implement interceptors to redact sensitive information before it enters logs.

7. **Implement Async Logging for Performance-Critical Code**: Use async logging targets to avoid blocking the main thread.

## Troubleshooting

### Common Issues

1. **Missing Log Messages**: Verify that the log level for the category is set appropriately and that the log type is allowed.

2. **Performance Issues**: Consider using async logging and check for frequent, high-volume logging in hot code paths.

3. **High Memory Usage**: Make sure to flush log buffers regularly and consider implementing size limits for batched logs.

### Debugging the Logging System

```csharp
// Add a debug target to help troubleshoot logging issues
loggerFactory.AddTarget(new ConsoleLogTarget(new DefaultLogFormatter()));

// Temporarily lower the minimum log level
loggerFactory.Config.MinimumLogLevel = LogLevel.Trace;
```

## API Reference

### LogType Enum

```csharp
public enum LogType
{
    Error = 0,
    Assert = 1,
    Warning = 2,
    Log = 3,
    Exception = 4
}
```

### LogLevel Enum

```csharp
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
```

## See Also

- [Events Documentation](./events.md)
- [Filters Documentation](./filters.md)
- [Formatters Documentation](./formatters.md)
- [Targets Documentation](./targets.md)
