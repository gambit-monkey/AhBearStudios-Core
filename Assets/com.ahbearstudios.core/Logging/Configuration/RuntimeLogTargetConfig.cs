using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Runtime implementation of ILogTargetConfig that can be used with builders
    /// </summary>
    public class RuntimeLogTargetConfig : ILogTargetConfig
    {
        public string TargetName { get; set; } = "RuntimeTarget";
        public bool Enabled { get; set; } = true;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
        public string[] IncludedTags { get; set; } = new string[0];
        public string[] ExcludedTags { get; set; } = new string[0];
        public bool ProcessUntaggedMessages { get; set; } = true;
        public bool CaptureUnityLogs { get; set; } = true;
        public bool IncludeStackTraces { get; set; } = true;
        public bool IncludeTimestamps { get; set; } = true;
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public bool IncludeSourceContext { get; set; } = true;
        public bool IncludeThreadId { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public bool AutoFlush { get; set; } = true;
        public int BufferSize { get; set; } = 0;
        public float FlushIntervalSeconds { get; set; } = 0;
        public bool LimitMessageLength { get; set; } = false;
        public int MaxMessageLength { get; set; } = 8192;

        public virtual ILogTarget CreateTarget()
        {
            throw new System.NotImplementedException("Runtime configs need specific implementations");
        }

        public virtual void ApplyTagFilters(ILogTarget target)
        {
            target?.SetTagFilters(IncludedTags, ExcludedTags, ProcessUntaggedMessages);
        }

        public virtual ILogTargetConfig Clone()
        {
            return new RuntimeLogTargetConfig
            {
                TargetName = this.TargetName,
                Enabled = this.Enabled,
                MinimumLevel = this.MinimumLevel,
                IncludedTags = (string[])this.IncludedTags?.Clone(),
                ExcludedTags = (string[])this.ExcludedTags?.Clone(),
                ProcessUntaggedMessages = this.ProcessUntaggedMessages,
                CaptureUnityLogs = this.CaptureUnityLogs,
                IncludeStackTraces = this.IncludeStackTraces,
                IncludeTimestamps = this.IncludeTimestamps,
                TimestampFormat = this.TimestampFormat,
                IncludeSourceContext = this.IncludeSourceContext,
                IncludeThreadId = this.IncludeThreadId,
                EnableStructuredLogging = this.EnableStructuredLogging,
                AutoFlush = this.AutoFlush,
                BufferSize = this.BufferSize,
                FlushIntervalSeconds = this.FlushIntervalSeconds,
                LimitMessageLength = this.LimitMessageLength,
                MaxMessageLength = this.MaxMessageLength
            };
        }
    }
}