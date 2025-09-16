using System.Collections.Generic;
using AhBearStudios.Core.Serialization.Models;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Service interface for managing serialization format selection, detection, and fallback chains.
    /// Extracts format selection logic from SerializationOperationCoordinator for better separation of concerns.
    /// Follows CLAUDE.md service patterns and Unity game development performance requirements.
    /// </summary>
    public interface IFormatSelectionService
    {
        /// <summary>
        /// Determines the best serialization format for a given type.
        /// Considers type characteristics, performance requirements, and available serializers.
        /// </summary>
        /// <typeparam name="T">Type to evaluate</typeparam>
        /// <param name="preferredFormat">Preferred format if available</param>
        /// <param name="availableFormats">Collection of currently available formats</param>
        /// <returns>Best available serialization format</returns>
        SerializationFormat SelectBestFormat<T>(
            SerializationFormat? preferredFormat,
            IReadOnlyCollection<SerializationFormat> availableFormats);

        /// <summary>
        /// Detects the serialization format of byte array data.
        /// Analyzes byte patterns and signatures to identify format.
        /// </summary>
        /// <param name="data">Data to analyze</param>
        /// <returns>Detected format or null if unable to detect</returns>
        SerializationFormat? DetectFormat(byte[] data);

        /// <summary>
        /// Gets the fallback chain for a specific format.
        /// Returns ordered list of formats to try if primary format fails.
        /// </summary>
        /// <param name="primaryFormat">Primary format to get fallbacks for</param>
        /// <returns>Ordered array of fallback formats</returns>
        SerializationFormat[] GetFallbackChain(SerializationFormat primaryFormat);

        /// <summary>
        /// Checks if a format is suitable for a specific type.
        /// Evaluates compatibility and performance characteristics.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <param name="format">Format to evaluate</param>
        /// <returns>True if format is suitable for the type</returns>
        bool IsFormatSuitableForType<T>(SerializationFormat format);

        /// <summary>
        /// Gets the recommended formats for a specific type.
        /// Returns formats ordered by suitability.
        /// </summary>
        /// <typeparam name="T">Type to evaluate</typeparam>
        /// <returns>Ordered collection of recommended formats</returns>
        IReadOnlyList<SerializationFormat> GetRecommendedFormats<T>();

        /// <summary>
        /// Registers a custom fallback chain for a specific format.
        /// Allows runtime configuration of fallback strategies.
        /// </summary>
        /// <param name="primaryFormat">Format to configure</param>
        /// <param name="fallbackChain">Custom fallback chain</param>
        void RegisterFallbackChain(SerializationFormat primaryFormat, SerializationFormat[] fallbackChain);

        /// <summary>
        /// Clears all custom fallback chains and resets to defaults.
        /// </summary>
        void ResetFallbackChains();

        /// <summary>
        /// Gets format compatibility score for a specific type.
        /// Higher scores indicate better compatibility.
        /// </summary>
        /// <typeparam name="T">Type to evaluate</typeparam>
        /// <param name="format">Format to score</param>
        /// <returns>Compatibility score (0.0 to 1.0)</returns>
        double GetFormatCompatibilityScore<T>(SerializationFormat format);
    }
}