# Unity Runtime Profiling System

## Overview

This runtime profiling system combines Unity's `ProfilerRecorder` and `ProfilerMarker` APIs to create a powerful hybrid profiling framework that works both in the editor and in builds (including non-development builds). The system provides a centralized way to track both Unity's internal performance metrics and custom code execution times with minimal overhead.

## Key Features

- Profile Unity internals (GC, FrameTime, etc.) using `ProfilerRecorder`
- Profile custom code sections (both managed and Burst/job code) using `ProfilerMarker`
- Unified runtime interface for profiling sessions
- Tag and categorize profiled sections
- Optional runtime UI dashboard for visualization
- Dynamic registration of custom metrics
- Compatible with Unity Jobs and Burst-compiled code
- Support for threshold alerts and logging

## Core Components

### ProfilerTag
A lightweight struct that combines a category and name to uniquely identify profiled sections.

### ProfilerSession
Wrapper for Unity's `ProfilerMarker` that provides scoped timing via `IDisposable`.

### SystemMetricsTracker
Class that manages a collection of `ProfilerRecorder` instances to track Unity's internal metrics.

### RuntimeProfilerManager
Central manager that controls the profiling system and provides APIs for starting/stopping profiling sessions.

### ThresholdAlertSystem
System for monitoring metrics and triggering alerts when thresholds are exceeded.

### ProfilerOverlayUI
Optional runtime UI dashboard that visualizes the profiler data.

### Burst/Jobs Support
Examples of how to use `ProfilerMarker` in Burst-compiled jobs.

## Getting Started

1. Add the scripts to your Unity project
2. Create a ProfilerBootstrap in your scene (Tools > Runtime Profiler > Create Profiler)
3. Configure the profiler settings in the inspector
4. Use the runtime profiler APIs in your code

## Usage Examples

### Profile a code block with scoped timing

```csharp
using (RuntimeProfilerManager.Instance.BeginScope(ProfilerCategory.AI, "Update"))
{
    UpdateEnemies();
}
```

### Track Unity's internal metrics

```csharp
// Start with default metrics (GC, FrameTime, etc.)
SystemMetricsTracker.StartDefault();

// Register a custom Unity metric
SystemMetricsTracker.RegisterCustomMetric("Draw Calls", ProfilerCategory.Rendering, "Batches Count", "count");
```

### Profile code in a Burst-compiled job

```csharp
[BurstCompile]
public struct PhysicsJob : IJob
{
    static readonly ProfilerMarker marker = new ProfilerMarker("PhysicsJob");
    
    public void Execute()
    {
        marker.Begin();
        // Perform job work
        marker.End();
    }
}
```

### Set up threshold alerts

```csharp
// Alert if frame time exceeds 33.3ms (30 FPS)
RuntimeProfilerManager.Instance.RegisterMetricAlert(
    new ProfilerTag(ProfilerCategory.Rendering, "Frame Time"), 
    33.3f,
    OnFrameTimeAlert);

void OnFrameTimeAlert(MetricEventArgs args)
{
    Debug.LogWarning($"Frame time alert: {args.Value:F2} ms");
}
```

## Best Practices

1. Use `ProfilerTags` to categorize and organize your profiling data
2. Avoid profiling every frame for high-frequency operations
3. Use `ProfilerMarker` directly for performance-critical code
4. Set appropriate thresholds for alerts to avoid spam
5. Dispose `SystemMetricsTracker` when no longer needed
6. Be careful with nested profiling to avoid overhead
7. Use the UI overlay only when needed to minimize impact

## Performance Considerations

- The profiling system is designed to be lightweight, but still adds some overhead
- Use conditional profiling based on build type or configuration flags
- For production builds, consider disabling UI but keeping metrics tracking
- Be cautious with threshold alerts in production as they can cause GC allocations in callbacks

## Extending the System

The system is designed to be extensible. You can add custom:

- ProfilerCategories
- Metrics
- Thresholds
- UI visualizations
- Integration with logging systems

## Compatibility

- Unity 2022+ (some features may work with earlier versions)
- Works with Burst compiled code
- Works with the Jobs system
- Compatible with both editor and runtime
- Works in development and non-development builds