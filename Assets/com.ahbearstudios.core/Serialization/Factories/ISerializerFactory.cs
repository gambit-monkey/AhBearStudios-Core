using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;

namespace AhBearStudios.Core.Serialization.Factories
{
    /// <summary>
    /// Interface for creating serializer instances.
    /// </summary>
    public interface ISerializerFactory
    {
        /// <summary>
        /// Creates a serializer with the specified configuration.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <returns>Configured serializer instance</returns>
        ISerializer CreateSerializer(SerializationConfig config);

        /// <summary>
        /// Creates a serializer for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>Serializer for the specified format</returns>
        ISerializer CreateSerializer(SerializationFormat format);

        /// <summary>
        /// Gets or creates a cached serializer instance.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <returns>Cached or new serializer instance</returns>
        ISerializer GetOrCreateSerializer(SerializationConfig config);

        /// <summary>
        /// Validates that a serializer can be created for the given configuration.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>True if serializer can be created</returns>
        bool CanCreateSerializer(SerializationConfig config);

        /// <summary>
        /// Gets all supported serialization formats.
        /// </summary>
        /// <returns>Array of supported formats</returns>
        SerializationFormat[] GetSupportedFormats();

        /// <summary>
        /// Clears the serializer cache.
        /// </summary>
        void ClearCache();
    }
}