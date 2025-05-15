# Logging Events System

## Overview

The AhBearStudios.Core Logging Events system provides a robust framework for creating, dispatching, and handling logging events throughout your application. This system integrates with the broader AhBearStudios logging infrastructure to enable flexible, extensible logging with support for different log levels, contextual information, and custom event handling.

## Key Components

The Events system consists of several key components:

1. **LogEvent**: Base event class that all logging events inherit from
2. **ILogEventDispatcher**: Interface for dispatching log events
3. **LogEventDispatcher**: Default implementation of the ILogEventDispatcher interface
4. **LogEventExtensions**: Extension methods to simplify working with log events
5. **LogEventHandler**: Delegate for handling log events
6. **LogLevel**: Enumeration of log severity levels
7. **StackFrame API**: Tools for capturing and managing stack frames

## Log Levels

The logging system supports multiple log severity levels:

| Level | Purpose |
|-------|---------|
| `Trace` | Highly detailed information, typically only enabled during extended debugging sessions |
| `Debug` | Information useful for debugging, more verbose than Info |
| `Info` | General information about application flow |
| `Warning` | Potential issues that aren't immediately problematic |
| `Error` | Runtime errors or unexpected conditions |
| `Critical` | Critical failures requiring immediate attention |

## Basic Usage

### Creating a Log Event

```csharp
// Create a simple log event
var logEvent = new LogEvent(LogLevel.Info, "Component initialized successfully");

// Create a log event with additional context
var contextualEvent = new LogEvent(
    LogLevel.Warning,
    "Resource loading failed",
    new Dictionary<string, object> {
        { "resourcePath", "Assets/Textures/background.png" },
        { "attemptCount", 3 }
    }
);
```

### Dispatching Log Events

```csharp
// Get or create a dispatcher
ILogEventDispatcher dispatcher = LogEventDispatcher.Instance;

// Dispatch an event
dispatcher.Dispatch(new LogEvent(LogLevel.Info, "Application started"));

// Using extension methods for convenience
dispatcher.LogInfo("Player joined the game", new Dictionary<string, object> {
    { "playerId", "user-123" },
    { "sessionId", "abc-xyz-789" }
});

// Error logging with exception
try {
    // Some operation that might fail
} catch (Exception ex) {
    dispatcher.LogError("Failed to load configuration", ex);
}
```

### Registering Event Handlers

```csharp
// Create a handler
LogEventHandler handler = (LogEvent evt) => {
    Debug.Log($"[{evt.Level}] {evt.Message}");
    
    // Access additional context if available
    if (evt.Context != null && evt.Context.ContainsKey("playerId")) {
        Debug.Log($"Player ID: {evt.Context["playerId"]}");
    }
};

// Register the handler
LogEventDispatcher.Instance.RegisterHandler(handler);

// Unregister when no longer needed
LogEventDispatcher.Instance.UnregisterHandler(handler);
```

## Advanced Features

### Conditional Logging

The system supports conditional logging to minimize performance impact in production:

```csharp
// This will only evaluate the getMessage lambda if the Debug level is enabled
dispatcher.LogDebugIf(
    () => ShouldLogDebug(), 
    () => GenerateExpensiveDebugMessage()
);
```

### Stack Frame Capture

The logging system can automatically capture stack frame information:

```csharp
// Create a log event with stack frame capture
var event = new LogEvent(
    LogLevel.Error, 
    "Invalid operation detected", 
    captureStackFrame: true
);

// Access stack frame information
if (event.StackFrame != null) {
    Console.WriteLine($"Error occurred in {event.StackFrame.MethodName} at line {event.StackFrame.LineNumber}");
}
```

### Custom Event Types

You can extend the `LogEvent` class to create custom event types:

```csharp
public class NetworkLogEvent : LogEvent
{
    public string RemoteAddress { get; }
    public int StatusCode { get; }
    
    public NetworkLogEvent(
        LogLevel level, 
        string message, 
        string remoteAddress, 
        int statusCode, 
        Dictionary<string, object> context = null) 
        : base(level, message, context)
    {
        RemoteAddress = remoteAddress;
        StatusCode = statusCode;
    }
}

// Using the custom event type
dispatcher.Dispatch(new NetworkLogEvent(
    LogLevel.Warning,
    "API request failed",
    "api.example.com",
    404,
    new Dictionary<string, object> {
        { "endpoint", "/users" },
        { "requestId", "req-123" }
    }
));
```

### Filtering Events

You can implement filtering logic in your event handlers:

```csharp
LogEventHandler filteredHandler = (LogEvent evt) => {
    // Only process Info or higher severity
    if (evt.Level >= LogLevel.Info) {
        // Handle the event
    }
    
    // Only process events from a specific component
    if (evt.Context != null && 
        evt.Context.TryGetValue("component", out var component) &&
        component.ToString() == "NetworkManager") {
        // Handle the networking-specific event
    }
};
```

## Integration with Unity

The logging system integrates seamlessly with Unity:

