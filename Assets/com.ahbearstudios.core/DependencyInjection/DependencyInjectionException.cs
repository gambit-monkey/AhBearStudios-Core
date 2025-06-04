using System;
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

    /// <summary>
    /// Exception thrown when a service cannot be resolved from the container.
    /// </summary>
    [Serializable]
    public sealed class ServiceResolutionException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the type that failed to resolve.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        public ServiceResolutionException(Type serviceType) 
            : base($"Unable to resolve service of type '{serviceType?.FullName ?? "null"}'")
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with a custom message.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="message">The custom error message.</param>
        public ServiceResolutionException(Type serviceType, string message) : base(message)
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with a custom message and inner exception.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="message">The custom error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceResolutionException(Type serviceType, string message, Exception innerException) 
            : base(message, innerException)
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private ServiceResolutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ServiceType = (Type)info.GetValue(nameof(ServiceType), typeof(Type));
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
        }
    }

    /// <summary>
    /// Exception thrown when a circular dependency is detected.
    /// </summary>
    [Serializable]
    public sealed class CircularDependencyException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the types involved in the circular dependency.
        /// </summary>
        public Type[] DependencyChain { get; }

        /// <summary>
        /// Initializes a new instance of the CircularDependencyException class.
        /// </summary>
        /// <param name="dependencyChain">The chain of types forming the circular dependency.</param>
        public CircularDependencyException(Type[] dependencyChain) 
            : base(CreateMessage(dependencyChain))
        {
            DependencyChain = dependencyChain ?? throw new ArgumentNullException(nameof(dependencyChain));
        }

        /// <summary>
        /// Initializes a new instance of the CircularDependencyException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private CircularDependencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DependencyChain = (Type[])info.GetValue(nameof(DependencyChain), typeof(Type[]));
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(DependencyChain), DependencyChain);
        }

        /// <summary>
        /// Creates an error message from the dependency chain.
        /// </summary>
        /// <param name="dependencyChain">The dependency chain.</param>
        /// <returns>A formatted error message.</returns>
        private static string CreateMessage(Type[] dependencyChain)
        {
            if (dependencyChain == null || dependencyChain.Length == 0)
                return "Circular dependency detected";

            var typeNames = new string[dependencyChain.Length];
            for (int i = 0; i < dependencyChain.Length; i++)
            {
                typeNames[i] = dependencyChain[i]?.Name ?? "null";
            }

            return $"Circular dependency detected: {string.Join(" -> ", typeNames)}";
        }
    }

    /// <summary>
    /// Exception thrown when attempting to register a service that is already registered.
    /// </summary>
    [Serializable]
    public sealed class ServiceAlreadyRegisteredException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the type that was already registered.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceAlreadyRegisteredException class.
        /// </summary>
        /// <param name="serviceType">The type that was already registered.</param>
        public ServiceAlreadyRegisteredException(Type serviceType) 
            : base($"Service of type '{serviceType?.FullName ?? "null"}' is already registered")
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceAlreadyRegisteredException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private ServiceAlreadyRegisteredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ServiceType = (Type)info.GetValue(nameof(ServiceType), typeof(Type));
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
        }
    }

    /// <summary>
    /// Exception thrown when the container has been disposed and operations are attempted on it.
    /// </summary>
    [Serializable]
    public sealed class ContainerDisposedException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the name of the disposed container.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Initializes a new instance of the ContainerDisposedException class.
        /// </summary>
        /// <param name="containerName">The name of the disposed container.</param>
        public ContainerDisposedException(string containerName) 
            : base($"Container '{containerName ?? "unnamed"}' has been disposed and cannot be used")
        {
            ContainerName = containerName;
        }

        /// <summary>
        /// Initializes a new instance of the ContainerDisposedException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private ContainerDisposedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
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
            info.AddValue(nameof(ContainerName), ContainerName);
        }
    }
}