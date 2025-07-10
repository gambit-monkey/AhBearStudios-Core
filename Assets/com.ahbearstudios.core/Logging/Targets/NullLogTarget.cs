using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// A null log target that discards all messages (for testing/disabled scenarios).
    /// </summary>
    internal sealed class NullLogTarget : ILogTarget
    {
        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy => true;
        public IReadOnlyList<string> Channels { get; }

        public NullLogTarget(LogTargetConfig config)
        {
            Name = config.Name;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.IsEnabled;
            Channels = config.Channels.AsReadOnly();
        }

        public void Write(in LogMessage logMessage) { }
        public void WriteBatch(IReadOnlyList<LogMessage> logMessages) { }
        public bool ShouldProcessMessage(in LogMessage logMessage) => false;
        public void Flush() { }
        public bool PerformHealthCheck() => true;
        public void Dispose() { }
    }
}