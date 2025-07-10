using System;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// POCO configuration for Unity Console log targets.
    /// </summary>
    public class UnityConsoleTargetConfig : ILogTargetConfig
    {
        // ——— General Settings ———
        public string TargetName                { get; set; } = "UnityConsole";
        public bool   Enabled                   { get; set; } = true;
        public LogLevel MinimumLevel            { get; set; } = LogLevel.Debug;

        // ——— Tag Filtering ———
        public string[] IncludedTags            { get; set; } = Array.Empty<string>();
        public string[] ExcludedTags            { get; set; } = Array.Empty<string>();
        public bool     ProcessUntaggedMessages { get; set; } = true;

        // ——— Unity Integration ———
        public bool CaptureUnityLogs            { get; set; } = true;

        // ——— Message Formatting ———
        public bool   IncludeStackTraces        { get; set; } = true;
        public bool   IncludeTimestamps         { get; set; } = true;
        public string TimestampFormat           { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public bool   IncludeSourceContext      { get; set; } = true;
        public bool   IncludeThreadId           { get; set; } = false;
        public bool   EnableStructuredLogging   { get; set; } = false;

        // ——— Performance ———
        public bool  AutoFlush              { get; set; } = true;
        public int   BufferSize             { get; set; } = 0;
        public float FlushIntervalSeconds   { get; set; } = 0f;
        public bool  LimitMessageLength     { get; set; } = false;
        public int   MaxMessageLength       { get; set; } = 8192;

        // ——— Console Configuration ———
        public ColorizedConsoleFormatter Formatter             { get; set; }
        public bool                       UseColorizedOutput  { get; set; } = true;
        public bool                       RegisterUnityLogHandler     { get; set; } = true;
        public bool                       DuplicateToOriginalHandler  { get; set; } = false;

        public UnityConsoleTargetConfig() { }

        /// <summary>
        /// Create a UnityConsoleTarget using this configuration.
        /// </summary>
        public ILogTarget CreateTarget(IMessageBusService messageBusService = null)
        {
            var target = new UnityConsoleTarget(this, Formatter, messageBusService);
            ApplyTagFilters(target);
            return target;
        }

        ILogTarget ILogTargetConfig.CreateTarget() => CreateTarget(null);

        /// <summary>
        /// Shallow‐clone of this config.
        /// </summary>
        public ILogTargetConfig Clone() => (UnityConsoleTargetConfig)MemberwiseClone();

        // ——— Helpers ———

        public void ApplyTagFilters(ILogTarget target)
        {
            target.SetTagFilters(IncludedTags, ExcludedTags, ProcessUntaggedMessages);
        }
    }
}