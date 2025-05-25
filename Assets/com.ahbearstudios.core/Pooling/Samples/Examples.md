# Unity Object Pooling System Examples

This document provides a comprehensive guide to the example scenes and demos included with the Unity Object Pooling System. These examples demonstrate various pooling strategies, performance optimization techniques, and integration with Unity's job system and Burst compiler.

## Table of Contents

- [Basic Pooling Demo](#basic-pooling-demo)
- [Advanced Pooling Demo](#advanced-pooling-demo)
- [Burst Job Demo](#burst-job-demo)
- [Example Scene Manager](#example-scene-manager)
- [Complex Pooling Demo](#complex-pooling-demo)

## Basic Pooling Demo

A demonstration of GameObject pooling with Unity's Jobs and Burst compiler integration.

### Overview

`BasicPoolingDemo` showcases a simple but effective object pooling implementation, demonstrating how to:

- Create and manage pools of GameObjects
- Use Unity's Jobs system with pooled objects
- Apply Burst compilation for high-performance processing
- Track and manage pooled objects efficiently

### Features

- **GameObject Pooling**: Efficiently reuses bullet and explosion prefab instances
- **Performance Modes**: Switch between standard and Burst-accelerated processing
- **Configuration Options**: Adjustable spawn settings and performance parameters
- **Statistics**: Runtime monitoring of pool usage and performance

### How It Works

The demo creates two GameObject pools on startup for bullets and explosions with configurations for initial capacity, maximum size, and auto-shrinking capabilities. It demonstrates both standard GameObject management with coroutines and Burst-optimized processing using Unity's Job System.

### Usage

1. Add the script to a GameObject in your scene
2. Assign bullet and explosion prefabs 
3. Configure spawn settings and performance parameters
4. Press Play to see the demo in action
5. Press `Space` to toggle spawning
6. Press `I` to view pool statistics

### Performance Considerations

- Batch size affects job scheduling efficiency
- Prewarming creates objects in advance to prevent runtime hitches
- The demo handles cleanup properly to prevent memory leaks

## Advanced Pooling Demo

A comprehensive demonstration of pooling systems with Unity's job system integration, showcasing three distinct pool types.

### Overview

`AdvancedPoolingDemo` provides a detailed showcase of different pooling approaches:

- **Managed Pools**: Standard C# reference type pooling
- **Thread-Safe Pools**: Synchronized pools for multi-threaded access
- **Native Pools**: High-performance native memory pools for use with Jobs and Burst

### Key Features

- **Multiple Pool Types**: Compare different pooling strategies
- **Performance Testing**: Run various tests to compare pool performance
- **Multi-threading Support**: Test thread-safe operations
- **Burst Integration**: Native pools working with Unity's Burst compiler

### Test Scenarios

The demo includes single-threaded tests, multi-threaded tests with Tasks, and Native pools with Unity's job system for performance comparisons.

### Usage

1. Attach the script to a GameObject in your scene
2. Configure the test parameters in the Inspector
3. Run the scene and call `RunAllTests()` to execute all test scenarios
4. Monitor the console for performance results

### Best Practices Demonstrated

- Proper pool initialization with prewarming
- Efficient object recycling patterns
- Proper disposal of native resources
- Thread-safe pooling operations
- Job system integration for high-performance processing

## Burst Job Demo

A performance comparison tool demonstrating the benefits of Unity's Job System and Burst compiler with object pooling integration.

### Overview

`BurstJobDemo` provides a visualization and benchmarking suite for comparing three different processing approaches:

1. **Standard Processing**: Single-threaded main thread execution
2. **Job System**: Multi-threaded execution using Unity's Job System
3. **Burst-Compiled Jobs**: Maximum performance with Burst compiler optimizations

### Features

- **Interactive UI**: Adjustable data size and visualization controls
- **Performance Metrics**: Precise timing of different processing methods
- **Visual Results**: Optional 3D visualization of processed data
- **Native Pooling Integration**: Optional use of `NativePool<T>` for optimized memory management

### Processing Methods

The demo implements standard main thread processing, Job System processing for multi-threading, and Burst-compiled job processing for maximum performance, allowing direct comparisons between approaches.

### Visualization

The demo can visualize processing results in 3D space, creating point objects to represent the processed data for visual comparison between methods.

### Usage

1. Add the script to a GameObject in your scene
2. Configure UI elements in the inspector
3. Assign a point prefab for visualization
4. Run the scene and use the UI buttons to execute different processing methods
5. Compare the performance metrics in the results text

### Performance Expectations

- **Standard Processing**: Baseline performance
- **Job System**: 2-8x faster than standard processing (depending on CPU core count)
- **Burst Jobs**: 2-20x faster than standard processing (depending on vectorization opportunities)

## Example Scene Manager

A comprehensive demo manager for showcasing various pooling strategies and features.

### Overview

`ExampleSceneManager` provides a complete demo environment for the pooling system, with UI controls, multiple pool types, and various spawning methods.

### Key Features

- **Multiple Pool Types**: Standard, Component and Managed pool implementations
- **UI Controls**: Interactive buttons and displays for demo operations
- **Batch Processing**: Demonstrates efficient batch spawning techniques
- **Job System Integration**: Uses Unity's job system for high-performance operations
- **Stress Testing**: Performance testing with high object counts

### Spawning Methods

The manager demonstrates single object spawning, batch spawning for efficiency, and job-based spawning using Unity's job system for generating many spawn positions simultaneously.

### Stress Testing

Includes a stress test mode that dynamically adjusts spawn rates based on performance, helping to identify the optimal limits of the pooling system on the current hardware.

### Usage

1. Add the script to a GameObject in your scene
2. Configure the prefabs, UI elements, and pool settings in the Inspector
3. Run the scene to interact with the demo UI
4. Use buttons to spawn objects, change pool types, and run stress tests
5. Monitor the stats display for pool performance information

### Best Practices Demonstrated

- Proper pool initialization and cleanup
- Efficient object spawning and recycling
- Performance monitoring and statistics tracking
- Integration with Unity's job system for improved performance
- UI feedback for pool operations and status

## Complex Pooling Demo

An advanced demonstration of complex entity management using the pooling system, with nested pools and hierarchical object relationships.

### Overview

`ComplexPoolingDemo` showcases a more sophisticated use case for object pooling, managing complex entities with sub-components, projectiles, and visual effects in a game-like environment.

### Key Features

- **Hierarchical Pooling**: Manages complex entities with child objects
- **Multiple Pool Types**: Uses various pool implementations for different needs
- **Complexity Levels**: Adjustable entity complexity for performance testing
- **Project & Effect Systems**: Includes projectile firing and visual effects
- **Dynamic Pool Usage**: Runtime adjustment of pool demands

### Complexity Levels

Entities have adjustable complexity levels from basic to ultra complex, affecting their behavior, performance requirements, and visual effects. This allows for testing pool performance under varying loads.

### Entity Group Processing

The demo efficiently processes groups of entities, applying movement, rotation, and optional projectile firing based on their complexity level and state.

### Pool Validation

Includes validation functionality to ensure pool integrity, identifying and handling any invalid entities that might cause issues in the pool system.

### Usage

1. Add the script to a GameObject in your scene
2. Configure the prefabs and UI elements in the Inspector
3. Run the scene to see the complex entity management in action
4. Use the UI to spawn entities, change complexity, and fire projectiles
5. Monitor the stats display for pool performance information

### Best Practices Demonstrated

- Management of complex object hierarchies with pooling
- Effective recycling of various object types
- Performance scaling with adjustable complexity
- Proper cleanup and resource management
- Integration with game systems like projectiles and effects

## Implementation Details

The examples in this package demonstrate various ways to use the pooling system with Unity's latest features. Here are some key implementation details that are showcased across different examples:

### Pool Configuration

The pooling system offers flexible configuration options to customize pool behavior for different use cases. Here's how you can configure your pools:
