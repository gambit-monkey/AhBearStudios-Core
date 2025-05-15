# Logging Attributes Documentation

## Overview

The AhBearStudios-Core Logging system includes a set of powerful attributes that enable declarative logging capabilities within your codebase. These attributes allow you to automatically inject logging functionality into methods, classes, and properties without manually writing repetitive logging code. This document describes the available attributes and how to use them effectively.

The logging attributes system is built around the following core components:

- **LogMethodAttribute**: Automatically logs method entry, exit, and exceptions
- **LogPropertyAttribute**: Tracks property value changes
- **LogClassAttribute**: Applies logging to all methods and properties in a class
- **LogParameterAttribute**: Customizes logging for specific method parameters
- **LogIgnoreAttribute**: Excludes methods or properties from automatic logging

## LogMethodAttribute

The `LogMethodAttribute` automatically logs method entry and exit, execution time, parameter values, and return values. It also logs any exceptions thrown during method execution.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `EntryLevel` | `LogLevel` | Log level for method entry (default: Debug) |
| `ExitLevel` | `LogLevel` | Log level for method exit (default: Debug) |
| `ErrorLevel` | `LogLevel` | Log level for exceptions (default: Error) |
| `Channel` | `LogChannel` | Log channel to use (default: None) |
| `LogParameters` | `bool` | Whether to log parameter values (default: true) |
| `LogReturnValue` | `bool` | Whether to log the return value (default: true) |
| `LogExecutionTime` | `bool` | Whether to log method execution time (default: true) |

### Basic Usage

```csharp
// Log method entry and exit at Debug level, exceptions at Error level
[LogMethod]
public void ProcessData(string data)
{
    // Method implementation
}

// Log method entry at Trace level, exit at Info level
[LogMethod(EntryLevel = LogLevel.Trace, ExitLevel = LogLevel.Info)]
public int CalculateValue(int input)
{
    return input * 2;
}

// Log to a specific channel
[LogMethod(Channel = LogChannel.Networking)]
public async Task<Response> SendRequest(Request request)
{
    // Method implementation
}
```

### Advanced Configuration

```csharp
// Customize all aspects of method logging
[LogMethod(
    EntryLevel = LogLevel.Debug,
    ExitLevel = LogLevel.Info,
    ErrorLevel = LogLevel.Critical,
    Channel = LogChannel.Core,
    LogParameters = true,
    LogReturnValue = true,
    LogExecutionTime = true
)]
public Result ProcessComplexOperation(int id, string name, Options options)
{
    // Complex implementation
    return new Result();
}
```

## LogPropertyAttribute

The `LogPropertyAttribute` logs property value changes. It can be configured to log both before and after the change or just the new value.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Level` | `LogLevel` | Log level for property changes (default: Debug) |
| `Channel` | `LogChannel` | Log channel to use (default: None) |
| `LogOldValue` | `bool` | Whether to log the previous value (default: true) |
| `LogOnlyWhenChanged` | `bool` | Whether to log only when the value actually changes (default: true) |

### Basic Usage

```csharp
// Log property changes at Debug level
private string _status;
[LogProperty]
public string Status
{
    get => _status;
    set => _status = value;
}

// Log property changes at Info level on the UI channel
private bool _isVisible;
[LogProperty(Level = LogLevel.Info, Channel = LogChannel.UI)]
public bool IsVisible
{
    get => _isVisible;
    set => _isVisible = value;
}
```

### Advanced Configuration

```csharp
// Customize property change logging
private int _healthPoints;
[LogProperty(
    Level = LogLevel.Debug,
    Channel = LogChannel.Core,
    LogOldValue = true,
    LogOnlyWhenChanged = true
)]
public int HealthPoints
{
    get => _healthPoints;
    set => _healthPoints = value;
}
```

## LogClassAttribute

The `LogClassAttribute` applies logging to all methods and properties in a class, reducing the need to add attributes to each member individually. It serves as a template that can be overridden by member-specific attributes.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MethodEntryLevel` | `LogLevel` | Default log level for method entries (default: Debug) |
| `MethodExitLevel` | `LogLevel` | Default log level for method exits (default: Debug) |
| `MethodErrorLevel` | `LogLevel` | Default log level for method exceptions (default: Error) |
| `PropertyLevel` | `LogLevel` | Default log level for property changes (default: Debug) |
| `Channel` | `LogChannel` | Default log channel for all logging (default: None) |
| `LogMethodParameters` | `bool` | Whether to log method parameters by default (default: true) |
| `LogMethodReturnValues` | `bool` | Whether to log method return values by default (default: true) |
| `LogMethodExecutionTime` | `bool` | Whether to log method execution time by default (default: true) |
| `LogPropertyOldValue` | `bool` | Whether to log property old values by default (default: true) |
| `LogPropertyOnlyWhenChanged` | `bool` | Whether to log property changes only when values actually change (default: true) |

