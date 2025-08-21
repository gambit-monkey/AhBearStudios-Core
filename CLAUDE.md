# AhBearStudios Core - Unity Game Development Architecture

This document provides Claude Code with essential information about the AhBearStudios Core project structure, development patterns, and Unity-specific requirements.

## Project Overview

AhBearStudios Core is a **modular, high-performance Unity game development framework** that prioritizes practical game solutions over enterprise patterns. The architecture focuses on:

- **Game Development First**: 60+ FPS performance targets, frame budget constraints (16.67ms), memory-conscious design
- **Unity-Native Integration**: Built for Unity's component system, Inspector workflow, and platform diversity
- **Modular Architecture**: Swappable interfaces, dependency injection via Reflex, compositional design
- **Performance Critical**: Unity Jobs & Burst, Unity Collections v2, zero-allocation patterns where possible

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

### Correct Builder → Factory → Service Pattern Example

```csharp
// ✅ CORRECT: Builder handles complexity
public class AlertConfigBuilder : IAlertConfigBuilder
{
    public IAlertConfigBuilder AddSeverityFilter(AlertSeverity severity, bool allowCritical = true)
    {
        // Builder handles validation, defaults, complex logic
        // Returns configuration objects
    }
    
    public AlertServiceConfiguration Build()
    {
        // Builder creates validated configuration
        return new AlertServiceConfiguration { /* validated config */ };
    }
}

// ✅ CORRECT: Factory is simple creation only
public class AlertServiceFactory : IAlertServiceFactory  // NO IDisposable!
{
    public async UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration config)
    {
        // Simple creation using validated config
        return new AlertService(config);
        // Factory does NOT track or manage created objects
    }
}

// ✅ CORRECT: Service manages its own lifecycle if needed
public class AlertService : IAlertService, IDisposable
{
    public void Dispose()
    {
        // Service handles its own cleanup
    }
}

// ✅ CORRECT: Usage pattern
var config = new AlertConfigBuilder(_poolingService)
    .AddSeverityFilter(AlertSeverity.Warning)
    .AddRateLimitFilter("RateLimit", 100)
    .Build();

var alertService = await _alertServiceFactory.CreateAlertServiceAsync(config);
// Factory doesn't track alertService - caller manages lifecycle
```

## Code Organization

### Functional Domain Organization (NOT Layered)

Code is organized by **functional domain**, not architectural layers. Related functionality stays together.

### Core POCO System Structure
```
AhBearStudios.Core.{FunctionalSystem}/
├── I{System}Service.cs              // Primary interface at root
├── {System}Service.cs               // Primary implementation at root
├── Configs/                         // Configuration objects
├── Builders/                        // Builder pattern implementations
├── Factories/                       // Factory pattern implementations
├── Services/                        // Additional service implementations
├── Models/                          // Data structures, enums, structs
└── HealthChecks/                    // Health monitoring
```

### Unity Application Layer Structure
```
AhBearStudios.Unity.{GameSystem}/
├── I{System}Manager.cs              // Primary interface at root
├── {System}Manager.cs               // MonoBehaviour implementation at root
├── Configs/                         // ScriptableObject configurations
├── Builders/                        // Unity object builders
├── Components/                      // MonoBehaviour components
├── Systems/                         // Game logic (avoid MonoBehaviour when possible)
└── Monitoring/                      // Performance profiling
```

## Key Development Commands

### Building the Project
```bash
# Build all core assemblies
dotnet build AhBearStudios.Core.csproj

# Build specific system
dotnet build AhBearStudios.Core.Logging.csproj
```

### Running Tests
```bash
# Run edit mode tests
dotnet test AhBearStudios.Core.Logging.EditMode.Tests.csproj

# Run play mode tests
dotnet test AhBearStudios.Core.Logging.PlayMode.Tests.csproj

# Unity test runner (from Unity Editor)
# Window > General > Test Runner
```

### Unity-Specific Commands
```bash
# Open Unity project
unity -projectPath .

# Build for specific platform
unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64
```

## Core Systems

### Dependency Injection (Reflex)
- Primary DI container for all systems
- Registration: `Container.Bind<TInterface>().To<TImplementation>()`
- Resolution: Constructor injection preferred
- Game-optimized: Balance DI benefits with performance

