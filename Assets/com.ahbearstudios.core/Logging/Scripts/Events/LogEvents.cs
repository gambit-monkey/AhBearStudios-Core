using System;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Static class for log-related events.
    /// </summary>
    public static class LogEvents
    {
        /// <summary>
        /// Event raised when a log message is about to be processed.
        /// </summary>
        public static event EventHandler<LogMessageEventArgs> OnMessageReceived;

        /// <summary>
        /// Event raised when a batch of log messages has been processed.
        /// </summary>
        public static event EventHandler<LogProcessingEventArgs> OnBatchProcessed;

        /// <summary>
        /// Event raised when a log message is filtered out.
        /// </summary>
        public static event EventHandler<LogMessageEventArgs> OnMessageFiltered;

        /// <summary>
        /// Occurs when a new log message is created but before it enters the processing pipeline.
        /// </summary>
        public static event EventHandler<LogMessageEventArgs> OnMessageCreated;

        /// <summary>
        /// Occurs when a log message has passed through the pipeline and is about to be written.
        /// </summary>
        public static event EventHandler<LogMessageEventArgs> OnMessageProcessed;

        /// <summary>
        /// Occurs when a log message has been written to at least one target.
        /// </summary>
        public static event EventHandler<LogMessageWrittenEventArgs> OnMessageWritten;

        /// <summary>
        /// Occurs when the log queue is flushed.
        /// </summary>
        public static event EventHandler<LogFlushEventArgs> OnLogFlushed;

        /// <summary>
        /// Occurs when the global log level is changed.
        /// </summary>
        public static event EventHandler<LogLevelChangedEventArgs> OnLogLevelChanged;

        /// <summary>
        /// Raises the MessageCreated event.
        /// </summary>
        /// <param name="message">The log message that was created.</param>
        internal static void RaiseMessageCreated(LogMessage message)
        {
            OnMessageCreated?.Invoke(null, new LogMessageEventArgs(message));
        }

        /// <summary>
        /// Raises the MessageProcessed event.
        /// </summary>
        /// <param name="message">The log message that was processed.</param>
        internal static void RaiseMessageProcessed(LogMessage message)
        {
            OnMessageProcessed?.Invoke(null, new LogMessageEventArgs(message));
        }

        /// <summary>
        /// Raises the MessageWritten event.
        /// </summary>
        /// <param name="message">The log message that was written.</param>
        /// <param name="targetCount">The number of targets the message was written to.</param>
        internal static void RaiseMessageWritten(LogMessage message, int targetCount)
        {
            OnMessageWritten?.Invoke(null, new LogMessageWrittenEventArgs(message, targetCount));
        }

        /// <summary>
        /// Raises the LogFlushed event.
        /// </summary>
        /// <param name="processedCount">The number of messages processed during the flush.</param>
        /// <param name="durationMs">The duration of the flush operation in milliseconds.</param>
        internal static void RaiseLogFlushed(int processedCount, float durationMs)
        {
            OnLogFlushed?.Invoke(null, new LogFlushEventArgs(processedCount, durationMs));
        }

        /// <summary>
        /// Raises the LogLevelChanged event.
        /// </summary>
        /// <param name="oldLevel">The previous log level.</param>
        /// <param name="newLevel">The new log level.</param>
        internal static void RaiseLogLevelChanged(byte oldLevel, byte newLevel)
        {
            OnLogLevelChanged?.Invoke(null, new LogLevelChangedEventArgs(oldLevel, newLevel));
        }
        
        

        /// <summary>
        /// Raises the MessageReceived event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="message">The log message.</param>
        internal static void RaiseMessageReceived(object sender, LogMessage message)
        {
            OnMessageReceived?.Invoke(sender, new LogMessageEventArgs(message));
        }

        /// <summary>
        /// Raises the BatchProcessed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="processedCount">The number of messages processed.</param>
        /// <param name="remainingCount">The number of messages remaining.</param>
        /// <param name="processingTimeMs">The processing time in milliseconds.</param>
        internal static void RaiseBatchProcessed(
            object sender, int processedCount, int remainingCount, float processingTimeMs)
        {
            OnBatchProcessed?.Invoke(sender,
                new LogProcessingEventArgs(processedCount, remainingCount, processingTimeMs));
        }

        /// <summary>
        /// Raises the MessageFiltered event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="message">The log message that was filtered out.</param>
        internal static void RaiseMessageFiltered(object sender, LogMessage message)
        {
            OnMessageFiltered?.Invoke(sender, new LogMessageEventArgs(message));
        }
    }
}