using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Unity.Common.Messages;

/// <summary>
/// Message published when MainThreadDispatcher is being destroyed/shutdown.
/// Contains performance statistics from the dispatcher's lifetime.
/// </summary>
public record struct MainThreadDispatcherShutdownMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public FixedString64Bytes Source { get; init; }
    public MessagePriority Priority { get; init; }
    public Guid CorrelationId { get; init; }
    
    // Statistics properties
    public int TotalActionsProcessed { get; init; }
    public int TotalActionsDropped { get; init; }
    public int PeakQueueCount { get; init; }

    /// <summary>
    /// Initializes a new MainThreadDispatcherShutdownMessage.
    /// </summary>
    /// <param name="totalActionsProcessed">Total number of actions processed during lifetime</param>
    /// <param name="totalActionsDropped">Total number of actions dropped due to backpressure</param>
    /// <param name="peakQueueCount">Peak queue count reached during lifetime</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MainThreadDispatcherShutdownMessage(int totalActionsProcessed, int totalActionsDropped, int peakQueueCount, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = 3004; // Unity Common range
        Source = "MainThreadDispatcher";
        Priority = MessagePriority.Normal;
        CorrelationId = correlationId;
        TotalActionsProcessed = totalActionsProcessed;
        TotalActionsDropped = totalActionsDropped;
        PeakQueueCount = peakQueueCount;
    }
}