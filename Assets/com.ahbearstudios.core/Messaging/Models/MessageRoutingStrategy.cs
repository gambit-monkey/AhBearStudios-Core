namespace AhBearStudios.Core.com.ahbearstudios.core.Messaging.Models;

/// <summary>
/// Defines the routing strategy for messages.
/// </summary>
public enum MessageRoutingStrategy : byte
{
    /// <summary>
    /// Default routing based on message type and configuration.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Direct routing to specific destinations.
    /// </summary>
    Direct = 1,

    /// <summary>
    /// Broadcast routing to all subscribers.
    /// </summary>
    Broadcast = 2,

    /// <summary>
    /// Round-robin routing among available handlers.
    /// </summary>
    RoundRobin = 3,

    /// <summary>
    /// Load-balanced routing based on handler capacity.
    /// </summary>
    LoadBalanced = 4,

    /// <summary>
    /// Priority-based routing with handler selection.
    /// </summary>
    PriorityBased = 5,

    /// <summary>
    /// Content-based routing using message properties.
    /// </summary>
    ContentBased = 6
}