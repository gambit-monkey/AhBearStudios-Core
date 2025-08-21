namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Enumeration of simple message routing strategies.
/// Simplified from complex routing system to basic game-focused routing.
/// </summary>
public enum MessageRoutingStrategy : byte
{
    /// <summary>
    /// No specific routing strategy. Use system defaults.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Default routing strategy for standard message delivery.
    /// Routes based on message type and destination.
    /// </summary>
    Default = 1
}