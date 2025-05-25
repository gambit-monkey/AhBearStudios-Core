using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AhBearStudios.Pooling.Core;
using UnityEngine;

namespace AhBearStudios.Pooling.Diagnostics
{
    /// <summary>
    /// Provides logging and diagnostic features for pool operations
    /// </summary>
    public class PoolLogger : IPoolLogger
    {
        /// <summary>
        /// Log level for pool operations
        /// </summary>
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
            Debug = 4,
            Verbose = 5
        }
        
        /// <summary>
        /// Log target for pool operations
        /// </summary>
        [Flags]
        public enum LogTarget
        {
            None = 0,
            Console = 1,
            File = 2,
            Memory = 4,
            DebuggerDisplay = 8,
            All = Console | File | Memory | DebuggerDisplay
        }
        
        private static readonly Dictionary<string, PoolLogger> _loggers = new Dictionary<string, PoolLogger>();
        private static LogLevel _globalLogLevel = LogLevel.Warning;
        private static LogTarget _globalLogTarget = LogTarget.Console;
        private static string _logFilePath;
        private static readonly List<string> _memoryLog = new List<string>();
        private static readonly int _maxMemoryLogEntries = 1000;
        
        private readonly string _poolName;
        private LogLevel _logLevel;
        private LogTarget _logTarget;
        private bool _includeTimestamps = true;
        private bool _includeStackTraceForErrors = true;
        
        /// <summary>
        /// Gets or sets the global log level for all pool loggers
        /// </summary>
        public static LogLevel GlobalLogLevel
        {
            get => _globalLogLevel;
            set => _globalLogLevel = value;
        }
        
        /// <summary>
        /// Gets or sets the global log target for all pool loggers
        /// </summary>
        public static LogTarget GlobalLogTarget
        {
            get => _globalLogTarget;
            set => _globalLogTarget = value;
        }
        
        /// <summary>
        /// Gets or sets the file path for log files
        /// </summary>
        public static string LogFilePath
        {
            get => _logFilePath ?? Path.Combine(Application.persistentDataPath, "PoolLogs");
            set => _logFilePath = value;
        }
        
        /// <summary>
        /// Gets the memory log buffer
        /// </summary>
        public static IReadOnlyList<string> MemoryLog => _memoryLog;
        
