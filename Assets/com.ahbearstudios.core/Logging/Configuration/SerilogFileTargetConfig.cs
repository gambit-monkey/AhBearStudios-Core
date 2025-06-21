using System;
using System.IO;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// POCO configuration for Serilog file log targets.
    /// </summary>
    public class SerilogFileTargetConfig : ILogTargetConfig
    {
        // ——— General Settings ———
        public string TargetName                { get; set; } = "SerilogFile";
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

        // ——— File Configuration ———
        public string LogFilePath    { get; set; } = "Logs/app.log";
        public bool   UseJsonFormat  { get; set; } = false;
        public bool   LogToConsole   { get; set; } = false;
        public int    RetainedDays   { get; set; } = 7;

        public SerilogFileTargetConfig() { }

        /// <summary>
        /// Create a SerilogTarget using this configuration.
        /// </summary>
        public ILogTarget CreateTarget(IMessageBus messageBus = null)
        {
            var path = ResolveFilePath(LogFilePath);
            EnsureDirectoryExists(path);

            var target = new SerilogTarget(this, messageBus);
            ApplyTagFilters(target);
            return target;
        }

        ILogTarget ILogTargetConfig.CreateTarget() => CreateTarget(null);

        /// <summary>
        /// Shallow‐clone of this config.
        /// </summary>
        public ILogTargetConfig Clone() => (SerilogFileTargetConfig)MemberwiseClone();

        // ——— Helpers ———

        private string ResolveFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "Logs/app.log";

            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppContext.BaseDirectory, path);

            return path;
        }

        private void EnsureDirectoryExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void ApplyTagFilters(ILogTarget target)
        {
            target.SetTagFilters(IncludedTags, ExcludedTags, ProcessUntaggedMessages);
        }
    }
}