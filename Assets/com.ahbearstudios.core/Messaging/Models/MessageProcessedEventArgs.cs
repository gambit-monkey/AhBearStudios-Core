using AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Event arguments for successful message processing.
/// </summary>
public sealed class MessageProcessedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the processed message.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the time taken to process the message.
    /// </summary>
    public TimeSpan ProcessingTime { get; }

    /// <summary>
    /// Initializes a new instance of MessageProcessedEventArgs.
    /// </summary>
    /// <param name="message">The processed message</param>
    /// <param name="processingTime">The processing time</param>
    public MessageProcessedEventArgs(IMessage message, TimeSpan processingTime)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ProcessingTime = processingTime;
    }
}