```csharp
// Register a Unity-specific handler
LogEventDispatcher.Instance.RegisterHandler(evt => {
    switch (evt.Level) {
        case LogLevel.Debug:
        case LogLevel.Info:
        case LogLevel.Trace:
            UnityEngine.Debug.Log($"[{evt.Level}] {evt.Message}");
            break;
        case LogLevel.Warning:
            UnityEngine.Debug.LogWarning($"[{evt.Level}] {evt.Message}");
            break;
        case LogLevel.Error:
        case LogLevel.Critical:
            UnityEngine.Debug.LogError($"[{evt.Level}] {evt.Message}");
            break;
    }
});
```

## Best Practices

1. **Consistent Log Levels**: Establish clear guidelines for when to use each log level
2. **Contextual Information**: Include relevant contextual data to make debugging easier
3. **Performance Awareness**: Use conditional logging for verbose or high-frequency events
4. **Structured Logging**: Include structured data in the context dictionary rather than embedding it in the message string
5. **Handler Management**: Unregister handlers when they are no longer needed to prevent memory leaks
6. **Custom Events**: Create specialized event types for different subsystems for better organization

## Complete Architecture Diagram

```
                 ┌─────────────────┐
                 │   Application   │
                 └────────┬────────┘
                          │
                          ▼
           ┌─────────────────────────────┐
           │     LogEventDispatcher      │
           │                             │
           │  - RegisterHandler()        │
           │  - UnregisterHandler()      │
           │  - Dispatch()               │
           └───────────────┬─────────────┘
                           │
                 ┌─────────┴─────────┐
                 │                   │
        ┌────────▼──────┐    ┌───────▼──────┐
        │   LogEvent    │    │ Event Handlers│
        │               │    │               │
        │ - Level       │    │ - Console     │
        │ - Message     │    │ - File        │
        │ - Context     │    │ - Unity Debug │
        │ - Timestamp   │    │ - Analytics   │
        │ - StackFrame  │    │ - Custom      │
        └───────────────┘    └───────────────┘
                 │
        ┌────────┴──────────┐
        │                   │
┌───────▼───────┐    ┌──────▼───────┐
│ Custom Events  │    │  LogLevel    │
│               │    │              │
│ - NetworkLog  │    │ - Trace      │
│ - SecurityLog │    │ - Debug      │
│ - PerformanceLog│   │ - Info       │
└───────────────┘    │ - Warning    │
                     │ - Error      │
                     │ - Critical   │
                     └──────────────┘
```

## Contributing to the Logging System

When extending the logging system, follow these guidelines:

1. New log event types should inherit from `LogEvent`
2. Custom dispatchers should implement `ILogEventDispatcher`
3. Extension methods should be added to `LogEventExtensions`
4. Maintain backward compatibility when modifying existing classes

## Troubleshooting

### Common Issues

1. **Events not being logged**: Ensure you've registered appropriate handlers with the dispatcher
2. **High performance overhead**: Review the frequency of log calls, especially in tight loops
3. **Missing context information**: Verify context dictionaries are being properly populated

### Debug Mode

Enable debug mode for the logging system to troubleshoot issues:

```csharp
LogEventDispatcher.EnableDebugMode = true;
```

This will output additional information about event dispatching and handler execution.

## API Reference

### LogEvent

```csharp
public class LogEvent
{
    public LogLevel Level { get; }
    public string Message { get; }
    public Dictionary<string, object> Context { get; }
    public DateTime Timestamp { get; }
    public StackFrame StackFrame { get; }
    
    public LogEvent(
        LogLevel level, 
        string message, 
        Dictionary<string, object> context = null, 
        bool captureStackFrame = false);
}
```

### ILogEventDispatcher

```csharp
public interface ILogEventDispatcher
{
    void RegisterHandler(LogEventHandler handler);
    void UnregisterHandler(LogEventHandler handler);
    void Dispatch(LogEvent logEvent);
}
```

### LogEventExtensions

```csharp
public static class LogEventExtensions
{
    public static void LogTrace(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogDebug(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogInfo(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogWarning(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogError(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogError(this ILogEventDispatcher dispatcher, string message, Exception exception, Dictionary<string, object> context = null);
    public static void LogCritical(this ILogEventDispatcher dispatcher, string message, Dictionary<string, object> context = null);
    public static void LogCritical(this ILogEventDispatcher dispatcher, string message, Exception exception, Dictionary<string, object> context = null);
    
    // Conditional logging methods
    public static void LogTraceIf(this ILogEventDispatcher dispatcher, Func<bool> condition, Func<string> getMessage);
    public static void LogDebugIf(this ILogEventDispatcher dispatcher, Func<bool> condition, Func<string> getMessage);
    // ... etc.
}
```

## See Also

- [General Logging Documentation](./logging.md)
- [Filters Documentation](./filters.md)
- [Formatters Documentation](./formatters.md)
- [Sinks Documentation](./sinks.md)
