using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization.Builders;

/// <summary>
    /// Builder implementation for creating serialization configurations.
    /// Provides fluent API with validation and error checking.
    /// </summary>
    public class SerializationConfigBuilder : ISerializationConfigBuilder
    {
        private SerializationFormat _format = SerializationFormat.MemoryPack;
        private CompressionLevel _compression = CompressionLevel.Optimal;
        private SerializationMode _mode = SerializationMode.Production;
        private bool _enableTypeValidation = true;
        private bool _enablePerformanceMonitoring = true;
        private bool _enableBufferPooling = true;
        private int _maxBufferPoolSize = 1024 * 1024;
        private bool _enableVersioning = true;
        private bool _strictVersioning = false;
        private int _maxConcurrentOperations = Environment.ProcessorCount * 2;
        private int _asyncTimeoutMs = 30000;
        private bool _enableEncryption = false;
        private FixedString128Bytes _encryptionKey = default;
        private readonly List<string> _typeWhitelist = new();
        private readonly List<string> _typeBlacklist = new();
        private readonly Dictionary<string, object> _customProperties = new();
        private bool _enableFishNetSupport = false;
        private FishNetSerializationOptions _fishNetOptions = new();

        /// <inheritdoc />
        public ISerializationConfigBuilder WithFormat(SerializationFormat format)
        {
            _format = format;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithCompression(CompressionLevel level)
        {
            _compression = level;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithMode(SerializationMode mode)
        {
            _mode = mode;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithTypeValidation(bool enabled)
        {
            _enableTypeValidation = enabled;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithPerformanceMonitoring(bool enabled)
        {
            _enablePerformanceMonitoring = enabled;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithBufferPooling(bool enabled, int maxPoolSize = 1024 * 1024)
        {
            if (maxPoolSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Pool size must be positive");

            _enableBufferPooling = enabled;
            _maxBufferPoolSize = maxPoolSize;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithVersioning(bool enabled, bool strictMode = false)
        {
            _enableVersioning = enabled;
            _strictVersioning = strictMode;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithMaxConcurrentOperations(int maxConcurrent)
        {
            if (maxConcurrent <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent operations must be positive");

            _maxConcurrentOperations = maxConcurrent;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithAsyncTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive");

            _asyncTimeoutMs = timeoutMs;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithEncryption(bool enabled, FixedString128Bytes encryptionKey = default)
        {
            if (enabled && encryptionKey.IsEmpty)
                throw new ArgumentException("Encryption key cannot be empty when encryption is enabled", nameof(encryptionKey));

            _enableEncryption = enabled;
            _encryptionKey = encryptionKey;
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithTypeWhitelist(params string[] typePatterns)
        {
            if (typePatterns == null)
                throw new ArgumentNullException(nameof(typePatterns));

            _typeWhitelist.AddRange(typePatterns.Where(p => !string.IsNullOrWhiteSpace(p)));
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithTypeBlacklist(params string[] typePatterns)
        {
            if (typePatterns == null)
                throw new ArgumentNullException(nameof(typePatterns));

            _typeBlacklist.AddRange(typePatterns.Where(p => !string.IsNullOrWhiteSpace(p)));
            return this;
        }

        /// <inheritdoc />
        public ISerializationConfigBuilder WithCustomProperty(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));

            _customProperties[key] = value;
            return this;
        }

        /// <summary>
        /// Configures FishNet network serialization support.
        /// </summary>
        /// <param name="enabled">Whether to enable FishNet support</param>
        /// <param name="options">FishNet-specific options</param>
        /// <returns>Builder instance for chaining</returns>
        public ISerializationConfigBuilder WithFishNetSupport(bool enabled, FishNetSerializationOptions options = null)
        {
            _enableFishNetSupport = enabled;
            _fishNetOptions = options ?? (enabled ? FishNetSerializationOptions.Default : new FishNetSerializationOptions());
            return this;
        }

        /// <inheritdoc />
        public SerializationConfig Build()
        {
            var config = new SerializationConfig
            {
                Format = _format,
                Compression = _compression,
                Mode = _mode,
                EnableTypeValidation = _enableTypeValidation,
                EnablePerformanceMonitoring = _enablePerformanceMonitoring,
                EnableBufferPooling = _enableBufferPooling,
                MaxBufferPoolSize = _maxBufferPoolSize,
                EnableVersioning = _enableVersioning,
                StrictVersioning = _strictVersioning,
                MaxConcurrentOperations = _maxConcurrentOperations,
                AsyncTimeoutMs = _asyncTimeoutMs,
                EnableEncryption = _enableEncryption,
                EncryptionKey = _encryptionKey,
                TypeWhitelist = _typeWhitelist.AsReadOnly(),
                TypeBlacklist = _typeBlacklist.AsReadOnly(),
                CustomProperties = new ReadOnlyDictionary<string, object>(_customProperties),
                EnableFishNetSupport = _enableFishNetSupport,
                FishNetOptions = _fishNetOptions
            };

            return config.Validate();
        }

        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        /// <returns>New builder instance</returns>
        public static ISerializationConfigBuilder Create() => new SerializationConfigBuilder();

        /// <summary>
        /// Creates a builder from an existing configuration.
        /// </summary>
        /// <param name="config">Base configuration</param>
        /// <returns>Builder with pre-populated values</returns>
        public static ISerializationConfigBuilder FromConfig(SerializationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var builder = new SerializationConfigBuilder
            {
                _format = config.Format,
                _compression = config.Compression,
                _mode = config.Mode,
                _enableTypeValidation = config.EnableTypeValidation,
                _enablePerformanceMonitoring = config.EnablePerformanceMonitoring,
                _enableBufferPooling = config.EnableBufferPooling,
                _maxBufferPoolSize = config.MaxBufferPoolSize,
                _enableVersioning = config.EnableVersioning,
                _strictVersioning = config.StrictVersioning,
                _maxConcurrentOperations = config.MaxConcurrentOperations,
                _asyncTimeoutMs = config.AsyncTimeoutMs,
                _enableEncryption = config.EnableEncryption,
                _encryptionKey = config.EncryptionKey,
                _enableFishNetSupport = config.EnableFishNetSupport,
                _fishNetOptions = config.FishNetOptions
            };

            builder._typeWhitelist.AddRange(config.TypeWhitelist);
            builder._typeBlacklist.AddRange(config.TypeBlacklist);
            
            foreach (var kvp in config.CustomProperties)
            {
                builder._customProperties[kvp.Key] = kvp.Value;
            }

            return builder;
        }
    }