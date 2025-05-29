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
    public bool H