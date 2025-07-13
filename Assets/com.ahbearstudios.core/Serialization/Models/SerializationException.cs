namespace AhBearStudios.Core.Serialization.Models;

// <summary>
/// Custom exception for serialization-related errors.
/// </summary>
public class SerializationException : Exception
{
    /// <summary>
    /// The type that failed to serialize/deserialize.
    /// </summary>
    public Type FailedType { get; }

    /// <summary>
    /// The operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of SerializationException.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="failedType">Type that failed</param>
    /// <param name="operation">Operation that failed</param>
    /// <param name="innerException">Inner exception</param>
    public SerializationException(string message, Type failedType = null, string operation = null, Exception innerException = null)
        : base(message, innerException)
    {
        FailedType = failedType;
        Operation = operation;
    }
}