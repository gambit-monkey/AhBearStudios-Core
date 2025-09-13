using System;
using System.Collections.Concurrent;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using Cysharp.Threading.Tasks;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service implementation for managing circuit breakers using HealthChecking system.
/// Provides proper integration with AhBearStudios core systems following CLAUDE.md guidelines.
/// </summary>
public sealed class MessageCircuitBreakerService : IMessageCircuitBreakerService
{
    private readonly MessageCircuitBreakerConfig _config;
    private readonly ILoggingService _logger;
    private readonly ICircuitBreakerFactory _circuitBreakerFactory;
    private readonly IMessageBusService _messageBus;
    private readonly ConcurrentDictionary<Type, ICircuitBreaker> _circuitBreakers;
    private readonly ProfilerMarker _getStateMarker;
    private readonly ProfilerMarker _recordMarker;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the MessageCircuitBreakerService.
    /// </summary>
    /// <param name="config">Configuration for the message circuit breaker service</param>
    /// <param name="logger">The logging service</param>
    /// <param name="circuitBreakerFactory">The circuit breaker factory from HealthChecking system</param>
    /// <param name="messageBus">The message bus service for publishing state changes</param>
    public MessageCircuitBreakerService(
        MessageCircuitBreakerConfig config,
        ILoggingService logger,
        ICircuitBreakerFactory circuitBreakerFactory,
        IMessageBusService messageBus = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreakerFactory = circuitBreakerFactory ?? throw new ArgumentNullException(nameof(circuitBreakerFactory));
        _messageBus = messageBus; // Optional dependency

        _circuitBreakers = new ConcurrentDictionary<Type, ICircuitBreaker>();

        // Initialize performance markers if enabled
        if (_config.EnablePerformanceMonitoring)
        {
            _getStateMarker = new ProfilerMarker("MessageCircuitBreaker.GetState");
            _recordMarker = new ProfilerMarker("MessageCircuitBreaker.Record");
        }

        _logger.LogInfo("MessageCircuitBreakerService initialized with HealthChecking system integration");
    }

    /// <summary>
    /// Gets the circuit breaker state for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <returns>Current circuit breaker state</returns>
    public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
    {
        using var marker = _config.EnablePerformanceMonitoring ? _getStateMarker.Auto() : default;
        
        ThrowIfDisposed();

        var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
        return circuitBreaker.State;
    }

    /// <summary>
    /// Resets the circuit breaker for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
    {
        ThrowIfDisposed();

        var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
        var previousState = circuitBreaker.State;
        
        circuitBreaker.Reset("Manual reset via MessageCircuitBreakerService");
        
        var newState = circuitBreaker.State;
        _logger.LogInfo($"Reset circuit breaker for message type {typeof(TMessage).Name}");
        
        // Publish state change message if enabled and state actually changed
        if (_config.PublishStateChanges && previousState != newState)
        {
            PublishStateChangeMessage<TMessage>(previousState, newState, "Manual reset");
        }
    }

    /// <summary>
    /// Checks if a message type's circuit breaker allows processing.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <returns>True if processing is allowed</returns>
    public bool CanProcess<TMessage>() where TMessage : IMessage
    {
        using var marker = _config.EnablePerformanceMonitoring ? _getStateMarker.Auto() : default;
        
        ThrowIfDisposed();

        var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
        return circuitBreaker.AllowsRequests();
    }

    /// <summary>
    /// Records a successful operation for a message type synchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public void RecordSuccess<TMessage>() where TMessage : IMessage
    {
        using var marker = _config.EnablePerformanceMonitoring ? _recordMarker.Auto() : default;
        
        ThrowIfDisposed();

        var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
        var previousState = circuitBreaker.State;

        // Use the circuit breaker's direct RecordSuccess method
        circuitBreaker.RecordSuccess();

        // Check if state changed and publish message if enabled
        var newState = circuitBreaker.State;
        if (_config.PublishStateChanges && previousState != newState)
        {
            PublishStateChangeMessage<TMessage>(previousState, newState, "Success recorded");
        }
    }

