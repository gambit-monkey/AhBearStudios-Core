using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.HealthCheckING
{
    /// <summary>
    /// Production-ready circuit breaker implementation providing fault tolerance and system protection
    /// </summary>
    public sealed class CircuitBreaker : ICircuitBreaker, IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly object _stateLock = new();
        private readonly Timer _resetTimer;
        
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount;
        private DateTime? _lastFailureTime;
        private DateTime _lastStateChangeTime = DateTime.UtcNow;
        private string _lastStateChangeReason;
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private bool _disposed;

        /// <summary>
        /// Event triggered when circuit breaker state changes
        /// </summary>
        public event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Unique name of this circuit breaker
        /// </summary>
        public FixedString64Bytes Name { get; }

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public CircuitBreakerState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Configuration for this circuit breaker
        /// </summary>
        public CircuitBreakerConfig Configuration { get; }

        /// <summary>
        /// Number of consecutive failures recorded
        /// </summary>
        public int FailureCount
        {
            get
            {
                lock (_stateLock)
                {
                    return _failureCount;
                }
            }
        }

        /// <summary>
        /// Timestamp of the last failure
        /// </summary>
        public DateTime? LastFailureTime
        {
            get
            {
                lock (_stateLock)
                {
                    return _lastFailureTime;
                }
            }
        }

        /// <summary>
        /// Timestamp when circuit breaker last changed state
        /// </summary>
        public DateTime LastStateChangeTime
        {
            get
            {
                lock (_stateLock)
                {
                    return _lastStateChangeTime;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the CircuitBreaker class
        /// </summary>
        /// <param name="name">Unique name for this circuit breaker</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="logger">Logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public CircuitBreaker(
            FixedString64Bytes name,
            CircuitBreakerConfig configuration,
            ILoggingService logger)
        {
            if (name.IsEmpty)
                throw new ArgumentException("Circuit breaker name cannot be empty", nameof(name));

            Name = name;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate configuration
            var validationErrors = Configuration.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException($"Invalid circuit breaker configuration: {string.Join(", ", validationErrors)}");
            }

            // Set up reset timer for automatic state transitions
            _resetTimer = new Timer(CheckForAutomaticReset, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInfo($"Circuit breaker '{Name}' initialized with configuration: " +
                          $"FailureThreshold={Configuration.FailureThreshold}, " +
                          $"Timeout={Configuration.Timeout}, " +
                          $"SamplingDuration={Configuration.SamplingDuration}");
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when circuit breaker is open</exception>
        /// <exception cref="ArgumentNullException">Thrown when operation is null</exception>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            ThrowIfDisposed();

            if (!AllowsRequests())
            {
                var exception = new CircuitBreakerOpenException(Name, $"Circuit breaker '{Name}' is open");
                _logger.LogWarning($"Request blocked by circuit breaker '{Name}' in {State} state");
                throw exception;
            }

            Interlocked.Increment(ref _totalRequests);

            try
            {
                using var timeoutCts = new CancellationTokenSource(Configuration.Timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var result = await operation(combinedCts.Token).ConfigureAwait(false);
                
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                RecordFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection (void return)
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="CircuitBreakerOpenException">Thrown when circuit breaker is open</exception>
        /// <exception cref="ArgumentNullException">Thrown when operation is null</exception>
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return true; // Dummy return value
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Manually opens the circuit breaker
        /// </summary>
        /// <param name="reason">Reason for opening the circuit breaker</param>
        public void Open(string reason = null)
        {
            lock (_stateLock)
            {
                if (_state != CircuitBreakerState.Open)
                {
                    var oldState = _state;
                    _state = CircuitBreakerState.Open;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = reason ?? "Manually opened";

                    _logger.LogWarning($"Circuit breaker '{Name}' manually opened. Reason: {_lastStateChangeReason}");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Manually closes the circuit breaker
        /// </summary>
        /// <param name="reason">Reason for closing the circuit breaker</param>
        public void Close(string reason = null)
        {
            lock (_stateLock)
            {
                if (_state != CircuitBreakerState.Closed)
                {
                    var oldState = _state;
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _lastFailureTime = null;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = reason ?? "Manually closed";

                    _logger.LogInfo($"Circuit breaker '{Name}' manually closed. Reason: {_lastStateChangeReason}");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Moves circuit breaker to half-open state for testing
        /// </summary>
        /// <param name="reason">Reason for half-opening the circuit breaker</param>
        public void HalfOpen(string reason = null)
        {
            lock (_stateLock)
            {
                if (_state != CircuitBreakerState.HalfOpen)
                {
                    var oldState = _state;
                    _state = CircuitBreakerState.HalfOpen;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = reason ?? "Manually half-opened";

                    _logger.LogInfo($"Circuit breaker '{Name}' manually half-opened. Reason: {_lastStateChangeReason}");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Resets the circuit breaker to closed state and clears failure count
        /// </summary>
        /// <param name="reason">Reason for resetting the circuit breaker</param>
        public void Reset(string reason = null)
        {
            lock (_stateLock)
            {
                var oldState = _state;
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _lastFailureTime = null;
                _lastStateChangeTime = DateTime.UtcNow;
                _lastStateChangeReason = reason ?? "Reset";
                _totalRequests = 0;
                _successfulRequests = 0;
                _failedRequests = 0;

                _logger.LogInfo($"Circuit breaker '{Name}' reset. Reason: {_lastStateChangeReason}");
                
                if (oldState != _state)
                {
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Records a successful operation
        /// </summary>
        public void RecordSuccess()
        {
            Interlocked.Increment(ref _successfulRequests);

            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Successful request in half-open state should close the circuit
                    var oldState = _state;
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _lastFailureTime = null;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = "Successful request in half-open state";

                    _logger.LogInfo($"Circuit breaker '{Name}' closed after successful request in half-open state");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
                else if (_state == CircuitBreakerState.Closed)
                {
                    // Reset failure count on success
                    _failureCount = Math.Max(0, _failureCount - 1);
                }
            }
        }

        /// <summary>
        /// Records a failed operation
        /// </summary>
        /// <param name="exception">Exception that caused the failure</param>
        public void RecordFailure(Exception exception)
        {
            if (exception == null)
                return;

            // Check if this exception should be counted as a failure
            if (!ShouldCountAsFailure(exception))
                return;

            Interlocked.Increment(ref _failedRequests);

            lock (_stateLock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                _logger.LogWarning($"Circuit breaker '{Name}' recorded failure #{_failureCount}. Exception: {exception.GetType().Name}: {exception.Message}");

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Any failure in half-open state should open the circuit
                    var oldState = _state;
                    _state = CircuitBreakerState.Open;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = $"Failure in half-open state: {exception.GetType().Name}";

                    _logger.LogError($"Circuit breaker '{Name}' opened due to failure in half-open state");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
                else if (_state == CircuitBreakerState.Closed && _failureCount >= Configuration.FailureThreshold)
                {
                    // Threshold exceeded, open the circuit
                    var oldState = _state;
                    _state = CircuitBreakerState.Open;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = $"Failure threshold exceeded ({_failureCount}/{Configuration.FailureThreshold})";

                    _logger.LogError($"Circuit breaker '{Name}' opened due to failure threshold exceeded ({_failureCount}/{Configuration.FailureThreshold})");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Checks if the circuit breaker allows requests through
        /// </summary>
        /// <returns>True if requests are allowed, false otherwise</returns>
        public bool AllowsRequests()
        {
            lock (_stateLock)
            {
                return _state switch
                {
                    CircuitBreakerState.Closed => true,
                    CircuitBreakerState.Open => false,
                    CircuitBreakerState.HalfOpen => true,
                    _ => false
                };
            }
        }

        /// <summary>
        /// Gets statistics about this circuit breaker
        /// </summary>
        /// <returns>Circuit breaker statistics</returns>
        public CircuitBreakerStatistics GetStatistics()
        {
            lock (_stateLock)
            {
                var totalRequests = Interlocked.Read(ref _totalRequests);
                var successfulRequests = Interlocked.Read(ref _successfulRequests);
                var failedRequests = Interlocked.Read(ref _failedRequests);

                return new CircuitBreakerStatistics
                {
                    Name = Name,
                    State = _state,
                    FailureCount = _failureCount,
                    TotalRequests = totalRequests,
                    SuccessfulRequests = successfulRequests,
                    FailedRequests = failedRequests,
                    SuccessRate = totalRequests > 0 ? (double)successfulRequests / totalRequests : 0.0,
                    LastFailureTime = _lastFailureTime,
                    LastStateChangeTime = _lastStateChangeTime,
                    LastStateChangeReason = _lastStateChangeReason,
                    Configuration = Configuration
                };
            }
        }

        /// <summary>
        /// Gets the reason for the last state change
        /// </summary>
        /// <returns>Reason for last state change, or null if no reason was provided</returns>
        public string GetLastStateChangeReason()
        {
            lock (_stateLock)
            {
                return _lastStateChangeReason;
            }
        }

        /// <summary>
        /// Checks for automatic reset from open to half-open state
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void CheckForAutomaticReset(object state)
        {
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open && 
                    _lastFailureTime.HasValue && 
                    DateTime.UtcNow - _lastFailureTime.Value >= Configuration.Timeout)
                {
                    var oldState = _state;
                    _state = CircuitBreakerState.HalfOpen;
                    _lastStateChangeTime = DateTime.UtcNow;
                    _lastStateChangeReason = "Automatic transition to half-open after timeout";

                    _logger.LogInfo($"Circuit breaker '{Name}' automatically transitioned to half-open state after timeout");
                    OnStateChanged(oldState, _state, _lastStateChangeReason);
                }
            }
        }

        /// <summary>
        /// Determines if an exception should count as a failure for circuit breaker purposes
        /// </summary>
        /// <param name="exception">Exception to evaluate</param>
        /// <returns>True if the exception should count as a failure</returns>
        private bool ShouldCountAsFailure(Exception exception)
        {
            // Don't count cancellation as failures
            if (exception is OperationCanceledException)
                return false;

            // Count timeout exceptions as failures
            if (exception is TimeoutException)
                return true;

            // Add custom logic based on Configuration.ExceptionsToIgnore if implemented
            return true;
        }

        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="reason">Reason for state change</param>
        private void OnStateChanged(CircuitBreakerState oldState, CircuitBreakerState newState, string reason)
        {
            try
            {
                StateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
                {
                    CircuitBreakerName = Name,
                    OldState = oldState,
                    NewState = newState,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error raising StateChanged event for circuit breaker '{Name}'");
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the circuit breaker has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CircuitBreaker));
        }

        /// <summary>
        /// Disposes the circuit breaker and its resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _resetTimer?.Dispose();
                _disposed = true;
                _logger.LogInfo($"Circuit breaker '{Name}' disposed");
            }
        }
    }
}