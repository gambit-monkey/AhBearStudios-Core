using System.Collections.Generic;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Descriptor for type metadata during serialization.
/// </summary>
public record TypeDescriptor
{
    /// <summary>
    /// The type being described.
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// Type name for serialization.
    /// </summary>
    public string TypeName { get; init; }

    /// <summary>
    /// Schema version for this type.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Whether the type is registered for serialization.
    /// </summary>
    public bool IsRegistered { get; init; }

    /// <summary>
    /// Whether the type supports versioning.
    /// </summary>
    public bool SupportsVersioning { get; init; }

    /// <summary>
    /// Custom formatter type if applicable.
    /// </summary>
    public Type CustomFormatterType { get; init; }

    /// <summary>
    /// Estimated serialized size in bytes.
    /// </summary>
    public int EstimatedSize { get; init; }

    /// <summary>
    /// Additional metadata properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = 
        new Dictionary<string, object>();
}