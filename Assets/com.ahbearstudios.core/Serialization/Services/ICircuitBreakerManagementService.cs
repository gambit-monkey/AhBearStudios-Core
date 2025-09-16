using System;
using System.Collections.Generic;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Service interface for managing circuit breakers for serialization operations.
    /// Extracts circuit breaker management logic from SerializationOperationCoordinator.
    /// Follows CLAUDE.md service patterns and fault tolerance best practices.
    /// </summary>
    public interface ICircuitBreakerManagementService
    {
        /// <summary>
        /// Gets or creates a circuit breaker for a specific serialization format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="config">Circuit breaker configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Circuit breaker instance</returns>
        CircuitBreaker GetOrCreateCircuitBreaker(
            SerializationFormat format,
            CircuitBreakerConfig config,
            Guid correlationId = default);

        /// <summary>
        /// Gets the circuit breaker for a specific format if it exists.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>Circuit breaker instance or null if not found</returns>
        CircuitBreaker GetCircuitBreaker(SerializationFormat format);

        /// <summary>
        /// Opens a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="reason">Reason for opening the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void OpenCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default);

        /// <summary>
        /// Closes a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="reason">Reason for closing the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void CloseCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default);

        /// <summary>
        /// Resets all circuit breakers to their initial state.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetAllCircuitBreakers(Guid correlationId = default);

        /// <summary>
        /// Resets a specific circuit breaker to its initial state.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetCircuitBreaker(SerializationFormat format, Guid correlationId = default);

        /// <summary>
        /// Gets circuit breaker statistics for all formats.
        /// </summary>
        /// <returns>Dictionary mapping formats to their circuit breaker statistics</returns>
        IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetAllStatistics();

        /// <summary>
        /// Gets circuit breaker statistics for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>Circuit breaker statistics or null if not found</returns>
        CircuitBreakerStatistics? GetStatistics(SerializationFormat format);

        /// <summary>
        /// Checks if a circuit breaker allows requests for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>True if requests are allowed, false otherwise</returns>
        bool IsRequestAllowed(SerializationFormat format);

        /// <summary>
        /// Records a successful operation for a specific format.
        /// Updates circuit breaker state accordingly.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RecordSuccess(SerializationFormat format, Guid correlationId = default);

        /// <summary>
        /// Records a failed operation for a specific format.
        /// May trigger circuit breaker state changes.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RecordFailure(SerializationFormat format, Exception exception, Guid correlationId = default);

        /// <summary>
        /// Gets the health status of all circuit breakers.
        /// </summary>
        /// <returns>Dictionary mapping formats to their health status</returns>
        IReadOnlyDictionary<SerializationFormat, bool> GetHealthStatus();

        /// <summary>
        /// Removes a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>True if removed, false if not found</returns>
        bool RemoveCircuitBreaker(SerializationFormat format);

        /// <summary>
        /// Updates the configuration for a specific circuit breaker.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="config">New configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void UpdateConfiguration(SerializationFormat format, CircuitBreakerConfig config, Guid correlationId = default);
    }
}