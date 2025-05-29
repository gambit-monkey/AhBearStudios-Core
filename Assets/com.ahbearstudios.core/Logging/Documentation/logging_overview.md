# AhBearStudios Core Logging System

## 🎯 Overview

The AhBearStudios Core Logging System is a high-performance, modular logging framework designed specifically for Unity applications. It provides comprehensive logging capabilities with support for both managed and unmanaged code, Burst compilation compatibility, and extensive customization options.

## ✨ Key Features

- **🚀 High Performance**: Optimized for minimal GC allocations and maximum throughput
- **⚡ Burst Compatible**: Full support for Unity's Burst compiler in job systems
- **🔧 Highly Configurable**: Flexible configuration system with runtime adjustments
- **📊 Structured Logging**: Support for contextual data and structured log messages
- **🎯 Channel-Based Organization**: Categorize logs by domain or feature
- **📁 Multiple Output Targets**: Console, file, custom destinations
- **🔍 Advanced Filtering**: Sophisticated filtering by level, channel, and content
- **🏷️ Attribute-Driven**: Automatic logging through method and property attributes
- **🔄 Middleware Pipeline**: Extensible processing pipeline for log transformation
- **📱 Unity Integration**: Seamless integration with Unity's logging system

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                        │
├─────────────────────────────────────────────────────────────────┤
│  Logger API  │  Attributes  │  Extensions  │   Events          │
├─────────────────────────────────────────────────────────────────┤
│           Logging Configuration & Channel Management            │
├─────────────────────────────────────────────────────────────────┤
│     Middleware Pipeline     │        Message Processing         │
├─────────────────────────────────────────────────────────────────┤
│  Formatters  │  Interceptors  │  Context  │   Message Builder  │
├─────────────────────────────────────────────────────────────────┤
│            Target System (Console, File, Custom)                │
├─────────────────────────────────────────────────────────────────┤
│              Unity Integration & Burst Compatibility            │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Basic Setup

```csharp
using AhBearStudios.Core.Logging;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Create configuration
        var config = new LoggingConfiguration
        {
            DefaultMinimumLogLevel = LogLevel.Info,
            IsStackTraceEnabled = false,
            ApplicationVersion = Application.version
        };

        // Add console output
        config.AddTarget(new ConsoleLoggingTarget
        {
            Format = new LogFormat("[{timestamp}] [{level}] {message}")
        });

        // Add file output
        config.AddTarget(new FileLoggingTarget
        {
            FilePath = Path.Combine(Application.persistentDataPath, "game.log"),
            Format = new LogFormat("{timestamp} | {level} | {channel} | {message}")
        });

        // Initialize the logging system
        Logger.Initialize(config);
        
        Logger.Info(LogChannel.Core, "Logging system initialized");
    }
}
```

### Basic Logging

```csharp
// Simple logging
Logger.Info(LogChannel.Core, "Game started successfully");
Logger.Warning(LogChannel.UI, "Button texture missing, using fallback");
Logger.Error(LogChannel.Networking, "Failed to connect to server");

// Structured logging with context
Logger.Info(LogChannel.Core, "Player joined game", new Dictionary<string, object>
{
    { "PlayerId", "player-123" },
    { "Level", 5 },
    { "JoinTime", DateTime.Now }
});

// Using format strings
Logger.Debug(LogChannel.AI, "Enemy health: {0}/{1}", currentHealth, maxHealth);
```

## 📚 Documentation Structure

| Document | Description |
|----------|-------------|
| **[Configuration Guide](logging-configuration.md)** | Complete configuration system setup and options |
| **[Logging Interfaces](logging-interfaces.md)** | Core interfaces and their implementations |
| **[Data Structures](logging-data.md)** | Log messages, levels, channels, and context |
| **[Attributes System](logging-attributes.md)** | Declarative logging through attributes |
| **[Events System](logging-events.md)** | Event-driven logging and handlers |
| **[Targets & Formatters](logging-targets.md)** | Output destinations and message formatting |
| **[Middleware & Extensions](logging-middleware.md)** | Processing pipeline and helper methods |
| **[Burst Compatibility](logging-burst.md)** | Job system and Burst compiler support |
| **[Best Practices](logging-best-practices.md)** | Performance tips and recommended patterns |
| **[Troubleshooting](logging-troubleshooting.md)** | Common issues and solutions |

## 🎯 Core Concepts

### Log Levels

The system uses hierarchical log levels for severity classification:

```csharp
public enum LogLevel
{
    Trace = 0,     // Most detailed information
    Debug = 1,     // Debugging information
    Info = 2,      // General information
    Warning = 3,   // Potential issues
    Error = 4,     // Error conditions
    Critical = 5,  // Severe errors
    None = 6       // Disable logging
}
```

### Log Channels

Organize logs by application domain:

```csharp
public enum LogChannel
{
    None,           // Default channel
    Core,           // Core system logs
    UI,             // User interface
    Audio,          // Audio system
    Networking,     // Network operations
    Physics,        // Physics simulation
    Input,          // Input handling
    AI,             // Artificial intelligence
    Performance,    // Performance monitoring
    Custom          // Custom categories
}
```

### Message Context

Add structured data to log messages:

```csharp
Logger.Error(LogChannel.Networking, "Connection failed", new Dictionary<string, object>
{
    { "ServerAddress", "game.example.com" },
    { "Port", 7777 },
    { "Timeout", 30 },
    { "AttemptNumber", 3 },
    { "LastError", "Connection timeout" }
});
```

## 🔧 Configuration Examples

### Development Configuration

