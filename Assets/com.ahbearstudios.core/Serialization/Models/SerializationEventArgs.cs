using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Event arguments for serialization events.
/// </summary>
public class SerializationEventArgs : EventArgs
{
    /// <summary>
    /// The type being serialized/deserialized.
    /// </summary>
    public Type TargetType { get; init; }

    /// <summary>
    /// Operation that triggered the event.
    /// </summary>
    public string Operation { get; init; }

    /// <summary>
    /// Size of data involved in the operation.
    /// </summary>
    public int DataSize { get; init; }

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Correlation ID for the operation.
    /// </summary>
    public FixedString64Bytes CorrelationId { get; init; }
}