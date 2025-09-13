# AhBearStudios Core - Unity Game Development Architecture

This document provides Claude Code with essential information about the AhBearStudios Core project structure, development patterns, and Unity-specific requirements.

## Project Overview

AhBearStudios Core is a **modular, high-performance Unity game development framework** that prioritizes practical game solutions over enterprise patterns. The architecture focuses on:

- **Game Development First**: 60+ FPS performance targets, frame budget constraints (16.67ms), memory-conscious design
- **Unity-Native Integration**: Built for Unity's component system, Inspector workflow, and platform diversity
- **Modular Architecture**: Swappable interfaces, dependency injection via Reflex, compositional design
- **Performance Critical**: Unity Jobs & Burst, Unity Collections v2, zero-allocation patterns where possible

### Package Architecture

The framework is split into two distinct packages for maximum flexibility:

1. **com.ahbearstudios.core** - Platform-agnostic POCO (Plain Old C# Objects) systems
   - Pure C# implementations without Unity dependencies
   - Can be used in server applications or tools
   - Easier to unit test without Unity runtime
   
2. **com.ahbearstudios.unity** - Unity-specific implementations
   - MonoBehaviour components and managers
   - ScriptableObject configurations
   - Unity Editor tools and inspectors
   - References and extends core functionality

## Core Architecture Pattern

**ALWAYS follow Builder → Config → Factory → Service design flow**

This pattern ensures consistent, testable, and performant code:
1. **Builder** creates configurations and handles complexity
2. **Config** holds runtime/design-time settings
3. **Factory** creates instances using configs (simple creation only)
4. **Service** provides the actual functionality

### Builder → Factory → Service Responsibilities

#### Builders Handle Complexity
- **Builders manage configuration complexity** through fluent APIs
- Handle validation, default values, and complex setup logic
- Create rich, validated configurations for factories
- Example: `FilterConfigBuilder` manages complex filter setup

#### Factories Are Simple Creation Only
- **Factories only create, never destroy** - no lifecycle management
- Take validated configs and create instances
- **DO NOT implement IDisposable** on factories
- Keep factory methods simple and focused on instantiation
- Example: `AlertServiceFactory.CreateAlertServiceAsync(config)`

#### Object Lifecycle Management
- **IPoolingService manages object lifecycle**, not factories
- Services and managers handle their own disposal when needed
- Factories are stateless creation utilities
- Clear separation: Creation (Factory) vs Lifecycle (Pooling/Service)

### Pattern Example

```csharp
// Builder handles complexity, creates validated config
var config = new AlertConfigBuilder(_poolingService)
    .AddSeverityFilter(AlertSeverity.Warning)
    .AddRateLimitFilter("RateLimit", 100)
    .Build();

// Factory creates instances (NO IDisposable, no tracking)
var alertService = await _alertServiceFactory.CreateAlertServiceAsync(config);

// Service manages its own lifecycle
public class AlertService : IAlertService, IDisposable
{
    public void Dispose() { /* cleanup */ }
}
```

## Code Organization

### Functional Domain Organization (NOT Layered)

Code is organized by **functional domain**, not architectural layers. Related functionality stays together.

### Package Structure
The project is organized into two separate Unity packages:
- **com.ahbearstudios.core** - POCO core systems (platform-agnostic)
- **com.ahbearstudios.unity** - Unity-specific implementations

### Core POCO System Structure
```
Assets/com.ahbearstudios.core/
├── AhBearStudios.Core.asmdef        // Core assembly definition
├── {FunctionalSystem}/               // e.g., Logging, Messaging, Pooling
│   ├── I{System}Service.cs          // Primary interface at root
│   ├── {System}Service.cs           // Primary implementation at root
│   ├── Configs/                     // Configuration objects
│   ├── Builders/                    // Builder pattern implementations
│   ├── Factories/                   // Factory pattern implementations
│   ├── Services/                    // Additional service implementations
│   ├── Models/                      // Data structures, enums, structs
│   └── HealthChecks/                // Health monitoring
└── Common/                           // Shared utilities across systems
    └── Models/                       // Shared data structures
```

### Unity Application Layer Structure
```
Assets/com.ahbearstudios.unity/
├── AhBearStudios.Unity.asmdef       // Unity assembly definition
├── package.json                     // Unity package metadata
└── {GameSystem}/                    // e.g., Logging, Messaging, Serialization
    ├── I{System}Manager.cs          // Primary interface at root
    ├── {System}Manager.cs           // MonoBehaviour implementation at root
    ├── Configs/                     // ScriptableObject configurations
    ├── Builders/                    // Unity object builders
    ├── Components/                  // MonoBehaviour components
    ├── Systems/                     // Game logic (avoid MonoBehaviour when possible)
    └── Monitoring/                  // Performance profiling
```

## Development Commands

For detailed build, test, and Unity commands, see [COMMANDS.md](COMMANDS.md).

## Core Systems

- **Dependency Injection (Reflex)**: `Container.Bind<TInterface>().To<TImplementation>()`, constructor injection preferred
- **Logging**: `ILoggingService` - structured levels (Debug, Info, Warning, Error, Critical), performance markers, Unity Console integration
- **Message Bus**: `IMessageBusService` - event-driven pub/sub communication between systems
- **Object Pooling**: `IPoolingService` - zero-allocation object reuse, critical for mobile performance
- **Alerting**: `IAlertService` - runtime error handling with severity-based notifications
- **Serialization**: `ISerializationService` - **ALWAYS use instead of direct MemoryPack calls**, unified data serialization with MemoryPack backend
- **Health Checking**: `IHealthCheckService` - runtime system health monitoring, performance metrics, status reporting
- **Profiling**: `IProfilerService` - unified profiling abstraction that wraps Unity's ProfilerMarker internally while adding metrics, thresholds, production monitoring, and custom performance tracking capabilities

## Common Utilities

### Shared Models and Classes
- Location: `AhBearStudios.Core.Common`
- Purpose: Classes, structs, enums used across multiple systems
- Examples:
  - `AhBearStudios.Core.Common.Models` - Shared data structures
  - `AhBearStudios.Core.Common.Extensions` - Extension methods
  - `AhBearStudios.Core.Common.Utilities` - Helper classes
  - `AhBearStudios.Core.Common.Constants` - Shared constants

When creating models or utilities that will be used by multiple systems, **ALWAYS place them in Common namespace**, not in a specific system namespace.

## Performance Libraries

### ZLinq Instead of LINQ
- **ALWAYS use ZLinq instead of System.Linq** for zero-allocation operations
- Use `.AsValueEnumerable()` to enable ZLinq operations
```csharp
// ❌ AVOID: Standard LINQ
using System.Linq;
var filtered = items.Where(x => x.IsActive).ToList();

// ✅ CORRECT: ZLinq
using ZLinq;
var filtered = items.AsValueEnumerable().Where(x => x.IsActive).ToList();
```

### UniTask Instead of Task
- **ALWAYS use UniTask** for Unity-optimized async operations
```csharp
// ❌ AVOID: async Task LoadDataAsync() { }
// ✅ CORRECT: async UniTask LoadDataAsync() { }
```

## Unity 6+ .NET Compatibility

### .NET Profile and Language Support
- Project targets **Unity 6+** with **.NET Standard 2.1**
- **C# 10 language features enabled** via `csc.rsp` configuration (`-langversion:10`)
- Most modern .NET classes and features are supported
- Full access to C# 10 syntax and language features

### Available C# 10 Features
```csharp
// File-scoped namespaces, global using, records, pattern matching
namespace AhBearStudios.Core.Logging;
global using System;
public record PlayerData(int Id, string Name);
var result = value switch { > 100 => "High", > 50 => "Medium", _ => "Low" };
```

### Unity-Specific Limitations and Constraints
While Unity 6+ supports most .NET features, be aware of these Unity-specific constraints:

#### Platform and Runtime Limitations
- **System.Drawing**: Not guaranteed to work on all Unity deployment platforms
- **File I/O**: Some `System.IO` operations may have restrictions on mobile/console platforms
- **Reflection**: IL2CPP AOT compilation may limit dynamic reflection and code generation
- **Threading**: Follow Unity's threading model (main thread + Unity Jobs system)

#### Deployment Target Considerations
- Test cross-platform compatibility for all target platforms
- Mobile platforms may have additional .NET API restrictions
- Console platforms may limit certain system-level operations

### Preferred Unity Patterns Over Standard .NET

#### Unity-Specific Preferences
- **Performance**: Use Unity.Collections for performance-critical code
- **Existing Patterns**: Use project implementations over standard .NET equivalents  
- **Threading**: Use Unity Jobs instead of System.Threading.Tasks
- **Serialization**: Use ISerializationService instead of standard .NET serialization
- **Async**: Use UniTask instead of Task

### Development Guidelines
- Test on all target deployment platforms and verify IL2CPP compatibility
- Prefer Unity-native APIs and check platform-specific limitations
- Use modern C# 10 features while following Unity patterns
- Always test cross-platform compatibility

## Message Bus Usage

### IMessage Pattern & Implementation Standards

- **ALWAYS use IMessageBusService for events - NEVER create raw C# events**
- **ALWAYS implement static factory methods for IMessage creation**
- **NEVER use field initializers in IMessage structs**
- **ALWAYS use explicit parameter names for DeterministicIdGenerator calls**

#### Required IMessage Structure

All IMessages MUST follow this exact pattern:

```csharp
/// <summary>
/// Message description and purpose.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct ExampleMessage : IMessage
{
    #region IMessage Implementation
    
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the message type code for efficient routing and filtering.
    /// </summary>
    public ushort TypeCode { get; init; }

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; }

    #endregion

    #region Message-Specific Properties

    /// <summary>
    /// Gets the message-specific data.
    /// </summary>
    public FixedString64Bytes Data { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new ExampleMessage with proper validation and defaults.
    /// </summary>
    /// <param name="data">The message data</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New ExampleMessage instance</returns>
    public static ExampleMessage Create(
        string data,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.Normal)
    {
        // Input validation
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "DefaultSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("ExampleMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("Example", data)
            : correlationId;

        return new ExampleMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.ExampleMessage,
            Source = source.IsEmpty ? "DefaultSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            Data = data?.Length <= 64 ? data : data?[..64] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Example message string representation</returns>
    public override string ToString()
    {
        return $"ExampleMessage: {Data} from {Source}";
    }

    #endregion
}
```

#### IMessage Creation Rules

1. **Static Factory Methods**: All IMessages MUST have static `Create()` methods
2. **No Field Initializers**: NEVER use `{ get; init; } = value` - causes constructor requirement errors
3. **Parameter Validation**: Always validate input parameters in Create methods
4. **Explicit DeterministicIdGenerator Calls**: Always use named parameters to avoid ambiguous invocations
5. **Default Source Handling**: Provide sensible defaults for source when not specified
6. **String Length Validation**: Truncate strings to fit FixedString types
7. **Comprehensive Documentation**: Include XML docs for all Create methods

#### Usage Examples

```csharp
// ✅ CORRECT: Using static factory method
var message = PlayerSpawnedMessage.Create(
    playerId: 123,
    position: Vector3.zero,
    source: "GameManager",
    priority: MessagePriority.Normal
);

_messageBus.PublishMessage(message);
_messageBus.Subscribe<PlayerSpawnedMessage>(OnPlayerSpawned);

// ❌ AVOID: Direct struct construction
var message = new PlayerSpawnedMessage { PlayerId = 123, Position = Vector3.zero };
```

## Message Type Code Standards

### Context-Prefixed Naming Convention

**ALWAYS use system context prefixes for all message type codes** to eliminate ambiguity and enable immediate identification of message origin.

#### Naming Pattern: `{System}{Action}Message`

All message type codes follow this strict pattern:
- **Core System**: `Core{Action}Message`
- **Messaging System**: `MessageBus{Action}Message` 
- **Logging System**: `Logging{Action}Message`
- **Health System**: `HealthCheck{Action}Message`
- **Pooling System**: `Pool{Action}Message`
- **Alerting System**: `Alert{Action}Message`

#### Examples:
```csharp
// ✅ CORRECT: Clear system context
CoreSystemStartupMessage = 1001
MessageBusCircuitBreakerStateChangedMessage = 1062
PoolExpansionMessage = 1300
AlertRaisedMessage = 1401

// ❌ AVOID: Ambiguous without context
SystemStartup = 1001
CircuitBreakerStateChanged = 1304
Expansion = 1300
Raised = 1401
```

### Type Code Range Allocation

**Ranges are strictly enforced** to prevent conflicts and enable efficient routing:

```
Core System:        1000-1049 (System startup, shutdown, general)
Messaging System:   1050-1099 (Message bus, routing, subscriptions)  
Logging System:     1100-1199 (Logging infrastructure)
Health System:      1200-1299 (Health checks and monitoring)
Pooling System:     1300-1399 (Object pooling strategies)
Alerting System:    1400-1499 (Alert and notification messages)
Profiling System:   1500-1599 (Performance profiling)
Serialization:      1600-1699 (Serialization infrastructure)
Authentication:     1700-1799 (Auth and security)
Networking:         1800-1899 (Network communication)
User Interface:     1900-1999 (UI and interaction)
Game Systems:       2000-2999 (Game-specific messages)
Custom/Third-party: 3000-64999 (Custom integrations)
Reserved/Testing:   65000-65535 (Special cases and testing)
```

### Message Implementation Requirements

#### IMessage Interface Compliance & ID Generation
All messages **MUST implement IMessage interface completely** and **use static factory methods with DeterministicIdGenerator**:

```csharp
// ✅ CORRECT: Full IMessage implementation with static factory method
public record struct AlertRaisedMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public FixedString64Bytes Source { get; init; }
    public MessagePriority Priority { get; init; }
    public Guid CorrelationId { get; init; }
    
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; }

    public static AlertRaisedMessage Create(
        AlertSeverity severity,
        string message,
        string userId,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var sourceString = source.IsEmpty ? "AlertSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("AlertRaisedMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("AlertOperation", userId)
            : correlationId;

        return new AlertRaisedMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.AlertRaisedMessage,
            Source = source.IsEmpty ? "AlertSystem" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            Severity = severity,
            Message = message
        };
    }
}
```

#### ID Generation Standards
- **NEVER use `new Guid()` - ALWAYS use `DeterministicIdGenerator`**
- Available methods: `GenerateMessageId()`, `GenerateCorrelationId()`, `GeneratePoolId()`, `GenerateHealthCheckId()`, `GenerateAlertId()`, `GenerateLogEntryId()`, `GenerateSessionId()`, `GenerateCoreId()`
- Benefits: Consistent IDs, easy correlation, reliable debugging, deterministic testing

#### Type Code Assignment
```csharp
// ✅ CORRECT: Use system-specific type code
TypeCode = MessageTypeCodes.MessageBusCircuitBreakerStateChangedMessage

// ❌ AVOID: Hardcoded values or wrong system codes
TypeCode = 1062 // Hard-coded
TypeCode = MessageTypeCodes.PoolCircuitBreakerStateChangedMessage // Wrong system
```

### Benefits of Context-Prefixed Naming
- **Immediate Clarity** - No guessing about message origin
- **IDE IntelliSense** - Easy to find all messages from a system  
- **Debugging** - Logs clearly show which system sent the message
- **Prevents Duplicates** - Can't accidentally create same-named messages
- **Scalability** - Easy to add new systems without naming conflicts

### Requesting New Ranges

To request a new type code range:
1. Update `MessageTypeCodes.cs` header documentation
2. Add range constants in Range Validation Constants section
3. Update `GetSystemForTypeCode()` method
4. Register the range in the MessageRegistry system

## Serialization Usage

### ISerializationService Pattern
- **NEVER use MemoryPack directly**
- **ALWAYS use ISerializationService**
- Provides consistent serialization across the project
- Example:
```csharp
// ❌ AVOID: Direct MemoryPack usage
var bytes = MemoryPackSerializer.Serialize(data);
var result = MemoryPackSerializer.Deserialize<MyData>(bytes);

// ✅ CORRECT: ISerializationService
var bytes = _serializationService.Serialize(data);
var result = _serializationService.Deserialize<MyData>(bytes);
```

## Code Patterns

### MonoBehaviour Integration
```csharp
public class PlayerManager : MonoBehaviour, IPlayerManager
{
    // Injected services
    private ILoggingService _loggingService;
    private IMessageBusService _messageBus;
    private IProfilerService _profilerService;
    
    [Inject]
    public void Initialize(
        ILoggingService loggingService, 
        IMessageBusService messageBus,
        IProfilerService profilerService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _profilerService = profilerService ?? NullProfilerService.Instance;
    }
    
    private void Update()
    {
        using (_profilerService.BeginScope("PlayerManager.Update"))
        {
            // Game logic here
            
            // Track custom metrics
            _profilerService.RecordMetric("entities.processed", entityCount);
        }
    }
}
```

### Service Implementation
```csharp
namespace AhBearStudios.Core.Logging
{
    public interface ILoggingService { }  // At root level
    public class LoggingService : ILoggingService { }  // At root level
}
```

## Important Constraints

### Performance Requirements
- **Frame Budget**: All Update() operations must fit within 16.67ms (60 FPS)
- **Memory**: Minimize allocations, use object pooling
- **Platform Diversity**: Support low-end mobile to high-end PC

### Unity-Specific Rules
- **Inspector Friendly**: Design for Unity's visual workflow
- **Component-Based**: Leverage Unity's component system
- **ScriptableObjects**: Use for designer-friendly configuration
- **Prefab Workflow**: Support prefab variants and overrides

### Code Quality Standards
- **composition over inheritance**: Always prefer composition over inheritance
- **No incomplete code**: No TODOs, stubs, or placeholders
- **One type per file**: Each class/interface in its own file
- **Full implementation**: All methods must be implemented
- **XML comments**: Code should be well commented

## Anti-Patterns to Avoid

### Architecture & Enterprise
- ❌ **Over-abstraction**: No 5-layer abstractions
- ❌ **Heavy DI**: Don't inject everything  
- ❌ **Excessive interfaces**: Avoid micro-interfaces
- ❌ **Complex configuration**: Use simple, designer-friendly configs
- ❌ **Database patterns**: Use Unity's save system, not ORMs

### Factory & Builder
- ❌ **Factory lifecycle management**: Factories should NEVER implement IDisposable
- ❌ **Factory object tracking**: Factories don't track created objects
- ❌ **Mixed responsibilities**: Clear separation between creation and lifecycle

### Code Organization
- ❌ **Layered organization**: Group by function, not technical layers
- ❌ **Separate interface namespaces**: Keep interfaces with implementations
- ❌ **Deep nesting**: Maximum 3-4 namespace levels

### Message & ID Patterns
- ❌ **Random GUIDs**: Use DeterministicIdGenerator instead of `new Guid()`
- ❌ **Raw C# events**: Use IMessageBusService instead
- ❌ **Ambiguous message naming**: Always use system context prefixes
- ❌ **Type code range violations**: Follow strict system ranges
- ❌ **Field initializers in IMessage structs**: Causes "must include explicitly declared constructor" errors
- ❌ **Direct message construction**: Use static factory methods instead of `new MessageType { ... }`
- ❌ **Ambiguous DeterministicIdGenerator calls**: Always use explicit named parameters
- ❌ **Missing validation in Create methods**: Always validate input parameters
- ❌ **Inconsistent factory method signatures**: Follow standard parameter patterns

### Profiling Anti-Patterns
- ❌ **Missing profiling in critical paths**: Always profile Update(), FixedUpdate(), and performance-sensitive operations
- ❌ **Hardcoded profiler tags**: Use ProfilerTag.CreateMethodTag() or ProfilerTag.CreateSystemTag() for consistent naming
- ❌ **Bypassing IProfilerService**: Use dependency injection instead of direct ProfilerMarker instantiation

## Testing Strategy

### Test Organization
- Edit Mode Tests: Unit tests for POCO systems
- Play Mode Tests: Integration tests requiring Unity runtime
- Performance Tests: Profiler-based benchmarks

### Test Naming
```csharp
[Test]
public void MethodName_StateUnderTest_ExpectedBehavior()
```

## Unity Integration

### Supported Unity Versions
- Minimum: Unity 2021.3 LTS
- Recommended: Unity 2022.3 LTS or newer

### Package Dependencies

#### Core Package (com.ahbearstudios.core)
- Reflex (Dependency Injection)
- Unity Collections
- Unity Burst Compiler
- Unity Mathematics
- ZLinq (Zero-allocation LINQ)
- UniTask (Unity async/await)
- MemoryPack (via ISerializationService)

#### Unity Package (com.ahbearstudios.unity)
- com.ahbearstudios.core (references core package)
- Unity Input System
- Unity Jobs System
- All dependencies from Core package (inherited)

## Development Workflow

1. **Understand existing patterns**: Check neighboring files for conventions
2. **Follow functional organization**: Place code in appropriate functional folders
3. **Choose correct package**: 
   - POCO/platform-agnostic code → `com.ahbearstudios.core`
   - Unity-specific/MonoBehaviour code → `com.ahbearstudios.unity`
4. **Implement Builder → Config → Factory → Service**: Use established pattern
5. **Test with Unity Profiler**: Validate performance impact
6. **Verify frame budget**: Ensure operations fit within 16.67ms

## Quick Reference

### Namespace Examples
```csharp
// Core POCO Systems (com.ahbearstudios.core package)
AhBearStudios.Core.Logging
AhBearStudios.Core.Messaging
AhBearStudios.Core.Pooling
AhBearStudios.Core.Serialization
AhBearStudios.Core.HealthCheck
AhBearStudios.Core.Profiling
AhBearStudios.Core.Alerting

// Common Utilities (com.ahbearstudios.core package)
AhBearStudios.Core.Common.Models
AhBearStudios.Core.Common.Extensions
AhBearStudios.Core.Common.Utilities

// Unity Implementations (com.ahbearstudios.unity package)
AhBearStudios.Unity.Logging
AhBearStudios.Unity.Messaging
AhBearStudios.Unity.Serialization
AhBearStudios.Unity.HealthChecking
AhBearStudios.Unity.Alerting
AhBearStudios.Unity.Common

// Unity Game Systems (com.ahbearstudios.unity package)
AhBearStudios.Unity.Player
AhBearStudios.Unity.Combat
AhBearStudios.Unity.UI
```

### File Naming
- Interfaces: `I{Name}.cs`
- Implementations: `{Name}.cs`
- Configs: `{Name}Config.cs`
- Builders: `{Name}Builder.cs`
- Factories: `{Name}Factory.cs`

## Performance Monitoring

Always use IProfilerService for profiling operations:
```csharp
// ✅ CORRECT: Use injected IProfilerService
private readonly IProfilerService _profilerService;

public void ProcessData()
{
    using (_profilerService.BeginScope("SystemName.MethodName"))
    {
        // Performance-critical code
        
        // Track custom metrics
        _profilerService.RecordMetric("items.processed", count);
        
        // Record performance samples
        _profilerService.RecordSample(ProfilerTag.CreateMethodTag("SystemName", "ProcessData"), processingTime);
    }
}
```

**Key Benefits:**
- Wraps Unity's ProfilerMarker internally for Unity Profiler integration
- Provides additional capabilities: custom metrics, thresholds, production monitoring
- Supports runtime enable/disable and sampling rate control
- Integrates with health checking and alerting systems

## Remember

**Game Development First** - Every decision should prioritize:
1. Player experience and smooth gameplay
2. Frame rate and performance
3. Memory efficiency
4. Platform compatibility
5. Designer/artist workflow

When in doubt, choose the solution that keeps the game running at 60 FPS over the one that follows enterprise patterns perfectly.