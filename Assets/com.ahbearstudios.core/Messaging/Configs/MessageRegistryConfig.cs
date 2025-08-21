using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for the message registry service.
/// Follows CLAUDE.md guidelines for simple, designer-friendly configuration.
/// </summary>
public sealed class MessageRegistryConfig
{
    /// <summary>
    /// Default configuration for message registry.
    /// </summary>
    public static readonly MessageRegistryConfig Default = new()
    {
        EnableTypeDiscovery = true,
        EnableCaching = true,
        EnableStatistics = true,
        CacheCleanupIntervalSeconds = 300,
        StatisticsResetIntervalSeconds = 3600,
        MaxCacheEntries = 1000,
        EnableAutoRegistration = false,
        InitialTypeCodeRangeStart = 3000, // Custom range for dynamic types
        InitialTypeCodeRangeEnd = 4999
    };

    /// <summary>
    /// Gets or sets whether to enable automatic type discovery.
    /// </summary>
    public bool EnableTypeDiscovery { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to enable type info caching for performance.
    /// </summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to track statistics.
    /// </summary>
    public bool EnableStatistics { get; init; } = true;

    /// <summary>
    /// Gets or sets the cache cleanup interval in seconds.
    /// </summary>
    public int CacheCleanupIntervalSeconds { get; init; } = 300;

    /// <summary>
    /// Gets or sets the statistics reset interval in seconds.
    /// </summary>
    public int StatisticsResetIntervalSeconds { get; init; } = 3600;

    /// <summary>
    /// Gets or sets the maximum number of cache entries.
    /// </summary>
    public int MaxCacheEntries { get; init; } = 1000;

    /// <summary>
    /// Gets or sets whether to automatically register discovered message types.
    /// </summary>
    public bool EnableAutoRegistration { get; init; } = false;

    /// <summary>
    /// Gets or sets the starting type code for dynamically assigned message types.
    /// Should be in the custom range (3000-64999).
    /// </summary>
    public ushort InitialTypeCodeRangeStart { get; init; } = 3000;

    /// <summary>
    /// Gets or sets the ending type code for dynamically assigned message types.
    /// Should be in the custom range (3000-64999).
    /// </summary>
    public ushort InitialTypeCodeRangeEnd { get; init; } = 4999;

    /// <summary>
    /// Gets or sets custom categories for message organization.
    /// </summary>
    public Dictionary<string, string> CustomCategories { get; init; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (CacheCleanupIntervalSeconds <= 0)
            errors.Add("CacheCleanupIntervalSeconds must be greater than zero");

        if (StatisticsResetIntervalSeconds <= 0)
            errors.Add("StatisticsResetIntervalSeconds must be greater than zero");

        if (MaxCacheEntries <= 0)
            errors.Add("MaxCacheEntries must be greater than zero");

        if (InitialTypeCodeRangeStart < 3000 || InitialTypeCodeRangeStart > 64999)
            errors.Add("InitialTypeCodeRangeStart must be in the custom range (3000-64999)");

        if (InitialTypeCodeRangeEnd < 3000 || InitialTypeCodeRangeEnd > 64999)
            errors.Add("InitialTypeCodeRangeEnd must be in the custom range (3000-64999)");

        if (InitialTypeCodeRangeStart >= InitialTypeCodeRangeEnd)
            errors.Add("InitialTypeCodeRangeStart must be less than InitialTypeCodeRangeEnd");

        return errors;
    }

    /// <summary>
    /// Creates a configuration optimized for development.
    /// </summary>
    public static MessageRegistryConfig ForDevelopment()
    {
        return new MessageRegistryConfig
        {
            EnableTypeDiscovery = true,
            EnableCaching = false, // Disable caching for development
            EnableStatistics = true,
            CacheCleanupIntervalSeconds = 60,
            StatisticsResetIntervalSeconds = 300,
            MaxCacheEntries = 100,
            EnableAutoRegistration = true, // Auto-register for convenience
            InitialTypeCodeRangeStart = 5000,
            InitialTypeCodeRangeEnd = 5999
        };
    }

    /// <summary>
    /// Creates a configuration optimized for production.
    /// </summary>
    public static MessageRegistryConfig ForProduction()
    {
        return new MessageRegistryConfig
        {
            EnableTypeDiscovery = false, // Manual registration only
            EnableCaching = true,
            EnableStatistics = true,
            CacheCleanupIntervalSeconds = 600,
            StatisticsResetIntervalSeconds = 7200,
            MaxCacheEntries = 5000,
            EnableAutoRegistration = false,
            InitialTypeCodeRangeStart = 3000,
            InitialTypeCodeRangeEnd = 3999
        };
    }
}