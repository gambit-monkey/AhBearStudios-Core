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
1. **Builder** creates configurations
2. **Config** holds runtime/design-time settings
3. **Factory** creates instances using configs
4. **Service** provides the actual functionality

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
- Example:
```csharp
// ❌ AVOID: Standard LINQ (allocates)
using System.Linq;
var filtered = items.Where(x => x.IsActive).ToList();

// ✅ CORRECT: ZLinq (zero-allocation)
using ZLinq;
var filtered = items.AsZLinq().Where(x => x.IsActive).ToList();
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
- **No incomplete code**: No TODOs, stubs, or placeholders
- **One type per file**: Each class/interface in its own file
- **Full implementation**: All methods must be implemented
- **No comments**: Code should be self-documenting

## Anti-Patterns to Avoid

### From Enterprise Development
- ❌ **Over-abstraction**: No 5-layer abstractions
- ❌ **Heavy DI**: Don't inject everything
- ❌ **Excessive interfaces**: Avoid micro-interfaces
- ❌ **Enterprise logging**: Keep logging lightweight
- ❌ **Complex configuration**: Use simple, designer-friendly configs
- ❌ **Database patterns**: Use Unity's save system, not ORMs

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