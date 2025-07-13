namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for subscription events.
/// </summary>
public sealed class SubscriptionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; }

    /// <summary>
    /// Gets the message type for the subscription.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of SubscriptionEventArgs.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="messageType">The message type</param>
    public SubscriptionEventArgs(Guid subscriptionId, Type messageType)
    {
        SubscriptionId = subscriptionId;
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        Timestamp = DateTime.UtcNow;
    }
}