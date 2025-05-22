using System;

namespace AhBearStudios.Core.Messaging.Reliability
{
    /// <summary>
    /// Interface for messages that require reliable delivery.
    /// </summary>
    public interface IReliableMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the delivery ID for tracking the message.
        /// </summary>
        Guid DeliveryId { get; set; }
        
        /// <summary>
        /// Gets or sets the number of delivery attempts.
        /// </summary>
        int DeliveryAttempts { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of delivery attempts.
        /// </summary>
        int MaxDeliveryAttempts { get; set; }
        
        /// <summary>
        /// Gets or sets the time of the next delivery attempt.
        /// </summary>
        long NextAttemptTicks { get; set; }
    }
}