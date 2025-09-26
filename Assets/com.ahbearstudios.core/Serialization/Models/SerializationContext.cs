using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
    /// Context information for serialization operations.
    /// Provides state and configuration during serialization/deserialization.
    /// </summary>
    public record SerializationContext
    {
        /// <summary>
        /// Schema version for the serialization operation.
        /// </summary>
        public int Version { get; init; } = 1;

        /// <summary>
        /// Serialization mode being used.
        /// </summary>
        public SerializationMode Mode { get; init; } = SerializationMode.Production;

        /// <summary>
        /// Compression level being applied.
        /// </summary>
        public CompressionLevel Compression { get; init; } = CompressionLevel.Optimal;

        /// <summary>
        /// Custom properties for the serialization context.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = 
            new Dictionary<string, object>();

        /// <summary>
        /// Correlation ID for tracing serialization operations.
        /// </summary>
        public FixedString64Bytes CorrelationId { get; init; }

        /// <summary>
        /// Timestamp when the context was created.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Type resolver for custom type handling.
        /// </summary>
        public ITypeResolver TypeResolver { get; init; }

        /// <summary>
        /// Creates a context with a specific property.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>New context with the property added</returns>
        public SerializationContext WithProperty(string key, object value)
        {
            var newProperties = new Dictionary<string, object>(Properties) { [key] = value };
            return this with { Properties = newProperties };
        }

        /// <summary>
        /// Gets a property value with a default fallback.
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Property value or default</returns>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            return Properties.TryGetValue(key, out var value) && value is T typedValue 
                ? typedValue 
                : defaultValue;
        }
    }