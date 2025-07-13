namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
    /// Metadata associated with a subscription for extended information and diagnostics.
    /// Uses immutable design for thread safety and performance.
    /// </summary>
    public sealed class SubscriptionMetadata
    {
        /// <summary>
        /// Gets the description of the subscription configuration.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the minimum priority level (if applicable).
        /// </summary>
        public MessagePriority? MinPriority { get; }

        /// <summary>
        /// Gets the source filter (if applicable).
        /// </summary>
        public string SourceFilter { get; }

        /// <summary>
        /// Gets the correlation ID filter (if applicable).
        /// </summary>
        public Guid? CorrelationFilter { get; }

        /// <summary>
        /// Gets additional custom properties.
        /// </summary>
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets whether this metadata contains any filtering information.
        /// </summary>
        public bool HasFiltering => MinPriority.HasValue || !string.IsNullOrEmpty(SourceFilter) || CorrelationFilter.HasValue;

        /// <summary>
        /// Gets an empty metadata instance.
        /// </summary>
        public static SubscriptionMetadata Empty { get; } = new();

        /// <summary>
        /// Initializes a new instance of the SubscriptionMetadata class.
        /// </summary>
        /// <param name="description">The subscription description</param>
        /// <param name="minPriority">The minimum priority level</param>
        /// <param name="sourceFilter">The source filter</param>
        /// <param name="correlationFilter">The correlation ID filter</param>
        /// <param name="properties">Additional custom properties</param>
        public SubscriptionMetadata(
            string description = null,
            MessagePriority? minPriority = null,
            string sourceFilter = null,
            Guid? correlationFilter = null,
            System.Collections.Generic.IReadOnlyDictionary<string, object> properties = null)
        {
            Description = description ?? string.Empty;
            MinPriority = minPriority;
            SourceFilter = sourceFilter;
            CorrelationFilter = correlationFilter;
            Properties = properties ?? new System.Collections.Generic.Dictionary<string, object>();
        }

        /// <summary>
        /// Returns a string representation of the metadata.
        /// </summary>
        /// <returns>Formatted metadata string</returns>
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(Description))
                parts.Add($"Description: {Description}");

            if (MinPriority.HasValue)
                parts.Add($"MinPriority: {MinPriority.Value}");

            if (!string.IsNullOrEmpty(SourceFilter))
                parts.Add($"Source: {SourceFilter}");

            if (CorrelationFilter.HasValue)
                parts.Add($"Correlation: {CorrelationFilter.Value}");

            if (Properties.Count > 0)
                parts.Add($"Properties: {Properties.Count}");

            return parts.Count > 0 ? string.Join(", ", parts) : "Empty";
        }

        /// <summary>
        /// Creates metadata for a priority subscription.
        /// </summary>
        /// <param name="minPriority">The minimum priority level</param>
        /// <returns>Priority subscription metadata</returns>
        public static SubscriptionMetadata ForPriority(MessagePriority minPriority) =>
            new($"Priority subscription with minimum level {minPriority}", minPriority);

        /// <summary>
        /// Creates metadata for a source-filtered subscription.
        /// </summary>
        /// <param name="sourceFilter">The source filter</param>
        /// <returns>Source-filtered subscription metadata</returns>
        public static SubscriptionMetadata ForSource(string sourceFilter) =>
            new($"Source-filtered subscription for '{sourceFilter}'", sourceFilter: sourceFilter);

        /// <summary>
        /// Creates metadata for a correlation-filtered subscription.
        /// </summary>
        /// <param name="correlationFilter">The correlation ID filter</param>
        /// <returns>Correlation-filtered subscription metadata</returns>
        public static SubscriptionMetadata ForCorrelation(Guid correlationFilter) =>
            new($"Correlation-filtered subscription for {correlationFilter}", correlationFilter: correlationFilter);
    }