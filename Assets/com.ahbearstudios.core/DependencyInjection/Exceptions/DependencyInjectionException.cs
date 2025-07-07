using System.Runtime.Serialization;

namespace AhBearStudios.Core.DependencyInjection.Exceptions
{
    /// <summary>
    /// Base exception for dependency injection related errors.
    /// </summary>
    [Serializable]
    public class DependencyInjectionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the DependencyInjectionException class.
        /// </summary>
        public DependencyInjectionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DependencyInjectionException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DependencyInjectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DependencyInjectionException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DependencyInjectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DependencyInjectionException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DependencyInjectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}