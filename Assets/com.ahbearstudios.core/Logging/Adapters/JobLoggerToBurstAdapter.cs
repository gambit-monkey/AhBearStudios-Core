using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Adapters
{
    /// <summary>
    /// Adapter that bridges JobLogger functionality to the IBurstLogger interface.
    /// Provides seamless integration between job-based logging and the standard logging infrastructure.
    /// </summary>
    public sealed class JobLoggerToBurstAdapter : IBurstLogger, IDisposable
    {
        private readonly JobLoggerManager _manager;
        private readonly object _syncLock = new object();
        private LogLevel _minimumLevel;
        private bool _isEnabled;
        private bool _disposed;
        
        /// <summary>
        /// Gets or sets the minimum log level that will be processed by this adapter.
        /// </summary>
        public LogLevel MinimumLevel
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
                    return _isEnabled && !_disposed;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    if (!_disposed)
                    {
                        _isEnabled = value;
                    }
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the JobLoggerToBurstAdapter class.
        /// </summary>
        /// <param name="manager">The JobLoggerManager to adapt.</param>
        /// <param name="minimumLevel">The minimum log level to process.</param>
        public JobLoggerToBurstAdapter(JobLoggerManager manager, LogLevel minimumLevel = LogLevel.Debug)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _minimumLevel = minimumLevel;
            _isEnabled = true;
            _disposed = false;
        }
        
        /// <inheritdoc />
        public void Log(LogLevel level, string message, string tag)
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
        public void Log(LogLevel level, string message, string tag, LogProperties properties)
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
        public bool IsEnabled(LogLevel level)
        {
            lock (_syncLock)
            {
                // Check if disposed
                if (_disposed)
                    return false;
                    
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
            if (_disposed)
                return;
                
            try
            {
                _manager?.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing JobLoggerToBurstAdapter: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes the adapter and disables further logging operations.
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_disposed)
                    return;
                    
                try
                {
                    // Flush any remaining messages before disposing
                    Flush();
                    
                    // Disable the adapter
                    _isEnabled = false;
                    _disposed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing JobLoggerToBurstAdapter: {ex.Message}");
                }
            }
        }
    }
}