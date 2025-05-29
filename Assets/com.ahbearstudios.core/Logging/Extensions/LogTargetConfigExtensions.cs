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
        /// Creates a builder from an existing ScriptableObject targetConfig
        /// </summary>
        public static SerilogFileConfigBuilder ToBuilder(this SerilogFileTargetConfig targetConfig)
        {
            return new SerilogFileConfigBuilder().FromExisting(targetConfig);
        }
        
        /// <summary>
        /// Creates a builder from an existing ScriptableObject config
        /// </summary>
        public static UnityConsoleConfigBuilder ToBuilder(this UnityConsoleTargetConfig config)
        {
            return new UnityConsoleConfigBuilder().FromExisting(config);
        }
        
        /// <summary>
        /// Applies builder settings to an existing ScriptableObject targetConfig
        /// </summary>
        public static void ApplyBuilder(this SerilogFileTargetConfig targetConfig, SerilogFileConfigBuilder builder)
        {
            // Use the builder's ApplyTo method instead of Build()
            builder.ApplyTo(targetConfig);
        }
        
        /// <summary>
        /// Applies builder settings to an existing ScriptableObject config
        /// </summary>
        public static void ApplyBuilder(this UnityConsoleTargetConfig config, UnityConsoleConfigBuilder builder)
        {
            // Use the builder's ApplyTo method instead of Build()
            builder.ApplyTo(config);
        }
        
        /// <summary>
        /// Copies ConfigData to SerilogFileTargetConfig ScriptableObject
        /// </summary>
        private static void CopyToScriptableObject(SerilogFileConfigBuilder.ConfigData data, SerilogFileTargetConfig target)
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
            // Add any SerilogFileTargetConfig-specific properties here
        }
        
        /// <summary>
        /// Copies ConfigData to UnityConsoleTargetConfig ScriptableObject
        /// </summary>
        private static void CopyToScriptableObject(UnityConsoleConfigBuilder.ConfigData data, UnityConsoleTargetConfig target)
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
            // Add any UnityConsoleTargetConfig-specific properties here
            target.UseColorizedOutput = data.UseColorizedOutput;
            target.RegisterUnityLogHandler = data.RegisterUnityLogHandler;
            target.DuplicateToOriginalHandler = data.DuplicateToOriginalHandler;
        }
    }
}