### Basic Usage

```csharp
// Apply logging to all methods and properties in a class
[LogClass]
public class PlayerController
{
    private int _health;
    public int Health
    {
        get => _health;
        set => _health = value;
    }
    
    public void Move(Vector3 direction)
    {
        // Movement implementation
    }
    
    public void TakeDamage(int amount)
    {
        Health -= amount;
    }
}
```

### Advanced Configuration

```csharp
// Customize class-wide logging configuration
[LogClass(
    MethodEntryLevel = LogLevel.Debug,
    MethodExitLevel = LogLevel.Info,
    MethodErrorLevel = LogLevel.Critical,
    PropertyLevel = LogLevel.Debug,
    Channel = LogChannel.Core,
    LogMethodParameters = true,
    LogMethodReturnValues = true,
    LogMethodExecutionTime = true,
    LogPropertyOldValue = true,
    LogPropertyOnlyWhenChanged = true
)]
public class NetworkManager
{
    // All methods and properties will use the class-level logging configuration
    // unless they have their own attributes
}
```

## LogParameterAttribute

The `LogParameterAttribute` customizes how specific method parameters are logged. It can be used to control the format, provide a custom name, or exclude sensitive parameters from logging.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Format` | `string` | Custom format string for the parameter value |
| `Name` | `string` | Custom name to use in logs instead of parameter name |
| `Exclude` | `bool` | Whether to exclude this parameter from logs (default: false) |

### Usage Examples

```csharp
public void ProcessPayment(
    string orderId,
    [LogParameter(Exclude = true)] string creditCardNumber,
    [LogParameter(Name = "Amount")] decimal paymentAmount,
    [LogParameter(Format = "{0:yyyy-MM-dd}")] DateTime paymentDate)
{
    // Implementation
}
```

## LogIgnoreAttribute

The `LogIgnoreAttribute` excludes methods or properties from automatic logging, even if the containing class has the `LogClassAttribute`.

### Usage Examples

```csharp
[LogClass]
public class UserManager
{
    // This method will be automatically logged
    public User GetUser(int userId)
    {
        // Implementation
    }
    
    // This method will NOT be logged
    [LogIgnore]
    public void ValidateCredentials(string username, string password)
    {
        // Sensitive operation that shouldn't be logged
    }
    
    // This property will NOT have value changes logged
    [LogIgnore]
    public string SessionToken { get; set; }
}
```

## Integration with Logging System

The attributes system integrates with the core `Logger` class and logging configuration to provide a cohesive logging experience.

### Runtime Configuration Interaction

The attributes respect the logging configuration settings, including minimum log levels and channel-specific log levels:

```csharp
// Method with LogMethod attribute
[LogMethod(EntryLevel = LogLevel.Debug, Channel = LogChannel.Networking)]
public void Connect(string server)
{
    // If the logging configuration has a minimum level of Info for the Networking channel,
    // the method entry log (at Debug level) won't be output, even though the attribute
    // requests it, because Debug < Info
}
```

### Attribute Processor Registration

For the logging attributes to function, you need to register the attribute processor during application startup:

```csharp
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Create and configure logging
        var config = new LoggingConfiguration();
        // ... configure targets, levels, etc.
        
        // Initialize the logger
        Logger.Initialize(config);
        
        // Register the attribute processor
        LoggingAttributeProcessor.Register();
    }
}
```

## Advanced Usage Scenarios

### Custom Formatting

You can combine attributes with custom formatting for more detailed logs:

```csharp
[LogClass(Channel = LogChannel.Networking)]
public class ApiClient
{
    [LogMethod(
        EntryLevel = LogLevel.Debug,
        ExitLevel = LogLevel.Info
    )]
    public async Task<ApiResponse> SendRequest(
        [LogParameter(Name = "Endpoint")] string url,
        [LogParameter(Format = "{0} bytes")] byte[] payload)
    {
        // Implementation
    }
}
```

### Performance Tracking

Use method execution time logging to identify performance bottlenecks:

```csharp
[LogMethod(
    EntryLevel = LogLevel.Trace,
    ExitLevel = LogLevel.Debug,
    LogExecutionTime = true,
    Channel = LogChannel.Performance
)]
public void ProcessLargeDataSet(List<DataItem> items)
{
    // The execution time will be logged when the method completes
}
```

### Hierarchical Attribute Configuration

When you have both class and method/property level attributes, the more specific attribute overrides the class-level settings:

```csharp
[LogClass(
    MethodEntryLevel = LogLevel.Debug,
    MethodExitLevel = LogLevel.Debug,
    PropertyLevel = LogLevel.Debug,
    Channel = LogChannel.Core
)]
public class GameManager
{
    // Uses class-level settings: Debug level, Core channel
    public void StartGame()
    {
        // Implementation
    }
    
