using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Implementation of ILogConfigBuilder that provides a fluent interface for building logging configurations.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// </summary>
    public sealed class LogConfigBuilder : ILogConfigBuilder
    {
        private LogLevel _globalMinimumLevel = LogLevel.Info;
        private bool _loggingEnabled = true;
        private int _maxQueueSize = 1000;
        private TimeSpan _flushInterval = TimeSpan.FromMilliseconds(100);
        private bool _highPerformanceMode = false;
        private bool _burstCompatibility = false;
        private bool _structuredLogging = true;
        private string _correlationIdFormat = "{0:N}";
        private bool _autoCorrelationId = true;
        private string _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
        private bool _includeTimestamps = true;
        private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        
        private readonly List<LogTargetConfig> _targetConfigs = new();
        private readonly List<LogChannelConfig> _channelConfigs = new();

        /// <summary>
        /// Initializes a new instance of the LogConfigBuilder.
        /// </summary>
        public LogConfigBuilder()
        {
            // Add default channel configuration
            _channelConfigs.Add(new LogChannelConfig
            {
                Name = "Default",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true
            });
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithGlobalMinimumLevel(LogLevel logLevel)
        {
            _globalMinimumLevel = logLevel;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithLoggingEnabled(bool enabled)
        {
            _loggingEnabled = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithMaxQueueSize(int maxQueueSize)
        {
            if (maxQueueSize <= 0)
                throw new ArgumentException("Max queue size must be greater than zero.", nameof(maxQueueSize));
            
            _maxQueueSize = maxQueueSize;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithFlushInterval(TimeSpan flushInterval)
        {
            if (flushInterval <= TimeSpan.Zero)
                throw new ArgumentException("Flush interval must be greater than zero.", nameof(flushInterval));
            
            _flushInterval = flushInterval;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithHighPerformanceMode(bool enabled)
        {
            _highPerformanceMode = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithBurstCompatibility(bool enabled)
        {
            _burstCompatibility = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithStructuredLogging(bool enabled)
        {
            _structuredLogging = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithTarget(LogTargetConfig targetConfig)
        {
            if (targetConfig == null)
                throw new ArgumentNullException(nameof(targetConfig));
            
            _targetConfigs.Add(targetConfig);
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithTargets(params LogTargetConfig[] targetConfigs)
        {
            if (targetConfigs == null)
                throw new ArgumentNullException(nameof(targetConfigs));
            
            return WithTargets(targetConfigs.AsEnumerable());
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithTargets(IEnumerable<LogTargetConfig> targetConfigs)
        {
            if (targetConfigs == null)
                throw new ArgumentNullException(nameof(targetConfigs));
            
            _targetConfigs.AddRange(targetConfigs);
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithChannel(LogChannelConfig channelConfig)
        {
            if (channelConfig == null)
                throw new ArgumentNullException(nameof(channelConfig));
            
            _channelConfigs.Add(channelConfig);
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithChannels(params LogChannelConfig[] channelConfigs)
        {
            if (channelConfigs == null)
                throw new ArgumentNullException(nameof(channelConfigs));
            
            return WithChannels(channelConfigs.AsEnumerable());
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithChannels(IEnumerable<LogChannelConfig> channelConfigs)
        {
            if (channelConfigs == null)
                throw new ArgumentNullException(nameof(channelConfigs));
            
            _channelConfigs.AddRange(channelConfigs);
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithCorrelationIdFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Correlation ID format cannot be null or empty.", nameof(format));
            
            _correlationIdFormat = format;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithAutoCorrelationId(bool enabled)
        {
            _autoCorrelationId = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithMessageFormat(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("Message format template cannot be null or empty.", nameof(template));
            
            _messageFormat = template;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithTimestamps(bool enabled)
        {
            _includeTimestamps = enabled;
            return this;
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithTimestampFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Timestamp format cannot be null or empty.", nameof(format));
            
            _timestampFormat = format;
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            // Validate global settings
            if (_maxQueueSize <= 0)
                errors.Add("Max queue size must be greater than zero.");

            if (_flushInterval <= TimeSpan.Zero)
                errors.Add("Flush interval must be greater than zero.");

            if (string.IsNullOrWhiteSpace(_correlationIdFormat))
                errors.Add("Correlation ID format cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(_messageFormat))
                errors.Add("Message format template cannot be null or empty.");

            if (_includeTimestamps && string.IsNullOrWhiteSpace(_timestampFormat))
                errors.Add("Timestamp format cannot be null or empty when timestamps are enabled.");

            // Validate target configurations
            if (_targetConfigs.Count == 0)
                errors.Add("At least one log target must be configured.");

            var targetNames = new HashSet<string>();
            foreach (var target in _targetConfigs)
            {
                if (target == null)
                {
                    errors.Add("Target configuration cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(target.Name))
                {
                    errors.Add("Target name cannot be null or empty.");
                }
                else if (!targetNames.Add(target.Name))
                {
                    errors.Add($"Duplicate target name: {target.Name}");
                }
            }

            // Validate channel configurations
            if (_channelConfigs.Count == 0)
                errors.Add("At least one log channel must be configured.");

            var channelNames = new HashSet<string>();
            foreach (var channel in _channelConfigs)
            {
                if (channel == null)
                {
                    errors.Add("Channel configuration cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(channel.Name))
                {
                    errors.Add("Channel name cannot be null or empty.");
                }
                else if (!channelNames.Add(channel.Name))
                {
                    errors.Add($"Duplicate channel name: {channel.Name}");
                }
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public LoggingConfig Build()
        {
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            return new LoggingConfig
            {
                GlobalMinimumLevel = _globalMinimumLevel,
                IsLoggingEnabled = _loggingEnabled,
                MaxQueueSize = _maxQueueSize,
                FlushInterval = _flushInterval,
                HighPerformanceMode = _highPerformanceMode,
                BurstCompatibility = _burstCompatibility,
                StructuredLogging = _structuredLogging,
                CorrelationIdFormat = _correlationIdFormat,
                AutoCorrelationId = _autoCorrelationId,
                MessageFormat = _messageFormat,
                IncludeTimestamps = _includeTimestamps,
                TimestampFormat = _timestampFormat,
                TargetConfigs = _targetConfigs.ToList().AsReadOnly(),
                ChannelConfigs = _channelConfigs.ToList().AsReadOnly()
            };
        }

        /// <inheritdoc />
        public ILogConfigBuilder Reset()
        {
            _globalMinimumLevel = LogLevel.Info;
            _loggingEnabled = true;
            _maxQueueSize = 1000;
            _flushInterval = TimeSpan.FromMilliseconds(100);
            _highPerformanceMode = false;
            _burstCompatibility = false;
            _structuredLogging = true;
            _correlationIdFormat = "{0:N}";
            _autoCorrelationId = true;
            _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            _includeTimestamps = true;
            _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            
            _targetConfigs.Clear();
            _channelConfigs.Clear();
            
            // Add default channel configuration
            _channelConfigs.Add(new LogChannelConfig
            {
                Name = "Default",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true
            });
            
            return this;
        }
    }
}