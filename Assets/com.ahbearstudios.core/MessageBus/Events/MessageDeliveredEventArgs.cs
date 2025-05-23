using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Events
{
    /// <summary>
    /// Event arguments for message delivery events.
    /// </summary>
    public class MessageDeliveredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the message that was delivered.
        /// </summary>
        public IMessage Message { get; }
        
        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; }
        
        /// <summary>
        /// Gets the time when the message was delivered.
        /// </summary>
        public DateTime DeliveryTime { get; }
        
        /// <summary>
        /// Gets the number of delivery attempts made.
        /// </summary>
        public int DeliveryAttempts { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageDeliveredEventArgs class.
        /// </summary>
        /// <param name="message">The message that was delivered.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryTime">The time when the message was delivered.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        public MessageDeliveredEventArgs(IMessage message, Guid deliveryId, DateTime deliveryTime, int deliveryAttempts)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DeliveryId = deliveryId;
            DeliveryTime = deliveryTime;
            DeliveryAttempts = deliveryAttempts;
        }
    }
    
    /// <summary>
    /// Event arguments for message delivery failure events.
    /// </summary>
    public class MessageDeliveryFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the message that failed to deliver.
        /// </summary>
        public IMessage Message { get; }
        
        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; }
        
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Gets the exception that occurred, if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Gets the number of delivery attempts made.
        /// </summary>
        public int DeliveryAttempts { get; }
        
        /// <summary>
        /// Gets whether more delivery attempts will be made.
        /// </summary>
        public bool WillRetry { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageDeliveryFailedEventArgs class.
        /// </summary>
        /// <param name="message">The message that failed to deliver.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="deliveryAttempts">The number of delivery attempts made.</param>
        /// <param name="willRetry">Whether more delivery attempts will be made.</param>
        public MessageDeliveryFailedEventArgs(IMessage message, Guid deliveryId, string errorMessage, Exception exception, int deliveryAttempts, bool willRetry)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DeliveryId = deliveryId;
            ErrorMessage = errorMessage;
            Exception = exception;
            DeliveryAttempts = deliveryAttempts;
            WillRetry = willRetry;
        }
    }
    
    /// <summary>
    /// Event arguments for message acknowledgment events.
    /// </summary>
    public class MessageAcknowledgedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the message ID that was acknowledged.
        /// </summary>
        public Guid MessageId { get; }
        
        /// <summary>
        /// Gets the delivery ID that was acknowledged.
        /// </summary>
        public Guid DeliveryId { get; }
        
        /// <summary>
        /// Gets the time when the acknowledgment was received.
        /// </summary>
        public DateTime AcknowledgmentTime { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageAcknowledgedEventArgs class.
        /// </summary>
        /// <param name="messageId">The message ID that was acknowledged.</param>
        /// <param name="deliveryId">The delivery ID that was acknowledged.</param>
        /// <param name="acknowledgmentTime">The time when the acknowledgment was received.</param>
        public MessageAcknowledgedEventArgs(Guid messageId, Guid deliveryId, DateTime acknowledgmentTime)
        {
            MessageId = messageId;
            DeliveryId = deliveryId;
            AcknowledgmentTime = acknowledgmentTime;
        }
    }
    
    /// <summary>
    /// Event arguments for delivery service status change events.
    /// </summary>
    public class DeliveryServiceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous status.
        /// </summary>
        public DeliveryServiceStatus PreviousStatus { get; }
        
        /// <summary>
        /// Gets the current status.
        /// </summary>
        public DeliveryServiceStatus CurrentStatus { get; }
        
        /// <summary>
        /// Gets the time when the status changed.
        /// </summary>
        public DateTime StatusChangeTime { get; }
        
        /// <summary>
        /// Gets the reason for the status change, if any.
        /// </summary>
        public string Reason { get; }
        
        /// <summary>
        /// Initializes a new instance of the DeliveryServiceStatusChangedEventArgs class.
        /// </summary>
        /// <param name="previousStatus">The previous status.</param>
        /// <param name="currentStatus">The current status.</param>
        /// <param name="statusChangeTime">The time when the status changed.</param>
        /// <param name="reason">The reason for the status change.</param>
        public DeliveryServiceStatusChangedEventArgs(DeliveryServiceStatus previousStatus, DeliveryServiceStatus currentStatus, DateTime statusChangeTime, string reason = null)
        {
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            StatusChangeTime = statusChangeTime;
            Reason = reason;
        }
    }
}