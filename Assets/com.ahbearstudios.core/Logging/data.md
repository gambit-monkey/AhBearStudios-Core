# Logging Data Documentation

## Overview

The AhBearStudios-Core Logging system utilizes a set of structured data types to represent, store, and transfer log information throughout the logging pipeline. These data structures form the foundation of the logging system, enabling consistent formatting, efficient storage, and flexible processing of log messages.

This document describes the core data structures in the logging system, their properties, and how they interact with other components of the framework.

The logging data system is built around the following core components:

- **LogMessage**: The fundamental unit of logging data that encapsulates all information about a log event
- **LogLevel**: An enumeration representing the severity or importance of a log message
- **LogChannel**: An enumeration for categorizing logs by application domain or feature
- **LogFormat**: A template-based system for formatting log messages
- **LogMessageBuilder**: A fluent API for constructing log messages

## LogMessage

The `LogMessage` class is the primary data structure in the logging system. It represents a discrete logging event and contains all relevant information about that event.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | The main content of the log message |
| `Level` | `LogLevel` | The severity/importance level of the message |
| `Channel` | `LogChannel` | The category or domain of the message |
| `Timestamp` | `DateTime` | When the log message was created |
| `Logger` | `string` | The name of the logger that created the message |
| `StackTrace` | `string` | The stack trace at the time the message was created (if enabled) |
| `ThreadId` | `int` | The ID of the thread that created the message |
| `ThreadName` | `string` | The name of the thread that created the message |
| `ApplicationVersion` | `string` | The version of the application |
| `Context` | `Dictionary<string, object>` | Additional contextual data associated with the message |

### Code Example

```csharp
// Creating a LogMessage directly
var message = new LogMessage
{
    Text = "Player health changed",
    Level = LogLevel.Info,
    Channel = LogChannel.Core,
    Timestamp = DateTime.Now,
    Logger = "HealthSystem",
    Context = new Dictionary<string, object>
    {
        { "OldHealth", 100 },
        { "NewHealth", 75 },
        { "Damage", 25 }
    }
};

// Log message is typically created by the Logger class
Logger.Log(LogLevel.Info, LogChannel.Core, "Player health changed", new Dictionary<string, object>
{
    { "OldHealth", 100 },
    { "NewHealth", 75 },
    { "Damage", 25 }
});
```

## LogLevel

The `LogLevel` enumeration defines the severity or importance of a log message. It uses a hierarchical system where higher levels include all lower levels for filtering purposes.

```csharp
public enum LogLevel
{
    Trace = 0,     // Most detailed information, typically for debugging
    Debug = 1,     // Detailed debugging information
    Info = 2,      // General information about application progress
    Warning = 3,   // Potential issues that aren't immediately problematic
    Error = 4,     // Error conditions that might allow the application to continue
    Critical = 5,  // Severe errors that may cause the application to terminate
    None = 6       // Used to disable logging
}
```

The logging system uses this enumeration to filter messages based on minimum log level settings in the logging configuration.

### Usage Examples

```csharp
// Creating messages with different log levels
Logger.Log(LogLevel.Trace, LogChannel.Core, "Detailed tracing information");
Logger.Log(LogLevel.Debug, LogChannel.Core, "Debugging information");
Logger.Log(LogLevel.Info, LogChannel.Core, "General information");
Logger.Log(LogLevel.Warning, LogChannel.Core, "Warning message");
Logger.Log(LogLevel.Error, LogChannel.Core, "Error message");
Logger.Log(LogLevel.Critical, LogChannel.Core, "Critical error message");

// Using convenience methods
Logger.Trace(LogChannel.Core, "Detailed tracing information");
Logger.Debug(LogChannel.Core, "Debugging information");
Logger.Info(LogChannel.Core, "General information");
Logger.Warning(LogChannel.Core, "Warning message");
Logger.Error(LogChannel.Core, "Error message");
Logger.Critical(LogChannel.Core, "Critical error message");
```

### LogLevel Extensions

The logging system includes extension methods to make working with log levels more convenient:

```csharp
// Check if one log level includes another
if (LogLevel.Info.Includes(LogLevel.Debug))
{
    // This will be false because Info > Debug
}

if (LogLevel.Info.Includes(LogLevel.Warning))
{
    // This will be true because Info < Warning
}

// Convert log level to string representation
string levelName = LogLevel.Warning.ToString(); // "Warning"

// Parse log level from string
LogLevel level = LogLevelExtensions.Parse("Error"); // LogLevel.Error
```

