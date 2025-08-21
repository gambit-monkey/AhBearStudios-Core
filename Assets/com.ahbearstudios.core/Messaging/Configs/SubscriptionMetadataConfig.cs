using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for subscription metadata.
/// Immutable configuration object following CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public sealed record SubscriptionMetadataConfig
{
    /// <summary>
    /// Gets the description of the subscription configuration.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets the minimum priority level (if applicable).
    /// </summary>
    public MessagePriority? MinPriority { get; init; }

    /// <summary>
    /// Gets the source filter (if applicable).
    /// </summary>
    public string SourceFilter { get; init; }

    /// <summary>
    /// Gets the correlation ID filter (if applicable).
    /// </summary>
    public Guid? CorrelationFilter { get; init; }

    /// <summary>
    /// Gets additional custom properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; }

    /// <summary>
    /// Gets whether this configuration contains any filtering information.
    /// </summary>
    public bool HasFiltering => MinPriority.HasValue || 
                               !string.IsNullOrEmpty(SourceFilter) || 
                               CorrelationFilter.HasValue;

    /// <summary>
    /// Gets whether this configuration has any properties.
    /// </summary>
    public bool HasProperties => Properties.Count > 0;

    /// <summary>
    /// Gets whether this configuration is effectively empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Description) && 
                          !HasFiltering && 
                          !HasProperties;

    /// <summary>
    /// Initializes a new instance of the SubscriptionMetadataConfig record.
    /// </summary>
    /// <param name="description">The subscription description</param>
    /// <param name="minPriority">The minimum priority level</param>
    /// <param name="sourceFilter">The source filter</param>
    /// <param name="correlationFilter">The correlation ID filter</param>
    /// <param name="properties">Additional custom properties</param>
    public SubscriptionMetadataConfig(
        string description = null,
        MessagePriority? minPriority = null,
        string sourceFilter = null,
        Guid? correlationFilter = null,
        IReadOnlyDictionary<string, object> properties = null)
    {
        Description = description ?? string.Empty;
        MinPriority = minPriority;
        SourceFilter = sourceFilter;
        CorrelationFilter = correlationFilter;
        Properties = properties ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets an empty configuration instance.
    /// </summary>
    public static SubscriptionMetadataConfig Empty => new();
}