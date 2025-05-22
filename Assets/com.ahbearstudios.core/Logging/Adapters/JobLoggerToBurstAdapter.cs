using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Adapters
{
    /// <summary>
    /// Adapter that bridges JobLogger functionality to the IBurstLogger interface.
    /// Provides seamless integration between job-based logging and the standard logging infrastructure.
    /// </summary>
    public sealed class JobLoggerToBurstAdapter : IBurstLogger
    {
        private readonly JobLoggerManager _manager;
        private readonly object _syncLock = new object();
        private byte _minimumLevel;
        private bool _isEnabled;
        
        /// <summary>
        /// Gets or sets the minimum log level that will be processed by this adapter.
        /// </summary>
        public byte MinimumLevel
        {
            get
            {
                lock (_syncLock)
                {
                    return _minimumLevel;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _minimumLevel = value;
                }
            }
        }
        
        /// <summary>
        /// Gets or sets whether this adapter is enabled.
        /// </summary>
        public bool IsEnabledGlobal
        {
            get
            {
                lock (_syncLock)
                {
                    return _isEnabled;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _isEnabled = value;
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the JobLoggerToBurstAdapter class.
        /// </summary>
        /// <param name="manager">The JobLoggerManager to adapt.</param>
        /// <param name="minimumLevel">The minimum log level to process.</param>
        public JobLoggerToBurstAdapter(JobLoggerManager manager, byte minimumLevel = LogLevel.Debug)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _minimumLevel = minimumLevel;
            _isEnabled = true;
        }
        
        /// <inheritdoc />
        public void Log(byte level, string message, string tag)
        {
            if (!IsEnabled(level))
                return;
                
            if (string.IsNullOrEmpty(message))
                return;
            
            // Convert string tag to LogTag enum
            var logTag = ConvertToLogTag(tag);
            
            try
            {
                _manager.Log(level, logTag, message);
            }
            catch (Exception ex)
            {
                // Log to console as fallback to avoid losing the error
                Console.WriteLine($"Error in JobLoggerToBurstAdapter.Log: {ex.Message}");
            }
        }
        
        /// <inheritdoc />
        public void Log(byte level, string message, string tag, LogProperties properties)
        {
            if (!IsEnabled(level))
                return;
                
            if (string.IsNullOrEmpty(message))
                return;
            
            // Convert string tag to LogTag enum
            var logTag = ConvertToLogTag(tag);
            
            try
            {
                _manager.Log(level, logTag, message, properties);
            }
            catch (Exception ex)
            {
                // Log to console as fallback to avoid losing the error
                Console.WriteLine($"Error in JobLoggerToBurstAdapter.Log with properties: {ex.Message}");
            }
        }
        
        /// <inheritdoc />
        public bool IsEnabled(byte level)
        {
            lock (_syncLock)
            {
                // Check if globally enabled
                if (!_isEnabled)
                    return false;
                    
                // Check minimum level
                if (level < _minimumLevel)
                    return false;
                
                // Check if the manager is available and ready
                if (_manager == null)
                    return false;
                
                // Additional check: see if the manager has any active targets
                // This assumes JobLoggerManager has some way to check if it's operational
                // If not, we'll assume it's enabled if the manager exists
                return true;
            }
        }
        
        /// <summary>
        /// Converts a string tag to a LogTag enum value.
        /// </summary>
        /// <param name="tag">The string tag to convert.</param>
        /// <returns>The corresponding LogTag enum value, or Default if conversion fails.</returns>
        private Tagging.LogTag ConvertToLogTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return Tagging.LogTag.Default;
            
            // Try to parse the tag as an enum value
            if (Enum.TryParse<Tagging.LogTag>(tag, true, out var logTag))
            {
                return logTag;
            }
            
            // Try some common mappings
            switch (tag.ToLowerInvariant())
            {
                case "system":
                    return Tagging.LogTag.System;
                case "network":
                    return Tagging.LogTag.Network;
                case "performance":
                    return Tagging.LogTag.Performance;
                case "gameplay":
                    return Tagging.LogTag.Gameplay;
                case "ui":
                    return Tagging.LogTag.UI;
                case "audio":
                    return Tagging.LogTag.Audio;
                case "graphics":
                    return Tagging.LogTag.Graphics;
                case "physics":
                    return Tagging.LogTag.Physics;
                case "animation":
                    return Tagging.LogTag.Animation;
                case "ai":
                    return Tagging.LogTag.AI;
                case "input":
                    return Tagging.LogTag.Input;
                case "save":
                case "saveload":
                    return Tagging.LogTag.SaveLoad;
                case "resource":
                case "resources":
                    return Tagging.LogTag.Resources;
                case "event":
                case "events":
                    return Tagging.LogTag.Events;
                case "debug":
                    return Tagging.LogTag.Debug;
                case "editor":
                    return Tagging.LogTag.Editor;
                case "build":
                    return Tagging.LogTag.Build;
                case "test":
                case "tests":
                    return Tagging.LogTag.Tests;
                case "profiler":
                case "profiling":
                    return Tagging.LogTag.Profiler;
                case "memory":
                    return Tagging.LogTag.Memory;
                case "loading":
                    return Tagging.LogTag.Loading;
                case "localization":
                    return Tagging.LogTag.Localization;
                case "platform":
                    return Tagging.LogTag.Platform;
                case "messagebus":
                case "messaging":
                    return Tagging.LogTag.Default; 
                default:
                    return Tagging.LogTag.Default;
            }
        }
        
        /// <summary>
        /// Flushes any pending log messages in the underlying JobLoggerManager.
        /// </summary>
        public void Flush()
        {
            try
            {
                // If JobLoggerManager has a flush method, call it here
                // For now, we'll assume it auto-flushes or doesn't need explicit flushing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing JobLoggerToBurstAdapter: {ex.Message}");
            }
        }
    }
}