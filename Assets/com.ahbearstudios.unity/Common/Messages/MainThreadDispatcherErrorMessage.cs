using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Unity.Common.Messages;

/// <summary>
/// Message published when MainThreadDispatcher encounters an error during action execution.
/// </summary>
public record struct MainThreadDispatcherErrorMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public FixedString64Bytes Source { get; init; }
    public MessagePriority Priority { get; init; }
    public Guid CorrelationId { get; init; }
    
    // Error-specific properties
    public string ErrorMessage { get; init; }
    public string Exception { get; init; }

    /// <summary>
    /// Initializes a new MainThreadDispatcherErrorMessage.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="exception">The exception details</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MainThreadDispatcherErrorMessage(string errorMessage, string exception, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = 3003; // Unity Common range
        Source = "MainThreadDispatcher";
        Priority = MessagePriority.High;
        CorrelationId = correlationId;
        ErrorMessage = errorMessage ?? string.Empty;
        Exception = exception ?? string.Empty;
    }
}