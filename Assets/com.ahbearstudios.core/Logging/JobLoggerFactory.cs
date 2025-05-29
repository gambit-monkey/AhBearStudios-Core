
using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Factory class for creating JobLogger instances and JobLoggerManager configurations.
    /// Provides both low-level JobLogger creation and high-level manager setup using builders.
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
        public static JobLogger CreateParallel(NativeQueue<LogMessage> queue, LogLevel minimumLevel, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            // NativeQueue is a struct, so we can only check if it's created
            if (!queue.IsCreated)
                throw new InvalidOperationException("The queue must be created before creating a JobLogger.");
        
            return new JobLogger(queue.AsParallelWriter(), minimumLevel, defaultTag);
        }

        /// <summary>
        /// Creates a JobLogger from a JobLoggerManager instance.
        /// This is the recommended way to create JobLoggers as it ensures consistency with the manager's configuration.
        /// </summary>
        /// <param name="manager">The JobLoggerManager to create the logger from.</param>
        /// <param name="minimumLevel">Optional minimum level override. If null, uses the manager's global level.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for the manager.</returns>
        /// <exception cref="ArgumentNullException">Thrown if manager is null.</exception>
        public static JobLogger CreateFromManager(JobLoggerManager manager, LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            return manager.CreateJobLogger(minimumLevel, defaultTag);
        }

        /// <summary>
        /// Creates a complete logging setup with JobLoggerManager and returns a JobLogger for immediate use.
        /// This is ideal for quick setup scenarios where you want both a manager and a logger.
        /// </summary>
        /// <param name="builderConfigurator">Action to configure log target builders.</param>
        /// <param name="formatter">Optional formatter. Uses DefaultLogFormatter if null.</param>
        /// <param name="minimumLevel">Minimum level for both manager and logger.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger created from it.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateComplete(
            Action<JobLoggerManager.LogTargetBuilderCollection> builderConfigurator,
            ILogFormatter formatter = null,
            LogLevel minimumLevel = LogLevel.Info,
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            if (builderConfigurator == null)
                throw new ArgumentNullException(nameof(builderConfigurator));

            var manager = new JobLoggerManager(
                builderConfigurator,
                formatter,
                64, // default capacity
                200, // default max messages per flush
                minimumLevel,
                messageBus);

            var logger = manager.CreateJobLogger(minimumLevel, defaultTag);
            
            return (manager, logger);
        }

        /// <summary>
        /// Creates a development logging setup with file and console logging.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger for development use.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateForDevelopment(
            string logFilePath = "Logs/debug.log",
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            return CreateComplete(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileDebug(logFilePath));
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsoleDevelopment());
                },
                null, // Use default formatter
                LogLevel.Debug,
                defaultTag,
                messageBus);
        }

        /// <summary>
        /// Creates a production logging setup optimized for performance.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger for production use.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateForProduction(
            string logFilePath = "Logs/app.log",
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            return CreateComplete(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileHighPerformance(logFilePath));
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsoleProduction());
                },
                null, // Use default formatter
                LogLevel.Warning,
                defaultTag,
                messageBus);
        }

        /// <summary>
        /// Creates a console-only logging setup for lightweight scenarios.
        /// </summary>
        /// <param name="consoleLevel">Minimum level for console output.</param>
        /// <param name="useColorizedOutput">Whether to use colorized console output.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger for console-only logging.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateConsoleOnly(
            LogLevel consoleLevel = LogLevel.Info,
            bool useColorizedOutput = true,
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            return CreateComplete(
                builders =>
                {
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsole()
                        .WithMinimumLevel(consoleLevel)
                        .WithColorizedOutput(useColorizedOutput));
                },
                null, // Use default formatter
                consoleLevel,
                defaultTag,
                messageBus);
        }

        /// <summary>
        /// Creates a file-only logging setup for scenarios where console output is not desired.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="fileLevel">Minimum level for file output.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger for file-only logging.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateFileOnly(
            string logFilePath = "Logs/app.log",
            LogLevel fileLevel = LogLevel.Debug,
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            return CreateComplete(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFile(logFilePath)
                        .WithMinimumLevel(fileLevel));
                },
                null, // Use default formatter
                fileLevel,
                defaultTag,
                messageBus);
        }

        /// <summary>
        /// Creates a high-performance logging setup optimized for minimal overhead.
        /// Uses buffering and reduced formatting for maximum performance.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="defaultTag">Default tag for the returned logger.</param>
        /// <param name="messageBus">Optional message bus.</param>
        /// <returns>A tuple containing the manager and a logger for high-performance logging.</returns>
        public static (JobLoggerManager manager, JobLogger logger) CreateHighPerformance(
            string logFilePath = "Logs/hp.log",
            Tagging.LogTag defaultTag = Tagging.LogTag.Job,
            IMessageBus messageBus = null)
        {
            return CreateComplete(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileHighPerformance(logFilePath));
                    // No console output for maximum performance
                },
                null, // Use default formatter
                LogLevel.Warning, // Higher threshold for performance
                defaultTag,
                messageBus);
        }

        // Legacy convenience methods - maintained for backward compatibility
        
        /// <summary>
        /// Convenience method to create a JobLogger with Debug as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for all log levels.</returns>
        public static JobLogger CreateDebugLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, LogLevel.Debug, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Info as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for info and higher log levels.</returns>
        public static JobLogger CreateInfoLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, LogLevel.Info, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Warning as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for warning and higher log levels.</returns>
        public static JobLogger CreateWarningLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, LogLevel.Warning, defaultTag);
        }
        
        /// <summary>
        /// Convenience method to create a JobLogger with Error as the minimum level.
        /// </summary>
        /// <param name="queue">The queue to write log messages to.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A JobLogger configured for error and higher log levels.</returns>
        public static JobLogger CreateErrorLogger(NativeQueue<LogMessage> queue, Tagging.LogTag defaultTag = Tagging.LogTag.Job)
        {
            return CreateParallel(queue, LogLevel.Error, defaultTag);
        }
    }
}