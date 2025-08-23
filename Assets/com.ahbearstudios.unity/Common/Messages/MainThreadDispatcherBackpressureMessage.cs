using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Unity.Common.Messages;

/// <summary>
/// Message published when MainThreadDispatcher encounters backpressure.
/// </summary>
public record struct MainThreadDispatcherBackpressureMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public FixedString64Bytes Source { get; init; }
    public MessagePriority Priority { get; init; }
    public Guid CorrelationId { get; init; }
    
    // Backpressure-specific properties
    public int QueueCount { get; init; }
    public int MaxCapacity { get; init; }
    public string Strategy { get; init; }

    /// <summary>
    /// Initializes a new MainThreadDispatcherBackpressureMessage.
    /// </summary>
    /// <param name="queueCount">Current queue count</param>
    /// <param name="maxCapacity">Maximum queue capacity</param>
    /// <param name="strategy">Backpressure strategy being applied</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MainThreadDispatcherBackpressureMessage(int queueCount, int maxCapacity, string strategy, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = 3002; // Unity Common range
        Source = "MainThreadDispatcher";
        Priority = MessagePriority.High;
        CorrelationId = correlationId;
        QueueCount = queueCount;
        MaxCapacity = maxCapacity;
        Strategy = strategy ?? string.Empty;
    }
}