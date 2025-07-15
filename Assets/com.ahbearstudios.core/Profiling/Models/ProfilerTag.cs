using System;
using Unity.Collections;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Models
{
    /// <summary>
    /// Represents a profiler tag for performance monitoring operations.
    /// Provides a high-performance, zero-allocation identifier that integrates seamlessly
    /// with Unity's profiling system and custom profiling services.
    /// </summary>
    /// <remarks>
    /// The ProfilerTag record struct is designed for Unity game development performance requirements:
    /// - Uses FixedString64Bytes for zero-allocation string handling and Burst compatibility
    /// - Integrates with Unity's ProfilerMarker system for consistent naming
    /// - Provides automatic equality comparison and hashing via record struct semantics
    /// - Includes implicit conversions for ease of use with Unity systems
    /// - Supports hierarchical naming conventions for organized profiling
    /// - Maintains compatibility with existing dual profiling architecture
    /// </remarks>
    /// <param name="Name">The profiler tag name as a FixedString64Bytes for performance optimization.</param>
    public readonly record struct ProfilerTag(FixedString64Bytes Name)
    {
        /// <summary>
        /// Creates a ProfilerTag with string validation and conversion.
        /// </summary>
        /// <param name="name">The tag name. If null or empty, defaults to "Unknown".</param>
        /// <exception cref="ArgumentException">Thrown when the name exceeds 64 bytes when encoded as UTF-8.</exception>
        public ProfilerTag(string name) : this(ValidateAndConvert(name))
        {
        }

        /// <summary>
        /// Gets a value indicating whether this ProfilerTag is empty or uninitialized.
        /// </summary>
        public bool IsEmpty => Name.Length == 0;

        /// <summary>
        /// Validates and converts a string to FixedString64Bytes with proper error handling.
        /// </summary>
        /// <param name="name">The name to validate and convert.</param>
        /// <returns>A validated FixedString64Bytes instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the name exceeds 64 bytes.</exception>
        private static FixedString64Bytes ValidateAndConvert(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new FixedString64Bytes("Unknown");

            // Validate that the name fits within FixedString64Bytes constraints
            if (System.Text.Encoding.UTF8.GetByteCount(name) > 61) // Account for null terminator
                throw new ArgumentException($"ProfilerTag name '{name}' exceeds 64 byte limit", nameof(name));

            return new FixedString64Bytes(name);
        }

        /// <summary>
        /// Implicitly converts a string to a ProfilerTag.
        /// </summary>
        /// <param name="name">The string to convert.</param>
        /// <returns>A new ProfilerTag instance.</returns>
        public static implicit operator ProfilerTag(string name) => new(name);

        /// <summary>
        /// Implicitly converts a FixedString64Bytes to a ProfilerTag.
        /// </summary>
        /// <param name="name">The FixedString64Bytes to convert.</param>
        /// <returns>A new ProfilerTag instance.</returns>
        public static implicit operator ProfilerTag(FixedString64Bytes name) => new(name);

        /// <summary>
        /// Implicitly converts a ProfilerTag to a FixedString64Bytes.
        /// </summary>
        /// <param name="tag">The ProfilerTag to convert.</param>
        /// <returns>The underlying FixedString64Bytes.</returns>
        public static implicit operator FixedString64Bytes(ProfilerTag tag) => tag.Name;

        /// <summary>
        /// Implicitly converts a ProfilerTag to a string for Unity ProfilerMarker compatibility.
        /// </summary>
        /// <param name="tag">The ProfilerTag to convert.</param>
        /// <returns>The tag name as a string.</returns>
        public static implicit operator string(ProfilerTag tag) => tag.Name.ToString();

        /// <summary>
        /// Creates a Unity ProfilerMarker using this ProfilerTag's name.
        /// This enables seamless integration with Unity's profiling system.
        /// </summary>
        /// <returns>A new ProfilerMarker instance for Unity profiler integration.</returns>
        public ProfilerMarker CreateUnityMarker() => new(Name.ToString());

        /// <summary>
        /// Creates a ProfilerTag for a method or operation name following Unity's naming conventions.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="methodName">The method name.</param>
        /// <returns>A ProfilerTag in the format "ClassName.MethodName".</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag CreateMethodTag(string className, string methodName)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentException("Class name cannot be null or empty", nameof(className));
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

            return new ProfilerTag($"{className}.{methodName}");
        }

        /// <summary>
        /// Creates a ProfilerTag for a system operation following the established naming convention.
        /// </summary>
        /// <param name="systemName">The system name.</param>
        /// <param name="operationName">The operation name.</param>
        /// <returns>A ProfilerTag in the format "SystemName.OperationName".</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag CreateSystemTag(string systemName, string operationName)
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentException("System name cannot be null or empty", nameof(systemName));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            return new ProfilerTag($"{systemName}.{operationName}");
        }

        /// <summary>
        /// Creates a ProfilerTag for a Unity-specific operation following Unity's naming conventions.
        /// </summary>
        /// <param name="prefix">The Unity prefix (e.g., "Unity", "GameObject", "Component").</param>
        /// <param name="operationName">The operation name.</param>
        /// <returns>A ProfilerTag in the format "Prefix.OperationName".</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag CreateUnityTag(string prefix, string operationName)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            return new ProfilerTag($"{prefix}.{operationName}");
        }

        /// <summary>
        /// Creates a ProfilerTag for hierarchical profiling with three levels.
        /// </summary>
        /// <param name="system">The system name.</param>
        /// <param name="component">The component name.</param>
        /// <param name="operation">The operation name.</param>
        /// <returns>A ProfilerTag in the format "System.Component.Operation".</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag CreateHierarchicalTag(string system, string component, string operation)
        {
            if (string.IsNullOrEmpty(system))
                throw new ArgumentException("System name cannot be null or empty", nameof(system));
            if (string.IsNullOrEmpty(component))
                throw new ArgumentException("Component name cannot be null or empty", nameof(component));
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operation));

            return new ProfilerTag($"{system}.{component}.{operation}");
        }

        /// <summary>
        /// Creates a ProfilerTag for batched operations.
        /// </summary>
        /// <param name="baseTag">The base operation tag.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <returns>A ProfilerTag in the format "BaseTag.Batch{size}".</returns>
        /// <exception cref="ArgumentException">Thrown when the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag CreateBatchTag(ProfilerTag baseTag, int batchSize)
        {
            return new ProfilerTag($"{baseTag.Name}.Batch{batchSize}");
        }

        /// <summary>
        /// Creates a ProfilerTag with a suffix for operation variants.
        /// </summary>
        /// <param name="baseTag">The base operation tag.</param>
        /// <param name="suffix">The suffix to append.</param>
        /// <returns>A ProfilerTag in the format "BaseTag.Suffix".</returns>
        /// <exception cref="ArgumentException">Thrown when the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag WithSuffix(ProfilerTag baseTag, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
                throw new ArgumentException("Suffix cannot be null or empty", nameof(suffix));

            return new ProfilerTag($"{baseTag.Name}.{suffix}");
        }

        /// <summary>
        /// Creates a ProfilerTag with a prefix for operation variants.
        /// </summary>
        /// <param name="prefix">The prefix to prepend.</param>
        /// <param name="baseTag">The base operation tag.</param>
        /// <returns>A ProfilerTag in the format "Prefix.BaseTag".</returns>
        /// <exception cref="ArgumentException">Thrown when the combined name exceeds 64 bytes.</exception>
        public static ProfilerTag WithPrefix(string prefix, ProfilerTag baseTag)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

            return new ProfilerTag($"{prefix}.{baseTag.Name}");
        }

        // Predefined ProfilerTags for common operations aligned with Unity naming conventions

        /// <summary>
        /// Predefined ProfilerTag for unknown or default operations.
        /// </summary>
        public static readonly ProfilerTag Unknown = new("Unknown");

        /// <summary>
        /// Predefined ProfilerTag for initialization operations.
        /// </summary>
        public static readonly ProfilerTag Initialize = new("Initialize");

        /// <summary>
        /// Predefined ProfilerTag for cleanup operations.
        /// </summary>
        public static readonly ProfilerTag Cleanup = new("Cleanup");

        /// <summary>
        /// Predefined ProfilerTag for update operations.
        /// </summary>
        public static readonly ProfilerTag Update = new("Update");

        /// <summary>
        /// Predefined ProfilerTag for render operations.
        /// </summary>
        public static readonly ProfilerTag Render = new("Render");

        /// <summary>
        /// Predefined ProfilerTag for Unity slow frame detection.
        /// </summary>
        public static readonly ProfilerTag UnitySlowFrame = new("Unity.SlowFrame");

        /// <summary>
        /// Predefined ProfilerTag for logging write operations.
        /// </summary>
        public static readonly ProfilerTag LogWrite = new("Log.Write");

        /// <summary>
        /// Predefined ProfilerTag for batch logging operations.
        /// </summary>
        public static readonly ProfilerTag LogBatch = new("Log.Batch");

        /// <summary>
        /// Predefined ProfilerTag for health check operations.
        /// </summary>
        public static readonly ProfilerTag HealthCheck = new("HealthCheck");

        /// <summary>
        /// Predefined ProfilerTag for message bus operations.
        /// </summary>
        public static readonly ProfilerTag MessageBus = new("MessageBus");

        /// <summary>
        /// Predefined ProfilerTag for serialization operations.
        /// </summary>
        public static readonly ProfilerTag Serialization = new("Serialization");

        /// <summary>
        /// Predefined ProfilerTag for pooling operations.
        /// </summary>
        public static readonly ProfilerTag Pooling = new("Pooling");

        /// <summary>
        /// Predefined ProfilerTag for alert operations.
        /// </summary>
        public static readonly ProfilerTag Alert = new("Alert");

        /// <summary>
        /// Gets an empty ProfilerTag instance.
        /// </summary>
        public static ProfilerTag Empty => new(new FixedString64Bytes());
    }
}