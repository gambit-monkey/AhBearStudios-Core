using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder interface for creating message registry configurations.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageRegistryBuilder
{
    /// <summary>
    /// Enables or disables automatic type discovery.
    /// </summary>
    /// <param name="enabled">True to enable type discovery</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithTypeDiscovery(bool enabled = true);

    /// <summary>
    /// Enables or disables caching for performance optimization.
    /// </summary>
    /// <param name="enabled">True to enable caching</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithCaching(bool enabled = true);

    /// <summary>
    /// Enables or disables statistics tracking.
    /// </summary>
    /// <param name="enabled">True to enable statistics</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithStatistics(bool enabled = true);

    /// <summary>
    /// Sets the cache cleanup interval.
    /// </summary>
    /// <param name="seconds">Cleanup interval in seconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithCacheCleanupInterval(int seconds);

    /// <summary>
    /// Sets the statistics reset interval.
    /// </summary>
    /// <param name="seconds">Reset interval in seconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithStatisticsResetInterval(int seconds);

    /// <summary>
    /// Sets the maximum number of cache entries.
    /// </summary>
    /// <param name="maxEntries">Maximum cache entries</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithMaxCacheEntries(int maxEntries);

    /// <summary>
    /// Enables or disables automatic registration of discovered types.
    /// </summary>
    /// <param name="enabled">True to enable auto-registration</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithAutoRegistration(bool enabled = true);

    /// <summary>
    /// Sets the type code range for dynamic message types.
    /// </summary>
    /// <param name="rangeStart">Starting type code (3000-64999)</param>
    /// <param name="rangeEnd">Ending type code (3000-64999)</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder WithTypeCodeRange(ushort rangeStart, ushort rangeEnd);

    /// <summary>
    /// Adds a custom category for message organization.
    /// </summary>
    /// <param name="categoryKey">Category key</param>
    /// <param name="categoryName">Category display name</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder AddCustomCategory(string categoryKey, string categoryName);

    /// <summary>
    /// Uses a predefined configuration template.
    /// </summary>
    /// <param name="template">Configuration template</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageRegistryBuilder UseTemplate(MessageRegistryConfig template);

    /// <summary>
    /// Builds the final configuration.
    /// </summary>
    /// <returns>Validated message registry configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessageRegistryConfig Build();
}