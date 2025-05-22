using System;
using AhBearStudios.Core.Messaging.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Interface for a native message bus that can be used with Burst and Jobs.
    /// Provides a thread-safe way to publish and subscribe to messages in a DOTS-compatible way.
    /// </summary>
    /// <typeparam name="T">The type of message to handle.</typeparam>
    public interface INativeMessageBus<T> where T : unmanaged, IMessage
    {
        /// <summary>
        /// Gets the number of pending messages in the bus.
        /// </summary>
        int PendingMessageCount { get; }

        /// <summary>
        /// Subscribes to messages of a specific type.
        /// </summary>
        /// <param name="messageTypeId">The type ID of the message to subscribe to, or 0 for all messages.</param>
        /// <param name="handler">Function pointer to the message handler.</param>
        /// <returns>A subscription handle that can be used to unsubscribe.</returns>
        SubscriptionHandle Subscribe(int messageTypeId, FunctionPointer<MessageHandler> handler);

        /// <summary>
        /// Unsubscribes from messages using a subscription handle.
        /// </summary>
        /// <param name="handle">The subscription handle to unsubscribe.</param>
        /// <returns>True if the subscription was found and removed, false otherwise.</returns>
        bool Unsubscribe(SubscriptionHandle handle);

        /// <summary>
        /// Publishes a message to all subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <returns>True if the message was successfully published, false otherwise.</returns>
        bool Publish(T message);

        /// <summary>
        /// Processes a specified number of pending messages.
        /// </summary>
        /// <param name="maxMessages">The maximum number of messages to process.</param>
        /// <returns>The number of messages that were processed.</returns>
        int ProcessMessages(int maxMessages = 100);

        /// <summary>
        /// Schedules a job to process a specified number of pending messages.
        /// </summary>
        /// <param name="maxMessages">The maximum number of messages to process.</param>
        /// <param name="dependsOn">The JobHandle to depend on.</param>
        /// <returns>A JobHandle for the scheduled job.</returns>
        JobHandle ScheduleMessageProcessing(int maxMessages = 100, JobHandle dependsOn = default);
    }
}