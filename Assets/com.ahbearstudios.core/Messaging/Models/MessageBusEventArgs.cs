using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for message bus events.
/// </summary>
public sealed class MessageBusEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message involved in the event.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of MessageBusEventArgs.
    /// </summary>
    /// <param name="message">The message</param>
    public MessageBusEventArgs(IMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Timestamp = DateTime.UtcNow;
    }
}