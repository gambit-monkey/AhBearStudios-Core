using AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Event arguments for successful message publication.
/// </summary>
public sealed class MessagePublishedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the published message.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the time taken to publish the message.
    /// </summary>
    public TimeSpan PublishTime { get; }

    /// <summary>
    /// Initializes a new instance of MessagePublishedEventArgs.
    /// </summary>
    /// <param name="message">The published message</param>
    /// <param name="publishTime">The time taken to publish</param>
    public MessagePublishedEventArgs(IMessage message, TimeSpan publishTime)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        PublishTime = publishTime;
    }
}