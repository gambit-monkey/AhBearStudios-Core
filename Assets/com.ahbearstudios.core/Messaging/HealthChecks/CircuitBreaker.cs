using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.HealthChecks;

/// <summary>
/// Circuit breaker implementation for message types.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly CircuitBreakerConfig _config;
    private readonly ILoggingService _logger;
    private volatile CircuitBreakerState _state;
    private DateTime _lastFailureTime;
    private DateTime _lastSuccessTime;
    private int _failureCount;
    private int _halfOpenSuccessCount;
    private readonly object _lock = new object();

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State => _state;

    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    public int FailureCount => _failureCount;

    /// <summary>
    /// Initializes a new instance of CircuitBreaker.
    /// </summary>
    /// <param name="config">Circuit breaker configuration</param>
    /// <param name="logger">Logging service</param>
    public CircuitBreaker(CircuitBreakerConfig config, ILoggingService logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = CircuitBreakerState.Closed;
        _lastSuccessTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _lastSuccessTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= _config.HalfOpenSuccessThreshold)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _halfOpenSuccessCount = 0;
                    _logger?.LogInfo("Circuit breaker transitioned to Closed state");
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                _failureCount = 0; // Reset failure count on success
            }
        }
    }

    /// <summary>
    /// Records a failed operation.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _lastFailureTime = DateTime.UtcNow;
            _failureCount++;

            if (_state == CircuitBreakerState.Closed && _failureCount >= _config.FailureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger?.LogWarning($"Circuit breaker opened after {_failureCount} failures");
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Open;
                _halfOpenSuccessCount = 0;
                _logger?.LogWarning("Circuit breaker returned to Open state from HalfOpen");
            }
        }
    }

    /// <summary>
    /// Updates the circuit breaker state based on timeout conditions.
    /// </summary>
    public void Update()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
                if (timeSinceLastFailure >= _config.OpenTimeout)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _halfOpenSuccessCount = 0;
                    _logger?.LogInfo("Circuit breaker transitioned to HalfOpen state");
                }
            }
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _halfOpenSuccessCount = 0;
            _logger?.LogInfo("Circuit breaker manually reset to Closed state");
        }
    }
}