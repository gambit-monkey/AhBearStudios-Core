using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Specialized service responsible for managing circuit breaker states for health checks.
    /// Handles circuit breaker lifecycle, state transitions, and failure tracking.
    /// Follows CLAUDE.md patterns for specialized service delegation.
    /// </summary>
    public interface IHealthCheckCircuitBreakerService
    {
        /// <summary>
        /// Gets the current state of a circuit breaker for a specific operation.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Current circuit breaker state</returns>
        CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName);

        /// <summary>
        /// Gets all circuit breaker states.
        /// </summary>
        /// <returns>Dictionary of operation names to circuit breaker states</returns>
        Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates();

        /// <summary>
        /// Records a success for the specified operation, potentially closing an open circuit breaker.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="executionTime">Time taken for the successful operation</param>
        void RecordSuccess(FixedString64Bytes operationName, TimeSpan executionTime);

        /// <summary>
        /// Records a failure for the specified operation, potentially opening the circuit breaker.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="executionTime">Time taken before failure</param>
        void RecordFailure(FixedString64Bytes operationName, Exception exception, TimeSpan executionTime);

        /// <summary>
        /// Forces a circuit breaker to the open state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker open</param>
        void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Forces a circuit breaker to the closed state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker closed</param>
        void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Checks if an operation can be executed based on its circuit breaker state.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>True if the operation can be executed, false if circuit breaker is open</returns>
        bool CanExecuteOperation(FixedString64Bytes operationName);

        /// <summary>
        /// Registers a new circuit breaker for an operation.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit</param>
        /// <param name="timeout">Time to wait before attempting to close the circuit</param>
        /// <param name="successThreshold">Number of successes required to close the circuit</param>
        void RegisterCircuitBreaker(
            FixedString64Bytes operationName,
            int failureThreshold,
            TimeSpan timeout,
            int successThreshold = 1);

        /// <summary>
        /// Unregisters a circuit breaker for an operation.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>True if the circuit breaker was removed, false if it didn't exist</returns>
        bool UnregisterCircuitBreaker(FixedString64Bytes operationName);

        /// <summary>
        /// Gets statistics for a specific circuit breaker.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Circuit breaker statistics</returns>
        CircuitBreakerStatistics GetCircuitBreakerStatistics(FixedString64Bytes operationName);

        /// <summary>
        /// Gets statistics for all circuit breakers.
        /// </summary>
        /// <returns>Dictionary of operation names to statistics</returns>
        Dictionary<FixedString64Bytes, CircuitBreakerStatistics> GetAllCircuitBreakerStatistics();

        /// <summary>
        /// Resets statistics for a specific circuit breaker.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        void ResetCircuitBreakerStatistics(FixedString64Bytes operationName);

        /// <summary>
        /// Resets statistics for all circuit breakers.
        /// </summary>
        void ResetAllCircuitBreakerStatistics();

        /// <summary>
        /// Updates the configuration for an existing circuit breaker.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="failureThreshold">New failure threshold</param>
        /// <param name="timeout">New timeout</param>
        /// <param name="successThreshold">New success threshold</param>
        /// <returns>True if updated, false if circuit breaker doesn't exist</returns>
        bool UpdateCircuitBreakerConfiguration(
            FixedString64Bytes operationName,
            int failureThreshold,
            TimeSpan timeout,
            int successThreshold);

        /// <summary>
        /// Gets the count of registered circuit breakers.
        /// </summary>
        /// <returns>Number of registered circuit breakers</returns>
        int GetCircuitBreakerCount();

        /// <summary>
        /// Gets the count of open circuit breakers.
        /// </summary>
        /// <returns>Number of open circuit breakers</returns>
        int GetOpenCircuitBreakerCount();

        /// <summary>
        /// Event raised when a circuit breaker state changes.
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
    }

    /// <summary>
    /// Event arguments for circuit breaker state changes.
    /// </summary>
    public class CircuitBreakerStateChangedEventArgs : EventArgs
    {
        public FixedString64Bytes OperationName { get; }
        public CircuitBreakerState OldState { get; }
        public CircuitBreakerState NewState { get; }
        public string Reason { get; }
        public DateTime Timestamp { get; }

        public CircuitBreakerStateChangedEventArgs(
            FixedString64Bytes operationName,
            CircuitBreakerState oldState,
            CircuitBreakerState newState,
            string reason)
        {
            OperationName = operationName;
            OldState = oldState;
            NewState = newState;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }
}