namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Enumeration of message delivery statuses.
    /// </summary>
    public enum MessageDeliveryStatus
    {
        /// <summary>
        /// The message is queued for delivery.
        /// </summary>
        Queued,
        
        /// <summary>
        /// The message is being sent.
        /// </summary>
        Sending,
        
        /// <summary>
        /// The message has been sent and is awaiting acknowledgment.
        /// </summary>
        Sent,
        
        /// <summary>
        /// The message has been delivered and acknowledged.
        /// </summary>
        Delivered,
        
        /// <summary>
        /// The message delivery failed.
        /// </summary>
        Failed,
        
        /// <summary>
        /// The message delivery was cancelled.
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// The message delivery has expired (max attempts reached).
        /// </summary>
        Expired
    }
}