using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Factories;
using AhBearStudios.Core.Serialization.Services;
using AhBearStudios.Core.Serialization.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Messaging;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Refactored high-performance serialization service following CLAUDE.md patterns.
    /// Orchestrates serialization operations by delegating to specialized services.
    /// Follows Builder → Config → Factory → Service pattern with proper separation of concerns.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public sealed class SerializationService : ISerializationService
    {
        #region Private Fields

        private readonly SerializationConfig _config;

        // Core infrastructure services
        private readonly ISerializerFactory _serializerFactory;
        private readonly ILoggingService _loggingService;
        private readonly IMessagePublishingService _messagePublisher;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IPoolingService _poolingService;

        // Domain-specific services (new architecture)
        private readonly ISerializationOperationCoordinator _operationCoordinator;

        // Supporting services (optional)
        private readonly IHealthCheckService _healthCheckService;

        private volatile bool _disposed;
        private volatile bool _isEnabled;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SerializationService with specialized service delegation.
        /// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <param name="serializerFactory">Factory for creating serializer instances</param>
        /// <param name="operationCoordinator">Coordinator for complex serialization operations</param>
        /// <param name="loggingService">Logging service for monitoring</param>
        /// <param name="messagePublisher">Message publisher for events</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="poolingService">Pooling service for buffer management</param>
        /// <param name="healthCheckService">Health check service for monitoring</param>
        public SerializationService(
            SerializationConfig config,
            ISerializerFactory serializerFactory,
            ISerializationOperationCoordinator operationCoordinator,
            ILoggingService loggingService,
            IMessagePublishingService messagePublisher,
            IProfilerService profilerService,
            IAlertService alertService,
            IPoolingService poolingService,
            IHealthCheckService healthCheckService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            _operationCoordinator = operationCoordinator ?? throw new ArgumentNullException(nameof(operationCoordinator));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));

            _healthCheckService = healthCheckService;

            _isEnabled = true;

            // Initialize serializers through coordinator (follows Factory → Service pattern)
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SerializationServiceInit", "Bootstrap");
            _operationCoordinator.InitializeSerializers(_serializerFactory, _config, correlationId);

            _loggingService.LogInfo("SerializationService initialized successfully with coordinator delegation",
                new FixedString64Bytes("Serialization.Bootstrap"), "SerializationService");
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public SerializationConfig Configuration => _config;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled && !_disposed;

        #endregion

        #region Core Serialization Methods

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Serialize<T>(T obj, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "Serialize"));

            return _operationCoordinator.CoordinateSerialize(obj, format, finalCorrelationId);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>(byte[] data, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "Deserialize"));

            return _operationCoordinator.CoordinateDeserialize<T>(data, format, finalCorrelationId);
        }

        /// <inheritdoc />
        public bool TrySerialize<T>(T obj, out byte[] result, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            result = null;

            try
            {
                result = Serialize(obj, correlationId, format);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"TrySerialize failed for {typeof(T).Name}: {ex.Message}",
                    correlationId, "SerializationService");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            result = default(T);

            try
            {
                result = Deserialize<T>(data, correlationId, format);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"TryDeserialize failed for {typeof(T).Name}: {ex.Message}",
                    correlationId, "SerializationService");
                return false;
            }
        }

        #endregion

        #region Async Serialization Methods

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "SerializeAsync"));

            return await _operationCoordinator.CoordinateSerializeAsync(obj, format, finalCorrelationId, cancellationToken);
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "DeserializeAsync"));

            return await _operationCoordinator.CoordinateDeserializeAsync<T>(data, format, finalCorrelationId, cancellationToken);
        }

        #endregion

        #region Stream-based Serialization Methods

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            // For stream operations, we serialize to bytes then write to stream
            // This delegates to the coordinator for consistency
            var data = Serialize(obj, correlationId, format);
            stream.Write(data, 0, data.Length);
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            // For stream operations, we read bytes then deserialize
            // This delegates to the coordinator for consistency
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var data = memoryStream.ToArray();

            return Deserialize<T>(data, correlationId, format);
        }

        #endregion

        #region Burst-compatible Methods

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator,
            FixedString64Bytes correlationId = default) where T : unmanaged
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "SerializeToNativeArray"));

            return _operationCoordinator.CoordinateSerializeToNativeArray(obj, allocator, null, finalCorrelationId);
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data,
            FixedString64Bytes correlationId = default) where T : unmanaged
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeof(T).Name)
                : DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "DeserializeFromNativeArray"));

            return _operationCoordinator.CoordinateDeserializeFromNativeArray<T>(data, null, finalCorrelationId);
        }

        #endregion

        #region Batch Operations

        /// <inheritdoc />
        public byte[][] SerializeBatch<T>(IEnumerable<T> objects, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            ThrowIfDisposed();

            var objectList = objects.AsValueEnumerable().ToList();
            var results = new byte[objectList.Count][];

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "SerializeBatch"));

            for (int i = 0; i < objectList.Count; i++)
            {
                results[i] = Serialize(objectList[i], correlationId, format);
            }

            return results;
        }

        /// <inheritdoc />
        public T[] DeserializeBatch<T>(IEnumerable<byte[]> dataArray, FixedString64Bytes correlationId = default,
            SerializationFormat? format = null)
        {
            if (dataArray == null)
                throw new ArgumentNullException(nameof(dataArray));

            ThrowIfDisposed();

            var dataList = dataArray.AsValueEnumerable().ToList();
            var results = new T[dataList.Count];

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "DeserializeBatch"));

            for (int i = 0; i < dataList.Count; i++)
            {
                results[i] = Deserialize<T>(dataList[i], correlationId, format);
            }

            return results;
        }

        #endregion

        #region Serializer Management

        /// <inheritdoc />
        public void RegisterSerializer(SerializationFormat format, ISerializer serializer,
            FixedString64Bytes correlationId = default)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializerRegistration", format.ToString())
                : DeterministicIdGenerator.GenerateCorrelationId("SerializerRegistration", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "RegisterSerializer"));

            _operationCoordinator.RegisterSerializer(format, serializer, finalCorrelationId);

            _loggingService.LogInfo($"Delegated serializer registration for format: {format}",
                correlationId, "SerializationService");
        }

        /// <inheritdoc />
        public bool UnregisterSerializer(SerializationFormat format, FixedString64Bytes correlationId = default)
        {
            ThrowIfDisposed();

            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializerUnregistration", format.ToString())
                : DeterministicIdGenerator.GenerateCorrelationId("SerializerUnregistration", correlationId.ToString());

            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "UnregisterSerializer"));

            var removed = _operationCoordinator.UnregisterSerializer(format, finalCorrelationId);

            _loggingService.LogInfo($"Delegated serializer unregistration for format: {format}, Success: {removed}",
                correlationId, "SerializationService");

            return removed;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SerializationFormat> GetRegisteredFormats()
        {
            return _operationCoordinator.GetRegisteredFormats();
        }

        /// <inheritdoc />
        public ISerializer GetSerializer(SerializationFormat format)
        {
            return _operationCoordinator.GetSerializer(format);
        }

        /// <inheritdoc />
        public bool IsSerializerAvailable(SerializationFormat format)
        {
            return _operationCoordinator.IsFormatAvailable(format);
        }

        #endregion

        #region Circuit Breaker Management

        /// <inheritdoc />
        public ICircuitBreaker GetCircuitBreaker(SerializationFormat format)
        {
            return _operationCoordinator.GetCircuitBreaker(format);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics()
        {
            return _operationCoordinator.GetCircuitBreakerStatistics();
        }

        /// <inheritdoc />
        public void OpenCircuitBreaker(SerializationFormat format, string reason,
            FixedString64Bytes correlationId = default)
        {
            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerOpen", format.ToString())
                : DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerOpen", correlationId.ToString());

            _operationCoordinator.OpenCircuitBreaker(format, reason, finalCorrelationId);

            // Raise critical alert for circuit breaker opening
            _alertService.RaiseAlertAsync(
                $"Serialization circuit breaker opened for {format}: {reason}",
                AlertSeverity.Critical,
                "SerializationService",
                "CircuitBreaker",
                finalCorrelationId).Forget();
        }

        /// <inheritdoc />
        public void CloseCircuitBreaker(SerializationFormat format, string reason,
            FixedString64Bytes correlationId = default)
        {
            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerClose", format.ToString())
                : DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerClose", correlationId.ToString());

            _operationCoordinator.CloseCircuitBreaker(format, reason, finalCorrelationId);

            // Raise informational alert for circuit breaker closing
            _alertService.RaiseAlertAsync(
                $"Serialization circuit breaker closed for {format}: {reason}",
                AlertSeverity.Info,
                "SerializationService",
                "CircuitBreaker",
                finalCorrelationId).Forget();
        }

        /// <inheritdoc />
        public void ResetAllCircuitBreakers(FixedString64Bytes correlationId = default)
        {
            var finalCorrelationId = correlationId.IsEmpty
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerReset", "AllFormats")
                : DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerReset", correlationId.ToString());

            _operationCoordinator.ResetAllCircuitBreakers(finalCorrelationId);
        }

        #endregion

        #region Type Registration

        /// <inheritdoc />
        public void RegisterType<T>(FixedString64Bytes correlationId = default)
        {
            RegisterType(typeof(T), correlationId);
        }

        /// <inheritdoc />
        public void RegisterType(Type type, FixedString64Bytes correlationId = default)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            // Register with all available serializers through coordinator
            var registeredFormats = _operationCoordinator.GetRegisteredFormats();
            foreach (var format in registeredFormats)
            {
                try
                {
                    var serializer = _operationCoordinator.GetSerializer(format);
                    serializer?.RegisterType(type);
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to register type {type.Name} with {format} serializer: {ex.Message}",
                        correlationId, "SerializationService");
                }
            }

            _loggingService.LogDebug($"Registered type: {type.Name}", correlationId, "SerializationService");
        }

        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Check if any serializer has this type registered
            var registeredFormats = _operationCoordinator.GetRegisteredFormats();
            foreach (var format in registeredFormats)
            {
                try
                {
                    var serializer = _operationCoordinator.GetSerializer(format);
                    if (serializer?.IsRegistered(type) == true)
                        return true;
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to check type registration for {format}: {ex.Message}",
                        default(FixedString64Bytes), "SerializationService");
                }
            }

            return false;
        }

        #endregion

        #region Format Detection and Negotiation

        /// <inheritdoc />
        public SerializationFormat? DetectFormat(byte[] data)
        {
            return _operationCoordinator.DetectFormat(data);
        }

        /// <inheritdoc />
        public SerializationFormat GetBestFormat<T>(SerializationFormat? preferredFormat = null)
        {
            return _operationCoordinator.DetermineBestFormat<T>(preferredFormat);
        }

        /// <inheritdoc />
        public IReadOnlyList<SerializationFormat> GetFallbackChain(SerializationFormat primaryFormat)
        {
            return _operationCoordinator.GetFallbackChain(primaryFormat);
        }

        #endregion

        #region Performance and Monitoring

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            // Get metrics from ProfilerService
            var profilerStats = _profilerService.GetStatistics();

            // Extract serialization-specific metrics and build statistics
            // This is a simplified implementation - in practice, you'd extract specific metrics
            return new SerializationStatistics
            {
                TotalSerializations = GetCounterValue(profilerStats, "serialization.operations.success"),
                FailedOperations = GetCounterValue(profilerStats, "serialization.operations.failed"),
                TotalBytesProcessed = GetMetricSum(profilerStats, "serialization.bytes_processed"),
                RegisteredTypeCount = _operationCoordinator.GetRegisteredFormats().Count,
                LastResetTime = DateTime.UtcNow
            };
        }

        /// <inheritdoc />
        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService.BeginScope(
                ProfilerTag.CreateMethodTag("SerializationService", "FlushAsync"));

            // Flush profiler service
            _profilerService.Flush();

            // Flush any other services that need it
            var registeredFormats = _operationCoordinator.GetRegisteredFormats();
            foreach (var format in registeredFormats)
            {
                try
                {
                    var serializer = _operationCoordinator.GetSerializer(format);
                    // Note: ISerializer doesn't have FlushAsync, so we would need to add it
                    // For now, we'll just get statistics as a form of flush
                    serializer?.GetStatistics();
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to flush {format} serializer: {ex.Message}",
                        correlationId, "SerializationService");
                }
            }

            await UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            // Validate configuration
            if (!_config.IsValid())
            {
                errors.Add(new ValidationError("Invalid serialization configuration", "Configuration"));
            }

            // Validate serializers
            var registeredFormats = _operationCoordinator.GetRegisteredFormats();
            if (registeredFormats.Count == 0)
            {
                warnings.Add(new ValidationWarning("No serializers registered", "Serializers"));
            }

            var formatHealth = _operationCoordinator.GetFormatHealth();
            foreach (var kvp in formatHealth)
            {
                if (!kvp.Value)
                {
                    warnings.Add(new ValidationWarning($"Serializer '{kvp.Key}' is not available", $"Serializer.{kvp.Key}"));
                }
            }

            return errors.Count == 0
                ? ValidationResult.Success("SerializationService", warnings)
                : ValidationResult.Failure(errors, "SerializationService", warnings);
        }

        /// <inheritdoc />
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            try
            {
                // Perform health checks on all components using existing services
                var formatHealth = _operationCoordinator.GetFormatHealth();
                var unhealthyFormats = 0;

                foreach (var kvp in formatHealth)
                {
                    if (!kvp.Value)
                    {
                        unhealthyFormats++;
                        _loggingService.LogWarning($"Format {kvp.Key} is not healthy",
                            correlationId, "SerializationService");
                    }
                }

                // Alert if too many formats are unhealthy
                if (unhealthyFormats >= formatHealth.Count)
                {
                    _alertService.RaiseAlertAsync(
                        $"All serialization formats are unhealthy ({unhealthyFormats}/{formatHealth.Count})",
                        AlertSeverity.Critical,
                        "SerializationService",
                        "HealthCheck",
                        correlationId.IsEmpty ? Guid.NewGuid() : DeterministicIdGenerator.GenerateCorrelationId("SerializationMaintenance", correlationId.ToString())).Forget();
                }
                else if (unhealthyFormats > formatHealth.Count / 2)
                {
                    _alertService.RaiseAlertAsync(
                        $"Most serialization formats are unhealthy ({unhealthyFormats}/{formatHealth.Count})",
                        AlertSeverity.Warning,
                        "SerializationService",
                        "HealthCheck",
                        correlationId.IsEmpty ? Guid.NewGuid() : DeterministicIdGenerator.GenerateCorrelationId("SerializationMaintenance", correlationId.ToString())).Forget();
                }

                _loggingService.LogInfo("Serialization service maintenance completed",
                    correlationId, "SerializationService");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Maintenance operation failed: {ex.Message}",
                    correlationId, "SerializationService");

                // Alert on maintenance failure
                _alertService.RaiseAlertAsync(
                    $"Serialization service maintenance failed: {ex.Message}",
                    AlertSeverity.Critical,
                    "SerializationService",
                    "Maintenance",
                    correlationId.IsEmpty ? Guid.NewGuid() : DeterministicIdGenerator.GenerateCorrelationId("SerializationMaintenance", correlationId.ToString())).Forget();
            }
        }

        /// <inheritdoc />
        public bool PerformHealthCheck()
        {
            if (_disposed) return false;

            var formatHealth = _operationCoordinator.GetFormatHealth();
            return formatHealth.Values.AsValueEnumerable().Any(healthy => healthy);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            var status = new Dictionary<string, bool>();

            var formatHealth = _operationCoordinator.GetFormatHealth();
            foreach (var kvp in formatHealth)
            {
                status[$"Serializer_{kvp.Key}"] = kvp.Value;
            }

            status["OperationCoordinator"] = !_disposed;
            status["MessagePublisher"] = true; // Assume healthy if not disposed

            return status;
        }

        #endregion

        #region Configuration Management

        /// <inheritdoc />
        public void UpdateConfiguration(SerializationConfig newConfig, FixedString64Bytes correlationId = default)
        {
            if (newConfig == null)
                throw new ArgumentNullException(nameof(newConfig));

            ThrowIfDisposed();

            // Note: Since _config is readonly, we would need to make it mutable or recreate the service
            // For now, log the intention
            _loggingService.LogInfo("Configuration update requested - requires service restart",
                correlationId, "SerializationService");
        }

        /// <inheritdoc />
        public void SetEnabled(bool enabled, FixedString64Bytes correlationId = default)
        {
            _isEnabled = enabled;

            _loggingService.LogInfo($"SerializationService enabled state set to {enabled}",
                correlationId, "SerializationService");
        }

        #endregion

        #region Private Helper Methods

        private long GetCounterValue(IReadOnlyDictionary<string, object> stats, string counterName)
        {
            try
            {
                if (stats != null && stats.TryGetValue(counterName, out var value))
                {
                    if (value is long longValue)
                        return longValue;
                    if (value is int intValue)
                        return intValue;
                    if (value is double doubleValue)
                        return (long)doubleValue;
                    if (value is float floatValue)
                        return (long)floatValue;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private long GetMetricSum(IReadOnlyDictionary<string, object> stats, string metricName)
        {
            try
            {
                if (stats != null && stats.TryGetValue(metricName, out var value))
                {
                    if (value is long longValue)
                        return longValue;
                    if (value is int intValue)
                        return intValue;
                    if (value is double doubleValue)
                        return (long)doubleValue;
                    if (value is float floatValue)
                        return (long)floatValue;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SerializationService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the serialization service and all registered serializers and circuit breakers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _isEnabled = false;

            try
            {
                // Dispose operation coordinator (handles all serializers and circuit breakers)
                _operationCoordinator?.Dispose();

                _loggingService?.LogInfo("SerializationService disposed successfully",
                    default(FixedString64Bytes), "SerializationService");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error during SerializationService disposal: {ex.Message}",
                    default(FixedString64Bytes), "SerializationService");
            }
        }

        #endregion
    }
}