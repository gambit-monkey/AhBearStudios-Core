using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Managed adapter to bridge between NativePoolMetrics and ThresholdAlertSystem
    /// </summary>
    public class NativePoolMetricsAlertAdapter : IDisposable
    {
        private readonly NativePoolMetrics _nativeMetrics;
        private readonly ThresholdAlertSystem _alertSystem;
        private readonly IMessageBus _messageBus;
        private bool _disposed;

        /// <summary>
        /// Creates a new adapter to connect native pool metrics to the threshold alert system
        /// </summary>
        /// <param name="nativeMetrics">Reference to native pool metrics to monitor</param>
        /// <param name="alertSystem">Reference to the alert system</param>
        /// <param name="messageBus">Reference to the message bus for publishing alerts</param>
        public NativePoolMetricsAlertAdapter(NativePoolMetrics nativeMetrics, ThresholdAlertSystem alertSystem, IMessageBus messageBus)
        {
            _nativeMetrics = nativeMetrics;
            _alertSystem = alertSystem;
            _messageBus = messageBus;
            _disposed = false;
        }

        /// <summary>
        /// Process any queued alerts from the native metrics
        /// Call this method regularly, typically in an Update method
        /// </summary>
        public void ProcessAlerts()
        {
            if (_disposed) return;

            // Process all queued alerts
            _nativeMetrics.ProcessQueuedAlerts();
        }

        /// <summary>
        /// Translates native pool metric alerts to managed alert messages via the MessageBus
        /// </summary>
        /// <param name="poolId">Native pool ID</param>
        /// <param name="metricName">Name of the metric that triggered the alert</param>
        /// <param name="currentValue">Current value of the metric</param>
        /// <param name="thresholdValue">Threshold value that was exceeded</param>
        private void SendAlertMessage(FixedString64Bytes poolId, FixedString64Bytes metricName, double currentValue, double thresholdValue)
        {
            if (_messageBus != null)
            {
                var poolIdStr = poolId.ToString();
                var poolName = _nativeMetrics.GetMetricsData(poolId).Name.ToString();

                // Convert the FixedString to a standard string for the message
                var message = new PoolMetricAlertMessage(
                    Guid.Parse(poolIdStr),
                    poolName,
                    metricName.ToString(),
                    currentValue,
                    thresholdValue);

                _messageBus.PublishMessage(message);
            }
        }

        /// <summary>
        /// Dispose the adapter and clean up resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}