using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for delivery service statistics.
    /// </summary>
    public interface IDeliveryStatistics
    {
        /// <summary>
        /// Gets the total number of messages sent.
        /// </summary>
        long TotalMessagesSent { get; }
        
        /// <summary>
        /// Gets the total number of messages delivered successfully.
        /// </summary>
        long TotalMessagesDelivered { get; }
        
        /// <summary>
        /// Gets the total number of messages that failed to deliver.
        /// </summary>
        long TotalMessagesFailed { get; }
        
        /// <summary>
        /// Gets the total number of messages acknowledged.
        /// </summary>
        long TotalMessagesAcknowledged { get; }
        
        /// <summary>
        /// Gets the number of messages currently pending delivery.
        /// </summary>
        long PendingDeliveries { get; }
        
        /// <summary>
        /// Gets the average delivery time in milliseconds.
        /// </summary>
        double AverageDeliveryTimeMs { get; }
        
        /// <summary>
        /// Gets the delivery success rate as a percentage.
        /// </summary>
        double DeliverySuccessRate { get; }
        
        /// <summary>
        /// Gets the time when statistics were last reset.
        /// </summary>
        DateTime LastResetTime { get; }
        
        /// <summary>
        /// Resets all statistics.
        /// </summary>
        void Reset();
    }
}