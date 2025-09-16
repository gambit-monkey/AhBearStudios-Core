using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Messages;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Utilities;
using ZLinq;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Coordinates serialization operations with fallback support and circuit breaker integration.
    /// Follows CLAUDE.md guidelines for separation of concerns and performance.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public sealed class SerializationOperationCoordinator : ISerializationOperationCoordinator
    {
        #region Private Fields

        private readonly SerializationConfig _config;
        private readonly ConcurrentDictionary<SerializationFormat, ISerializer> _serializers;
        private readonly ConcurrentDictionary<SerializationFormat, CircuitBreaker> _circuitBreakers;
        private readonly ILoggingService _loggingService;
        private readonly IMessagePublishingService _messagePublisher;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;

        private readonly SemaphoreSlim _concurrencyLimiter;
        private volatile bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SerializationOperationCoordinator.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <param name="serializers">Dictionary of registered serializers</param>
        /// <param name="circuitBreakers">Dictionary of circuit breakers</param>
        /// <param name="loggingService">Logging service for operation tracking</param>
        /// <param name="messagePublisher">Message publisher for events</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="poolingService">Pooling service for buffer management</param>
        public SerializationOperationCoordinator(
            SerializationConfig config,
            ConcurrentDictionary<SerializationFormat, ISerializer> serializers,
            ConcurrentDictionary<SerializationFormat, CircuitBreaker> circuitBreakers,
            ILoggingService loggingService,
            IMessagePublishingService messagePublisher,
            IProfilerService profilerService,
            IPoolingService poolingService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializers = serializers ?? throw new ArgumentNullException(nameof(serializers));
            _circuitBreakers = circuitBreakers ?? throw new ArgumentNullException(nameof(circuitBreakers));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));

            _concurrencyLimiter = new SemaphoreSlim(_config.MaxConcurrentOperations, _config.MaxConcurrentOperations);
        }

        #endregion

        #region Synchronous Operations

        /// <inheritdoc />
        public byte[] CoordinateSerialize<T>(T obj, SerializationFormat? preferredFormat = null,
            Guid correlationId = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            var selectedFormat = DetermineBestFormat<T>(preferredFormat);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : correlationId;

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateSerialize"));

            // Publish operation started message
            PublishOperationStarted(selectedFormat, typeof(T).Name, "Serialize", false, finalCorrelationId);

            var startTime = DateTime.UtcNow;
            try
            {
                var result = ExecuteWithFallback(selectedFormat, finalCorrelationId,
                    serializer => serializer.Serialize(obj));

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Record metrics
                _profilerService.RecordMetric("serialization.bytes_processed", result.Length, "bytes",
                    new Dictionary<string, string>
                    {
                        ["format"] = selectedFormat.ToString(),
                        ["type"] = typeof(T).Name,
                        ["operation"] = "serialize"
                    });

                _profilerService.IncrementCounter("serialization.operations.success",
                    tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                // Publish operation completed message
                PublishOperationCompleted(selectedFormat, typeof(T).Name, "Serialize", result.Length, duration, false, finalCorrelationId);

                return result;
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _profilerService.IncrementCounter("serialization.operations.failed",
                    tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                // Publish operation failed message
                PublishOperationFailed(selectedFormat, typeof(T).Name, "Serialize", ex, false, true, finalCorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public T CoordinateDeserialize<T>(byte[] data, SerializationFormat? preferredFormat = null,
            Guid correlationId = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            var selectedFormat = preferredFormat ?? DetectFormat(data) ?? DetermineBestFormat<T>();
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : correlationId;

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateDeserialize"));

            // Publish operation started message
            PublishOperationStarted(selectedFormat, typeof(T).Name, "Deserialize", false, finalCorrelationId);

            var startTime = DateTime.UtcNow;
            try
            {
                var result = ExecuteWithFallback(selectedFormat, finalCorrelationId,
                    serializer => serializer.Deserialize<T>(data));

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Record metrics
                _profilerService.RecordMetric("serialization.bytes_processed", data.Length, "bytes",
                    new Dictionary<string, string>
                    {
                        ["format"] = selectedFormat.ToString(),
                        ["type"] = typeof(T).Name,
                        ["operation"] = "deserialize"
                    });

                _profilerService.IncrementCounter("serialization.operations.success",
                    tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                // Publish operation completed message
                PublishOperationCompleted(selectedFormat, typeof(T).Name, "Deserialize", data.Length, duration, false, finalCorrelationId);

                return result;
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _profilerService.IncrementCounter("serialization.operations.failed",
                    tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                // Publish operation failed message
                PublishOperationFailed(selectedFormat, typeof(T).Name, "Deserialize", ex, false, true, finalCorrelationId);

                throw;
            }
        }

        #endregion

        #region Asynchronous Operations

        /// <inheritdoc />
        public async UniTask<byte[]> CoordinateSerializeAsync<T>(T obj, SerializationFormat? preferredFormat = null,
            Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                var selectedFormat = DetermineBestFormat<T>(preferredFormat);
                var finalCorrelationId = correlationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                    : correlationId;

                using var scope = _profilerService.BeginScope(
                    ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateSerializeAsync"));

                // Publish operation started message
                PublishOperationStarted(selectedFormat, typeof(T).Name, "Serialize", true, finalCorrelationId);

                var startTime = DateTime.UtcNow;
                try
                {
                    var result = await ExecuteWithFallbackAsync(selectedFormat, finalCorrelationId, cancellationToken,
                        async serializer => await serializer.SerializeAsync(obj, cancellationToken));

                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record metrics
                    _profilerService.RecordMetric("serialization.bytes_processed", result.Length, "bytes",
                        new Dictionary<string, string>
                        {
                            ["format"] = selectedFormat.ToString(),
                            ["type"] = typeof(T).Name,
                            ["operation"] = "serialize_async"
                        });

                    _profilerService.IncrementCounter("serialization.operations.success",
                        tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                    // Publish operation completed message
                    PublishOperationCompleted(selectedFormat, typeof(T).Name, "Serialize", result.Length, duration, true, finalCorrelationId);

                    return result;
                }
                catch (Exception ex)
                {
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    _profilerService.IncrementCounter("serialization.operations.failed",
                        tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                    // Publish operation failed message
                    PublishOperationFailed(selectedFormat, typeof(T).Name, "Serialize", ex, true, true, finalCorrelationId);

                    throw;
                }
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public async UniTask<T> CoordinateDeserializeAsync<T>(byte[] data, SerializationFormat? preferredFormat = null,
            Guid correlationId = default, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                var selectedFormat = preferredFormat ?? DetectFormat(data) ?? DetermineBestFormat<T>();
                var finalCorrelationId = correlationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                    : correlationId;

                using var scope = _profilerService.BeginScope(
                    ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateDeserializeAsync"));

                // Publish operation started message
                PublishOperationStarted(selectedFormat, typeof(T).Name, "Deserialize", true, finalCorrelationId);

                var startTime = DateTime.UtcNow;
                try
                {
                    var result = await ExecuteWithFallbackAsync(selectedFormat, finalCorrelationId, cancellationToken,
                        async serializer => await serializer.DeserializeAsync<T>(data, cancellationToken));

                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record metrics
                    _profilerService.RecordMetric("serialization.bytes_processed", data.Length, "bytes",
                        new Dictionary<string, string>
                        {
                            ["format"] = selectedFormat.ToString(),
                            ["type"] = typeof(T).Name,
                            ["operation"] = "deserialize_async"
                        });

                    _profilerService.IncrementCounter("serialization.operations.success",
                        tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                    // Publish operation completed message
                    PublishOperationCompleted(selectedFormat, typeof(T).Name, "Deserialize", data.Length, duration, true, finalCorrelationId);

                    return result;
                }
                catch (Exception ex)
                {
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    _profilerService.IncrementCounter("serialization.operations.failed",
                        tags: new Dictionary<string, string> { ["format"] = selectedFormat.ToString() });

                    // Publish operation failed message
                    PublishOperationFailed(selectedFormat, typeof(T).Name, "Deserialize", ex, true, true, finalCorrelationId);

                    throw;
                }
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        #endregion

        #region Burst-Compatible Operations

        /// <inheritdoc />
        public NativeArray<byte> CoordinateSerializeToNativeArray<T>(T obj, Allocator allocator,
            SerializationFormat? preferredFormat = null, Guid correlationId = default) where T : unmanaged
        {
            if (obj.Equals(default(T)))
                throw new ArgumentException("Object cannot be default value", nameof(obj));

            ThrowIfDisposed();

            var selectedFormat = DetermineBestFormat<T>(preferredFormat);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : correlationId;

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateSerializeToNativeArray"));

            return ExecuteWithFallback(selectedFormat, finalCorrelationId,
                serializer => serializer.SerializeToNativeArray(obj, allocator));
        }

        /// <inheritdoc />
        public T CoordinateDeserializeFromNativeArray<T>(NativeArray<byte> data,
            SerializationFormat? preferredFormat = null, Guid correlationId = default) where T : unmanaged
        {
            if (!data.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(data));

            ThrowIfDisposed();

            var selectedFormat = DetermineBestFormat<T>(preferredFormat);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : correlationId;

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationOperationCoordinator", "CoordinateDeserializeFromNativeArray"));

            return ExecuteWithFallback(selectedFormat, finalCorrelationId,
                serializer => serializer.DeserializeFromNativeArray<T>(data));
        }

        #endregion

        #region Format Management

        /// <inheritdoc />
        public SerializationFormat DetermineBestFormat<T>(SerializationFormat? preferredFormat = null)
        {
            // If preferred format is specified and available, use it
            if (preferredFormat.HasValue && IsFormatAvailable(preferredFormat.Value))
            {
                return preferredFormat.Value;
            }

            // Use configuration default if available
            if (IsFormatAvailable(_config.Format))
            {
                return _config.Format;
            }

            // Fallback chain
            var fallbackChain = GetFallbackChain(_config.Format);
            foreach (var format in fallbackChain)
            {
                if (IsFormatAvailable(format))
                {
                    return format;
                }
            }

            throw new SerializationException("No available serializers for the requested operation");
        }

        /// <inheritdoc />
        public SerializationFormat? DetectFormat(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            // Try to detect format based on data characteristics
            if (data.Length >= 1)
            {
                // JSON typically starts with '{' or '['
                if (data[0] == '{' || data[0] == '[')
                    return SerializationFormat.Json;

                // MemoryPack has specific header patterns
                if (data.Length >= 4)
                {
                    // Add MemoryPack detection logic here
                    // This is a simplified check - actual implementation would be more sophisticated
                    if (data[0] == 0x9A || data[0] == 0x9B) // Example MemoryPack markers
                        return SerializationFormat.MemoryPack;
                }

                // Binary serialization fallback
                return SerializationFormat.Binary;
            }

            return null;
        }

        /// <inheritdoc />
        public SerializationFormat[] GetFallbackChain(SerializationFormat primaryFormat)
        {
            var chain = new List<SerializationFormat> { primaryFormat };

            // Define fallback hierarchy
            switch (primaryFormat)
            {
                case SerializationFormat.MemoryPack:
                    chain.Add(SerializationFormat.Binary);
                    chain.Add(SerializationFormat.Json);
                    break;
                case SerializationFormat.Binary:
                    chain.Add(SerializationFormat.Json);
                    chain.Add(SerializationFormat.MemoryPack);
                    break;
                case SerializationFormat.Json:
                    chain.Add(SerializationFormat.Binary);
                    chain.Add(SerializationFormat.MemoryPack);
                    break;
                default:
                    chain.Add(SerializationFormat.Json);
                    chain.Add(SerializationFormat.Binary);
                    break;
            }

            return chain.ToArray();
        }

        #endregion

        #region Health and Status

        /// <inheritdoc />
        public bool IsFormatAvailable(SerializationFormat format)
        {
            return _serializers.ContainsKey(format) &&
                   _circuitBreakers.TryGetValue(format, out var breaker) &&
                   breaker.AllowsRequests();
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<SerializationFormat, bool> GetFormatHealth()
        {
            var health = new Dictionary<SerializationFormat, bool>();

            foreach (var format in _serializers.Keys.AsValueEnumerable())
            {
                health[format] = IsFormatAvailable(format);
            }

            return health;
        }

        #endregion

        #region Serializer Management

        /// <inheritdoc />
        public void RegisterSerializer(SerializationFormat format, ISerializer serializer, Guid correlationId = default)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializerRegistration", format.ToString())
                : correlationId;

            _serializers.AddOrUpdate(format, serializer, (key, oldValue) => serializer);

            _loggingService.LogInfo($"Serializer registered for format {format}",
                new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
        }

        /// <inheritdoc />
        public bool UnregisterSerializer(SerializationFormat format, Guid correlationId = default)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializerUnregistration", format.ToString())
                : correlationId;

            var success = _serializers.TryRemove(format, out _);

            if (success)
            {
                _loggingService.LogInfo($"Serializer unregistered for format {format}",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
            }
            else
            {
                _loggingService.LogWarning($"Failed to unregister serializer for format {format} - not found",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
            }

            return success;
        }

        /// <inheritdoc />
        public ISerializer GetSerializer(SerializationFormat format)
        {
            ThrowIfDisposed();
            return _serializers.TryGetValue(format, out var serializer) ? serializer : null;
        }

        /// <inheritdoc />
        public System.Collections.Generic.IReadOnlyCollection<SerializationFormat> GetRegisteredFormats()
        {
            ThrowIfDisposed();
            return _serializers.Keys.AsValueEnumerable().ToArray();
        }

        /// <inheritdoc />
        public void InitializeSerializers(Factories.ISerializerFactory factory, Configs.SerializationConfig config, Guid correlationId = default)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializerInitialization", "AllFormats")
                : correlationId;

            try
            {
                // Clear existing serializers
                _serializers.Clear();

                // Initialize serializers for all configured formats
                foreach (SerializationFormat format in Enum.GetValues(typeof(SerializationFormat)))
                {
                    try
                    {
                        var serializer = factory.CreateSerializer(format);
                        if (serializer != null)
                        {
                            _serializers.TryAdd(format, serializer);
                            _loggingService.LogDebug($"Initialized serializer for format {format}",
                                new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Failed to initialize serializer for format {format}: {ex.Message}",
                            new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                    }
                }

                _loggingService.LogInfo($"Serializer initialization completed. {_serializers.Count} serializers registered.",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Serializer initialization failed: {ex.Message}",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                throw;
            }
        }

        #endregion

        #region Circuit Breaker Management

        /// <inheritdoc />
        public CircuitBreaker GetCircuitBreaker(SerializationFormat format)
        {
            ThrowIfDisposed();
            return _circuitBreakers.TryGetValue(format, out var circuitBreaker) ? circuitBreaker : null;
        }

        /// <inheritdoc />
        public void OpenCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerOperation", format.ToString())
                : correlationId;

            if (_circuitBreakers.TryGetValue(format, out var circuitBreaker))
            {
                try
                {
                    circuitBreaker.Open(reason);
                    _loggingService.LogWarning($"Circuit breaker opened for format {format}: {reason}",
                        new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to open circuit breaker for format {format}: {ex.Message}",
                        new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                    throw;
                }
            }
            else
            {
                _loggingService.LogWarning($"Circuit breaker not found for format {format}",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
            }
        }

        /// <inheritdoc />
        public void CloseCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerOperation", format.ToString())
                : correlationId;

            if (_circuitBreakers.TryGetValue(format, out var circuitBreaker))
            {
                try
                {
                    circuitBreaker.Close(reason);
                    _loggingService.LogInfo($"Circuit breaker closed for format {format}: {reason}",
                        new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to close circuit breaker for format {format}: {ex.Message}",
                        new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                    throw;
                }
            }
            else
            {
                _loggingService.LogWarning($"Circuit breaker not found for format {format}",
                    new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
            }
        }

        /// <inheritdoc />
        public void ResetAllCircuitBreakers(Guid correlationId = default)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerReset", "AllFormats")
                : correlationId;

            var resetCount = 0;
            foreach (var kvp in _circuitBreakers)
            {
                try
                {
                    kvp.Value.Reset("Manual reset requested");
                    resetCount++;
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to reset circuit breaker for format {kvp.Key}: {ex.Message}",
                        new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
                }
            }

            _loggingService.LogInfo($"Reset {resetCount} circuit breakers",
                new FixedString64Bytes(finalCorrelationId.ToString()), "SerializationOperationCoordinator");
        }

        /// <inheritdoc />
        public System.Collections.Generic.IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics()
        {
            ThrowIfDisposed();

            var statistics = new Dictionary<SerializationFormat, CircuitBreakerStatistics>();

            foreach (var kvp in _circuitBreakers)
            {
                try
                {
                    statistics[kvp.Key] = kvp.Value.GetStatistics();
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to get statistics for circuit breaker {kvp.Key}: {ex.Message}",
                        new FixedString64Bytes(Guid.NewGuid().ToString()), "SerializationOperationCoordinator");
                }
            }

            return statistics;
        }

        #endregion

        #region Private Helper Methods

        private TResult ExecuteWithFallback<TResult>(SerializationFormat primaryFormat, Guid correlationId,
            Func<ISerializer, TResult> operation)
        {
            var fallbackChain = GetFallbackChain(primaryFormat);
            Exception lastException = null;

            foreach (var format in fallbackChain)
            {
                if (!_serializers.TryGetValue(format, out var serializer))
                    continue;

                if (!_circuitBreakers.TryGetValue(format, out var circuitBreaker))
                    continue;

                try
                {
                    return circuitBreaker.ExecuteAsync(async _ =>
                    {
                        return await UniTask.FromResult(operation(serializer));
                    }).GetAwaiter().GetResult();
                }
                catch (CircuitBreakerOpenException)
                {
                    _loggingService.LogWarning($"Circuit breaker open for {format}, trying next fallback",
                        new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    circuitBreaker.RecordFailure(ex);
                    _loggingService.LogWarning($"Operation failed with {format}: {ex.Message}",
                        new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
                }
            }

            // All fallbacks failed
            var finalException = new SerializationException(
                $"All serializers failed. Last error: {lastException?.Message}",
                typeof(TResult),
                "ExecuteWithFallback",
                lastException);

            throw finalException;
        }

        private async UniTask<TResult> ExecuteWithFallbackAsync<TResult>(SerializationFormat primaryFormat,
            Guid correlationId, CancellationToken cancellationToken, Func<ISerializer, UniTask<TResult>> operation)
        {
            var fallbackChain = GetFallbackChain(primaryFormat);
            Exception lastException = null;

            foreach (var format in fallbackChain)
            {
                if (!_serializers.TryGetValue(format, out var serializer))
                    continue;

                if (!_circuitBreakers.TryGetValue(format, out var circuitBreaker))
                    continue;

                try
                {
                    return await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        return await operation(serializer);
                    }, cancellationToken);
                }
                catch (CircuitBreakerOpenException)
                {
                    _loggingService.LogWarning($"Circuit breaker open for {format}, trying next fallback",
                        new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    circuitBreaker.RecordFailure(ex);
                    _loggingService.LogWarning($"Async operation failed with {format}: {ex.Message}",
                        new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
                }
            }

            // All fallbacks failed
            var finalException = new SerializationException(
                $"All serializers failed. Last error: {lastException?.Message}",
                typeof(TResult),
                "ExecuteWithFallbackAsync",
                lastException);

            throw finalException;
        }

        private void PublishOperationStarted(SerializationFormat format, string typeName, string operationType,
            bool isAsync, Guid correlationId)
        {
            try
            {
                var message = SerializationOperationStartedMessage.Create(
                    format: format,
                    typeName: typeName,
                    operationType: operationType,
                    isAsync: isAsync,
                    source: "SerializationOperationCoordinator",
                    correlationId: correlationId);

                _messagePublisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Failed to publish operation started message: {ex.Message}",
                    new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
            }
        }

        private void PublishOperationCompleted(SerializationFormat format, string typeName, string operationType,
            long bytesProcessed, double durationMs, bool isAsync, Guid correlationId)
        {
            try
            {
                var message = SerializationOperationCompletedMessage.Create(
                    format: format,
                    typeName: typeName,
                    operationType: operationType,
                    bytesProcessed: bytesProcessed,
                    durationMs: durationMs,
                    isAsync: isAsync,
                    source: "SerializationOperationCoordinator",
                    correlationId: correlationId);

                _messagePublisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Failed to publish operation completed message: {ex.Message}",
                    new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
            }
        }

        private void PublishOperationFailed(SerializationFormat format, string typeName, string operationType,
            Exception exception, bool isAsync, bool fallbackAttempted, Guid correlationId)
        {
            try
            {
                var message = SerializationOperationFailedMessage.Create(
                    format: format,
                    typeName: typeName,
                    operationType: operationType,
                    exception: exception,
                    isAsync: isAsync,
                    fallbackAttempted: fallbackAttempted,
                    source: "SerializationOperationCoordinator",
                    correlationId: correlationId);

                _messagePublisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Failed to publish operation failed message: {ex.Message}",
                    new FixedString64Bytes(correlationId.ToString()), "SerializationOperationCoordinator");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SerializationOperationCoordinator));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the coordination service and its resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _concurrencyLimiter?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}