using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for creating message registry configurations.
/// Handles complexity and validation following CLAUDE.md guidelines.
/// </summary>
public sealed class MessageRegistryBuilder : IMessageRegistryBuilder
{
    private bool _enableTypeDiscovery = true;
    private bool _enableCaching = true;
    private bool _enableStatistics = true;
    private int _cacheCleanupIntervalSeconds = 300;
    private int _statisticsResetIntervalSeconds = 3600;
    private int _maxCacheEntries = 1000;
    private bool _enableAutoRegistration = false;
    private ushort _typeCodeRangeStart = 3000;
    private ushort _typeCodeRangeEnd = 4999;
    private readonly Dictionary<string, string> _customCategories = new();

    /// <inheritdoc />
    public IMessageRegistryBuilder WithTypeDiscovery(bool enabled = true)
    {
        _enableTypeDiscovery = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithCaching(bool enabled = true)
    {
        _enableCaching = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithStatistics(bool enabled = true)
    {
        _enableStatistics = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithCacheCleanupInterval(int seconds)
    {
        if (seconds <= 0)
            throw new ArgumentException("Cache cleanup interval must be greater than zero", nameof(seconds));
        
        _cacheCleanupIntervalSeconds = seconds;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithStatisticsResetInterval(int seconds)
    {
        if (seconds <= 0)
            throw new ArgumentException("Statistics reset interval must be greater than zero", nameof(seconds));
        
        _statisticsResetIntervalSeconds = seconds;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithMaxCacheEntries(int maxEntries)
    {
        if (maxEntries <= 0)
            throw new ArgumentException("Max cache entries must be greater than zero", nameof(maxEntries));
        
        _maxCacheEntries = maxEntries;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithAutoRegistration(bool enabled = true)
    {
        _enableAutoRegistration = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder WithTypeCodeRange(ushort rangeStart, ushort rangeEnd)
    {
        if (rangeStart < 3000 || rangeStart > 64999)
            throw new ArgumentException("Range start must be in the custom range (3000-64999)", nameof(rangeStart));
        
        if (rangeEnd < 3000 || rangeEnd > 64999)
            throw new ArgumentException("Range end must be in the custom range (3000-64999)", nameof(rangeEnd));
        
        if (rangeStart >= rangeEnd)
            throw new ArgumentException("Range start must be less than range end");
        
        _typeCodeRangeStart = rangeStart;
        _typeCodeRangeEnd = rangeEnd;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder AddCustomCategory(string categoryKey, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryKey))
            throw new ArgumentException("Category key cannot be null or empty", nameof(categoryKey));
        
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name cannot be null or empty", nameof(categoryName));
        
        _customCategories[categoryKey] = categoryName;
        return this;
    }

    /// <inheritdoc />
    public IMessageRegistryBuilder UseTemplate(MessageRegistryConfig template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));
        
        _enableTypeDiscovery = template.EnableTypeDiscovery;
        _enableCaching = template.EnableCaching;
        _enableStatistics = template.EnableStatistics;
        _cacheCleanupIntervalSeconds = template.CacheCleanupIntervalSeconds;
        _statisticsResetIntervalSeconds = template.StatisticsResetIntervalSeconds;
        _maxCacheEntries = template.MaxCacheEntries;
        _enableAutoRegistration = template.EnableAutoRegistration;
        _typeCodeRangeStart = template.InitialTypeCodeRangeStart;
        _typeCodeRangeEnd = template.InitialTypeCodeRangeEnd;
        
        if (template.CustomCategories != null)
        {
            foreach (var kvp in template.CustomCategories)
            {
                _customCategories[kvp.Key] = kvp.Value;
            }
        }
        
        return this;
    }

    /// <inheritdoc />
    public MessageRegistryConfig Build()
    {
        var config = new MessageRegistryConfig
        {
            EnableTypeDiscovery = _enableTypeDiscovery,
            EnableCaching = _enableCaching,
            EnableStatistics = _enableStatistics,
            CacheCleanupIntervalSeconds = _cacheCleanupIntervalSeconds,
            StatisticsResetIntervalSeconds = _statisticsResetIntervalSeconds,
            MaxCacheEntries = _maxCacheEntries,
            EnableAutoRegistration = _enableAutoRegistration,
            InitialTypeCodeRangeStart = _typeCodeRangeStart,
            InitialTypeCodeRangeEnd = _typeCodeRangeEnd,
            CustomCategories = new Dictionary<string, string>(_customCategories)
        };

        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"MessageRegistryConfig validation failed: {string.Join(", ", validationErrors)}";
            throw new InvalidOperationException(errorMessage);
        }

        return config;
    }
}