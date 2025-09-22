using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Common.Configs;

namespace AhBearStudios.Unity.Common.Builders;

/// <summary>
/// Builder for MainThreadDispatcherConfig following CLAUDE.md Builder pattern.
/// Handles configuration complexity through fluent APIs.
/// </summary>
public interface IMainThreadDispatcherBuilder
{
    /// <summary>
    /// Sets the frame budget for processing actions per frame.
    /// </summary>
    /// <param name="budgetMs">Time budget in milliseconds (must be > 0 and ≤ 16.67ms)</param>
    IMainThreadDispatcherBuilder WithFrameBudget(float budgetMs);
    
    /// <summary>
    /// Sets the maximum number of actions to process per frame.
    /// </summary>
    /// <param name="maxActions">Maximum actions per frame (must be > 0)</param>
    IMainThreadDispatcherBuilder WithMaxActionsPerFrame(int maxActions);
    
    /// <summary>
    /// Sets the queue capacity limits.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity to pre-allocate</param>
    /// <param name="maxCapacity">Maximum capacity before backpressure</param>
    IMainThreadDispatcherBuilder WithQueueCapacity(int initialCapacity, int maxCapacity);
    
    /// <summary>
    /// Sets the backpressure strategy for queue overflow handling.
    /// </summary>
    /// <param name="strategy">Strategy to use when queue is full</param>
    IMainThreadDispatcherBuilder WithBackpressureStrategy(BackpressureStrategy strategy);
    
    /// <summary>
    /// Sets the warning threshold for queue depth monitoring.
    /// </summary>
    /// <param name="threshold">Threshold as percentage (0.0 to 1.0)</param>
    IMainThreadDispatcherBuilder WithQueueWarningThreshold(float threshold);
    
    /// <summary>
    /// Enables or disables performance monitoring.
    /// </summary>
    /// <param name="enabled">Whether to enable performance monitoring</param>
    IMainThreadDispatcherBuilder WithPerformanceMonitoring(bool enabled = true);
    
    /// <summary>
    /// Enables or disables health checking integration.
    /// </summary>
    /// <param name="enabled">Whether to enable health checking</param>
    IMainThreadDispatcherBuilder WithHealthChecking(bool enabled = true);
    
    /// <summary>
    /// Enables or disables message bus integration.
    /// </summary>
    /// <param name="enabled">Whether to enable message bus integration</param>
    IMainThreadDispatcherBuilder WithMessageBusIntegration(bool enabled = true);
    
    /// <summary>
    /// Sets Unity-specific configuration.
    /// </summary>
    /// <param name="gameObjectName">Name for the dispatcher GameObject</param>
    /// <param name="dontDestroyOnLoad">Whether to persist across scenes</param>
    IMainThreadDispatcherBuilder WithUnitySettings(string gameObjectName, bool dontDestroyOnLoad = true);
    
    /// <summary>
    /// Applies a high-performance preset configuration.
    /// Optimized for maximum FPS with reduced throughput.
    /// </summary>
    IMainThreadDispatcherBuilder UseHighPerformancePreset();
    
    /// <summary>
    /// Applies a high-throughput preset configuration.
    /// Optimized for batch processing with higher frame budget.
    /// </summary>
    IMainThreadDispatcherBuilder UseHighThroughputPreset();
    
    /// <summary>
    /// Builds and validates the configuration.
    /// </summary>
    /// <returns>Validated configuration ready for factory</returns>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid</exception>
    MainThreadDispatcherConfig Build();
}

/// <summary>
/// Implementation of MainThreadDispatcherBuilder.
/// Manages configuration complexity through fluent APIs.
/// </summary>
public sealed class MainThreadDispatcherBuilder : IMainThreadDispatcherBuilder
{
    private readonly ILoggingService _logger;
    private float _frameBudgetMs = 8.33f;
    private int _maxActionsPerFrame = 50;
    private int _initialQueueCapacity = 256;
    private int _maxQueueCapacity = 10000;
    private BackpressureStrategy _backpressureStrategy = BackpressureStrategy.DropOldest;
    private float _queueWarningThreshold = 0.8f;
    private bool _enablePerformanceMonitoring = true;
    private bool _enableHealthChecking = true;
    private bool _enableMessageBusIntegration = true;
    private string _gameObjectName = "MainThreadDispatcher";
    private bool _dontDestroyOnLoad = true;

    /// <summary>
    /// Initializes a new MainThreadDispatcherBuilder.
    /// </summary>
    /// <param name="logger">Optional logging service for validation warnings</param>
    public MainThreadDispatcherBuilder(ILoggingService logger = null)
    {
        _logger = logger;
    }

