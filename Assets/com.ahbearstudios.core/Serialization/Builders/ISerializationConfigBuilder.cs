using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization.Builders
{
    /// <summary>
    /// Interface for building serialization configurations.
    /// </summary>
    public interface ISerializationConfigBuilder
    {
        /// <summary>
        /// Sets the serialization format.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithFormat(SerializationFormat format);

        /// <summary>
        /// Sets the compression level.
        /// </summary>
        /// <param name="level">The compression level</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithCompression(CompressionLevel level);

        /// <summary>
        /// Sets the serialization mode.
        /// </summary>
        /// <param name="mode">The serialization mode</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithMode(SerializationMode mode);

        /// <summary>
        /// Enables or disables type validation.
        /// </summary>
        /// <param name="enabled">Whether to enable type validation</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithTypeValidation(bool enabled);

        /// <summary>
        /// Enables or disables performance monitoring.
        /// </summary>
        /// <param name="enabled">Whether to enable performance monitoring</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithPerformanceMonitoring(bool enabled);

        /// <summary>
        /// Configures buffer pooling settings.
        /// </summary>
        /// <param name="enabled">Whether to enable buffer pooling</param>
        /// <param name="maxPoolSize">Maximum pool size in bytes</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithBufferPooling(bool enabled, int maxPoolSize = 1024 * 1024);

        /// <summary>
        /// Configures versioning settings.
        /// </summary>
        /// <param name="enabled">Whether to enable versioning</param>
        /// <param name="strictMode">Whether to use strict versioning</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithVersioning(bool enabled, bool strictMode = false);

        /// <summary>
        /// Sets the maximum number of concurrent operations.
        /// </summary>
        /// <param name="maxConcurrent">Maximum concurrent operations</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithMaxConcurrentOperations(int maxConcurrent);

        /// <summary>
        /// Sets the async operation timeout.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithAsyncTimeout(int timeoutMs);

        /// <summary>
        /// Configures encryption settings.
        /// </summary>
        /// <param name="enabled">Whether to enable encryption</param>
        /// <param name="encryptionKey">Encryption key</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithEncryption(bool enabled, FixedString128Bytes encryptionKey = default);

        /// <summary>
        /// Adds types to the whitelist.
        /// </summary>
        /// <param name="typePatterns">Type name patterns to whitelist</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithTypeWhitelist(params string[] typePatterns);

        /// <summary>
        /// Adds types to the blacklist.
        /// </summary>
        /// <param name="typePatterns">Type name patterns to blacklist</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithTypeBlacklist(params string[] typePatterns);

        /// <summary>
        /// Adds a custom property.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>Builder instance for chaining</returns>
        ISerializationConfigBuilder WithCustomProperty(string key, object value);

        /// <summary>
        /// Builds the final configuration.
        /// </summary>
        /// <returns>Validated serialization configuration</returns>
        SerializationConfig Build();
    }
}