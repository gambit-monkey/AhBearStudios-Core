using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Pooling;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Concrete implementation of IAlertChannelFactory for creating and configuring alert channel instances.
    /// Supports various channel types including logging, console, file, memory, and Unity-specific channels.
    /// Designed for Unity game development with zero-allocation patterns and pooling support.
    /// Follows strict factory pattern - creates objects but does not manage their lifecycle.
    /// </summary>
    public sealed class AlertChannelFactory : IAlertChannelFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        private readonly Dictionary<AlertChannelType, Func<ChannelConfig, ILoggingService, UniTask<IAlertChannel>>> _channelCreators;
        private readonly ProfilerMarker _createChannelMarker;
        private readonly ProfilerMarker _validateConfigMarker;

        /// <summary>
        /// Initializes a new instance of the AlertChannelFactory class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for factory operations</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        /// <param name="poolingService">Optional pooling service for temporary allocations</param>
        public AlertChannelFactory(ILoggingService loggingService = null, ISerializationService serializationService = null, IPoolingService poolingService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _poolingService = poolingService;
            _channelCreators = InitializeChannelCreators();
            _createChannelMarker = new ProfilerMarker("AlertChannelFactory.CreateChannel");
            _validateConfigMarker = new ProfilerMarker("AlertChannelFactory.ValidateConfig");
        }

        #region IAlertChannelFactory Implementation

        /// <summary>
        /// Creates a new alert channel instance by type with default configuration.
        /// </summary>
        public async UniTask<IAlertChannel> CreateChannelAsync(AlertChannelType channelType, FixedString64Bytes name, ILoggingService loggingService = null)
        {
            var configuration = GetDefaultConfiguration(channelType) with { Name = name };
            return await CreateAndConfigureChannelAsync(configuration, loggingService ?? _loggingService);
        }

        /// <summary>
        /// Creates a new alert channel instance by type name with default configuration.
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
        /// Factory does not track created channels - lifecycle management is caller's responsibility.
        /// </summary>
        public async UniTask<IAlertChannel> CreateAndConfigureChannelAsync(ChannelConfig configuration, ILoggingService loggingService = null, Guid correlationId = default)
        {
            using (_createChannelMarker.Auto())
            {
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                var validationResult = ValidateChannelConfiguration(configuration);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Invalid channel configuration: {string.Join(", ", validationResult.Errors.AsValueEnumerable().Select(e => e.Message))}", nameof(configuration));
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
        /// Uses pooling service for temporary collections and parallel creation for performance.
        /// </summary>
        public async UniTask<IEnumerable<IAlertChannel>> CreateChannelsAsync(
            IEnumerable<ChannelConfig> configurations,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var channels = new List<IAlertChannel>();
            var tasks = configurations.AsValueEnumerable().Select(config => CreateChannelSafely(config, loggingService ?? _loggingService, correlationId)).ToArray();
            var results = await UniTask.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result.IsSuccessful)
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
        /// Validates a channel configuration before creation.
        /// Uses pooling service for temporary collections to avoid allocations.
        /// </summary>
        public ValidationResult ValidateChannelConfiguration(ChannelConfig configuration)
        {
            using (_validateConfigMarker.Auto())
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
                        else
                        {
                            // Validate file path is valid
                            try
                            {
                                var directoryPath = System.IO.Path.GetDirectoryName(fileSettings.FilePath);
                                if (!string.IsNullOrEmpty(directoryPath) && !System.IO.Directory.Exists(directoryPath))
                                {
                                    // Try to create the directory to verify the path is valid
                                    System.IO.Directory.CreateDirectory(directoryPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add(new ValidationError($"Invalid file path: {ex.Message}"));
                            }
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

            return errors.AsValueEnumerable().Any()
                    ? ValidationResult.Failure(errors, "AlertChannelFactory")
                    : ValidationResult.Success("AlertChannelFactory");
            }
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
                AlertChannelType.File => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                    TypedSettings = FileChannelSettings.Default
                },
                AlertChannelType.Memory => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Debug,
                    MaximumSeverity = AlertSeverity.Emergency,
                    TypedSettings = MemoryChannelSettings.Default
                },
                AlertChannelType.Email => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Critical,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Severity}] {Source}: {Message}",
                    EnableBatching = true,
                    BatchSize = 5
                },
                AlertChannelType.UnityNotification => new ChannelConfig
                {
                    Name = $"Default{channelType}",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Warning,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Source}] {Message}"
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

            if (settings.TryGetValue("MaximumSeverity", out var maxSeverityValue))
            {
                if (Enum.TryParse<AlertSeverity>(maxSeverityValue.ToString(), out var maxSeverity))
                    configBuilder = configBuilder with { MaximumSeverity = maxSeverity };
            }

            if (settings.TryGetValue("MessageFormat", out var formatValue))
            {
                configBuilder = configBuilder with { MessageFormat = formatValue.ToString() };
            }

            if (settings.TryGetValue("EnableBatching", out var batchingValue))
            {
                if (bool.TryParse(batchingValue.ToString(), out var batching))
                    configBuilder = configBuilder with { EnableBatching = batching };
            }

            if (settings.TryGetValue("BatchSize", out var batchSizeValue))
            {
                if (int.TryParse(batchSizeValue.ToString(), out var batchSize))
                    configBuilder = configBuilder with { BatchSize = batchSize };
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
                [AlertChannelType.UnityNotification] = CreateUnityNotificationChannelInternal,
                [AlertChannelType.Network] = CreateNetworkChannelInternal,
                [AlertChannelType.Email] = CreateEmailChannelInternal
            };
        }

        private UniTask<IAlertChannel> CreateLoggingChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            if (loggingService == null)
                throw new ArgumentException("Logging channel requires ILoggingService", nameof(loggingService));

            return UniTask.FromResult<IAlertChannel>(new LogAlertChannel(loggingService));
        }

        private UniTask<IAlertChannel> CreateConsoleChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            return UniTask.FromResult<IAlertChannel>(new ConsoleAlertChannel());
        }

        private UniTask<IAlertChannel> CreateFileChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            var filePath = (config.TypedSettings as FileChannelSettings)?.FilePath ?? "alerts.log";

            return UniTask.FromResult<IAlertChannel>(new FileAlertChannel(filePath));
        }

        private UniTask<IAlertChannel> CreateMemoryChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            var maxAlerts = (config.TypedSettings as MemoryChannelSettings)?.MaxStoredAlerts ?? 1000;

            return UniTask.FromResult<IAlertChannel>(new MemoryAlertChannel(maxAlerts));
        }

        private UniTask<IAlertChannel> CreateUnityDebugChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            return UniTask.FromResult<IAlertChannel>(new UnityDebugAlertChannel());
        }

        private UniTask<IAlertChannel> CreateUnityNotificationChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            // Unity Notification channel implementation - for now use NullAlertChannel as placeholder
            // TODO: Implement UnityNotificationAlertChannel when Unity notification system is integrated
            return UniTask.FromResult<IAlertChannel>(new NullAlertChannel());
        }

        private UniTask<IAlertChannel> CreateNetworkChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            // Network channel implementation - for now use NullAlertChannel as placeholder
            // TODO: Implement NetworkAlertChannel for HTTP/webhook delivery
            return UniTask.FromResult<IAlertChannel>(new NullAlertChannel());
        }

        private UniTask<IAlertChannel> CreateEmailChannelInternal(ChannelConfig config, ILoggingService loggingService)
        {
            // Email channel implementation - for now use NullAlertChannel as placeholder
            // TODO: Implement EmailAlertChannel for SMTP delivery
            return UniTask.FromResult<IAlertChannel>(new NullAlertChannel());
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