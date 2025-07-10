using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// A console-based log target for development scenarios.
    /// </summary>
    internal sealed class ConsoleLogTarget : ILogTarget
    {
        private bool _disposed = false;

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy => !_disposed;
        public IReadOnlyList<string> Channels { get; }

        public ConsoleLogTarget(LogTargetConfig config)
        {
            Name = config.Name;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.IsEnabled;
            Channels = config.Channels.AsReadOnly();
        }

        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage)) return;
            Console.WriteLine(logMessage.Format());
        }

        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            foreach (var message in logMessages)
            {
                Write(message);
            }
        }

        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            return IsEnabled && logMessage.Level >= MinimumLevel;
        }

        public void Flush()
        {
            // Console automatically flushes
        }

        public bool PerformHealthCheck()
        {
            return !_disposed;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}