### Logging System
- Interface: `ILoggingService`
- Structured levels: Debug, Info, Warning, Error, Critical
- Performance markers for profiling
- Unity Console integration

### Message Bus
- Interface: `IMessageBusService`
- Event-driven communication between systems
- Performance-conscious pub/sub

### Object Pooling
- Interface: `IPoolingService`
- Zero-allocation object reuse
- Critical for mobile performance

### Alerting System
- Interface: `IAlertService`
- Runtime error handling
- Severity-based notifications

### Serialization System
- Interface: `ISerializationService`
- **ALWAYS use ISerializationService instead of direct MemoryPack calls**
- Unified serialization/deserialization for all data
- Performance-optimized with MemoryPack backend
- Support for save games, network packets, configurations

### Health Checking System
- Interface: `IHealthCheckService`
- Runtime system health monitoring
- Performance metrics collection
- System status reporting
- Critical for production monitoring

### Profiling System
- Interface: `IProfilerService`
- Performance profiling and metrics
- Frame time analysis
- Memory allocation tracking
- Integration with Unity Profiler

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
- **ALWAYS use ZLinq instead of System.Linq**
- Zero-allocation LINQ operations
- Critical for maintaining 60 FPS
- **IMPORTANT**: Use `.AsValueEnumerable()` to enable ZLinq operations
- **DO NOT USE**: `AsZLinq()`, `ZToList()`, `ZAny()`, `ZAll()`, `ZCount()` (these do not exist in ZLinq)
- Example:
```csharp
// ❌ AVOID: Standard LINQ (allocates)
using System.Linq;
var filtered = items.Where(x => x.IsActive).ToList();

// ❌ AVOID: Non-existent ZLinq methods
items.AsZLinq().Where(x => x.IsActive).ZToList();

// ✅ CORRECT: ZLinq (zero-allocation)
using ZLinq;
var filtered = items.AsValueEnumerable().Where(x => x.IsActive).ToList();

// ✅ PERFORMANCE: Use specialized ZLinq methods
using var pooledArray = items.AsValueEnumerable().ToArrayPool();
var sum = numbers.AsValueEnumerable().SumUnchecked(); // Skip overflow checking
var joined = items.AsValueEnumerable().JoinToString(",");
```

### UniTask Instead of Task
- **ALWAYS use UniTask instead of System.Threading.Tasks**
- Unity-optimized async/await
- Zero-allocation async operations
- Runs on Unity main thread
- Example:
```csharp
// ❌ AVOID: System.Threading.Tasks
async Task LoadDataAsync() { }

// ✅ CORRECT: UniTask
async UniTask LoadDataAsync() { }
```

## Unity 6+ .NET Compatibility

### .NET Profile and Language Support
- Project targets **Unity 6+** with **.NET Standard 2.1**
- **C# 10 language features enabled** via `csc.rsp` configuration (`-langversion:10`)
- Most modern .NET classes and features are supported
- Full access to C# 10 syntax and language features

### Available C# 10 Features
You can use modern C# 10 features including:
```csharp
// ✅ File-scoped namespaces
namespace AhBearStudios.Core.Logging;

// ✅ Global using statements
global using System;
global using Unity.Collections;

// ✅ Record types and structs
public record PlayerData(int Id, string Name);
public record struct GameEvent(string Type, DateTime Timestamp);

// ✅ Pattern matching enhancements
var result = value switch
{
    > 100 => "High",
    > 50 => "Medium",
    _ => "Low"
};
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

#### Use Unity Collections for Performance
```csharp
// ✅ PREFERRED: Unity Collections for performance-critical code
using Unity.Collections;
var nativeArray = new NativeArray<int>(1000, Allocator.Temp);

// ⚠️ ACCEPTABLE: Standard collections for non-performance-critical code
var list = new List<int>();
```

#### Use Project Custom Classes When Available
```csharp
// ✅ PREFERRED: Use existing project implementations
using AhBearStudios.Core.Common.Models;
var timeRange = TimeRange.BusinessHours();