## LogChannel

The `LogChannel` enumeration provides a categorical organization system, allowing you to group logs by feature, module, or domain.

```csharp
public enum LogChannel
{
    None,           // Default channel
    Core,           // Core system logs
    UI,             // User interface logs
    Audio,          // Audio system logs
    Networking,     // Networking logs
    Physics,        // Physics system logs
    Input,          // Input system logs
    AI,             // Artificial intelligence logs
    Performance,    // Performance monitoring logs
    Custom          // Custom user-defined logs
}
```

The logging system can be configured with different minimum log levels for different channels, providing fine-grained control over what gets logged.

### Usage Examples

```csharp
// Logging to different channels
Logger.Info(LogChannel.Core, "Core system initialized");
Logger.Info(LogChannel.UI, "Main menu loaded");
Logger.Info(LogChannel.Audio, "Background music started");
Logger.Info(LogChannel.Networking, "Connected to server");
Logger.Info(LogChannel.Physics, "Physics system initialized");
Logger.Info(LogChannel.Input, "Input system initialized");
Logger.Info(LogChannel.AI, "AI behavior tree loaded");
Logger.Info(LogChannel.Performance, "Frame time: 16.7ms");
Logger.Info(LogChannel.Custom, "Custom subsystem message");
```

### LogChannel Extensions

The logging system includes extension methods for working with log channels:

```csharp
// Convert log channel to string representation
string channelName = LogChannel.Networking.ToString(); // "Networking"

// Parse log channel from string
LogChannel channel = LogChannelExtensions.Parse("UI"); // LogChannel.UI
```

## LogFormat

The `LogFormat` class provides a template-based system for formatting log messages. It uses placeholders to represent different components of a log message.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Template` | `string` | The template string with placeholders |
| `TimestampFormat` | `string` | Format for timestamp placeholders |

### Available Placeholders

| Placeholder | Description |
|-------------|-------------|
| `{timestamp}` | The date and time when the log was created |
| `{level}` | The log level (Trace, Debug, Info, etc.) |
| `{channel}` | The log channel (Core, UI, Networking, etc.) |
| `{message}` | The actual log message text |
| `{logger}` | The name of the logger that created the log |
| `{stacktrace}` | The stack trace (if enabled) |
| `{appversion}` | The application version |
| `{threadid}` | The ID of the thread that created the log |
| `{threadname}` | The name of the thread that created the log |

### Code Examples

```csharp
// Simple format
var simpleFormat = new LogFormat("[{level}] {message}");

// Detailed format
var detailedFormat = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}\nStack Trace: {stacktrace}");

// Custom timestamp format
var customFormat = new LogFormat(
    template: "[{timestamp}] [{level}] {message}",
    timestampFormat: "yyyy-MM-dd HH:mm:ss,fff"
);

// Formatting a log message
string formattedLog = detailedFormat.Format(logMessage);
```

## LogMessageBuilder

The `LogMessageBuilder` class provides a fluent API for constructing `LogMessage` objects. It's useful for building complex log messages with context data.

### Methods

| Method | Description |
|--------|-------------|
| `WithText(string text)` | Sets the main message text |
| `WithLevel(LogLevel level)` | Sets the log level |
| `WithChannel(LogChannel channel)` | Sets the log channel |
| `WithLogger(string logger)` | Sets the logger name |
| `WithTimestamp(DateTime timestamp)` | Sets the message timestamp |
| `WithStackTrace(string stackTrace)` | Sets the stack trace |
| `WithThreadInfo(int id, string name)` | Sets thread ID and name |
| `WithApplicationVersion(string version)` | Sets the application version |
| `WithContext(string key, object value)` | Adds a context key-value pair |
| `WithContext(Dictionary<string, object> context)` | Adds multiple context key-value pairs |
| `Build()` | Creates the final LogMessage object |

### Code Example

```csharp
// Building a complex log message
var message = new LogMessageBuilder()
    .WithText("Player completed level")
    .WithLevel(LogLevel.Info)
    .WithChannel(LogChannel.Core)
    .WithLogger("GameProgressManager")
    .WithContext("LevelId", 3)
    .WithContext("CompletionTime", 245.8f)
    .WithContext("CollectedItems", 12)
    .WithContext("TotalItems", 15)
    .Build();

