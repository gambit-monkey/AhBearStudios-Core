using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Comprehensive statistics for message bus performance and health monitoring.
/// </summary>
public sealed class MessageBusStatistics
{
    /// <summary>
    /// Gets the instance name of the message bus.
    /// </summary>
    public string InstanceName { get; init; }

    /// <summary>
    /// Gets the total number of messages published.
    /// </summary>
    public long MessagesPublished { get; init; }

    /// <summary>
    /// Gets the total number of messages successfully processed.
    /// </summary>
    public long MessagesProcessed { get; init; }

    /// <summary>
    /// Gets the total number of messages that failed processing.
    /// </summary>
    public long MessagesFailed { get; init; }

    /// <summary>
    /// Gets the current number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Gets the current size of the dead letter queue.
    /// </summary>
    public int DeadLetterQueueSize { get; init; }

    /// <summary>
    /// Gets the current size of the retry queue.
    /// </summary>
    public int RetryQueueSize { get; init; }

    /// <summary>
    /// Gets the current queue depth (pending messages).
    /// </summary>
    public int CurrentQueueDepth { get; init; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; init; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public HealthStatus CurrentHealthStatus { get; init; }

    /// <summary>
    /// Gets statistics per message type.
    /// </summary>
    public Dictionary<Type, MessageTypeStatistics> MessageTypeStatistics { get; init; } = new();

    /// <summary>
    /// Gets circuit breaker states per message type.
    /// </summary>
    public Dictionary<Type, CircuitBreakerState> CircuitBreakerStates { get; init; } = new();

    /// <summary>
    /// Gets the number of active message scopes.
    /// </summary>
    public int ActiveScopes { get; init; }

    /// <summary>
    /// Gets the timestamp when statistics were last reset.
    /// </summary>
    public DateTime LastStatsReset { get; init; }

    /// <summary>
    /// Gets the current error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Gets the average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime { get; init; }
}