    public IMainThreadDispatcherBuilder WithFrameBudget(float budgetMs)
    {
        if (budgetMs <= 0 || budgetMs > 16.67f)
        {
            var message = $"Frame budget {budgetMs}ms is outside valid range (0, 16.67]. Using default 8.33ms.";
            _logger?.LogWarning(message);
            throw new ArgumentOutOfRangeException(nameof(budgetMs), message);
        }
        
        _frameBudgetMs = budgetMs;
        return this;
    }

    public IMainThreadDispatcherBuilder WithMaxActionsPerFrame(int maxActions)
    {
        if (maxActions <= 0)
        {
            var message = $"Max actions per frame {maxActions} must be positive.";
            _logger?.LogWarning(message);
            throw new ArgumentOutOfRangeException(nameof(maxActions), message);
        }
        
        _maxActionsPerFrame = maxActions;
        return this;
    }

    public IMainThreadDispatcherBuilder WithQueueCapacity(int initialCapacity, int maxCapacity)
    {
        if (initialCapacity <= 0 || maxCapacity <= 0 || initialCapacity > maxCapacity)
        {
            var message = $"Invalid queue capacities: initial={initialCapacity}, max={maxCapacity}. Initial must be > 0 and ≤ max.";
            _logger?.LogWarning(message);
            throw new ArgumentException(message);
        }
        
        _initialQueueCapacity = initialCapacity;
        _maxQueueCapacity = maxCapacity;
        return this;
    }

    public IMainThreadDispatcherBuilder WithBackpressureStrategy(BackpressureStrategy strategy)
    {
        _backpressureStrategy = strategy;
        return this;
    }

    public IMainThreadDispatcherBuilder WithQueueWarningThreshold(float threshold)
    {
        if (threshold <= 0 || threshold > 1.0f)
        {
            var message = $"Queue warning threshold {threshold} must be between 0 and 1.";
            _logger?.LogWarning(message);
            throw new ArgumentOutOfRangeException(nameof(threshold), message);
        }
        
        _queueWarningThreshold = threshold;
        return this;
    }

    public IMainThreadDispatcherBuilder WithPerformanceMonitoring(bool enabled = true)
    {
        _enablePerformanceMonitoring = enabled;
        return this;
    }

    public IMainThreadDispatcherBuilder WithHealthChecking(bool enabled = true)
    {
        _enableHealthChecking = enabled;
        return this;
    }

    public IMainThreadDispatcherBuilder WithMessageBusIntegration(bool enabled = true)
    {
        _enableMessageBusIntegration = enabled;
        return this;
    }

    public IMainThreadDispatcherBuilder WithUnitySettings(string gameObjectName, bool dontDestroyOnLoad = true)
    {
        if (string.IsNullOrWhiteSpace(gameObjectName))
        {
            var message = "GameObject name cannot be null or empty.";
            _logger?.LogWarning(message);
            throw new ArgumentException(message, nameof(gameObjectName));
        }
        
        _gameObjectName = gameObjectName;
        _dontDestroyOnLoad = dontDestroyOnLoad;
        return this;
    }

    public IMainThreadDispatcherBuilder UseHighPerformancePreset()
    {
        _frameBudgetMs = 4.0f;
        _maxActionsPerFrame = 25;
        _maxQueueCapacity = 5000;
        _initialQueueCapacity = 128;
        _logger?.LogInfo("Applied high-performance preset configuration");
        return this;
    }

    public IMainThreadDispatcherBuilder UseHighThroughputPreset()
    {
        _frameBudgetMs = 12.0f;
        _maxActionsPerFrame = 100;
        _maxQueueCapacity = 25000;
        _initialQueueCapacity = 512;
        _logger?.LogInfo("Applied high-throughput preset configuration");
        return this;
    }

    public MainThreadDispatcherConfig Build()
    {
        var config = new MainThreadDispatcherConfig
        {
            FrameBudgetMs = _frameBudgetMs,
            MaxActionsPerFrame = _maxActionsPerFrame,
            InitialQueueCapacity = _initialQueueCapacity,
            MaxQueueCapacity = _maxQueueCapacity,
            BackpressureStrategy = _backpressureStrategy,
            QueueWarningThreshold = _queueWarningThreshold,
            EnablePerformanceMonitoring = _enablePerformanceMonitoring,
            EnableHealthChecking = _enableHealthChecking,
            EnableMessageBusIntegration = _enableMessageBusIntegration,
            GameObjectName = _gameObjectName,
            DontDestroyOnLoad = _dontDestroyOnLoad
        };

        if (!config.IsValid())
        {
            var message = "MainThreadDispatcherConfig validation failed. Check configuration values.";
            _logger?.LogError(message);
            throw new InvalidOperationException(message);
        }

        _logger?.LogInfo($"MainThreadDispatcherConfig built successfully: FrameBudget={config.FrameBudgetMs}ms, MaxQueue={config.MaxQueueCapacity}");
        return config;
    }
}