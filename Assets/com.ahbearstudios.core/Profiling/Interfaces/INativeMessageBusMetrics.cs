using System;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Burst-compatible interface for message bus metrics tracking.
    /// Designed to be used with the Unity Job System and Burst compiler.
    /// </summary>
    public interface INativeMessageBusMetrics : IDisposable
    {
        /// <summary>
        /// Gets metrics data for a specific message bus
        /// </summary>
        MessageBusMetricsData GetMetricsData(FixedString64Bytes busId);
        
        /// <summary>
        /// Gets global metrics data aggregated across all message buses
        /// </summary>
        MessageBusMetricsData GetGlobalMetricsData();
        
        /// <summary>
        /// Records a message publish operation for a bus
        /// </summary>
        JobHandle RecordPublish(FixedString64Bytes busId, FixedString64Bytes messageType, float publishTimeMs, int subscriberCount, JobHandle dependencies = default);
        
        /// <summary>
        /// Records a message delivery operation for a bus
        /// </summary>
        JobHandle RecordDelivery(FixedString64Bytes busId, FixedString64Bytes messageType, float deliveryTimeMs, bool successful, JobHandle dependencies = default);
        
        /// <summary>
        /// Records a subscription operation for a bus
        /// </summary>
        JobHandle RecordSubscription(FixedString64Bytes busId, FixedString64Bytes messageType, float subscriptionTimeMs, JobHandle dependencies = default);
        
        /// <summary>
        /// Records an unsubscription operation for a bus
        /// </summary>
        JobHandle RecordUnsubscription(FixedString64Bytes busId, FixedString64Bytes messageType, float unsubscriptionTimeMs, JobHandle dependencies = default);
        
        /// <summary>
        /// Updates message bus capacity and configuration
        /// </summary>
        void UpdateBusConfiguration(FixedString64Bytes busId, int queueCapacity, int maxSubscribers = 0, FixedString64Bytes busName = default, FixedString64Bytes busType = default);
        
        /// <summary>
        /// Gets metrics data for all tracked message buses
        /// </summary>
        NativeArray<MessageBusMetricsData> GetAllBusMetrics(Allocator allocator);
        
        /// <summary>
        /// Reset statistics for a specific message bus
        /// </summary>
        void ResetBusStats(FixedString64Bytes busId);
        
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
        void RegisterAlert(FixedString64Bytes busId, FixedString64Bytes metricName, double threshold);
        
        /// <summary>
        /// Gets the message delivery success ratio for a specific bus
        /// </summary>
        float GetDeliverySuccessRatio(FixedString64Bytes busId);
        
        /// <summary>
        /// Gets the message bus efficiency
        /// </summary>
        float GetBusEfficiency(FixedString64Bytes busId);
        
        /// <summary>
        /// Whether the native metrics tracker is created and initialized
        /// </summary>
        bool IsCreated { get; }
        
        /// <summary>
        /// Gets the allocator used by this native metrics instance
        /// </summary>
        Allocator Allocator { get; }
    }
}