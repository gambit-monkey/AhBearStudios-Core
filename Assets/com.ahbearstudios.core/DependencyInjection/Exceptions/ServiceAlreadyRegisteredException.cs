using System.Runtime.Serialization;

namespace AhBearStudios.Core.DependencyInjection.Exceptions;

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