// The builder is typically used internally by the Logger class
Logger.Log(LogLevel.Info, LogChannel.Core, "Player completed level", new Dictionary<string, object>
{
    { "LevelId", 3 },
    { "CompletionTime", 245.8f },
    { "CollectedItems", 12 },
    { "TotalItems", 15 }
});
```

## LogContext

The `LogContext` class provides a way to attach contextual information to log messages without passing it explicitly with each log call. It maintains context data for the current thread and automatically adds it to log messages.

### Methods

| Method | Description |
|--------|-------------|
| `Set(string key, object value)` | Sets a context value for the current thread |
| `Get(string key)` | Gets a context value for the current thread |
| `Remove(string key)` | Removes a context value for the current thread |
| `Clear()` | Clears all context values for the current thread |
| `GetSnapshot()` | Gets a copy of the current thread's context data |

### Code Examples

```csharp
// Setting context values that will be included in all subsequent log messages
LogContext.Set("SessionId", "abc123");
LogContext.Set("UserId", 42);

// These log messages will automatically include the SessionId and UserId context
Logger.Info(LogChannel.Core, "User logged in");
Logger.Info(LogChannel.UI, "Main menu loaded");

// Clear specific context values when they're no longer needed
LogContext.Remove("UserId");

// This log message will only include SessionId in its context
Logger.Info(LogChannel.Core, "Application state changed");

// Clear all context values
LogContext.Clear();
```

### Using LogContext with `using` Statement

For temporary context values that should be limited to a specific scope, you can use the `LogContextScope` class:

```csharp
public void ProcessRequest(string requestId)
{
    // Create a scope that automatically adds the requestId to all logs within the scope
    using (new LogContextScope("RequestId", requestId))
    {
        Logger.Info(LogChannel.Networking, "Processing request"); // Includes RequestId
        
        // Do processing...
        
        Logger.Info(LogChannel.Networking, "Request completed"); // Also includes RequestId
    }
    
    // Outside the scope, logs no longer include the RequestId
    Logger.Info(LogChannel.Networking, "Ready for next request");
}
```

## LoggingEventArgs

The `LoggingEventArgs` class provides an event data container for logging events, used primarily for the event system within the logging framework.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Message` | `LogMessage` | The log message associated with the event |

### Usage in Event Handling

```csharp
// Subscribing to the Logger's LogMessageCreated event
Logger.LogMessageCreated += OnLogMessageCreated;

// Event handler
private void OnLogMessageCreated(object sender, LoggingEventArgs e)
{
    LogMessage message = e.Message;
    
    // Custom processing of the log message
    if (message.Level == LogLevel.Error && message.Channel == LogChannel.Networking)
    {
        // Send network errors to a remote monitoring service
        _monitoringService.ReportError(message.Text, message.Context);
    }
}
```

## Integration with Logging System

The data structures form the backbone of the entire logging system, integrating with other components as follows:

### With LoggingConfiguration

The `LoggingConfiguration` class uses `LogLevel` and `LogChannel` to define filtering rules:

```csharp
// Configure minimum log levels for specific channels
var config = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Info
};

config.SetChannelLogLevel(LogChannel.Core, LogLevel.Debug);
config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Warning);
```

### With LoggingTargets

Logging targets use `LogFormat` to format `LogMessage` objects for output:

```csharp
// Configure a console target with custom format
var consoleTarget = new ConsoleLoggingTarget
{
    Format = new LogFormat("[{timestamp}] [{level}] {message}")
};

// Configure a file target with different format
var fileTarget = new FileLoggingTarget
{
    Format = new LogFormat("{timestamp}|{level}|{channel}|{message}|{threadid}"),
    FilePath = "Logs/application.log"
};
```

### With Logger Class

The `Logger` class creates `LogMessage` objects (often using `LogMessageBuilder` internally) and processes them:

```csharp
// Logger creates LogMessage objects based on method parameters
Logger.Log(LogLevel.Warning, LogChannel.UI, "Failed to load texture", new Dictionary<string, object>
{
    { "TexturePath", "Assets/Textures/Player.png" },
    { "ErrorCode", 404 }
});

// And convenience methods for each log level
Logger.Warning(LogChannel.UI, "Failed to load texture", new Dictionary<string, object>
{
    { "TexturePath", "Assets/Textures/Player.png" },
    { "ErrorCode", 404 }
});
```

