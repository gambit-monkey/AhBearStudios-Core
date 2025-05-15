# Logging Configuration Documentation

## Overview

The AhBearStudios-Core Logging system provides a flexible, configurable logging framework for Unity applications. This document describes the configuration components that allow you to control how your application's logs are processed, filtered, and output to various destinations.

The logging configuration system is built around the following core components:

- **LoggingConfiguration**: The central configuration object that defines the behavior of the entire logging system
- **LoggingTarget**: Destinations where log messages are sent (console, file, custom targets)
- **LogFormat**: Controls the format and structure of log messages
- **LogLevel**: Hierarchical severity system to categorize and filter logs
- **LogChannel**: Categorical organization system for logs by application domain or feature

## LoggingConfiguration

The `LoggingConfiguration` class serves as the central configuration point for the entire logging system. It holds collections of logging targets, log level settings, and global configuration properties.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `LoggingTargets` | `List<LoggingTarget>` | Collection of destinations where logs will be sent |
| `DefaultMinimumLogLevel` | `LogLevel` | The minimum log level for messages to be processed |
| `ChannelMinimumLogLevels` | `Dictionary<LogChannel, LogLevel>` | Per-channel log level overrides |
| `DefaultLogFormat` | `LogFormat` | The default formatting for log messages |
| `IsStackTraceEnabled` | `bool` | Whether to include stack traces in log messages |
| `ApplicationVersion` | `string` | The current application version (included in logs) |

### Methods

| Method | Description |
|--------|-------------|
| `AddTarget(LoggingTarget target)` | Adds a new logging target |
| `RemoveTarget(LoggingTarget target)` | Removes an existing logging target |
| `SetChannelLogLevel(LogChannel channel, LogLevel level)` | Sets the minimum log level for a specific channel |
| `ShouldLog(LogLevel level, LogChannel channel)` | Determines if a message with the given level and channel should be logged |

## Log Levels

Log levels define the severity of log messages and provide a mechanism for filtering logs. The logging system uses a hierarchical level system, where each level includes all higher severity levels.

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

When configuring minimum log levels, any message with a severity at or above the minimum level will be logged. For example, setting a minimum level of `Warning` will allow `Warning`, `Error`, and `Critical` messages to be logged, while filtering out `Info`, `Debug`, and `Trace` messages.

## Log Channels

Log channels provide a categorical organization system, allowing you to group logs by feature, module, or domain. This enables more granular control over log filtering and presentation.

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

The `LoggingConfiguration` allows setting different minimum log levels for different channels, providing fine-grained control over what gets logged.

## Logging Targets

Logging targets define where log messages are sent. The system includes several built-in targets and allows for custom targets.

### Built-in Targets

1. **ConsoleLoggingTarget**: Outputs log messages to the Unity console
2. **FileLoggingTarget**: Writes log messages to files on disk
3. **CustomLoggingTarget**: Base class for implementing custom logging targets

### Target Configuration

Each target has its own configuration options:

#### ConsoleLoggingTarget
```csharp
var consoleTarget = new ConsoleLoggingTarget
{
    MinimumLogLevel = LogLevel.Info,
    Format = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}")
};
```

#### FileLoggingTarget
```csharp
var fileTarget = new FileLoggingTarget
{
    MinimumLogLevel = LogLevel.Debug,
    Format = new LogFormat("{timestamp} | {level} | {channel} | {message}"),
    FilePath = "Logs/application.log",
    RollingInterval = RollingInterval.Day,
    MaxFileSize = 10 * 1024 * 1024  // 10 MB
};
```

## Log Formatting

The `LogFormat` class controls how log messages are structured. It uses template strings with placeholders for log components.

### Available Placeholders

| Placeholder | Description |
|-------------|-------------|
| `{timestamp}` | The date and time when the log was created |
| `{level}` | The log level (Trace, Debug, Info, etc.) |
| `{channel}` | The log channel (Core, UI, Networking, etc.) |
| `{message}` | The actual log message |
| `{logger}` | The name of the logger that created the log |
| `{stacktrace}` | The stack trace (if enabled) |
| `{appversion}` | The application version |
| `{threadid}` | The ID of the thread that created the log |

