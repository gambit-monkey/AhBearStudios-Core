
using System;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Jobs
{
    /// <summary>
    /// A Burst-compatible logger that can be used within Job contexts.
    /// Designed to work with the Unity Jobs system to provide safe,
    /// thread-friendly logging capabilities.
    /// </summary>
    [BurstCompile]
    public readonly struct JobLogger
    {
        /// <summary>
        /// The parallel writer for the log message queue.
        /// </summary>
        public readonly NativeQueue<LogMessage>.ParallelWriter LogWriter;
        
        /// <summary>
        /// The minimum severity level for logs to be recorded.
        /// Messages with lower severity will be ignored.
        /// </summary>
        public readonly byte MinimumLevel;
        
        /// <summary>
        /// Default tag to apply when no tag is specified.
        /// </summary>
        public readonly Tagging.LogTag DefaultTag;

        /// <summary>
        /// Creates a new JobLogger with the specified queue writer, minimum log level, and default tag.
        /// </summary>
        /// <param name="writer">The parallel writer for log messages.</param>
        /// <param name="minimumLevel">The minimum severity level to log.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        public JobLogger(NativeQueue<LogMessage>.ParallelWriter writer, byte minimumLevel, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            LogWriter = writer;
            MinimumLevel = minimumLevel;
            DefaultTag = defaultTag;
        }
        
        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="level">The severity level.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the log message.</param>
        public void Log(byte level, in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            if (level < MinimumLevel)
                return;
                
            // Use default tag if undefined
            Tagging.LogTag logTag = tag == Tagging.LogTag.Undefined ? DefaultTag : tag;
            
            // Create and enqueue the log message
            LogMessage logMessage = new LogMessage(message, level, logTag);
            LogWriter.Enqueue(logMessage);
        }
        
        /// <summary>
        /// Logs a message with a custom string tag.
        /// </summary>
        /// <param name="level">The severity level.</param>
        /// <param name="message">The message content.</param>
        /// <param name="customTag">A custom tag string.</param>
        public void Log(byte level, in FixedString512Bytes message, in FixedString32Bytes customTag)
        {
            if (level < MinimumLevel || customTag.IsEmpty)
                return;
                
            // Create and enqueue the log message with custom tag
            LogMessage logMessage = new LogMessage(message, level, customTag);
            LogWriter.Enqueue(logMessage);
        }
        
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public void LogDebug(in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            Log((byte)Tagging.LogTag.Debug, message, tag);
        }
        
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public void LogInfo(in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            Log((byte)Tagging.LogTag.Info, message, tag);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public void LogWarning(in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            Log((byte)Tagging.LogTag.Warning, message, tag);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public void LogError(in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            Log((byte)Tagging.LogTag.Error, message, tag);
        }
        
        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public void LogCritical(in FixedString512Bytes message, Tagging.LogTag tag = Tagging.LogTag.Undefined)
        {
            Log((byte)Tagging.LogTag.Critical, message, tag);
        }
    }
}