### With Attributes

Logging attributes use the data structures to generate `LogMessage` objects automatically:

```csharp
// LogMethodAttribute automatically creates LogMessage objects for method entry and exit
[LogMethod(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Info, Channel = LogChannel.Core)]
public void ProcessData(string data)
{
    // Method implementation
}
```

## Practical Examples

### Logging with Context Data

```csharp
public class PlayerController
{
    private string _playerId;
    private string _currentLevel;
    
    public void Initialize(string playerId)
    {
        _playerId = playerId;
        
        // Set persistent player context
        LogContext.Set("PlayerId", playerId);
        
        Logger.Info(LogChannel.Core, "Player controller initialized", new Dictionary<string, object>
        {
            { "InitialPosition", Vector3.zero }
        });
    }
    
    public void LoadLevel(string levelId)
    {
        _currentLevel = levelId;
        
        // Set level context
        LogContext.Set("LevelId", levelId);
        
        // This log includes both PlayerId and LevelId in context
        Logger.Info(LogChannel.Core, "Loading level");
        
        // Load level implementation...
        
        Logger.Info(LogChannel.Core, "Level loaded", new Dictionary<string, object>
        {
            { "LoadTime", 3.45f }
        });
    }
    
    public void CollectItem(string itemId, int score)
    {
        // Log with both persistent context and call-specific context
        Logger.Info(LogChannel.Core, "Item collected", new Dictionary<string, object>
        {
            { "ItemId", itemId },
            { "Score", score }
        });
        
        // This log includes PlayerId and LevelId from context, plus the explicit parameters
    }
}
```

### Using Log Format for Different Outputs

```csharp
public class LoggingSetup
{
    public void ConfigureLogging()
    {
        var config = new LoggingConfiguration
        {
            DefaultMinimumLogLevel = LogLevel.Info,
            IsStackTraceEnabled = true,
            ApplicationVersion = Application.version
        };
        
        // Console target with simple format for readability
        config.AddTarget(new ConsoleLoggingTarget
        {
            Format = new LogFormat("[{level}] {message}")
        });
        
        // File target with detailed format for troubleshooting
        config.AddTarget(new FileLoggingTarget
        {
            FilePath = "Logs/detailed.log",
            Format = new LogFormat("[{timestamp}] [{threadid}] [{level}] [{channel}] {message}\n{stacktrace}")
        });
        
        // Analytics target with structured format for data processing
        config.AddTarget(new CustomLoggingTarget
        {
            Format = new LogFormat("{timestamp}|{level}|{channel}|{message}"),
            Name = "Analytics"
        });
        
        Logger.Initialize(config);
    }
}
```

### Advanced Context Scope Management

```csharp
public class NetworkManager
{
    public async Task ProcessRequestAsync(NetworkRequest request)
    {
        // Create a context scope for this request
        using (var scope = new LogContextScope())
        {
            // Add multiple context values that will be included in all logs within this scope
            scope.Set("RequestId", request.Id);
            scope.Set("ClientId", request.ClientId);
            scope.Set("RequestType", request.Type);
            
            try
            {
                Logger.Info(LogChannel.Networking, "Processing request started");
                
                // Simulate processing
                await Task.Delay(100);
                
                // Add more context as processing continues
                scope.Set("ProcessingStage", "Validation");
                Logger.Debug(LogChannel.Networking, "Validating request data");
                
                await Task.Delay(100);
                
                scope.Set("ProcessingStage", "Execution");
                Logger.Debug(LogChannel.Networking, "Executing request");
                
                await Task.Delay(100);
                
                Logger.Info(LogChannel.Networking, "Request processing completed", new Dictionary<string, object>
                {
                    { "ProcessingTime", 300 }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogChannel.Networking, $"Request processing failed: {ex.Message}");
                throw;
            }
        }
        
        // Outside the scope, logs no longer include the request context
    }
}
```

## Building Custom Data Extensions

You can extend the logging data system with custom functionality:

### Custom LogLevel Extension

```csharp
public static class CustomLogLevelExtensions
{
    public static string ToDisplayEmoji(this LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace: return "🔍";
            case LogLevel.Debug: return "🐞";
            case LogLevel.Info: return "ℹ️";
            case LogLevel.Warning: return "⚠️";
            case LogLevel.Error: return "❌";
            case LogLevel.Critical: return "🔥";
            default: return "";
        }
    }
}

// Usage:
var format = new LogFormat($"{{timestamp}} {LogLevel.Warning.ToDisplayEmoji()} {{message}}");
```

