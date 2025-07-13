using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
    /// Result of a serialization operation.
    /// </summary>
    public record SerializationResult
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Exception that occurred during operation.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Size of serialized data in bytes.
        /// </summary>
        public int DataSize { get; init; }

        /// <summary>
        /// Time taken for the operation.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Type that was serialized/deserialized.
        /// </summary>
        public Type TargetType { get; init; }

        /// <summary>
        /// Correlation ID for tracing.
        /// </summary>
        public FixedString64Bytes CorrelationId { get; init; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="dataSize">Size of serialized data</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="targetType">Target type</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Success result</returns>
        public static SerializationResult Success(int dataSize, TimeSpan duration, Type targetType, FixedString64Bytes correlationId)
        {
            return new SerializationResult
            {
                Success = true,
                DataSize = dataSize,
                Duration = duration,
                TargetType = targetType,
                CorrelationId = correlationId
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="targetType">Target type</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Failure result</returns>
        public static SerializationResult Failure(string errorMessage, Exception exception, Type targetType, FixedString64Bytes correlationId)
        {
            return new SerializationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                TargetType = targetType,
                CorrelationId = correlationId
            };
        }
    }