        /// <summary>
        /// Gets a logger instance for the specified pool
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>A logger instance</returns>
        public static PoolLogger GetLogger(string poolName)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                poolName = "DefaultPool";
            }
            
            if (!_loggers.TryGetValue(poolName, out var logger))
            {
                logger = new PoolLogger(poolName);
                _loggers[poolName] = logger;
            }
            
            return logger;
        }
        
        /// <summary>
        /// Clears all logger instances
        /// </summary>
        public static void ClearLoggers()
        {
            _loggers.Clear();
        }
        
        /// <summary>
        /// Clears the memory log
        /// </summary>
        public static void ClearMemoryLog()
        {
            _memoryLog.Clear();
        }
        
        /// <summary>
        /// Writes all memory logs to a file
        /// </summary>
        /// <param name="filePath">Optional file path</param>
        /// <returns>Path to the log file</returns>
        public static string WriteMemoryLogToFile(string filePath = null)
        {
            if (filePath == null)
            {
                string directory = LogFilePath;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                filePath = Path.Combine(directory, $"PoolLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }
            
            try
            {
                File.WriteAllLines(filePath, _memoryLog);
                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write memory log to file: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Logs an error message statically without requiring a logger instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            var logger = GetLogger("Global");
            logger.LogErrorInstance(message, context);
        }
        
        /// <summary>
        /// Logs a warning message statically without requiring a logger instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            var logger = GetLogger("Global");
            logger.LogWarningInstance(message, context);
        }
        
        /// <summary>
        /// Logs an info message statically without requiring a logger instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public static void LogInfo(string message, UnityEngine.Object context = null)
        {
            var logger = GetLogger("Global");
            logger.LogInfoInstance(message, context);
        }
        
        /// <summary>
        /// Logs a debug message statically without requiring a logger instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public static void LogDebug(string message, UnityEngine.Object context = null)
        {
            var logger = GetLogger("Global");
            logger.LogDebugInstance(message, context);
        }
        
        /// <summary>
        /// Creates a new pool logger
        /// </summary>
        public PoolLogger()
        {
            
        }
        
        /// <summary>
        /// Creates a new pool logger
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        public PoolLogger(string poolName) : this()
        {
            _poolName = poolName;
            _logLevel = _globalLogLevel;
            _logTarget = _globalLogTarget;
        }
        
        /// <summary>
        /// Gets or sets the log level for this logger
        /// </summary>
        public LogLevel Level
        {
            get => _logLevel;
            set => _logLevel = value;
        }
        
        /// <summary>
        /// Gets or sets the log target for this logger
        /// </summary>
        public LogTarget Target
        {
            get => _logTarget;
            set => _logTarget = value;
        }
        
        /// <summary>
        /// Gets or sets whether to include timestamps in log entries
        /// </summary>
        public bool IncludeTimestamps
        {
            get => _includeTimestamps;
            set => _includeTimestamps = value;
        }
        
        /// <summary>
        /// Gets or sets whether to include stack traces for error log entries
        /// </summary>
        public bool IncludeStackTraceForErrors
        {
            get => _includeStackTraceForErrors;
            set => _includeStackTraceForErrors = value;
        }
        
        /// <summary>
        /// Logs an error message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public void LogErrorInstance(string message, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Error)
            {
                LogInternal(LogLevel.Error, message, context);
            }
        }
        
        /// <summary>
        /// Logs a warning message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public void LogWarningInstance(string message, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Warning)
            {
                LogInternal(LogLevel.Warning, message, context);
            }
        }
        
        /// <summary>
        /// Logs an info message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public void LogInfoInstance(string message, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Info)
            {
                LogInternal(LogLevel.Info, message, context);
            }
        }
        
        /// <summary>
        /// Logs a debug message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public void LogDebugInstance(string message, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Debug)
            {
                LogInternal(LogLevel.Debug, message, context);
            }
        }
        
        /// <summary>
        /// Logs a verbose message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        public void LogVerboseInstance(string message, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Verbose)
            {
                LogInternal(LogLevel.Verbose, message, context);
            }
        }
        
        /// <summary>
        /// Logs pool metrics from an instance
        /// </summary>
        /// <param name="metrics">Dictionary of metrics</param>
        public void LogMetrics(Dictionary<string, object> metrics)
        {
            if (_logLevel < LogLevel.Info)
                return;
                
            var sb = new StringBuilder();
            sb.AppendLine($"Pool Metrics for {_poolName}:");
            
            foreach (var kvp in metrics)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            LogInfoInstance(sb.ToString());
        }
        
        /// <summary>
        /// Logs an exception from an instance
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Optional context object</param>
        public void LogException(Exception exception, UnityEngine.Object context = null)
        {
            if (_logLevel >= LogLevel.Error)
            {
                string message = $"Exception: {exception.Message}";
                
                if (_includeStackTraceForErrors)
                {
                    message += $"\nStack Trace: {exception.StackTrace}";
                }
                
                LogInternal(LogLevel.Error, message, context);
            }
        }
        
        /// <summary>
        /// Logs details about a pool operation from an instance
        /// </summary>
        /// <param name="operation">Name of the operation</param>
        /// <param name="details">Details about the operation</param>
        /// <param name="level">Log level for this entry</param>
        public void LogOperation(string operation, string details, LogLevel level = LogLevel.Debug)
        {
            if (_logLevel >= level)
            {
                LogInternal(level, $"Operation: {operation} - {details}");
            }
        }
        
        /// <summary>
        /// Logs an internal message with the specified log level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="context">Optional context object</param>
        private void LogInternal(LogLevel level, string message, UnityEngine.Object context = null)
        {
            string formattedMessage = FormatMessage(level, message);
            
            // Log to console if enabled
            if ((_logTarget & LogTarget.Console) != 0)
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Debug.LogError(formattedMessage, context);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(formattedMessage, context);
                        break;
                    default:
                        Debug.Log(formattedMessage, context);
                        break;
                }
            }
            
            // Log to file if enabled
            if ((_logTarget & LogTarget.File) != 0)
            {
                LogToFile(formattedMessage);
            }
            
            // Log to memory if enabled
            if ((_logTarget & LogTarget.Memory) != 0)
            {
                LogToMemory(formattedMessage);
            }
            
            // Display in debugger if enabled
            if ((_logTarget & LogTarget.DebuggerDisplay) != 0 && System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(formattedMessage);
            }
        }
        
        /// <summary>
        /// Formats a log message with the pool name and timestamp
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to format</param>
        /// <returns>Formatted message</returns>
        private string FormatMessage(LogLevel level, string message)
        {
            var sb = new StringBuilder();
            
            if (_includeTimestamps)
            {
                sb.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
            }
            
            sb.Append($"[{level}] ");
            sb.Append($"[{_poolName}] ");
            sb.Append(message);
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Logs a message to the configured log file
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogToFile(string message)
        {
            try
            {
                string directory = LogFilePath;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string filePath = Path.Combine(directory, $"PoolLog_{DateTime.Now:yyyyMMdd}.txt");
                File.AppendAllText(filePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fall back to Debug.LogError but avoid infinite loops
                if ((_logTarget & LogTarget.Console) == 0)
                {
                    Debug.LogError($"Failed to write to log file: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Logs a message to the in-memory log buffer
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogToMemory(string message)
        {
            _memoryLog.Add(message);
            
            // Keep the memory log from growing too large
            while (_memoryLog.Count > _maxMemoryLogEntries)
            {
                _memoryLog.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// Pool logger extensions to make logging more convenient
    /// </summary>
    public static class PoolLoggerExtensions
    {
        /// <summary>
        /// Gets a logger for the pool and logs an acquisition operation
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="item">The acquired item</param>
        /// <param name="customMessage">Custom message to include</param>
        public static void LogAcquire<T>(this IPool<T> pool, T item, string customMessage = null)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            string itemId = item?.GetHashCode().ToString() ?? "null";
            string message = customMessage ?? $"Acquired item {itemId} from pool. Active: {pool.ActiveCount}, Free: {pool.InactiveCount}";
            
            logger.LogOperation("Acquire", message, PoolLogger.LogLevel.Debug);
        }
        
        /// <summary>
        /// Gets a logger for the pool and logs a release operation
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="item">The released item</param>
        /// <param name="customMessage">Custom message to include</param>
        public static void LogRelease<T>(this IPool<T> pool, T item, string customMessage = null)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            string itemId = item?.GetHashCode().ToString() ?? "null";
            string message = customMessage ?? $"Released item {itemId} to pool. Active: {pool.ActiveCount}, Free: {pool.InactiveCount}";
            
            logger.LogOperation("Release", message, PoolLogger.LogLevel.Debug);
        }
        
        /// <summary>
        /// Gets a logger for the pool and logs a pool expansion operation
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="oldSize">Old pool size</param>
        /// <param name="newSize">New pool size</param>
        public static void LogExpansion<T>(this IPool<T> pool, int oldSize, int newSize)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            string message = $"Pool expanded from {oldSize} to {newSize} items";
            
            logger.LogOperation("Expand", message, PoolLogger.LogLevel.Info);
        }
        
        /// <summary>
        /// Gets a logger for the pool and logs a pool prewarm operation
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of items prewarmed</param>
        public static void LogPrewarm<T>(this IPool<T> pool, int count)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            string message = $"Prewarmed pool with {count} items. Total: {pool.ActiveCount + pool.InactiveCount}";
            
            logger.LogOperation("PrewarmPool", message, PoolLogger.LogLevel.Info);
        }
        
        /// <summary>
        /// Gets a logger for the pool and logs a pool clear operation
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="itemCount">Number of items cleared</param>
        public static void LogClear<T>(this IPool<T> pool, int itemCount)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            string message = $"Cleared pool of {itemCount} items";
            
            logger.LogOperation("Clear", message, PoolLogger.LogLevel.Info);
        }
        
        /// <summary>
        /// Gets a logger for the pool and logs periodic metrics
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        public static void LogPeriodicMetrics<T>(this IPool<T> pool)
        {
            string poolName = pool.GetType().Name;
            var logger = PoolLogger.GetLogger(poolName);
            
            var metrics = pool.GetMetrics();
            logger.LogMetrics(metrics);
        }
    }
}