using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Processors;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory for creating logging primitives from a shared configuration.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Creates the high-level managed logging service.
        /// </summary>
        /// <param name="config">The logging configuration.</param>
        ILoggingService CreateLoggingService(ILoggerConfig config);

        /// <summary>
        /// Creates a burst-friendly logger for use inside jobs.
        /// </summary>
        /// <param name="config">The logging configuration.</param>
        IBurstLoggingService CreateBurstLoggingService(ILoggerConfig config);

        /// <summary>
        /// Creates the batch processor that drains burst logs into the managed service.
        /// </summary>
        /// <param name="burstLogger">The burst logging service.</param>
        /// <param name="config">The logging configuration.</param>
        LogBatchProcessor CreateBatchProcessor(IBurstLoggingService burstLogger, ILoggerConfig config);

        /// <summary>
        /// Creates a job-side logger that writes into a native queue.
        /// </summary>
        /// <param name="queue">The native queue for log messages.</param>
        /// <param name="config">The logging configuration.</param>
        JobLogger CreateJobLogger(NativeQueue<LogMessage> queue, ILoggerConfig config);
    }
}