# Logging Configuration Guide

## 📋 Table of Contents

- [Overview](#overview)
- [LoggingConfiguration Class](#loggingconfiguration-class)
- [Log Levels](#log-levels)
- [Log Channels](#log-channels)
- [Logging Targets](#logging-targets)
- [Log Formatting](#log-formatting)
- [Configuration Examples](#configuration-examples)
- [Runtime Configuration](#runtime-configuration)
- [JSON Configuration](#json-configuration)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

The AhBearStudios Core Logging Configuration system provides comprehensive control over how your application logs are processed, filtered, formatted, and output. This flexible system allows you to customize logging behavior for different environments (development, testing, production) and use cases.

### Key Configuration Components

- **LoggingConfiguration**: Central configuration object
- **LogLevel**: Hierarchical severity system
- **LogChannel**: Categorical organization system
- **LoggingTargets**: Output destinations
- **LogFormat**: Message formatting templates

## 🏗️ LoggingConfiguration Class

The `LoggingConfiguration` class serves as the central hub for all logging settings and behavior.

### Core Properties

```csharp
public class LoggingConfiguration
{
    // Target Management
    public List<LoggingTarget> LoggingTargets { get; }
    
    // Level Configuration
    public LogLevel DefaultMinimumLogLevel { get; set; }
    public Dictionary<LogChannel, LogLevel> ChannelMinimumLogLevels { get; }
    
    // Formatting
    public LogFormat DefaultLogFormat { get; set; }
    
    // Feature Flags
    public bool IsStackTraceEnabled { get; set; }
    public bool IncludeTimestamps { get; set; }
    public bool IncludeThreadInfo { get; set; }
    
    // Application Info
    public string ApplicationVersion { get; set; }
    public string ApplicationName { get; set; }
}
```

### Essential Methods

```csharp
// Target Management
config.AddTarget(LoggingTarget target);
config.RemoveTarget(LoggingTarget target);
config.ClearTargets();

// Channel Configuration
config.SetChannelLogLevel(LogChannel channel, LogLevel level);
config.GetChannelLogLevel(LogChannel channel);

// Filtering Logic
config.ShouldLog(LogLevel level, LogChannel channel);
config.IsEnabled(LogLevel level, LogChannel channel);
```

### Basic Setup Example

```csharp
var config = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Info,
    IsStackTraceEnabled = false,
    IncludeTimestamps = true,
    IncludeThreadInfo = false,
    ApplicationVersion = Application.version,
    ApplicationName = "My Unity Game"
};

// Initialize the logging system
Logger.Initialize(config);
```

## 📊 Log Levels

Log levels define message severity and provide hierarchical filtering capabilities.

### Level Hierarchy

```csharp
public enum LogLevel
{
    Trace = 0,     // Most detailed information (method entry/exit, variable values)
    Debug = 1,     // Debugging information (algorithm steps, state changes)
    Info = 2,      // General information (application flow, major events)
    Warning = 3,   // Potential issues (deprecated usage, fallback behavior)
    Error = 4,     // Error conditions (handled exceptions, failed operations)
    Critical = 5,  // Severe errors (unhandled exceptions, system failures)
    None = 6       // Disable all logging
}
```

### Level Configuration Examples

```csharp
// Set global minimum level
config.DefaultMinimumLogLevel = LogLevel.Warning;

// Channel-specific levels
config.SetChannelLogLevel(LogChannel.Core, LogLevel.Info);
config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Debug);
config.SetChannelLogLevel(LogChannel.UI, LogLevel.Warning);
config.SetChannelLogLevel(LogChannel.Performance, LogLevel.Trace);

// Check if logging is enabled
if (config.ShouldLog(LogLevel.Debug, LogChannel.AI))
{
    // Expensive debug operation
    var debugInfo = GenerateAIDebugReport();
    Logger.Debug(LogChannel.AI, debugInfo);
}
```

### Filtering Behavior

When you set a minimum log level, only messages at that level or higher will be processed:

```csharp
config.DefaultMinimumLogLevel = LogLevel.Warning;

// These will be logged
Logger.Warning(LogChannel.Core, "This will appear");
Logger.Error(LogChannel.Core, "This will appear");
Logger.Critical(LogChannel.Core, "This will appear");

// These will be filtered out
Logger.Trace(LogChannel.Core, "This will NOT appear");
Logger.Debug(LogChannel.Core, "This will NOT appear");
Logger.Info(LogChannel.Core, "This will NOT appear");
```

## 📁 Log Channels

Channels provide categorical organization for logs, enabling domain-specific filtering and processing.

### Built-in Channels

```csharp
public enum LogChannel
{
    None,           // Default/unspecified channel
    Core,           // Core game systems and engine
    UI,             // User interface and HUD
    Audio,          // Audio system and sound effects
    Networking,     // Network communication and multiplayer
    Physics,        // Physics simulation and collision
    Input,          // Input handling and player actions
    AI,             // Artificial intelligence and NPCs
    Performance,    // Performance monitoring and profiling
    Custom          // User-defined custom categories
}
```

### Channel Configuration

```csharp
// Development: Verbose networking logs
config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Trace);

// Production: Only show UI warnings and errors
config.SetChannelLogLevel(LogChannel.UI, LogLevel.Warning);

// Performance monitoring: Capture all performance data
config.SetChannelLogLevel(LogChannel.Performance, LogLevel.Trace);

// AI debugging: Detailed AI logs during development
config.SetChannelLogLevel(LogChannel.AI, LogLevel.Debug);
```

### Custom Channel Usage

```csharp
// Using built-in channels
Logger.Info(LogChannel.Core, "Game initialized");
Logger.Debug(LogChannel.Networking, "Connected to server");
Logger.Warning(LogChannel.UI, "Missing texture, using fallback");

// Using None channel for general messages
Logger.Info(LogChannel.None, "Application started");

// Custom categorization using context
Logger.Info(LogChannel.Custom, "Custom system event", new Dictionary<string, object>
{
    { "Subsystem", "Inventory" },
    { "Event", "ItemAdded" },
    { "ItemId", "sword_001" }
});
```

## 🎯 Logging Targets

Targets define where log messages are sent. The system supports multiple simultaneous targets with individual configuration.

### Built-in Target Types

#### ConsoleLoggingTarget

Outputs to Unity's console and debug log.

```csharp
var consoleTarget = new ConsoleLoggingTarget
{
    Name = "Console",
    MinimumLogLevel = LogLevel.Debug,
    Format = new LogFormat("[{timestamp}] [{level}] {message}"),
    ColorizeOutput = true,  // Use colors in supported consoles
    IncludeStackTrace = false
};

config.AddTarget(consoleTarget);
```

#### FileLoggingTarget

Writes logs to files with rotation and archiving support.

```csharp
var fileTarget = new FileLoggingTarget
{
    Name = "MainLog",
    FilePath = Path.Combine(Application.persistentDataPath, "Logs", "app.log"),
    MinimumLogLevel = LogLevel.Info,
    Format = new LogFormat("{timestamp} | {level} | {channel} | {message}"),
    
    // File Management
    RollingInterval = RollingInterval.Day,
    MaxFileSize = 10 * 1024 * 1024, // 10 MB
    MaxArchiveFiles = 7,             // Keep 7 days of logs
    CreateDirectoryIfNotExists = true,
    
    // Performance Options
    BufferSize = 4096,
    AutoFlush = false,
    FlushInterval = TimeSpan.FromSeconds(5)
};

config.AddTarget(fileTarget);
```

#### CustomLoggingTarget

Base class for implementing custom targets.

```csharp
public class NetworkLoggingTarget : CustomLoggingTarget
{
    private readonly string _serverUrl;
    private readonly HttpClient _httpClient;
    
    public NetworkLoggingTarget(string serverUrl)
    {
        _serverUrl = serverUrl;
        _httpClient = new HttpClient();
        Name = "NetworkLog";
        MinimumLogLevel = LogLevel.Warning; // Only send warnings and above
    }
    
    public override void Write(LogMessage message)
    {
        if (!ShouldLog(message.Level))
            return;
            
        var logData = new
        {
            timestamp = message.Timestamp,
            level = message.Level.ToString(),
            channel = message.Channel.ToString(),
            message = message.Text,
            application = ApplicationName,
            version = ApplicationVersion
        };
        
        var json = JsonUtility.ToJson(logData);
        
        // Send asynchronously to avoid blocking
        _ = Task.Run(async () =>
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(_serverUrl, content);
            }
            catch (Exception ex)
            {
                // Handle network errors (could log to a fallback target)
                UnityEngine.Debug.LogError($"Failed to send log to server: {ex.Message}");
            }
        });
    }
}

// Usage
config.AddTarget(new NetworkLoggingTarget("https://logs.mycompany.com/api/logs"));
```

### Target Configuration Examples

```csharp
// Multiple targets with different purposes
var config = new LoggingConfiguration();

// Console: Show info and above, with colors
config.AddTarget(new ConsoleLoggingTarget
{
    MinimumLogLevel = LogLevel.Info,
    Format = new LogFormat("[{level}] {message}"),
    ColorizeOutput = true
});

// File: Comprehensive logging with rotation
config.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/comprehensive.log",
    MinimumLogLevel = LogLevel.Debug,
    Format = new LogFormat("{timestamp} | {threadid} | {level} | {channel} | {message}"),
    RollingInterval = RollingInterval.Day,
    MaxFileSize = 50 * 1024 * 1024 // 50 MB
});

// Error file: Only errors and critical issues
config.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/errors.log",
    MinimumLogLevel = LogLevel.Error,
    Format = new LogFormat("{timestamp} | {level} | {channel} | {message}\n{stacktrace}"),
    IncludeStackTrace = true
});

// Performance file: Only performance channel
config.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/performance.log",
    MinimumLogLevel = LogLevel.Trace,
    Format = new LogFormat("{timestamp} | {message}"),
    ChannelFilter = LogChannel.Performance // Only performance logs
});
```

## 🎨 Log Formatting

The `LogFormat` class controls how log messages are structured and presented.

### Available Placeholders

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{timestamp}` | Message creation time | `2024-01-15 14:30:25.123` |
| `{level}` | Log level | `INFO`, `WARNING`, `ERROR` |
| `{channel}` | Log channel | `Core`, `Networking`, `UI` |
| `{message}` | Actual log message | `Player health changed` |
| `{logger}` | Logger name/source | `PlayerController` |
| `{stacktrace}` | Stack trace (if enabled) | `at PlayerController.TakeDamage()...` |
| `{appversion}` | Application version | `1.2.3` |
| `{threadid}` | Thread identifier | `1`, `23`, `WorkerThread` |
| `{threadname}` | Thread name | `Main Thread`, `Network Thread` |

### Format Examples

```csharp
// Simple format for console output
var simpleFormat = new LogFormat("[{level}] {message}");
// Output: [INFO] Player joined the game

// Detailed format for file logging
var detailedFormat = new LogFormat("[{timestamp}] [{threadid}] [{level}] [{channel}] {message}");
// Output: [2024-01-15 14:30:25.123] [1] [INFO] [Core] Player joined the game

// Structured format for parsing
var parsableFormat = new LogFormat("{timestamp}|{level}|{channel}|{message}");
// Output: 2024-01-15 14:30:25.123|INFO|Core|Player joined the game

// Debug format with stack trace
var debugFormat = new LogFormat(
    template: "[{timestamp}] [{level}] {message}\nStack: {stacktrace}",
    timestampFormat: "yyyy-MM-dd HH:mm:ss.fff"
);

// Custom timestamp formatting
var customTimestampFormat = new LogFormat(
    template: "[{timestamp}] {message}",
    timestampFormat: "HH:mm:ss"
);
// Output: [14:30:25] Player joined the game
```

### Target-Specific Formatting

```csharp
// Console: Clean, readable format
config.AddTarget(new ConsoleLoggingTarget
{
    Format = new LogFormat("[{level}] {message}")
});

// Development file: Comprehensive information
config.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/debug.log",
    Format = new LogFormat("{timestamp} | {threadname} | {level} | {channel} | {message}\n{stacktrace}")
});

// Production file: Structured for log analysis tools
config.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/production.log",
    Format = new LogFormat("{timestamp}|{appversion}|{level}|{channel}|{message}")
});

// Analytics: JSON-like format
config.AddTarget(new CustomLoggingTarget
{
    Format = new LogFormat("{{\"timestamp\":\"{timestamp}\",\"level\":\"{level}\",\"message\":\"{message}\"}}")
});
```

## 🛠️ Configuration Examples

### Development Configuration

Optimized for debugging and detailed information:

```csharp
public static LoggingConfiguration CreateDevelopmentConfig()
{
    var config = new LoggingConfiguration
    {
        DefaultMinimumLogLevel = LogLevel.Debug,
        IsStackTraceEnabled = true,
        IncludeTimestamps = true,
        IncludeThreadInfo = true,
        ApplicationVersion = Application.version
    };
    
    // Verbose console output
    config.AddTarget(new ConsoleLoggingTarget
    {
        MinimumLogLevel = LogLevel.Debug,
        Format = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}"),
        ColorizeOutput = true
    });
    
    // Comprehensive file logging
    config.AddTarget(new FileLoggingTarget
    {
        FilePath = "Logs/development.log",
        MinimumLogLevel = LogLevel.Trace,
        Format = new LogFormat("{timestamp} | {threadname} | {level} | {channel} | {message}\n{stacktrace}"),
        RollingInterval = RollingInterval.Hour, // Frequent rotation for development
        IncludeStackTrace = true
    });
    
    // Channel-specific debugging
    config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Trace);
    config.SetChannelLogLevel(LogChannel.AI, LogLevel.Debug);
    config.SetChannelLogLevel(LogChannel.Performance, LogLevel.Trace);
    
    return config;
}
```

### Production Configuration

Optimized for performance and essential information:

```csharp
public static LoggingConfiguration CreateProductionConfig()
{
    var config = new LoggingConfiguration
    {
        DefaultMinimumLogLevel = LogLevel.Warning,
        IsStackTraceEnabled = false,
        IncludeTimestamps = true,
        IncludeThreadInfo = false,
        ApplicationVersion = Application.version
    };
    
    // Minimal console output
    config.AddTarget(new ConsoleLoggingTarget
    {
        MinimumLogLevel = LogLevel.Error,
        Format = new LogFormat("[{level}] {message}"),
        ColorizeOutput = false
    });
    
    // Structured file logging
    config.AddTarget(new FileLoggingTarget
    {
        FilePath = Path.Combine(Application.persistentDataPath, "Logs", "app.log"),
        MinimumLogLevel = LogLevel.Warning,
        Format = new LogFormat("{timestamp}|{level}|{channel}|{message}"),
        RollingInterval = RollingInterval.Day,
        MaxFileSize = 10 * 1024 * 1024, // 10 MB
        MaxArchiveFiles = 30 // Keep 30 days
    });
    
    // Error-only file for critical issues
    config.AddTarget(new FileLoggingTarget
    {
        FilePath = Path.Combine(Application.persistentDataPath, "Logs", "errors.log"),
        MinimumLogLevel = LogLevel.Error,
        Format = new LogFormat("{timestamp} | {level} | {channel} | {message}\n{stacktrace}"),
        IncludeStackTrace = true
    });
    
    // Channel-specific production levels
    config.SetChannelLogLevel(LogChannel.Performance, LogLevel.Info);
    config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Warning);
    
    return config;
}
```

### Testing Configuration

Optimized for test scenarios and result verification:

```csharp
public static LoggingConfiguration CreateTestConfig()
{
    var config = new LoggingConfiguration
    {
        DefaultMinimumLogLevel = LogLevel.Info,
        IsStackTraceEnabled = false,
        IncludeTimestamps = false, // Consistent output for test comparison
        IncludeThreadInfo = false
    };
    
    // Memory target for test verification
    config.AddTarget(new MemoryLoggingTarget
    {
        MinimumLogLevel = LogLevel.Info,
        Format = new LogFormat("[{level}] {message}"),
        MaxEntries = 1000
    });
    
    // Optional file output for test debugging
    if (Environment.GetEnvironmentVariable("LOG_TESTS") == "true")
    {
        config.AddTarget(new FileLoggingTarget
        {
            FilePath = "Logs/tests.log",
            MinimumLogLevel = LogLevel.Debug,
            Format = new LogFormat("{timestamp} | {level} | {message}"),
            RollingInterval = RollingInterval.Never
        });
    }
    
    return config;
}
```

## 🔄 Runtime Configuration

The logging system supports dynamic configuration changes without restarting the application.

### Dynamic Level Adjustment

```csharp
public class LoggingManager : MonoBehaviour
{
    [Header("Runtime Configuration")]
    [SerializeField] private LogLevel _globalLogLevel = LogLevel.Info;
    [SerializeField] private LogLevel _networkingLogLevel = LogLevel.Warning;
    [SerializeField] private LogLevel _aiLogLevel = LogLevel.Info;
    [SerializeField] private bool _enableStackTraces = false;
    
    private LoggingConfiguration _config;
    
    private void Start()
    {
        _config = Logger.Configuration;
        ApplyConfiguration();
    }
    
    private void OnValidate()
    {
        if (_config != null && Application.isPlaying)
        {
            ApplyConfiguration();
        }
    }
    
    private void ApplyConfiguration()
    {
        _config.DefaultMinimumLogLevel = _globalLogLevel;
        _config.SetChannelLogLevel(LogChannel.Networking, _networkingLogLevel);
        _config.SetChannelLogLevel(LogChannel.AI, _aiLogLevel);
        _config.IsStackTraceEnabled = _enableStackTraces;
        
        Logger.Info(LogChannel.Core, "Logging configuration updated");
    }
    
    // Runtime methods for external control
    public void SetGlobalLogLevel(LogLevel level)
    {
        _globalLogLevel = level;
        _config.DefaultMinimumLogLevel = level;
    }
    
    public void EnableVerboseLogging()
    {
        SetGlobalLogLevel(LogLevel.Debug);
        _config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Trace);
        _config.SetChannelLogLevel(LogChannel.AI, LogLevel.Debug);
        _config.IsStackTraceEnabled = true;
    }
    
    public void EnableProductionLogging()
    {
        SetGlobalLogLevel(LogLevel.Warning);
        _config.SetChannelLogLevel(LogChannel.Networking, LogLevel.Error);
        _config.SetChannelLogLevel(LogChannel.AI, LogLevel.Warning);
        _config.IsStackTraceEnabled = false;
    }
}
```

### Dynamic Target Management

```csharp
public class DynamicLoggingController
{
    private LoggingConfiguration _config;
    private FileLoggingTarget _debugFileTarget;
    private NetworkLoggingTarget _remoteTarget;
    
    public void Initialize(LoggingConfiguration config)
    {
        _config = config;
    }
    
    public void EnableDebugFileLogging(string filePath)
    {
        if (_debugFileTarget == null)
        {
            _debugFileTarget = new FileLoggingTarget
            {
                FilePath = filePath,
                MinimumLogLevel = LogLevel.Debug,
                Format = new LogFormat("{timestamp} | {level} | {channel} | {message}")
            };
            _config.AddTarget(_debugFileTarget);
            Logger.Info(LogChannel.Core, "Debug file logging enabled: " + filePath);
        }
    }
    
    public void DisableDebugFileLogging()
    {
        if (_debugFileTarget != null)
        {
            _config.RemoveTarget(_debugFileTarget);
            _debugFileTarget.Dispose();
            _debugFileTarget = null;
            Logger.Info(LogChannel.Core, "Debug file logging disabled");
        }
    }
    
    public void EnableRemoteLogging(string serverUrl)
    {
        if (_remoteTarget == null)
        {
            _remoteTarget = new NetworkLoggingTarget(serverUrl)
            {
                MinimumLogLevel = LogLevel.Warning
            };
            _config.AddTarget(_remoteTarget);
            Logger.Info(LogChannel.Core, "Remote logging enabled: " + serverUrl);
        }
    }
    
    public void DisableRemoteLogging()
    {
        if (_remoteTarget != null)
        {
            _config.RemoveTarget(_remoteTarget);
            _remoteTarget.Dispose();
            _remoteTarget = null;
            Logger.Info(LogChannel.Core, "Remote logging disabled");
        }
    }
}
```

## 📄 JSON Configuration

Load and save logging configurations from JSON files for easy management.

### Configuration Schema

```json
{
  "defaultMinimumLogLevel": "Info",
  "isStackTraceEnabled": true,
  "includeTimestamps": true,
  "includeThreadInfo": false,
  "applicationVersion": "1.0.0",
  "applicationName": "My Unity Game",
  "channelLogLevels": {
    "Core": "Debug",
    "Networking": "Info",
    "UI": "Warning",
    "AI": "Debug",
    "Performance": "Trace"
  },
  "targets": [
    {
      "type": "Console",
      "name": "MainConsole",
      "minimumLogLevel": "Info",
      "format": {
        "template": "[{timestamp}] [{level}] {message}",
        "timestampFormat": "HH:mm:ss.fff"
      },
      "colorizeOutput": true
    },
    {
      "type": "File",
      "name": "ApplicationLog",
      "minimumLogLevel": "Debug",
      "filePath": "Logs/app.log",
      "format": {
        "template": "{timestamp} | {level} | {channel} | {message}",
        "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
      },
      "rollingInterval": "Day",
      "maxFileSize": 10485760,
      "maxArchiveFiles": 7,
      "autoFlush": false,
      "bufferSize": 4096
    },
    {
      "type": "File",
      "name": "ErrorLog",
      "minimumLogLevel": "Error",
      "filePath": "Logs/errors.log",
      "format": {
        "template": "{timestamp} | {level} | {channel} | {message}\n{stacktrace}",
        "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
      },
      "includeStackTrace": true,
      "rollingInterval": "Week"
    }
  ]
}
```

### JSON Loading Implementation

```csharp
public static class LoggingConfigurationLoader
{
    [System.Serializable]
    public class ConfigurationData
    {
        public string defaultMinimumLogLevel = "Info";
        public bool isStackTraceEnabled = false;
        public bool includeTimestamps = true;
        public bool includeThreadInfo = false;
        public string applicationVersion = "1.0.0";
        public string applicationName = "Unity Application";
        public Dictionary<string, string> channelLogLevels = new Dictionary<string, string>();
        public TargetData[] targets = new TargetData[0];
    }
    
    [System.Serializable]
    public class TargetData
    {
        public string type;
        public string name;
        public string minimumLogLevel = "Info";
        public FormatData format;
        public string filePath;
        public string rollingInterval = "Day";
        public long maxFileSize = 10485760; // 10 MB
        public int maxArchiveFiles = 7;
        public bool autoFlush = false;
        public int bufferSize = 4096;
        public bool colorizeOutput = true;
        public bool includeStackTrace = false;
    }
    
    [System.Serializable]
    public class FormatData
    {
        public string template = "[{timestamp}] [{level}] {message}";
        public string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }
    
    public static LoggingConfiguration LoadFromJson(string jsonPath)
    {
        try
        {
            string json = File.ReadAllText(jsonPath);
            return LoadFromJsonString(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load logging configuration from {jsonPath}: {ex.Message}");
            return CreateDefaultConfiguration();
        }
    }
    
    public static LoggingConfiguration LoadFromJsonString(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<ConfigurationData>(json);
            return CreateConfigurationFromData(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse logging configuration JSON: {ex.Message}");
            return CreateDefaultConfiguration();
        }
    }
    
    private static LoggingConfiguration CreateConfigurationFromData(ConfigurationData data)
    {
        var config = new LoggingConfiguration
        {
            DefaultMinimumLogLevel = ParseLogLevel(data.defaultMinimumLogLevel),
            IsStackTraceEnabled = data.isStackTraceEnabled,
            IncludeTimestamps = data.includeTimestamps,
            IncludeThreadInfo = data.includeThreadInfo,
            ApplicationVersion = data.applicationVersion,
            ApplicationName = data.applicationName
        };
        
        // Set channel-specific log levels
        foreach (var channelLevel in data.channelLogLevels)
        {
            if (Enum.TryParse<LogChannel>(channelLevel.Key, out var channel))
            {
                config.SetChannelLogLevel(channel, ParseLogLevel(channelLevel.Value));
            }
        }
        
        // Create targets
        foreach (var targetData in data.targets)
        {
            var target = CreateTarget(targetData);
            if (target != null)
            {
                config.AddTarget(target);
            }
        }
        
        return config;
    }
    
    private static LoggingTarget CreateTarget(TargetData data)
    {
        var format = new LogFormat(
            data.format?.template ?? "[{timestamp}] [{level}] {message}",
            data.format?.timestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff"
        );
        
        LoggingTarget target = null;
        
        switch (data.type?.ToLowerInvariant())
        {
            case "console":
                target = new ConsoleLoggingTarget
                {
                    ColorizeOutput = data.colorizeOutput
                };
                break;
                
            case "file":
                target = new FileLoggingTarget
                {
                    FilePath = data.filePath,
                    RollingInterval = ParseRollingInterval(data.rollingInterval),
                    MaxFileSize = data.maxFileSize,
                    MaxArchiveFiles = data.maxArchiveFiles,
                    AutoFlush = data.autoFlush,
                    BufferSize = data.bufferSize,
                    IncludeStackTrace = data.includeStackTrace
                };
                break;
                
            default:
                Debug.LogWarning($"Unknown target type: {data.type}");
                return null;
        }
        
        if (target != null)
        {
            target.Name = data.name;
            target.MinimumLogLevel = ParseLogLevel(data.minimumLogLevel);
            target.Format = format;
        }
        
        return target;
    }
    
    private static LogLevel ParseLogLevel(string levelString)
    {
        if (Enum.TryParse<LogLevel>(levelString, true, out var level))
        {
            return level;
        }
        
        Debug.LogWarning($"Unknown log level: {levelString}, using Info");
        return LogLevel.Info;
    }
    
    private static RollingInterval ParseRollingInterval(string intervalString)
    {
        if (Enum.TryParse<RollingInterval>(intervalString, true, out var interval))
        {
            return interval;
        }
        
        Debug.LogWarning($"Unknown rolling interval: {intervalString}, using Day");
        return RollingInterval.Day;
    }
    
    public static LoggingConfiguration CreateDefaultConfiguration()
    {
        var config = new LoggingConfiguration
        {
            DefaultMinimumLogLevel = LogLevel.Info,
            IsStackTraceEnabled = false,
            ApplicationVersion = Application.version
        };
        
        config.AddTarget(new ConsoleLoggingTarget
        {
            Format = new LogFormat("[{timestamp}] [{level}] {message}")
        });
        
        return config;
    }
    
    public static void SaveToJson(LoggingConfiguration config, string jsonPath)
    {
        try
        {
            var data = CreateDataFromConfiguration(config);
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(jsonPath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save logging configuration to {jsonPath}: {ex.Message}");
        }
    }
    
    private static ConfigurationData CreateDataFromConfiguration(LoggingConfiguration config)
    {
        var data = new ConfigurationData
        {
            defaultMinimumLogLevel = config.DefaultMinimumLogLevel.ToString(),
            isStackTraceEnabled = config.IsStackTraceEnabled,
            includeTimestamps = config.IncludeTimestamps,
            includeThreadInfo = config.IncludeThreadInfo,
            applicationVersion = config.ApplicationVersion,
            applicationName = config.ApplicationName
        };
        
        // Channel log levels
        foreach (var channelLevel in config.ChannelMinimumLogLevels)
        {
            data.channelLogLevels[channelLevel.Key.ToString()] = channelLevel.Value.ToString();
        }
        
        // Targets
        var targetList = new List<TargetData>();
        foreach (var target in config.LoggingTargets)
        {
            var targetData = CreateTargetData(target);
            if (targetData != null)
            {
                targetList.Add(targetData);
            }
        }
        data.targets = targetList.ToArray();
        
        return data;
    }
    
    private static TargetData CreateTargetData(LoggingTarget target)
    {
        var data = new TargetData
        {
            name = target.Name,
            minimumLogLevel = target.MinimumLogLevel.ToString(),
            format = new FormatData
            {
                template = target.Format.Template,
                timestampFormat = target.Format.TimestampFormat
            }
        };
        
        switch (target)
        {
            case ConsoleLoggingTarget consoleTarget:
                data.type = "Console";
                data.colorizeOutput = consoleTarget.ColorizeOutput;
                break;
                
            case FileLoggingTarget fileTarget:
                data.type = "File";
                data.filePath = fileTarget.FilePath;
                data.rollingInterval = fileTarget.RollingInterval.ToString();
                data.maxFileSize = fileTarget.MaxFileSize;
                data.maxArchiveFiles = fileTarget.MaxArchiveFiles;
                data.autoFlush = fileTarget.AutoFlush;
                data.bufferSize = fileTarget.BufferSize;
                data.includeStackTrace = fileTarget.IncludeStackTrace;
                break;
                
            default:
                return null; // Unknown target type
        }
        
        return data;
    }
}
```

### Usage Examples

```csharp
// Load configuration from file
var config = LoggingConfigurationLoader.LoadFromJson("Configs/logging.json");
Logger.Initialize(config);

// Save current configuration
LoggingConfigurationLoader.SaveToJson(Logger.Configuration, "Configs/current-logging.json");

// Load from embedded resource
var jsonAsset = Resources.Load<TextAsset>("logging-config");
var config = LoggingConfigurationLoader.LoadFromJsonString(jsonAsset.text);
```

## 📋 Best Practices

### 1. Environment-Specific Configurations

```csharp
public static class LoggingBootstrap
{
    public static void Initialize()
    {
        LoggingConfiguration config;
        
        #if DEVELOPMENT_BUILD || UNITY_EDITOR
            config = CreateDevelopmentConfig();
        #elif UNITY_ANDROID || UNITY_IOS
            config = CreateMobileConfig();
        #else
            config = CreateProductionConfig();
        #endif
        
        Logger.Initialize(config);
    }
    
    private static LoggingConfiguration CreateMobileConfig()
    {
        var config = new LoggingConfiguration
        {
            DefaultMinimumLogLevel = LogLevel.Warning,
            IsStackTraceEnabled = false,
            IncludeTimestamps = true
        };
        
        // Mobile: Minimal console output
        config.AddTarget(new ConsoleLoggingTarget
        {
            MinimumLogLevel = LogLevel.Error,
            Format = new LogFormat("[{level}] {message}")
        });
        
        // Mobile: Compact file logging
        config.AddTarget(new FileLoggingTarget
        {
            FilePath = Path.Combine(Application.persistentDataPath, "app.log"),
            MinimumLogLevel = LogLevel.Warning,
            Format = new LogFormat("{timestamp}|{level}|{message}"),
            MaxFileSize = 1 * 1024 * 1024, // 1 MB - smaller for mobile
            MaxArchiveFiles = 3 // Keep fewer files on mobile
        });
        
        return config;
    }
}
```

### 2. Performance Optimization

```csharp
// Use conditional compilation for expensive logging
public static class PerformanceLogger
{
    [System.Diagnostics.Conditional("ENABLE_PERFORMANCE_LOGGING")]
    public static void LogPerformance(string message, params object[] args)
    {
        if (Logger.IsEnabled(LogLevel.Debug, LogChannel.Performance))
        {
            Logger.Debug(LogChannel.Performance, string.Format(message, args));
        }
    }
    
    [System.Diagnostics.Conditional("ENABLE_DETAILED_LOGGING")]
    public static void LogDetailed(LogChannel channel, string message, object context = null)
    {
        if (Logger.IsEnabled(LogLevel.Trace, channel))
        {
            Logger.Trace(channel, message, context);
        }
    }
}
```

### 3. Configuration Validation

```csharp
public static class ConfigurationValidator
{
    public static bool ValidateConfiguration(LoggingConfiguration config, out List<string> errors)
    {
        errors = new List<string>();
        
        // Check for at least one target
        if (config.LoggingTargets.Count == 0)
        {
            errors.Add("Configuration must have at least one logging target");
        }
        
        // Validate file targets
        foreach (var target in config.LoggingTargets.OfType<FileLoggingTarget>())
        {
            if (string.IsNullOrEmpty(target.FilePath))
            {
                errors.Add($"File target '{target.Name}' has no file path specified");
            }
            
            try
            {
                var directory = Path.GetDirectoryName(target.FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot create directory for file target '{target.Name}': {ex.Message}");
            }
        }
        
        // Check for duplicate target names
        var targetNames = config.LoggingTargets.Select(t => t.Name).Where(n => !string.IsNullOrEmpty(n));
        var duplicateNames = targetNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var duplicateName in duplicateNames)
        {
            errors.Add($"Duplicate target name: '{duplicateName}'");
        }
        
        return errors.Count == 0;
    }
    
    public static void ValidateAndApplyConfiguration(LoggingConfiguration config)
    {
        if (ValidateConfiguration(config, out var errors))
        {
            Logger.Initialize(config);
            Logger.Info(LogChannel.Core, "Logging configuration applied successfully");
        }
        else
        {
            Debug.LogError("Invalid logging configuration:");
            foreach (var error in errors)
            {
                Debug.LogError($"  - {error}");
            }
            
            // Fall back to default configuration
            Logger.Initialize(LoggingConfigurationLoader.CreateDefaultConfiguration());
            Logger.Warning(LogChannel.Core, "Using default logging configuration due to validation errors");
        }
    }
}
```

## 🐛 Troubleshooting

### Common Issues and Solutions

#### No Logs Appearing

**Problem**: No log messages are being output despite calling logging methods.

**Solutions**:
1. Verify logging system initialization:
   ```csharp
   if (!Logger.IsInitialized)
   {
       Debug.LogError("Logger not initialized - call Logger.Initialize() first");
   }
   ```

2. Check minimum log levels:
   ```csharp
   var config = Logger.Configuration;
   Debug.Log($"Global minimum level: {config.DefaultMinimumLogLevel}");
   Debug.Log($"Channel level for Core: {config.GetChannelLogLevel(LogChannel.Core)}");
   ```

3. Verify target configuration:
   ```csharp
   var config = Logger.Configuration;
   Debug.Log($"Number of targets: {config.LoggingTargets.Count}");
   foreach (var target in config.LoggingTargets)
   {
       Debug.Log($"Target: {target.Name}, MinLevel: {target.MinimumLogLevel}");
   }
   ```

#### Performance Issues

**Problem**: Logging is causing performance degradation.

**Solutions**:
1. Increase minimum log levels in production
2. Use conditional logging for expensive operations
3. Enable async file writing
4. Reduce format complexity

```csharp
// Example: Conditional expensive logging
if (Logger.IsEnabled(LogLevel.Debug, LogChannel.AI))
{
    var expensiveDebugData = GenerateAIDebugReport(); // Only if logging enabled
    Logger.Debug(LogChannel.AI, "AI State: {0}", expensiveDebugData);
}
```

#### File Logging Issues

**Problem**: File logs are not being written or files are growing too large.

**Solutions**:
1. Check file permissions and directory creation
2. Configure file rotation properly
3. Ensure proper disposal of file targets

```csharp
// Example: Robust file target setup
var logsDirectory = Path.Combine(Application.persistentDataPath, "Logs");
if (!Directory.Exists(logsDirectory))
{
    Directory.CreateDirectory(logsDirectory);
}

var fileTarget = new FileLoggingTarget
{
    FilePath = Path.Combine(logsDirectory, "app.log"),
    RollingInterval = RollingInterval.Day,
    MaxFileSize = 10 * 1024 * 1024, // 10 MB
    MaxArchiveFiles = 7,
    AutoFlush = true, // Ensure data is written
    CreateDirectoryIfNotExists = true
};
```

#### Memory Usage Issues

**Problem**: High memory usage from logging system.

**Solutions**:
1. Limit log retention
2. Use appropriate buffer sizes
3. Avoid excessive context data

```csharp
// Example: Memory-conscious configuration
var config = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Info // Reduce log volume
};

// File target with limited retention
config.AddTarget(new FileLoggingTarget
{
    FilePath = "app.log",
    MaxFileSize = 5 * 1024 * 1024, // 5 MB
    MaxArchiveFiles = 3, // Keep only 3 archive files
    BufferSize = 1024 // Smaller buffer
});
```

## 🔗 Related Documentation

- **[Logging Interfaces](logging-interfaces.md)**: Core interfaces and contracts
- **[Data Structures](logging-data.md)**: Message formats and data types
- **[Targets & Formatters](logging-targets.md)**: Output destinations and formatting
- **[Best Practices](logging-best-practices.md)**: Performance and usage guidelines

---

**Next Steps**: Once you have your configuration set up, explore the [Logging Interfaces](logging-interfaces.md) to understand the core contracts, or jump to [Best Practices](logging-best-practices.md) for optimization tips.