```csharp
var devConfig = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Debug,
    IsStackTraceEnabled = true
};

// Verbose console output
devConfig.AddTarget(new ConsoleLoggingTarget
{
    Format = new LogFormat("[{timestamp}] [{level}] [{channel}] {message}")
});

// Detailed file logging
devConfig.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/debug.log",
    Format = new LogFormat("{timestamp} | {threadid} | {level} | {channel} | {message}\n{stacktrace}")
});
```

### Production Configuration

```csharp
var prodConfig = new LoggingConfiguration
{
    DefaultMinimumLogLevel = LogLevel.Warning,
    IsStackTraceEnabled = false
};

// Minimal console output
prodConfig.AddTarget(new ConsoleLoggingTarget
{
    MinimumLogLevel = LogLevel.Error,
    Format = new LogFormat("[{level}] {message}")
});

// Structured file logging
prodConfig.AddTarget(new FileLoggingTarget
{
    FilePath = "Logs/app.log",
    RollingInterval = RollingInterval.Day,
    Format = new LogFormat("{timestamp}|{level}|{channel}|{message}")
});

// Error-only analytics
prodConfig.AddTarget(new AnalyticsLoggingTarget
{
    MinimumLogLevel = LogLevel.Error
});
```

## 🧩 Integration Examples

### With Dependency Injection

```csharp
public class PlayerController
{
    private readonly IBurstLogger _logger;
    
    public PlayerController(IBurstLogger logger)
    {
        _logger = logger;
    }
    
    public void Move(Vector3 direction)
    {
        _logger.Debug(LogChannel.Core, $"Player moving: {direction}");
        // Movement logic...
    }
}
```

### With Attributes

```csharp
[LogClass(Channel = LogChannel.Core)]
public class GameManager
{
    [LogMethod(EntryLevel = LogLevel.Info, ExitLevel = LogLevel.Info)]
    public void StartGame()
    {
        // Automatically logs method entry and exit
        InitializeGame();
    }
    
    [LogProperty(Level = LogLevel.Debug)]
    public GameState CurrentState { get; set; }
}
```

### With Job System

```csharp
[BurstCompile]
public struct ProcessingJob : IJob
{
    public UnsafeLogQueue.Writer LogWriter;
    public IBurstLogger Logger;
    
    public void Execute()
    {
        Logger.Info(ref LogWriter, new FixedString128Bytes("Job started"));
        
        // Processing logic...
        
        Logger.Info(ref LogWriter, new FixedString128Bytes("Job completed"));
    }
}
```

## 🎯 Use Cases

### Game Development

```csharp
// Player actions
Logger.Info(LogChannel.Core, "Player {0} collected {1} coins", playerId, coinCount);

// Performance monitoring
Logger.Debug(LogChannel.Performance, "Frame time: {0:F2}ms", deltaTime * 1000);

// Error handling
try
{
    LoadLevel(levelId);
}
catch (Exception ex)
{
    Logger.Error(LogChannel.Core, "Failed to load level {0}: {1}", levelId, ex.Message);
}
```

### Network Debugging

```csharp
// Connection events
Logger.Info(LogChannel.Networking, "Client connected", new Dictionary<string, object>
{
    { "ClientId", clientId },
    { "RemoteEndpoint", remoteEndpoint },
    { "ConnectionTime", DateTime.Now }
});

// Data transfer
Logger.Debug(LogChannel.Networking, "Packet sent: {0} bytes", packetSize);

// Network errors
Logger.Warning(LogChannel.Networking, "Packet loss detected: {0}%", lossPercentage);
```

### Performance Analysis

```csharp
using (var scope = Logger.BeginScope(LogChannel.Performance, "LevelGeneration"))
{
    GenerateTerrain();
    GenerateObjects();
    GenerateLighting();
    
    // Automatically logs execution time when scope is disposed
}
```

## 🔍 Advanced Features

### Conditional Logging

```csharp
// Only evaluate expensive operations when logging is enabled
if (Logger.IsEnabled(LogLevel.Debug, LogChannel.AI))
{
    var aiState = GenerateDetailedAIReport(); // Expensive operation
    Logger.Debug(LogChannel.AI, "AI State: {0}", aiState);
}
```

### Context Scoping

```csharp
// Set context for all logs in this scope
using (LogContext.BeginScope("RequestId", requestId))
{
    ProcessUserRequest(); // All logs will include RequestId
    
    using (LogContext.BeginScope("UserId", userId))
    {
        UpdateUserData(); // Logs include both RequestId and UserId
    }
}
```

### Custom Middleware

```csharp
public class SensitiveDataRedactionMiddleware : ILogMiddleware
{
    public bool Process(ref LogMessage message)
    {
        // Redact sensitive information
        message.Text = RedactCreditCards(message.Text);
        message.Text = RedactPasswords(message.Text);
        return true; // Continue processing
    }
}
```

## 📊 Performance Characteristics

- **Allocation-Free**: Zero GC allocations for most common operations
- **Burst Compatible**: Full support in Unity Job System
- **Async Processing**: Non-blocking log output options
- **Efficient Filtering**: Early filtering prevents unnecessary work
- **Pooled Objects**: Object pooling for high-frequency logging

## 🔗 Related Systems

The logging system integrates with other AhBearStudios Core components:

- **Message Bus**: Event-driven architecture support
- **Profiling**: Performance measurement integration
- **Dependency Injection**: Service container integration
- **Object Pooling**: Memory management optimization

## 📝 License

MIT License - See LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please read the contributing guidelines and submit pull requests for any improvements.

---

**Next Steps**: Start with the [Configuration Guide](logging-configuration.md) to set up your logging system, or jump to [Best Practices](logging-best-practices.md) for optimization tips.