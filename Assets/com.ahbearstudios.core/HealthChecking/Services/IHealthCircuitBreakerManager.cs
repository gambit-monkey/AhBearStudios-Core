using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for managing circuit breakers in the health check system.
    /// Provides automatic failure protection, state management, and recovery coordination.
    /// </summary>
    public interface IHealthCircuitBreakerManager : IDisposable
    {
        /// <summary>
        /// Event triggered when a circuit breaker state changes.
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event triggered when a circuit breaker trip occurs.
        /// </summary>
        event EventHandler<CircuitBreakerTripEventArgs> CircuitBreakerTripped;

        /// <summary>
        /// Event triggered when a circuit breaker recovery attempt is made.
        /// </summary>
        event EventHandler<CircuitBreakerRecoveryEventArgs> RecoveryAttempted;

        /// <summary>
        /// Gets whether the circuit breaker manager is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the total number of registered circuit breakers.
        /// </summary>
        int RegisteredCircuitBreakers { get; }

        /// <summary>
        /// Registers a circuit breaker for a health check.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="failureThreshold">Number of consecutive failures before opening</param>
        /// <param name="recoveryTimeout">Time to wait before attempting recovery</param>
        /// <param name="halfOpenTestCount">Number of tests to perform in half-open state</param>
        /// <returns>True if successfully registered, false if already exists</returns>
        bool RegisterCircuitBreaker(FixedString64Bytes healthCheckName, int failureThreshold, TimeSpan recoveryTimeout, int halfOpenTestCount = 1);

        /// <summary>
        /// Unregisters a circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>True if found and removed, false if not found</returns>
        bool UnregisterCircuitBreaker(FixedString64Bytes healthCheckName);

        /// <summary>
        /// Records a health check result and updates circuit breaker state.
        /// </summary>
        /// <param name="result">Health check result to record</param>
        /// <returns>True if execution should be allowed, false if circuit is open</returns>
        bool RecordHealthCheckResult(HealthCheckResult result);

        /// <summary>
        /// Checks if a health check execution is allowed by the circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>True if execution is allowed, false if circuit is open</returns>
        bool IsExecutionAllowed(FixedString64Bytes healthCheckName);

        /// <summary>
        /// Gets the current state of a circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>Current circuit breaker state, or null if not found</returns>
        CircuitBreakerState? GetCircuitBreakerState(FixedString64Bytes healthCheckName);

        /// <summary>
        /// Manually sets the state of a circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="newState">State to set</param>
        /// <param name="reason">Reason for the manual state change</param>
        /// <returns>True if state was changed, false if not found or already in that state</returns>
        bool SetCircuitBreakerState(FixedString64Bytes healthCheckName, CircuitBreakerState newState, string reason);

        /// <summary>
        /// Forces all circuit breakers to reset to closed state.
        /// </summary>
        /// <param name="reason">Reason for the reset</param>
        void ResetAllCircuitBreakers(string reason = null);

        /// <summary>
        /// Gets statistics for a specific circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>Circuit breaker statistics, or null if not found</returns>
        CircuitBreakerStatistics GetCircuitBreakerStatistics(FixedString64Bytes healthCheckName);

        /// <summary>
        /// Gets statistics for all registered circuit breakers.
        /// </summary>
        /// <returns>Dictionary of circuit breaker statistics</returns>
        IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> GetAllCircuitBreakerStatistics();

        /// <summary>
        /// Gets circuit breakers in a specific state.
        /// </summary>
        /// <param name="state">State to filter by</param>
        /// <returns>Collection of health check names in the specified state</returns>
        IEnumerable<FixedString64Bytes> GetCircuitBreakersInState(CircuitBreakerState state);

        /// <summary>
        /// Updates configuration for an existing circuit breaker.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="failureThreshold">New failure threshold</param>
        /// <param name="recoveryTimeout">New recovery timeout</param>
        /// <param name="halfOpenTestCount">New half-open test count</param>
        /// <returns>True if updated, false if not found</returns>
        bool UpdateCircuitBreakerConfiguration(FixedString64Bytes healthCheckName, int failureThreshold, TimeSpan recoveryTimeout, int halfOpenTestCount);

        /// <summary>
        /// Enables or disables the circuit breaker manager.
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker protection</param>
        /// <param name="reason">Reason for the change</param>
        void SetEnabled(bool enabled, string reason = null);

        /// <summary>
        /// Gets comprehensive circuit breaker system statistics.
        /// </summary>
        /// <returns>System-wide circuit breaker statistics</returns>
        CircuitBreakerSystemStatistics GetSystemStatistics();
    }

    /// <summary>
    /// Event arguments for circuit breaker state changes.
    /// </summary>
    public sealed record CircuitBreakerStateChangedEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the state change.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public CircuitBreakerState OldState { get; init; }

        /// <summary>
        /// Gets the new state.
        /// </summary>
        public CircuitBreakerState NewState { get; init; }

        /// <summary>
        /// Gets the reason for the state change.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the failure count at time of change.
        /// </summary>
        public int FailureCount { get; init; }

        /// <summary>
        /// Gets whether this was an automatic state change.
        /// </summary>
        public bool IsAutomatic { get; init; } = true;
    }

    /// <summary>
    /// Event arguments for circuit breaker trip events.
    /// </summary>
    public sealed record CircuitBreakerTripEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the trip.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the failure threshold that was exceeded.
        /// </summary>
        public int FailureThreshold { get; init; }

        /// <summary>
        /// Gets the consecutive failure count.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the last failure message.
        /// </summary>
        public string LastFailureMessage { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the recovery timeout period.
        /// </summary>
        public TimeSpan RecoveryTimeout { get; init; }
    }

    /// <summary>
    /// Event arguments for circuit breaker recovery events.
    /// </summary>
    public sealed record CircuitBreakerRecoveryEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the recovery attempt.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets whether the recovery attempt was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }

        /// <summary>
        /// Gets the new state after recovery attempt.
        /// </summary>
        public CircuitBreakerState NewState { get; init; }

        /// <summary>
        /// Gets the number of tests performed.
        /// </summary>
        public int TestsPerformed { get; init; }

        /// <summary>
        /// Gets the number of successful tests.
        /// </summary>
        public int SuccessfulTests { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets additional context about the recovery attempt.
        /// </summary>
        public string Context { get; init; }
    }

    /// <summary>
    /// System-wide circuit breaker statistics.
    /// </summary>
    public sealed record CircuitBreakerSystemStatistics
    {
        /// <summary>
        /// Gets the total number of registered circuit breakers.
        /// </summary>
        public int TotalCircuitBreakers { get; init; }

        /// <summary>
        /// Gets the number of circuit breakers in closed state.
        /// </summary>
        public int ClosedCircuitBreakers { get; init; }

        /// <summary>
        /// Gets the number of circuit breakers in open state.
        /// </summary>
        public int OpenCircuitBreakers { get; init; }

        /// <summary>
        /// Gets the number of circuit breakers in half-open state.
        /// </summary>
        public int HalfOpenCircuitBreakers { get; init; }

        /// <summary>
        /// Gets the total number of circuit breaker trips.
        /// </summary>
        public long TotalTrips { get; init; }

        /// <summary>
        /// Gets the total number of successful recoveries.
        /// </summary>
        public long TotalRecoveries { get; init; }

        /// <summary>
        /// Gets the total number of failed recovery attempts.
        /// </summary>
        public long FailedRecoveries { get; init; }

        /// <summary>
        /// Gets whether circuit breaker protection is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the timestamp when the manager started.
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Gets the uptime of the circuit breaker manager.
        /// </summary>
        public TimeSpan Uptime { get; init; }

        /// <summary>
        /// Gets the most frequently tripped circuit breaker.
        /// </summary>
        public FixedString64Bytes MostTrippedCircuitBreaker { get; init; }

        /// <summary>
        /// Gets the circuit breaker with the longest recovery time.
        /// </summary>
        public FixedString64Bytes LongestRecoveryCircuitBreaker { get; init; }
    }
}