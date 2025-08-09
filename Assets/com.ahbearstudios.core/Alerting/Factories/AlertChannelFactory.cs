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
            var configuration = GetDefaultConfiguration(channelType) with { Name = name };
            return await CreateAndConfigureChannelAsync(configuration, loggingService ?? _loggingService);
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
                throw new ArgumentException($"Invalid channel configuration: {string.Join(", ", validationResult.Errors.ZSelect(e => e.Message))}", nameof(configuration));
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
                    LogCategory = "Alerts"
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
            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.Console,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                TypedSettings = new ConsoleChannelSettings
                {
                    EnableColors = useColors,
                    IncludeTimestamps = includeTimestamp,
                    IncludeSource = true
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

            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.File,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                TypedSettings = new FileChannelSettings
                {
                    FilePath = filePath,
                    MaxFileSize = maxFileSize,
                    MaxBackupFiles = maxBackupFiles,
                    AutoFlush = true,
                    IncludeTimestamp = true,
                    DateFormat = "yyyy-MM-dd HH:mm:ss.fff"
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
            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.Memory,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                TypedSettings = new MemoryChannelSettings
                {
                    MaxStoredAlerts = maxStoredAlerts,
                    CircularBuffer = true,
                    PreserveOrder = true
                }
            };

            return await CreateAndConfigureChannelAsync(configuration);
        }

        /// <summary>
        /// Creates multiple channels from a collection of configurations.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateChannelsAsync(
            IEnumerable<ChannelConfig> configurations,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var channels = new List<IAlertChannel>();
            var tasks = configurations.ZSelect(config => CreateChannelSafely(config, loggingService ?? _loggingService, correlationId));
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
            var nullChannel = await CreateChannelAsync(AlertChannelType.Network, "TestNull");
            channels.Add(nullChannel);

            return channels;
        }

        /// <summary>
        /// Validates a channel configuration before creation.
        /// </summary>
        public ValidationResult ValidateChannelConfiguration(ChannelConfig configuration)
        {
            if (configuration == null)
                return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "AlertChannelFactory");

            var errors = new List<ValidationError>();

            // Validate basic properties
            if (configuration.ChannelType == 0)
                errors.Add(new ValidationError("Channel type must be specified"));

            if (configuration.Name.IsEmpty)
                errors.Add(new ValidationError("Channel name cannot be empty"));

            // Validate type-specific settings
            var channelType = DetermineChannelType(configuration);
            switch (channelType)
            {
                case AlertChannelType.File:
                    if (configuration.TypedSettings is not FileChannelSettings fileSettings || 
                        string.IsNullOrEmpty(fileSettings.FilePath))
                    {
                        errors.Add(new ValidationError("File channel requires FilePath setting"));
                    }
                    break;

                case AlertChannelType.Memory:
                    if (configuration.TypedSettings is MemoryChannelSettings memorySettings &&
                        memorySettings.MaxStoredAlerts <= 0)
                    {
                        errors.Add(new ValidationError("Memory channel MaxStoredAlerts must be a positive integer"));
                    }
                    break;
            }

        return errors.ZAny()
                ? ValidationResult.Failure(errors, "AlertChannelFactory")
                : ValidationResult.Success("AlertChannelFactory");
        }

        /// <summary>
        /// Gets the default configuration for a specific channel type.
        /// </summary>
        public ChannelConfig GetDefaultConfiguration(AlertChannelType channelType)
        {
            return channelType switch
            {
                AlertChannelType.Log => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                    TypedSettings = LogChannelSettings.Default
                },
                AlertChannelType.Console => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                    TypedSettings = ConsoleChannelSettings.Default
                },
                AlertChannelType.Network => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Critical,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "{\"timestamp\":\"{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\",\"severity\":\"{Severity}\",\"source\":\"{Source}\",\"message\":\"{Message}\",\"tag\":\"{Tag}\",\"correlationId\":\"{CorrelationId}\"}",
                    EnableBatching = true,
                    BatchSize = 10
                },
                AlertChannelType.UnityConsole => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Warning,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Source}] {Message}",
                    TypedSettings = UnityChannelSettings.Default
                },
                _ => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency
                }
            };
        }

        /// <summary>
        /// Gets all supported channel types.
        /// </summary>
        public IEnumerable<AlertChannelType> GetSupportedChannelTypes()
        {
            return _channelCreators.Keys;
        }

        /// <summary>
        /// Checks if a channel type is supported by this factory.
        /// </summary>
        public bool IsChannelTypeSupported(AlertChannelType channelType)
        {
            return _channelCreators.ContainsKey(channelType);
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public ChannelConfig CreateConfigurationFromSettings(AlertChannelType channelType, FixedString64Bytes name, Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var config = GetDefaultConfiguration(channelType) with { Name = name };
            var configBuilder = config with { };

            // Override with provided settings
            if (settings.TryGetValue("IsEnabled", out var enabledValue))
            {
                if (bool.TryParse(enabledValue.ToString(), out var enabled))
                    configBuilder = configBuilder with { IsEnabled = enabled };
            }

            if (settings.TryGetValue("MinimumSeverity", out var severityValue))
            {
                if (Enum.TryParse<AlertSeverity>(severityValue.ToString(), out var severity))
                    configBuilder = configBuilder with { MinimumSeverity = severity };
            }

            return configBuilder;
        }


        #endregion

        #region Private Helper Methods

        private Dictionary<AlertChannelType, Func<ChannelConfig, ILoggingService, UniTask<IAlertChannel>>> InitializeChannelCreators()
        {
            return new Dictionary<AlertChannelType, Func<ChannelConfig, ILoggingService, UniTask<IAlertChannel>>>
            {
                [AlertChannelType.Log] = CreateLoggingChannelInternal,
                [AlertChannelType.Console] = CreateConsoleChannelInternal,
                [AlertChannelType.File] = CreateFileChannelInternal,
                [AlertChannelType.Memory] = CreateMemoryChannelInternal,
                [AlertChannelType.UnityConsole] = CreateUnityDebugChannelInternal,
                [AlertChannelType.Network] = CreateNullChannelInternal
            };
        }

        private async UniTask<IAlertChannel> CreateLoggingChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            if (loggingService == null)
                throw new ArgumentException("Logging channel requires ILoggingService", nameof(loggingService));

            await UniTask.CompletedTask;
            return new LogAlertChannel(loggingService);
        }

        private async UniTask<IAlertChannel> CreateConsoleChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new ConsoleAlertChannel();
        }

        private async UniTask<IAlertChannel> CreateFileChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            var filePath = (config.TypedSettings as FileChannelSettings)?.FilePath ?? "alerts.log";

            await UniTask.CompletedTask;
            return new FileAlertChannel(filePath);
        }

        private async UniTask<IAlertChannel> CreateMemoryChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            var maxAlerts = (config.TypedSettings as MemoryChannelSettings)?.MaxStoredAlerts ?? 1000;

            await UniTask.CompletedTask;
            return new MemoryAlertChannel(maxAlerts);
        }

        private async UniTask<IAlertChannel> CreateUnityDebugChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new UnityDebugAlertChannel();
        }

        private async UniTask<IAlertChannel> CreateNullChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            await UniTask.CompletedTask;
            return new NullAlertChannel();
        }

        private AlertChannelType DetermineChannelType(ChannelConfig configuration)
        {
            return configuration.ChannelType;
        }

        private ChannelConfig CreateChannelConfig(ChannelConfig configuration)
        {
            return configuration; // Already the correct type
        }

        private async UniTask<ChannelCreationResult> CreateChannelSafely(ChannelConfig configuration, ILoggingService loggingService, Guid correlationId)
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

}