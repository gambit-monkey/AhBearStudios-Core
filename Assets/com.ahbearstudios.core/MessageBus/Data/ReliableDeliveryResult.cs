using System;
using AhBearStudios.Core.Messaging.Services;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Result of a reliable message delivery operation.
    /// </summary>
    public readonly struct ReliableDeliveryResult
    {
        /// <summary>
        /// Gets whether the delivery was successful.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the message ID.
        /// </summary>
        public Guid MessageId { get; }
        
        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; }
        
        /// <summary>
        /// Gets the number of delivery attempts made.
        /// </summary>
        public int DeliveryAttempts { get; }
        
        /// <summary>
        /// Gets the time when the message was finally delivered.
        /// </summary>
        public DateTime? DeliveryTime { get; }
        
        /// <summary>
        /// Gets the final status of the delivery.
        /// </summary>
        public MessageDeliveryStatus FinalStatus { get; }
        
        /// <summary>
        /// Gets the error message if delivery failed.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that occurred during delivery, if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new successful reliable delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        /// <param name="deliveryTime">The time when the message was delivered.</param>
        public ReliableDeliveryResult(Guid messageId, Guid deliveryId, int deliveryAttempts, DateTime deliveryTime)
        {
            IsSuccess = true;
            MessageId = messageId;
            DeliveryId = deliveryId;
            DeliveryAttempts = deliveryAttempts;
            DeliveryTime = deliveryTime;
            FinalStatus = MessageDeliveryStatus.Delivered;
            ErrorMessage = null;
            Exception = null;
        }
        
        /// <summary>
        /// Initializes a new failed reliable delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        /// <param name="finalStatus">The final status of the delivery.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public ReliableDeliveryResult(Guid messageId, Guid deliveryId, int deliveryAttempts, MessageDeliveryStatus finalStatus, string errorMessage, Exception exception = null)
        {
            IsSuccess = false;
            MessageId = messageId;
            DeliveryId = deliveryId;
            DeliveryAttempts = deliveryAttempts;
            DeliveryTime = null;
            FinalStatus = finalStatus;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
        
        /// <summary>
        /// Creates a successful reliable delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        /// <param name="deliveryTime">The time when the message was delivered.</param>
        /// <returns>A successful reliable delivery result.</returns>
        public static ReliableDeliveryResult Success(Guid messageId, Guid deliveryId, int deliveryAttempts, DateTime deliveryTime) =>
            new ReliableDeliveryResult(messageId, deliveryId, deliveryAttempts, deliveryTime);
        
        /// <summary>
        /// Creates a failed reliable delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        /// <param name="finalStatus">The final status of the delivery.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A failed reliable delivery result.</returns>
        public static ReliableDeliveryResult Failure(Guid messageId, Guid deliveryId, int deliveryAttempts, MessageDeliveryStatus finalStatus, string errorMessage, Exception exception = null) =>
            new ReliableDeliveryResult(messageId, deliveryId, deliveryAttempts, finalStatus, errorMessage, exception);
    }
}