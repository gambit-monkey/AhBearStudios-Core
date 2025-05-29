using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Extensions
{
    /// <summary>
    /// Extension methods for LogTargetConfig to enable builder-style configuration
    /// </summary>
    public static class LogTargetConfigExtensions
    {
        /// <summary>
        /// Creates a builder from an existing ScriptableObject config
        /// </summary>
        public static SerilogFileConfigBuilder ToBuilder(this SerilogFileConfig config)
        {
            return new SerilogFileConfigBuilder().FromExisting(config);
        }
        
        /// <summary>
        /// Creates a builder from an existing ScriptableObject config
        /// </summary>
        public static UnityConsoleConfigBuilder ToBuilder(this UnityConsoleLogConfig config)
        {
            return new UnityConsoleConfigBuilder().FromExisting(config);
        }
        
        /// <summary>
        /// Applies builder settings to an existing ScriptableObject config
        /// </summary>
        public static void ApplyBuilder(this SerilogFileConfig config, SerilogFileConfigBuilder builder)
        {
            // Use the builder's ApplyTo method instead of Build()
            builder.ApplyTo(config);
        }
        
        /// <summary>
        /// Applies builder settings to an existing ScriptableObject config
        /// </summary>
        public static void ApplyBuilder(this UnityConsoleLogConfig config, UnityConsoleConfigBuilder builder)
        {
            // Use the builder's ApplyTo method instead of Build()
            builder.ApplyTo(config);
        }
        
        /// <summary>
        /// Copies ConfigData to SerilogFileConfig ScriptableObject
        /// </summary>
        private static void CopyToScriptableObject(SerilogFileConfigBuilder.ConfigData data, SerilogFileConfig target)
        {
            target.TargetName = data.TargetName;
            target.Enabled = data.Enabled;
            target.MinimumLevel = data.MinimumLevel;
            target.IncludedTags = data.IncludedTags;
            target.ExcludedTags = data.ExcludedTags;
            target.ProcessUntaggedMessages = data.ProcessUntaggedMessages;
            target.CaptureUnityLogs = data.CaptureUnityLogs;
            target.IncludeStackTraces = data.IncludeStackTraces;
            target.IncludeTimestamps = data.IncludeTimestamps;
            target.TimestampFormat = data.TimestampFormat;
            target.IncludeSourceContext = data.IncludeSourceContext;
            target.IncludeThreadId = data.IncludeThreadId;
            target.EnableStructuredLogging = data.EnableStructuredLogging;
            target.AutoFlush = data.AutoFlush;
            target.BufferSize = data.BufferSize;
            target.FlushIntervalSeconds = data.FlushIntervalSeconds;
            target.LimitMessageLength = data.LimitMessageLength;
            target.MaxMessageLength = data.MaxMessageLength;
            // Add any SerilogFileConfig-specific properties here
        }
        
        /// <summary>
        /// Copies ConfigData to UnityConsoleLogConfig ScriptableObject
        /// </summary>
        private static void CopyToScriptableObject(UnityConsoleConfigBuilder.ConfigData data, UnityConsoleLogConfig target)
        {
            target.TargetName = data.TargetName;
            target.Enabled = data.Enabled;
            target.MinimumLevel = data.MinimumLevel;
            target.IncludedTags = data.IncludedTags;
            target.ExcludedTags = data.ExcludedTags;
            target.ProcessUntaggedMessages = data.ProcessUntaggedMessages;
            target.CaptureUnityLogs = data.CaptureUnityLogs;
            target.IncludeStackTraces = data.IncludeStackTraces;
            target.IncludeTimestamps = data.IncludeTimestamps;
            target.TimestampFormat = data.TimestampFormat;
            target.IncludeSourceContext = data.IncludeSourceContext;
            target.IncludeThreadId = data.IncludeThreadId;
            target.EnableStructuredLogging = data.EnableStructuredLogging;
            target.AutoFlush = data.AutoFlush;
            target.BufferSize = data.BufferSize;
            target.FlushIntervalSeconds = data.FlushIntervalSeconds;
            target.LimitMessageLength = data.LimitMessageLength;
            target.MaxMessageLength = data.MaxMessageLength;
            // Add any UnityConsoleLogConfig-specific properties here
            target.UseColorizedOutput = data.UseColorizedOutput;
            target.RegisterUnityLogHandler = data.RegisterUnityLogHandler;
            target.DuplicateToOriginalHandler = data.DuplicateToOriginalHandler;
        }
    }
}