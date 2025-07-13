using AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Event arguments for failed message processing.
/// </summary>
public sealed class MessageProcessingFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message that failed processing.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the subscription ID where the failure occurred.
    /// </summary>
    public Guid SubscriptionId { get; }

    /// <summary>
    /// Initializes a new instance of MessageProcessingFailedEventArgs.
    /// </summary>
    /// <param name="message">The message that failed</param>
    /// <param name="exception">The failure exception</param>
    /// <param name="subscriptionId">The subscription ID</param>
    public MessageProcessingFailedEventArgs(IMessage message, Exception exception, Guid subscriptionId)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        SubscriptionId = subscriptionId;
    }
}