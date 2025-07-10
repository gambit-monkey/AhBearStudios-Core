using System;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Represents a validation error found during container validation.
    /// </summary>
    public readonly record struct ValidationError
    {
        /// <summary>
        /// Gets the error type.
        /// </summary>
        public ValidationErrorType ErrorType { get; }
        
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the service type that caused the error.
        /// </summary>
        public Type ServiceType { get; }
        
        /// <summary>
        /// Gets the implementation type that caused the error.
        /// </summary>
        public Type ImplementationType { get; }
        
        /// <summary>
        /// Gets the underlying exception if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new validation error.
        /// </summary>
        public ValidationError(
            ValidationErrorType errorType,
            string message,
            Type serviceType = null,
            Type implementationType = null,
            Exception exception = null)
        {
            ErrorType = errorType;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Exception = exception;
        }
        
        /// <summary>
        /// Returns a string representation of this error.
        /// </summary>
        public override string ToString()
        {
            var result = $"[{ErrorType}] {Message}";
            
            if (ServiceType != null)
                result += $" (Service: {ServiceType.Name})";
            
            if (ImplementationType != null)
                result += $" (Implementation: {ImplementationType.Name})";
            
            return result;
        }
    }
}