using AhBearStudios.Core.Logging.Data;
using Unity.Collections;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Processors;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Allocation-free logging interface for use inside Burst-compiled jobs.
    /// Writes entries into a native buffer for later draining by the main
    /// <see cref="ILoggingService"/>.
    /// </summary>
    public interface IBurstLoggingService
    {
        /// <summary>
        /// Logs a message at the specified <paramref name="level"/>.
        /// </summary>
        /// <param name="message">The text to log (must be a FixedString).</param>
        /// <param name="level">The severity level for filtering and routing.</param>
        void Log(in FixedString512Bytes message, LogLevel level);

        /// <summary>
        /// Logs a message under the given <paramref name="category"/> at the specified <paramref name="level"/>.
        /// </summary>
        /// <param name="category">A short category tag (e.g. "Physics", "AI").</param>
        /// <param name="message">The text to log.</param>
        /// <param name="level">The severity level for filtering and routing.</param>
        void Log(in Tagging.LogTag category, in FixedString512Bytes message, LogLevel level);
        
        void Log(Tagging.LogTag tag, in FixedString512Bytes message, LogLevel level, in LogProperties properties);

        /// <summary>
        /// Clears all buffered Burst log entries without processing them.
        /// </summary>
        void ClearBuffer();

        /// <summary>
        /// Gets the number of entries currently buffered awaiting processing.
        /// </summary>
        int PendingCount { get; }

        /// <summary>
        /// Attempts to dequeue the next <see cref="LogMessage"/> from the internal buffer.
        /// Intended for use by <see cref="LogBatchProcessor"/>.
        /// </summary>
        /// <param name="message">The dequeued log message, if any.</param>
        /// <returns><c>true</c> if a message was dequeued; otherwise <c>false</c>.</returns>
        bool TryDequeue(out LogMessage message);
    }
}