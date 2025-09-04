using System;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking
{
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