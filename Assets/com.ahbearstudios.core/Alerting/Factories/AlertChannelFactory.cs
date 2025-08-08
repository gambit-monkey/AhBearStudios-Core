using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Concrete implementation of IAlertChannelFactory for creating and configuring alert channel instances.
    /// Supports various channel types including logging, console, file, memory, and Unity-specific channels.
    /// Designed for Unity game development with comprehensive configuration and validation support.
    /// </summary>
    public sealed class AlertChannelFactory : IAlertChannelFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly Dictionary<AlertChannelType, Func<ChannelConfig, ILoggingService, UniTask<IAlertChannel>>> _channelCreators;

        /// <summary>
        /// Initializes a new instance of the AlertChannelFactory class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for factory operations</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        public AlertChannelFactory(ILoggingService loggingService = null, ISerializationService serializationService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _channelCreators = InitializeChannelCreators();
        }

        #region IAlertChannelFactory Implementation

        /// <summary>
        /// Creates a new alert channel instance by type.
        /// </summary>
        public async UniTask<IAlertChannel> CreateChannelAsync(AlertChannelType channelType, FixedString64Bytes name, ILoggingService loggingService = null)
        {
            var configuration = ExtendedChannelConfiguration.Default(channelType, name);
            return await CreateAndConfigureChannelAsync(configuration.ToChannelConfig(), loggingService ?? _loggingService);
        }

        /// <summary>
        /// Creates a new alert channel instance by type name.
        /// </summary>
        public async UniTask<IAlertChannel> CreateChannelAsync(string channelTypeName, FixedString64Bytes name, ILoggingService loggingService = null)
        {
            if (!Enum.TryParse<AlertChannelType>(channelTypeName, true, out var channelType))
            {
                throw new ArgumentException($"Unsupported channel type: {channelTypeName}", nameof(channelTypeName));
            }

            return await CreateChannelAsync(channelType, name, loggingService);
        }

        /// <summary>
        /// Creates and configures a new alert channel instance.
        /// </summary>
        public async UniTask<IAlertChannel> CreateAndConfigureChannelAsync(ChannelConfig configuration, ILoggingService loggingService = null, Guid correlationId = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResult = ValidateChannelConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid channel configuration: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}", nameof(configuration));
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Determine channel type
                var channelType = DetermineChannelType(configuration);
                
                // Create the channel
                IAlertChannel channel;
                if (_channelCreators.TryGetValue(channelType, out var creator))
                {
                    channel = await creator(configuration, loggingService ?? _loggingService);
                }
                else
                {
                    throw new NotSupportedException($"Channel type '{channelType}' is not supported");
                }

                // Configure the channel
                var channelConfig = CreateChannelConfig(configuration);
                var initResult = await channel.InitializeAsync(channelConfig, correlationId);
                
                if (!initResult)
                {
                    throw new InvalidOperationException($"Failed to initialize channel '{configuration.Name}'");
                }

                var creationTime = DateTime.UtcNow - startTime;
                LogInfo($"Created and configured channel '{configuration.Name}' of type '{channelType}' in {creationTime.TotalMilliseconds:F2}ms", correlationId);
                
                return channel;
            }
            catch (Exception ex)
            {
                var creationTime = DateTime.UtcNow - startTime;
                LogError($"Failed to create channel '{configuration.Name}': {ex.Message} (took {creationTime.TotalMilliseconds:F2}ms)", correlationId);
                throw;
            }
        }

        /// <summary>
        /// Creates a logging channel with specific configuration.
        /// </summary>
        public async UniTask<IAlertChannel> CreateLoggingChannelAsync(
            FixedString64Bytes name,
            ILoggingService loggingService,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool includeContext = true,
            bool includeStackTrace = false)
        {
            if (loggingService == null)
                throw new ArgumentNullException(nameof(loggingService));

            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.Log,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                TypedSettings = new LogChannelSettings
                {
                    IncludeContext = includeContext,
                    IncludeStackTrace = includeStackTrace,
                    LogPrefix = "[ALERT]"
                }
            };

            return await CreateAndConfigureChannelAsync(configuration, loggingService);
        }

        /// <summary>
        /// Creates a console channel with specific configuration.
        /// </summary>
        public async UniTask<IAlertChannel> CreateConsoleChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool useColors = true,
            bool includeTimestamp = true)
        {
            var configuration = new ChannelConfiguration
            {
                Type = "Console",
                Name = name,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                MaxAlertsPerSecond = 100,
                Settings = new Dictionary<string, object>
                {
                    ["UseColors"] = useColors,
                    ["IncludeTimestamp"] = includeTimestamp,
                    ["ExpandContext"] = false
                }
            };

            return await CreateAndConfigureChannelAsync(configuration);
        }

        /// <summary>
        /// Creates a file channel with specific configuration.
        /// </summary>
        public async UniTask<IAlertChannel> CreateFileChannelAsync(
            FixedString64Bytes name,
            string filePath,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            long maxFileSize = 10485760,
            int maxBackupFiles = 5)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var configuration = new ChannelConfiguration
            {
                Type = "File",
                Name = name,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                MaxAlertsPerSecond = 500,
                Settings = new Dictionary<string, object>
                {
                    ["FilePath"] = filePath,
                    ["MaxFileSize"] = maxFileSize,
                    ["MaxBackupFiles"] = maxBackupFiles,
                    ["AutoFlush"] = true,
                    ["IncludeTimestamp"] = true,
                    ["DateFormat"] = "yyyy-MM-dd HH:mm:ss.fff"
                }
            };

            return await CreateAndConfigureChannelAsync(configuration);
        }

        /// <summary>
        /// Creates an in-memory channel for testing scenarios.
        /// </summary>
        public async UniTask<IAlertChannel> CreateMemoryChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Debug,
            int maxStoredAlerts = 1000)
        {
            var configuration = new ChannelConfiguration
            {
                Type = "Memory",
                Name = name,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                MaxAlertsPerSecond = 1000,
                Settings = new Dictionary<string, object>
                {
                    ["MaxStoredAlerts"] = maxStoredAlerts,
                    ["CircularBuffer"] = true,
                    ["PreserveOrder"] = true
                }
            };

            return await CreateAndConfigureChannelAsync(configuration);
        }

        /// <summary>
        /// Creates multiple channels from a collection of configurations.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateChannelsAsync(
            IEnumerable<ChannelConfiguration> configurations,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var channels = new List<IAlertChannel>();
            var tasks = configurations.Select(config => CreateChannelSafely(config, loggingService ?? _loggingService, correlationId));
            var results = await UniTask.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result.Success)
                {
                    channels.Add(result.Channel);
                }
                else
                {
                    LogError($"Failed to create channel: {result.Error}", correlationId);
                }
            }

            return channels;
        }

        /// <summary>
        /// Creates channels optimized for development environments.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateDevelopmentChannelsAsync(ILoggingService loggingService)
        {
            if (loggingService == null)
                throw new ArgumentNullException(nameof(loggingService));

            var channels = new List<IAlertChannel>();

            // Development logging channel
            var logChannel = await CreateLoggingChannelAsync(
                "DevelopmentLog",
                loggingService,
                AlertSeverity.Debug,
                includeContext: true,
                includeStackTrace: true);
            channels.Add(logChannel);

            // Development console channel
            var consoleChannel = await CreateConsoleChannelAsync(
                "DevelopmentConsole",
                AlertSeverity.Debug,
                useColors: true,
                includeTimestamp: true);
            channels.Add(consoleChannel);

            // Development memory channel for debugging
            var memoryChannel = await CreateMemoryChannelAsync(
                "DevelopmentMemory",
                AlertSeverity.Debug,
                maxStoredAlerts: 2000);
            channels.Add(memoryChannel);

            return channels;
        }

        /// <summary>
        /// Creates channels optimized for production environments.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateProductionChannelsAsync(ILoggingService loggingService, string logFilePath = null)
        {
            if (loggingService == null)
                throw new ArgumentNullException(nameof(loggingService));

            var channels = new List<IAlertChannel>();

            // Production logging channel
            var logChannel = await CreateLoggingChannelAsync(
                "ProductionLog",
                loggingService,
                AlertSeverity.Warning,
                includeContext: false,
                includeStackTrace: false);
            channels.Add(logChannel);

            // Production file channel if path specified
            if (!string.IsNullOrEmpty(logFilePath))
            {
                var fileChannel = await CreateFileChannelAsync(
                    "ProductionFile",
                    logFilePath,
                    AlertSeverity.Error,
                    maxFileSize: 52428800, // 50MB
                    maxBackupFiles: 10);
                channels.Add(fileChannel);
            }

            return channels;
        }

        /// <summary>
        /// Creates channels optimized for testing scenarios.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateTestChannelsAsync()
        {
            var channels = new List<IAlertChannel>();

            // Test memory channel
            var memoryChannel = await CreateMemoryChannelAsync(
                "TestMemory",
                AlertSeverity.Debug,
                maxStoredAlerts: 500);
            channels.Add(memoryChannel);

            // Test null channel for performance testing
            var nullChannel = await CreateChannelAsync(ChannelType.Null, "TestNull");
            channels.Add(nullChannel);

            return channels;
        }

        /// <summary>
        /// Validates a channel configuration before creation.
        /// </summary>
        public ValidationResult ValidateChannelConfiguration(ChannelConfiguration configuration)
        {
            if (configuration == null)
                return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "AlertChannelFactory");

            var errors = new List<ValidationError>();

            // Validate basic properties
            if (string.IsNullOrEmpty(configuration.Type))
                errors.Add(new ValidationError("Channel type cannot be null or empty"));

            if (configuration.Name.IsEmpty)
                errors.Add(new ValidationError("Channel name cannot be empty"));

            if (configuration.MaxAlertsPerSecond <= 0)
                errors.Add(new ValidationError("MaxAlertsPerSecond must be greater than 0"));

            // Validate type-specific settings
            var channelType = DetermineChannelType(configuration);
            switch (channelType)
            {
                case ChannelType.File:
                    if (!configuration.Settings.ContainsKey("FilePath") || 
                        string.IsNullOrEmpty(configuration.Settings["FilePath"]?.ToString()))
                    {
                        errors.Add(new ValidationError("File channel requires FilePath setting"));
                    }
                    break;

                case ChannelType.Memory:
                    if (configuration.Settings.ContainsKey("MaxStoredAlerts"))
                    {
                        if (!int.TryParse(configuration.Settings["MaxStoredAlerts"].ToString(), out var maxStored) || maxStored <= 0)
                            errors.Add(new ValidationError("Memory channel MaxStoredAlerts must be a positive integer"));
                    }
                    break;
            }

            return errors.Any()
                ? ValidationResult.Failure(errors, "AlertChannelFactory")
                : ValidationResult.Success("AlertChannelFactory");
        }

        /// <summary>
        /// Gets the default configuration for a specific channel type.
        /// </summary>
        public ChannelConfiguration GetDefaultConfiguration(ChannelType channelType)
        {
            return ExtendedChannelConfiguration.Default(channelType, $"Default{channelType}");
        }

        /// <summary>
        /// Gets all supported channel types.
        /// </summary>
        public IEnumerable<ChannelType> GetSupportedChannelTypes()
        {
            return _channelCreators.Keys;
        }

        /// <summary>
        /// Checks if a channel type is supported by this factory.
        /// </summary>
        public bool IsChannelTypeSupported(ChannelType channelType)
        {
            return _channelCreators.ContainsKey(channelType);
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public ChannelConfiguration CreateConfigurationFromSettings(ChannelType channelType, FixedString64Bytes name, Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var config = ExtendedChannelConfiguration.Default(channelType, name);

            // Override with provided settings
            if (settings.TryGetValue("IsEnabled", out var enabledValue))
            {
                if (bool.TryParse(enabledValue.ToString(), out var enabled))
                    config.IsEnabled = enabled;
            }

            if (settings.TryGetValue("MinimumSeverity", out var severityValue))
            {
                if (Enum.TryParse<AlertSeverity>(severityValue.ToString(), out var severity))
                    config.MinimumSeverity = severity;
            }

            if (settings.TryGetValue("MaxAlertsPerSecond", out var rateValue))
            {
                if (int.TryParse(rateValue.ToString(), out var rate))
                    config.MaxAlertsPerSecond = rate;
            }

            // Merge channel-specific settings
            foreach (var setting in settings)
            {
                if (!config.Settings.ContainsKey(setting.Key))
                    config.Settings[setting.Key] = setting.Value;
                else
                    config.Settings[setting.Key] = setting.Value; // Override default
            }

            return config;
        }

        #endregion

        #region Private Helper Methods

        private Dictionary<ChannelType, Func<ChannelConfiguration, ILoggingService, UniTask<IAlertChannel>>> InitializeChannelCreators()
        {
            return new Dictionary<ChannelType, Func<ChannelConfiguration, ILoggingService, UniTask<IAlertChannel>>>
            {
                [ChannelType.Logging] = CreateLoggingChannelInternal,
                [ChannelType.Console] = CreateConsoleChannelInternal,
                [ChannelType.File] = CreateFileChannelInternal,
                [ChannelType.Memory] = CreateMemoryChannelInternal,
                [ChannelType.UnityDebug] = CreateUnityDebugChannelInternal,
                [ChannelType.Null] = CreateNullChannelInternal
            };
        }

        private async UniTask<IAlertChannel> CreateLoggingChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            if (loggingService == null)
                throw new ArgumentException("Logging channel requires ILoggingService", nameof(loggingService));

            await UniTask.CompletedTask;
            return new LogAlertChannel(loggingService);
        }

        private async UniTask<IAlertChannel> CreateConsoleChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new ConsoleAlertChannel();
        }

        private async UniTask<IAlertChannel> CreateFileChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            var filePath = config.Settings.TryGetValue("FilePath", out var pathValue) 
                ? pathValue.ToString() 
                : "alerts.log";

            await UniTask.CompletedTask;
            return new FileAlertChannel(filePath);
        }

        private async UniTask<IAlertChannel> CreateMemoryChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            var maxAlerts = config.Settings.TryGetValue("MaxStoredAlerts", out var maxValue)
                && int.TryParse(maxValue.ToString(), out var max)
                ? max
                : 1000;

            await UniTask.CompletedTask;
            return new MemoryAlertChannel(maxAlerts);
        }

        private async UniTask<IAlertChannel> CreateUnityDebugChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new UnityDebugAlertChannel();
        }

        private async UniTask<IAlertChannel> CreateNullChannelInternal(ChannelConfiguration config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new NullAlertChannel();
        }

        private ChannelType DetermineChannelType(ChannelConfiguration configuration)
        {
            if (Enum.TryParse<ChannelType>(configuration.Type, true, out var channelType))
                return channelType;

            // Fallback mapping for string types
            return configuration.Type.ToLowerInvariant() switch
            {
                "log" or "logging" => ChannelType.Logging,
                "console" => ChannelType.Console,
                "file" => ChannelType.File,
                "memory" => ChannelType.Memory,
                "unity" or "unitydebug" => ChannelType.UnityDebug,
                "null" or "discard" => ChannelType.Null,
                _ => throw new ArgumentException($"Unknown channel type: {configuration.Type}")
            };
        }

        private ChannelConfig CreateChannelConfig(ChannelConfiguration configuration)
        {
            return new ChannelConfig
            {
                Name = configuration.Name.ToString(),
                IsEnabled = configuration.IsEnabled,
                MinimumSeverity = configuration.MinimumSeverity,
                Settings = new Dictionary<string, object>(configuration.Settings)
            };
        }

        private async UniTask<ChannelCreationResult> CreateChannelSafely(ChannelConfiguration configuration, ILoggingService loggingService, Guid correlationId)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var channel = await CreateAndConfigureChannelAsync(configuration, loggingService, correlationId);
                var creationTime = DateTime.UtcNow - startTime;
                return ChannelCreationResult.Success(channel, configuration, creationTime);
            }
            catch (Exception ex)
            {
                var creationTime = DateTime.UtcNow - startTime;
                return ChannelCreationResult.Failure(ex.Message, configuration, creationTime);
            }
        }

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertChannelFactory] {message}", correlationId.ToString(), "AlertChannelFactory");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertChannelFactory] {message}", correlationId.ToString(), "AlertChannelFactory");
        }

        #endregion
    }

    #region Additional Channel Implementations

    /// <summary>
    /// File-based alert channel that writes alerts to disk.
    /// </summary>
    internal sealed class FileAlertChannel : BaseAlertChannel
    {
        private readonly string _filePath;

        public FileAlertChannel(string filePath) : base("FileChannel")
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            MinimumSeverity = AlertSeverity.Information;
        }

        protected override async UniTask<bool> SendAlertInternalAsync(Alert alert, Guid correlationId)
        {
            try
            {
                var message = FormatAlert(alert);
                await System.IO.File.AppendAllTextAsync(_filePath, message + Environment.NewLine);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override async UniTask<HealthCheckResult> PerformHealthCheckAsync(Guid correlationId)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(_filePath);
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);

                // Test write access
                await System.IO.File.WriteAllTextAsync(_filePath + ".healthcheck", "test");
                System.IO.File.Delete(_filePath + ".healthcheck");
                
                await UniTask.CompletedTask;
                return HealthCheckResult.Healthy($"File channel healthy: {_filePath}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"File channel unhealthy: {ex.Message}");
            }
        }

        private string FormatAlert(Alert alert)
        {
            return $"[{alert.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{alert.Severity}] [{alert.Source}] {alert.Message}";
        }
    }

    /// <summary>
    /// Memory-based alert channel for testing and debugging.
    /// </summary>
    internal sealed class MemoryAlertChannel : BaseAlertChannel
    {
        private readonly Queue<Alert> _storedAlerts;
        private readonly int _maxStoredAlerts;

        public IReadOnlyCollection<Alert> StoredAlerts => _storedAlerts.ToList();

        public MemoryAlertChannel(int maxStoredAlerts = 1000) : base("MemoryChannel")
        {
            _maxStoredAlerts = maxStoredAlerts;
            _storedAlerts = new Queue<Alert>(maxStoredAlerts);
            MinimumSeverity = AlertSeverity.Debug;
        }

        protected override async UniTask<bool> SendAlertInternalAsync(Alert alert, Guid correlationId)
        {
            lock (_storedAlerts)
            {
                while (_storedAlerts.Count >= _maxStoredAlerts)
                    _storedAlerts.Dequeue();
                
                _storedAlerts.Enqueue(alert);
            }
            
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<HealthCheckResult> PerformHealthCheckAsync(Guid correlationId)
        {
            await UniTask.CompletedTask;
            return HealthCheckResult.Healthy($"Memory channel healthy: {_storedAlerts.Count}/{_maxStoredAlerts} stored");
        }

        public override void ResetStatistics(Guid correlationId = default)
        {
            base.ResetStatistics(correlationId);
            lock (_storedAlerts)
            {
                _storedAlerts.Clear();
            }
        }
    }

    /// <summary>
    /// Unity Debug.Log-based alert channel.
    /// </summary>
    internal sealed class UnityDebugAlertChannel : BaseAlertChannel
    {
        public UnityDebugAlertChannel() : base("UnityDebugChannel")
        {
            MinimumSeverity = AlertSeverity.Information;
        }

        protected override async UniTask<bool> SendAlertInternalAsync(Alert alert, Guid correlationId)
        {
            var message = $"[ALERT] [{alert.Source}] {alert.Message}";
            
            switch (alert.Severity)
            {
                case AlertSeverity.Critical:
                case AlertSeverity.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
                case AlertSeverity.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
            
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<HealthCheckResult> PerformHealthCheckAsync(Guid correlationId)
        {
            await UniTask.CompletedTask;
            return HealthCheckResult.Healthy("Unity Debug channel always healthy");
        }
    }

    /// <summary>
    /// Null object pattern channel that discards all alerts.
    /// </summary>
    internal sealed class NullAlertChannel : BaseAlertChannel
    {
        public NullAlertChannel() : base("NullChannel")
        {
            MinimumSeverity = AlertSeverity.Debug;
        }

        protected override async UniTask<bool> SendAlertInternalAsync(Alert alert, Guid correlationId)
        {
            await UniTask.CompletedTask;
            return true; // Always succeeds by doing nothing
        }

        protected override async UniTask<HealthCheckResult> PerformHealthCheckAsync(Guid correlationId)
        {
            await UniTask.CompletedTask;
            return HealthCheckResult.Healthy("Null channel always healthy");
        }
    }

    #endregion
}