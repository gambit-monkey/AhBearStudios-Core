using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration for duplicate alert detection algorithms.
    /// Defines how alerts are compared to determine if they are duplicates.
    /// </summary>
    public sealed record DuplicateDetectionConfig
    {
        /// <summary>
        /// Gets whether the alert source is included in duplicate comparison.
        /// </summary>
        public bool CompareSource { get; init; } = true;

        /// <summary>
        /// Gets whether the alert message is included in duplicate comparison.
        /// </summary>
        public bool CompareMessage { get; init; } = true;

        /// <summary>
        /// Gets whether the alert severity is included in duplicate comparison.
        /// </summary>
        public bool CompareSeverity { get; init; } = false;

        /// <summary>
        /// Gets whether the alert tag is included in duplicate comparison.
        /// </summary>
        public bool CompareTag { get; init; } = false;

        /// <summary>
        /// Gets the similarity threshold for message comparison (0.0 to 1.0).
        /// Messages with similarity above this threshold are considered duplicates.
        /// </summary>
        public double MessageSimilarityThreshold { get; init; } = 0.95;

        /// <summary>
        /// Gets whether timestamps are ignored in duplicate comparison.
        /// When true, alerts with identical content but different timestamps are considered duplicates.
        /// </summary>
        public bool IgnoreTimestamps { get; init; } = true;

        /// <summary>
        /// Gets whether case sensitivity is applied to text comparisons.
        /// </summary>
        public bool CaseSensitive { get; init; } = false;

        /// <summary>
        /// Gets the collection of message patterns to normalize before comparison.
        /// Useful for removing variable parts like timestamps or IDs from messages.
        /// </summary>
        public IReadOnlyList<string> NormalizationPatterns { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Validates the duplicate detection configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (!CompareSource && !CompareMessage && !CompareSeverity && !CompareTag)
                throw new InvalidOperationException("At least one comparison field must be enabled.");

            if (MessageSimilarityThreshold < 0.0 || MessageSimilarityThreshold > 1.0)
                throw new InvalidOperationException("Message similarity threshold must be between 0.0 and 1.0.");
        }

        /// <summary>
        /// Gets the default duplicate detection configuration.
        /// </summary>
        public static DuplicateDetectionConfig Default => new()
        {
            CompareSource = true,
            CompareMessage = true,
            CompareSeverity = false,
            CompareTag = false,
            MessageSimilarityThreshold = 0.95,
            IgnoreTimestamps = true,
            CaseSensitive = false,
            NormalizationPatterns = Array.Empty<string>()
        };
    }
}