### Examples

```csharp
// Simple format
var simpleFormat = new LogFormat("[{level}] {message}");

// Detailed format
var detailedFormat = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}\nStack Trace: {stacktrace}");

// Custom format with timestamp formatting
var customFormat = new LogFormat(
    template: "[{timestamp}] [{level}] {message}",
    timestampFormat: "yyyy-MM-dd HH:mm:ss,fff"
);
```

## Integration with Logging System

The configuration components integrate with the core `Logger` class to control logging behavior throughout your application.

### Setting Up the Logging System

```csharp
// Create a logging configuration
var config = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Info,
    IsStackTraceEnabled = true,
    ApplicationVersion = Application.version
};

// Add console target
config.AddTarget(new ConsoleLoggingTarget
{
    MinimumLogLevel = LogLevel.Debug,
    Format = new LogFormat("[{timestamp}] [{level}] {message}")
});

// Add file target
config.AddTarget(new FileLoggingTarget
{
    FilePath = $"{Application.persistentDataPath}/Logs/app.log",
    RollingInterval = RollingInterval.Day,
    Format = new LogFormat("{timestamp} | {level} | {channel} | {message}")
});

// Configure channel-specific log levels
config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Debug);
config.SetChannelLogLevel(LogChannel.UI, LogLevel.Warning);

// Initialize the logger with this configuration
Logger.Initialize(config);
```

### Using the Logger with Configuration

```csharp
// These log messages will respect the configuration settings
Logger.Log(LogLevel.Info, LogChannel.Core, "Application started");
Logger.Log(LogLevel.Debug, LogChannel.Networking, "Connected to server at 192.168.1.1");
Logger.Log(LogLevel.Warning, LogChannel.UI, "Button reference is null, using fallback");

// These convenience methods are also available
Logger.Trace(LogChannel.Core, "Detailed trace information");
Logger.Debug(LogChannel.Core, "Debug information");
Logger.Info(LogChannel.Core, "General information");
Logger.Warning(LogChannel.Core, "Warning message");
Logger.Error(LogChannel.Core, "Error message");
Logger.Critical(LogChannel.Core, "Critical error message");
```

## Creating Custom Logging Targets

You can extend the logging system by creating custom targets that send logs to any destination you need:

```csharp
public class CustomAnalyticsTarget : LoggingTarget
{
    private readonly IAnalyticsService _analyticsService;
    
    public CustomAnalyticsTarget(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        MinimumLogLevel = LogLevel.Error; // Only send errors and above to analytics
    }
    
    public override void Log(LogMessage message)
    {
        if (!ShouldLog(message.Level))
            return;
            
        _analyticsService.TrackEvent("AppError", new Dictionary<string, string>
        {
            { "message", message.Text },
            { "level", message.Level.ToString() },
            { "channel", message.Channel.ToString() },
            { "timestamp", message.Timestamp.ToString() }
        });
    }
}

// Add to configuration
config.AddTarget(new CustomAnalyticsTarget(analyticsService));
```

## Runtime Configuration Changes

The logging configuration can be modified at runtime to adjust logging behavior based on application state:

```csharp
// Enable more detailed logging when troubleshooting is needed
public void EnableDetailedLogging()
{
    var config = Logger.Configuration;
    config.DefaultMinimumLogLevel = LogLevel.Debug;
    config.IsStackTraceEnabled = true;
    config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Trace);
    
    // Force targets to use a more detailed format
    foreach (var target in config.LoggingTargets)
    {
        target.Format = new LogFormat("[{timestamp}] [{threadid}] [{level}] [{channel}] {message}");
    }
}

// Restore normal logging
public void RestoreNormalLogging()
{
    var config = Logger.Configuration;
    config.DefaultMinimumLogLevel = LogLevel.Info;
    config.IsStackTraceEnabled = false;
    config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Warning);
    
    // Restore simpler format
    foreach (var target in config.LoggingTargets)
    {
        target.Format = new LogFormat("[{timestamp}] [{level}] {message}");
    }
}
```

## Configuration via JSON

The logging system supports configuration via JSON files, allowing for easy changes without recompiling:

