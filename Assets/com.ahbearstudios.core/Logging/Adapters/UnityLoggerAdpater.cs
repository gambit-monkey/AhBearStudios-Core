using System;
using UnityEngine;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Configuration;
using Object = UnityEngine.Object;

namespace AhBearStudios.Core.Logging.Adapters
{
    /// <summary>
    /// Adapter class that connects Unity's logging system to the core logging framework.
    /// Implements UnityEngine.ILogHandler to intercept Unity's log messages and can be registered
    /// with Debug.unityLogger to capture all Unity engine logs.
    /// </summary>
    public class UnityLoggerAdapter : ILogHandler, IDisposable
    {
        private readonly IBurstLogger _coreLogger;
        private readonly ILogHandler _originalLogHandler;
        private readonly bool _isRegisteredWithUnity;
        private readonly UnityConsoleTargetConfig _config;
        private bool _disposed;
        
        /// <summary>
        /// Gets whether this adapter is registered with the Unity logger.
        /// </summary>
        public bool IsRegisteredWithUnity => _isRegisteredWithUnity && !_disposed;

        /// <summary>
        /// Creates a new UnityLoggerAdapter with the specified core logger.
        /// </summary>
        /// <param name="coreLogger">The core logging implementation to forward logs to.</param>
        /// <param name="config">Optional configuration for the Unity console logger.</param>
        public UnityLoggerAdapter(IBurstLogger coreLogger, UnityConsoleTargetConfig config = null)
        {
            _coreLogger = coreLogger ?? throw new ArgumentNullException(nameof(coreLogger));
            _config = config;
            _disposed = false;
            
            // Store original log handler for cleanup
            _originalLogHandler = Debug.unityLogger.logHandler;
            
            // Check if we should register with Unity logging system
            if (config != null && config.RegisterUnityLogHandler)
            {
                RegisterWithUnity();
                _isRegisteredWithUnity = true;
            }
            else
            {
                _isRegisteredWithUnity = false;
            }
        }

