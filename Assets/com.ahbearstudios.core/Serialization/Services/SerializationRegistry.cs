using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Services;

/// <summary>
    /// Service for managing type registration in serialization system.
    /// Provides thread-safe registration and metadata management.
    /// </summary>
    public class SerializationRegistry : ISerializationRegistry
    {
        private readonly ILoggingService _logger;
        private readonly ConcurrentDictionary<Type, TypeDescriptor> _registeredTypes;
        private readonly object _registrationLock = new();

        /// <summary>
        /// Initializes a new instance of SerializationRegistry.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public SerializationRegistry(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredTypes = new ConcurrentDictionary<Type, TypeDescriptor>();

            var correlationId = GetCorrelationId();
            _logger.LogInfo("SerializationRegistry initialized", correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var descriptor = new TypeDescriptor
            {
                Type = type,
                TypeName = type.FullName ?? type.Name,
                Version = 1,
                IsRegistered = true,
                SupportsVersioning = false,
                EstimatedSize = EstimateTypeSize(type)
            };

            RegisterType(type, descriptor);
        }

        /// <inheritdoc />
        public void RegisterType(Type type, TypeDescriptor descriptor)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            lock (_registrationLock)
            {
                var correlationId = GetCorrelationId();

                if (_registeredTypes.TryAdd(type, descriptor))
                {
                    _logger.LogInfo($"Registered type {type.FullName} with version {descriptor.Version}", correlationId, sourceContext: null, properties: null);
                }
                else
                {
                    _registeredTypes[type] = descriptor;
                    _logger.LogInfo($"Updated registration for type {type.FullName}", correlationId, sourceContext: null, properties: null);
                }
            }
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return _registeredTypes.ContainsKey(type);
        }

        /// <inheritdoc />
        public TypeDescriptor GetTypeDescriptor(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return _registeredTypes.TryGetValue(type, out var descriptor) ? descriptor : null;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<Type> GetRegisteredTypes()
        {
            return _registeredTypes.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public bool UnregisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var correlationId = GetCorrelationId();
            var result = _registeredTypes.TryRemove(type, out _);

            if (result)
            {
                _logger.LogInfo($"Unregistered type {type.FullName}", correlationId, sourceContext: null, properties: null);
            }

            return result;
        }

        /// <inheritdoc />
        public void Clear()
        {
            var correlationId = GetCorrelationId();
            var count = _registeredTypes.Count;
            
            _registeredTypes.Clear();
            
            _logger.LogInfo($"Cleared {count} registered types", correlationId, sourceContext: null, properties: null);
        }

        private int EstimateTypeSize(Type type)
        {
            // Basic size estimation - can be enhanced with more sophisticated analysis
            if (type.IsPrimitive)
                return System.Runtime.InteropServices.Marshal.SizeOf(type);
            
            if (type == typeof(string))
                return 50; // Average string size estimate
            
            if (type.IsArray || type.IsGenericType)
                return 100; // Collection estimate
            
            return 64; // Default object estimate
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }