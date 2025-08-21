using System;
using System.Threading;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// MessagePipe filter that implements circuit breaker pattern for production stability.
/// Prevents cascading failures by monitoring error rates and temporarily blocking message processing
/// when failure thresholds are exceeded. Essential for Unity games to prevent system crashes.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class CircuitBreakerFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly ProfilerMarker _filterMarker;
    
    private readonly double _failureThreshold;
    private readonly int _minimumThroughput;
    private readonly TimeSpan _timeoutPeriod;
    private readonly TimeSpan _retryPeriod;
    
    private volatile CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _nextRetryTime = DateTime.MinValue;
    
    private long _successCount;
    private long _failureCount;
    private long _totalAttempts;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("CircuitBreakerFilter.Handle");

    /// <summary>
    /// Represents the state of the circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,     // Normal operation
        Open,       // Blocking requests due to failures
        HalfOpen    // Testing if service has recovered
    }

    /// <summary>
    /// Initializes a new CircuitBreakerFilter with production-ready circuit breaker settings.
    /// </summary>
    /// <param name="failureThreshold">Failure rate threshold (0.0 to 1.0) before opening circuit (default: 0.5)</param>
    /// <param name="minimumThroughput">Minimum number of attempts before evaluating failure rate (default: 10)</param>
    /// <param name="timeoutPeriod">Time to keep circuit open before attempting recovery (default: 30 seconds)</param>
    /// <param name="retryPeriod">Time between retry attempts in half-open state (default: 5 seconds)</param>
    /// <param name="logger">Optional logging service for circuit breaker events</param>
    /// <param name="alertService">Optional alert service for critical circuit breaker events</param>
    public CircuitBreakerFilter(
        double failureThreshold = 0.5,
        int minimumThroughput = 10,
        TimeSpan? timeoutPeriod = null,
        TimeSpan? retryPeriod = null,
        ILoggingService logger = null,
        IAlertService alertService = null)
    {
        if (failureThreshold is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Failure threshold must be between 0.0 and 1.0");
        if (minimumThroughput < 1)
            throw new ArgumentOutOfRangeException(nameof(minimumThroughput), "Minimum throughput must be at least 1");

        _failureThreshold = failureThreshold;
        _minimumThroughput = minimumThroughput;
        _timeoutPeriod = timeoutPeriod ?? TimeSpan.FromSeconds(30);
        _retryPeriod = retryPeriod ?? TimeSpan.FromSeconds(5);
        _logger = logger;
        _alertService = alertService;
        
        _filterMarker = new ProfilerMarker($"CircuitBreakerFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State => _state;

    /// <summary>
    /// Gets the current failure rate (0.0 to 1.0).
    /// </summary>
    public double FailureRate
    {
        get
        {
            var total = Interlocked.Read(ref _totalAttempts);
            var failures = Interlocked.Read(ref _failureCount);
            return total > 0 ? (double)failures / total : 0.0;
        }
    }

    /// <summary>
    /// Handles message processing with circuit breaker protection.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="next">The next handler in the filter chain</param>
    public override void Handle(TMessage message, Action<TMessage> next)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            // Check circuit breaker state before processing
            if (!ShouldAllowRequest())
            {
                _logger?.LogWarning($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Circuit breaker is {_state} - blocking message {message.Id}");
                return; // Circuit is open, block the request
            }

            Interlocked.Increment(ref _totalAttempts);

            try
            {
                // Process the message
                next(message);
                
                // Record success
                RecordSuccess();
                
                _logger?.LogDebug($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Successfully processed message {message.Id}");
            }
            catch (Exception ex)
            {
                // Record failure
                RecordFailure(ex);
                
                _logger?.LogException($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Failed to process message {message.Id}", ex);
                
                // Re-throw to maintain filter chain behavior
                throw;
            }
        }
    }

    /// <summary>
    /// Determines whether a request should be allowed based on circuit breaker state.
    /// </summary>
    private bool ShouldAllowRequest()
    {
        var currentTime = DateTime.UtcNow;
        
        switch (_state)
        {
            case CircuitBreakerState.Closed:
                return true;
                
            case CircuitBreakerState.Open:
                if (currentTime >= _nextRetryTime)
                {
                    TransitionToHalfOpen();
                    return true;
                }
                return false;
                
            case CircuitBreakerState.HalfOpen:
                return currentTime >= _nextRetryTime;
                
            default:
                return true;
        }
    }

    /// <summary>
    /// Records a successful message processing attempt.
    /// </summary>
    private void RecordSuccess()
    {
        Interlocked.Increment(ref _successCount);
        
        // If in half-open state, transition back to closed
        if (_state == CircuitBreakerState.HalfOpen)
        {
            TransitionToClosed();
        }
    }

    /// <summary>
    /// Records a failed message processing attempt.
    /// </summary>
    private void RecordFailure(Exception exception)
    {
        Interlocked.Increment(ref _failureCount);
        _lastFailureTime = DateTime.UtcNow;
        
        // Check if we should open the circuit
        if (_state != CircuitBreakerState.Open && ShouldOpenCircuit())
        {
            TransitionToOpen();
        }
        else if (_state == CircuitBreakerState.HalfOpen)
        {
            // Failure in half-open state - go back to open
            TransitionToOpen();
        }
    }

    /// <summary>
    /// Determines whether the circuit should be opened based on failure rate.
    /// </summary>
    private bool ShouldOpenCircuit()
    {
        var total = Interlocked.Read(ref _totalAttempts);
        var failures = Interlocked.Read(ref _failureCount);
        
        if (total < _minimumThroughput)
            return false;
            
        var failureRate = (double)failures / total;
        return failureRate >= _failureThreshold;
    }

    /// <summary>
    /// Transitions the circuit breaker to the Closed state.
    /// </summary>
    private void TransitionToClosed()
    {
        _state = CircuitBreakerState.Closed;
        ResetCounters();
        
        _logger?.LogInfo($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Circuit breaker transitioned to CLOSED");
        
        _alertService?.RaiseAlert(
            $"Circuit Breaker Closed: {typeof(TMessage).Name}",
            AlertSeverity.Info,
            new FixedString64Bytes("Circuit breaker recovered and is now closed"),
            new FixedString32Bytes("CircuitBreaker"),
            Guid.NewGuid());
    }

    /// <summary>
    /// Transitions the circuit breaker to the Open state.
    /// </summary>
    private void TransitionToOpen()
    {
        _state = CircuitBreakerState.Open;
        _nextRetryTime = DateTime.UtcNow.Add(_timeoutPeriod);
        
        var failureRate = FailureRate;
        _logger?.LogWarning($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Circuit breaker OPENED - failure rate: {failureRate:P2}");
        
        _alertService?.RaiseAlert(
            $"Circuit Breaker Opened: {typeof(TMessage).Name}",
            AlertSeverity.Critical,
            new FixedString64Bytes($"Failure rate: {failureRate:P2}, blocking requests"),
            new FixedString32Bytes("CircuitBreaker"),
            Guid.NewGuid());
    }

    /// <summary>
    /// Transitions the circuit breaker to the HalfOpen state.
    /// </summary>
    private void TransitionToHalfOpen()
    {
        _state = CircuitBreakerState.HalfOpen;
        _nextRetryTime = DateTime.UtcNow.Add(_retryPeriod);
        
        _logger?.LogInfo($"CircuitBreakerFilter<{typeof(TMessage).Name}>: Circuit breaker transitioned to HALF-OPEN for testing");
    }

    /// <summary>
    /// Resets the success and failure counters.
    /// </summary>
    private void ResetCounters()
    {
        Interlocked.Exchange(ref _successCount, 0);
        Interlocked.Exchange(ref _failureCount, 0);
        Interlocked.Exchange(ref _totalAttempts, 0);
    }
}