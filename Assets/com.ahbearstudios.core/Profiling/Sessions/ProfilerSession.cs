using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Wrapper for Unity's ProfilerMarker that provides scoped profiling with tagging.
    /// Properly utilizes ProfilerTag functionality and provides comprehensive session management.
    /// </summary>
    public class ProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBusService _messageBusService;
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private readonly Guid _sessionId;
        private readonly bool _isNullSession;

        /// <summary>
        /// Creates a new ProfilerSession
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="messageBusService">Message bus for sending profiling messages (null for disabled sessions)</param>
        internal ProfilerSession(ProfilerTag tag, IMessageBusService messageBusService)
        {
            _tag = tag;
            _messageBusService = messageBusService;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _isNullSession = messageBusService == null;
            
            // Only create marker and start timing if this isn't a null session
            if (!_isNullSession)
            {
                _marker = new ProfilerMarker(_tag.FullName);
                
                // Begin the profiler marker
                _marker.Begin();
                _startTimeNs = GetHighPrecisionTimestampNs();
                
                // Notify that session started via message bus
                try
                {
                    var publisher = _messageBusService.GetPublisher<ProfilerSessionStartedMessage>();
                    publisher?.Publish(new ProfilerSessionStartedMessage(_tag, _sessionId));
                }
                catch
                {
                    // Silently handle publication errors during session start
                }
            }
        }

        /// <summary>
        /// Factory method to create a session with a predefined tag
        /// </summary>
        /// <param name="predefinedTag">One of the predefined ProfilerTag constants</param>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <returns>A new ProfilerSession with the predefined tag</returns>
        public static ProfilerSession CreateWithPredefinedTag(ProfilerTag predefinedTag, IMessageBusService messageBusService)
        {
            return new ProfilerSession(predefinedTag, messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for rendering operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific rendering operation (defaults to "Main")</param>
        /// <returns>A new ProfilerSession for rendering</returns>
        public static ProfilerSession CreateForRendering(IMessageBusService messageBusService, string operation = "Main")
        {
            if (operation == "Main")
                return new ProfilerSession(ProfilerTag.RenderingMain, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Render, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for physics operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific physics operation (defaults to "Update")</param>
        /// <returns>A new ProfilerSession for physics</returns>
        public static ProfilerSession CreateForPhysics(IMessageBusService messageBusService, string operation = "Update")
        {
            if (operation == "Update")
                return new ProfilerSession(ProfilerTag.PhysicsUpdate, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Physics, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for animation operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific animation operation (defaults to "Update")</param>
        /// <returns>A new ProfilerSession for animation</returns>
        public static ProfilerSession CreateForAnimation(IMessageBusService messageBusService, string operation = "Update")
        {
            if (operation == "Update")
                return new ProfilerSession(ProfilerTag.AnimationUpdate, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Animation, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for AI operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific AI operation (defaults to "Update")</param>
        /// <returns>A new ProfilerSession for AI</returns>
        public static ProfilerSession CreateForAI(IMessageBusService messageBusService, string operation = "Update")
        {
            if (operation == "Update")
                return new ProfilerSession(ProfilerTag.AIUpdate, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Ai, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for gameplay operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific gameplay operation (defaults to "Update")</param>
        /// <returns>A new ProfilerSession for gameplay</returns>
        public static ProfilerSession CreateForGameplay(IMessageBusService messageBusService, string operation = "Update")
        {
            if (operation == "Update")
                return new ProfilerSession(ProfilerTag.GameplayUpdate, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Internal, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for UI operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific UI operation (defaults to "Update")</param>
        /// <returns>A new ProfilerSession for UI</returns>
        public static ProfilerSession CreateForUI(IMessageBusService messageBusService, string operation = "Update")
        {
            if (operation == "Update")
                return new ProfilerSession(ProfilerTag.UIUpdate, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Gui, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for loading operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific loading operation (defaults to "Main")</param>
        /// <returns>A new ProfilerSession for loading</returns>
        public static ProfilerSession CreateForLoading(IMessageBusService messageBusService, string operation = "Main")
        {
            if (operation == "Main")
                return new ProfilerSession(ProfilerTag.LoadingMain, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Loading, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for memory operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Specific memory operation (defaults to "Allocation")</param>
        /// <returns>A new ProfilerSession for memory</returns>
        public static ProfilerSession CreateForMemory(IMessageBusService messageBusService, string operation = "Allocation")
        {
            if (operation == "Allocation")
                return new ProfilerSession(ProfilerTag.MemoryAllocation, messageBusService);
            
            return new ProfilerSession(new ProfilerTag(ProfilerCategory.Memory, operation), messageBusService);
        }

        /// <summary>
        /// Factory method to create a session for network operations
        /// </summary>
        /// <param name="messageBusService">Message bus for sending profiling messages</param>
        /// <param name="operation">Network operation ("Send" or "Receive")</param>
        /// <returns>A new ProfilerSession for network operations</returns>
        public static ProfilerSession CreateForNetwork(IMessageBusService messageBusService, string operation)
        {
            switch (operation?.ToLowerInvariant())
            {
                case "send":
                    return new ProfilerSession(ProfilerTag.NetworkSend, messageBusService);
                case "receive":
                    return new ProfilerSession(ProfilerTag.NetworkReceive, messageBusService);
                default:
                    return new ProfilerSession(new ProfilerTag(ProfilerCategory.Network, operation ?? "Unknown"), messageBusService);
            }
        }

        /// <summary>
        /// Get the tag associated with this session
        /// </summary>
        public ProfilerTag Tag => _tag;
        
        /// <summary>
        /// Gets the elapsed time in milliseconds
        /// </summary>
        public double ElapsedMilliseconds
        {
            get
            {
                if (_isNullSession) return 0.0;
                
                long currentTimeNs = _isDisposed ? _endTimeNs : GetHighPrecisionTimestampNs();
                return (currentTimeNs - _startTimeNs) / 1000000.0;
            }
        }
        
        /// <summary>
        /// Gets the elapsed time in nanoseconds
        /// </summary>
        public long ElapsedNanoseconds
        {
            get
            {
                if (_isNullSession) return 0L;
                
                return _isDisposed ? (_endTimeNs - _startTimeNs) : (GetHighPrecisionTimestampNs() - _startTimeNs);
            }
        }
        
        /// <summary>
        /// Gets whether this session has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Gets whether this is a null session (no profiling)
        /// </summary>
        public bool IsNullSession => _isNullSession;
        
        /// <summary>
        /// Records a custom metric with this session
        /// </summary>
        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName) || _isNullSession)
                return;
                
            _customMetrics[metricName] = value;
        }
        
        /// <summary>
        /// Gets all recorded metrics for this session
        /// </summary>
        /// <returns>Dictionary of metric names and values</returns>
        public IReadOnlyDictionary<string, double> GetMetrics()
        {
            return _customMetrics;
        }

        /// <summary>
        /// Records performance metrics for this session
        /// </summary>
        /// <param name="frameTime">Frame time in milliseconds</param>
        /// <param name="cpuUsage">CPU usage percentage</param>
        /// <param name="memoryUsage">Memory usage in bytes</param>
        public void RecordPerformanceMetrics(double frameTime, double cpuUsage, long memoryUsage)
        {
            if (_isNullSession) return;
            
            RecordMetric("FrameTime", frameTime);
            RecordMetric("CPUUsage", cpuUsage);
            RecordMetric("MemoryUsage", memoryUsage);
        }

        /// <summary>
        /// Records error information for this session
        /// </summary>
        /// <param name="errorType">Type of error</param>
        /// <param name="errorCount">Number of errors</param>
        public void RecordError(string errorType, int errorCount = 1)
        {
            if (_isNullSession) return;
            
            RecordMetric("HasError", 1.0);
            RecordMetric("ErrorCount", errorCount);
            if (!string.IsNullOrEmpty(errorType))
                RecordMetric($"ErrorType_{errorType}", errorCount);
        }

        /// <summary>
        /// Records throughput information for this session
        /// </summary>
        /// <param name="itemsProcessed">Number of items processed</param>
        /// <param name="bytesProcessed">Number of bytes processed</param>
        public void RecordThroughput(int itemsProcessed, long bytesProcessed = 0)
        {
            if (_isNullSession) return;
            
            RecordMetric("ItemsProcessed", itemsProcessed);
            if (bytesProcessed > 0)
                RecordMetric("BytesProcessed", bytesProcessed);
                
            // Calculate rates if we have timing information
            double elapsedSeconds = ElapsedMilliseconds / 1000.0;
            if (elapsedSeconds > 0)
            {
                RecordMetric("ItemsPerSecond", itemsProcessed / elapsedSeconds);
                if (bytesProcessed > 0)
                    RecordMetric("BytesPerSecond", bytesProcessed / elapsedSeconds);
            }
        }

        /// <summary>
        /// Records allocation information for this session
        /// </summary>
        /// <param name="allocatedBytes">Number of bytes allocated</param>
        /// <param name="allocatedObjects">Number of objects allocated</param>
        public void RecordAllocation(long allocatedBytes, int allocatedObjects = 0)
        {
            if (_isNullSession) return;
            
            RecordMetric("AllocatedBytes", allocatedBytes);
            if (allocatedObjects > 0)
                RecordMetric("AllocatedObjects", allocatedObjects);
        }

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed || _isNullSession)
                return;

            _marker.End();
            _endTimeNs = GetHighPrecisionTimestampNs();
            _isDisposed = true;
            
            // Call protected method for derived classes
            OnDispose();
            
            // Notify that session ended via message bus
            if (_messageBusService != null)
            {
                try
                {
                    var publisher = _messageBusService.GetPublisher<ProfilerSessionCompletedMessage>();
                    publisher?.Publish(new ProfilerSessionCompletedMessage(_tag, ElapsedMilliseconds, _customMetrics, _sessionId));
                }
                catch
                {
                    // Silently handle publication errors during session completion
                }
            }
        }
        
        /// <summary>
        /// Protected method for session cleanup tasks in derived classes
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// Gets high precision timestamp in nanoseconds
        /// </summary>
        private static long GetHighPrecisionTimestampNs()
        {
            long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            return (long)((double)timestamp / frequency * 1_000_000_000);
        }
    }
    
    /// <summary>
    /// Static helper class for profiling utility methods with comprehensive ProfilerTag support
    /// </summary>
    public static class ProfilerSessionExtensions
    {
        /// <summary>
        /// Profile a block of code with a custom tag
        /// </summary>
        public static void Profile(this Action action, ProfilerTag tag, IMessageBusService messageBusService = null)
        {
            if (action == null) return;
            
            using (new ProfilerSession(tag, messageBusService))
            {
                action();
            }
        }
        
        /// <summary>
        /// Profile a function with a return value
        /// </summary>
        public static T Profile<T>(this Func<T> func, ProfilerTag tag, IMessageBusService messageBusService = null)
        {
            if (func == null) return default(T);
            
            using (new ProfilerSession(tag, messageBusService))
            {
                return func();
            }
        }

        /// <summary>
        /// Profile a block of code with a predefined ProfilerTag
        /// </summary>
        public static void ProfileWithPredefinedTag(this Action action, ProfilerTag predefinedTag, IMessageBusService messageBusService = null)
        {
            action.Profile(predefinedTag, messageBusService);
        }

        /// <summary>
        /// Profile a rendering operation
        /// </summary>
        public static void ProfileRendering(this Action action, IMessageBusService messageBusService = null, string operation = "Main")
        {
            using (ProfilerSession.CreateForRendering(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a physics operation
        /// </summary>
        public static void ProfilePhysics(this Action action, IMessageBusService messageBusService = null, string operation = "Update")
        {
            using (ProfilerSession.CreateForPhysics(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile an animation operation
        /// </summary>
        public static void ProfileAnimation(this Action action, IMessageBusService messageBusService = null, string operation = "Update")
        {
            using (ProfilerSession.CreateForAnimation(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile an AI operation
        /// </summary>
        public static void ProfileAI(this Action action, IMessageBusService messageBusService = null, string operation = "Update")
        {
            using (ProfilerSession.CreateForAI(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a gameplay operation
        /// </summary>
        public static void ProfileGameplay(this Action action, IMessageBusService messageBusService = null, string operation = "Update")
        {
            using (ProfilerSession.CreateForGameplay(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a UI operation
        /// </summary>
        public static void ProfileUI(this Action action, IMessageBusService messageBusService = null, string operation = "Update")
        {
            using (ProfilerSession.CreateForUI(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a loading operation
        /// </summary>
        public static void ProfileLoading(this Action action, IMessageBusService messageBusService = null, string operation = "Main")
        {
            using (ProfilerSession.CreateForLoading(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a memory operation
        /// </summary>
        public static void ProfileMemory(this Action action, IMessageBusService messageBusService = null, string operation = "Allocation")
        {
            using (ProfilerSession.CreateForMemory(messageBusService, operation))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Profile a network operation
        /// </summary>
        public static void ProfileNetwork(this Action action, string operation, IMessageBusService messageBusService = null)
        {
            using (ProfilerSession.CreateForNetwork(messageBusService, operation))
            {
                action?.Invoke();
            }
        }
    }
}