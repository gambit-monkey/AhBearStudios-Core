using System;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Unity.Attributes
{
    /// <summary>
    /// Attribute that can be applied to classes or methods to automatically generate logging code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WithLogAttribute : Attribute
    {
        #region Core Logging Properties
        /// <summary>
        /// Gets the minimum log level for generated log messages.
        /// </summary>
        public LogLevel MinimumLevel { get; }

        /// <summary>
        /// Gets the predefined tag for log messages.
        /// </summary>
        public Tagging.LogTag Tag { get; }

        /// <summary>
        /// Gets the custom tag string for log messages when using custom tagging.
        /// </summary>
        public string CustomTag { get; }

        /// <summary>
        /// Gets the tag category for enhanced filtering and organization.
        /// </summary>
        public Tagging.TagCategory TagCategory { get; }

        #endregion

        #region Method Lifecycle Logging

        /// <summary>
        /// Gets whether to log method entry.
        /// </summary>
        public bool LogEntry { get; }

        /// <summary>
        /// Gets whether to log method exit.
        /// </summary>
        public bool LogExit { get; }

        /// <summary>
        /// Gets whether to log method parameters.
        /// </summary>
        public bool LogParameters { get; }

        /// <summary>
        /// Gets whether to log method return value.
        /// </summary>
        public bool LogReturnValue { get; }

        #endregion

        #region Performance and Profiling

        /// <summary>
        /// Gets whether to track method execution time.
        /// </summary>
        public bool TrackTime { get; }

        /// <summary>
        /// Gets whether to integrate with the profiling system for detailed performance metrics.
        /// </summary>
        public bool EnableProfiling { get; }

        /// <summary>
        /// Gets the performance threshold in milliseconds above which warnings should be logged.
        /// </summary>
        public double PerformanceThresholdMs { get; }

        #endregion

        #region Structured Logging

        /// <summary>
        /// Gets whether to enable structured logging with properties.
        /// </summary>
        public bool EnableStructuredLogging { get; }

        /// <summary>
        /// Gets whether to include source context information (method name, class name, etc.).
        /// </summary>
        public bool IncludeSourceContext { get; }

        /// <summary>
        /// Gets whether to include thread information in log messages.
        /// </summary>
        public bool IncludeThreadInfo { get; }

        /// <summary>
        /// Gets whether to include stack trace information for debugging.
        /// </summary>
        public bool IncludeStackTrace { get; }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Gets whether to automatically log exceptions thrown by the method.
        /// </summary>
        public bool LogExceptions { get; }

        /// <summary>
        /// Gets the log level to use for exception logging.
        /// </summary>
        public LogLevel ExceptionLogLevel { get; }

        #endregion

        #region Message Bus Integration

        /// <summary>
        /// Gets whether to publish log events to the message bus for system-wide monitoring.
        /// </summary>
        public bool PublishToMessageBus { get; }

        /// <summary>
        /// Gets whether to enable batch processing for high-frequency logging scenarios.
        /// </summary>
        public bool EnableBatchProcessing { get; }

        #endregion

        #region Filtering and Conditional Logging

        /// <summary>
        /// Gets whether logging should be conditional based on runtime configuration.
        /// </summary>
        public bool ConditionalLogging { get; }

        /// <summary>
        /// Gets the maximum parameter value length for logging (prevents large object serialization).
        /// </summary>
        public int MaxParameterLength { get; }

        /// <summary>
        /// Gets parameter names that should be excluded from logging (for security/privacy).
        /// </summary>
        public string[] ExcludedParameters { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new WithLogAttribute with comprehensive configuration options.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level for generated messages.</param>
        /// <param name="tag">The predefined tag for log messages.</param>
        /// <param name="tagCategory">The tag category for enhanced organization.</param>
        /// <param name="logEntry">Whether to log method entry.</param>
        /// <param name="logExit">Whether to log method exit.</param>
        /// <param name="logParameters">Whether to log method parameters.</param>
        /// <param name="logReturnValue">Whether to log method return value.</param>
        /// <param name="trackTime">Whether to track method execution time.</param>
        /// <param name="enableProfiling">Whether to integrate with the profiling system.</param>
        /// <param name="performanceThresholdMs">Performance threshold for warning logs.</param>
        /// <param name="enableStructuredLogging">Whether to use structured logging.</param>
        /// <param name="includeSourceContext">Whether to include source context.</param>
        /// <param name="includeThreadInfo">Whether to include thread information.</param>
        /// <param name="includeStackTrace">Whether to include stack trace.</param>
        /// <param name="logExceptions">Whether to automatically log exceptions.</param>
        /// <param name="exceptionLogLevel">Log level for exception logging.</param>
        /// <param name="publishToMessageBus">Whether to publish to message bus.</param>
        /// <param name="enableBatchProcessing">Whether to enable batch processing.</param>
        /// <param name="conditionalLogging">Whether to enable conditional logging.</param>
        /// <param name="maxParameterLength">Maximum parameter value length.</param>
        /// <param name="excludedParameters">Parameter names to exclude from logging.</param>
        public WithLogAttribute(
            LogLevel minimumLevel = LogLevel.Debug,
            Tagging.LogTag tag = Tagging.LogTag.Info,
            Tagging.TagCategory tagCategory = Tagging.TagCategory.Gameplay,
            bool logEntry = true,
            bool logExit = true,
            bool logParameters = false,
            bool logReturnValue = false,
            bool trackTime = false,
            bool enableProfiling = false,
            double performanceThresholdMs = 100.0,
            bool enableStructuredLogging = true,
            bool includeSourceContext = true,
            bool includeThreadInfo = false,
            bool includeStackTrace = false,
            bool logExceptions = true,
            LogLevel exceptionLogLevel = LogLevel.Error,
            bool publishToMessageBus = false,
            bool enableBatchProcessing = false,
            bool conditionalLogging = true,
            int maxParameterLength = 1024,
            string[] excludedParameters = null)
        {
            MinimumLevel = minimumLevel;
            Tag = tag;
            TagCategory = tagCategory;
            CustomTag = null;
            LogEntry = logEntry;
            LogExit = logExit;
            LogParameters = logParameters;
            LogReturnValue = logReturnValue;
            TrackTime = trackTime;
            EnableProfiling = enableProfiling;
            PerformanceThresholdMs = performanceThresholdMs;
            EnableStructuredLogging = enableStructuredLogging;
            IncludeSourceContext = includeSourceContext;
            IncludeThreadInfo = includeThreadInfo;
            IncludeStackTrace = includeStackTrace;
            LogExceptions = logExceptions;
            ExceptionLogLevel = exceptionLogLevel;
            PublishToMessageBus = publishToMessageBus;
            EnableBatchProcessing = enableBatchProcessing;
            ConditionalLogging = conditionalLogging;
            MaxParameterLength = maxParameterLength;
            ExcludedParameters = excludedParameters ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates a new WithLogAttribute with a custom tag and comprehensive configuration.
        /// </summary>
        /// <param name="customTag">The custom tag string for log messages.</param>
        /// <param name="minimumLevel">The minimum log level for generated messages.</param>
        /// <param name="tagCategory">The tag category for enhanced organization.</param>
        /// <param name="logEntry">Whether to log method entry.</param>
        /// <param name="logExit">Whether to log method exit.</param>
        /// <param name="logParameters">Whether to log method parameters.</param>
        /// <param name="logReturnValue">Whether to log method return value.</param>
        /// <param name="trackTime">Whether to track method execution time.</param>
        /// <param name="enableProfiling">Whether to integrate with the profiling system.</param>
        /// <param name="performanceThresholdMs">Performance threshold for warning logs.</param>
        /// <param name="enableStructuredLogging">Whether to use structured logging.</param>
        /// <param name="includeSourceContext">Whether to include source context.</param>
        /// <param name="includeThreadInfo">Whether to include thread information.</param>
        /// <param name="includeStackTrace">Whether to include stack trace.</param>
        /// <param name="logExceptions">Whether to automatically log exceptions.</param>
        /// <param name="exceptionLogLevel">Log level for exception logging.</param>
        /// <param name="publishToMessageBus">Whether to publish to message bus.</param>
        /// <param name="enableBatchProcessing">Whether to enable batch processing.</param>
        /// <param name="conditionalLogging">Whether to enable conditional logging.</param>
        /// <param name="maxParameterLength">Maximum parameter value length.</param>
        /// <param name="excludedParameters">Parameter names to exclude from logging.</param>
        public WithLogAttribute(
            string customTag,
            LogLevel minimumLevel = LogLevel.Debug,
            Tagging.TagCategory tagCategory = Tagging.TagCategory.Gameplay,
            bool logEntry = true,
            bool logExit = true,
            bool logParameters = false,
            bool logReturnValue = false,
            bool trackTime = false,
            bool enableProfiling = false,
            double performanceThresholdMs = 100.0,
            bool enableStructuredLogging = true,
            bool includeSourceContext = true,
            bool includeThreadInfo = false,
            bool includeStackTrace = false,
            bool logExceptions = true,
            LogLevel exceptionLogLevel = LogLevel.Error,
            bool publishToMessageBus = false,
            bool enableBatchProcessing = false,
            bool conditionalLogging = true,
            int maxParameterLength = 1024,
            string[] excludedParameters = null)
        {
            if (string.IsNullOrWhiteSpace(customTag))
                throw new ArgumentException("Custom tag cannot be null or empty.", nameof(customTag));

            MinimumLevel = minimumLevel;
            Tag = Tagging.LogTag.Undefined;
            TagCategory = tagCategory;
            CustomTag = customTag;
            LogEntry = logEntry;
            LogExit = logExit;
            LogParameters = logParameters;
            LogReturnValue = logReturnValue;
            TrackTime = trackTime;
            EnableProfiling = enableProfiling;
            PerformanceThresholdMs = performanceThresholdMs;
            EnableStructuredLogging = enableStructuredLogging;
            IncludeSourceContext = includeSourceContext;
            IncludeThreadInfo = includeThreadInfo;
            IncludeStackTrace = includeStackTrace;
            LogExceptions = logExceptions;
            ExceptionLogLevel = exceptionLogLevel;
            PublishToMessageBus = publishToMessageBus;
            EnableBatchProcessing = enableBatchProcessing;
            ConditionalLogging = conditionalLogging;
            MaxParameterLength = maxParameterLength;
            ExcludedParameters = excludedParameters ?? Array.Empty<string>();
        }

        #endregion

        #region Convenience Factory Methods

        /// <summary>
        /// Creates a lightweight logging attribute for high-frequency scenarios.
        /// </summary>
        /// <param name="tag">The tag for log messages.</param>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <returns>A configured WithLogAttribute optimized for performance.</returns>
        public static WithLogAttribute Lightweight(
            Tagging.LogTag tag = Tagging.LogTag.Performance,
            LogLevel minimumLevel = LogLevel.Trace)
        {
            return new WithLogAttribute(
                minimumLevel: minimumLevel,
                tag: tag,
                logEntry: true,
                logExit: true,
                logParameters: false,
                logReturnValue: false,
                trackTime: true,
                enableProfiling: false,
                enableStructuredLogging: false,
                includeSourceContext: false,
                enableBatchProcessing: true);
        }

        /// <summary>
        /// Creates a comprehensive debugging attribute with full logging capabilities.
        /// </summary>
        /// <param name="tag">The tag for log messages.</param>
        /// <returns>A configured WithLogAttribute for debugging scenarios.</returns>
        public static WithLogAttribute Debug(Tagging.LogTag tag = Tagging.LogTag.Debug)
        {
            return new WithLogAttribute(
                minimumLevel: LogLevel.Debug,
                tag: tag,
                logEntry: true,
                logExit: true,
                logParameters: true,
                logReturnValue: true,
                trackTime: true,
                enableProfiling: true,
                enableStructuredLogging: true,
                includeSourceContext: true,
                includeThreadInfo: true,
                includeStackTrace: true,
                logExceptions: true,
                publishToMessageBus: true);
        }

        /// <summary>
        /// Creates a performance-focused attribute with profiling integration.
        /// </summary>
        /// <param name="thresholdMs">Performance threshold in milliseconds.</param>
        /// <param name="tag">The tag for log messages.</param>
        /// <returns>A configured WithLogAttribute for performance monitoring.</returns>
        public static WithLogAttribute Performance(
            double thresholdMs = 50.0,
            Tagging.LogTag tag = Tagging.LogTag.Performance)
        {
            return new WithLogAttribute(
                minimumLevel: LogLevel.Info,
                tag: tag,
                logEntry: false,
                logExit: false,
                logParameters: false,
                logReturnValue: false,
                trackTime: true,
                enableProfiling: true,
                performanceThresholdMs: thresholdMs,
                enableStructuredLogging: true,
                publishToMessageBus: true);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the effective tag string to use for logging.
        /// </summary>
        /// <returns>The custom tag if specified, otherwise the string representation of the predefined tag.</returns>
        public string GetEffectiveTag()
        {
            return !string.IsNullOrEmpty(CustomTag) ? CustomTag : Tag.ToString();
        }

        /// <summary>
        /// Determines if the specified log level meets the minimum threshold.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the level meets or exceeds the minimum level.</returns>
        public bool ShouldLog(LogLevel level)
        {
            return level >= MinimumLevel;
        }

        /// <summary>
        /// Determines if a parameter should be excluded from logging.
        /// </summary>
        /// <param name="parameterName">The parameter name to check.</param>
        /// <returns>True if the parameter should be excluded.</returns>
        public bool IsParameterExcluded(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName) || ExcludedParameters == null)
                return false;

            for (int i = 0; i < ExcludedParameters.Length; i++)
            {
                if (string.Equals(ExcludedParameters[i], parameterName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        #endregion
    }
}