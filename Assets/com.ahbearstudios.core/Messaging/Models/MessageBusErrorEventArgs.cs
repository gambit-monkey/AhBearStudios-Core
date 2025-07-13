using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for message bus error events.
/// </summary>
public sealed class MessageBusErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message that caused the error.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of MessageBusErrorEventArgs.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="exception">The exception</param>
    public MessageBusErrorEventArgs(IMessage message, Exception exception)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Timestamp = DateTime.UtcNow;
    }
}