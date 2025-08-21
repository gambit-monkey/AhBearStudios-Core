using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder interface for creating SubscriptionMetadata configurations.
/// Follows the Builder → Config → Factory → Service pattern from CLAUDE.md.
/// </summary>
public interface ISubscriptionMetadataBuilder
{
    /// <summary>
    /// Sets the description for the subscription.
    /// </summary>
    /// <param name="description">The subscription description</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder WithDescription(string description);

    /// <summary>
    /// Sets the minimum priority level for message filtering.
    /// </summary>
    /// <param name="minPriority">The minimum priority level</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder WithMinimumPriority(MessagePriority minPriority);

    /// <summary>
    /// Sets the source filter for message filtering.
    /// </summary>
    /// <param name="sourceFilter">The source filter</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder WithSourceFilter(string sourceFilter);

    /// <summary>
    /// Sets the correlation ID filter for message filtering.
    /// </summary>
    /// <param name="correlationFilter">The correlation ID filter</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder WithCorrelationFilter(Guid correlationFilter);

    /// <summary>
    /// Adds a custom property to the subscription metadata.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder AddProperty(string key, object value);

    /// <summary>
    /// Adds multiple custom properties to the subscription metadata.
    /// </summary>
    /// <param name="properties">The properties to add</param>
    /// <returns>Builder instance for fluent API</returns>
    ISubscriptionMetadataBuilder AddProperties(IDictionary<string, object> properties);

    /// <summary>
    /// Builds the subscription metadata configuration.
    /// </summary>
    /// <returns>Validated SubscriptionMetadataConfig</returns>
    SubscriptionMetadataConfig Build();
}

/// <summary>
/// Builder implementation for creating SubscriptionMetadata configurations.
/// Handles validation, default values, and complex setup logic.
/// </summary>
public sealed class SubscriptionMetadataBuilder : ISubscriptionMetadataBuilder
{
    private string _description;
    private MessagePriority? _minPriority;
    private string _sourceFilter;
    private Guid? _correlationFilter;
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// Sets the description for the subscription.
    /// </summary>
    /// <param name="description">The subscription description</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the minimum priority level for message filtering.
    /// </summary>
    /// <param name="minPriority">The minimum priority level</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder WithMinimumPriority(MessagePriority minPriority)
    {
        _minPriority = minPriority;
        return this;
    }

    /// <summary>
    /// Sets the source filter for message filtering.
    /// </summary>
    /// <param name="sourceFilter">The source filter</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder WithSourceFilter(string sourceFilter)
    {
        _sourceFilter = sourceFilter;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID filter for message filtering.
    /// </summary>
    /// <param name="correlationFilter">The correlation ID filter</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder WithCorrelationFilter(Guid correlationFilter)
    {
        _correlationFilter = correlationFilter;
        return this;
    }

    /// <summary>
    /// Adds a custom property to the subscription metadata.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder AddProperty(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Property key cannot be null or whitespace", nameof(key));

        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple custom properties to the subscription metadata.
    /// </summary>
    /// <param name="properties">The properties to add</param>
    /// <returns>Builder instance for fluent API</returns>
    public ISubscriptionMetadataBuilder AddProperties(IDictionary<string, object> properties)
    {
        if (properties == null) return this;

        foreach (var kvp in properties)
        {
            AddProperty(kvp.Key, kvp.Value);
        }

        return this;
    }

    /// <summary>
    /// Builds the subscription metadata configuration.
    /// Validates all parameters and creates a configuration object.
    /// </summary>
    /// <returns>Validated SubscriptionMetadataConfig</returns>
    public SubscriptionMetadataConfig Build()
    {
        return new SubscriptionMetadataConfig(
            description: _description,
            minPriority: _minPriority,
            sourceFilter: _sourceFilter,
            correlationFilter: _correlationFilter,
            properties: new Dictionary<string, object>(_properties)
        );
    }

    /// <summary>
    /// Creates a builder for a priority subscription.
    /// </summary>
    /// <param name="minPriority">The minimum priority level</param>
    /// <returns>Configured builder instance</returns>
    public static ISubscriptionMetadataBuilder ForPriority(MessagePriority minPriority)
    {
        return new SubscriptionMetadataBuilder()
            .WithDescription($"Priority subscription with minimum level {minPriority}")
            .WithMinimumPriority(minPriority);
    }

    /// <summary>
    /// Creates a builder for a source-filtered subscription.
    /// </summary>
    /// <param name="sourceFilter">The source filter</param>
    /// <returns>Configured builder instance</returns>
    public static ISubscriptionMetadataBuilder ForSource(string sourceFilter)
    {
        if (string.IsNullOrWhiteSpace(sourceFilter))
            throw new ArgumentException("Source filter cannot be null or whitespace", nameof(sourceFilter));

        return new SubscriptionMetadataBuilder()
            .WithDescription($"Source-filtered subscription for '{sourceFilter}'")
            .WithSourceFilter(sourceFilter);
    }

    /// <summary>
    /// Creates a builder for a correlation-filtered subscription.
    /// </summary>
    /// <param name="correlationFilter">The correlation ID filter</param>
    /// <returns>Configured builder instance</returns>
    public static ISubscriptionMetadataBuilder ForCorrelation(Guid correlationFilter)
    {
        return new SubscriptionMetadataBuilder()
            .WithDescription($"Correlation-filtered subscription for {correlationFilter}")
            .WithCorrelationFilter(correlationFilter);
    }
}