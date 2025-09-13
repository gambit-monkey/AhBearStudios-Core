using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Concrete implementation of message bus monitoring service.
    /// Provides comprehensive monitoring, statistics collection, and anomaly detection
    /// for message bus performance with Unity-optimized patterns and zero-allocation operations.
    /// </summary>
    public sealed class MessageBusMonitoringService : IMessageBusMonitoringService
    {
        #region Fields

        private readonly MessageBusMonitoringConfig _config;
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        private readonly ConcurrentDictionary<string, MonitoringThreshold> _thresholds;
        private readonly ConcurrentDictionary<Type, MessageTypeStatistics> _messageTypeStats;
        private readonly ConcurrentQueue<PerformanceDataPoint> _historicalData;
        private readonly ConcurrentQueue<PerformanceAnomaly> _anomalies;
        private readonly ConcurrentDictionary<string, long> _memoryUsage;
        
        private readonly object _statisticsLock = new object();
        private readonly Timer _statisticsUpdateTimer;
        private readonly Timer _thresholdCheckTimer;
        private readonly Timer _anomalyDetectionTimer;
        private readonly Timer _memoryCleanupTimer;

        private volatile bool _disposed = false;
        private volatile bool _monitoringEnabled;

        // Core statistics
        private long _totalMessagesPublished;
        private long _totalMessagesProcessed;
        private long _totalMessagesFailed;
        private int _activePublishers;
        private int _activeSubscribers;
        private DateTime _lastStatsReset;

        // Performance tracking
        private readonly ConcurrentQueue<double> _processingTimes;
        private readonly ConcurrentQueue<DateTime> _messageTimestamps;
        private double _lastCalculatedErrorRate;
        private double _lastCalculatedAverageProcessingTime;
        private double _lastCalculatedMessageRate;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusMonitoringService.
        /// </summary>
        /// <param name="config">The monitoring configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="ArgumentException">Thrown when config is invalid</exception>
        public MessageBusMonitoringService(
            MessageBusMonitoringConfig config,
            ILoggingService logger,
            IProfilerService profilerService,
            IPoolingService poolingService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            
            if (!_config.IsValid())
                throw new ArgumentException("Invalid monitoring configuration", nameof(config));

            _monitoringEnabled = _config.MonitoringEnabled;
            _thresholds = new ConcurrentDictionary<string, MonitoringThreshold>();
            _messageTypeStats = new ConcurrentDictionary<Type, MessageTypeStatistics>();
            _historicalData = new ConcurrentQueue<PerformanceDataPoint>();
            _anomalies = new ConcurrentQueue<PerformanceAnomaly>();
            _memoryUsage = new ConcurrentDictionary<string, long>();
            _processingTimes = new ConcurrentQueue<double>();
            _messageTimestamps = new ConcurrentQueue<DateTime>();
            _lastStatsReset = DateTime.UtcNow;

            // Initialize custom thresholds
            foreach (var threshold in _config.CustomThresholds)
            {
                _thresholds.TryAdd(threshold.Key, threshold.Value);
            }

            // Initialize timers
            var statisticsInterval = (int)_config.StatisticsUpdateInterval.TotalMilliseconds;
            var thresholdInterval = (int)_config.ThresholdCheckInterval.TotalMilliseconds;
            var anomalyInterval = (int)_config.AnomalyDetectionInterval.TotalMilliseconds;

            _statisticsUpdateTimer = new Timer(UpdateStatistics, null, statisticsInterval, statisticsInterval);
            _thresholdCheckTimer = new Timer(CheckThresholdsCallback, null, thresholdInterval, thresholdInterval);
            _anomalyDetectionTimer = new Timer(DetectAnomaliesCallback, null, anomalyInterval, anomalyInterval);
            _memoryCleanupTimer = new Timer(PerformMemoryCleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInfo($"MessageBusMonitoringService initialized with monitoring enabled: {_monitoringEnabled}");
        }

        #endregion

        #region IMessageBusMonitoringService Implementation

        #region Statistics Collection

        /// <summary>
        /// Gets comprehensive statistics about message bus performance and health.
        /// </summary>
        /// <returns>Current message bus statistics</returns>
        public MessageBusStatistics GetStatistics()
        {
            if (!_monitoringEnabled) return CreateEmptyStatistics();

            lock (_statisticsLock)
            {
                return new MessageBusStatistics
                {
                    InstanceName = "MessageBusMonitoringService",
                    TotalMessagesPublished = _totalMessagesPublished,
                    TotalMessagesProcessed = _totalMessagesProcessed,
                    TotalMessagesFailed = _totalMessagesFailed,
                    ActiveSubscribers = _activeSubscribers,
                    DeadLetterQueueSize = 0, // TODO: Implement when DLQ service is available
                    MessagesInRetry = 0, // TODO: Implement when retry service is available
                    CurrentQueueDepth = 0, // TODO: Implement when queue service is available
                    MemoryUsage = GetCurrentMemoryUsage(),
                    CurrentHealthStatus = CalculateHealthStatus(),
                    MessageTypeStatistics = _messageTypeStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CircuitBreakerStates = new Dictionary<Type, CircuitBreakerState>(), // TODO: Implement when circuit breaker is available
                    ActiveScopes = 0, // TODO: Implement scope tracking
                    LastStatsReset = _lastStatsReset,
                    ErrorRate = _lastCalculatedErrorRate,
                    AverageProcessingTimeMs = _lastCalculatedAverageProcessingTime,
                    MessagesPerSecond = _lastCalculatedMessageRate,
                    SuccessRate = CalculateSuccessRate(),
                    FailureRate = _lastCalculatedErrorRate
                };
            }
        }

        /// <summary>
        /// Gets publishing-specific statistics.
        /// </summary>
        /// <returns>Current publishing statistics</returns>
        public MessagePublishingStatistics GetPublishingStatistics()
        {
            if (!_monitoringEnabled) return new MessagePublishingStatistics();

            return new MessagePublishingStatistics
            {
                TotalMessagesPublished = _totalMessagesPublished - _totalMessagesFailed,
                TotalMessagesFailedToPublish = _totalMessagesFailed,
                TotalBatchOperations = 0, // TODO: Track batch operations separately
                ActivePublishers = _activePublishers,
                AveragePublishingTimeMs = _lastCalculatedAverageProcessingTime,
                MessagesPerSecond = _lastCalculatedMessageRate,
                ErrorRate = _lastCalculatedErrorRate,
                MemoryUsageBytes = GetCurrentMemoryUsage(),
                CapturedAt = DateTime.UtcNow,
                LastResetAt = _lastStatsReset,
                MessageTypeStatistics = _messageTypeStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        /// <summary>
        /// Gets subscription-specific statistics.
        /// </summary>
        /// <returns>Current subscription statistics</returns>
        public MessageSubscriptionStatistics GetSubscriptionStatistics()
        {
            if (!_monitoringEnabled) return new MessageSubscriptionStatistics();

            return new MessageSubscriptionStatistics
            {
                TotalSubscriptionsCreated = 0, // TODO: Track subscription lifecycle
                TotalSubscriptionsDisposed = 0, // TODO: Track subscription lifecycle
                TotalMessagesProcessed = _totalMessagesProcessed - _totalMessagesFailed,
                TotalMessagesFailedToProcess = _totalMessagesFailed,
                ActiveSubscriptions = _activeSubscribers,
                ActiveScopes = 0, // TODO: Track active scopes
                AverageProcessingTimeMs = _lastCalculatedAverageProcessingTime,
                MessagesPerSecond = _lastCalculatedMessageRate,
                ErrorRate = _lastCalculatedErrorRate,
                MemoryUsageBytes = GetCurrentMemoryUsage(),
                CapturedAt = DateTime.UtcNow,
                LastResetAt = _lastStatsReset
            };
        }

        /// <summary>
        /// Clears message history and resets all statistics counters.
        /// </summary>
        public void ClearMessageHistory()
        {
            if (!_monitoringEnabled) return;

            lock (_statisticsLock)
            {
                Interlocked.Exchange(ref _totalMessagesPublished, 0);
                Interlocked.Exchange(ref _totalMessagesProcessed, 0);
                Interlocked.Exchange(ref _totalMessagesFailed, 0);
                
                _messageTypeStats.Clear();
                
                // Clear processing times and timestamps
                while (_processingTimes.TryDequeue(out _)) { }
                while (_messageTimestamps.TryDequeue(out _)) { }
                
                // Clear historical data
                while (_historicalData.TryDequeue(out _)) { }
                
                _lastStatsReset = DateTime.UtcNow;
                _lastCalculatedErrorRate = 0;
                _lastCalculatedAverageProcessingTime = 0;
                _lastCalculatedMessageRate = 0;
            }
        }

        /// <summary>
        /// Clears only publishing statistics.
        /// </summary>
        public void ClearPublishingStatistics()
        {
            if (!_monitoringEnabled) return;

            Interlocked.Exchange(ref _totalMessagesPublished, 0);
            Interlocked.Exchange(ref _activePublishers, 0);
        }

        /// <summary>
        /// Clears only subscription statistics.
        /// </summary>
        public void ClearSubscriptionStatistics()
        {
            if (!_monitoringEnabled) return;

            Interlocked.Exchange(ref _totalMessagesProcessed, 0);
            Interlocked.Exchange(ref _activeSubscribers, 0);
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Records a message publishing operation for metrics.
        /// </summary>
        /// <param name="messageType">The type of message published</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        /// <param name="batchSize">Size of batch (1 for single messages)</param>
        public void RecordPublishingOperation(Type messageType, bool success, double processingTimeMs, int batchSize = 1)
        {
            if (!_monitoringEnabled) return;

            Interlocked.Add(ref _totalMessagesPublished, batchSize);
            
            if (!success)
            {
                Interlocked.Add(ref _totalMessagesFailed, batchSize);
            }

            _processingTimes.Enqueue(processingTimeMs);
            _messageTimestamps.Enqueue(DateTime.UtcNow);

            // Update per-type statistics
            if (_config.TrackPerTypeStatistics && messageType != null)
            {
                _messageTypeStats.AddOrUpdate(messageType,
                    new MessageTypeStatistics(
                        processedCount: success ? batchSize : 0,
                        failedCount: success ? 0 : batchSize,
                        totalProcessingTime: processingTimeMs * batchSize),
                    (key, existing) => new MessageTypeStatistics(
                        processedCount: existing.ProcessedCount + (success ? batchSize : 0),
                        failedCount: existing.FailedCount + (success ? 0 : batchSize),
                        totalProcessingTime: existing.TotalProcessingTime + (processingTimeMs * batchSize),
                        peakProcessingTime: Math.Max(existing.PeakProcessingTime, processingTimeMs)));
            }

            // Limit queue sizes to prevent memory leaks
            LimitQueueSize(_processingTimes, 10000);
            LimitQueueSize(_messageTimestamps, 10000);
        }

        /// <summary>
        /// Records a message processing operation for metrics.
        /// </summary>
        /// <param name="messageType">The type of message processed</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        /// <param name="subscriberId">Identifier of the subscriber</param>
        public void RecordProcessingOperation(Type messageType, bool success, double processingTimeMs, string subscriberId)
        {
            if (!_monitoringEnabled) return;

            Interlocked.Increment(ref _totalMessagesProcessed);
            
            if (!success)
            {
                Interlocked.Increment(ref _totalMessagesFailed);
            }

            _processingTimes.Enqueue(processingTimeMs);
            _messageTimestamps.Enqueue(DateTime.UtcNow);

            // Update per-type statistics
            if (_config.TrackPerTypeStatistics && messageType != null)
            {
                _messageTypeStats.AddOrUpdate(messageType,
                    new MessageTypeStatistics(
                        processedCount: success ? 1 : 0,
                        failedCount: success ? 0 : 1,
                        totalProcessingTime: processingTimeMs),
                    (key, existing) => new MessageTypeStatistics(
                        processedCount: existing.ProcessedCount + (success ? 1 : 0),
                        failedCount: existing.FailedCount + (success ? 0 : 1),
                        totalProcessingTime: existing.TotalProcessingTime + processingTimeMs,
                        peakProcessingTime: Math.Max(existing.PeakProcessingTime, processingTimeMs)));
            }

            // Limit queue sizes
            LimitQueueSize(_processingTimes, 10000);
            LimitQueueSize(_messageTimestamps, 10000);
        }

        /// <summary>
        /// Records a subscription lifecycle event for metrics.
        /// </summary>
        /// <param name="messageType">The type of message subscribed to</param>
        /// <param name="operation">The operation (Create, Dispose, etc.)</param>
        /// <param name="subscriberId">Identifier of the subscriber</param>
        public void RecordSubscriptionOperation(Type messageType, string operation, string subscriberId)
        {
            if (!_monitoringEnabled) return;

            if (string.Equals(operation, "Create", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _activeSubscribers);
            }
            else if (string.Equals(operation, "Dispose", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Decrement(ref _activeSubscribers);
            }
        }

        /// <summary>
        /// Records memory usage metrics.
        /// </summary>
        /// <param name="component">The component reporting memory usage</param>
        /// <param name="memoryUsageBytes">Memory usage in bytes</param>
        public void RecordMemoryUsage(string component, long memoryUsageBytes)
        {
            if (!_monitoringEnabled || string.IsNullOrEmpty(component)) return;

            _memoryUsage.AddOrUpdate(component, memoryUsageBytes, (key, existing) => memoryUsageBytes);
        }

        #endregion

        #region Real-time Monitoring

        /// <summary>
        /// Gets the current message processing rate (messages per second).
        /// </summary>
        /// <returns>Current messages per second rate</returns>
        public double GetCurrentMessageRate()
        {
            if (!_monitoringEnabled) return 0;

            return _lastCalculatedMessageRate;
        }

        /// <summary>
        /// Gets the current error rate (0.0 to 1.0).
        /// </summary>
        /// <returns>Current error rate</returns>
        public double GetCurrentErrorRate()
        {
            if (!_monitoringEnabled) return 0;

            return _lastCalculatedErrorRate;
        }

        /// <summary>
        /// Gets the current average processing time in milliseconds.
        /// </summary>
        /// <returns>Current average processing time</returns>
        public double GetCurrentAverageProcessingTime()
        {
            if (!_monitoringEnabled) return 0;

            return _lastCalculatedAverageProcessingTime;
        }

        /// <summary>
        /// Gets the current total memory usage in bytes.
        /// </summary>
        /// <returns>Current memory usage</returns>
        public long GetCurrentMemoryUsage()
        {
            if (!_monitoringEnabled) return 0;

            return _memoryUsage.AsValueEnumerable().Sum(kvp => kvp.Value);
        }

        /// <summary>
        /// Gets the number of active publishers.
        /// </summary>
        /// <returns>Number of active publishers</returns>
        public int GetActivePublishersCount()
        {
            return _activePublishers;
        }

        /// <summary>
        /// Gets the number of active subscribers.
        /// </summary>
        /// <returns>Number of active subscribers</returns>
        public int GetActiveSubscribersCount()
        {
            return _activeSubscribers;
        }

        #endregion

        #region Historical Data

        /// <summary>
        /// Gets historical statistics for a specific time range.
        /// </summary>
        /// <param name="startTime">Start of the time range</param>
        /// <param name="endTime">End of the time range</param>
        /// <returns>Historical statistics</returns>
        public MessageBusStatistics GetHistoricalStatistics(DateTime startTime, DateTime endTime)
        {
            if (!_monitoringEnabled || !_config.TrackHistoricalStatistics)
                return CreateEmptyStatistics();

            var dataPoints = _historicalData.AsValueEnumerable()
                .Where(dp => dp.Timestamp >= startTime && dp.Timestamp <= endTime)
                .ToList();

            if (dataPoints.Count == 0)
                return CreateEmptyStatistics();

            var avgProcessingTime = dataPoints.AsValueEnumerable().Average(dp => dp.AverageProcessingTimeMs);
            var avgErrorRate = dataPoints.AsValueEnumerable().Average(dp => dp.ErrorRate);
            var avgMessageRate = dataPoints.AsValueEnumerable().Average(dp => dp.MessagesPerSecond);
            var totalMemoryUsage = dataPoints.AsValueEnumerable().Sum(dp => dp.MemoryUsageBytes);

            return new MessageBusStatistics
            {
                InstanceName = "MessageBusMonitoringService_Historical",
                TotalMessagesPublished = 0, // Historical aggregate not available
                TotalMessagesProcessed = 0, // Historical aggregate not available
                TotalMessagesFailed = 0, // Historical aggregate not available
                ActiveSubscribers = _activeSubscribers,
                ErrorRate = avgErrorRate,
                AverageProcessingTimeMs = avgProcessingTime,
                MessagesPerSecond = avgMessageRate,
                MemoryUsage = totalMemoryUsage,
                CurrentHealthStatus = CalculateHealthStatus(),
                LastStatsReset = _lastStatsReset,
                SuccessRate = 1.0 - avgErrorRate,
                FailureRate = avgErrorRate
            };
        }

        /// <summary>
        /// Gets performance trend data for the specified duration.
        /// </summary>
        /// <param name="duration">Duration to analyze</param>
        /// <param name="interval">Interval for data points</param>
        /// <returns>Performance trend data</returns>
        public IEnumerable<PerformanceDataPoint> GetPerformanceTrend(TimeSpan duration, TimeSpan interval)
        {
            if (!_monitoringEnabled || !_config.PerformanceTrendAnalysisEnabled)
                return Enumerable.Empty<PerformanceDataPoint>();

            var endTime = DateTime.UtcNow;
            var startTime = endTime - duration;

            return _historicalData.AsValueEnumerable()
                .Where(dp => dp.Timestamp >= startTime && dp.Timestamp <= endTime)
                .OrderBy(dp => dp.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Gets the top performing message types by throughput.
        /// </summary>
        /// <param name="count">Number of top performers to return</param>
        /// <returns>Top performing message types</returns>
        public IEnumerable<MessageTypePerformance> GetTopPerformingMessageTypes(int count = 10)
        {
            if (!_monitoringEnabled || !_config.TrackPerTypeStatistics)
                return Enumerable.Empty<MessageTypePerformance>();

            return _messageTypeStats.AsValueEnumerable()
                .Select(kvp => new MessageTypePerformance
                {
                    MessageType = kvp.Key,
                    TotalMessages = kvp.Value.TotalMessages,
                    SuccessRate = kvp.Value.SuccessRate,
                    AverageProcessingTimeMs = kvp.Value.AverageProcessingTime,
                    MessagesPerSecond = CalculateMessagesPerSecond(kvp.Value)
                })
                .OrderByDescending(mtp => mtp.MessagesPerSecond)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets the worst performing message types by error rate.
        /// </summary>
        /// <param name="count">Number of worst performers to return</param>
        /// <returns>Worst performing message types</returns>
        public IEnumerable<MessageTypePerformance> GetWorstPerformingMessageTypes(int count = 10)
        {
            if (!_monitoringEnabled || !_config.TrackPerTypeStatistics)
                return Enumerable.Empty<MessageTypePerformance>();

            return _messageTypeStats.AsValueEnumerable()
                .Select(kvp => new MessageTypePerformance
                {
                    MessageType = kvp.Key,
                    TotalMessages = kvp.Value.TotalMessages,
                    SuccessRate = kvp.Value.SuccessRate,
                    AverageProcessingTimeMs = kvp.Value.AverageProcessingTime,
                    MessagesPerSecond = CalculateMessagesPerSecond(kvp.Value)
                })
                .Where(mtp => mtp.TotalMessages > 0)
                .OrderBy(mtp => mtp.SuccessRate)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Alerts and Thresholds

        /// <summary>
        /// Sets a threshold for monitoring alerts.
        /// </summary>
        /// <param name="metric">The metric to monitor</param>
        /// <param name="threshold">The threshold value</param>
        /// <param name="comparisonType">How to compare against the threshold</param>
        public void SetMonitoringThreshold(string metric, double threshold, ThresholdComparisonType comparisonType)
        {
            if (string.IsNullOrEmpty(metric)) return;

            var monitoringThreshold = new MonitoringThreshold
            {
                Metric = metric,
                Threshold = threshold,
                ComparisonType = comparisonType,
                Enabled = true,
                LastTriggered = null
            };

            _thresholds.AddOrUpdate(metric, monitoringThreshold, (key, existing) => monitoringThreshold);
        }

        /// <summary>
        /// Removes a monitoring threshold.
        /// </summary>
        /// <param name="metric">The metric to stop monitoring</param>
        public void RemoveMonitoringThreshold(string metric)
        {
            if (string.IsNullOrEmpty(metric)) return;

            _thresholds.TryRemove(metric, out _);
        }

        /// <summary>
        /// Gets all currently configured monitoring thresholds.
        /// </summary>
        /// <returns>Dictionary of metric names to thresholds</returns>
        public Dictionary<string, MonitoringThreshold> GetMonitoringThresholds()
        {
            return _thresholds.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Manually checks all thresholds and triggers alerts if necessary.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of thresholds that triggered alerts</returns>
        public async UniTask<int> CheckThresholds(CancellationToken cancellationToken = default)
        {
            if (!_monitoringEnabled) return 0;

            int triggeredCount = 0;

            foreach (var threshold in _thresholds.Values)
            {
                if (!threshold.Enabled || cancellationToken.IsCancellationRequested)
                    continue;

                var currentValue = GetMetricValue(threshold.Metric);
                
                if (IsThresholdExceeded(currentValue, threshold))
                {
                    threshold.LastTriggered = DateTime.UtcNow;
                    ThresholdExceeded?.Invoke(threshold.Metric, currentValue, threshold.Threshold);
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Updates the monitoring configuration at runtime.
        /// </summary>
        /// <param name="config">The new monitoring configuration</param>
        public void UpdateConfiguration(MessageBusMonitoringConfig config)
        {
            if (config == null || !config.IsValid())
                throw new ArgumentException("Invalid configuration", nameof(config));

            // Update enabled state
            _monitoringEnabled = config.MonitoringEnabled;

            // Update custom thresholds
            _thresholds.Clear();
            foreach (var threshold in config.CustomThresholds)
            {
                _thresholds.TryAdd(threshold.Key, threshold.Value);
            }
        }

        /// <summary>
        /// Gets the current monitoring configuration.
        /// </summary>
        /// <returns>Current monitoring configuration</returns>
        public MessageBusMonitoringConfig GetConfiguration()
        {
            var config = _config.Clone();
            config.MonitoringEnabled = _monitoringEnabled;
            config.CustomThresholds = GetMonitoringThresholds();
            return config;
        }

        /// <summary>
        /// Enables or disables monitoring temporarily.
        /// </summary>
        /// <param name="enabled">Whether monitoring should be enabled</param>
        public void SetMonitoringEnabled(bool enabled)
        {
            _monitoringEnabled = enabled;
        }

        /// <summary>
        /// Gets whether monitoring is currently enabled.
        /// </summary>
        /// <returns>True if monitoring is enabled</returns>
        public bool IsMonitoringEnabled()
        {
            return _monitoringEnabled;
        }

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a monitoring threshold is exceeded.
        /// </summary>
        public event Action<string, double, double> ThresholdExceeded;

        /// <summary>
        /// Event fired when statistics are updated.
        /// </summary>
        public event Action<MessageBusStatistics> StatisticsUpdated;

        /// <summary>
        /// Event fired when a performance anomaly is detected.
        /// </summary>
        public event Action<PerformanceAnomaly> AnomalyDetected;

        #endregion

        #endregion

        #region Private Methods

        private MessageBusStatistics CreateEmptyStatistics()
        {
            return new MessageBusStatistics
            {
                InstanceName = "MessageBusMonitoringService_Disabled",
                CurrentHealthStatus = HealthChecking.Models.HealthStatus.Unknown,
                LastStatsReset = _lastStatsReset
            };
        }

        private HealthChecking.Models.HealthStatus CalculateHealthStatus()
        {
            if (!_monitoringEnabled)
                return HealthChecking.Models.HealthStatus.Unknown;

            var errorRate = _lastCalculatedErrorRate;
            var processingTime = _lastCalculatedAverageProcessingTime;

            if (errorRate >= _config.CriticalErrorRateThreshold || 
                processingTime >= _config.CriticalProcessingTimeThreshold)
            {
                return HealthChecking.Models.HealthStatus.Critical;
            }

            if (errorRate >= _config.WarningErrorRateThreshold ||
                processingTime >= _config.WarningProcessingTimeThreshold)
            {
                return HealthChecking.Models.HealthStatus.Warning;
            }

            return HealthChecking.Models.HealthStatus.Healthy;
        }

        private double CalculateSuccessRate()
        {
            var total = _totalMessagesPublished + _totalMessagesProcessed;
            if (total == 0) return 1.0;

            var successful = total - _totalMessagesFailed;
            return (double)successful / total;
        }

        private double CalculateMessagesPerSecond(MessageTypeStatistics stats)
        {
            // Simple calculation - could be enhanced with time tracking
            return stats.TotalMessages / Math.Max(1.0, (DateTime.UtcNow - _lastStatsReset).TotalSeconds);
        }

        private void UpdateStatistics(object state)
        {
            if (!_monitoringEnabled || _disposed) return;

            try
            {
                // Calculate current rates
                CalculateCurrentRates();

                // Create performance data point
                if (_config.TrackHistoricalStatistics)
                {
                    var dataPoint = new PerformanceDataPoint
                    {
                        Timestamp = DateTime.UtcNow,
                        MessagesPerSecond = _lastCalculatedMessageRate,
                        ErrorRate = _lastCalculatedErrorRate,
                        AverageProcessingTimeMs = _lastCalculatedAverageProcessingTime,
                        MemoryUsageBytes = GetCurrentMemoryUsage()
                    };

                    _historicalData.Enqueue(dataPoint);

                    // Limit historical data size
                    while (_historicalData.Count > _config.MaxHistoricalDataPoints)
                    {
                        _historicalData.TryDequeue(out _);
                    }
                }

                // Fire statistics updated event
                StatisticsUpdated?.Invoke(GetStatistics());
            }
            catch (Exception ex)
            {
                // Log error but don't throw - monitoring should not crash the system
                System.Diagnostics.Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void CalculateCurrentRates()
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Calculate message rate (messages per second in last minute)
            var recentMessages = _messageTimestamps.AsValueEnumerable()
                .Count(timestamp => timestamp >= oneMinuteAgo);
            _lastCalculatedMessageRate = recentMessages / 60.0;

            // Calculate average processing time
            var recentProcessingTimes = _processingTimes.ToArray();
            _lastCalculatedAverageProcessingTime = recentProcessingTimes.Length > 0
                ? recentProcessingTimes.AsValueEnumerable().Average()
                : 0;

            // Calculate error rate
            var totalOperations = _totalMessagesPublished + _totalMessagesProcessed;
            _lastCalculatedErrorRate = totalOperations > 0
                ? (double)_totalMessagesFailed / totalOperations
                : 0;
        }

        private void CheckThresholdsCallback(object state)
        {
            if (!_monitoringEnabled || _disposed) return;

            try
            {
                CheckThresholds().Forget();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking thresholds: {ex.Message}");
            }
        }

        private void DetectAnomaliesCallback(object state)
        {
            if (!_monitoringEnabled || !_config.AnomalyDetectionEnabled || _disposed) return;

            try
            {
                DetectAnomalies();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting anomalies: {ex.Message}");
            }
        }

        private void DetectAnomalies()
        {
            var now = DateTime.UtcNow;
            var baselineStart = now - _config.AnomalyDetectionBaselinePeriod;

            var baselineData = _historicalData.AsValueEnumerable()
                .Where(dp => dp.Timestamp >= baselineStart && dp.Timestamp < now.AddMinutes(-5))
                .ToList();

            if (baselineData.Count < 10) return; // Need sufficient baseline data

            var currentErrorRate = _lastCalculatedErrorRate;
            var currentProcessingTime = _lastCalculatedAverageProcessingTime;
            var currentMessageRate = _lastCalculatedMessageRate;

            var baselineErrorRate = baselineData.AsValueEnumerable().Average(dp => dp.ErrorRate);
            var baselineProcessingTime = baselineData.AsValueEnumerable().Average(dp => dp.AverageProcessingTimeMs);
            var baselineMessageRate = baselineData.AsValueEnumerable().Average(dp => dp.MessagesPerSecond);

            // Check for anomalies
            CheckForAnomaly("ErrorRate", currentErrorRate, baselineErrorRate);
            CheckForAnomaly("ProcessingTime", currentProcessingTime, baselineProcessingTime);
            CheckForAnomaly("MessageRate", currentMessageRate, baselineMessageRate);
        }

        private void CheckForAnomaly(string metric, double currentValue, double baselineValue)
        {
            if (baselineValue == 0) return;

            var deviationPercentage = Math.Abs((currentValue - baselineValue) / baselineValue) * 100;

            if (deviationPercentage >= _config.MinimumAnomalyDeviationPercentage)
            {
                var severity = CalculateAnomalySeverity(deviationPercentage);
                
                var anomaly = new PerformanceAnomaly
                {
                    Metric = metric,
                    CurrentValue = currentValue,
                    BaselineValue = baselineValue,
                    DeviationPercentage = deviationPercentage,
                    Severity = severity,
                    DetectedAt = DateTime.UtcNow
                };

                _anomalies.Enqueue(anomaly);

                // Limit anomalies queue size
                while (_anomalies.Count > _config.MaxTrackedAnomalies)
                {
                    _anomalies.TryDequeue(out _);
                }

                AnomalyDetected?.Invoke(anomaly);
            }
        }

        private AnomalySeverity CalculateAnomalySeverity(double deviationPercentage)
        {
            return deviationPercentage switch
            {
                >= 100 => AnomalySeverity.Critical,
                >= 50 => AnomalySeverity.High,
                >= 25 => AnomalySeverity.Medium,
                _ => AnomalySeverity.Low
            };
        }

        private void PerformMemoryCleanup(object state)
        {
            if (!_monitoringEnabled || !_config.AutoCleanupOnHighMemoryPressure || _disposed) return;

            try
            {
                var currentMemoryUsage = GetCurrentMemoryUsage();
                
                if (currentMemoryUsage > _config.MaxMonitoringMemoryPressure)
                {
                    var cleanupCount = (int)(_config.MemoryPressureCleanupPercentage * _historicalData.Count);
                    
                    for (int i = 0; i < cleanupCount; i++)
                    {
                        _historicalData.TryDequeue(out _);
                    }

                    var anomalyCleanupCount = (int)(_config.MemoryPressureCleanupPercentage * _anomalies.Count);
                    for (int i = 0; i < anomalyCleanupCount; i++)
                    {
                        _anomalies.TryDequeue(out _);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error performing memory cleanup: {ex.Message}");
            }
        }

        private double GetMetricValue(string metric)
        {
            return metric switch
            {
                "ErrorRate" => _lastCalculatedErrorRate,
                "ProcessingTime" => _lastCalculatedAverageProcessingTime,
                "MessageRate" => _lastCalculatedMessageRate,
                "MemoryUsage" => GetCurrentMemoryUsage(),
                "ActiveSubscribers" => _activeSubscribers,
                "ActivePublishers" => _activePublishers,
                _ => 0.0
            };
        }

        private bool IsThresholdExceeded(double currentValue, MonitoringThreshold threshold)
        {
            return threshold.ComparisonType switch
            {
                ThresholdComparisonType.GreaterThan => currentValue > threshold.Threshold,
                ThresholdComparisonType.LessThan => currentValue < threshold.Threshold,
                ThresholdComparisonType.Equals => Math.Abs(currentValue - threshold.Threshold) < 0.001,
                ThresholdComparisonType.GreaterThanOrEqual => currentValue >= threshold.Threshold,
                ThresholdComparisonType.LessThanOrEqual => currentValue <= threshold.Threshold,
                _ => false
            };
        }

        private static void LimitQueueSize<T>(ConcurrentQueue<T> queue, int maxSize)
        {
            while (queue.Count > maxSize)
            {
                queue.TryDequeue(out _);
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the monitoring service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _statisticsUpdateTimer?.Dispose();
            _thresholdCheckTimer?.Dispose();
            _anomalyDetectionTimer?.Dispose();
            _memoryCleanupTimer?.Dispose();

            _thresholds?.Clear();
            _messageTypeStats?.Clear();
            _memoryUsage?.Clear();

            while (_historicalData?.TryDequeue(out _) == true) { }
            while (_anomalies?.TryDequeue(out _) == true) { }
            while (_processingTimes?.TryDequeue(out _) == true) { }
            while (_messageTimestamps?.TryDequeue(out _) == true) { }
        }

        #endregion
    }
}