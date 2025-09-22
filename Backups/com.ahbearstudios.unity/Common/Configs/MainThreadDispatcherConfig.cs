using UnityEngine;

namespace AhBearStudios.Unity.Common.Configs;

/// <summary>
/// Configuration for MainThreadDispatcher with frame budget and performance settings.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public sealed class MainThreadDispatcherConfig
{
    /// <summary>
    /// Maximum time in milliseconds to spend processing actions per frame.
    /// Default: 8.33ms (half of 16.67ms frame budget for 60 FPS).
    /// </summary>
    public float FrameBudgetMs { get; init; } = 8.33f;
    
    /// <summary>
    /// Maximum number of actions to process per frame, regardless of time budget.
    /// Prevents infinite loops and provides predictable behavior.
    /// Default: 50 actions per frame.
    /// </summary>
    public int MaxActionsPerFrame { get; init; } = 50;
    
    /// <summary>
    /// Maximum queue capacity before backpressure kicks in.
    /// When exceeded, oldest actions are dropped to prevent memory leaks.
    /// Default: 10000 actions.
    /// </summary>
    public int MaxQueueCapacity { get; init; } = 10000;
    
    /// <summary>
    /// Initial queue capacity to pre-allocate and avoid allocations during runtime.
    /// Should be set based on expected normal queue usage.
    /// Default: 256 actions.
    /// </summary>
    public int InitialQueueCapacity { get; init; } = 256;
    
    /// <summary>
    /// Whether to enable performance monitoring and profiling.
    /// Adds minimal overhead but provides valuable production metrics.
    /// Default: true.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = true;
    
    /// <summary>
    /// Whether to enable health checking integration.
    /// Reports dispatcher health to the health checking system.
    /// Default: true.
    /// </summary>
    public bool EnableHealthChecking { get; init; } = true;
    
    /// <summary>
    /// Whether to enable message bus integration for dispatcher events.
    /// Publishes events for queue overflow, performance issues, etc.
    /// Default: true.
    /// </summary>
    public bool EnableMessageBusIntegration { get; init; } = true;
    
    /// <summary>
    /// Backpressure strategy when queue reaches capacity.
    /// </summary>
    public BackpressureStrategy BackpressureStrategy { get; init; } = BackpressureStrategy.DropOldest;
    
    /// <summary>
    /// Warning threshold for queue depth as percentage of max capacity.
    /// When exceeded, warning messages/events are generated.
    /// Default: 0.8 (80% of max capacity).
    /// </summary>
    public float QueueWarningThreshold { get; init; } = 0.8f;
    
    /// <summary>
    /// GameObject name for the dispatcher MonoBehaviour.
    /// Used for debugging and Unity Inspector identification.
    /// Default: "MainThreadDispatcher".
    /// </summary>
    public string GameObjectName { get; init; } = "MainThreadDispatcher";
    
    /// <summary>
    /// Whether the dispatcher GameObject should persist across scene loads.
    /// Default: true for singleton behavior.
    /// </summary>
    public bool DontDestroyOnLoad { get; init; } = true;
    
    /// <summary>
    /// Validates the configuration values are within acceptable ranges.
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        return FrameBudgetMs > 0 && FrameBudgetMs <= 16.67f &&
               MaxActionsPerFrame > 0 &&
               MaxQueueCapacity > 0 &&
               InitialQueueCapacity > 0 &&
               InitialQueueCapacity <= MaxQueueCapacity &&
               QueueWarningThreshold > 0 && QueueWarningThreshold <= 1.0f &&
               !string.IsNullOrWhiteSpace(GameObjectName);
    }
    
    /// <summary>
    /// Creates a default configuration optimized for 60 FPS gameplay.
    /// </summary>
    public static MainThreadDispatcherConfig Default => new();
    
    /// <summary>
    /// Creates a configuration optimized for high-performance scenarios.
    /// Reduces frame budget and queue capacity for maximum FPS.
    /// </summary>
    public static MainThreadDispatcherConfig HighPerformance => new()
    {
        FrameBudgetMs = 4.0f,
        MaxActionsPerFrame = 25,
        MaxQueueCapacity = 5000,
        InitialQueueCapacity = 128
    };
    
    /// <summary>
    /// Creates a configuration optimized for high-throughput scenarios.
    /// Increases frame budget and queue capacity for batch processing.
    /// </summary>
    public static MainThreadDispatcherConfig HighThroughput => new()
    {
        FrameBudgetMs = 12.0f,
        MaxActionsPerFrame = 100,
        MaxQueueCapacity = 25000,
        InitialQueueCapacity = 512
    };
}

/// <summary>
/// Strategy for handling queue overflow when capacity is reached.
/// </summary>
public enum BackpressureStrategy
{
    /// <summary>
    /// Drop the oldest actions in the queue to make room for new ones.
    /// Ensures recent actions are processed while preventing memory growth.
    /// </summary>
    DropOldest,
    
    /// <summary>
    /// Drop the newest action when queue is full.
    /// Preserves existing work queue but may lose recent requests.
    /// </summary>
    DropNewest,
    
    /// <summary>
    /// Throw exception when trying to enqueue to full queue.
    /// Forces caller to handle overflow condition explicitly.
    /// </summary>
    ThrowException
}