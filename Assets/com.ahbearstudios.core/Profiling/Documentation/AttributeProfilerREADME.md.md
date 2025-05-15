# Unity Runtime Profiling System - Attribute Extension

## Overview

This attribute-based extension to the runtime profiling system allows you to easily profile methods and classes using simple attributes, reducing the amount of manual profiling code needed in your project. The system uses reflection to discover and profile marked methods.

## Attribute Types

The system provides three main attribute types:

### ProfileMethodAttribute

Apply this attribute to individual methods to profile them:

```csharp
[ProfileMethod(ProfilerCategory.Gameplay, "CustomName")]
private void SomeMethod() 
{
    // Method code here
}
```

### ProfileClassAttribute

Apply this attribute to a class to profile all its methods:

```csharp
[ProfileClass(ProfilerCategory.AI, "AISystem", includeInherited: true, includePrivate: true)]
public class EnemyAI : MonoBehaviour
{
    // All methods will be profiled
}
```

### DoNotProfileAttribute

Use this attribute to exclude specific methods from a class-wide profiling:

```csharp
[ProfileClass(ProfilerCategory.AI)]
public class AIController
{
    // This method will be profiled
    public void Update() { }
    
    // This method will NOT be profiled
    [DoNotProfile]
    private void InternalCalculation() { }
}
```

## Components

### AttributeProfilerWeaver

Core component that handles the low-level profiling of methods. It creates and manages ProfilerMarker instances to minimize overhead.

### AttributeProfilerBehaviour

MonoBehaviour that scans and collects profiled methods at runtime, and can be used to profile specific instances.

### ProfileProxyFactory

Factory that creates specialized proxy objects for different method signatures to optimize profiling performance.

### ProfileInvoker

Helper class for manually invoking methods with profiling applied, useful when you need to profile methods called via reflection.

## Usage Examples

### Profiling a Single Method

```csharp
// Profile just this specific method
[ProfileMethod(ProfilerCategory.Gameplay, "PlayerUpdate")]
private void UpdatePlayerState()
{
    // Method body
}
```

### Profiling an Entire Class

```csharp
[ProfileClass(ProfilerCategory.Gameplay, "AI", includeInherited: true)]
public class EnemyAI : MonoBehaviour
{
    // All methods will be profiled automatically
    
    private void Update() { } // Profiled as "AI.Update"
    private void PathFind() { } // Profiled as "AI.PathFind"
    
    [DoNotProfile]
    private void InternalCalculation() { } // Not profiled
}
```

### Manual Method Invocation with Profiling

```csharp
// Profile a method call via reflection
var method = GetType().GetMethod("ProcessData", 
    BindingFlags.NonPublic | BindingFlags.Instance);
    
// Invoke with profiling
ProfileInvoker.InvokeMethod<int>(this, method, 100);

// Or use the method name directly (slower but more convenient)
ProfileInvoker.InvokeFunction<int, string>(this, "CalculateValue", 42);
```

## Setup

1. Add the AttributeProfilerBehaviour component to a GameObject in your scene
2. Configure the assembly filtering to include your game assemblies
3. Add ProfileMethod or ProfileClass attributes to your code
4. Run your game and watch the profiling data appear in the RuntimeProfilerManager

## Performance Considerations

- The attribute-based system adds some overhead due to reflection
- For high-frequency methods (called every frame), consider using direct ProfilerMarker instances instead
- Method proxies are more efficient than reflection-based invocation
- The system caches reflection data to minimize lookup costs
- Static methods are currently not supported for proxying (but work with manual invocation)

## Integration with Base Profiling System

The attribute system integrates seamlessly with the core profiling framework:

- All profiled methods use the same underlying ProfilerMarker system
- Profiling data is accessible through the RuntimeProfilerManager
- Threshold alerts work with attribute-profiled methods
- The runtime UI displays data from both direct and attribute-based profiling

## Additional Features

- Assembly filtering to limit which assemblies are scanned
- Support for different method signatures (void, with return values, with parameters)
- Customizable naming to organize profiling data
- Runtime discovery and proxy generation
- Support for profiling methods with return values