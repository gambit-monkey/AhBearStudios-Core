using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for message bus metrics tracking for managed message bus operations.
    /// Provides metrics collection and performance analysis capabilities
    /// for message publishing, subscribing, and delivery operations.
    /// </summary>
    public interface IMessageBusMetrics
    {
        /// <summary>
        /// Gets metrics data for a specific message bus instance
        /// </summary>
        /// <param name="busId">Unique identifier of the message bus</param>
        /// <returns>Message bus metrics data</returns>
        MessageBusMetricsData GetMetricsData(Guid busId);
        
        /// <summary>
        /// Gets metrics data for a specific message bus with nullable return for error handling
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <returns>Message bus metrics data if found, null otherwise</returns>
        MessageBusMetricsData? GetMessageBusMetrics(Guid busId);
        
        /// <summary>
        /// Gets global metrics data aggregated across all message buses
        /// </summary>
        /// <returns>Aggregated global metrics</returns>
        MessageBusMetricsData GetGlobalMetricsData();
        
        /// <summary>
        /// Records a message publish operation
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="messageType">Type of message published</param>
        /// <param name="publishTimeMs">Time taken to publish in milliseconds</param>
        /// <param name="subscriberCount">Number of subscribers that received the message</param>
        void RecordPublish(Guid busId, string messageType, float publishTimeMs, int subscriberCount);
        
        /// <summary>
        /// Records a message delivery operation
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="messageType">Type of message delivered</param>
        /// <param name="deliveryTimeMs">Time taken to deliver in milliseconds</param>
        /// <param name="successful">Whether the delivery was successful</param>
        void RecordDelivery(Guid busId, string messageType, float deliveryTimeMs, bool successful);
        
        /// <summary>
        /// Records a subscription operation
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="messageType">Type of message subscribed to</param>
        /// <param name="subscriptionTimeMs">Time taken to subscribe in milliseconds</param>
        void RecordSubscription(Guid busId, string messageType, float subscriptionTimeMs);
        
        /// <summary>
        /// Records an unsubscription operation
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="messageType">Type of message unsubscribed from</param>
        /// <param name="unsubscriptionTimeMs">Time taken to unsubscribe in milliseconds</param>
        void RecordUnsubscription(Guid busId, string messageType, float unsubscriptionTimeMs);
        
        /// <summary>
        /// Updates message bus configuration and capacity
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="queueCapacity">Current queue capacity</param>
        /// <param name="maxSubscribers">Maximum number of subscribers</param>
        /// <param name="busName">Name of the message bus</param>
        /// <param name="busType">Type of message bus</param>
        void UpdateBusConfiguration(Guid busId, int queueCapacity, int maxSubscribers, string busName = "", string busType = "");
        
        /// <summary>
        /// Gets metrics data for all tracked message buses
        /// </summary>
        /// <returns>Collection of all message bus metrics</returns>
        IEnumerable<MessageBusMetricsData> GetAllBusMetrics();
        
        /// <summary>
        /// Reset statistics for a specific message bus
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        void ResetBusStats(Guid busId);
        
        /// <summary>
        /// Reset statistics for all message buses
        /// </summary>
        void ResetAllBusStats();
        
        /// <summary>
        /// Reset global statistics
        /// </summary>
        void ResetStats();
        
        /// <summary>
        /// Registers an alert for a specific metric threshold
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="metricName">Name of the metric to monitor</param>
        /// <param name="threshold">Threshold value for triggering the alert</param>
        void RegisterAlert(Guid busId, string metricName, double threshold);
        
        /// <summary>
        /// Gets the message delivery success ratio for a specific bus
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <returns>Success ratio between 0.0 and 1.0</returns>
        float GetDeliverySuccessRatio(Guid busId);
        
        /// <summary>
        /// Gets the message bus efficiency (successful operations / total operations)
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <returns>Efficiency ratio between 0.0 and 1.0</returns>
        float GetBusEfficiency(Guid busId);
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        bool IsCreated { get; }
    }
}