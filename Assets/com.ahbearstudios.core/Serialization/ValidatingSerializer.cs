using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using AhBearStudios.Core.Common.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Decorator serializer that adds comprehensive type validation to any ISerializer implementation.
    /// Provides security through type whitelisting/blacklisting, assembly validation, and custom validation rules.
    /// </summary>
    public class ValidatingSerializer : ISerializer, IDisposable
    {
        private readonly ISerializer _innerSerializer;
        private readonly ILoggingService _logger;
        private readonly SerializationConfig _config;
        private readonly ValidationRuleEngine _validationEngine;
        private readonly ConcurrentDictionary<Type, ValidationResult> _typeValidationCache;
        private readonly ValidationStatistics _statistics;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of ValidatingSerializer.
        /// </summary>
        /// <param name="innerSerializer">The serializer to wrap with validation</param>
        /// <param name="config">Serialization configuration containing validation rules</param>
        /// <param name="logger">Logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public ValidatingSerializer(
            ISerializer innerSerializer,
            SerializationConfig config,
            ILoggingService logger)
        {
            _innerSerializer = innerSerializer ?? throw new ArgumentNullException(nameof(innerSerializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _validationEngine = new ValidationRuleEngine(_config, _logger);
            _typeValidationCache = new ConcurrentDictionary<Type, ValidationResult>();
            _statistics = new ValidationStatistics();

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"ValidatingSerializer initialized wrapping {innerSerializer.GetType().Name}", correlationId: correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForSerialization(type, obj);

            try
            {
                var result = _innerSerializer.Serialize(obj);
                _statistics.RecordValidationSuccess(type, "Serialize");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "Serialize", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForDeserialization(type);

            try
            {
                var result = _innerSerializer.Deserialize<T>(data);
                ValidateDeserializedObject(result);
                _statistics.RecordValidationSuccess(type, "Deserialize");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "Deserialize", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForDeserialization(type);

            try
            {
                var result = _innerSerializer.Deserialize<T>(data);
                ValidateDeserializedObject(result);
                _statistics.RecordValidationSuccess(type, "DeserializeSpan");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "DeserializeSpan", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            ThrowIfDisposed();

            result = default;
            var type = typeof(T);

            try
            {
                // Check type validation first
                var validationResult = ValidateTypeForDeserialization(type, throwOnFailure: false);
                if (!validationResult.IsValid)
                {
                    _statistics.RecordValidationFailure(type, "TryDeserialize", new ValidationException(validationResult.Summary));
                    return false;
                }

                if (_innerSerializer.TryDeserialize(data, out result))
                {
                    ValidateDeserializedObject(result, throwOnFailure: false);
                    _statistics.RecordValidationSuccess(type, "TryDeserialize");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "TryDeserialize", ex);
                var correlationId = GetCorrelationId();
                _logger.LogError($"TryDeserialize validation failed for type {type.Name}: {ex.Message}", correlationId: correlationId, sourceContext: null, properties: null);
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            return TryDeserialize(data.ToArray(), out result);
        }

        /// <inheritdoc />
        public void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                // Validate the type before registering
                var validationResult = _validationEngine.ValidateType(type);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException($"Cannot register type {type.FullName}: {validationResult.Summary}");
                }

                _innerSerializer.RegisterType(type);
                _statistics.RecordTypeRegistration(type, true);

                _logger.LogInfo($"Successfully registered and validated type {type.FullName}", correlationId: correlationId, sourceContext: null, properties: null);
            }
            catch (Exception ex)
            {
                _statistics.RecordTypeRegistration(type, false);
                _logger.LogException($"Failed to register type {type.FullName}", ex, correlationId: correlationId, sourceContext: null, properties: null);
                throw;
            }
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

            return _innerSerializer.IsRegistered(type);
        }

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForSerialization(type, obj);

            try
            {
                var result = await _innerSerializer.SerializeAsync(obj, cancellationToken);
                _statistics.RecordValidationSuccess(type, "SerializeAsync");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "SerializeAsync", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForDeserialization(type);

            try
            {
                var result = await _innerSerializer.DeserializeAsync<T>(data, cancellationToken);
                ValidateDeserializedObject(result);
                _statistics.RecordValidationSuccess(type, "DeserializeAsync");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "DeserializeAsync", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForSerialization(type, obj);

            try
            {
                _innerSerializer.SerializeToStream(obj, stream);
                _statistics.RecordValidationSuccess(type, "SerializeToStream");
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "SerializeToStream", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForDeserialization(type);

            try
            {
                var result = _innerSerializer.DeserializeFromStream<T>(stream);
                ValidateDeserializedObject(result);
                _statistics.RecordValidationSuccess(type, "DeserializeFromStream");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "DeserializeFromStream", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForSerialization(type, obj);

            try
            {
                var result = _innerSerializer.SerializeToNativeArray(obj, allocator);
                _statistics.RecordValidationSuccess(type, "SerializeToNativeArray");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "SerializeToNativeArray", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            ThrowIfDisposed();

            var type = typeof(T);
            ValidateTypeForDeserialization(type);

            try
            {
                var result = _innerSerializer.DeserializeFromNativeArray<T>(data);
                ValidateDeserializedObject(result);
                _statistics.RecordValidationSuccess(type, "DeserializeFromNativeArray");
                return result;
            }
            catch (Exception ex)
            {
                _statistics.RecordValidationFailure(type, "DeserializeFromNativeArray", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            var baseStats = _innerSerializer.GetStatistics();
            
            // Add validation-specific statistics
            return baseStats with
            {
                ValidationEnabled = true,
                ValidationStatistics = _statistics.GetStatistics(),
                CachedValidationResults = _typeValidationCache.Count
            };
        }

        /// <summary>
        /// Gets detailed validation statistics.
        /// </summary>
        /// <returns>Validation statistics</returns>
        public ValidationStatisticsSummary GetValidationStatistics()
        {
            return _statistics.GetStatistics();
        }

        /// <summary>
        /// Clears the type validation cache.
        /// </summary>
        public void ClearValidationCache()
        {
            var correlationId = GetCorrelationId();
            var count = _typeValidationCache.Count;
            
            _typeValidationCache.Clear();
            
            _logger.LogInfo($"Cleared validation cache. Removed {count} cached validation results", correlationId: correlationId, sourceContext: null, properties: null);
        }

        /// <summary>
        /// Adds a custom validation rule.
        /// </summary>
        /// <param name="rule">The validation rule to add</param>
        public void AddValidationRule(IValidationRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            _validationEngine.AddRule(rule);

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"Added custom validation rule: {rule.GetType().Name}", correlationId: correlationId, sourceContext: null, properties: null);
        }

        private void ValidateTypeForSerialization<T>(Type type, T obj)
        {
            var validationResult = GetOrComputeTypeValidation(type);
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"Type {type.FullName} is not valid for serialization: {validationResult.Summary}");
            }

            // Additional runtime validation for the specific object
            if (obj != null)
            {
                var objectValidationResult = _validationEngine.ValidateObject(obj);
                if (!objectValidationResult.IsValid)
                {
                    throw new ValidationException($"Object of type {type.FullName} failed validation: {objectValidationResult.Summary}");
                }
            }
        }

        private ValidationResult ValidateTypeForDeserialization(Type type, bool throwOnFailure = true)
        {
            var validationResult = GetOrComputeTypeValidation(type);
            if (!validationResult.IsValid && throwOnFailure)
            {
                throw new ValidationException($"Type {type.FullName} is not valid for deserialization: {validationResult.Summary}");
            }

            return validationResult;
        }

        private void ValidateDeserializedObject<T>(T obj, bool throwOnFailure = true)
        {
            if (obj == null) return;

            var objectValidationResult = _validationEngine.ValidateObject(obj);
            if (!objectValidationResult.IsValid && throwOnFailure)
            {
                throw new ValidationException($"Deserialized object of type {typeof(T).FullName} failed validation: {objectValidationResult.Summary}");
            }
        }

        private ValidationResult GetOrComputeTypeValidation(Type type)
        {
            return _typeValidationCache.GetOrAdd(type, t => _validationEngine.ValidateType(t));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ValidatingSerializer));
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the validating serializer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _typeValidationCache?.Clear();
                _validationEngine?.Dispose();
                
                // Dispose inner serializer if it implements IDisposable
                if (_innerSerializer is IDisposable disposableSerializer)
                {
                    disposableSerializer.Dispose();
                }

                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("ValidatingSerializer disposed", correlationId: correlationId, sourceContext: null, properties: null);
            }
        }
    }

    /// <summary>
    /// Engine that handles validation rules and type checking.
    /// </summary>
    internal class ValidationRuleEngine : IDisposable
    {
        private readonly SerializationConfig _config;
        private readonly ILoggingService _logger;
        private readonly List<IValidationRule> _customRules;
        private readonly object _rulesLock = new();

        public ValidationRuleEngine(SerializationConfig config, ILoggingService logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _customRules = new List<IValidationRule>();
        }

        public ValidationResult ValidateType(Type type)
        {
            if (type == null)
                return ValidationResult.Failure("Type cannot be null", "ValidationRuleEngine");

            var typeName = type.FullName ?? type.Name;

            // Check blacklist first
            foreach (var pattern in _config.TypeBlacklist)
            {
                if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Failure($"Type {typeName} is blacklisted (matches pattern: {pattern})", "ValidationRuleEngine");
                }
            }

            // Check whitelist if configured
            if (_config.TypeWhitelist.Count > 0)
            {
                var isWhitelisted = false;
                foreach (var pattern in _config.TypeWhitelist)
                {
                    if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isWhitelisted = true;
                        break;
                    }
                }

                if (!isWhitelisted)
                {
                    return ValidationResult.Failure($"Type {typeName} is not whitelisted", "ValidationRuleEngine");
                }
            }

            // Check assembly trust
            if (!IsAssemblyTrusted(type.Assembly))
            {
                return ValidationResult.Failure($"Type {typeName} is from an untrusted assembly: {type.Assembly.FullName}", "ValidationRuleEngine");
            }

            // Apply custom rules
            lock (_rulesLock)
            {
                foreach (var rule in _customRules)
                {
                    var ruleResult = rule.ValidateType(type);
                    if (!ruleResult.IsValid)
                    {
                        return ruleResult;
                    }
                }
            }

            return ValidationResult.Success("ValidationRuleEngine");
        }

        public ValidationResult ValidateObject<T>(T obj)
        {
            if (obj == null)
                return ValidationResult.Success("ValidationRuleEngine");

            // Apply custom object validation rules
            lock (_rulesLock)
            {
                foreach (var rule in _customRules)
                {
                    var ruleResult = rule.ValidateObject(obj);
                    if (!ruleResult.IsValid)
                    {
                        return ruleResult;
                    }
                }
            }

            return ValidationResult.Success("ValidationRuleEngine");
        }

        public void AddRule(IValidationRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            lock (_rulesLock)
            {
                _customRules.Add(rule);
            }
        }

        private bool IsAssemblyTrusted(Assembly assembly)
        {
            if (assembly == null)
                return false;

            var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? "";

            // Always trust core system assemblies
            var trustedPrefixes = new[]
            {
                "AhBearStudios.Core",
                "AhBearStudios.Unity",
                "System",
                "Microsoft",
                "Unity",
                "mscorlib",
                "netstandard"
            };

            return trustedPrefixes.AsValueEnumerable().Any(prefix => assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            lock (_rulesLock)
            {
                foreach (var rule in _customRules.AsValueEnumerable().OfType<IDisposable>())
                {
                    rule.Dispose();
                }
                _customRules.Clear();
            }
        }
    }

    /// <summary>
    /// Interface for custom validation rules.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Gets the name of this validation rule.
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Validates a type for serialization/deserialization.
        /// </summary>
        /// <param name="type">The type to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateType(Type type);

        /// <summary>
        /// Validates an object instance.
        /// </summary>
        /// <param name="obj">The object to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateObject<T>(T obj);
    }

    /// <summary>
    /// Tracks validation statistics for monitoring and debugging.
    /// </summary>
    internal class ValidationStatistics
    {
        private long _totalValidations;
        private long _successfulValidations; 
        private long _failedValidations;
        private long _typeRegistrations;
        private long _failedTypeRegistrations;
        private readonly ConcurrentDictionary<string, long> _operationCounts;
        private readonly ConcurrentDictionary<Type, long> _typeValidationCounts;
        private readonly object _lockObject = new();

        public ValidationStatistics()
        {
            _operationCounts = new ConcurrentDictionary<string, long>();
            _typeValidationCounts = new ConcurrentDictionary<Type, long>();
        }

        public void RecordValidationSuccess(Type type, string operation)
        {
            lock (_lockObject)
            {
                Interlocked.Increment(ref _totalValidations);
                Interlocked.Increment(ref _successfulValidations);
                _operationCounts.AddOrUpdate(operation, 1, (_, count) => count + 1);
                _typeValidationCounts.AddOrUpdate(type, 1, (_, count) => count + 1);
            }
        }

        public void RecordValidationFailure(Type type, string operation, Exception exception)
        {
            lock (_lockObject)
            {
                Interlocked.Increment(ref _totalValidations);
                Interlocked.Increment(ref _failedValidations);
                _operationCounts.AddOrUpdate($"{operation}_Failed", 1, (_, count) => count + 1);
            }
        }

        public void RecordTypeRegistration(Type type, bool success)
        {
            lock (_lockObject)
            {
                if (success)
                    Interlocked.Increment(ref _typeRegistrations);
                else
                    Interlocked.Increment(ref _failedTypeRegistrations);
            }
        }

        public ValidationStatisticsSummary GetStatistics()
        {
            lock (_lockObject)
            {
                return new ValidationStatisticsSummary
                {
                    TotalValidations = _totalValidations,
                    SuccessfulValidations = _successfulValidations,
                    FailedValidations = _failedValidations,
                    SuccessRate = _totalValidations > 0 ? (double)_successfulValidations / _totalValidations : 0.0,
                    TypeRegistrations = _typeRegistrations,
                    FailedTypeRegistrations = _failedTypeRegistrations,
                    OperationCounts = _operationCounts.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    TypeValidationCounts = _typeValidationCounts.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            }
        }
    }

    /// <summary>
    /// Summary of validation statistics.
    /// </summary>
    public record ValidationStatisticsSummary
    {
        public long TotalValidations { get; init; }
        public long SuccessfulValidations { get; init; }
        public long FailedValidations { get; init; }
        public double SuccessRate { get; init; }
        public long TypeRegistrations { get; init; }
        public long FailedTypeRegistrations { get; init; }
        public IReadOnlyDictionary<string, long> OperationCounts { get; init; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<Type, long> TypeValidationCounts { get; init; } = new Dictionary<Type, long>();
    }

    /// <summary>
    /// Exception thrown when validation fails.
    /// </summary>
    public class ValidationException : SerializationException
    {
        public ValidationResult ValidationResult { get; }

        public ValidationException(string message, Type failedType = null, string operation = null, Exception innerException = null) 
            : base(message, failedType, operation, innerException) 
        { 
        }

        public ValidationException(string message, ValidationResult validationResult, Type failedType = null, string operation = null, Exception innerException = null) 
            : base(message, failedType, operation, innerException)
        {
            ValidationResult = validationResult;
        }
    }
}