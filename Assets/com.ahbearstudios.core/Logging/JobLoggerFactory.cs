using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;
using Unity.Collections;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Factory class for creating JobLogger instances.
    /// </summary>
    public static class JobLoggerFactory
    {
        /// <summary>
        /// Creates a JobLogger suitable for use in parallel jobs.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="minimumLevel">The minimum severity level to log.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for parallel job use.</returns>
        /// <exception cref="InvalidOperationException">Thrown if queue is not created.</exception>
        public static JobLogger CreateParallel(NativeQueue<LogMessage> queue, byte minimumLevel, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            // NativeQueue is a struct, so we can only check if it's created
            if (!queue.IsCreated)
                throw new InvalidOperationException("The queue must be created before creating a JobLogger.");
        
            return new JobLogger(queue.AsParallelWriter(), minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Debug as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for all log levels.</returns>
        public static JobLogger CreateDebugLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, (byte)Tagging.LogTag.Debug, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Info as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for info and higher log levels.</returns>
        public static JobLogger CreateInfoLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, (byte)Tagging.LogTag.Info, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Warning as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for warning and higher log levels.</returns>
        public static JobLogger CreateWarningLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, (byte)Tagging.LogTag.Warning, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Error as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for error and higher log levels.</returns>
        public static JobLogger CreateErrorLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, (byte)Tagging.LogTag.Error, defaultTag);
        }
    }
}