    /// <summary>
    /// Records a successful operation for a message type asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public async UniTask RecordSuccessAsync<TMessage>() where TMessage : IMessage
    {
        // For now, just call the synchronous version since circuit breaker RecordSuccess is sync
        // This could be enhanced in the future if async success recording is needed
        await UniTask.SwitchToMainThread();
        RecordSuccess<TMessage>();
    }

    /// <summary>
    /// Records a failed operation for a message type synchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="exception">The exception that caused the failure</param>
    public void RecordFailure<TMessage>(Exception exception) where TMessage : IMessage
    {
        using var marker = _config.EnablePerformanceMonitoring ? _recordMarker.Auto() : default;
        
        ThrowIfDisposed();

        var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
        var previousState = circuitBreaker.State;

        // Use the circuit breaker's direct RecordFailure method
        circuitBreaker.RecordFailure(exception);

        // Check if state changed and publish message if enabled
        var newState = circuitBreaker.State;
        if (_config.PublishStateChanges && previousState != newState)
        {
            PublishStateChangeMessage<TMessage>(previousState, newState, $"Failure recorded: {exception?.Message}");
        }
    }

    /// <summary>
    /// Records a failed operation for a message type asynchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="exception">The exception that caused the failure</param>
    public async UniTask RecordFailureAsync<TMessage>(Exception exception) where TMessage : IMessage
    {
        // For now, just call the synchronous version since circuit breaker RecordFailure is sync
        // This could be enhanced in the future if async failure recording is needed
        await UniTask.SwitchToMainThread();
        RecordFailure<TMessage>(exception);
    }

    /// <summary>
    /// Gets or creates a circuit breaker for the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <returns>Circuit breaker instance</returns>
    private ICircuitBreaker GetOrCreateCircuitBreaker<TMessage>() where TMessage : IMessage
    {
        var messageType = typeof(TMessage);
        
        return _circuitBreakers.GetOrAdd(messageType, _ =>
        {
            var circuitBreakerConfig = _config.GetConfigForMessageType(messageType);
            var circuitBreakerName = $"MessageBus_{messageType.Name}";

            // Use async factory method and wait for result
            // This is acceptable in GetOrAdd since circuit breaker creation is typically a one-time operation
            var breaker = _circuitBreakerFactory.CreateCircuitBreakerAsync(circuitBreakerName, circuitBreakerConfig)
                .GetAwaiter()
                .GetResult();
            
            // Note: The circuit breaker itself publishes state change messages via IMessageBusService
            // when state changes occur. We don't need to subscribe to events here.
            // If PublishStateChanges is enabled, we'll publish additional messages specific to the message type
            // when we detect state changes in RecordSuccess and RecordFailure methods.

            _logger.LogInfo($"Created circuit breaker for message type {messageType.Name}");
            return breaker;
        });
    }

    /// <summary>
    /// Publishes a state change message to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="oldState">Previous state</param>
    /// <param name="newState">New state</param>
    /// <param name="reason">Reason for change</param>
    private void PublishStateChangeMessage<TMessage>(CircuitBreakerState oldState, CircuitBreakerState newState, string reason)
        where TMessage : IMessage
    {
        if (_messageBus == null) return;

        try
        {
            var stateChangeMessage = new MessageBusCircuitBreakerStateChangedMessage
            {
                MessageType = typeof(TMessage),
                OldState = oldState,
                NewState = newState,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                CircuitBreakerName = $"MessageBus_{typeof(TMessage).Name}"
            };

            _messageBus.PublishMessage(stateChangeMessage);

            _logger.LogInfo($"Published circuit breaker state change for {typeof(TMessage).Name}: {oldState} → {newState} ({reason})");
        }
        catch (Exception ex)
        {
            _logger.LogException("Error publishing circuit breaker state change message", ex);
        }
    }

    /// <summary>
    /// Disposes the circuit breaker service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var breaker in _circuitBreakers.Values)
        {
            try
            {
                breaker?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException("Error disposing circuit breaker", ex);
            }
        }

        _circuitBreakers.Clear();
        _disposed = true;

        _logger.LogInfo("MessageCircuitBreakerService disposed");
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageCircuitBreakerService));
    }
}