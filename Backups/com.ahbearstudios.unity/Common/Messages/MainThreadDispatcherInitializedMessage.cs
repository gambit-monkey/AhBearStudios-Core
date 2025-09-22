using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Unity.Common.Messages;

/// <summary>
/// Message published when MainThreadDispatcher is initialized.
/// </summary>
public record struct MainThreadDispatcherInitializedMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public FixedString64Bytes Source { get; init; }
    public MessagePriority Priority { get; init; }
    public Guid CorrelationId { get; init; }
    
    // Dispatcher-specific properties
    public float FrameBudgetMs { get; init; }
    public int MaxQueueCapacity { get; init; }

    /// <summary>
    /// Initializes a new MainThreadDispatcherInitializedMessage.
    /// </summary>
    /// <param name="frameBudgetMs">The frame budget in milliseconds</param>
    /// <param name="maxQueueCapacity">The maximum queue capacity</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MainThreadDispatcherInitializedMessage(float frameBudgetMs, int maxQueueCapacity, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = 3001; // Unity Common range
        Source = "MainThreadDispatcher";
        Priority = MessagePriority.Low;
        CorrelationId = correlationId;
        FrameBudgetMs = frameBudgetMs;
        MaxQueueCapacity = maxQueueCapacity;
    }
}