        /// <summary>
        /// Registers this adapter with Unity's logging system to capture all Unity log messages.
        /// </summary>
        public void RegisterWithUnity()
        {
            if (_disposed)
                return;
                
            try
            {
                // Register this adapter as Unity's log handler
                Debug.unityLogger.logHandler = this;
                Debug.Log($"[{nameof(UnityLoggerAdapter)}] Successfully registered with Unity logger");
            }
            catch (Exception ex)
            {
                // If registration fails, log to original handler
                _originalLogHandler.LogFormat(LogType.Error, null, 
                    $"[{nameof(UnityLoggerAdapter)}] Failed to register with Unity logger: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores the original Unity log handler.
        /// </summary>
        public void RestoreOriginalHandler()
        {
            if (_isRegisteredWithUnity && _originalLogHandler != null && !_disposed)
            {
                try
                {
                    Debug.unityLogger.logHandler = _originalLogHandler;
                    Debug.Log($"[{nameof(UnityLoggerAdapter)}] Restored original Unity log handler");
                }
                catch (Exception ex)
                {
                    // Try to log the error without causing additional exceptions
                    try
                    {
                        _originalLogHandler.LogFormat(LogType.Error, null, 
                            $"[{nameof(UnityLoggerAdapter)}] Failed to restore original log handler: {ex.Message}");
                    }
                    catch
                    {
                        // Last resort if even that fails
                        Console.WriteLine($"Failed to restore original Unity log handler: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Updates the configuration for this adapter.
        /// </summary>
        /// <param name="config">The new configuration to apply.</param>
        public void UpdateConfiguration(UnityConsoleTargetConfig config)
        {
            if (_disposed)
                return;
                
            // This would be used to update runtime configuration
            // For now, we'll just store the reference for future use
            // In a more complete implementation, you might want to
            // re-register with Unity if the registration setting changed
        }

        /// <summary>
        /// Handles formatted log messages from Unity's logging system.
        /// Forwards them to the core logger with appropriate level and tag.
        /// </summary>
        /// <param name="logType">The Unity log type.</param>
        /// <param name="context">The Unity object context.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">Format arguments.</param>
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (_disposed)
            {
                // If disposed, just pass to original handler
                _originalLogHandler?.LogFormat(logType, context, format, args);
                return;
            }
            
            try
            {
                string message = string.Format(format, args);
                Tagging.LogTag tag = MapUnityLogTypeToTag(logType);
                LogLevel level = MapUnityLogTypeToLogLevel(logType);

                // Add context information if available
                if (context != null)
                {
                    message = $"{message} [Context: {context.name}]";
                }
        
                // Forward to core logger - convert the tag to a string
                _coreLogger.Log(level, message, tag.ToString());
        
                // If not registered with Unity or in duplicated output mode, pass to original handler too
                if (!_isRegisteredWithUnity || (_config != null && _config.DuplicateToOriginalHandler))
                {
                    _originalLogHandler?.LogFormat(logType, context, format, args);
                }
            }
            catch (Exception ex)
            {
                // Prevent infinite recursion by using the original handler directly
                _originalLogHandler?.LogFormat(LogType.Exception, null, 
                    $"[{nameof(UnityLoggerAdapter)}] Error in LogFormat: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles exception logs from Unity's logging system.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">The Unity object context.</param>
        public void LogException(Exception exception, Object context)
        {
            if (_disposed)
            {
                // If disposed, just pass to original handler
                _originalLogHandler?.LogException(exception, context);
                return;
            }
            
            try
            {
                // Build message with stack trace
                string message = exception.ToString();
        
                // Add context information if available
                if (context != null)
                {
                    message = $"{message} [Context: {context.name}]";
                }
        
                // Log to core logger - convert the tag to a string
                _coreLogger.Log(LogLevel.Error, message, Tagging.LogTag.Exception.ToString());
        
                // If not registered with Unity or in duplicated output mode, pass to original handler too
                if (!_isRegisteredWithUnity || (_config != null && _config.DuplicateToOriginalHandler))
                {
                    _originalLogHandler?.LogException(exception, context);
                }
            }
            catch (Exception ex)
            {
                // Prevent infinite recursion by using the original handler directly
                _originalLogHandler?.LogFormat(LogType.Exception, null, 
                    $"[{nameof(UnityLoggerAdapter)}] Error in LogException: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps Unity's LogType to the corresponding log level byte value.
        /// </summary>
        /// <param name="type">The Unity log type.</param>
        /// <returns>The corresponding log level byte value.</returns>
        private LogLevel MapUnityLogTypeToLogLevel(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return LogLevel.Error;
                case LogType.Assert: return LogLevel.Critical;
                case LogType.Warning: return LogLevel.Warning;
                case LogType.Log: return LogLevel.Info;
                case LogType.Exception: return LogLevel.Critical;
                default: return LogLevel.Info;
            }
        }
        
        /// <summary>
        /// Maps Unity's LogType to the appropriate tag.
        /// </summary>
        /// <param name="type">The Unity log type.</param>
        /// <returns>The corresponding log tag.</returns>
        private Tagging.LogTag MapUnityLogTypeToTag(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return Tagging.LogTag.Error;
                case LogType.Assert: return Tagging.LogTag.Assert;
                case LogType.Warning: return Tagging.LogTag.Warning;
                case LogType.Log: return Tagging.LogTag.Unity;
                case LogType.Exception: return Tagging.LogTag.Exception;
                default: return Tagging.LogTag.Unity;
            }
        }
        
        /// <summary>
        /// Disposes resources and restores the original Unity log handler if this adapter
        /// was registered with Unity.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            try
            {
                RestoreOriginalHandler();
                _disposed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing UnityLoggerAdapter: {ex.Message}");
                _disposed = true;
            }
        }
    }
}