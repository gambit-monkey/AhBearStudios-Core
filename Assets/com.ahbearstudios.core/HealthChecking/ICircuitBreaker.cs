using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking
{
    /// <summary>
    /// Interface for circuit breaker implementation providing fault tolerance and system protection
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Event triggered when circuit breaker state changes
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Unique name of this circuit breaker
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Configuration for this circuit breaker
        /// </summary>
        CircuitBreakerConfig Configuration { get; }

        /// <summary>
        /// Number of consecutive failures recorded
        /// </summary>
        int FailureCount { get; }

        /// <summary>
        /// Timestamp of the last failure
        /// </summary>
        DateTime? LastFailureTime { get; }

        /// <summary>
        /// Timestamp when circuit breaker last changed state
        /// </summary>
        DateTime LastStateChangeTime { get; }

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when circuit breaker is open</exception>
        /// <exception cref="ArgumentNullException">Thrown when operation is null</exception>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with circuit breaker protection (void return)
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="CircuitBreakerOpenException">Thrown when circuit breaker is open</exception>
        /// <exception cref="ArgumentNullException">Thrown when operation is null</exception>
        Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually opens the circuit breaker
        /// </summary>
        /// <param name="reason">Reason for opening the circuit breaker</param>
        void Open(string reason = null);

        /// <summary>
        /// Manually closes the circuit breaker
        /// </summary>
        /// <param name="reason">Reason for closing the circuit breaker</param>
        void Close(string reason = null);

        /// <summary>
        /// Moves circuit breaker to half-open state for testing
        /// </summary>
        /// <param name="reason">Reason for half-opening the circuit breaker</param>
        void HalfOpen(string reason = null);

        /// <summary>
        /// Resets the circuit breaker to closed state and clears failure count
        /// </summary>
        /// <param name="reason">Reason for resetting the circuit breaker</param>
        void Reset(string reason = null);

        /// <summary>
        /// Records a successful operation
        /// </summary>
        void RecordSuccess();

        /// <summary>
        /// Records a failed operation
        /// </summary>
        /// <param name="exception">Exception that caused the failure</param>
        void RecordFailure(Exception exception);

        /// <summary>
        /// Checks if the circuit breaker allows requests through
        /// </summary>
        /// <returns>True if requests are allowed, false otherwise</returns>
        bool AllowsRequests();

        /// <summary>
        /// Gets statistics about this circuit breaker
        /// </summary>
        /// <returns>Circuit breaker statistics</returns>
        CircuitBreakerStatistics GetStatistics();

        /// <summary>
        /// Gets the reason for the last state change
        /// </summary>
        /// <returns>Reason for last state change, or null if no reason was provided</returns>
        string GetLastStateChangeReason();
    }

    /// <summary>
    /// States of a circuit breaker
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Normal operation - all requests pass through
        /// </summary>
        Closed,

        /// <summary>
        /// Failure threshold exceeded - requests are blocked
        /// </summary>
        Open,

        /// <summary>
        /// Testing state - limited requests allowed to test recovery
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Name of the circuit breaker that is open
        /// </summary>
        public FixedString64Bytes CircuitBreakerName { get; }

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerOpenException class
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        public CircuitBreakerOpenException(FixedString64Bytes circuitBreakerName)
            : base($"Circuit breaker '{circuitBreakerName}' is open")
        {
            CircuitBreakerName = circuitBreakerName;
        }

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerOpenException class
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        /// <param name="message">Custom error message</param>
        public CircuitBreakerOpenException(FixedString64Bytes circuitBreakerName, string message)
            : base(message)
        {
            CircuitBreakerName = circuitBreakerName;
        }

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerOpenException class
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        /// <param name="message">Custom error message</param>
        /// <param name="innerException">Inner exception</param>
        public CircuitBreakerOpenException(FixedString64Bytes circuitBreakerName, string message, Exception innerException)
            : base(message, innerException)
        {
            CircuitBreakerName = circuitBreakerName;
        }
    }
}