```csharp
// Load configuration from JSON
public static LoggingConfiguration LoadFromJson(string json)
{
    var configData = JsonUtility.FromJson<LoggingConfigurationData>(json);
    var config = new LoggingConfiguration
    {
        DefaultMinimumLogLevel = configData.DefaultMinimumLogLevel,
        IsStackTraceEnabled = configData.IsStackTraceEnabled,
        ApplicationVersion = configData.ApplicationVersion
    };
    
    // Set up targets from the configuration data
    foreach (var targetData in configData.Targets)
    {
        LoggingTarget target = null;
        
        switch (targetData.Type)
        {
            case "Console":
                target = new ConsoleLoggingTarget();
                break;
            case "File":
                target = new FileLoggingTarget
                {
                    FilePath = targetData.FilePath,
                    RollingInterval = targetData.RollingInterval
                };
                break;
        }
        
        if (target != null)
        {
            target.MinimumLogLevel = targetData.MinimumLogLevel;
            target.Format = new LogFormat(targetData.FormatTemplate);
            config.AddTarget(target);
        }
    }
    
    // Set channel-specific log levels
    foreach (var channelLevel in configData.ChannelLogLevels)
    {
        config.SetChannelLogLevel(channelLevel.Channel, channelLevel.Level);
    }
    
    return config;
}
```

Example JSON configuration:

```json
{
  "DefaultMinimumLogLevel": "Info",
  "IsStackTraceEnabled": true,
  "ApplicationVersion": "1.0.0",
  "Targets": [
    {
      "Type": "Console",
      "MinimumLogLevel": "Debug",
      "FormatTemplate": "[{timestamp}] [{level}] {message}"
    },
    {
      "Type": "File",
      "MinimumLogLevel": "Info",
      "FormatTemplate": "{timestamp} | {level} | {channel} | {message}",
      "FilePath": "Logs/app.log",
      "RollingInterval": "Day"
    }
  ],
  "ChannelLogLevels": [
    {
      "Channel": "Networking",
      "Level": "Debug"
    },
    {
      "Channel": "UI",
      "Level": "Warning"
    }
  ]
}
```

## Best Practices

1. **Configure appropriate log levels**: Use `LogLevel.Trace` and `LogLevel.Debug` during development, but switch to `LogLevel.Info` or higher in production to reduce overhead.

2. **Leverage channels effectively**: Create a logical channel structure that mirrors your application's architecture, making it easier to isolate issues.

3. **Use different targets for different purposes**: For example, send all logs to a file, but only show warnings and above in the console.

4. **Be mindful of performance**: Excessive logging, especially with stack traces enabled, can impact application performance. Consider using conditional logging:

   ```csharp
   if (Logger.Configuration.ShouldLog(LogLevel.Debug, LogChannel.Physics))
   {
       // Only perform expensive operations if the log will actually be output
       var detailedState = CalculateDetailedPhysicsState();
       Logger.Debug(LogChannel.Physics, $"Physics state: {detailedState}");
   }
   ```

5. **Format logs consistently**: Use consistent log formats to make logs easier to read and parse.

6. **Include context information**: Add relevant context to log messages to aid troubleshooting:

   ```csharp
   Logger.Warning(LogChannel.UI, $"Failed to load UI element '{elementName}' (ID: {elementId})");
   ```

## Troubleshooting Common Issues

### No Logs Are Being Output

1. Verify that the logging system has been initialized with a valid configuration.
2. Check that the minimum log levels are set appropriately.
3. Ensure that at least one logging target has been added to the configuration.

### Performance Issues

1. Reduce the log level in production builds.
2. Disable stack traces unless necessary.
3. Use conditional logging for expensive operations.
4. Consider using asynchronous file logging for high-volume logs.

### File Logs Growing Too Large

1. Configure appropriate rolling intervals for file targets.
2. Set maximum file sizes and implement log rotation.
3. Increase the minimum log level for file targets.

## Further Reading

For more information on the AhBearStudios-Core logging system, refer to:

- [Logger Class Documentation](../logger.md)
- [Log Messages Documentation](../logmessage.md)
- [Custom Logging Targets Guide](../customloggingtargets.md)
