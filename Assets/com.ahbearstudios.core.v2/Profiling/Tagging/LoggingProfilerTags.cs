using AhBearStudios.Core.Logging;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Static class for generating profiler tags for logging operations.
    /// Provides consistent tagging for logging-related profiling sessions.
    /// </summary>
    public static class LoggingProfilerTags
    {
        #region Categories

        /// <summary>
        /// Profiler category for logging operations.
        /// </summary>
        public static readonly ProfilerCategory LoggingCategory = ProfilerCategory.Scripts;

        #endregion

        #region Message Processing Tags

        /// <summary>
        /// Creates a profiler tag for message processing operations.
        /// </summary>
        /// <param name="logLevel">Log level being processed.</param>
        /// <param name="tag">Log tag being processed.</param>
        /// <returns>Profiler tag for message processing.</returns>
        public static ProfilerTag ForMessageProcessing(LogLevel logLevel, string tag)
        {
            var name = string.IsNullOrEmpty(tag)
                ? $"Logging.Processing.{logLevel}"
                : $"Logging.Processing.{logLevel}.{tag}";
            return new ProfilerTag(LoggingCategory, name);
        }

        /// <summary>
        /// Creates a profiler tag for message processing by level only.
        /// </summary>
        /// <param name="logLevel">Log level being processed.</param>
        /// <returns>Profiler tag for message processing.</returns>
        public static ProfilerTag ForMessageProcessing(LogLevel logLevel)
        {
            return new ProfilerTag(LoggingCategory, $"Logging.Processing.{logLevel}");
        }

        #endregion

        #region Target Write Tags

        /// <summary>
        /// Creates a profiler tag for target write operations.
        /// </summary>
        /// <param name="targetName">Name of the log target.</param>
        /// <param name="logLevel">Log level being written.</param>
        /// <returns>Profiler tag for target writes.</returns>
        public static ProfilerTag ForTargetWrite(string targetName, LogLevel logLevel)
        {
            var safeName = string.IsNullOrEmpty(targetName) ? "Unknown" : targetName;
            return new ProfilerTag(LoggingCategory, $"Logging.Target.{safeName}.{logLevel}");
        }

        /// <summary>
        /// Creates a profiler tag for target write operations without level specification.
        /// </summary>
        /// <param name="targetName">Name of the log target.</param>
        /// <returns>Profiler tag for target writes.</returns>
        public static ProfilerTag ForTargetWrite(string targetName)
        {
            var safeName = string.IsNullOrEmpty(targetName) ? "Unknown" : targetName;
            return new ProfilerTag(LoggingCategory, $"Logging.Target.{safeName}");
        }

        #endregion

        #region Queue Flush Tags

        /// <summary>
        /// Creates a profiler tag for queue flush operations.
        /// </summary>
        /// <param name="messageCount">Number of messages being flushed.</param>
        /// <returns>Profiler tag for queue flushes.</returns>
        public static ProfilerTag ForQueueFlush(int messageCount)
        {
            return new ProfilerTag(LoggingCategory, $"Logging.Flush.{messageCount}Messages");
        }

        /// <summary>
        /// Creates a profiler tag for generic queue flush operations.
        /// </summary>
        /// <returns>Profiler tag for queue flushes.</returns>
        public static ProfilerTag ForQueueFlush()
        {
            return new ProfilerTag(LoggingCategory, "Logging.Flush");
        }

        #endregion

        #region Level Change Tags

        /// <summary>
        /// Creates a profiler tag for log level change operations.
        /// </summary>
        /// <param name="oldLevel">Previous log level.</param>
        /// <param name="newLevel">New log level.</param>
        /// <returns>Profiler tag for level changes.</returns>
        public static ProfilerTag ForLevelChange(LogLevel oldLevel, LogLevel newLevel)
        {
            return new ProfilerTag(LoggingCategory, $"Logging.LevelChange.{oldLevel}To{newLevel}");
        }

        #endregion

        #region Formatter Tags

        /// <summary>
        /// Creates a profiler tag for log formatter operations.
        /// </summary>
        /// <param name="formatterName">Name of the log formatter.</param>
        /// <param name="logLevel">Log level being formatted.</param>
        /// <returns>Profiler tag for formatter operations.</returns>
        public static ProfilerTag ForFormatter(string formatterName, LogLevel logLevel)
        {
            var safeName = string.IsNullOrEmpty(formatterName) ? "Unknown" : formatterName;
            return new ProfilerTag(LoggingCategory, $"Logging.Formatter.{safeName}.{logLevel}");
        }

        /// <summary>
        /// Creates a profiler tag for log formatter operations without level specification.
        /// </summary>
        /// <param name="formatterName">Name of the log formatter.</param>
        /// <returns>Profiler tag for formatter operations.</returns>
        public static ProfilerTag ForFormatter(string formatterName)
        {
            var safeName = string.IsNullOrEmpty(formatterName) ? "Unknown" : formatterName;
            return new ProfilerTag(LoggingCategory, $"Logging.Formatter.{safeName}");
        }

        #endregion

        #region Generic Operation Tags

        /// <summary>
        /// Creates a profiler tag for generic logging operations.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <returns>Profiler tag for generic operations.</returns>
        public static ProfilerTag ForGenericOperation(string operationType, LogLevel logLevel)
        {
            var safeOperation = string.IsNullOrEmpty(operationType) ? "Unknown" : operationType;
            return new ProfilerTag(LoggingCategory, $"Logging.{safeOperation}.{logLevel}");
        }

        /// <summary>
        /// Creates a profiler tag for generic logging operations without level specification.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <returns>Profiler tag for generic operations.</returns>
        public static ProfilerTag ForGenericOperation(string operationType)
        {
            var safeOperation = string.IsNullOrEmpty(operationType) ? "Unknown" : operationType;
            return new ProfilerTag(LoggingCategory, $"Logging.{safeOperation}");
        }

        #endregion

        #region Tag Utilities

        /// <summary>
        /// Creates a profiler tag for logging system operations.
        /// </summary>
        /// <param name="systemName">Name of the logging system component.</param>
        /// <returns>Profiler tag for system operations.</returns>
        public static ProfilerTag ForSystem(string systemName)
        {
            var safeName = string.IsNullOrEmpty(systemName) ? "Unknown" : systemName;
            return new ProfilerTag(LoggingCategory, $"Logging.System.{safeName}");
        }

        /// <summary>
        /// Creates a profiler tag for logging initialization operations.
        /// </summary>
        /// <param name="componentName">Name of the component being initialized.</param>
        /// <returns>Profiler tag for initialization operations.</returns>
        public static ProfilerTag ForInitialization(string componentName)
        {
            var safeName = string.IsNullOrEmpty(componentName) ? "Unknown" : componentName;
            return new ProfilerTag(LoggingCategory, $"Logging.Init.{safeName}");
        }

        /// <summary>
        /// Creates a profiler tag for logging disposal operations.
        /// </summary>
        /// <param name="componentName">Name of the component being disposed.</param>
        /// <returns>Profiler tag for disposal operations.</returns>
        public static ProfilerTag ForDisposal(string componentName)
        {
            var safeName = string.IsNullOrEmpty(componentName) ? "Unknown" : componentName;
            return new ProfilerTag(LoggingCategory, $"Logging.Dispose.{safeName}");
        }

        /// <summary>
        /// Creates a profiler tag for logging configuration operations.
        /// </summary>
        /// <param name="configOperation">Type of configuration operation.</param>
        /// <returns>Profiler tag for configuration operations.</returns>
        public static ProfilerTag ForConfiguration(string configOperation)
        {
            var safeOperation = string.IsNullOrEmpty(configOperation) ? "Unknown" : configOperation;
            return new ProfilerTag(LoggingCategory, $"Logging.Config.{safeOperation}");
        }

        #endregion

        #region Batch Operation Tags

        /// <summary>
        /// Creates a profiler tag for batch logging operations.
        /// </summary>
        /// <param name="batchSize">Number of messages in the batch.</param>
        /// <param name="operationType">Type of batch operation.</param>
        /// <returns>Profiler tag for batch operations.</returns>
        public static ProfilerTag ForBatchOperation(int batchSize, string operationType)
        {
            var safeOperation = string.IsNullOrEmpty(operationType) ? "Batch" : operationType;
            return new ProfilerTag(LoggingCategory, $"Logging.Batch.{safeOperation}.{batchSize}");
        }

        /// <summary>
        /// Creates a profiler tag for batch logging operations without specifying size.
        /// </summary>
        /// <param name="operationType">Type of batch operation.</param>
        /// <returns>Profiler tag for batch operations.</returns>
        public static ProfilerTag ForBatchOperation(string operationType)
        {
            var safeOperation = string.IsNullOrEmpty(operationType) ? "Batch" : operationType;
            return new ProfilerTag(LoggingCategory, $"Logging.Batch.{safeOperation}");
        }

        #endregion
    }
}