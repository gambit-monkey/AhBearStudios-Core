using Unity.Collections;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Interface for factory classes that create different types of loggers.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Creates a standard logger based on the provided configuration.
        /// </summary>
        /// <param name="config">Configuration controlling the logger's behavior.</param>
        /// <returns>A configured IBurstLogger instance.</returns>
        IBurstLogger CreateLogger(ILoggerConfig config);
        
        /// <summary>
        /// Creates a LogBatchProcessor for processing log messages from jobs.
        /// </summary>
        /// <param name="burstLogger">The target burstLogger to receive processed messages.</param>
        /// <param name="config">Configuration controlling processor behavior.</param>
        /// <returns>A configured LogBatchProcessor instance.</returns>
        LogBatchProcessor CreateBatchProcessor(IBurstLogger burstLogger, ILoggerConfig config);
        
        /// <summary>
        /// Creates a JobLogger for use in Unity job contexts.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="config">Configuration controlling logger behavior.</param>
        /// <returns>A configured JobLogger instance.</returns>
        JobLogger CreateJobLogger(NativeQueue<LogMessage> queue, ILoggerConfig config);
    }
}