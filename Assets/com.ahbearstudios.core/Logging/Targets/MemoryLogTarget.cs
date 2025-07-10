using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
/// <summary>
    /// A simple memory-based log target for high-performance scenarios.
    /// </summary>
    internal sealed class MemoryLogTarget : ILogTarget
    {
        private readonly List<LogMessage> _messages = new();
        private readonly object _lock = new();
        private bool _disposed = false;

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy => !_disposed;
        public IReadOnlyList<string> Channels { get; }

        public MemoryLogTarget(LogTargetConfig config)
        {
            Name = config.Name;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.IsEnabled;
            Channels = config.Channels.AsReadOnly();
        }

        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage)) return;

            lock (_lock)
            {
                if (!_disposed)
                {
                    _messages.Add(logMessage);
                }
            }
        }

        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    foreach (var message in logMessages)
                    {
                        if (ShouldProcessMessage(message))
                        {
                            _messages.Add(message);
                        }
                    }
                }
            }
        }

        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            return IsEnabled && logMessage.Level >= MinimumLevel;
        }

        public void Flush()
        {
            // Memory target doesn't need flushing
        }

        public bool PerformHealthCheck()
        {
            return !_disposed;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    _messages.Clear();
                    _disposed = true;
                }
            }
        }
    }
}