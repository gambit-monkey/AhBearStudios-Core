using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Extensions
{
    /// <summary>
    /// Extension methods for log configuration builders to provide common configuration patterns
    /// </summary>
    public static class LogConfigBuilderExtensions
    {
        /// <summary>
        /// Configures builder for game development with appropriate settings
        /// </summary>
        public static SerilogFileConfigBuilder ForGameDevelopment(this SerilogFileConfigBuilder builder)
        {
            return builder
                .WithMinimumLevel(LogLevel.Debug)
                .WithJsonFormat(true)
                .WithConsoleOutput(true)
                .WithTimestamps(true, "HH:mm:ss.fff")
                .WithMessageFormatting(includeStackTraces: true, includeSourceContext: true)
                .WithPerformance(autoFlush: true, bufferSize: 512)
                .WithRetention(14); // Keep logs for 2 weeks
        }

        /// <summary>
        /// Configures builder for production deployment
        /// </summary>
        public static SerilogFileConfigBuilder ForProduction(this SerilogFileConfigBuilder builder)
        {
            return builder
                .WithMinimumLevel(LogLevel.Warning)
                .WithJsonFormat(false)
                .WithConsoleOutput(false)
                .WithTimestamps(true, "yyyy-MM-dd HH:mm:ss")
                .WithMessageFormatting(includeStackTraces: false, includeSourceContext: false)
                .WithPerformance(autoFlush: false, bufferSize: 2048, flushInterval: 5.0f)
                .WithRetention(30); // Keep logs for 30 days
        }

        /// <summary>
        /// Configures builder for mobile development with size constraints
        /// </summary>
        public static SerilogFileConfigBuilder ForMobile(this SerilogFileConfigBuilder builder)
        {
            return builder
                .WithMinimumLevel(LogLevel.Info)
                .WithJsonFormat(false)
                .WithConsoleOutput(false)
                .WithTimestamps(false) // Save space
                .WithMessageFormatting(includeStackTraces: false, includeSourceContext: false)
                .WithMessageLengthLimit(true, 512) // Limit message size
                .WithPerformance(autoFlush: false, bufferSize: 1024, flushInterval: 10.0f)
                .WithRetention(7); // Keep logs for 1 week only
        }

        /// <summary>
        /// Configures Unity console for editor development
        /// </summary>
        public static UnityConsoleConfigBuilder ForEditor(this UnityConsoleConfigBuilder builder)
        {
            return builder
                .WithMinimumLevel(LogLevel.Debug)
                .WithColorizedOutput(true)
                .WithTimestamps(true, "HH:mm:ss.fff")
                .WithMessageFormatting(includeStackTraces: true, includeSourceContext: true)
                .WithUnityLogHandlerRegistration(true, false);
        }

        /// <summary>
        /// Configures Unity console for build testing
        /// </summary>
        public static UnityConsoleConfigBuilder ForBuildTesting(this UnityConsoleConfigBuilder builder)
        {
            return builder
                .WithMinimumLevel(LogLevel.Info)
                .WithColorizedOutput(false)
                .WithTimestamps(true, "HH:mm:ss")
                .WithMessageFormatting(includeStackTraces: false, includeSourceContext: true)
                .WithUnityLogHandlerRegistration(true, false);
        }

        /// <summary>
        /// Sets up tag filtering for specific systems
        /// </summary>
        public static T WithSystemTags<T>(this T builder, params string[] systemTags)
            where T : ILogTargetConfigBuilder<ILogTargetConfig, T>
        {
            return builder.WithTagFilters(systemTags, new string[0], true);
        }

        /// <summary>
        /// Excludes verbose systems from logging
        /// </summary>
        public static T WithoutVerboseSystems<T>(this T builder)
            where T : ILogTargetConfigBuilder<ILogTargetConfig, T>
        {
            return builder.WithTagFilters(
                new string[0],
                new string[] { "Rendering", "Physics", "Animation", "UI" },
                true);
        }
    }
}