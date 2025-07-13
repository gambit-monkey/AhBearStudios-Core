namespace AhBearStudios.Core.com.ahbearstudios.core.Messaging.Models;

/// <summary>
/// Defines the delivery mode for messages.
/// </summary>
public enum MessageDeliveryMode : byte
{
    /// <summary>
    /// Standard delivery with no special guarantees.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Express delivery with minimal latency.
    /// </summary>
    Express = 1,

    /// <summary>
    /// Persistent delivery with durability guarantees.
    /// </summary>
    Persistent = 2,

    /// <summary>
    /// Scheduled delivery at a specific time.
    /// </summary>
    Scheduled = 3,

    /// <summary>
    /// Batch delivery for high-throughput scenarios.
    /// </summary>
    Batch = 4
}