// ⚠️ AVOID: Don't reinvent existing project patterns
var systemRange = System.Range.All; // Use project's TimeRange instead
```

#### Follow Unity Threading Model
```csharp
// ✅ PREFERRED: Unity Jobs for parallel work
using Unity.Jobs;

// ❌ AVOID: Standard .NET threading (can cause issues)
using System.Threading.Tasks;
Task.Run(() => { /* work */ }); // Don't use on main thread
```

### Unity-Specific API Preferences

#### Serialization
- **ALWAYS use ISerializationService** instead of standard .NET serialization
- Leverages MemoryPack backend optimized for Unity

#### Collections
- Use **Unity.Collections** for performance-critical scenarios
- Use **ZLinq** for zero-allocation LINQ operations
- Standard .NET collections acceptable for non-performance-critical code

#### Async Operations
- **ALWAYS use UniTask** instead of System.Threading.Tasks.Task
- Integrates with Unity's frame-based execution model

### Development Guidelines

#### Testing Requirements
- Test on all target deployment platforms
- Verify IL2CPP build compatibility for production builds
- Profile performance impact of .NET APIs on mobile targets

#### API Usage Validation
- Prefer Unity-native APIs when available
- Check Unity documentation for platform-specific API limitations
- Use Unity's package dependencies listed in project configuration

#### Modern C# Usage
With C# 10 enabled, you can use modern language features while following Unity best practices:
```csharp
// ✅ Modern C# with Unity patterns
namespace AhBearStudios.Core.Player;

using Unity.Profiling;
using UniTask = Cysharp.Threading.Tasks.UniTask;

public class PlayerService : IPlayerService
{
    private readonly ProfilerMarker _updateMarker = new("PlayerService.Update");
    
    public async UniTask<PlayerData> LoadPlayerAsync(int playerId)
    {
        using (_updateMarker.Auto())
        {
            // Modern C# with Unity-optimized patterns
            return await LoadPlayerDataAsync(playerId);
        }
    }
}
```

### Remember: Unity 6+ First
- Leverage modern C# 10 features for cleaner, more maintainable code
- Always test cross-platform compatibility
- Prefer Unity-specific implementations over generic .NET when available
- Follow project conventions even when standard .NET alternatives exist

## Message Bus Usage

### IMessage Pattern
- **ALWAYS use IMessageBusService for events**
- **NEVER create raw events - use IMessage instead**
- All messages must implement `IMessage` interface
- Example:
```csharp
// ❌ AVOID: Raw C# events
public event Action<PlayerData> OnPlayerSpawned;

// ✅ CORRECT: IMessage with MessageBus
public record struct PlayerSpawnedMessage : IMessage
{
    public int PlayerId { get; init; }
    public Vector3 Position { get; init; }
}

// Publishing
_messageBus.PublishMessage(new PlayerSpawnedMessage 
{ 
    PlayerId = 1, 
    Position = Vector3.zero 
});

// Subscribing
_messageBus.Subscribe<PlayerSpawnedMessage>(OnPlayerSpawned);
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

#### IMessage Interface Compliance
All messages **MUST implement IMessage interface completely**:

```csharp
// ✅ CORRECT: Full IMessage implementation
public record struct AlertRaisedMessage : IMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long TimestampTicks { get; init; } = DateTime.UtcNow.Ticks;
    public ushort TypeCode { get; init; } = MessageTypeCodes.AlertRaisedMessage;
    public FixedString64Bytes Source { get; init; } = "AlertSystem";
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public Guid CorrelationId { get; init; }
    
    // Message-specific properties
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; }
}
```

#### Type Code Assignment
```csharp
// ✅ CORRECT: Use system-specific type code
TypeCode = MessageTypeCodes.MessageBusCircuitBreakerStateChangedMessage

// ❌ AVOID: Hardcoded values or wrong system codes
TypeCode = 1062 // Hard-coded
TypeCode = MessageTypeCodes.PoolCircuitBreakerStateChangedMessage // Wrong system
```

### Anti-Patterns to Avoid

#### ❌ **Ambiguous Naming**
```csharp
// Bad: Which system does this belong to?
public const ushort StateChanged = 1234;
public const ushort ProcessingFailed = 5678;
```

