using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Managed implementation of message bus metrics tracking.
    /// Provides thread-safe tracking of performance and usage metrics for message bus operations.
    /// </summary>
    public class MessageBusMetrics : IMessageBusMetrics
    {
        // Thread safety
        private readonly ReaderWriterLockSlim _metricsLock;
        
        // Storage
        private readonly Dictionary<Guid, MessageBusMetricsData> _busMetrics;
        private MessageBusMetricsData _globalMetrics;
        
        // Alert storage
        private readonly Dictionary<Guid, Dictionary<string, MetricAlert>> _busAlerts;
        
        // Message bus for alerts
        private readonly IMessageBus _messageBus;
        
        // State
        private bool _isCreated;
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// Creates a new message bus metrics tracker
        /// </summary>
        /// <param name="messageBus">Message bus for sending alerts</param>
        /// <param name="initialCapacity">Initial capacity for dictionary storage</param>
        public MessageBusMetrics(IMessageBus messageBus = null, int initialCapacity = 64)
        {
            // Create storage
            _busMetrics = new Dictionary<Guid, MessageBusMetricsData>(initialCapacity);
            _busAlerts = new Dictionary<Guid, Dictionary<string, MetricAlert>>();
            _metricsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _messageBus = messageBus;
            
            // Initialize global metrics
            float currentTime = GetCurrentTime();
            _globalMetrics = new MessageBusMetricsData(default, "Global")
            {
                CreationTime = currentTime,
                LastResetTime = currentTime
            };
            
            _isCreated = true;
        }
        
        /// <summary>
        /// Creates a new message bus metrics tracker with a specific bus already configured
        /// </summary>
        /// <param name="busId">Bus identifier</param>
        /// <param name="busName">Bus name</param>
        /// <param name="busType">Type of message bus</param>
        /// <param name="messageBus">Message bus for sending alerts</param>
        public MessageBusMetrics(
            Guid busId,
            string busName,
            string busType,
            IMessageBus messageBus = null)
            : this(messageBus)
        {
            // Configure the initial bus
            UpdateBusConfiguration(busId, 0, 0, busName, busType);
        }
        
        // IMessageBusMetrics Implementation
        
        /// <summary>
        /// Gets metrics data for a specific message bus instance
        /// </summary>
        public MessageBusMetricsData GetMetricsData(Guid busId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                    return metricsData;
                
                return default;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for a specific message bus with nullable return for error handling
        /// </summary>
        public MessageBusMetricsData? GetMessageBusMetrics(Guid busId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                    return metricsData;
                
                return null;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets global metrics data aggregated across all message buses
        /// </summary>
        public MessageBusMetricsData GetGlobalMetricsData()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return _globalMetrics;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Records a message publish operation
        /// </summary>
        public void RecordPublish(Guid busId, string messageType, float publishTimeMs, int subscriberCount)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure bus exists
                EnsureBusMetricsExists(busId);
                
                // Update metrics
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordPublish(messageType, publishTimeMs, subscriberCount, currentTime);
                    _busMetrics[busId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(busId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a message delivery operation
        /// </summary>
        public void RecordDelivery(Guid busId, string messageType, float deliveryTimeMs, bool successful)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure bus exists
                EnsureBusMetricsExists(busId);
                
                // Update metrics
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordDelivery(messageType, deliveryTimeMs, successful, currentTime);
                    _busMetrics[busId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(busId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a subscription operation
        /// </summary>
        public void RecordSubscription(Guid busId, string messageType, float subscriptionTimeMs)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure bus exists
                EnsureBusMetricsExists(busId);
                
                // Update metrics
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordSubscription(messageType, subscriptionTimeMs, currentTime);
                    _busMetrics[busId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(busId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records an unsubscription operation
        /// </summary>
        public void RecordUnsubscription(Guid busId, string messageType, float unsubscriptionTimeMs)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure bus exists
                EnsureBusMetricsExists(busId);
                
                // Update metrics
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordUnsubscription(messageType, unsubscriptionTimeMs, currentTime);
                    _busMetrics[busId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(busId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Updates message bus configuration and capacity
        /// </summary>
        public void UpdateBusConfiguration(Guid busId, int queueCapacity, int maxSubscribers, string busName = "", string busType = "")
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Get or create bus metrics
                MessageBusMetricsData metricsData;
                bool isNewBus = false;
                
                if (!_busMetrics.TryGetValue(busId, out metricsData))
                {
                    // Create new bus metrics
                    var busIdStr = busId.ToString();
                    var name = !string.IsNullOrEmpty(busName) ? busName : busIdStr;
                    
                    metricsData = new MessageBusMetricsData(busId, name)
                    {
                        CreationTime = GetCurrentTime(),
                        LastResetTime = GetCurrentTime()
                    };
                    
                    isNewBus = true;
                }
                
                // Update configuration values
                if (queueCapacity > 0)
                    metricsData = metricsData.WithQueueCapacity(queueCapacity);
                
                if (maxSubscribers > 0)
                    metricsData = metricsData.WithMaxSubscribers(maxSubscribers);
                
                if (!string.IsNullOrEmpty(busType))
                    metricsData.BusType = busType;
                
                // Store updated metrics
                _busMetrics[busId] = metricsData;
                
                // Update global metrics
                UpdateGlobalMetrics();
                
                // Check alerts
                CheckAlerts(busId, metricsData);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for all tracked message buses
        /// </summary>
        public IEnumerable<MessageBusMetricsData> GetAllBusMetrics()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return new List<MessageBusMetricsData>(_busMetrics.Values);
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Reset statistics for a specific message bus
        /// </summary>
        public void ResetBusStats(Guid busId)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    var resetMetrics = metricsData.Reset(GetCurrentTime());
                    _busMetrics[busId] = resetMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Reset statistics for all message buses
        /// </summary>
        public void ResetAllBusStats()
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                float currentTime = GetCurrentTime();
                var busIds = new List<Guid>(_busMetrics.Keys);
                
                foreach (var busId in busIds)
                {
                    if (_busMetrics.TryGetValue(busId, out var metricsData))
                    {
                        var resetMetrics = metricsData.Reset(currentTime);
                        _busMetrics[busId] = resetMetrics;
                    }
                }
                
                // Reset global metrics
                _globalMetrics = _globalMetrics.Reset(currentTime);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Reset global statistics
        /// </summary>
        public void ResetStats()
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                float currentTime = GetCurrentTime();
                _globalMetrics = _globalMetrics.Reset(currentTime);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Registers an alert for a specific metric threshold
        /// </summary>
        public void RegisterAlert(Guid busId, string metricName, double threshold)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                if (!_busAlerts.TryGetValue(busId, out var busAlertsDict))
                {
                    busAlertsDict = new Dictionary<string, MetricAlert>();
                    _busAlerts[busId] = busAlertsDict;
                }
                
                busAlertsDict[metricName] = new MetricAlert
                {
                    MetricName = metricName,
                    Threshold = threshold,
                    LastTriggered = 0f,
                    CooldownSeconds = 5f // Default cooldown
                };
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets the message delivery success ratio for a specific bus
        /// </summary>
        public float GetDeliverySuccessRatio(Guid busId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    return metricsData.GetDeliverySuccessRatio();
                }
                
                return 0f;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the message bus efficiency (successful operations / total operations)
        /// </summary>
        public float GetBusEfficiency(Guid busId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_busMetrics.TryGetValue(busId, out var metricsData))
                {
                    return metricsData.GetEfficiency();
                }
                
                return 0f;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        // Private Helper Methods
        
        private float GetCurrentTime()
        {
#if UNITY_2019_3_OR_NEWER
            return Time.time;
#else
            return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
        
        private void CheckInitialized()
        {
            if (!_isCreated)
                throw new InvalidOperationException("MessageBusMetrics has not been initialized");
        }
        
        private void EnsureBusMetricsExists(Guid busId)
        {
            if (!_busMetrics.ContainsKey(busId))
            {
                var busIdStr = busId.ToString();
                var metricsData = new MessageBusMetricsData(busId, busIdStr)
                {
                    CreationTime = GetCurrentTime(),
                    LastResetTime = GetCurrentTime()
                };
                
                _busMetrics[busId] = metricsData;
            }
        }
        
        private void UpdateGlobalMetrics()
        {
            // Aggregate metrics from all buses
            long totalPublishes = 0;
            long totalDeliveries = 0;
            long totalSubscriptions = 0;
            long totalUnsubscriptions = 0;
            long totalFailures = 0;
            double totalPublishTime = 0;
            double totalDeliveryTime = 0;
            double totalSubscriptionTime = 0;
            double totalUnsubscriptionTime = 0;
            
            foreach (var metricsData in _busMetrics.Values)
            {
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
            
            // Update global metrics
            var updatedGlobal = _globalMetrics;
            updatedGlobal.TotalPublishes = totalPublishes;
            updatedGlobal.TotalDeliveries = totalDeliveries;
            updatedGlobal.TotalSubscriptions = totalSubscriptions;
            updatedGlobal.TotalUnsubscriptions = totalUnsubscriptions;
            updatedGlobal.TotalFailures = totalFailures;
            updatedGlobal.TotalPublishTimeMs = totalPublishTime;
            updatedGlobal.TotalDeliveryTimeMs = totalDeliveryTime;
            updatedGlobal.TotalSubscriptionTimeMs = totalSubscriptionTime;
            updatedGlobal.TotalUnsubscriptionTimeMs = totalUnsubscriptionTime;
            
            _globalMetrics = updatedGlobal;
        }
        
        private void CheckAlerts(Guid busId, MessageBusMetricsData metricsData)
        {
            if (_messageBus == null || !_busAlerts.TryGetValue(busId, out var busAlerts))
                return;
    
            float currentTime = GetCurrentTime();
    
            foreach (var alert in busAlerts.Values)
            {
                // Skip if in cooldown
                if (currentTime - alert.LastTriggered < alert.CooldownSeconds)
                    continue;
        
                double currentValue = GetMetricValue(metricsData, alert.MetricName);
        
                if (currentValue > alert.Threshold)
                {
                    // Trigger alert
                    alert.LastTriggered = currentTime;
            
                    try
                    {
                        // Create a profiler tag for this alert
                        var profilerTag = new Profiling.ProfilerTag(
                            new ProfilerCategory("MessageBus"), 
                            $"Alert_{alert.MetricName}");
                
                        var alertMessage = new MessageBusAlertMessage(
                            profilerTag,
                            busId,
                            metricsData.Name.ToString(),
                            "MessageBus",
                            currentValue,
                            alert.Threshold,
                            alert.MetricName,
                            "Warning",
                            "Threshold exceeded");
                
                        var publisher = _messageBus.GetPublisher<MessageBusAlertMessage>();
                        publisher?.Publish(alertMessage);
                    }
                    catch
                    {
                        // Silently handle publication errors
                    }
                }
            }
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
    /// Internal class for managing metric alerts
    /// </summary>
    internal class MetricAlert
    {
        public string MetricName { get; set; }
        public double Threshold { get; set; }
        public float LastTriggered { get; set; }
        public float CooldownSeconds { get; set; }
    }
}