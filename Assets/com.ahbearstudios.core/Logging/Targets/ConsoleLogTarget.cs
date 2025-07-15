using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// A console-based log target for development scenarios with game-optimized performance.
    /// </summary>
    internal sealed class ConsoleLogTarget : ILogTarget
    {
        private readonly ILogTargetConfig _config;
        private volatile bool _disposed = false;
        private long _messagesWritten = 0;
        private long _messagesDropped = 0;

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy => !_disposed;
        public IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Initializes a new instance of ConsoleLogTarget with strongly-typed configuration.
        /// </summary>
        /// <param name="config">The target configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public ConsoleLogTarget(ILogTargetConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            Name = _config.Name;
            MinimumLevel = _config.MinimumLevel;
            IsEnabled = _config.IsEnabled;
            Channels = _config.Channels;
        }

        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage)) 
            {
                Interlocked.Increment(ref _messagesDropped);
                return;
            }

            try
            {
                Console.WriteLine(logMessage.Format());
                Interlocked.Increment(ref _messagesWritten);
            }
            catch
            {
                Interlocked.Increment(ref _messagesDropped);
                // Ignore console write failures to prevent exceptions in logging
            }
        }

        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            if (logMessages == null || logMessages.Count == 0) return;

            foreach (var message in logMessages)
            {
                Write(message);
            }
        }

        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (!IsEnabled || _disposed) return false;
            
            if (logMessage.Level < MinimumLevel) return false;
            
            // Check channel filtering
            if (Channels.Count > 0)
            {
                var messageChannel = logMessage.Channel.ToString();
                var channelFound = false;
                
                foreach (var channel in Channels)
                {
                    if (string.Equals(channel, messageChannel, StringComparison.OrdinalIgnoreCase))
                    {
                        channelFound = true;
                        break;
                    }
                }
                
                if (!channelFound) return false;
            }
            
            return true;
        }

        public void Flush()
        {
            // Console automatically flushes
        }

        public bool PerformHealthCheck()
        {
            return !_disposed;
        }

        /// <summary>
        /// Gets performance statistics for this target.
        /// </summary>
        /// <returns>Dictionary containing performance data</returns>
        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["MessagesWritten"] = _messagesWritten,
                ["MessagesDropped"] = _messagesDropped,
                ["IsHealthy"] = IsHealthy,
                ["Name"] = Name,
                ["MinimumLevel"] = MinimumLevel.ToString(),
                ["IsEnabled"] = IsEnabled,
                ["ChannelCount"] = Channels.Count
            };
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}