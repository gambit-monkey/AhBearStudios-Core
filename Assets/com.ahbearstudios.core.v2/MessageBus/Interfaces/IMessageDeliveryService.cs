using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message delivery services that handle message transmission with various delivery guarantees.
    /// </summary>
    public interface IMessageDeliveryService : IDisposable
    {
        /// <summary>
        /// Gets the name of this delivery service implementation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this delivery service is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the current status of the delivery service.
        /// </summary>
        DeliveryServiceStatus Status { get; }

        /// <summary>
        /// Sends a message with fire-and-forget semantics (no delivery guarantees).
        /// </summary>
        /// <typeparam name="TMessage">The type of message to send.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the message has been queued for delivery.</returns>
        Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : IMessage;

        /// <summary>
        /// Sends a message with delivery confirmation (at-least-once delivery).
        /// </summary>
        /// <typeparam name="TMessage">The type of message to send.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the message has been delivered and acknowledged.</returns>
        Task<DeliveryResult> SendWithConfirmationAsync<TMessage>(TMessage message,
            CancellationToken cancellationToken = default) where TMessage : IMessage;

        /// <summary>
        /// Sends a reliable message with retry logic and persistence.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to send.</typeparam>
        /// <param name="message">The reliable message to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the message is delivered or all retry attempts are exhausted.</returns>
        Task<ReliableDeliveryResult> SendReliableAsync<TMessage>(TMessage message,
            CancellationToken cancellationToken = default) where TMessage : IReliableMessage;

        /// <summary>
        /// Sends a batch of messages with the specified delivery options.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        /// <param name="options">Delivery options for the batch.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when all messages in the batch have been processed.</returns>
        Task<BatchDeliveryResult> SendBatchAsync(IEnumerable<IMessage> messages, BatchDeliveryOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges receipt of a message.
        /// </summary>
        /// <param name="messageId">The ID of the message to acknowledge.</param>
        /// <param name="deliveryId">The delivery ID associated with the message.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the acknowledgment has been processed.</returns>
        Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the delivery status for a specific message.
        /// </summary>
        /// <param name="messageId">The ID of the message to check.</param>
        /// <param name="deliveryId">The delivery ID associated with the message.</param>
        /// <returns>The delivery status of the message, or null if not found.</returns>
        MessageDeliveryStatus? GetMessageStatus(Guid messageId, Guid deliveryId);

        /// <summary>
        /// Gets all pending message deliveries.
        /// </summary>
        /// <returns>A collection of pending message deliveries.</returns>
        IReadOnlyCollection<IPendingDelivery> GetPendingDeliveries();

        /// <summary>
        /// Gets delivery statistics for this service.
        /// </summary>
        /// <returns>Delivery statistics.</returns>
        IDeliveryStatistics GetStatistics();

        /// <summary>
        /// Cancels delivery of a specific message.
        /// </summary>
        /// <param name="messageId">The ID of the message to cancel.</param>
        /// <param name="deliveryId">The delivery ID associated with the message.</param>
        /// <returns>True if the message delivery was cancelled; otherwise, false.</returns>
        bool CancelDelivery(Guid messageId, Guid deliveryId);

        /// <summary>
        /// Starts the delivery service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the service has started.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the delivery service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes when the service has stopped.</returns>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}