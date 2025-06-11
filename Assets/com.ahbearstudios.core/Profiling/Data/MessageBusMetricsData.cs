using System;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling.Data
{
    /// <summary>
    /// Burst-compatible structure containing message bus metrics data
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct MessageBusMetricsData : IEquatable<MessageBusMetricsData>
    {
        // Message bus identification
        public FixedString128Bytes Name;
        public FixedString64Bytes BusId;
        public FixedString64Bytes BusType;
        
        // Capacity and configuration
        public int QueueCapacity;
        public int MaxSubscribers;
        public int CurrentSubscriberCount;
        public int PeakSubscriberCount;
        public int CurrentQueueSize;
        public int PeakQueueSize;
        
        // Message statistics - Using the names expected by the implementations
        public long TotalPublishes;
        public long TotalDeliveries;
        public long TotalFailures;
        public long TotalSubscriptions;
        public long TotalUnsubscriptions;
        
        // Performance metrics - Publishing
        public float AveragePublishTimeMs;
        public float MaxPublishTimeMs;
        public float MinPublishTimeMs;
        public float LastPublishTimeMs;
        public double TotalPublishTimeMs;
        public int PublishSampleCount;
        
        // Performance metrics - Delivery
        public float AverageDeliveryTimeMs;
        public float MaxDeliveryTimeMs;
        public float MinDeliveryTimeMs;
        public float LastDeliveryTimeMs;
        public double TotalDeliveryTimeMs;
        public int DeliverySampleCount;
        
        // Performance metrics - Subscription management
        public float AverageSubscriptionTimeMs;
        public float MaxSubscriptionTimeMs;
        public float MinSubscriptionTimeMs;
        public double TotalSubscriptionTimeMs;
        public int SubscriptionSampleCount;
        
        public float AverageUnsubscriptionTimeMs;
        public float MaxUnsubscriptionTimeMs;
        public float MinUnsubscriptionTimeMs;
        public double TotalUnsubscriptionTimeMs;
        public int UnsubscriptionSampleCount;
        
        // Reliability metrics
        public float DeliverySuccessRatio;
        public int DeliveryRetryCount;
        public long TotalReliableMessages;
        public long ReliableDeliveryFailures;
        
        // Throughput metrics
        public float MessagesPerSecond;
        public float PeakMessagesPerSecond;
        public int LastBatchSize;
        public int PeakBatchSize;
        
        // Memory tracking
        public long TotalMemoryBytes;
        public long PeakMemoryBytes;
        public int EstimatedMessageSizeBytes;
        
        // Queue metrics
        public int QueueOverflows;
        public int QueueUnderflows;
        public float QueueUtilizationRatio;
        
        // Time tracking
        public float LastOperationTime;
        public float LastResetTime;
        public float CreationTime;
        
        // Additional properties needed by implementations
        public int CurrentSubscribers => CurrentSubscriberCount;
        
        /// <summary>
        /// Creates a new message bus metrics data instance
        /// </summary>
        /// <param name="busId">Bus identifier</param>
        /// <param name="name">Bus name</param>
        /// <param name="busType">Bus type</param>
        public MessageBusMetricsData(FixedString64Bytes busId, FixedString128Bytes name, FixedString64Bytes busType = default)
        {
            // Basic identification
            Name = name;
            BusId = busId;
            BusType = busType.IsEmpty ? new FixedString64Bytes("MessageBus") : busType;
            
            // Initialize capacity
            QueueCapacity = 0;
            MaxSubscribers = 0;
            CurrentSubscriberCount = 0;
            PeakSubscriberCount = 0;
            CurrentQueueSize = 0;
            PeakQueueSize = 0;
            
            // Initialize counters - using the correct names
            TotalPublishes = 0;
            TotalDeliveries = 0;
            TotalFailures = 0;
            TotalSubscriptions = 0;
            TotalUnsubscriptions = 0;
            
            // Initialize publish metrics
            AveragePublishTimeMs = 0f;
            MaxPublishTimeMs = 0f;
            MinPublishTimeMs = float.MaxValue;
            LastPublishTimeMs = 0f;
            TotalPublishTimeMs = 0.0;
            PublishSampleCount = 0;
            
            // Initialize delivery metrics
            AverageDeliveryTimeMs = 0f;
            MaxDeliveryTimeMs = 0f;
            MinDeliveryTimeMs = float.MaxValue;
            LastDeliveryTimeMs = 0f;
            TotalDeliveryTimeMs = 0.0;
            DeliverySampleCount = 0;
            
            // Initialize subscription metrics
            AverageSubscriptionTimeMs = 0f;
            MaxSubscriptionTimeMs = 0f;
            MinSubscriptionTimeMs = float.MaxValue;
            TotalSubscriptionTimeMs = 0.0;
            SubscriptionSampleCount = 0;
            
            // Initialize unsubscription metrics
            AverageUnsubscriptionTimeMs = 0f;
            MaxUnsubscriptionTimeMs = 0f;
            MinUnsubscriptionTimeMs = float.MaxValue;
            TotalUnsubscriptionTimeMs = 0.0;
            UnsubscriptionSampleCount = 0;
            
            // Initialize reliability metrics
            DeliverySuccessRatio = 1.0f;
            DeliveryRetryCount = 0;
            TotalReliableMessages = 0;
            ReliableDeliveryFailures = 0;
            
            // Initialize throughput metrics
            MessagesPerSecond = 0f;
            PeakMessagesPerSecond = 0f;
            LastBatchSize = 0;
            PeakBatchSize = 0;
            
            // Initialize memory tracking
            TotalMemoryBytes = 0;
            PeakMemoryBytes = 0;
            EstimatedMessageSizeBytes = 0;
            
            // Initialize queue metrics
            QueueOverflows = 0;
            QueueUnderflows = 0;
            QueueUtilizationRatio = 0f;
            
            // Initialize time tracking
            float currentTime = UnityEngine.Time.realtimeSinceStartup;
            LastOperationTime = currentTime;
            LastResetTime = currentTime;
            CreationTime = currentTime;
        }
        
        /// <summary>
        /// Constructor overload that accepts Guid for busId
        /// </summary>
        public MessageBusMetricsData(Guid busId, string name, string busType = "MessageBus")
            : this(new FixedString64Bytes(busId.ToString()), new FixedString128Bytes(name), new FixedString64Bytes(busType))
        {
        }
        
        /// <summary>
        /// Records a publish operation
        /// </summary>
        public MessageBusMetricsData RecordPublish(string messageType, float publishTimeMs, int subscriberCount, float currentTime)
        {
            var updated = this;
            updated.TotalPublishes++;
            updated.LastPublishTimeMs = publishTimeMs;
            updated.TotalPublishTimeMs += publishTimeMs;
            updated.PublishSampleCount++;
            updated.LastOperationTime = currentTime;
            
            // Update min/max
            if (publishTimeMs > updated.MaxPublishTimeMs)
                updated.MaxPublishTimeMs = publishTimeMs;
            if (publishTimeMs < updated.MinPublishTimeMs)
                updated.MinPublishTimeMs = publishTimeMs;
                
            // Update average
            updated.AveragePublishTimeMs = (float)(updated.TotalPublishTimeMs / updated.PublishSampleCount);
            
            return updated;
        }
        
        /// <summary>
        /// Records a delivery operation
        /// </summary>
        public MessageBusMetricsData RecordDelivery(string messageType, float deliveryTimeMs, bool successful, float currentTime)
        {
            var updated = this;
            updated.TotalDeliveries++;
            updated.LastDeliveryTimeMs = deliveryTimeMs;
            updated.TotalDeliveryTimeMs += deliveryTimeMs;
            updated.DeliverySampleCount++;
            updated.LastOperationTime = currentTime;
            
            if (!successful)
                updated.TotalFailures++;
            
            // Update min/max
            if (deliveryTimeMs > updated.MaxDeliveryTimeMs)
                updated.MaxDeliveryTimeMs = deliveryTimeMs;
            if (deliveryTimeMs < updated.MinDeliveryTimeMs)
                updated.MinDeliveryTimeMs = deliveryTimeMs;
                
            // Update average
            updated.AverageDeliveryTimeMs = (float)(updated.TotalDeliveryTimeMs / updated.DeliverySampleCount);
            
            // Update success ratio
            updated.DeliverySuccessRatio = (float)(updated.TotalDeliveries - updated.TotalFailures) / updated.TotalDeliveries;
            
            return updated;
        }
        
        /// <summary>
        /// Records a subscription operation
        /// </summary>
        public MessageBusMetricsData RecordSubscription(string messageType, float subscriptionTimeMs, float currentTime)
        {
            var updated = this;
            updated.TotalSubscriptions++;
            updated.CurrentSubscriberCount++;
            updated.TotalSubscriptionTimeMs += subscriptionTimeMs;
            updated.SubscriptionSampleCount++;
            updated.LastOperationTime = currentTime;
            
            // Update peak
            if (updated.CurrentSubscriberCount > updated.PeakSubscriberCount)
                updated.PeakSubscriberCount = updated.CurrentSubscriberCount;
            
            // Update min/max
            if (subscriptionTimeMs > updated.MaxSubscriptionTimeMs)
                updated.MaxSubscriptionTimeMs = subscriptionTimeMs;
            if (subscriptionTimeMs < updated.MinSubscriptionTimeMs)
                updated.MinSubscriptionTimeMs = subscriptionTimeMs;
                
            // Update average
            updated.AverageSubscriptionTimeMs = (float)(updated.TotalSubscriptionTimeMs / updated.SubscriptionSampleCount);
            
            return updated;
        }
        
        /// <summary>
        /// Records an unsubscription operation
        /// </summary>
        public MessageBusMetricsData RecordUnsubscription(string messageType, float unsubscriptionTimeMs, float currentTime)
        {
            var updated = this;
            updated.TotalUnsubscriptions++;
            if (updated.CurrentSubscriberCount > 0)
                updated.CurrentSubscriberCount--;
            updated.TotalUnsubscriptionTimeMs += unsubscriptionTimeMs;
            updated.UnsubscriptionSampleCount++;
            updated.LastOperationTime = currentTime;
            
            // Update min/max
            if (unsubscriptionTimeMs > updated.MaxUnsubscriptionTimeMs)
                updated.MaxUnsubscriptionTimeMs = unsubscriptionTimeMs;
            if (unsubscriptionTimeMs < updated.MinUnsubscriptionTimeMs)
                updated.MinUnsubscriptionTimeMs = unsubscriptionTimeMs;
                
            // Update average
            updated.AverageUnsubscriptionTimeMs = (float)(updated.TotalUnsubscriptionTimeMs / updated.UnsubscriptionSampleCount);
            
            return updated;
        }
        
        /// <summary>
        /// Updates queue capacity
        /// </summary>
        public MessageBusMetricsData WithQueueCapacity(int capacity)
        {
            var updated = this;
            updated.QueueCapacity = capacity;
            return updated;
        }
        
        /// <summary>
        /// Updates max subscribers
        /// </summary>
        public MessageBusMetricsData WithMaxSubscribers(int maxSubs)
        {
            var updated = this;
            updated.MaxSubscribers = maxSubs;
            return updated;
        }
        
        /// <summary>
        /// Resets the metrics with a new reset time
        /// </summary>
        public MessageBusMetricsData Reset(float resetTime)
        {
            var updated = this;
            
            // Reset counters
            updated.TotalPublishes = 0;
            updated.TotalDeliveries = 0;
            updated.TotalFailures = 0;
            updated.TotalSubscriptions = 0;
            updated.TotalUnsubscriptions = 0;
            
            // Reset timing
            updated.TotalPublishTimeMs = 0.0;
            updated.TotalDeliveryTimeMs = 0.0;
            updated.TotalSubscriptionTimeMs = 0.0;
            updated.TotalUnsubscriptionTimeMs = 0.0;
            
            // Reset sample counts
            updated.PublishSampleCount = 0;
            updated.DeliverySampleCount = 0;
            updated.SubscriptionSampleCount = 0;
            updated.UnsubscriptionSampleCount = 0;
            
            // Reset averages
            updated.AveragePublishTimeMs = 0f;
            updated.AverageDeliveryTimeMs = 0f;
            updated.AverageSubscriptionTimeMs = 0f;
            updated.AverageUnsubscriptionTimeMs = 0f;
            
            // Reset min values
            updated.MinPublishTimeMs = float.MaxValue;
            updated.MinDeliveryTimeMs = float.MaxValue;
            updated.MinSubscriptionTimeMs = float.MaxValue;
            updated.MinUnsubscriptionTimeMs = float.MaxValue;
            
            // Reset max values
            updated.MaxPublishTimeMs = 0f;
            updated.MaxDeliveryTimeMs = 0f;
            updated.MaxSubscriptionTimeMs = 0f;
            updated.MaxUnsubscriptionTimeMs = 0f;
            
            // Reset time tracking
            updated.LastResetTime = resetTime;
            updated.LastOperationTime = resetTime;
            
            // Reset success ratio
            updated.DeliverySuccessRatio = 1.0f;
            
            return updated;
        }
        
        /// <summary>
        /// Gets the delivery success ratio
        /// </summary>
        public float GetDeliverySuccessRatio()
        {
            if (TotalDeliveries == 0) return 1.0f;
            return (float)(TotalDeliveries - TotalFailures) / TotalDeliveries;
        }
        
        /// <summary>
        /// Gets the efficiency ratio (delivered messages / published messages)
        /// </summary>
        public float GetEfficiency()
        {
            if (TotalPublishes == 0) return 1.0f;
            return (float)TotalDeliveries / TotalPublishes;
        }
        
        /// <summary>
        /// Calculates the efficiency ratio of the message bus (legacy property)
        /// </summary>
        public float EfficiencyRatio => GetEfficiency();
        
        /// <summary>
        /// Calculates the average subscribers per message (legacy property)
        /// </summary>
        public float AverageSubscribersPerMessage
        {
            get
            {
                if (TotalPublishes == 0) return 0f;
                return (float)TotalDeliveries / TotalPublishes;
            }
        }
        
        public bool Equals(MessageBusMetricsData other)
        {
            return BusId.Equals(other.BusId);
        }
        
        public override bool Equals(object obj)
        {
            return obj is MessageBusMetricsData other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return BusId.GetHashCode();
        }
        
        public static bool operator ==(MessageBusMetricsData left, MessageBusMetricsData right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(MessageBusMetricsData left, MessageBusMetricsData right)
        {
            return !left.Equals(right);
        }
    }
}