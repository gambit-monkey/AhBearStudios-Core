using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Common.Utilities;
using ZLinq;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Service implementation for managing serialization format selection, detection, and fallback chains.
    /// Follows CLAUDE.md service patterns with proper separation of concerns.
    /// Optimized for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public sealed class FormatSelectionService : IFormatSelectionService
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly ConcurrentDictionary<SerializationFormat, SerializationFormat[]> _customFallbackChains;
        private readonly object _fallbackLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of FormatSelectionService.
        /// </summary>
        /// <param name="loggingService">Logging service for operation tracking</param>
        public FormatSelectionService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _customFallbackChains = new ConcurrentDictionary<SerializationFormat, SerializationFormat[]>();

            _loggingService.LogInfo("FormatSelectionService initialized successfully",
                GetCorrelationId(), nameof(FormatSelectionService));
        }

        #endregion

        #region IFormatSelectionService Implementation

        /// <inheritdoc />
        public SerializationFormat SelectBestFormat<T>(
            SerializationFormat? preferredFormat,
            IReadOnlyCollection<SerializationFormat> availableFormats)
        {
            if (availableFormats == null || availableFormats.Count == 0)
            {
                throw new InvalidOperationException("No serialization formats available");
            }

            var correlationId = GetCorrelationId();
            _loggingService.LogDebug($"Selecting best format for type {typeof(T).Name}",
                correlationId, nameof(FormatSelectionService));

            // If preferred format is specified and available, use it
            if (preferredFormat.HasValue && availableFormats.AsValueEnumerable().Contains(preferredFormat.Value))
            {
                _loggingService.LogDebug($"Using preferred format: {preferredFormat.Value}",
                    correlationId, nameof(FormatSelectionService));
                return preferredFormat.Value;
            }

            // Get recommended formats for the type
            var recommendedFormats = GetRecommendedFormats<T>();

            // Find first available recommended format
            foreach (var format in recommendedFormats)
            {
                if (availableFormats.AsValueEnumerable().Contains(format))
                {
                    _loggingService.LogDebug($"Selected recommended format: {format} for type {typeof(T).Name}",
                        correlationId, nameof(FormatSelectionService));
                    return format;
                }
            }

            // Fallback to first available format
            var fallbackFormat = availableFormats.AsValueEnumerable().First();
            _loggingService.LogWarning($"Using fallback format: {fallbackFormat} for type {typeof(T).Name}",
                correlationId, nameof(FormatSelectionService));
            return fallbackFormat;
        }

        /// <inheritdoc />
        public SerializationFormat? DetectFormat(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            var correlationId = GetCorrelationId();
            _loggingService.LogDebug($"Detecting format for {data.Length} bytes of data",
                correlationId, nameof(FormatSelectionService));

            // Check for JSON signature (starts with { or [)
            if (data[0] == 0x7B || data[0] == 0x5B) // { or [
            {
                _loggingService.LogDebug("Detected JSON format",
                    correlationId, nameof(FormatSelectionService));
                return SerializationFormat.Json;
            }

            // Check for XML signature (starts with <)
            if (data[0] == 0x3C) // <
            {
                _loggingService.LogDebug("Detected XML format",
                    correlationId, nameof(FormatSelectionService));
                return SerializationFormat.Xml;
            }

            // Check for MessagePack signature
            if (data.Length > 1 && (data[0] == 0xDC || data[0] == 0xDD))
            {
                _loggingService.LogDebug("Detected MessagePack format",
                    correlationId, nameof(FormatSelectionService));
                return SerializationFormat.MessagePack;
            }

            // Check for Protobuf (heuristic - first byte is field number and wire type)
            if (data.Length > 0 && (data[0] & 0x07) <= 5)
            {
                _loggingService.LogDebug("Possibly detected Protobuf format",
                    correlationId, nameof(FormatSelectionService));
                return SerializationFormat.Protobuf;
            }

            // Default to Binary or MemoryPack for unknown formats
            _loggingService.LogDebug("Could not detect format, assuming Binary/MemoryPack",
                correlationId, nameof(FormatSelectionService));
            return SerializationFormat.Binary;
        }

        /// <inheritdoc />
        public SerializationFormat[] GetFallbackChain(SerializationFormat primaryFormat)
        {
            // Check for custom fallback chain
            if (_customFallbackChains.TryGetValue(primaryFormat, out var customChain))
            {
                return customChain;
            }

            // Return default fallback chain based on format
            return primaryFormat switch
            {
                SerializationFormat.MemoryPack => new[]
                {
                    SerializationFormat.Binary,
                    SerializationFormat.MessagePack,
                    SerializationFormat.Json
                },
                SerializationFormat.Binary => new[]
                {
                    SerializationFormat.MemoryPack,
                    SerializationFormat.MessagePack,
                    SerializationFormat.Json
                },
                SerializationFormat.Json => new[]
                {
                    SerializationFormat.Xml,
                    SerializationFormat.Binary,
                    SerializationFormat.MemoryPack
                },
                SerializationFormat.Xml => new[]
                {
                    SerializationFormat.Json,
                    SerializationFormat.Binary,
                    SerializationFormat.MemoryPack
                },
                SerializationFormat.MessagePack => new[]
                {
                    SerializationFormat.MemoryPack,
                    SerializationFormat.Binary,
                    SerializationFormat.Json
                },
                SerializationFormat.Protobuf => new[]
                {
                    SerializationFormat.Binary,
                    SerializationFormat.MemoryPack,
                    SerializationFormat.MessagePack
                },
                SerializationFormat.FishNet => new[]
                {
                    SerializationFormat.Binary,
                    SerializationFormat.MemoryPack,
                    SerializationFormat.MessagePack
                },
                _ => new[] { SerializationFormat.Binary, SerializationFormat.Json }
            };
        }

        /// <inheritdoc />
        public bool IsFormatSuitableForType<T>(SerializationFormat format)
        {
            var type = typeof(T);

            // Check basic suitability rules
            switch (format)
            {
                case SerializationFormat.Json:
                case SerializationFormat.Xml:
                    // Text formats are suitable for most types but less efficient for binary data
                    return !type.IsArray || type.GetElementType() != typeof(byte);

                case SerializationFormat.MemoryPack:
                case SerializationFormat.MessagePack:
                case SerializationFormat.Binary:
                    // Binary formats are suitable for all types
                    return true;

                case SerializationFormat.Protobuf:
                    // Protobuf requires specific attributes or contracts
                    return type.IsDefined(typeof(SerializableAttribute), false) ||
                           type.IsDefined(typeof(DataContractAttribute), false);

                case SerializationFormat.FishNet:
                    // FishNet is for network-specific types
                    return type.IsValueType || type.IsPrimitive ||
                           type.IsArray || type.IsGenericType;

                default:
                    return true;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<SerializationFormat> GetRecommendedFormats<T>()
        {
            var type = typeof(T);
            var formats = new List<SerializationFormat>();

            // For primitive types, prefer binary formats
            if (type.IsPrimitive || type.IsValueType)
            {
                formats.Add(SerializationFormat.MemoryPack);
                formats.Add(SerializationFormat.Binary);
                formats.Add(SerializationFormat.MessagePack);
                formats.Add(SerializationFormat.Json);
            }
            // For arrays and collections, prefer efficient binary formats
            else if (type.IsArray || type.GetInterface("IEnumerable") != null)
            {
                formats.Add(SerializationFormat.MemoryPack);
                formats.Add(SerializationFormat.MessagePack);
                formats.Add(SerializationFormat.Binary);
                formats.Add(SerializationFormat.Json);
            }
            // For complex objects, prefer formats with good schema support
            else
            {
                formats.Add(SerializationFormat.Json);
                formats.Add(SerializationFormat.MemoryPack);
                formats.Add(SerializationFormat.MessagePack);
                formats.Add(SerializationFormat.Binary);
            }

            // Add remaining formats
            if (!formats.Contains(SerializationFormat.Xml))
                formats.Add(SerializationFormat.Xml);
            if (!formats.Contains(SerializationFormat.Protobuf))
                formats.Add(SerializationFormat.Protobuf);
            if (!formats.Contains(SerializationFormat.FishNet))
                formats.Add(SerializationFormat.FishNet);

            return formats;
        }

        /// <inheritdoc />
        public void RegisterFallbackChain(SerializationFormat primaryFormat, SerializationFormat[] fallbackChain)
        {
            if (fallbackChain == null || fallbackChain.Length == 0)
                throw new ArgumentException("Fallback chain cannot be null or empty", nameof(fallbackChain));

            var correlationId = GetCorrelationId();
            _customFallbackChains[primaryFormat] = fallbackChain;

            _loggingService.LogInfo($"Registered custom fallback chain for {primaryFormat} with {fallbackChain.Length} formats",
                correlationId, nameof(FormatSelectionService));
        }

        /// <inheritdoc />
        public void ResetFallbackChains()
        {
            lock (_fallbackLock)
            {
                _customFallbackChains.Clear();
            }

            _loggingService.LogInfo("All custom fallback chains have been reset",
                GetCorrelationId(), nameof(FormatSelectionService));
        }

        /// <inheritdoc />
        public double GetFormatCompatibilityScore<T>(SerializationFormat format)
        {
            var type = typeof(T);
            double score = 0.5; // Base score

            // Adjust score based on type characteristics
            if (type.IsPrimitive || type.IsValueType)
            {
                // Primitives work well with all formats
                score = format switch
                {
                    SerializationFormat.MemoryPack => 1.0,
                    SerializationFormat.Binary => 0.95,
                    SerializationFormat.MessagePack => 0.9,
                    SerializationFormat.Json => 0.7,
                    SerializationFormat.Xml => 0.6,
                    _ => 0.5
                };
            }
            else if (type.IsArray || type.GetInterface("IEnumerable") != null)
            {
                // Collections benefit from efficient binary formats
                score = format switch
                {
                    SerializationFormat.MemoryPack => 1.0,
                    SerializationFormat.MessagePack => 0.95,
                    SerializationFormat.Binary => 0.9,
                    SerializationFormat.Json => 0.6,
                    SerializationFormat.Xml => 0.5,
                    _ => 0.4
                };
            }
            else
            {
                // Complex objects need good schema support
                score = format switch
                {
                    SerializationFormat.Json => 0.9,
                    SerializationFormat.MemoryPack => 0.85,
                    SerializationFormat.MessagePack => 0.8,
                    SerializationFormat.Xml => 0.7,
                    SerializationFormat.Binary => 0.6,
                    _ => 0.5
                };
            }

            return Math.Max(0.0, Math.Min(1.0, score));
        }

        #endregion

        #region Private Methods

        private FixedString64Bytes GetCorrelationId()
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId(
                context: "FormatSelectionService",
                operation: "FormatSelection");
            return new FixedString64Bytes(correlationId.ToString("N")[..32]);
        }

        #endregion
    }
}