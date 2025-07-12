using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Enhanced implementation of ILogConfigBuilder that provides a fluent interface for building logging configurations.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture with complete pattern integration.
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
        private bool _batchingEnabled = false;
        private int _batchSize = 100;
        private string _correlationIdFormat = "{0:N}";
        private bool _autoCorrelationId = true;
        private string _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
        private bool _includeTimestamps = true;
        private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private bool _cachingEnabled = true;
        private int _maxCacheSize = 1000;
        
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

        /// <summary>
        /// Enables or disables batching for high-performance logging scenarios.
        /// </summary>
        /// <param name="enabled">True to enable batching</param>
        /// <param name="batchSize">The size of each batch (default: 100)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithBatching(bool enabled, int batchSize = 100)
        {
            if (enabled && batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than zero when batching is enabled.", nameof(batchSize));
            
            _batchingEnabled = enabled;
            _batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Enables or disables message caching for performance optimization.
        /// </summary>
        /// <param name="enabled">True to enable caching</param>
        /// <param name="maxCacheSize">The maximum cache size (default: 1000)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithCaching(bool enabled, int maxCacheSize = 1000)
        {
            if (enabled && maxCacheSize <= 0)
                throw new ArgumentException("Max cache size must be greater than zero when caching is enabled.", nameof(maxCacheSize));
            
            _cachingEnabled = enabled;
            _maxCacheSize = maxCacheSize;
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

        /// <summary>
        /// Adds a console target with the specified configuration.
        /// </summary>
        /// <param name="name">The name of the target (default: "Console")</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithConsoleTarget(string name = "Console", LogLevel minimumLevel = LogLevel.Debug)
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Console",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = false // Console output is typically synchronous
            };
            
            return WithTarget(targetConfig);
        }

        /// <summary>
        /// Adds a file target with the specified configuration.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="filePath">The path to the log file</param>
        /// <param name="minimumLevel">The minimum log level (default: Info)</param>
        /// <param name="bufferSize">The buffer size for file writing (default: 100)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithFileTarget(string name, string filePath, LogLevel minimumLevel = LogLevel.Info, int bufferSize = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Target name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "File",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = true,
                BufferSize = bufferSize,
                Properties = new Dictionary<string, object>
                {
                    ["FilePath"] = filePath
                }
            };
            
            return WithTarget(targetConfig);
        }

        /// <summary>
        /// Adds a memory target for in-memory log storage.
        /// </summary>
        /// <param name="name">The name of the target (default: "Memory")</param>
        /// <param name="maxEntries">The maximum number of entries to store (default: 1000)</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithMemoryTarget(string name = "Memory", int maxEntries = 1000, LogLevel minimumLevel = LogLevel.Debug)
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Memory",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = false,
                Properties = new Dictionary<string, object>
                {
                    ["MaxEntries"] = maxEntries
                }
            };
            
            return WithTarget(targetConfig);
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

        /// <summary>
        /// Adds a channel with the specified configuration.
        /// </summary>
        /// <param name="name">The channel name</param>
        /// <param name="minimumLevel">The minimum log level for the channel</param>
        /// <param name="enabled">Whether the channel is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder WithChannel(string name, LogLevel minimumLevel = LogLevel.Debug, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name cannot be null or empty.", nameof(name));
            
            var channelConfig = new LogChannelConfig
            {
                Name = name,
                MinimumLevel = minimumLevel,
                IsEnabled = enabled
            };
            
            return WithChannel(channelConfig);
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

        /// <summary>
        /// Configures the builder for production use with optimized settings.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder ForProduction()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Info)
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 100)
                .WithCaching(true, 1000)
                .WithStructuredLogging(true);
        }

        /// <summary>
        /// Configures the builder for development use with debugging-friendly settings.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder ForDevelopment()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(false)
                .WithBatching(false)
                .WithStructuredLogging(true)
                .WithTimestamps(true)
                .WithConsoleTarget()
                .WithMemoryTarget();
        }

        /// <summary>
        /// Configures the builder for testing scenarios.
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public ILogConfigBuilder ForTesting()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(false)
                .WithBatching(false)
                .WithMemoryTarget("TestMemory", 10000);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (_maxQueueSize <= 0)
                errors.Add("Max queue size must be greater than zero.");

            if (_flushInterval <= TimeSpan.Zero)
                errors.Add("Flush interval must be greater than zero.");

            if (_batchingEnabled && _batchSize <= 0)
                errors.Add("Batch size must be greater than zero when batching is enabled.");

            if (string.IsNullOrWhiteSpace(_correlationIdFormat))
                errors.Add("Correlation ID format cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(_messageFormat))
                errors.Add("Message format template cannot be null or empty.");

            if (_includeTimestamps && string.IsNullOrWhiteSpace(_timestampFormat))
                errors.Add("Timestamp format cannot be null or empty when timestamps are enabled.");

            if (_cachingEnabled && _maxCacheSize <= 0)
                errors.Add("Max cache size must be greater than zero when caching is enabled.");

            // Validate target configurations
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

                var targetErrors = target.Validate();
                errors.AddRange(targetErrors.Select(e => $"Target '{target.Name}': {e}"));
            }

            // Validate channel configurations
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

                var channelErrors = channel.Validate();
                errors.AddRange(channelErrors.Select(e => $"Channel '{channel.Name}': {e}"));
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
                BatchingEnabled = _batchingEnabled,
                BatchSize = _batchSize,
                CorrelationIdFormat = _correlationIdFormat,
                AutoCorrelationId = _autoCorrelationId,
                MessageFormat = _messageFormat,
                IncludeTimestamps = _includeTimestamps,
                TimestampFormat = _timestampFormat,
                CachingEnabled = _cachingEnabled,
                MaxCacheSize = _maxCacheSize,
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
            _batchingEnabled = false;
            _batchSize = 100;
            _correlationIdFormat = "{0:N}";
            _autoCorrelationId = true;
            _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            _includeTimestamps = true;
            _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            _cachingEnabled = true;
            _maxCacheSize = 1000;
            
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