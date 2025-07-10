using System;

namespace AhBearStudios.Core.MessageBus.Data
{
    /// <summary>
    /// Result of a message delivery operation.
    /// </summary>
    public readonly struct DeliveryResult
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
        /// Gets the time when the message was delivered.
        /// </summary>
        public DateTime DeliveryTime { get; }
        
        /// <summary>
        /// Gets the error message if delivery failed.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that occurred during delivery, if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new successful delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryTime">The time when the message was delivered.</param>
        public DeliveryResult(Guid messageId, Guid deliveryId, DateTime deliveryTime)
        {
            IsSuccess = true;
            MessageId = messageId;
            DeliveryId = deliveryId;
            DeliveryTime = deliveryTime;
            ErrorMessage = null;
            Exception = null;
        }
        
        /// <summary>
        /// Initializes a new failed delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public DeliveryResult(Guid messageId, Guid deliveryId, string errorMessage, Exception exception = null)
        {
            IsSuccess = false;
            MessageId = messageId;
            DeliveryId = deliveryId;
            DeliveryTime = default;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
        
        /// <summary>
        /// Creates a successful delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryTime">The time when the message was delivered.</param>
        /// <returns>A successful delivery result.</returns>
        public static DeliveryResult Success(Guid messageId, Guid deliveryId, DateTime deliveryTime) =>
            new DeliveryResult(messageId, deliveryId, deliveryTime);
        
        /// <summary>
        /// Creates a failed delivery result.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A failed delivery result.</returns>
        public static DeliveryResult Failure(Guid messageId, Guid deliveryId, string errorMessage, Exception exception = null) =>
            new DeliveryResult(messageId, deliveryId, errorMessage, exception);
    }
}