using AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Event arguments for failed message publication.
/// </summary>
public sealed class MessagePublishFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message that failed to publish.
    /// </summary>
    public IMessage Message { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    public int RetryAttempt { get; }

    /// <summary>
    /// Initializes a new instance of MessagePublishFailedEventArgs.
    /// </summary>
    /// <param name="message">The message that failed</param>
    /// <param name="exception">The failure exception</param>
    /// <param name="retryAttempt">The retry attempt number</param>
    public MessagePublishFailedEventArgs(IMessage message, Exception exception, int retryAttempt)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        RetryAttempt = retryAttempt;
    }
}