# Logging Data Structures Guide

## 📋 Table of Contents

- [Overview](#overview)
- [LogMessage](#logmessage)
- [LogLevel](#loglevel)
- [LogChannel](#logchannel)
- [LogFormat](#logformat)
- [LogProperties](#logproperties)
- [LogContext](#logcontext)
- [LogMessageBuilder](#logmessagebuilder)
- [Native Data Types](#native-data-types)
- [Serialization](#serialization)
- [Performance Considerations](#performance-considerations)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)

## 🎯 Overview

The AhBearStudios Core Logging system uses a comprehensive set of data structures to represent, store, and process log information. These structures are designed for high performance, Burst compatibility, and seamless integration with Unity's collections system.

### Key Design Principles

- **Burst Compatible**: All critical data structures support Burst compilation
- **Memory Efficient**: Minimal allocations and optimal memory layout
- **Strongly Typed**: Type-safe operations and compile-time validation
- **Extensible**: Support for custom properties and context data
- **Thread Safe**: Safe concurrent access patterns

## 📊 LogMessage

The `LogMessage` struct is the fundamental unit of logging data, representing a single log event with all associated information.

### Structure Definition

```csharp
[System.Serializable]
[BurstCompile]
public struct LogMessage : IMessage
{
    /// <summary>
    /// Unique identifier for this message instance
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// The main text content of the log message
    /// </summary>
    public FixedString512Bytes Text;
    
    /// <summary>
    /// Severity level of the message
    /// </summary>
    public LogLevel Level;
    
    /// <summary>
    /// Channel/category for the message
    /// </summary>
    public LogChannel Channel;
    
    /// <summary>
    /// Timestamp when the message was created (UTC ticks)
    /// </summary>
    public long TimestampTicks { get; private set; }
    
    /// <summary>
    /// Type code for message bus integration
    /// </summary>
    public ushort TypeCode { get; private set; }
    
    /// <summary>
    /// Thread ID where the message originated
    /// </summary>
    public int ThreadId;
    
    /// <summary>
    /// Source information (file, method, line number)
    /// </summary>
    public SourceInfo Source;
    
    /// <summary>
    /// Stack trace information (if enabled)
    /// </summary>
    public FixedString4096Bytes StackTrace;
    
    /// <summary>
    /// Additional context properties
    /// </summary>
    public LogProperties Properties;
}
```

### Properties and Methods

```csharp
public struct LogMessage
{
    /// <summary>
    /// Gets the timestamp as DateTime
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks);
    
    /// <summary>
    /// Gets whether this message has stack trace information
    /// </summary>
    public bool HasStackTrace => StackTrace.Length > 0;
    
    /// <summary>
    /// Gets whether this message has additional properties
    /// </summary>
    public bool HasProperties => Properties.Count > 0;
    
    /// <summary>
    /// Creates a new log message with specified parameters
    /// </summary>
    public static LogMessage Create(LogLevel level, FixedString512Bytes text, 
                                   LogChannel channel = LogChannel.None)
    {
        return new LogMessage
        {
            Id = Guid.NewGuid(),
            Level = level,
            Text = text,
            Channel = channel,
            TimestampTicks = DateTime.UtcNow.Ticks,
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            TypeCode = GetTypeCode<LogMessage>()
        };
    }
    
    /// <summary>
    /// Creates a copy of this message with modified properties
    /// </summary>
    public LogMessage WithLevel(LogLevel level)
    {
        var copy = this;
        copy.Level = level;
        return copy;
    }
    
    public LogMessage WithText(FixedString512Bytes text)
    {
        var copy = this;
        copy.Text = text;
        return copy;
    }
    
    public LogMessage WithChannel(LogChannel channel)
    {
        var copy = this;
        copy.Channel = channel;
        return copy;
    }
}
```

### Usage Examples

```csharp
// Basic message creation
var message = LogMessage.Create(LogLevel.Info, "Application started");

// Message with channel
var networkMessage = LogMessage.Create(
    LogLevel.Debug, 
    "Connection established", 
    LogChannel.Networking
);

// Message with properties
var gameMessage = LogMessage.Create(LogLevel.Info, "Player joined")
    .WithProperties(new LogProperties
    {
        { "PlayerId", "player-123" },
        { "Level", 5 },
        { "SessionTime", 45.2f }
    });

// Message modification
var modifiedMessage = message
    .WithLevel(LogLevel.Warning)
    .WithText("Application started with warnings");
```

## 📈 LogLevel

The `LogLevel` enum defines message severity in a hierarchical system that enables effective filtering.

### Enumeration Definition

```csharp
/// <summary>
/// Defines the severity level of log messages
/// </summary>
public enum LogLevel : byte
{
    /// <summary>
    /// Most detailed information, typically for step-by-step debugging
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Detailed debugging information, more focused than Trace
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// General operational information about application flow
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Potential issues that aren't immediately problematic
    /// </summary>
    Warning = 3,
    
    /// <summary>
    /// Error conditions that might allow the application to continue
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Severe errors that may cause the application to terminate
    /// </summary>
    Critical = 5,
    
    /// <summary>
    /// Used to disable all logging
    /// </summary>
    None = 6
}
```

### Extension Methods

```csharp
public static class LogLevelExtensions
{
    /// <summary>
    /// Checks if this level includes the specified level (this >= other)
    /// </summary>
    public static bool Includes(this LogLevel level, LogLevel other)
    {
        return level >= other;
    }
    
    /// <summary>
    /// Gets a short string representation of the log level
    /// </summary>
    public static string ToShortString(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            LogLevel.None => "NON",
            _ => "???"
        };
    }
    
    /// <summary>
    /// Gets the display color for the log level
    /// </summary>
    public static Color32 GetDisplayColor(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => new Color32(128, 128, 128, 255),    // Gray
            LogLevel.Debug => new Color32(0, 255, 255, 255),      // Cyan
            LogLevel.Info => new Color32(255, 255, 255, 255),     // White
            LogLevel.Warning => new Color32(255, 255, 0, 255),    // Yellow
            LogLevel.Error => new Color32(255, 0, 0, 255),        // Red
            LogLevel.Critical => new Color32(255, 0, 255, 255),   // Magenta
            _ => new Color32(255, 255, 255, 255)
        };
    }
    
    /// <summary>
    /// Parses a log level from string, case insensitive
    /// </summary>
    public static LogLevel Parse(string levelString)
    {
        if (Enum.TryParse<LogLevel>(levelString, true, out var level))
        {
            return level;
        }
        
        // Handle common aliases
        return levelString.ToLowerInvariant() switch
        {
            "verbose" => LogLevel.Trace,
            "information" => LogLevel.Info,
            "warn" => LogLevel.Warning,
            "err" => LogLevel.Error,
            "fatal" => LogLevel.Critical,
            _ => LogLevel.Info
        };
    }
    
    /// <summary>
    /// Gets the relative weight/importance of the log level
    /// </summary>
    public static float GetWeight(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => 0.1f,
            LogLevel.Debug => 0.2f,
            LogLevel.Info => 0.5f,
            LogLevel.Warning => 0.7f,
            LogLevel.Error => 0.9f,
            LogLevel.Critical => 1.0f,
            _ => 0.0f
        };
    }
}
```

### Usage Examples

```csharp
// Level comparison
if (LogLevel.Warning.Includes(LogLevel.Info))
{
    // This will be false because Warning > Info
}

if (LogLevel.Info.Includes(LogLevel.Warning))  
{
    // This will be true because Info < Warning
}

// Display formatting
var level = LogLevel.Warning;
Debug.Log($"Level: {level.ToShortString()}"); // "Level: WRN"

// Color coding in UI
var color = LogLevel.Error.GetDisplayColor(); // Red color

// Parsing from configuration
var configLevel = LogLevelExtensions.Parse("debug"); // LogLevel.Debug
var aliasLevel = LogLevelExtensions.Parse("verbose"); // LogLevel.Trace

// Weighted importance calculations
var weight = LogLevel.Critical.GetWeight(); // 1.0f
var isImportant = weight > 0.8f; // true for Error and Critical
```

## 📁 LogChannel

The `LogChannel` enum provides categorical organization for logs, enabling domain-specific filtering and routing.

### Enumeration Definition

```csharp
/// <summary>
/// Defines categories for organizing log messages by domain or feature
/// </summary>
public enum LogChannel : byte
{
    /// <summary>
    /// Default channel for unspecified logs
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Core game systems and engine functionality
    /// </summary>
    Core = 1,
    
    /// <summary>
    /// User interface and HUD systems
    /// </summary>
    UI = 2,
    
    /// <summary>
    /// Audio system, sound effects, and music
    /// </summary>
    Audio = 3,
    
    /// <summary>
    /// Network communication and multiplayer
    /// </summary>
    Networking = 4,
    
    /// <summary>
    /// Physics simulation and collision detection
    /// </summary>
    Physics = 5,
    
    /// <summary>
    /// Input handling and player actions
    /// </summary>
    Input = 6,
    
    /// <summary>
    /// Artificial intelligence and NPC behavior
    /// </summary>
    AI = 7,
    
    /// <summary>
    /// Performance monitoring and profiling
    /// </summary>
    Performance = 8,
    
    /// <summary>
    /// Custom user-defined categories
    /// </summary>
    Custom = 255
}
```

### Extension Methods

```csharp
public static class LogChannelExtensions
{
    private static readonly Dictionary<LogChannel, string> ChannelNames = new()
    {
        { LogChannel.None, "General" },
        { LogChannel.Core, "Core" },
        { LogChannel.UI, "UI" },
        { LogChannel.Audio, "Audio" },
        { LogChannel.Networking, "Network" },
        { LogChannel.Physics, "Physics" },
        { LogChannel.Input, "Input" },
        { LogChannel.AI, "AI" },
        { LogChannel.Performance, "Performance" },
        { LogChannel.Custom, "Custom" }
    };
    
    private static readonly Dictionary<LogChannel, string> ChannelIcons = new()
    {
        { LogChannel.None, "📋" },
        { LogChannel.Core, "⚙️" },
        { LogChannel.UI, "🖥️" },
        { LogChannel.Audio, "🔊" },
        { LogChannel.Networking, "🌐" },
        { LogChannel.Physics, "⚡" },
        { LogChannel.Input, "🎮" },
        { LogChannel.AI, "🤖" },
        { LogChannel.Performance, "📊" },
        { LogChannel.Custom, "🔧" }
    };
    
    /// <summary>
    /// Gets a user-friendly display name for the channel
    /// </summary>
    public static string GetDisplayName(this LogChannel channel)
    {
        return ChannelNames.GetValueOrDefault(channel, channel.ToString());
    }
    
    /// <summary>
    /// Gets an icon/emoji representation for the channel
    /// </summary>
    public static string GetIcon(this LogChannel channel)
    {
        return ChannelIcons.GetValueOrDefault(channel, "📋");
    }
    
    /// <summary>
    /// Gets the display color for the channel
    /// </summary>
    public static Color32 GetDisplayColor(this LogChannel channel)
    {
        return channel switch
        {
            LogChannel.None => new Color32(200, 200, 200, 255),      // Light Gray
            LogChannel.Core => new Color32(100, 149, 237, 255),      // Cornflower Blue
            LogChannel.UI => new Color32(138, 43, 226, 255),         // Blue Violet
            LogChannel.Audio => new Color32(255, 165, 0, 255),       // Orange
            LogChannel.Networking => new Color32(50, 205, 50, 255),  // Lime Green
            LogChannel.Physics => new Color32(255, 69, 0, 255),      // Red Orange
            LogChannel.Input => new Color32(218, 112, 214, 255),     // Orchid
            LogChannel.AI => new Color32(255, 20, 147, 255),         // Deep Pink
            LogChannel.Performance => new Color32(0, 191, 255, 255), // Deep Sky Blue
            LogChannel.Custom => new Color32(169, 169, 169, 255),    // Dark Gray
            _ => new Color32(255, 255, 255, 255)
        };
    }
    
    /// <summary>
    /// Parses a log channel from string, case insensitive
    /// </summary>
    public static LogChannel Parse(string channelString)
    {
        if (Enum.TryParse<LogChannel>(channelString, true, out var channel))
        {
            return channel;
        }
        
        // Handle common aliases
        return channelString.ToLowerInvariant() switch
        {
            "net" or "network" => LogChannel.Networking,
            "interface" or "gui" => LogChannel.UI,
            "sound" => LogChannel.Audio,
            "perf" => LogChannel.Performance,
            "artificial intelligence" => LogChannel.AI,
            _ => LogChannel.None
        };
    }
    
    /// <summary>
    /// Gets all available channels as an enumerable
    /// </summary>
    public static IEnumerable<LogChannel> GetAllChannels()
    {
        return Enum.GetValues<LogChannel>();
    }
    
    /// <summary>
    /// Checks if the channel represents a system-level category
    /// </summary>
    public static bool IsSystemChannel(this LogChannel channel)
    {
        return channel is LogChannel.Core or LogChannel.Performance;
    }
    
    /// <summary>
    /// Checks if the channel represents a gameplay category
    /// </summary>
    public static bool IsGameplayChannel(this LogChannel channel)
    {
        return channel is LogChannel.AI or LogChannel.Physics or LogChannel.Input;
    }
}
```

### Usage Examples

```csharp
// Channel display formatting
var channel = LogChannel.Networking;
Debug.Log($"{channel.GetIcon()} {channel.GetDisplayName()}"); // "🌐 Network"

// Channel color coding
var color = LogChannel.UI.GetDisplayColor(); // Blue Violet

// Parsing from configuration
var configChannel = LogChannelExtensions.Parse("net"); // LogChannel.Networking

// Channel categorization
var channels = LogChannelExtensions.GetAllChannels();
var gameplayChannels = channels.Where(c => c.IsGameplayChannel());
var systemChannels = channels.Where(c => c.IsSystemChannel());

// Conditional logging based on channel type
if (channel.IsSystemChannel())
{
    // Handle system-level logs differently
    ProcessSystemLog(message);
}
```

## 🎨 LogFormat

The `LogFormat` struct provides template-based formatting for log messages with support for various placeholders and custom formatting options.

### Structure Definition

```csharp
[System.Serializable]
public struct LogFormat
{
    /// <summary>
    /// Template string with placeholders
    /// </summary>
    public FixedString512Bytes Template;
    
    /// <summary>
    /// Format string for timestamp placeholders
    /// </summary>
    public FixedString64Bytes TimestampFormat;
    
    /// <summary>
    /// Whether to include stack trace information
    /// </summary>
    public bool IncludeStackTrace;
    
    /// <summary>
    /// Whether to include thread information
    /// </summary>
    public bool IncludeThreadInfo;
    
    /// <summary>
    /// Whether to include source information
    /// </summary>
    public bool IncludeSourceInfo;
    
    /// <summary>
    /// Maximum length of formatted output
    /// </summary>
    public int MaxLength;
}
```

### Available Placeholders

| Placeholder | Description | Example Output |
|-------------|-------------|----------------|
| `{timestamp}` | Message creation time | `2024-01-15 14:30:25.123` |
| `{level}` | Log level name | `INFO`, `WARNING`, `ERROR` |
| `{level:short}` | Short log level | `INF`, `WRN`, `ERR` |
| `{channel}` | Channel name | `Core`, `Networking`, `UI` |
| `{channel:icon}` | Channel with icon | `🌐 Networking` |
| `{message}` | Main log message | `Player joined the game` |
| `{thread}` | Thread ID | `1`, `23` |
| `{thread:name}` | Thread name | `Main Thread` |
| `{source}` | Source location | `PlayerController.cs:45` |
| `{source:method}` | Method name only | `UpdatePlayer` |
| `{source:file}` | File name only | `PlayerController.cs` |
| `{stacktrace}` | Stack trace | `at PlayerController.Update()...` |

### Predefined Formats

```csharp
public static class LogFormats
{
    /// <summary>
    /// Simple format for console output: [LEVEL] Message
    /// </summary>
    public static readonly LogFormat Simple = new LogFormat
    {
        Template = new FixedString512Bytes("[{level:short}] {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss"),
        MaxLength = 512
    };
    
    /// <summary>
    /// Detailed format for file output
    /// </summary>
    public static readonly LogFormat Detailed = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{thread}] [{level}] [{channel}] {message}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-dd HH:mm:ss.fff"),
        IncludeThreadInfo = true,
        MaxLength = 1024
    };
    
    /// <summary>
    /// Compact format for performance logs
    /// </summary>
    public static readonly LogFormat Compact = new LogFormat
    {
        Template = new FixedString512Bytes("{timestamp} | {level:short} | {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss.fff"),
        MaxLength = 256
    };
    
    /// <summary>
    /// Debug format with source information
    /// </summary>
    public static readonly LogFormat Debug = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{level}] {source:method} - {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss.fff"),
        IncludeSourceInfo = true,
        MaxLength = 1024
    };
    
    /// <summary>
    /// Comprehensive format with all information
    /// </summary>
    public static readonly LogFormat Comprehensive = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{thread:name}] [{level}] [{channel:icon}] {source} - {message}\n{stacktrace}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-dd HH:mm:ss.fff"),
        IncludeStackTrace = true,
        IncludeThreadInfo = true,
        IncludeSourceInfo = true,
        MaxLength = 4096
    };
    
    /// <summary>
    /// JSON-structured format for parsing
    /// </summary>
    public static readonly LogFormat Json = new LogFormat
    {
        Template = new FixedString512Bytes("{\"timestamp\":\"{timestamp}\",\"level\":\"{level}\",\"channel\":\"{channel}\",\"message\":\"{message}\"}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-ddTHH:mm:ss.fffZ"),
        MaxLength = 2048
    };
}
```

### Formatting Methods

```csharp
public static class LogFormatExtensions
{
    /// <summary>
    /// Formats a log message using the specified format
    /// </summary>
    public static FixedString4096Bytes Format(this LogFormat format, in LogMessage message)
    {
        var result = format.Template;
        
        // Replace timestamp
        if (result.ToString().Contains("{timestamp}"))
        {
            var timestamp = message.Timestamp.ToString(format.TimestampFormat.ToString());
            result = ReplaceAll(result, "{timestamp}", timestamp);
        }
        
        // Replace level
        result = ReplaceAll(result, "{level}", message.Level.ToString());
        result = ReplaceAll(result, "{level:short}", message.Level.ToShortString());
        
        // Replace channel
        result = ReplaceAll(result, "{channel}", message.Channel.ToString());
        result = ReplaceAll(result, "{channel:icon}", $"{message.Channel.GetIcon()} {message.Channel.GetDisplayName()}");
        
        // Replace message
        result = ReplaceAll(result, "{message}", message.Text.ToString());
        
        // Replace thread information
        if (format.IncludeThreadInfo)
        {
            result = ReplaceAll(result, "{thread}", message.ThreadId.ToString());
            result = ReplaceAll(result, "{thread:name}", GetThreadName(message.ThreadId));
        }
        
        // Replace source information
        if (format.IncludeSourceInfo && message.Source.IsValid)
        {
            result = ReplaceAll(result, "{source}", message.Source.ToString());
            result = ReplaceAll(result, "{source:method}", message.Source.MethodName.ToString());
            result = ReplaceAll(result, "{source:file}", message.Source.FileName.ToString());
        }
        
        // Replace stack trace
        if (format.IncludeStackTrace && message.HasStackTrace)
        {
            result = ReplaceAll(result, "{stacktrace}", message.StackTrace.ToString());
        }
        else
        {
            result = ReplaceAll(result, "{stacktrace}", "");
        }
        
        // Truncate if necessary
        if (format.MaxLength > 0 && result.Length > format.MaxLength)
        {
            var truncated = result.ToString().Substring(0, format.MaxLength - 3) + "...";
            result = new FixedString4096Bytes(truncated);
        }
        
        return new FixedString4096Bytes(result);
    }
    
    /// <summary>
    /// Creates a custom format with specified template
    /// </summary>
    public static LogFormat Custom(string template, string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff")
    {
        return new LogFormat
        {
            Template = new FixedString512Bytes(template),
            TimestampFormat = new FixedString64Bytes(timestampFormat),
            MaxLength = 1024
        };
    }
    
    private static FixedString4096Bytes ReplaceAll(FixedString4096Bytes source, string oldValue, string newValue)
    {
        return new FixedString4096Bytes(source.ToString().Replace(oldValue, newValue));
    }
    
    private static string GetThreadName(int threadId)
    {
        return threadId == 1 ? "Main Thread" : $"Thread-{threadId}";
    }
}
```

### Usage Examples

```csharp
// Using predefined formats
var message = LogMessage.Create(LogLevel.Info, "Player connected");

var simple = LogFormats.Simple.Format(message);
// Output: [INF] Player connected

var detailed = LogFormats.Detailed.Format(message);  
// Output: [2024-01-15 14:30:25.123] [1] [Info] [Core] Player connected

// Creating custom formats
var customFormat = LogFormatExtensions.Custom(
    "[{timestamp}] {channel:icon} {message}",
    "HH:mm:ss"
);

var formatted = customFormat.Format(message);
// Output: [14:30:25] ⚙️ Core Player connected

// JSON format for structured logging
var jsonMessage = LogFormats.Json.Format(message);
// Output: {"timestamp":"2024-01-15T14:30:25.123Z","level":"Info","channel":"Core","message":"Player connected"}
```

## 🏷️ LogProperties

The `LogProperties` struct provides a Burst-compatible way to attach structured data to log messages.

### Structure Definition

```csharp
[System.Serializable]
[BurstCompile]
public struct LogProperties : IDisposable
{
    private NativeHashMap<FixedString64Bytes, PropertyValue> _properties;
    
    /// <summary>
    /// Number of properties stored
    /// </summary>
    public int Count => _properties.IsCreated ? _properties.Count : 0;
    
    /// <summary>
    /// Checks if the properties container is created and valid
    /// </summary>
    public bool IsCreated => _properties.IsCreated;
    
    /// <summary>
    /// Creates a new properties container
    /// </summary>
    public static LogProperties Create(int initialCapacity = 8, Allocator allocator = Allocator.Temp)
    {
        return new LogProperties
        {
            _properties = new NativeHashMap<FixedString64Bytes, PropertyValue>(initialCapacity, allocator)
        };
    }
}
```

### Property Value Types

```csharp
[System.Serializable]
[BurstCompile]
public struct PropertyValue
{
    public PropertyType Type;
    public ValueUnion Value;
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct ValueUnion
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public bool BoolValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public int IntValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public float FloatValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public double DoubleValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public long LongValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public FixedString128Bytes StringValue;
    }
    
    public enum PropertyType : byte
    {
        Bool,
        Int,
        Float,
        Double,
        Long,
        String
    }
    
    // Factory methods
    public static PropertyValue FromBool(bool value) => new() { Type = PropertyType.Bool, Value = new() { BoolValue = value } };
    public static PropertyValue FromInt(int value) => new() { Type = PropertyType.Int, Value = new() { IntValue = value } };
    public static PropertyValue FromFloat(float value) => new() { Type = PropertyType.Float, Value = new() { FloatValue = value } };
    public static PropertyValue FromDouble(double value) => new() { Type = PropertyType.Double, Value = new() { DoubleValue = value } };
    public static PropertyValue FromLong(long value) => new() { Type = PropertyType.Long, Value = new() { LongValue = value } };
    public static PropertyValue FromString(string value) => new() { Type = PropertyType.String, Value = new() { StringValue = new FixedString128Bytes(value) } };
}
```

### Properties Methods

```csharp
public struct LogProperties
{
    /// <summary>
    /// Adds or updates a boolean property
    /// </summary>
    public void Set(string key, bool value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromBool(value);
    }
    
    /// <summary>
    /// Adds or updates an integer property
    /// </summary>
    public void Set(string key, int value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromInt(value);
    }
    
    /// <summary>
    /// Adds or updates a float property
    /// </summary>
    public void Set(string key, float value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromFloat(value);
    }
    
    /// <summary>
    /// Adds or updates a string property
    /// </summary>
    public void Set(string key, string value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromString(value);
    }
    
    /// <summary>
    /// Gets a property value by key
    /// </summary>
    public bool TryGet<T>(string key, out T value) where T : struct
    {
        value = default;
        
        if (!_properties.IsCreated)
            return false;
            
        if (!_properties.TryGetValue(new FixedString64Bytes(key), out var propertyValue))
            return false;
        
        return TryConvertValue(propertyValue, out value);
    }
    
    /// <summary>
    /// Checks if a property exists
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _properties.IsCreated && _properties.ContainsKey(new FixedString64Bytes(key));
    }
    
    /// <summary>
    /// Removes a property
    /// </summary>
    public bool Remove(string key)
    {
        return _properties.IsCreated && _properties.Remove(new FixedString64Bytes(key));
    }
    
    /// <summary>
    /// Clears all properties
    /// </summary>
    public void Clear()
    {
        if (_properties.IsCreated)
        {
            _properties.Clear();
        }
    }
    
    /// <summary>
    /// Gets all property keys
    /// </summary>
    public NativeArray<FixedString64Bytes> GetKeys(Allocator allocator)
    {
        if (!_properties.IsCreated)
            return new NativeArray<FixedString64Bytes>(0, allocator);
        
        return _properties.GetKeyArray(allocator);
    }
    
    private void EnsureCreated()
    {
        if (!_properties.IsCreated)
        {
            _properties = new NativeHashMap<FixedString64Bytes, PropertyValue>(8, Allocator.Temp);
        }
    }
    
    private bool TryConvertValue<T>(PropertyValue propertyValue, out T value) where T : struct
    {
        value = default;
        
        if (typeof(T) == typeof(bool) && propertyValue.Type == PropertyType.Bool)
        {
            value = (T)(object)propertyValue.Value.BoolValue;
            return true;
        }
        
        if (typeof(T) == typeof(int) && propertyValue.Type == PropertyType.Int)
        {
            value = (T)(object)propertyValue.Value.IntValue;
            return true;
        }
        
        if (typeof(T) == typeof(float) && propertyValue.Type == PropertyType.Float)
        {
            value = (T)(object)propertyValue.Value.FloatValue;
            return true;
        }
        
        if (typeof(T) == typeof(string) && propertyValue.Type == PropertyType.String)
        {
            value = (T)(object)propertyValue.Value.StringValue.ToString();
            return true;
        }
        
        return false;
    }
    
    public void Dispose()
    {
        if (_properties.IsCreated)
        {
            _properties.Dispose();
        }
    }
}
```

### Usage Examples

```csharp
// Creating and populating properties
var properties = LogProperties.Create();
properties.Set("PlayerId", "player-123");
properties.Set("Level", 5);
properties.Set("Health", 75.5f);
properties.Set("IsAlive", true);

// Using with log messages
var message
// Using with log messages
var message = LogMessage.Create(LogLevel.Info, "Player status updated");
message.Properties = properties;

// Retrieving property values
if (properties.TryGet<string>("PlayerId", out var playerId))
{
    Debug.Log($"Player ID: {playerId}");
}

if (properties.TryGet<int>("Level", out var level))
{
    Debug.Log($"Player Level: {level}");
}

// Builder pattern for convenience
var builderProperties = LogProperties.Create()
    .With("SessionId", "abc-123")
    .With("Duration", 45.2f)
    .With("Completed", true);

// Iterating through properties
using var keys = properties.GetKeys(Allocator.Temp);
for (int i = 0; i < keys.Length; i++)
{
    Debug.Log($"Property key: {keys[i]}");
}

// Don't forget to dispose
properties.Dispose();
builderProperties.Dispose();
```

## 📚 LogContext

The `LogContext` struct provides thread-local context data that automatically gets attached to log messages.

### Structure Definition

```csharp
[System.Serializable]
public struct LogContext : IDisposable
{
    private static readonly ThreadLocal<NativeHashMap<FixedString64Bytes, PropertyValue>> ThreadLocalContext 
        = new ThreadLocal<NativeHashMap<FixedString64Bytes, PropertyValue>>(
            () => new NativeHashMap<FixedString64Bytes, PropertyValue>(16, Allocator.Persistent));
    
    /// <summary>
    /// Sets a context value for the current thread
    /// </summary>
    public static void Set(string key, object value)
    {
        var context = GetOrCreateContext();
        var fixedKey = new FixedString64Bytes(key);
        
        var propertyValue = value switch
        {
            bool b => PropertyValue.FromBool(b),
            int i => PropertyValue.FromInt(i),
            float f => PropertyValue.FromFloat(f),
            double d => PropertyValue.FromDouble(d),
            long l => PropertyValue.FromLong(l),
            string s => PropertyValue.FromString(s),
            _ => PropertyValue.FromString(value?.ToString() ?? "null")
        };
        
        context[fixedKey] = propertyValue;
    }
    
    /// <summary>
    /// Gets a context value for the current thread
    /// </summary>
    public static T Get<T>(string key, T defaultValue = default) where T : struct
    {
        var context = GetOrCreateContext();
        var fixedKey = new FixedString64Bytes(key);
        
        if (context.TryGetValue(fixedKey, out var propertyValue))
        {
            if (TryConvertValue<T>(propertyValue, out var result))
            {
                return result;
            }
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Removes a context value for the current thread
    /// </summary>
    public static bool Remove(string key)
    {
        var context = GetOrCreateContext();
        return context.Remove(new FixedString64Bytes(key));
    }
    
    /// <summary>
    /// Clears all context values for the current thread
    /// </summary>
    public static void Clear()
    {
        var context = GetOrCreateContext();
        context.Clear();
    }
    
    /// <summary>
    /// Gets a snapshot of current context data
    /// </summary>
    public static LogProperties GetSnapshot()
    {
        var context = GetOrCreateContext();
        var snapshot = LogProperties.Create(context.Count);
        
        using var keys = context.GetKeyArray(Allocator.Temp);
        using var values = context.GetValueArray(Allocator.Temp);
        
        for (int i = 0; i < keys.Length; i++)
        {
            snapshot.SetPropertyValue(keys[i], values[i]);
        }
        
        return snapshot;
    }
    
    private static NativeHashMap<FixedString64Bytes, PropertyValue> GetOrCreateContext()
    {
        if (!ThreadLocalContext.Value.IsCreated)
        {
            ThreadLocalContext.Value = new NativeHashMap<FixedString64Bytes, PropertyValue>(16, Allocator.Persistent);
        }
        
        return ThreadLocalContext.Value;
    }
    
    public void Dispose()
    {
        Clear();
    }
}
```

### Context Scope Helper

```csharp
/// <summary>
/// Provides a using-block scope for temporary context values
/// </summary>
public readonly struct LogContextScope : IDisposable
{
    private readonly FixedString64Bytes _key;
    private readonly PropertyValue _previousValue;
    private readonly bool _hadPreviousValue;
    
    public LogContextScope(string key, object value)
    {
        _key = new FixedString64Bytes(key);
        
        // Save previous value if it exists
        var context = LogContext.GetOrCreateContext();
        _hadPreviousValue = context.TryGetValue(_key, out _previousValue);
        
        // Set new value
        LogContext.Set(key, value);
    }
    
    public void Dispose()
    {
        var context = LogContext.GetOrCreateContext();
        
        if (_hadPreviousValue)
        {
            // Restore previous value
            context[_key] = _previousValue;
        }
        else
        {
            // Remove the key since it didn't exist before
            context.Remove(_key);
        }
    }
}
```

### Usage Examples

```csharp
// Setting persistent context values
LogContext.Set("UserId", "user-123");
LogContext.Set("SessionId", "session-abc");
LogContext.Set("RequestId", Guid.NewGuid().ToString());

// All subsequent log messages will include these context values
Logger.Info("User action performed"); // Includes UserId, SessionId, RequestId

// Using scoped context
using (new LogContextScope("Operation", "PlayerLogin"))
{
    Logger.Info("Login process started"); // Includes Operation=PlayerLogin
    
    using (new LogContextScope("Step", "Validation"))
    {
        Logger.Debug("Validating credentials"); // Includes Operation=PlayerLogin, Step=Validation
    } // Step context removed
    
    Logger.Info("Login completed"); // Still includes Operation=PlayerLogin
} // Operation context removed

// Retrieving context values
var userId = LogContext.Get<string>("UserId");
var hasSession = LogContext.Get<bool>("HasActiveSession", false);

// Getting context snapshot for manual attachment
var contextSnapshot = LogContext.GetSnapshot();
var message = LogMessage.Create(LogLevel.Info, "Manual context example");
message.Properties = contextSnapshot;
```

## 🏗️ LogMessageBuilder

The `LogMessageBuilder` provides a fluent API for constructing complex log messages with various properties and context data.

### Builder Definition

```csharp
public struct LogMessageBuilder : IDisposable
{
    private LogMessage _message;
    private LogProperties _properties;
    private bool _hasProperties;
    
    /// <summary>
    /// Creates a new message builder with specified level and text
    /// </summary>
    public static LogMessageBuilder Create(LogLevel level, string text)
    {
        return new LogMessageBuilder
        {
            _message = LogMessage.Create(level, new FixedString512Bytes(text)),
            _properties = LogProperties.Create(),
            _hasProperties = false
        };
    }
    
    /// <summary>
    /// Sets the log level
    /// </summary>
    public LogMessageBuilder WithLevel(LogLevel level)
    {
        _message.Level = level;
        return this;
    }
    
    /// <summary>
    /// Sets the message text
    /// </summary>
    public LogMessageBuilder WithText(string text)
    {
        _message.Text = new FixedString512Bytes(text);
        return this;
    }
    
    /// <summary>
    /// Sets the log channel
    /// </summary>
    public LogMessageBuilder WithChannel(LogChannel channel)
    {
        _message.Channel = channel;
        return this;
    }
    
    /// <summary>
    /// Adds a string property
    /// </summary>
    public LogMessageBuilder WithProperty(string key, string value)
    {
        EnsureProperties();
        _properties.Set(key, value);
        return this;
    }
    
    /// <summary>
    /// Adds an integer property
    /// </summary>
    public LogMessageBuilder WithProperty(string key, int value)
    {
        EnsureProperties();
        _properties.Set(key, value);
        return this;
    }
    
    /// <summary>
    /// Adds a float property
    /// </summary>
    public LogMessageBuilder WithProperty(string key, float value)
    {
        EnsureProperties();
        _properties.Set(key, value);
        return this;
    }
    
    /// <summary>
    /// Adds a boolean property
    /// </summary>
    public LogMessageBuilder WithProperty(string key, bool value)
    {
        EnsureProperties();
        _properties.Set(key, value);
        return this;
    }
    
    /// <summary>
    /// Adds multiple properties from a dictionary
    /// </summary>
    public LogMessageBuilder WithProperties(Dictionary<string, object> properties)
    {
        EnsureProperties();
        
        foreach (var kvp in properties)
        {
            switch (kvp.Value)
            {
                case string s:
                    _properties.Set(kvp.Key, s);
                    break;
                case int i:
                    _properties.Set(kvp.Key, i);
                    break;
                case float f:
                    _properties.Set(kvp.Key, f);
                    break;
                case bool b:
                    _properties.Set(kvp.Key, b);
                    break;
                default:
                    _properties.Set(kvp.Key, kvp.Value?.ToString() ?? "null");
                    break;
            }
        }
        
        return this;
    }
    
    /// <summary>
    /// Sets the timestamp
    /// </summary>
    public LogMessageBuilder WithTimestamp(DateTime timestamp)
    {
        _message.TimestampTicks = timestamp.Ticks;
        return this;
    }
    
    /// <summary>
    /// Captures current stack trace
    /// </summary>
    public LogMessageBuilder WithStackTrace()
    {
        var stackTrace = Environment.StackTrace;
        _message.StackTrace = new FixedString4096Bytes(stackTrace);
        return this;
    }
    
    /// <summary>
    /// Sets source information
    /// </summary>
    public LogMessageBuilder WithSource(string fileName, string methodName, int lineNumber)
    {
        _message.Source = new SourceInfo
        {
            FileName = new FixedString256Bytes(fileName),
            MethodName = new FixedString128Bytes(methodName),
            LineNumber = lineNumber
        };
        return this;
    }
    
    /// <summary>
    /// Merges current thread context
    /// </summary>
    public LogMessageBuilder WithThreadContext()
    {
        var contextSnapshot = LogContext.GetSnapshot();
        
        if (contextSnapshot.Count > 0)
        {
            EnsureProperties();
            
            using var keys = contextSnapshot.GetKeys(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (contextSnapshot.TryGetPropertyValue(key, out var value))
                {
                    _properties.SetPropertyValue(key, value);
                }
            }
        }
        
        contextSnapshot.Dispose();
        return this;
    }
    
    /// <summary>
    /// Builds the final log message
    /// </summary>
    public LogMessage Build()
    {
        if (_hasProperties)
        {
            _message.Properties = _properties;
        }
        
        return _message;
    }
    
    private void EnsureProperties()
    {
        if (!_hasProperties)
        {
            _properties = LogProperties.Create();
            _hasProperties = true;
        }
    }
    
    public void Dispose()
    {
        if (_hasProperties)
        {
            _properties.Dispose();
        }
    }
}
```

### Usage Examples

```csharp
// Basic message building
var message = LogMessageBuilder.Create(LogLevel.Info, "Player action")
    .WithChannel(LogChannel.Core)
    .WithProperty("PlayerId", "player-123")
    .WithProperty("Action", "Jump")
    .WithProperty("Timestamp", DateTime.Now)
    .Build();

// Complex message with multiple properties
var complexMessage = LogMessageBuilder.Create(LogLevel.Warning, "Performance issue detected")
    .WithChannel(LogChannel.Performance)
    .WithProperty("FrameTime", 33.5f)
    .WithProperty("MemoryUsage", 512)
    .WithProperty("ActiveObjects", 1250)
    .WithProperty("IsLoadingLevel", true)
    .WithStackTrace()
    .WithThreadContext()
    .Build();

// Using with dictionary properties
var properties = new Dictionary<string, object>
{
    { "NetworkLatency", 45.2f },
    { "PacketLoss", 0.05f },
    { "ConnectionState", "Connected" }
};

var networkMessage = LogMessageBuilder.Create(LogLevel.Debug, "Network statistics")
    .WithChannel(LogChannel.Networking)
    .WithProperties(properties)
    .Build();

// Don't forget to dispose the builder if properties were used
using var builder = LogMessageBuilder.Create(LogLevel.Info, "Disposable example");
var disposableMessage = builder
    .WithProperty("Key", "Value")
    .Build();
```

## 🔧 Native Data Types

The logging system includes several native data types optimized for Burst compilation and high-performance scenarios.

### SourceInfo Structure

```csharp
[System.Serializable]
[BurstCompile]
public struct SourceInfo
{
    /// <summary>
    /// Source file name
    /// </summary>
    public FixedString256Bytes FileName;
    
    /// <summary>
    /// Method name where the log originated
    /// </summary>
    public FixedString128Bytes MethodName;
    
    /// <summary>
    /// Line number in the source file
    /// </summary>
    public int LineNumber;
    
    /// <summary>
    /// Gets whether this source info is valid/populated
    /// </summary>
    public bool IsValid => FileName.Length > 0 || MethodName.Length > 0;
    
    /// <summary>
    /// Creates source info from caller information
    /// </summary>
    public static SourceInfo FromCaller(
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        return new SourceInfo
        {
            FileName = new FixedString256Bytes(Path.GetFileName(filePath)),
            MethodName = new FixedString128Bytes(memberName),
            LineNumber = lineNumber
        };
    }
    
    public override string ToString()
    {
        if (!IsValid)
            return "Unknown";
            
        if (FileName.Length > 0 && LineNumber > 0)
            return $"{FileName}:{LineNumber}";
            
        if (MethodName.Length > 0)
            return MethodName.ToString();
            
        return FileName.ToString();
    }
}
```

### LogTag Structure

```csharp
[System.Serializable]
[BurstCompile]
public struct LogTag : IEquatable<LogTag>
{
    private FixedString64Bytes _value;
    
    /// <summary>
    /// Gets the tag value as string
    /// </summary>
    public string Value => _value.ToString();
    
    /// <summary>
    /// Gets whether this tag is empty
    /// </summary>
    public bool IsEmpty => _value.Length == 0;
    
    /// <summary>
    /// Default empty tag
    /// </summary>
    public static readonly LogTag Empty = new LogTag();
    
    /// <summary>
    /// Common system tags
    /// </summary>
    public static readonly LogTag System = new LogTag("System");
    public static readonly LogTag Network = new LogTag("Network");
    public static readonly LogTag UI = new LogTag("UI");
    public static readonly LogTag Audio = new LogTag("Audio");
    public static readonly LogTag Physics = new LogTag("Physics");
    public static readonly LogTag AI = new LogTag("AI");
    public static readonly LogTag Performance = new LogTag("Performance");
    
    public LogTag(string value)
    {
        _value = new FixedString64Bytes(value ?? "");
    }
    
    public static implicit operator LogTag(string value) => new LogTag(value);
    public static implicit operator string(LogTag tag) => tag.Value;
    
    public bool Equals(LogTag other) => _value.Equals(other._value);
    public override bool Equals(object obj) => obj is LogTag other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => Value;
    
    public static bool operator ==(LogTag left, LogTag right) => left.Equals(right);
    public static bool operator !=(LogTag left, LogTag right) => !left.Equals(right);
}
```

### UnsafeLogQueue for Job System

```csharp
[System.Serializable]
[BurstCompile]
public unsafe struct UnsafeLogQueue : IDisposable
{
    private UnsafeRingQueue<LogMessage>* _queue;
    private Allocator _allocator;
    
    /// <summary>
    /// Gets whether this queue is created and valid
    /// </summary>
    public bool IsCreated => _queue != null;
    
    /// <summary>
    /// Gets the current count of messages in the queue
    /// </summary>
    public int Count => IsCreated ? _queue->Count : 0;
    
    /// <summary>
    /// Gets the capacity of the queue
    /// </summary>
    public int Capacity => IsCreated ? _queue->Capacity : 0;
    
    /// <summary>
    /// Creates a new unsafe log queue
    /// </summary>
    public UnsafeLogQueue(int capacity, Allocator allocator)
    {
        _allocator = allocator;
        _queue = UnsafeRingQueue<LogMessage>.Alloc(capacity, allocator);
    }
    
    /// <summary>
    /// Writer interface for job system
    /// </summary>
    public struct Writer
    {
        private UnsafeRingQueue<LogMessage>* _queue;
        
        internal Writer(UnsafeRingQueue<LogMessage>* queue)
        {
            _queue = queue;
        }
        
        /// <summary>
        /// Writes a log message to the queue
        /// </summary>
        public bool TryWrite(in LogMessage message)
        {
            return _queue->TryEnqueue(message);
        }
        
        /// <summary>
        /// Writes a log message, overwriting oldest if full
        /// </summary>
        public void Write(in LogMessage message)
        {
            if (!_queue->TryEnqueue(message))
            {
                // Overwrite oldest message
                _queue->TryDequeue(out _);
                _queue->TryEnqueue(message);
            }
        }
    }
    
    /// <summary>
    /// Reader interface for processing messages
    /// </summary>
    public struct Reader
    {
        private UnsafeRingQueue<LogMessage>* _queue;
        
        internal Reader(UnsafeRingQueue<LogMessage>* queue)
        {
            _queue = queue;
        }
        
        /// <summary>
        /// Tries to read a message from the queue
        /// </summary>
        public bool TryRead(out LogMessage message)
        {
            return _queue->TryDequeue(out message);
        }
        
        /// <summary>
        /// Reads all available messages into a native list
        /// </summary>
        public int ReadAll(ref NativeList<LogMessage> messages)
        {
            int count = 0;
            while (_queue->TryDequeue(out var message))
            {
                messages.Add(message);
                count++;
            }
            return count;
        }
    }
    
    /// <summary>
    /// Gets a writer for this queue
    /// </summary>
    public Writer AsWriter()
    {
        return new Writer(_queue);
    }
    
    /// <summary>
    /// Gets a reader for this queue
    /// </summary>
    public Reader AsReader()
    {
        return new Reader(_queue);
    }
    
    public void Dispose()
    {
        if (IsCreated)
        {
            UnsafeRingQueue<LogMessage>.Free(_queue, _allocator);
            _queue = null;
        }
    }
}
```

## 💾 Serialization

The logging system provides efficient serialization support for persistence and network transmission.

### Binary Serialization

```csharp
[BurstCompile]
public static class LogMessageSerializer
{
    /// <summary>
    /// Serializes a log message to a byte array
    /// </summary>
    public static unsafe int Serialize(in LogMessage message, byte* buffer, int bufferSize)
    {
        if (bufferSize < GetSerializedSize(message))
            return -1;
        
        int offset = 0;
        
        // Write header
        WriteBytes(buffer, ref offset, message.Id.ToByteArray());
        WriteLong(buffer, ref offset, message.TimestampTicks);
        WriteByte(buffer, ref offset, (byte)message.Level);
        WriteByte(buffer, ref offset, (byte)message.Channel);
        WriteInt(buffer, ref offset, message.ThreadId);
        
        // Write text
        WriteFixedString(buffer, ref offset, message.Text);
        
        // Write properties if present
        WriteBool(buffer, ref offset, message.HasProperties);
        if (message.HasProperties)
        {
            SerializeProperties(buffer, ref offset, message.Properties);
        }
        
        // Write source info
        WriteBool(buffer, ref offset, message.Source.IsValid);
        if (message.Source.IsValid)
        {
            WriteFixedString(buffer, ref offset, message.Source.FileName);
            WriteFixedString(buffer, ref offset, message.Source.MethodName);
            WriteInt(buffer, ref offset, message.Source.LineNumber);
        }
        
        // Write stack trace
        WriteBool(buffer, ref offset, message.HasStackTrace);
        if (message.HasStackTrace)
        {
            WriteFixedString(buffer, ref offset, message.StackTrace);
        }
        
        return offset;
    }
    
    /// <summary>
    /// Deserializes a log message from a byte array
    /// </summary>
    public static unsafe bool Deserialize(byte* buffer, int bufferSize, out LogMessage message)
    {
        message = default;
        int offset = 0;
        
        try
        {
            // Read header
            var idBytes = ReadBytes(buffer, ref offset, 16);
            message.Id = new Guid(idBytes);
            message.TimestampTicks = ReadLong(buffer, ref offset);
            message.Level = (LogLevel)ReadByte(buffer, ref offset);
            message.Channel = (LogChannel)ReadByte(buffer, ref offset);
            message.ThreadId = ReadInt(buffer, ref offset);
            
            // Read text
            message.Text = ReadFixedString512(buffer, ref offset);
            
            // Read properties
            bool hasProperties = ReadBool(buffer, ref offset);
            if (hasProperties)
            {
                message.Properties = DeserializeProperties(buffer, ref offset);
            }
            
            // Read source info
            bool hasSourceInfo = ReadBool(buffer, ref offset);
            if (hasSourceInfo)
            {
                message.Source = new SourceInfo
                {
                    FileName = ReadFixedString256(buffer, ref offset),
                    MethodName = ReadFixedString128(buffer, ref offset),
                    LineNumber = ReadInt(buffer, ref offset)
                };
            }
            
            // Read stack trace
            bool hasStackTrace = ReadBool(buffer, ref offset);
            if (hasStackTrace)
            {
                message.StackTrace = ReadFixedString4096(buffer, ref offset);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the size needed to serialize a message
    /// </summary>
    public static int GetSerializedSize(in LogMessage message)
    {
        int size = 16 + 8 + 1 + 1 + 4; // Id + Timestamp + Level + Channel + ThreadId
        size += 4 + message.Text.Length; // Text length + text data
        size += 1; // HasProperties flag
        
        if (message.HasProperties)
        {
            size += GetPropertiesSize(message.Properties);
        }
        
        size += 1; // HasSourceInfo flag
        if (message.Source.IsValid)
        {
            size += 4 + message.Source.FileName.Length;
            size += 4 + message.Source.MethodName.Length;
            size += 4; // LineNumber
        }
        
        size += 1; // HasStackTrace flag
        if (message.HasStackTrace)
        {
            size += 4 + message.StackTrace.Length;
        }
        
        return size;
    }
    
    // Helper methods for reading/writing primitive types
    private static unsafe void WriteByte(byte* buffer, ref int offset, byte value)
    {
        buffer[offset] = value;
        offset++;
    }
    
    private static unsafe byte ReadByte(byte* buffer, ref int offset)
    {
        byte value = buffer[offset];
        offset++;
        return value;
    }
    
    // ... (additional helper methods for other types)
}
```

### JSON Serialization

```csharp
public static class LogMessageJsonSerializer
{
    [System.Serializable]
    public class SerializableLogMessage
    {
        public string id;
        public long timestampTicks;
        public string level;
        public string channel;
        public int threadId;
        public string text;
        public Dictionary<string, object> properties;
        public SerializableSourceInfo source;
        public string stackTrace;
        
        [System.Serializable]
        public class SerializableSourceInfo
        {
            public string fileName;
            public string methodName;
            public int lineNumber;
        }
    }
    
    /// <summary>
    /// Converts LogMessage to JSON string
    /// </summary>
    public static string ToJson(in LogMessage message)
    {
        var serializable = new SerializableLogMessage
        {
            id = message.Id.ToString(),
            timestampTicks = message.TimestampTicks,
            level = message.Level.ToString(),
            channel = message.Channel.ToString(),
            threadId = message.ThreadId,
            text = message.Text.ToString()
        };
        
        // Convert properties
        if (message.HasProperties)
        {
            serializable.properties = new Dictionary<string, object>();
            using var keys = message.Properties.GetKeys(Allocator.Temp);
            
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i].ToString();
                if (message.Properties.TryGetPropertyValue(keys[i], out var propertyValue))
                {
                    serializable.properties[key] = ConvertPropertyValue(propertyValue);
                }
            }
        }
        
        // Convert source info
        if (message.Source.IsValid)
        {
            serializable.source = new SerializableLogMessage.SerializableSourceInfo
            {
                fileName = message.Source.FileName.ToString(),
                methodName = message.Source.MethodName.ToString(),
                lineNumber = message.Source.LineNumber
            };
        }
        
        // Convert stack trace
        if (message.HasStackTrace)
        {
            serializable.stackTrace = message.StackTrace.ToString();
        }
        
        return JsonUtility.ToJson(serializable, true);
    }
    
    /// <summary>
    /// Converts JSON string to LogMessage
    /// </summary>
    public static bool FromJson(string json, out LogMessage message)
    {
        message = default;
        
        try
        {
            var serializable = JsonUtility.FromJson<SerializableLogMessage>(json);
            
            message = new LogMessage
            {
                Id = Guid.Parse(serializable.id),
                TimestampTicks = serializable.timestampTicks,
                Level = Enum.Parse<LogLevel>(serializable.level),
                Channel = Enum.Parse<LogChannel>(serializable.channel),
                ThreadId = serializable.threadId,
                Text = new FixedString512Bytes(serializable.text)
            };
            
            // Convert properties
            if (serializable.properties != null)
            {
                message.Properties = LogProperties.Create(serializable.properties.Count);
                foreach (var kvp in serializable.properties)
                {
                    message.Properties.Set(kvp.Key, kvp.Value?.ToString() ?? "null");
                }
            }
            
            // Convert source info
            if (serializable.source != null)
            {
                message.Source = new SourceInfo
                {
                    FileName = new FixedString256Bytes(serializable.source.fileName),
                    MethodName = new FixedString128Bytes(serializable.source.methodName),
                    LineNumber = serializable.source.lineNumber
                };
            }
            
            // Convert stack trace
            if (!string.IsNullOrEmpty(serializable.stackTrace))
            {
                message.StackTrace = new FixedString4096Bytes(serializable.stackTrace);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static object ConvertPropertyValue(PropertyValue propertyValue)
    {
        return propertyValue.Type switch
        {
            PropertyValue.PropertyType.Bool => propertyValue.Value.BoolValue,
            PropertyValue.PropertyType.Int => propertyValue.Value.IntValue,
            PropertyValue.PropertyType.Float => propertyValue.Value.FloatValue,
            PropertyValue.PropertyType.Double => propertyValue.Value.DoubleValue,
            PropertyValue.PropertyType.Long => propertyValue.Value.LongValue,
            PropertyValue.PropertyType.String => propertyValue.Value.StringValue.ToString(),
            _ => null
        };
    }
}
```

## ⚡ Performance Considerations

### Memory Management

```csharp
public static class LoggingPerformance
{
    /// <summary>
    /// Pre-allocated message pool for high-frequency logging
    /// </summary>
    private static readonly ObjectPool<LogMessageBuilder> MessageBuilderPool 
        = new ObjectPool<LogMessageBuilder>(
            createFunc: () => new LogMessageBuilder(),
            actionOnGet: builder => builder.Reset(),
            actionOnRelease: builder => builder.Clear(),
            actionOnDestroy: builder => builder.Dispose(),
            maxSize: 100
        );
    
    /// <summary>
    /// Gets a pooled message builder
    /// </summary>
    public static LogMessageBuilder GetPooledBuilder()
    {
        return MessageBuilderPool.Get();
    }
    
    /// <summary>
    /// Returns a message builder to the pool# Logging Data Structures Guide

## 📋 Table of Contents

- [Overview](#overview)
- [LogMessage](#logmessage)
- [LogLevel](#loglevel)
- [LogChannel](#logchannel)
- [LogFormat](#logformat)
- [LogProperties](#logproperties)
- [LogContext](#logcontext)
- [LogMessageBuilder](#logmessagebuilder)
- [Native Data Types](#native-data-types)
- [Serialization](#serialization)
- [Performance Considerations](#performance-considerations)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)

## 🎯 Overview

The AhBearStudios Core Logging system uses a comprehensive set of data structures to represent, store, and process log information. These structures are designed for high performance, Burst compatibility, and seamless integration with Unity's collections system.

### Key Design Principles

- **Burst Compatible**: All critical data structures support Burst compilation
- **Memory Efficient**: Minimal allocations and optimal memory layout
- **Strongly Typed**: Type-safe operations and compile-time validation
- **Extensible**: Support for custom properties and context data
- **Thread Safe**: Safe concurrent access patterns

## 📊 LogMessage

The `LogMessage` struct is the fundamental unit of logging data, representing a single log event with all associated information.

### Structure Definition

```csharp
[System.Serializable]
[BurstCompile]
public struct LogMessage : IMessage
{
    /// <summary>
    /// Unique identifier for this message instance
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// The main text content of the log message
    /// </summary>
    public FixedString512Bytes Text;
    
    /// <summary>
    /// Severity level of the message
    /// </summary>
    public LogLevel Level;
    
    /// <summary>
    /// Channel/category for the message
    /// </summary>
    public LogChannel Channel;
    
    /// <summary>
    /// Timestamp when the message was created (UTC ticks)
    /// </summary>
    public long TimestampTicks { get; private set; }
    
    /// <summary>
    /// Type code for message bus integration
    /// </summary>
    public ushort TypeCode { get; private set; }
    
    /// <summary>
    /// Thread ID where the message originated
    /// </summary>
    public int ThreadId;
    
    /// <summary>
    /// Source information (file, method, line number)
    /// </summary>
    public SourceInfo Source;
    
    /// <summary>
    /// Stack trace information (if enabled)
    /// </summary>
    public FixedString4096Bytes StackTrace;
    
    /// <summary>
    /// Additional context properties
    /// </summary>
    public LogProperties Properties;
}
```

### Properties and Methods

```csharp
public struct LogMessage
{
    /// <summary>
    /// Gets the timestamp as DateTime
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks);
    
    /// <summary>
    /// Gets whether this message has stack trace information
    /// </summary>
    public bool HasStackTrace => StackTrace.Length > 0;
    
    /// <summary>
    /// Gets whether this message has additional properties
    /// </summary>
    public bool HasProperties => Properties.Count > 0;
    
    /// <summary>
    /// Creates a new log message with specified parameters
    /// </summary>
    public static LogMessage Create(LogLevel level, FixedString512Bytes text, 
                                   LogChannel channel = LogChannel.None)
    {
        return new LogMessage
        {
            Id = Guid.NewGuid(),
            Level = level,
            Text = text,
            Channel = channel,
            TimestampTicks = DateTime.UtcNow.Ticks,
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            TypeCode = GetTypeCode<LogMessage>()
        };
    }
    
    /// <summary>
    /// Creates a copy of this message with modified properties
    /// </summary>
    public LogMessage WithLevel(LogLevel level)
    {
        var copy = this;
        copy.Level = level;
        return copy;
    }
    
    public LogMessage WithText(FixedString512Bytes text)
    {
        var copy = this;
        copy.Text = text;
        return copy;
    }
    
    public LogMessage WithChannel(LogChannel channel)
    {
        var copy = this;
        copy.Channel = channel;
        return copy;
    }
}
```

### Usage Examples

```csharp
// Basic message creation
var message = LogMessage.Create(LogLevel.Info, "Application started");

// Message with channel
var networkMessage = LogMessage.Create(
    LogLevel.Debug, 
    "Connection established", 
    LogChannel.Networking
);

// Message with properties
var gameMessage = LogMessage.Create(LogLevel.Info, "Player joined")
    .WithProperties(new LogProperties
    {
        { "PlayerId", "player-123" },
        { "Level", 5 },
        { "SessionTime", 45.2f }
    });

// Message modification
var modifiedMessage = message
    .WithLevel(LogLevel.Warning)
    .WithText("Application started with warnings");
```

## 📈 LogLevel

The `LogLevel` enum defines message severity in a hierarchical system that enables effective filtering.

### Enumeration Definition

```csharp
/// <summary>
/// Defines the severity level of log messages
/// </summary>
public enum LogLevel : byte
{
    /// <summary>
    /// Most detailed information, typically for step-by-step debugging
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Detailed debugging information, more focused than Trace
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// General operational information about application flow
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Potential issues that aren't immediately problematic
    /// </summary>
    Warning = 3,
    
    /// <summary>
    /// Error conditions that might allow the application to continue
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Severe errors that may cause the application to terminate
    /// </summary>
    Critical = 5,
    
    /// <summary>
    /// Used to disable all logging
    /// </summary>
    None = 6
}
```

### Extension Methods

```csharp
public static class LogLevelExtensions
{
    /// <summary>
    /// Checks if this level includes the specified level (this >= other)
    /// </summary>
    public static bool Includes(this LogLevel level, LogLevel other)
    {
        return level >= other;
    }
    
    /// <summary>
    /// Gets a short string representation of the log level
    /// </summary>
    public static string ToShortString(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            LogLevel.None => "NON",
            _ => "???"
        };
    }
    
    /// <summary>
    /// Gets the display color for the log level
    /// </summary>
    public static Color32 GetDisplayColor(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => new Color32(128, 128, 128, 255),    // Gray
            LogLevel.Debug => new Color32(0, 255, 255, 255),      // Cyan
            LogLevel.Info => new Color32(255, 255, 255, 255),     // White
            LogLevel.Warning => new Color32(255, 255, 0, 255),    // Yellow
            LogLevel.Error => new Color32(255, 0, 0, 255),        // Red
            LogLevel.Critical => new Color32(255, 0, 255, 255),   // Magenta
            _ => new Color32(255, 255, 255, 255)
        };
    }
    
    /// <summary>
    /// Parses a log level from string, case insensitive
    /// </summary>
    public static LogLevel Parse(string levelString)
    {
        if (Enum.TryParse<LogLevel>(levelString, true, out var level))
        {
            return level;
        }
        
        // Handle common aliases
        return levelString.ToLowerInvariant() switch
        {
            "verbose" => LogLevel.Trace,
            "information" => LogLevel.Info,
            "warn" => LogLevel.Warning,
            "err" => LogLevel.Error,
            "fatal" => LogLevel.Critical,
            _ => LogLevel.Info
        };
    }
    
    /// <summary>
    /// Gets the relative weight/importance of the log level
    /// </summary>
    public static float GetWeight(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => 0.1f,
            LogLevel.Debug => 0.2f,
            LogLevel.Info => 0.5f,
            LogLevel.Warning => 0.7f,
            LogLevel.Error => 0.9f,
            LogLevel.Critical => 1.0f,
            _ => 0.0f
        };
    }
}
```

### Usage Examples

```csharp
// Level comparison
if (LogLevel.Warning.Includes(LogLevel.Info))
{
    // This will be false because Warning > Info
}

if (LogLevel.Info.Includes(LogLevel.Warning))  
{
    // This will be true because Info < Warning
}

// Display formatting
var level = LogLevel.Warning;
Debug.Log($"Level: {level.ToShortString()}"); // "Level: WRN"

// Color coding in UI
var color = LogLevel.Error.GetDisplayColor(); // Red color

// Parsing from configuration
var configLevel = LogLevelExtensions.Parse("debug"); // LogLevel.Debug
var aliasLevel = LogLevelExtensions.Parse("verbose"); // LogLevel.Trace

// Weighted importance calculations
var weight = LogLevel.Critical.GetWeight(); // 1.0f
var isImportant = weight > 0.8f; // true for Error and Critical
```

## 📁 LogChannel

The `LogChannel` enum provides categorical organization for logs, enabling domain-specific filtering and routing.

### Enumeration Definition

```csharp
/// <summary>
/// Defines categories for organizing log messages by domain or feature
/// </summary>
public enum LogChannel : byte
{
    /// <summary>
    /// Default channel for unspecified logs
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Core game systems and engine functionality
    /// </summary>
    Core = 1,
    
    /// <summary>
    /// User interface and HUD systems
    /// </summary>
    UI = 2,
    
    /// <summary>
    /// Audio system, sound effects, and music
    /// </summary>
    Audio = 3,
    
    /// <summary>
    /// Network communication and multiplayer
    /// </summary>
    Networking = 4,
    
    /// <summary>
    /// Physics simulation and collision detection
    /// </summary>
    Physics = 5,
    
    /// <summary>
    /// Input handling and player actions
    /// </summary>
    Input = 6,
    
    /// <summary>
    /// Artificial intelligence and NPC behavior
    /// </summary>
    AI = 7,
    
    /// <summary>
    /// Performance monitoring and profiling
    /// </summary>
    Performance = 8,
    
    /// <summary>
    /// Custom user-defined categories
    /// </summary>
    Custom = 255
}
```

### Extension Methods

```csharp
public static class LogChannelExtensions
{
    private static readonly Dictionary<LogChannel, string> ChannelNames = new()
    {
        { LogChannel.None, "General" },
        { LogChannel.Core, "Core" },
        { LogChannel.UI, "UI" },
        { LogChannel.Audio, "Audio" },
        { LogChannel.Networking, "Network" },
        { LogChannel.Physics, "Physics" },
        { LogChannel.Input, "Input" },
        { LogChannel.AI, "AI" },
        { LogChannel.Performance, "Performance" },
        { LogChannel.Custom, "Custom" }
    };
    
    private static readonly Dictionary<LogChannel, string> ChannelIcons = new()
    {
        { LogChannel.None, "📋" },
        { LogChannel.Core, "⚙️" },
        { LogChannel.UI, "🖥️" },
        { LogChannel.Audio, "🔊" },
        { LogChannel.Networking, "🌐" },
        { LogChannel.Physics, "⚡" },
        { LogChannel.Input, "🎮" },
        { LogChannel.AI, "🤖" },
        { LogChannel.Performance, "📊" },
        { LogChannel.Custom, "🔧" }
    };
    
    /// <summary>
    /// Gets a user-friendly display name for the channel
    /// </summary>
    public static string GetDisplayName(this LogChannel channel)
    {
        return ChannelNames.GetValueOrDefault(channel, channel.ToString());
    }
    
    /// <summary>
    /// Gets an icon/emoji representation for the channel
    /// </summary>
    public static string GetIcon(this LogChannel channel)
    {
        return ChannelIcons.GetValueOrDefault(channel, "📋");
    }
    
    /// <summary>
    /// Gets the display color for the channel
    /// </summary>
    public static Color32 GetDisplayColor(this LogChannel channel)
    {
        return channel switch
        {
            LogChannel.None => new Color32(200, 200, 200, 255),      // Light Gray
            LogChannel.Core => new Color32(100, 149, 237, 255),      // Cornflower Blue
            LogChannel.UI => new Color32(138, 43, 226, 255),         // Blue Violet
            LogChannel.Audio => new Color32(255, 165, 0, 255),       // Orange
            LogChannel.Networking => new Color32(50, 205, 50, 255),  // Lime Green
            LogChannel.Physics => new Color32(255, 69, 0, 255),      // Red Orange
            LogChannel.Input => new Color32(218, 112, 214, 255),     // Orchid
            LogChannel.AI => new Color32(255, 20, 147, 255),         // Deep Pink
            LogChannel.Performance => new Color32(0, 191, 255, 255), // Deep Sky Blue
            LogChannel.Custom => new Color32(169, 169, 169, 255),    // Dark Gray
            _ => new Color32(255, 255, 255, 255)
        };
    }
    
    /// <summary>
    /// Parses a log channel from string, case insensitive
    /// </summary>
    public static LogChannel Parse(string channelString)
    {
        if (Enum.TryParse<LogChannel>(channelString, true, out var channel))
        {
            return channel;
        }
        
        // Handle common aliases
        return channelString.ToLowerInvariant() switch
        {
            "net" or "network" => LogChannel.Networking,
            "interface" or "gui" => LogChannel.UI,
            "sound" => LogChannel.Audio,
            "perf" => LogChannel.Performance,
            "artificial intelligence" => LogChannel.AI,
            _ => LogChannel.None
        };
    }
    
    /// <summary>
    /// Gets all available channels as an enumerable
    /// </summary>
    public static IEnumerable<LogChannel> GetAllChannels()
    {
        return Enum.GetValues<LogChannel>();
    }
    
    /// <summary>
    /// Checks if the channel represents a system-level category
    /// </summary>
    public static bool IsSystemChannel(this LogChannel channel)
    {
        return channel is LogChannel.Core or LogChannel.Performance;
    }
    
    /// <summary>
    /// Checks if the channel represents a gameplay category
    /// </summary>
    public static bool IsGameplayChannel(this LogChannel channel)
    {
        return channel is LogChannel.AI or LogChannel.Physics or LogChannel.Input;
    }
}
```

### Usage Examples

```csharp
// Channel display formatting
var channel = LogChannel.Networking;
Debug.Log($"{channel.GetIcon()} {channel.GetDisplayName()}"); // "🌐 Network"

// Channel color coding
var color = LogChannel.UI.GetDisplayColor(); // Blue Violet

// Parsing from configuration
var configChannel = LogChannelExtensions.Parse("net"); // LogChannel.Networking

// Channel categorization
var channels = LogChannelExtensions.GetAllChannels();
var gameplayChannels = channels.Where(c => c.IsGameplayChannel());
var systemChannels = channels.Where(c => c.IsSystemChannel());

// Conditional logging based on channel type
if (channel.IsSystemChannel())
{
    // Handle system-level logs differently
    ProcessSystemLog(message);
}
```

## 🎨 LogFormat

The `LogFormat` struct provides template-based formatting for log messages with support for various placeholders and custom formatting options.

### Structure Definition

```csharp
[System.Serializable]
public struct LogFormat
{
    /// <summary>
    /// Template string with placeholders
    /// </summary>
    public FixedString512Bytes Template;
    
    /// <summary>
    /// Format string for timestamp placeholders
    /// </summary>
    public FixedString64Bytes TimestampFormat;
    
    /// <summary>
    /// Whether to include stack trace information
    /// </summary>
    public bool IncludeStackTrace;
    
    /// <summary>
    /// Whether to include thread information
    /// </summary>
    public bool IncludeThreadInfo;
    
    /// <summary>
    /// Whether to include source information
    /// </summary>
    public bool IncludeSourceInfo;
    
    /// <summary>
    /// Maximum length of formatted output
    /// </summary>
    public int MaxLength;
}
```

### Available Placeholders

| Placeholder | Description | Example Output |
|-------------|-------------|----------------|
| `{timestamp}` | Message creation time | `2024-01-15 14:30:25.123` |
| `{level}` | Log level name | `INFO`, `WARNING`, `ERROR` |
| `{level:short}` | Short log level | `INF`, `WRN`, `ERR` |
| `{channel}` | Channel name | `Core`, `Networking`, `UI` |
| `{channel:icon}` | Channel with icon | `🌐 Networking` |
| `{message}` | Main log message | `Player joined the game` |
| `{thread}` | Thread ID | `1`, `23` |
| `{thread:name}` | Thread name | `Main Thread` |
| `{source}` | Source location | `PlayerController.cs:45` |
| `{source:method}` | Method name only | `UpdatePlayer` |
| `{source:file}` | File name only | `PlayerController.cs` |
| `{stacktrace}` | Stack trace | `at PlayerController.Update()...` |

### Predefined Formats

```csharp
public static class LogFormats
{
    /// <summary>
    /// Simple format for console output: [LEVEL] Message
    /// </summary>
    public static readonly LogFormat Simple = new LogFormat
    {
        Template = new FixedString512Bytes("[{level:short}] {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss"),
        MaxLength = 512
    };
    
    /// <summary>
    /// Detailed format for file output
    /// </summary>
    public static readonly LogFormat Detailed = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{thread}] [{level}] [{channel}] {message}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-dd HH:mm:ss.fff"),
        IncludeThreadInfo = true,
        MaxLength = 1024
    };
    
    /// <summary>
    /// Compact format for performance logs
    /// </summary>
    public static readonly LogFormat Compact = new LogFormat
    {
        Template = new FixedString512Bytes("{timestamp} | {level:short} | {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss.fff"),
        MaxLength = 256
    };
    
    /// <summary>
    /// Debug format with source information
    /// </summary>
    public static readonly LogFormat Debug = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{level}] {source:method} - {message}"),
        TimestampFormat = new FixedString64Bytes("HH:mm:ss.fff"),
        IncludeSourceInfo = true,
        MaxLength = 1024
    };
    
    /// <summary>
    /// Comprehensive format with all information
    /// </summary>
    public static readonly LogFormat Comprehensive = new LogFormat
    {
        Template = new FixedString512Bytes("[{timestamp}] [{thread:name}] [{level}] [{channel:icon}] {source} - {message}\n{stacktrace}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-dd HH:mm:ss.fff"),
        IncludeStackTrace = true,
        IncludeThreadInfo = true,
        IncludeSourceInfo = true,
        MaxLength = 4096
    };
    
    /// <summary>
    /// JSON-structured format for parsing
    /// </summary>
    public static readonly LogFormat Json = new LogFormat
    {
        Template = new FixedString512Bytes("{\"timestamp\":\"{timestamp}\",\"level\":\"{level}\",\"channel\":\"{channel}\",\"message\":\"{message}\"}"),
        TimestampFormat = new FixedString64Bytes("yyyy-MM-ddTHH:mm:ss.fffZ"),
        MaxLength = 2048
    };
}
```

### Formatting Methods

```csharp
public static class LogFormatExtensions
{
    /// <summary>
    /// Formats a log message using the specified format
    /// </summary>
    public static FixedString4096Bytes Format(this LogFormat format, in LogMessage message)
    {
        var result = format.Template;
        
        // Replace timestamp
        if (result.ToString().Contains("{timestamp}"))
        {
            var timestamp = message.Timestamp.ToString(format.TimestampFormat.ToString());
            result = ReplaceAll(result, "{timestamp}", timestamp);
        }
        
        // Replace level
        result = ReplaceAll(result, "{level}", message.Level.ToString());
        result = ReplaceAll(result, "{level:short}", message.Level.ToShortString());
        
        // Replace channel
        result = ReplaceAll(result, "{channel}", message.Channel.ToString());
        result = ReplaceAll(result, "{channel:icon}", $"{message.Channel.GetIcon()} {message.Channel.GetDisplayName()}");
        
        // Replace message
        result = ReplaceAll(result, "{message}", message.Text.ToString());
        
        // Replace thread information
        if (format.IncludeThreadInfo)
        {
            result = ReplaceAll(result, "{thread}", message.ThreadId.ToString());
            result = ReplaceAll(result, "{thread:name}", GetThreadName(message.ThreadId));
        }
        
        // Replace source information
        if (format.IncludeSourceInfo && message.Source.IsValid)
        {
            result = ReplaceAll(result, "{source}", message.Source.ToString());
            result = ReplaceAll(result, "{source:method}", message.Source.MethodName.ToString());
            result = ReplaceAll(result, "{source:file}", message.Source.FileName.ToString());
        }
        
        // Replace stack trace
        if (format.IncludeStackTrace && message.HasStackTrace)
        {
            result = ReplaceAll(result, "{stacktrace}", message.StackTrace.ToString());
        }
        else
        {
            result = ReplaceAll(result, "{stacktrace}", "");
        }
        
        // Truncate if necessary
        if (format.MaxLength > 0 && result.Length > format.MaxLength)
        {
            var truncated = result.ToString().Substring(0, format.MaxLength - 3) + "...";
            result = new FixedString4096Bytes(truncated);
        }
        
        return new FixedString4096Bytes(result);
    }
    
    /// <summary>
    /// Creates a custom format with specified template
    /// </summary>
    public static LogFormat Custom(string template, string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff")
    {
        return new LogFormat
        {
            Template = new FixedString512Bytes(template),
            TimestampFormat = new FixedString64Bytes(timestampFormat),
            MaxLength = 1024
        };
    }
    
    private static FixedString4096Bytes ReplaceAll(FixedString4096Bytes source, string oldValue, string newValue)
    {
        return new FixedString4096Bytes(source.ToString().Replace(oldValue, newValue));
    }
    
    private static string GetThreadName(int threadId)
    {
        return threadId == 1 ? "Main Thread" : $"Thread-{threadId}";
    }
}
```

### Usage Examples

```csharp
// Using predefined formats
var message = LogMessage.Create(LogLevel.Info, "Player connected");

var simple = LogFormats.Simple.Format(message);
// Output: [INF] Player connected

var detailed = LogFormats.Detailed.Format(message);  
// Output: [2024-01-15 14:30:25.123] [1] [Info] [Core] Player connected

// Creating custom formats
var customFormat = LogFormatExtensions.Custom(
    "[{timestamp}] {channel:icon} {message}",
    "HH:mm:ss"
);

var formatted = customFormat.Format(message);
// Output: [14:30:25] ⚙️ Core Player connected

// JSON format for structured logging
var jsonMessage = LogFormats.Json.Format(message);
// Output: {"timestamp":"2024-01-15T14:30:25.123Z","level":"Info","channel":"Core","message":"Player connected"}
```

## 🏷️ LogProperties

The `LogProperties` struct provides a Burst-compatible way to attach structured data to log messages.

### Structure Definition

```csharp
[System.Serializable]
[BurstCompile]
public struct LogProperties : IDisposable
{
    private NativeHashMap<FixedString64Bytes, PropertyValue> _properties;
    
    /// <summary>
    /// Number of properties stored
    /// </summary>
    public int Count => _properties.IsCreated ? _properties.Count : 0;
    
    /// <summary>
    /// Checks if the properties container is created and valid
    /// </summary>
    public bool IsCreated => _properties.IsCreated;
    
    /// <summary>
    /// Creates a new properties container
    /// </summary>
    public static LogProperties Create(int initialCapacity = 8, Allocator allocator = Allocator.Temp)
    {
        return new LogProperties
        {
            _properties = new NativeHashMap<FixedString64Bytes, PropertyValue>(initialCapacity, allocator)
        };
    }
}
```

### Property Value Types

```csharp
[System.Serializable]
[BurstCompile]
public struct PropertyValue
{
    public PropertyType Type;
    public ValueUnion Value;
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct ValueUnion
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public bool BoolValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public int IntValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public float FloatValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public double DoubleValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public long LongValue;
        [System.Runtime.InteropServices.FieldOffset(0)] public FixedString128Bytes StringValue;
    }
    
    public enum PropertyType : byte
    {
        Bool,
        Int,
        Float,
        Double,
        Long,
        String
    }
    
    // Factory methods
    public static PropertyValue FromBool(bool value) => new() { Type = PropertyType.Bool, Value = new() { BoolValue = value } };
    public static PropertyValue FromInt(int value) => new() { Type = PropertyType.Int, Value = new() { IntValue = value } };
    public static PropertyValue FromFloat(float value) => new() { Type = PropertyType.Float, Value = new() { FloatValue = value } };
    public static PropertyValue FromDouble(double value) => new() { Type = PropertyType.Double, Value = new() { DoubleValue = value } };
    public static PropertyValue FromLong(long value) => new() { Type = PropertyType.Long, Value = new() { LongValue = value } };
    public static PropertyValue FromString(string value) => new() { Type = PropertyType.String, Value = new() { StringValue = new FixedString128Bytes(value) } };
}
```

### Properties Methods

```csharp
public struct LogProperties
{
    /// <summary>
    /// Adds or updates a boolean property
    /// </summary>
    public void Set(string key, bool value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromBool(value);
    }
    
    /// <summary>
    /// Adds or updates an integer property
    /// </summary>
    public void Set(string key, int value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromInt(value);
    }
    
    /// <summary>
    /// Adds or updates a float property
    /// </summary>
    public void Set(string key, float value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromFloat(value);
    }
    
    /// <summary>
    /// Adds or updates a string property
    /// </summary>
    public void Set(string key, string value)
    {
        EnsureCreated();
        _properties[new FixedString64Bytes(key)] = PropertyValue.FromString(value);
    }
    
    /// <summary>
    /// Gets a property value by key
    /// </summary>
    public bool TryGet<T>(string key, out T value) where T : struct
    {
        value = default;
        
        if (!_properties.IsCreated)
            return false;
            
        if (!_properties.TryGetValue(new FixedString64Bytes(key), out var propertyValue))
            return false;
        
        return TryConvertValue(propertyValue, out value);
    }
    
    /// <summary>
    /// Checks if a property exists
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _properties.IsCreated && _properties.ContainsKey(new FixedString64Bytes(key));
    }
    
    /// <summary>
    /// Removes a property
    /// </summary>
    public bool Remove(string key)
    {
        return _properties.IsCreated && _properties.Remove(new FixedString64Bytes(key));
    }
    
    /// <summary>
    /// Clears all properties
    /// </summary>
    public void Clear()
    {
        if (_properties.IsCreated)
        {
            _properties.Clear();
        }
    }
    
    /// <summary>
    /// Gets all property keys
    /// </summary>
    public NativeArray<FixedString64Bytes> GetKeys(Allocator allocator)
    {
        if (!_properties.IsCreated)
            return new NativeArray<FixedString64Bytes>(0, allocator);
        
        return _properties.GetKeyArray(allocator);
    }
    
    private void EnsureCreated()
    {
        if (!_properties.IsCreated)
        {
            _properties = new NativeHashMap<FixedString64Bytes, PropertyValue>(8, Allocator.Temp);
        }
    }
    
    private bool TryConvertValue<T>(PropertyValue propertyValue, out T value) where T : struct
    {
        value = default;
        
        if (typeof(T) == typeof(bool) && propertyValue.Type == PropertyType.Bool)
        {
            value = (T)(object)propertyValue.Value.BoolValue;
            return true;
        }
        
        if (typeof(T) == typeof(int) && propertyValue.Type == PropertyType.Int)
        {
            value = (T)(object)propertyValue.Value.IntValue;
            return true;
        }
        
        if (typeof(T) == typeof(float) && propertyValue.Type == PropertyType.Float)
        {
            value = (T)(object)propertyValue.Value.FloatValue;
            return true;
        }
        
        if (typeof(T) == typeof(string) && propertyValue.Type == PropertyType.String)
        {
            value = (T)(object)propertyValue.Value.StringValue.ToString();
            return true;
        }
        
        return false;
    }
    
    public void Dispose()
    {
        if (_properties.IsCreated)
        {
            _properties.Dispose();
        }
    }
}
```