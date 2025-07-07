using System.Runtime.Serialization;

namespace AhBearStudios.Core.DependencyInjection.Exceptions;

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