using System;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Native implementation of message bus metrics tracking for use with Burst and Jobs
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct NativeMessageBusMetrics : INativeMessageBusMetrics
    {
        // Safety
        [NativeDisableUnsafePtrRestriction]
        private AtomicSafetyHandle m_Safety;
        [NativeSetThreadIndex]
        private int _threadIndex;
        
        // Storage
        private UnsafeParallelHashMap<FixedString64Bytes, MessageBusMetricsData> _busMetrics;
        private NativeReference<MessageBusMetricsData> _globalMetrics;
        
        // Alert storage
        private UnsafeParallelHashMap<FixedString64Bytes, UnsafeParallelHashMap<FixedString64Bytes, double>> _busAlerts;
        
        // Deferred alert processing
        private NativeQueue<AlertData> _alertQueue;
        
        // State
        private Allocator _allocatorLabel;
        private bool _isCreated;
        
        // Managed callback for alert handling
        [NativeDisableUnsafePtrRestriction]
        private IntPtr _alertCallbackFuncPtr;
        
        // Alert data structure for deferred processing
        private struct AlertData
        {
            public FixedString64Bytes BusId;
            public FixedString64Bytes MetricName;
            public double CurrentValue;
            public double ThresholdValue;
            public float Timestamp;
        }
        
        // Constructors
        /// <summary>
        /// Creates a new native message bus metrics tracker
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for the number of buses to track</param>
        /// <param name="allocator">Allocator to use</param>
        public NativeMessageBusMetrics(int initialCapacity, Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator", nameof(allocator));
        
            _allocatorLabel = allocator;
            _threadIndex = 0;
    
            // Create containers
            _busMetrics = new UnsafeParallelHashMap<FixedString64Bytes, MessageBusMetricsData>(initialCapacity, allocator);
            _globalMetrics = new NativeReference<MessageBusMetricsData>(allocator);
            _busAlerts = new UnsafeParallelHashMap<FixedString64Bytes, UnsafeParallelHashMap<FixedString64Bytes, double>>(initialCapacity, allocator);
            _alertQueue = new NativeQueue<AlertData>(allocator);
            
            // Set up safety
            m_Safety = AtomicSafetyHandle.Create();
            _alertCallbackFuncPtr = IntPtr.Zero;
            _isCreated = true;
    
            // Initialize global metrics
            float currentTime = GetCurrentTime();
            var globalMetricsData = new MessageBusMetricsData(default, "Global");
            globalMetricsData.CreationTime = currentTime;
            _globalMetrics.Value = globalMetricsData;
        }

        // Add a static version of GetTime for use in the constructor
        private static float GetCurrentTime()
        {
#if UNITY_2019_3_OR_NEWER
            return UnityEngine.Time.time;
#else
            return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
        
        /// <summary>
        /// Creates a new native message bus metrics tracker with default initial capacity
        /// </summary>
        /// <param name="allocator">Allocator to use</param>
        public NativeMessageBusMetrics(Allocator allocator) : this(64, allocator) { }
        
        /// <summary>
        /// Gets the allocator used by this native metrics instance
        /// </summary>
        public Allocator Allocator => _allocatorLabel;
        
        // INativeMessageBusMetrics Implementation
        
        /// <summary>
        /// Gets metrics data for a specific message bus
        /// </summary>
        public MessageBusMetricsData GetMetricsData(FixedString64Bytes busId)
        {
            CheckReadAccess();
            
            if (_busMetrics.TryGetValue(busId, out var metricsData))
                return metricsData;
                
            return default;
        }
        
        /// <summary>
        /// Gets global metrics data aggregated across all message buses
        /// </summary>
        public MessageBusMetricsData GetGlobalMetricsData()
        {
            CheckReadAccess();
            return _globalMetrics.Value;
        }
        
        /// <summary>
        /// Records a message publish operation for a bus
        /// </summary>
        public JobHandle RecordPublish(FixedString64Bytes busId, FixedString64Bytes messageType, float publishTimeMs, int subscriberCount, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(busId);
            writer = writer.PreparePublish(messageType, publishTimeMs, subscriberCount, GetTime());
            
            var job = new UpdateMessageBusMetricsJob { Writer = writer };
            var handle = job.Schedule(dependencies);
            
            // Queue metric check for deferred processing after the job completes
            QueueMetricCheck(busId);
            
            return handle;
        }
        
        /// <summary>
        /// Records a message delivery operation for a bus
        /// </summary>
        public JobHandle RecordDelivery(FixedString64Bytes busId, FixedString64Bytes messageType, float deliveryTimeMs, bool successful, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(busId);
            writer = writer.PrepareDelivery(messageType, deliveryTimeMs, successful, GetTime());
            
            var job = new UpdateMessageBusMetricsJob { Writer = writer };
            var handle = job.Schedule(dependencies);
            
            // Queue metric check for deferred processing after the job completes
            QueueMetricCheck(busId);
            
            return handle;
        }
        
        /// <summary>
        /// Records a subscription operation for a bus
        /// </summary>
        public JobHandle RecordSubscription(FixedString64Bytes busId, FixedString64Bytes messageType, float subscriptionTimeMs, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(busId);
            writer = writer.PrepareSubscription(messageType, subscriptionTimeMs, GetTime());
            
            var job = new UpdateMessageBusMetricsJob { Writer = writer };
            var handle = job.Schedule(dependencies);
            
            // Queue metric check for deferred processing after the job completes
            QueueMetricCheck(busId);
            
            return handle;
        }
        
        /// <summary>
        /// Records an unsubscription operation for a bus
        /// </summary>
        public JobHandle RecordUnsubscription(FixedString64Bytes busId, FixedString64Bytes messageType, float unsubscriptionTimeMs, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(busId);
            writer = writer.PrepareUnsubscription(messageType, unsubscriptionTimeMs, GetTime());
            
            var job = new UpdateMessageBusMetricsJob { Writer = writer };
            var handle = job.Schedule(dependencies);
            
            // Queue metric check for deferred processing after the job completes
            QueueMetricCheck(busId);
            
            return handle;
        }
        
        /// <summary>
        /// Updates message bus capacity and configuration
        /// </summary>
        public void UpdateBusConfiguration(FixedString64Bytes busId, int queueCapacity, int maxSubscribers = 0, FixedString64Bytes busName = default, FixedString64Bytes busType = default)
        {
            CheckWriteAccess();
            
            // Get existing or create new metrics data
            MessageBusMetricsData metricsData;
            bool isNewBus = false;
            
            if (!_busMetrics.TryGetValue(busId, out metricsData))
            {
                metricsData = new MessageBusMetricsData(default, busName.IsEmpty ? busId.ToString() : busName.ToString());
                metricsData.CreationTime = GetTime();
                isNewBus = true;
            }
            
            // Update configuration values
            if (queueCapacity > 0)
                metricsData = metricsData.WithQueueCapacity(queueCapacity);
                
            if (maxSubscribers > 0)
                metricsData = metricsData.WithMaxSubscribers(maxSubscribers);
                
            if (!busType.IsEmpty)
                metricsData.BusType = busType.ToString();
                
            // Store updated metrics
            if (isNewBus)
                _busMetrics.Add(busId, metricsData);
            else
            {
                _busMetrics.Remove(busId);
                _busMetrics.Add(busId, metricsData);
            }
            
            // Update global metrics
            UpdateGlobalMetrics();
            
            // Check alerts for this bus
            CheckAlerts(busId, metricsData);
        }
        
        /// <summary>
        /// Gets metrics data for all tracked message buses
        /// </summary>
        public NativeArray<MessageBusMetricsData> GetAllBusMetrics(Allocator allocator)
        {
            CheckReadAccess();
            
            var result = new NativeArray<MessageBusMetricsData>(_busMetrics.Count(), allocator);
            
            int index = 0;
            var keyValueArrays = _busMetrics.GetKeyValueArrays(Allocator.Temp);
            
            for (int i = 0; i < keyValueArrays.Length; i++)
            {
                result[index++] = keyValueArrays.Values[i];
            }
            
            keyValueArrays.Dispose();
            return result;
        }
        
        /// <summary>
        /// Reset statistics for a specific message bus
        /// </summary>
        public void ResetBusStats(FixedString64Bytes busId)
        {
            CheckWriteAccess();
            
            if (_busMetrics.TryGetValue(busId, out var metricsData))
            {
                var resetMetrics = metricsData.Reset(GetTime());
                _busMetrics.Remove(busId);
                _busMetrics.Add(busId, resetMetrics);
                
                // Update global metrics
                UpdateGlobalMetrics();
            }
        }
        
        /// <summary>
        /// Reset statistics for all message buses
        /// </summary>
        public void ResetAllBusStats()
        {
            CheckWriteAccess();
            
            float currentTime = GetTime();
            var keyValueArrays = _busMetrics.GetKeyValueArrays(Allocator.Temp);
            
            _busMetrics.Clear();
            
            for (int i = 0; i < keyValueArrays.Length; i++)
            {
                var resetMetrics = keyValueArrays.Values[i].Reset(currentTime);
                _busMetrics.Add(keyValueArrays.Keys[i], resetMetrics);
            }
            
            keyValueArrays.Dispose();
            
            // Reset global metrics
            var globalMetrics = _globalMetrics.Value;
            globalMetrics = globalMetrics.Reset(currentTime);
            _globalMetrics.Value = globalMetrics;
        }
        
        /// <summary>
        /// Reset global statistics
        /// </summary>
        public void ResetStats()
        {
            CheckWriteAccess();
            
            float currentTime = GetTime();
            var globalMetrics = _globalMetrics.Value;
            globalMetrics = globalMetrics.Reset(currentTime);
            _globalMetrics.Value = globalMetrics;
        }
        
        /// <summary>
        /// Registers an alert for a specific metric threshold
        /// </summary>
        public void RegisterAlert(FixedString64Bytes busId, FixedString64Bytes metricName, double threshold)
        {
            CheckWriteAccess();
            
            if (!_busAlerts.TryGetValue(busId, out var busAlertsMap))
            {
                busAlertsMap = new UnsafeParallelHashMap<FixedString64Bytes, double>(8, _allocatorLabel);
                _busAlerts.Add(busId, busAlertsMap);
            }
            
            if (busAlertsMap.ContainsKey(metricName))
                busAlertsMap.Remove(metricName);
            
            busAlertsMap.Add(metricName, threshold);
            
            // Update the map in the main dictionary
            _busAlerts.Remove(busId);
            _busAlerts.Add(busId, busAlertsMap);
        }
        
        /// <summary>
        /// Gets the message delivery success ratio for a specific bus
        /// </summary>
        public float GetDeliverySuccessRatio(FixedString64Bytes busId)
        {
            CheckReadAccess();
            
            if (_busMetrics.TryGetValue(busId, out var metricsData))
            {
                return metricsData.GetDeliverySuccessRatio();
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Gets the message bus efficiency
        /// </summary>
        public float GetBusEfficiency(FixedString64Bytes busId)
        {
            CheckReadAccess();
            
            if (_busMetrics.TryGetValue(busId, out var metricsData))
            {
                return metricsData.GetEfficiency();
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Whether the native metrics tracker is created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// Dispose of the native metrics tracker
        /// </summary>
        public void Dispose()
        {
            if (!_isCreated)
                return;
            
            // Dispose of all nested containers first
            if (_busAlerts.IsCreated)
            {
                var keyValueArrays = _busAlerts.GetKeyValueArrays(Allocator.Temp);
                for (int i = 0; i < keyValueArrays.Length; i++)
                {
                    if (keyValueArrays.Values[i].IsCreated)
                        keyValueArrays.Values[i].Dispose();
                }
                keyValueArrays.Dispose();
                _busAlerts.Dispose();
            }
            
            if (_busMetrics.IsCreated)
                _busMetrics.Dispose();
            
            if (_globalMetrics.IsCreated)
                _globalMetrics.Dispose();
            
            if (_alertQueue.IsCreated)
                _alertQueue.Dispose();
            
            AtomicSafetyHandle.Release(m_Safety);
            _isCreated = false;
        }
        
        // Private Helper Methods
        
        [BurstCompile]
        private float GetTime()
        {
#if UNITY_2019_3_OR_NEWER
            return UnityEngine.Time.time;
#else
            return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
        
        private void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }
        
        private void CheckWriteAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
        
        private MessageBusMetricsDataWriter CreateParallelWriter(FixedString64Bytes busId)
        {
            // Ensure bus exists
            if (!_busMetrics.TryGetValue(busId, out var metricsData))
            {
                metricsData = new MessageBusMetricsData(default, busId.ToString())
                {
                    CreationTime = GetTime(),
                    LastResetTime = GetTime()
                };
                
                _busMetrics.Add(busId, metricsData);
            }
            
            return new MessageBusMetricsDataWriter
            {
                BusId = busId,
                MetricsData = metricsData,
                BusMetrics = _busMetrics.AsParallelWriter()
            };
        }
        
        private void QueueMetricCheck(FixedString64Bytes busId)
        {
            // This would be implemented for deferred alert processing
            // For now, we'll do immediate checking
        }
        
        private void UpdateGlobalMetrics()
        {
            // Aggregate metrics from all buses
            var globalMetrics = _globalMetrics.Value;
            
            long totalPublishes = 0;
            long totalDeliveries = 0;
            long totalSubscriptions = 0;
            long totalUnsubscriptions = 0;
            long totalFailures = 0;
            double totalPublishTime = 0;
            double totalDeliveryTime = 0;
            double totalSubscriptionTime = 0;
            double totalUnsubscriptionTime = 0;
            
            var keyValueArrays = _busMetrics.GetKeyValueArrays(Allocator.Temp);
            
            for (int i = 0; i < keyValueArrays.Length; i++)
            {
                var metricsData = keyValueArrays.Values[i];
                totalPublishes += metricsData.TotalPublishes;
                totalDeliveries += metricsData.TotalDeliveries;
                totalSubscriptions += metricsData.TotalSubscriptions;
                totalUnsubscriptions += metricsData.TotalUnsubscriptions;
                totalFailures += metricsData.TotalFailures;
                totalPublishTime += metricsData.TotalPublishTimeMs;
                totalDeliveryTime += metricsData.TotalDeliveryTimeMs;
                totalSubscriptionTime += metricsData.TotalSubscriptionTimeMs;
                totalUnsubscriptionTime += metricsData.TotalUnsubscriptionTimeMs;
            }
            
            keyValueArrays.Dispose();
            
            // Update global metrics
            globalMetrics.TotalPublishes = totalPublishes;
            globalMetrics.TotalDeliveries = totalDeliveries;
            globalMetrics.TotalSubscriptions = totalSubscriptions;
            globalMetrics.TotalUnsubscriptions = totalUnsubscriptions;
            globalMetrics.TotalFailures = totalFailures;
            globalMetrics.TotalPublishTimeMs = totalPublishTime;
            globalMetrics.TotalDeliveryTimeMs = totalDeliveryTime;
            globalMetrics.TotalSubscriptionTimeMs = totalSubscriptionTime;
            globalMetrics.TotalUnsubscriptionTimeMs = totalUnsubscriptionTime;
            
            _globalMetrics.Value = globalMetrics;
        }
        
        private void CheckAlerts(FixedString64Bytes busId, MessageBusMetricsData metricsData)
        {
            if (!_busAlerts.TryGetValue(busId, out var busAlerts))
                return;
            
            float currentTime = GetTime();
            
            var alertKeyValueArrays = busAlerts.GetKeyValueArrays(Allocator.Temp);
            
            for (int i = 0; i < alertKeyValueArrays.Length; i++)
            {
                var metricName = alertKeyValueArrays.Keys[i];
                var threshold = alertKeyValueArrays.Values[i];
                
                double currentValue = GetMetricValue(metricsData, metricName.ToString());
                
                if (currentValue > threshold)
                {
                    // Queue alert for deferred processing
                    var alertData = new AlertData
                    {
                        BusId = busId,
                        MetricName = metricName,
                        CurrentValue = currentValue,
                        ThresholdValue = threshold,
                        Timestamp = currentTime
                    };
                    
                    _alertQueue.Enqueue(alertData);
                }
            }
            
            alertKeyValueArrays.Dispose();
        }
        
        private double GetMetricValue(MessageBusMetricsData metricsData, string metricName)
        {
            return metricName.ToLowerInvariant() switch
            {
                "totalpublishes" => metricsData.TotalPublishes,
                "totaldeliveries" => metricsData.TotalDeliveries,
                "totalsubscriptions" => metricsData.TotalSubscriptions,
                "totalunsubscriptions" => metricsData.TotalUnsubscriptions,
                "totalfailures" => metricsData.TotalFailures,
                "averagepublishtime" => metricsData.AveragePublishTimeMs,
                "averagedeliverytime" => metricsData.AverageDeliveryTimeMs,
                "averagesubscriptiontime" => metricsData.AverageSubscriptionTimeMs,
                "averageunsubscriptiontime" => metricsData.AverageUnsubscriptionTimeMs,
                "currentsubscribers" => metricsData.CurrentSubscribers,
                "currentqueuesize" => metricsData.CurrentQueueSize,
                _ => 0.0
            };
        }
    }
    
    /// <summary>
    /// Writer struct for updating message bus metrics data in parallel jobs
    /// </summary>
    [BurstCompile]
    public struct MessageBusMetricsDataWriter
    {
        public FixedString64Bytes BusId;
        public MessageBusMetricsData MetricsData;
        public UnsafeParallelHashMap<FixedString64Bytes, MessageBusMetricsData>.ParallelWriter BusMetrics;
        
        public MessageBusMetricsDataWriter PreparePublish(FixedString64Bytes messageType, float publishTimeMs, int subscriberCount, float currentTime)
        {
            MetricsData = MetricsData.RecordPublish(messageType.ToString(), publishTimeMs, subscriberCount, currentTime);
            return this;
        }
        
        public MessageBusMetricsDataWriter PrepareDelivery(FixedString64Bytes messageType, float deliveryTimeMs, bool successful, float currentTime)
        {
            MetricsData = MetricsData.RecordDelivery(messageType.ToString(), deliveryTimeMs, successful, currentTime);
            return this;
        }
        
        public MessageBusMetricsDataWriter PrepareSubscription(FixedString64Bytes messageType, float subscriptionTimeMs, float currentTime)
        {
            MetricsData = MetricsData.RecordSubscription(messageType.ToString(), subscriptionTimeMs, currentTime);
            return this;
        }
        
        public MessageBusMetricsDataWriter PrepareUnsubscription(FixedString64Bytes messageType, float unsubscriptionTimeMs, float currentTime)
        {
            MetricsData = MetricsData.RecordUnsubscription(messageType.ToString(), unsubscriptionTimeMs, currentTime);
            return this;
        }
    }
    
    /// <summary>
    /// Job for updating message bus metrics in parallel
    /// </summary>
    [BurstCompile]
    public struct UpdateMessageBusMetricsJob : IJob
    {
        public MessageBusMetricsDataWriter Writer;
        
        public void Execute()
        {
            Writer.BusMetrics.TryAdd(Writer.BusId, Writer.MetricsData);
        }
    }
}