
using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Metrics;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Implementation of the profiler interface using message bus for communication.
    /// Properly utilizes ProfilerTag predefined tags and provides comprehensive profiling capabilities.
    /// </summary>
    public class DefaultProfiler : IProfiler
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ProfilerStatsCollection _statsCollection = new ProfilerStatsCollection();
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<ProfilerTag, double> _metricAlerts = new Dictionary<ProfilerTag, double>();
        private readonly Dictionary<ProfilerTag, double> _sessionAlerts = new Dictionary<ProfilerTag, double>();
        private bool _isEnabled = false;
        private readonly int _maxHistoryItems = 100;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        /// <summary>
        /// Whether profiling is currently enabled
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Gets the message bus used by the profiler
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;

        /// <summary>
        /// Creates a new DefaultProfiler instance
        /// </summary>
        /// <param name="messageBusService">Message bus for publishing profiling messages</param>
        public DefaultProfiler(IMessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            SubscribeToMessages();
        }

        /// <summary>
        /// Begin a profiling sample with a name using the Scripts category
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            if (!IsEnabled)
                return new NoOpDisposable();

            if (string.IsNullOrEmpty(name))
                name = "UnnamedSample";

            // Use Scripts category for named samples, but check for predefined tags first
            var tag = GetOrCreateTag(name);
            return BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            if (!IsEnabled)
                return CreateNullSession(tag);

            return new ProfilerSession(tag, _messageBusService);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Unnamed";

            var tag = new ProfilerTag(category, name);
            return BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope using a predefined ProfilerTag
        /// </summary>
        /// <param name="predefinedTag">One of the predefined ProfilerTag constants</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginPredefinedScope(ProfilerTag predefinedTag)
        {
            return BeginScope(predefinedTag);
        }

        /// <summary>
        /// Begin a rendering profiling scope
        /// </summary>
        /// <param name="renderingOperation">Name of the rendering operation (defaults to "Main")</param>
        /// <returns>Profiler session for rendering operations</returns>
        public ProfilerSession BeginRenderingScope(string renderingOperation = "Main")
        {
            if (renderingOperation == "Main")
                return BeginScope(ProfilerTag.RenderingMain);
            
            return BeginScope(ProfilerCategory.Render, renderingOperation);
        }

        /// <summary>
        /// Begin a physics profiling scope
        /// </summary>
        /// <param name="physicsOperation">Name of the physics operation (defaults to "Update")</param>
        /// <returns>Profiler session for physics operations</returns>
        public ProfilerSession BeginPhysicsScope(string physicsOperation = "Update")
        {
            if (physicsOperation == "Update")
                return BeginScope(ProfilerTag.PhysicsUpdate);
            
            return BeginScope(ProfilerCategory.Physics, physicsOperation);
        }

        /// <summary>
        /// Begin an animation profiling scope
        /// </summary>
        /// <param name="animationOperation">Name of the animation operation (defaults to "Update")</param>
        /// <returns>Profiler session for animation operations</returns>
        public ProfilerSession BeginAnimationScope(string animationOperation = "Update")
        {
            if (animationOperation == "Update")
                return BeginScope(ProfilerTag.AnimationUpdate);
            
            return BeginScope(ProfilerCategory.Animation, animationOperation);
        }

        /// <summary>
        /// Begin an AI profiling scope
        /// </summary>
        /// <param name="aiOperation">Name of the AI operation (defaults to "Update")</param>
        /// <returns>Profiler session for AI operations</returns>
        public ProfilerSession BeginAIScope(string aiOperation = "Update")
        {
            if (aiOperation == "Update")
                return BeginScope(ProfilerTag.AIUpdate);
            
            return BeginScope(ProfilerCategory.Ai, aiOperation);
        }

        /// <summary>
        /// Begin a gameplay profiling scope
        /// </summary>
        /// <param name="gameplayOperation">Name of the gameplay operation (defaults to "Update")</param>
        /// <returns>Profiler session for gameplay operations</returns>
        public ProfilerSession BeginGameplayScope(string gameplayOperation = "Update")
        {
            if (gameplayOperation == "Update")
                return BeginScope(ProfilerTag.GameplayUpdate);
            
            return BeginScope(ProfilerCategory.Internal, gameplayOperation);
        }

        /// <summary>
        /// Begin a UI profiling scope
        /// </summary>
        /// <param name="uiOperation">Name of the UI operation (defaults to "Update")</param>
        /// <returns>Profiler session for UI operations</returns>
        public ProfilerSession BeginUIScope(string uiOperation = "Update")
        {
            if (uiOperation == "Update")
                return BeginScope(ProfilerTag.UIUpdate);
            
            return BeginScope(ProfilerCategory.Gui, uiOperation);
        }

        /// <summary>
        /// Begin a loading profiling scope
        /// </summary>
        /// <param name="loadingOperation">Name of the loading operation (defaults to "Main")</param>
        /// <returns>Profiler session for loading operations</returns>
        public ProfilerSession BeginLoadingScope(string loadingOperation = "Main")
        {
            if (loadingOperation == "Main")
                return BeginScope(ProfilerTag.LoadingMain);
            
            return BeginScope(ProfilerCategory.Loading, loadingOperation);
        }

        /// <summary>
        /// Begin a memory profiling scope
        /// </summary>
        /// <param name="memoryOperation">Name of the memory operation (defaults to "Allocation")</param>
        /// <returns>Profiler session for memory operations</returns>
        public ProfilerSession BeginMemoryScope(string memoryOperation = "Allocation")
        {
            if (memoryOperation == "Allocation")
                return BeginScope(ProfilerTag.MemoryAllocation);
            
            return BeginScope(ProfilerCategory.Memory, memoryOperation);
        }

        /// <summary>
        /// Begin a network profiling scope
        /// </summary>
        /// <param name="networkOperation">Name of the network operation ("Send" or "Receive")</param>
        /// <returns>Profiler session for network operations</returns>
        public ProfilerSession BeginNetworkScope(string networkOperation)
        {
            switch (networkOperation?.ToLowerInvariant())
            {
                case "send":
                    return BeginScope(ProfilerTag.NetworkSend);
                case "receive":
                    return BeginScope(ProfilerTag.NetworkReceive);
                default:
                    return BeginScope(ProfilerCategory.Network, networkOperation ?? "Unknown");
            }
        }

        /// <summary>
        /// Get metrics for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get metrics for</param>
        /// <returns>Profile metrics for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _statsCollection.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _statsCollection.GetAllGeneralMetrics();
        }

        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get history for</param>
        /// <returns>List of historical durations</returns>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            if (_history.TryGetValue(tag, out var history))
                return history;

            return Array.Empty<double>();
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _statsCollection.Reset();
            _history.Clear();
            
            // Publish stats reset message
            if (IsEnabled)
            {
                try
                {
                    var publisher = _messageBusService.GetPublisher<StatsResetMessage>();
                    publisher?.Publish(new StatsResetMessage());
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"DefaultProfiler: Failed to publish stats reset message: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            if (_isEnabled)
                return;

            _isEnabled = true;
            
            // Publish profiling started message
            try
            {
                var publisher = _messageBusService.GetPublisher<ProfilingStartedMessage>();
                publisher?.Publish(new ProfilingStartedMessage());
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"DefaultProfiler: Failed to publish profiling started message: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            if (!_isEnabled)
                return;

            _isEnabled = false;
            
            // Calculate total duration from all metrics
            double totalDuration = 0;
            foreach (var metrics in _statsCollection.GetAllGeneralMetrics().Values)
            {
                totalDuration += metrics.TotalValue;
            }
            
            // Publish profiling stopped message
            try
            {
                var publisher = _messageBusService.GetPublisher<ProfilingStoppedMessage>();
                publisher?.Publish(new ProfilingStoppedMessage(totalDuration));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"DefaultProfiler: Failed to publish profiling stopped message: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        /// <param name="metricTag">Tag for the metric to monitor</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            if (threshold <= 0)
                return;
                
            _metricAlerts[metricTag] = threshold;
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            if (thresholdMs <= 0)
                return;
                
            _sessionAlerts[sessionTag] = thresholdMs;
        }

        /// <summary>
        /// Register alerts for all predefined common operations
        /// </summary>
        /// <param name="thresholdMs">Threshold in milliseconds for all operations</param>
        public void RegisterCommonOperationAlerts(double thresholdMs)
        {
            if (thresholdMs <= 0)
                return;

            RegisterSessionAlert(ProfilerTag.RenderingMain, thresholdMs);
            RegisterSessionAlert(ProfilerTag.PhysicsUpdate, thresholdMs);
            RegisterSessionAlert(ProfilerTag.AnimationUpdate, thresholdMs);
            RegisterSessionAlert(ProfilerTag.AIUpdate, thresholdMs);
            RegisterSessionAlert(ProfilerTag.GameplayUpdate, thresholdMs);
            RegisterSessionAlert(ProfilerTag.UIUpdate, thresholdMs);
            RegisterSessionAlert(ProfilerTag.LoadingMain, thresholdMs * 5); // Loading expected to take longer
            RegisterSessionAlert(ProfilerTag.MemoryAllocation, thresholdMs);
            RegisterSessionAlert(ProfilerTag.NetworkSend, thresholdMs);
            RegisterSessionAlert(ProfilerTag.NetworkReceive, thresholdMs);
        }

        /// <summary>
        /// Register performance-sensitive operation alerts with different thresholds
        /// </summary>
        /// <param name="renderingThresholdMs">Threshold for rendering operations</param>
        /// <param name="physicsThresholdMs">Threshold for physics operations</param>
        /// <param name="networkThresholdMs">Threshold for network operations</param>
        public void RegisterPerformanceSensitiveAlerts(double renderingThresholdMs, double physicsThresholdMs, double networkThresholdMs)
        {
            RegisterSessionAlert(ProfilerTag.RenderingMain, renderingThresholdMs);
            RegisterSessionAlert(ProfilerTag.PhysicsUpdate, physicsThresholdMs);
            RegisterSessionAlert(ProfilerTag.NetworkSend, networkThresholdMs);
            RegisterSessionAlert(ProfilerTag.NetworkReceive, networkThresholdMs);
        }

        /// <summary>
        /// Gets or creates a ProfilerTag, checking predefined tags first
        /// </summary>
        /// <param name="name">Name to check against predefined tags</param>
        /// <returns>Existing predefined tag or new Scripts category tag</returns>
        private ProfilerTag GetOrCreateTag(string name)
        {
            // Check for predefined tags by name (case-insensitive)
            switch (name.ToLowerInvariant())
            {
                case "main":
                case "rendering":
                case "render":
                    return ProfilerTag.RenderingMain;
                    
                case "physics":
                case "physicsupdate":
                    return ProfilerTag.PhysicsUpdate;
                    
                case "animation":
                case "animationupdate":
                    return ProfilerTag.AnimationUpdate;
                    
                case "ai":
                case "aiupdate":
                    return ProfilerTag.AIUpdate;
                    
                case "gameplay":
                case "gameplayupdate":
                    return ProfilerTag.GameplayUpdate;
                    
                case "ui":
                case "uiupdate":
                    return ProfilerTag.UIUpdate;
                    
                case "loading":
                case "loadingmain":
                    return ProfilerTag.LoadingMain;
                    
                case "memory":
                case "allocation":
                case "memoryallocation":
                    return ProfilerTag.MemoryAllocation;
                    
                case "networksend":
                case "send":
                    return ProfilerTag.NetworkSend;
                    
                case "networkreceive":
                case "receive":
                    return ProfilerTag.NetworkReceive;
                    
                default:
                    return new ProfilerTag(ProfilerCategory.Scripts, name);
            }
        }

        /// <summary>
        /// Creates a null session for when profiling is disabled
        /// </summary>
        private ProfilerSession CreateNullSession(ProfilerTag tag)
        {
            return new ProfilerSession(tag, null);
        }

        /// <summary>
        /// Subscribe to profiler-related messages
        /// </summary>
        private void SubscribeToMessages()
        {
            try
            {
                // Subscribe to profiler session completed messages
                var sessionCompletedSub = _messageBusService.GetSubscriber<ProfilerSessionCompletedMessage>();
                if (sessionCompletedSub != null)
                {
                    sessionCompletedSub.Subscribe(OnSessionCompleted);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"DefaultProfiler: Failed to subscribe to messages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handler for session completed messages
        /// </summary>
        private void OnSessionCompleted(ProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            double durationMs = message.DurationMs;

            // Update metrics
            _statsCollection.AddSample(message.Tag, durationMs);

            // Update history
            if (!_history.TryGetValue(message.Tag, out var history))
            {
                history = new List<double>(_maxHistoryItems);
                _history[message.Tag] = history;
            }

            if (history.Count >= _maxHistoryItems)
                history.RemoveAt(0);

            history.Add(durationMs);

            // Check for session alerts
            if (_sessionAlerts.TryGetValue(message.Tag, out var threshold) && durationMs > threshold)
            {
                try
                {
                    var publisher = _messageBusService.GetPublisher<SessionAlertMessage>();
                    publisher?.Publish(new SessionAlertMessage(message.Tag, message.SessionId, durationMs, threshold));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"DefaultProfiler: Failed to publish session alert: {ex.Message}");
                }
            }
            
            // Check for custom metric alerts
            foreach (var metric in message.Metrics)
            {
                var metricTag = new ProfilerTag(message.Tag.Category, $"{message.Tag.Name}/{metric.Key}");
                if (_metricAlerts.TryGetValue(metricTag, out var metricThreshold) && metric.Value > metricThreshold)
                {
                    try
                    {
                        var publisher = _messageBusService.GetPublisher<MetricAlertMessage>();
                        publisher?.Publish(new MetricAlertMessage(metricTag, metric.Value, metricThreshold));
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"DefaultProfiler: Failed to publish metric alert: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            // Dispose of subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            // Clear collections
            _history.Clear();
            _metricAlerts.Clear();
            _sessionAlerts.Clear();
        }

        /// <summary>
        /// No-op disposable for when profiling is disabled
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}