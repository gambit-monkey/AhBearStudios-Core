using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Complete implementation of ILogConfigBuilder that provides a fluent interface for building logging configurations.
    /// Supports all available log targets and provides comprehensive scenario-specific presets.
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

        /// <inheritdoc />
        public ILogConfigBuilder WithBatching(bool enabled, int batchSize = 100)
        {
            if (enabled && batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than zero when batching is enabled.", nameof(batchSize));
            
            _batchingEnabled = enabled;
            _batchSize = batchSize;
            return this;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        public ILogConfigBuilder WithSerilogTarget(string name = "Serilog", LogLevel minimumLevel = LogLevel.Info, object loggerConfiguration = null)
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Serilog",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = true,
                BufferSize = 500,
                Properties = new Dictionary<string, object>()
            };

            if (loggerConfiguration != null)
            {
                targetConfig.Properties["LoggerConfiguration"] = loggerConfiguration;
            }
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithNullTarget(string name = "Null")
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Null",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true,
                UseAsyncWrite = false // No point in async for null target
            };
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithStandardConsoleTarget(string name = "StdConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true)
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Console",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = false, // Console output is typically synchronous
                Properties = new Dictionary<string, object>
                {
                    ["UseColors"] = useColors
                }
            };
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithUnityConsoleTarget(string name = "UnityConsole", LogLevel minimumLevel = LogLevel.Debug, bool useColors = true, bool showStackTraces = true)
        {
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "UnityConsole",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = false, // Unity Console is synchronous
                Properties = new Dictionary<string, object>
                {
                    ["UseColors"] = useColors,
                    ["ShowStackTraces"] = showStackTraces,
                    ["IncludeTimestamp"] = true,
                    ["IncludeThreadId"] = false
                }
            };
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithNetworkTarget(string name, string endpoint, LogLevel minimumLevel = LogLevel.Info, int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Target name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Network endpoint cannot be null or empty.", nameof(endpoint));
            
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Network",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = true, // Network operations should be async
                BufferSize = 200,
                Properties = new Dictionary<string, object>
                {
                    ["Endpoint"] = endpoint,
                    ["TimeoutSeconds"] = timeoutSeconds,
                    ["RetryCount"] = 3,
                    ["RetryDelayMs"] = 1000
                }
            };
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithDatabaseTarget(string name, string connectionString, string tableName = "Logs", LogLevel minimumLevel = LogLevel.Info)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Target name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Database",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = true,
                BufferSize = 100,
                Properties = new Dictionary<string, object>
                {
                    ["ConnectionString"] = connectionString,
                    ["TableName"] = tableName,
                    ["BatchInsert"] = true,
                    ["CreateTableIfNotExists"] = true
                }
            };
            
            return WithTarget(targetConfig);
        }

        /// <inheritdoc />
        public ILogConfigBuilder WithEmailTarget(string name, string smtpServer, string fromEmail, string[] toEmails, LogLevel minimumLevel = LogLevel.Error)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Target name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(smtpServer))
                throw new ArgumentException("SMTP server cannot be null or empty.", nameof(smtpServer));
            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new ArgumentException("From email cannot be null or empty.", nameof(fromEmail));
            if (toEmails == null || toEmails.Length == 0)
                throw new ArgumentException("At least one recipient email is required.", nameof(toEmails));
            
            var targetConfig = new LogTargetConfig
            {
                Name = name,
                TargetType = "Email",
                MinimumLevel = minimumLevel,
                IsEnabled = true,
                UseAsyncWrite = true,
                Properties = new Dictionary<string, object>
                {
                    ["SmtpServer"] = smtpServer,
                    ["FromEmail"] = fromEmail,
                    ["ToEmails"] = toEmails,
                    ["Subject"] = "Application Log Alert",
                    ["MaxEmailsPerHour"] = 10 // Prevent email spam
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ILogConfigBuilder ForProduction()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Info)
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 200)
                .WithCaching(true, 2000)
                .WithStructuredLogging(true)
                .WithSerilogTarget("Production", LogLevel.Info)           // Enterprise logging
                .WithFileTarget("ErrorLog", "logs/errors.log", LogLevel.Error, 500) // Error-specific file
                .WithMemoryTarget("Recent", 5000, LogLevel.Warning)       // Recent critical events
                .WithEmailTarget("Alerts", "smtp.company.com", "app@company.com", 
                    new[] { "ops@company.com" }, LogLevel.Critical);      // Critical alerts
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForDevelopment()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(false)
                .WithBatching(false)
                .WithStructuredLogging(true)
                .WithTimestamps(true)
                .WithUnityConsoleTarget("Unity", LogLevel.Debug, true, true) // Unity console with stack traces
                .WithStandardConsoleTarget("Console", LogLevel.Debug, true)  // Standard console
                .WithMemoryTarget("Debug", 10000, LogLevel.Debug)            // Large debug buffer
                .WithFileTarget("DevLog", "logs/development.log", LogLevel.Debug, 100); // Dev file logging
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForTesting()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(false)
                .WithBatching(false)
                .WithStructuredLogging(true)
                .WithMemoryTarget("TestCapture", 50000, LogLevel.Debug)     // Large test capture
                .WithNullTarget("Null");                                    // Null target for disabled scenarios
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForStaging()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 150)
                .WithCaching(true, 1500)
                .WithStructuredLogging(true)
                .WithSerilogTarget("Staging", LogLevel.Debug)               // Full Serilog capture
                .WithFileTarget("StagingApp", "logs/staging-app.log", LogLevel.Info, 300)
                .WithFileTarget("StagingError", "logs/staging-errors.log", LogLevel.Error, 200)
                .WithMemoryTarget("StagingRecent", 3000, LogLevel.Info)
                .WithUnityConsoleTarget("Unity", LogLevel.Warning, true, false); // Warnings+ only
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForPerformanceTesting()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Warning)                   // Reduce log volume
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 500)                                    // Large batches
                .WithCaching(true, 5000)                                    // Aggressive caching
                .WithStructuredLogging(false)                               // Reduce allocations
                .WithMemoryTarget("PerfTest", 20000, LogLevel.Warning)      // Memory only for speed
                .WithNullTarget("Disabled");                                // Null for comparison
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForHighAvailability()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Info)
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 300)
                .WithCaching(true, 3000)
                .WithStructuredLogging(true)
                .WithSerilogTarget("Primary", LogLevel.Info)                // Primary enterprise logging
                .WithDatabaseTarget("DbLogs", "Server=db;Database=Logs", "ApplicationLogs", LogLevel.Warning)
                .WithNetworkTarget("RemoteLogs", "https://logs.company.com/api", LogLevel.Error, 60)
                .WithEmailTarget("CriticalAlerts", "smtp.company.com", "system@company.com", 
                    new[] { "oncall@company.com", "ops@company.com" }, LogLevel.Critical)
                .WithMemoryTarget("HABuffer", 10000, LogLevel.Info);        // High-availability buffer
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForCloudDeployment()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Info)
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 250)
                .WithCaching(true, 2500)
                .WithStructuredLogging(true)
                .WithNetworkTarget("CloudLogs", "https://logging-service.cloud.com/ingest", LogLevel.Info, 120)
                .WithMemoryTarget("CloudBuffer", 5000, LogLevel.Warning)    // Cloud failover buffer
                .WithFileTarget("LocalBackup", "/var/log/app/backup.log", LogLevel.Error, 200) // Local backup
                .WithEmailTarget("CloudAlerts", "smtp.cloud.com", "alerts@app.com", 
                    new[] { "devops@company.com" }, LogLevel.Critical);
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForMobile()
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Warning)                   // Minimize mobile logging
                .WithHighPerformanceMode(true)
                .WithBurstCompatibility(true)
                .WithBatching(true, 100)                                    // Small batches for memory
                .WithCaching(true, 500)                                     // Limited caching
                .WithStructuredLogging(false)                               // Reduce allocations
                .WithMemoryTarget("Mobile", 1000, LogLevel.Warning)         // Small memory buffer
                .WithFileTarget("MobileErrors", "logs/mobile-errors.log", LogLevel.Error, 50); // Critical errors only
        }

        /// <inheritdoc />
        public ILogConfigBuilder ForDebugging(string debugChannel = "Debug")
        {
            return this
                .WithGlobalMinimumLevel(LogLevel.Debug)
                .WithHighPerformanceMode(false)                             // Favor completeness over speed
                .WithBatching(false)                                        // Immediate output
                .WithStructuredLogging(true)
                .WithTimestamps(true)
                .WithUnityConsoleTarget("Unity", LogLevel.Debug, true, true)
                .WithFileTarget("DebugTrace", $"logs/debug-{debugChannel}.log", LogLevel.Debug, 50)
                .WithMemoryTarget("DebugCapture", 25000, LogLevel.Debug)    // Large debug capture
                .WithChannel(debugChannel, LogLevel.Debug, true);           // Specific debug channel
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