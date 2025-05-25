using System;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Represents a factory error with an error code and message.
    /// Provides detailed information about errors that occur during factory operations.
    /// </summary>
    public readonly struct FactoryError : IEquatable<FactoryError>
    {
        /// <summary>
        /// Gets the error code
        /// </summary>
        public FactoryErrorCode Code { get; }
        
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Creates a new factory error
        /// </summary>
        /// <param name="code">Error code</param>
        /// <param name="message">Error message</param>
        public FactoryError(FactoryErrorCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Creates a new factory error with optional exception details
        /// </summary>
        /// <param name="code">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Optional exception that caused the error</param>
        public FactoryError(FactoryErrorCode code, string message, Exception exception)
            : this(code, $"{message}{(exception != null ? $" ({exception.Message})" : string.Empty)}")
        {
        }
        
        /// <summary>
        /// Returns a string representation of the error
        /// </summary>
        public override string ToString() => 
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Code}: {Message}";
            
        /// <summary>
        /// Checks if this error is equal to another error
        /// </summary>
        public bool Equals(FactoryError other) =>
            Code == other.Code && 
            Message == other.Message && 
            Timestamp.Equals(other.Timestamp);
            
        /// <summary>
        /// Checks if this error is equal to another object
        /// </summary>
        public override bool Equals(object obj) =>
            obj is FactoryError other && Equals(other);
            
        /// <summary>
        /// Gets a hash code for this error
        /// </summary>
        public override int GetHashCode() =>
            HashCode.Combine(Code, Message, Timestamp);
            
        /// <summary>
        /// Compares two factory errors for equality
        /// </summary>
        public static bool operator ==(FactoryError left, FactoryError right) =>
            left.Equals(right);
            
        /// <summary>
        /// Compares two factory errors for inequality
        /// </summary>
        public static bool operator !=(FactoryError left, FactoryError right) =>
            !left.Equals(right);
    }
}