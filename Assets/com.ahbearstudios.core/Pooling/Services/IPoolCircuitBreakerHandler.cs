using System;
using AhBearStudios.Core.HealthChecking.Messages;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for handling circuit breaker events in the context of pool operations.
    /// Leverages the existing HealthChecking system's circuit breakers without duplicating functionality.
    /// Coordinates pool-specific responses to circuit breaker state changes.
    /// </summary>
    public interface IPoolCircuitBreakerHandler : IDisposable
    {
        #region Circuit Breaker State Management

        /// <summary>
        /// Handles circuit breaker state change notifications from the HealthChecking system.
        /// Coordinates pool-specific responses without duplicating circuit breaker logic.
        /// </summary>
        /// <param name="message">Circuit breaker state change message from HealthChecking</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask HandleCircuitBreakerStateChangeAsync(HealthCheckCircuitBreakerStateChangedMessage message, Guid correlationId = default);

        /// <summary>
        /// Checks if pool operations should be allowed based on circuit breaker states.
        /// </summary>
        /// <param name="poolName">Name of the pool to check</param>
        /// <param name="operationType">Type of operation (get, return, validate, etc.)</param>
        /// <returns>True if the operation should be allowed, false if blocked</returns>
        bool ShouldAllowPoolOperation(string poolName, string operationType);

        /// <summary>
        /// Checks if pool operations should be allowed for a specific object type.
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="operationType">Type of operation (get, return, validate, etc.)</param>
        /// <returns>True if the operation should be allowed, false if blocked</returns>
        bool ShouldAllowPoolOperation<T>(string operationType) where T : class, IPooledObject;

        #endregion

        #region Pool Response Actions

        /// <summary>
        /// Executes pool-specific actions when a circuit breaker opens.
        /// Examples: pause auto-scaling, clear corrupted objects, log warnings.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the opened circuit breaker</param>
        /// <param name="reason">Reason for circuit breaker opening</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask HandleCircuitBreakerOpenedAsync(string circuitBreakerName, string reason, Guid correlationId = default);

        /// <summary>
        /// Executes pool-specific actions when a circuit breaker closes (recovers).
        /// Examples: resume auto-scaling, validate pool health, log recovery.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the closed circuit breaker</param>
        /// <param name="reason">Reason for circuit breaker closing</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask HandleCircuitBreakerClosedAsync(string circuitBreakerName, string reason, Guid correlationId = default);

        /// <summary>
        /// Executes pool-specific actions when a circuit breaker enters half-open state.
        /// Examples: enable limited operations, monitor performance closely.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the half-open circuit breaker</param>
        /// <param name="reason">Reason for circuit breaker half-opening</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask HandleCircuitBreakerHalfOpenAsync(string circuitBreakerName, string reason, Guid correlationId = default);

        #endregion

        #region Pool Health Integration

        /// <summary>
        /// Reports pool health status to circuit breaker for decision making.
        /// This allows pools to influence circuit breaker behavior based on pool-specific health.
        /// </summary>
        /// <param name="poolName">Name of the pool reporting health</param>
        /// <param name="isHealthy">Whether the pool is currently healthy</param>
        /// <param name="healthMetrics">Optional health metrics data</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask ReportPoolHealthAsync(string poolName, bool isHealthy, string healthMetrics = null, Guid correlationId = default);

        /// <summary>
        /// Gets the current circuit breaker state for pool operations.
        /// </summary>
        /// <param name="poolName">Name of the pool to check</param>
        /// <returns>Circuit breaker state name, or null if no specific state</returns>
        string GetCircuitBreakerState(string poolName);

        #endregion

        #region Configuration

        /// <summary>
        /// Registers a pool with the circuit breaker handler for monitoring.
        /// Enables pool-specific circuit breaker logic.
        /// </summary>
        /// <param name="poolName">Name of the pool to register</param>
        /// <param name="poolType">Type name of objects in the pool</param>
        /// <param name="circuitBreakerName">Optional specific circuit breaker name to associate</param>
        void RegisterPool(string poolName, string poolType, string circuitBreakerName = null);

        /// <summary>
        /// Unregisters a pool from circuit breaker monitoring.
        /// </summary>
        /// <param name="poolName">Name of the pool to unregister</param>
        void UnregisterPool(string poolName);

        #endregion

        #region Statistics

        /// <summary>
        /// Gets circuit breaker statistics for pool operations.
        /// </summary>
        /// <param name="poolName">Optional pool name to filter statistics</param>
        /// <returns>Circuit breaker statistics related to pool operations</returns>
        object GetCircuitBreakerStatistics(string poolName = null);

        #endregion
    }
}