### Custom LogFormat Extension

```csharp
public static class CustomLogFormatExtensions
{
    public static LogFormat WithEmojiLevel(this LogFormat format)
    {
        string template = format.Template;
        
        // Replace {level} placeholder with emoji representation
        if (template.Contains("{level}"))
        {
            template = template.Replace("{level}", "{emoji-level}");
        }
        
        return new CustomLogFormat(template, format.TimestampFormat);
    }
}

// Custom LogFormat that replaces {emoji-level} with emoji representations
public class CustomLogFormat : LogFormat
{
    public CustomLogFormat(string template, string timestampFormat = "yyyy-MM-dd HH:mm:ss")
        : base(template, timestampFormat)
    {
    }
    
    public override string Format(LogMessage message)
    {
        string formatted = base.Format(message);
        
        if (formatted.Contains("{emoji-level}"))
        {
            string emoji = message.Level.ToDisplayEmoji();
            formatted = formatted.Replace("{emoji-level}", emoji);
        }
        
        return formatted;
    }
}

// Usage:
var format = new LogFormat("[{timestamp}] [{level}] {message}").WithEmojiLevel();
```

## Best Practices

1. **Use contextual logging**: Add relevant context data to logs to make them more useful for debugging and analysis:

   ```csharp
   // Avoid this:
   Logger.Info(LogChannel.Core, "User logged in: userId=42, role=admin");
   
   // Do this instead:
   Logger.Info(LogChannel.Core, "User logged in", new Dictionary<string, object>
   {
       { "UserId", 42 },
       { "Role", "admin" }
   });
   ```

2. **Use LogContext for persistent data**: Set context values that should be included in multiple log messages:

   ```csharp
   void StartSession(string sessionId)
   {
       LogContext.Set("SessionId", sessionId);
       // All subsequent logs will include SessionId
   }
   ```

3. **Use appropriate log levels**: Choose log levels based on the significance of the information:
   - `Trace`: Extremely detailed information for step-by-step debugging
   - `Debug`: Useful debugging information, more focused than Trace
   - `Info`: General operational information
   - `Warning`: Potential issues that aren't immediately problematic
   - `Error`: Error conditions that might allow the application to continue
   - `Critical`: Severe errors that may cause the application to terminate

4. **Use channels to categorize logs**: Create a logical channel structure that reflects your application's architecture:

   ```csharp
   // Categorize logs by domain
   Logger.Info(LogChannel.UI, "Menu loaded");
   Logger.Info(LogChannel.Networking, "Connection established");
   Logger.Info(LogChannel.Audio, "Background music playing");
   ```

5. **Create consistent log formats**: Establish standard formats for different types of outputs:

   ```csharp
   // Console format - concise and readable
   var consoleFormat = new LogFormat("[{level}] {message}");
   
   // File format - detailed with timestamp
   var fileFormat = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}");
   
   // Debug format - comprehensive with thread and stack
   var debugFormat = new LogFormat("[{timestamp}] [{threadid}] [{level}] [{channel}] {message}\n{stacktrace}");
   ```

## Troubleshooting Common Issues

### Missing Context Data

1. Verify that context data is being set before the log message is created.
2. Check that you're not clearing the context inadvertently.
3. For thread-specific issues, remember that `LogContext` is thread-local. Context set in one thread is not available in another.

### Formatting Issues

1. Check that all placeholders in your format template are valid.
2. Verify that timestamp format strings follow the standard .NET DateTime format.
3. When custom formatters are used, ensure they handle all possible values correctly.

### Performance Considerations

1. Be mindful of creating complex context objects for high-frequency logs.
2. Consider using conditional logging for expensive operations:

   ```csharp
   if (Logger.ShouldLog(LogLevel.Debug, LogChannel.Performance))
   {
       var complexData = GatherExpensiveMetrics();
       Logger.Debug(LogChannel.Performance, "Performance metrics", complexData);
   }
   ```

## Further Reading

For more information on the AhBearStudios-Core logging system, refer to:

- [Logger Class Documentation](../logger.md)
- [Logging Configuration Documentation](../loggingconfiguration.md)
- [Logging Attributes Documentation](../attributes.md)
- [Logging Targets Documentation](../loggingtargets.md)
