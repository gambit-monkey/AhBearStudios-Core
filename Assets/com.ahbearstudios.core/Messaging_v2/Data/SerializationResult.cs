using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Result of a serialization operation.
    /// </summary>
    public readonly struct SerializationResult
    {
        /// <summary>
        /// Gets whether the serialization was successful.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the serialized data if successful.
        /// </summary>
        public byte[] Data { get; }
        
        /// <summary>
        /// Gets the error message if unsuccessful.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that occurred during serialization if unsuccessful.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new successful serialization result.
        /// </summary>
        /// <param name="data">The serialized data.</param>
        public SerializationResult(byte[] data)
        {
            IsSuccess = true;
            Data = data;
            ErrorMessage = null;
            Exception = null;
        }
        
        /// <summary>
        /// Initializes a new failed serialization result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public SerializationResult(string errorMessage, Exception exception = null)
        {
            IsSuccess = false;
            Data = null;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
        
        /// <summary>
        /// Creates a successful serialization result.
        /// </summary>
        /// <param name="data">The serialized data.</param>
        /// <returns>A successful serialization result.</returns>
        public static SerializationResult Success(byte[] data) => new SerializationResult(data);
        
        /// <summary>
        /// Creates a failed serialization result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A failed serialization result.</returns>
        public static SerializationResult Failure(string errorMessage, Exception exception = null) => 
            new SerializationResult(errorMessage, exception);
    }
    
    /// <summary>
    /// Result of a deserialization operation.
    /// </summary>
    /// <typeparam name="T">The type of the deserialized message.</typeparam>
    public readonly struct DeserializationResult<T> where T : IMessage
    {
        /// <summary>
        /// Gets whether the deserialization was successful.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the deserialized message if successful.
        /// </summary>
        public T Message { get; }
        
        /// <summary>
        /// Gets the error message if unsuccessful.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that occurred during deserialization if unsuccessful.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new successful deserialization result.
        /// </summary>
        /// <param name="message">The deserialized message.</param>
        public DeserializationResult(T message)
        {
            IsSuccess = true;
            Message = message;
            ErrorMessage = null;
            Exception = null;
        }
        
        /// <summary>
        /// Initializes a new failed deserialization result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public DeserializationResult(string errorMessage, Exception exception = null)
        {
            IsSuccess = false;
            Message = default;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
        
        /// <summary>
        /// Creates a successful deserialization result.
        /// </summary>
        /// <param name="message">The deserialized message.</param>
        /// <returns>A successful deserialization result.</returns>
        public static DeserializationResult<T> Success(T message) => new DeserializationResult<T>(message);
        
        /// <summary>
        /// Creates a failed deserialization result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A failed deserialization result.</returns>
        public static DeserializationResult<T> Failure(string errorMessage, Exception exception = null) => 
            new DeserializationResult<T>(errorMessage, exception);
    }
}