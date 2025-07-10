using System;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for pending message deliveries.
    /// </summary>
    public interface IPendingDelivery
    {
        /// <summary>
        /// Gets the message being delivered.
        /// </summary>
        IMessage Message { get; }
        
        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        Guid DeliveryId { get; }
        
        /// <summary>
        /// Gets the current delivery status.
        /// </summary>
        MessageDeliveryStatus Status { get; }
        
        /// <summary>
        /// Gets the number of delivery attempts made.
        /// </summary>
        int DeliveryAttempts { get; }
        
        /// <summary>
        /// Gets the time when the delivery was first attempted.
        /// </summary>
        DateTime FirstAttemptTime { get; }
        
        /// <summary>
        /// Gets the time when the next delivery attempt will be made.
        /// </summary>
        DateTime? NextAttemptTime { get; }
        
        /// <summary>
        /// Gets whether this is a reliable delivery.
        /// </summary>
        bool IsReliable { get; }
        
        /// <summary>
        /// Gets the maximum number of delivery attempts allowed.
        /// </summary>
        int MaxDeliveryAttempts { get; }
    }
}