    // Overrides class-level settings: Trace entry level, Info exit level, Performance channel
    [LogMethod(
        EntryLevel = LogLevel.Trace,
        ExitLevel = LogLevel.Info,
        Channel = LogChannel.Performance
    )]
    public void LoadLevel(int levelId)
    {
        // Implementation
    }
    
    // Uses class-level settings for level (Debug), but overrides channel to UI
    [LogProperty(Channel = LogChannel.UI)]
    public GameState CurrentState { get; set; }
}
```

## Practical Examples

### Player Health System

```csharp
[LogClass(Channel = LogChannel.Core)]
public class HealthSystem
{
    private int _currentHealth;
    
    [LogProperty(
        Level = LogLevel.Info,
        LogOldValue = true,
        LogOnlyWhenChanged = true
    )]
    public int CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }
    
    public int MaxHealth { get; private set; }
    
    [LogMethod(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug)]
    public void Initialize(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }
    
    [LogMethod(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Info)]
    public bool TakeDamage(int amount)
    {
        if (amount <= 0) return false;
        
        CurrentHealth -= amount;
        return CurrentHealth <= 0;
    }
    
    [LogMethod(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Info)]
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        
        CurrentHealth += amount;
    }
}
```

### Network Connection Handler

```csharp
[LogClass(
    MethodEntryLevel = LogLevel.Debug,
    MethodExitLevel = LogLevel.Info,
    MethodErrorLevel = LogLevel.Error,
    PropertyLevel = LogLevel.Debug,
    Channel = LogChannel.Networking,
    LogMethodExecutionTime = true
)]
public class NetworkConnectionHandler
{
    [LogProperty(Level = LogLevel.Info)]
    public ConnectionState State { get; private set; }
    
    [LogProperty]
    public string ServerAddress { get; private set; }
    
    [LogProperty(LogOldValue = false)]
    public int RetryCount { get; private set; }
    
    [LogMethod(
        EntryLevel = LogLevel.Info,
        ExitLevel = LogLevel.Info,
        ErrorLevel = LogLevel.Critical
    )]
    public async Task<bool> Connect(
        string serverAddress,
        [LogParameter(Exclude = true)] string authToken)
    {
        ServerAddress = serverAddress;
        State = ConnectionState.Connecting;
        
        try
        {
            // Connection logic
            State = ConnectionState.Connected;
            return true;
        }
        catch (Exception)
        {
            State = ConnectionState.Disconnected;
            RetryCount++;
            throw;
        }
    }
    
    [LogMethod]
    public void Disconnect()
    {
        // Disconnect logic
        State = ConnectionState.Disconnected;
    }
    
    [LogIgnore]
    private void UpdateInternalState()
    {
        // This helper method won't be logged
    }
}
```

## Best Practices

1. **Be selective with attribute usage**: Apply attributes where logging provides valuable information, not everywhere. Over-logging can decrease performance and create noise.

2. **Consider log levels carefully**: Use appropriate log levels to ensure logs can be filtered effectively in different environments:
   - `Trace`/`Debug`: Development-time diagnostics
   - `Info`: General operational information
   - `Warning`/`Error`/`Critical`: Issues that require attention

3. **Use class-level attributes for consistency**: Apply `LogClassAttribute` to maintain consistent logging across a class, then override specific methods or properties as needed.

4. **Protect sensitive data**: Use `LogParameterAttribute(Exclude = true)` for sensitive parameters like passwords, tokens, and personal information.

5. **Balance performance impact**: Logging, especially with attributes, adds overhead. Monitor performance impact and adjust logging levels in production.

6. **Implement custom formatting**: Use the `Format` property in `LogParameterAttribute` to make logs more readable and useful.

7. **Leverage execution time logging**: Use `LogExecutionTime = true` to identify performance bottlenecks in critical methods.

8. **Be intentional with property change logging**: Use `LogOnlyWhenChanged = true` for properties that change frequently to reduce log volume.

## Troubleshooting Common Issues

### Attributes Not Working

1. Verify that `LoggingAttributeProcessor.Register()` has been called during application initialization.
2. Check that the logging system itself has been properly initialized with `Logger.Initialize(config)`.
3. Ensure the log levels specified in attributes are at or above the minimum levels set in your logging configuration.

### Performance Degradation

1. Reduce the use of attributes on frequently called methods.
2. Increase the minimum log level in your logging configuration to filter out less important logs.
3. Set `LogExecutionTime = false` for methods where timing isn't critical.
4. Use `LogOnlyWhenChanged = true` for properties to reduce log volume.

### Excessive Log Volume

1. Review your attribute usage and remove unnecessary logging.
2. Increase minimum log levels for specific channels in your logging configuration.
3. Apply `LogIgnoreAttribute` to noisy methods or properties.

## Further Reading

For more information on the AhBearStudios-Core logging system, refer to:

- [Logger Class Documentation](../logger.md)
- [Logging Configuration Documentation](../loggingconfiguration.md)
- [Log Messages Documentation](../logmessage.md)
