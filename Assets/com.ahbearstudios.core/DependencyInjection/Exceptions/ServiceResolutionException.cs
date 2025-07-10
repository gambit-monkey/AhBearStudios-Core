using System.Runtime.Serialization;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Exceptions
{
    /// <summary>
    /// Enhanced exception thrown when a service cannot be resolved from the container.
    /// Includes framework information for better debugging.
    /// </summary>
    [Serializable]
    public sealed class ServiceResolutionException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the type that failed to resolve.
        /// </summary>
        public Type ServiceType { get; }
        
        /// <summary>
        /// Gets the framework that was used for resolution.
        /// </summary>
        public ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets the container name if available.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="framework">The framework used for resolution.</param>
        /// <param name="containerName">The container name if available.</param>
        public ServiceResolutionException(
            Type serviceType, 
            ContainerFramework framework = ContainerFramework.VContainer,
            string containerName = null) 
            : base($"Unable to resolve service of type '{serviceType?.FullName ?? "null"}' " +
                   $"from {framework} container{(containerName != null ? $" '{containerName}'" : "")}")
        {
            ServiceType = serviceType;
            Framework = framework;
            ContainerName = containerName;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with a custom message.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="message">The custom error message.</param>
        /// <param name="framework">The framework used for resolution.</param>
        /// <param name="containerName">The container name if available.</param>
        public ServiceResolutionException(
            Type serviceType, 
            string message,
            ContainerFramework framework = ContainerFramework.VContainer,
            string containerName = null) 
            : base(message)
        {
            ServiceType = serviceType;
            Framework = framework;
            ContainerName = containerName;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with a custom message and inner exception.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="message">The custom error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="framework">The framework used for resolution.</param>
        /// <param name="containerName">The container name if available.</param>
        public ServiceResolutionException(
            Type serviceType, 
            string message, 
            Exception innerException,
            ContainerFramework framework = ContainerFramework.VContainer,
            string containerName = null) 
            : base(message, innerException)
        {
            ServiceType = serviceType;
            Framework = framework;
            ContainerName = containerName;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private ServiceResolutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ServiceType = (Type)info.GetValue(nameof(ServiceType), typeof(Type));
            Framework = (ContainerFramework)info.GetValue(nameof(Framework), typeof(ContainerFramework));
            ContainerName = info.GetString(nameof(ContainerName));
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ServiceType), ServiceType);
            info.AddValue(nameof(Framework), Framework);
            info.AddValue(nameof(ContainerName), ContainerName);
        }
    }
}