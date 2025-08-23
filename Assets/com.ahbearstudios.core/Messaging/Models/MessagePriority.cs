namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Defines the priority levels for message processing.
/// Higher values indicate higher priority.
/// </summary>
public enum MessagePriority : byte
{
    /// <summary>
    /// Debug priority messages for development and diagnostic purposes.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Very low priority messages for informational purposes.
    /// </summary>
    VeryLow = 1,

    /// <summary>
    /// Low priority messages that can be processed when resources are available.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Normal priority messages for regular system operations.
    /// </summary>
    Normal = 3,

    /// <summary>
    /// High priority messages that should be processed quickly.
    /// </summary>
    High = 4,

    /// <summary>
    /// Critical priority messages that require immediate processing.
    /// </summary>
    Critical = 5
}