#### ❌ **Duplicate Type Codes**
```csharp
// Bad: Same concept, different systems, same code
CircuitBreakerStateChanged = 1304      // Pool system
CircuitBreakerStateChangedEvent = 1019 // Messaging system  
```

#### ❌ **Range Violations**
```csharp
// Bad: Using wrong range for system
public const ushort LoggingMessage = 1350; // Should be 1100-1199
```

#### ❌ **Inconsistent Suffixes**
```csharp
// Bad: Mixed naming conventions
MessagePublished = 1055      // No suffix
MessageProcessedEvent = 1063 // Event suffix  
MessageRoutedMessage = 1066  // Message suffix
```

### Benefits of Context-Prefixed Naming

1. **Immediate Clarity** - No guessing about message origin
2. **IDE IntelliSense** - Easy to find all messages from a system  
3. **Debugging** - Logs clearly show which system sent the message
4. **Prevents Duplicates** - Can't accidentally create same-named messages
5. **Scalability** - Easy to add new systems without naming conflicts

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
    
    // Performance monitoring
    private readonly ProfilerMarker _updateMarker = new ProfilerMarker("PlayerManager.Update");
    
    [Inject]
    public void Initialize(ILoggingService loggingService, IMessageBusService messageBus)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }
    
    private void Update()
    {
        using (_updateMarker.Auto())
        {
            // Game logic here
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

### From Enterprise Development
- ❌ **Over-abstraction**: No 5-layer abstractions
- ❌ **Heavy DI**: Don't inject everything
- ❌ **Excessive interfaces**: Avoid micro-interfaces
- ❌ **Enterprise logging**: Keep logging lightweight
- ❌ **Complex configuration**: Use simple, designer-friendly configs
- ❌ **Database patterns**: Use Unity's save system, not ORMs

### Factory and Builder Anti-Patterns
- ❌ **Factory lifecycle management**: Factories should NEVER implement IDisposable
- ❌ **Complex factories**: Move complexity to builders, keep factories simple
- ❌ **Factory object tracking**: Factories don't track created objects
- ❌ **Builder creation**: Builders create configs, not final objects
- ❌ **Mixed responsibilities**: Clear separation between creation and lifecycle

### Organizational Anti-Patterns
- ❌ **Layered organization**: Don't organize by technical layers
- ❌ **Separate interface namespaces**: Keep interfaces with implementations
- ❌ **Deep nesting**: Maximum 3-4 namespace levels
- ❌ **Type-based grouping**: Group by function, not type

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
- Reflex (Dependency Injection)
- Unity Collections
- Unity Burst Compiler
- Unity Jobs System
- ZLinq (Zero-allocation LINQ)
- UniTask (Unity async/await)
- MemoryPack (via ISerializationService)

## Development Workflow

1. **Understand existing patterns**: Check neighboring files for conventions
2. **Follow functional organization**: Place code in appropriate functional folders
3. **Implement Builder → Config → Factory → Service**: Use established pattern
4. **Test with Unity Profiler**: Validate performance impact
5. **Verify frame budget**: Ensure operations fit within 16.67ms

## Quick Reference

### Namespace Examples
```csharp
// Core POCO Systems
AhBearStudios.Core.Logging
AhBearStudios.Core.Messaging
AhBearStudios.Core.Pooling
AhBearStudios.Core.Serialization
AhBearStudios.Core.HealthCheck
AhBearStudios.Core.Profiling

// Common Utilities
AhBearStudios.Core.Common.Models
AhBearStudios.Core.Common.Extensions
AhBearStudios.Core.Common.Utilities

// Unity Game Systems
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

Always use ProfilerMarkers for critical paths:
```csharp
private readonly ProfilerMarker _marker = new ProfilerMarker("SystemName.MethodName");

using (_marker.Auto())
{
    // Performance-critical code
}
```

## Remember

**Game Development First** - Every decision should prioritize:
1. Player experience and smooth gameplay
2. Frame rate and performance
3. Memory efficiency
4. Platform compatibility
5. Designer/artist workflow

When in doubt, choose the solution that keeps the game running at 60 FPS over the one that follows enterprise patterns perfectly.