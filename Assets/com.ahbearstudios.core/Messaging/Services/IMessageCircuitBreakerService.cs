using System;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service interface for managing circuit breakers for message types.
/// Uses the HealthChecking system's ICircuitBreaker instead of custom implementation.
/// </summary>
public interface IMessageCircuitBreakerService : IDisposable
{
    /// <summary>
    /// Gets the circuit breaker state for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <returns>Current circuit breaker state</returns>
    CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Resets the circuit breaker for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    void ResetCircuitBreaker<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Checks if a message type's circuit breaker allows processing.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <returns>True if processing is allowed</returns>
    bool CanProcess<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Records a successful operation for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    UniTask RecordSuccessAsync<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Records a successful operation for a message type synchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    void RecordSuccess<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Records a failed operation for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="exception">The exception that caused the failure</param>
    UniTask RecordFailureAsync<TMessage>(Exception exception) where TMessage : IMessage;

    /// <summary>
    /// Records a failed operation for a message type synchronously.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="exception">The exception that caused the failure</param>
    void RecordFailure<TMessage>(Exception exception) where TMessage : IMessage;
}