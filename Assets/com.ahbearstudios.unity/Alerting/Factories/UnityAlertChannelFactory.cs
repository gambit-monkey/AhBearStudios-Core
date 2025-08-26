using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Factories;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Unity.Alerting.Channels;

namespace AhBearStudios.Unity.Alerting.Factories
{
    /// <summary>
    /// Unity-specific alert channel factory that extends the core factory with Unity channel types.
    /// Creates Unity-specific channel implementations like UnityDebugAlertChannel and UnityNotificationAlertChannel.
    /// Follows the AhBearStudios architecture pattern of separating Unity-specific code from core POCO systems.
    /// </summary>
    public sealed class UnityAlertChannelFactory : IAlertChannelFactory
    {
        private readonly IAlertChannelFactory _coreFactory;
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Initializes a new instance of the UnityAlertChannelFactory class.
        /// </summary>
        /// <param name="coreFactory">Core alert channel factory for non-Unity channels</param>
        /// <param name="messageBusService">Message bus service for channel event publishing</param>
        /// <param name="loggingService">Logging service for factory operations</param>
        public UnityAlertChannelFactory(
            IAlertChannelFactory coreFactory,
            IMessageBusService messageBusService,
            ILoggingService loggingService = null)
        {
            _coreFactory = coreFactory ?? throw new ArgumentNullException(nameof(coreFactory));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _loggingService = loggingService;
        }

        /// <summary>
        /// Creates a new alert channel instance by type with default configuration.
        /// Handles Unity-specific channels and delegates to core factory for others.
        /// </summary>
        public async UniTask<IAlertChannel> CreateChannelAsync(
            AlertChannelType channelType,
            FixedString64Bytes name,
            ILoggingService loggingService = null)
        {
            // Handle Unity-specific channels
            if (IsUnityChannel(channelType))
            {
                var config = GetDefaultConfiguration(channelType) with { Name = name };
                return await CreateAndConfigureChannelAsync(config, loggingService ?? _loggingService);
            }

            // Delegate to core factory for non-Unity channels
            return await _coreFactory.CreateChannelAsync(channelType, name, loggingService);
        }

        /// <summary>
        /// Creates a new alert channel instance by type name with default configuration.
        /// </summary>
        public async UniTask<IAlertChannel> CreateChannelAsync(
            string channelTypeName,
            FixedString64Bytes name,
            ILoggingService loggingService = null)
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
        public async UniTask<IAlertChannel> CreateAndConfigureChannelAsync(
            ChannelConfig configuration,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Handle Unity-specific channels
            if (IsUnityChannel(configuration.ChannelType))
            {
                var channel = await CreateUnityChannelAsync(configuration, loggingService ?? _loggingService);
                
                // Initialize the channel
                var initResult = await channel.InitializeAsync(configuration, correlationId);
                if (!initResult)
                {
                    throw new InvalidOperationException($"Failed to initialize Unity channel '{configuration.Name}'");
                }

                LogInfo($"Created Unity channel '{configuration.Name}' of type '{configuration.ChannelType}'", correlationId);
                return channel;
            }

            // Delegate to core factory for non-Unity channels
            return await _coreFactory.CreateAndConfigureChannelAsync(configuration, loggingService, correlationId);
        }

        /// <summary>
        /// Creates a logging channel with specific configuration.
        /// </summary>
        public UniTask<IAlertChannel> CreateLoggingChannelAsync(
            FixedString64Bytes name,
            ILoggingService loggingService,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool includeContext = true,
            bool includeStackTrace = false)
        {
            return _coreFactory.CreateLoggingChannelAsync(name, loggingService, minimumSeverity, includeContext, includeStackTrace);
        }

        /// <summary>
        /// Creates a console channel with specific configuration.
        /// </summary>
        public UniTask<IAlertChannel> CreateConsoleChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool useColors = true,
            bool includeTimestamp = true)
        {
            return _coreFactory.CreateConsoleChannelAsync(name, minimumSeverity, useColors, includeTimestamp);
        }

        /// <summary>
        /// Creates a file channel with specific configuration.
        /// </summary>
        public UniTask<IAlertChannel> CreateFileChannelAsync(
            FixedString64Bytes name,
            string filePath,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            long maxFileSize = 10485760,
            int maxBackupFiles = 5)
        {
            return _coreFactory.CreateFileChannelAsync(name, filePath, minimumSeverity, maxFileSize, maxBackupFiles);
        }

        /// <summary>
        /// Creates an in-memory channel for testing scenarios.
        /// </summary>
        public UniTask<IAlertChannel> CreateMemoryChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Debug,
            int maxStoredAlerts = 1000)
        {
            return _coreFactory.CreateMemoryChannelAsync(name, minimumSeverity, maxStoredAlerts);
        }

        /// <summary>
        /// Creates a Unity Debug console channel.
        /// </summary>
        public async UniTask<IAlertChannel> CreateUnityDebugChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool useColors = true)
        {
            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.UnityConsole,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                TypedSettings = new UnityChannelSettings
                {
                    EnableColors = useColors,
                    ShowStackTrace = minimumSeverity <= AlertSeverity.Warning
                }
            };

            return await CreateAndConfigureChannelAsync(configuration);
        }

        /// <summary>
        /// Creates a Unity notification channel.
        /// </summary>
        public async UniTask<IAlertChannel> CreateUnityNotificationChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Warning,
            bool showEditorDialogs = true,
            bool useRuntimeNotifications = true)
        {
            var configuration = new ChannelConfig
            {
                Name = name,
                ChannelType = AlertChannelType.UnityNotification,
                IsEnabled = true,
                MinimumSeverity = minimumSeverity,
                Settings = new Dictionary<string, string>
                {
                    ["ShowEditorDialogs"] = showEditorDialogs.ToString(),
                    ["UseRuntimeNotifications"] = useRuntimeNotifications.ToString(),
                    ["DialogSeverityThreshold"] = AlertSeverity.Critical.ToString()
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
            var channels = new List<IAlertChannel>();

            foreach (var config in configurations)
            {
                try
                {
                    var channel = await CreateAndConfigureChannelAsync(config, loggingService, correlationId);
                    channels.Add(channel);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to create channel '{config.Name}': {ex.Message}", correlationId);
                }
            }

            return channels;
        }

        /// <summary>
        /// Validates a channel configuration before creation.
        /// </summary>
        public Core.Common.Models.ValidationResult ValidateChannelConfiguration(ChannelConfig configuration)
        {
            // Unity channels have their own validation logic
            if (configuration != null && IsUnityChannel(configuration.ChannelType))
            {
                var errors = new List<Core.Common.Models.ValidationError>();

                if (configuration.Name.IsEmpty)
                    errors.Add(new Core.Common.Models.ValidationError("Channel name cannot be empty"));

                if (configuration.ChannelType == AlertChannelType.UnityConsole || 
                    configuration.ChannelType == AlertChannelType.UnityNotification)
                {
                    // Unity channels are always valid if name is provided
                    return errors.Count > 0
                        ? Core.Common.Models.ValidationResult.Failure(errors, "UnityAlertChannelFactory")
                        : Core.Common.Models.ValidationResult.Success("UnityAlertChannelFactory");
                }
            }

            // Delegate to core factory for validation
            return _coreFactory.ValidateChannelConfiguration(configuration);
        }

        /// <summary>
        /// Gets the default configuration for a specific channel type.
        /// </summary>
        public ChannelConfig GetDefaultConfiguration(AlertChannelType channelType)
        {
            // Provide Unity-specific defaults
            if (channelType == AlertChannelType.UnityConsole)
            {
                return new ChannelConfig
                {
                    Name = "UnityDebugChannel",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Source}] {Message}",
                    TypedSettings = UnityChannelSettings.Default
                };
            }

            if (channelType == AlertChannelType.UnityNotification)
            {
                return new ChannelConfig
                {
                    Name = "UnityNotificationChannel",
                    ChannelType = channelType,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Warning,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Source}] {Message}"
                };
            }

            // Delegate to core factory for other types
            return _coreFactory.GetDefaultConfiguration(channelType);
        }

        /// <summary>
        /// Gets all supported channel types.
        /// </summary>
        public IEnumerable<AlertChannelType> GetSupportedChannelTypes()
        {
            // Combine core and Unity channel types
            var coreTypes = _coreFactory.GetSupportedChannelTypes();
            var unityTypes = new[] { AlertChannelType.UnityConsole, AlertChannelType.UnityNotification };
            
            var allTypes = new HashSet<AlertChannelType>(coreTypes);
            foreach (var type in unityTypes)
            {
                allTypes.Add(type);
            }

            return allTypes;
        }

        /// <summary>
        /// Checks if a channel type is supported by this factory.
        /// </summary>
        public bool IsChannelTypeSupported(AlertChannelType channelType)
        {
            return IsUnityChannel(channelType) || _coreFactory.IsChannelTypeSupported(channelType);
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public ChannelConfig CreateConfigurationFromSettings(
            AlertChannelType channelType,
            FixedString64Bytes name,
            Dictionary<string, object> settings)
        {
            // Use core factory's implementation
            return _coreFactory.CreateConfigurationFromSettings(channelType, name, settings);
        }

        #region Private Methods

        private bool IsUnityChannel(AlertChannelType channelType)
        {
            return channelType == AlertChannelType.UnityConsole || 
                   channelType == AlertChannelType.UnityNotification;
        }

        private async UniTask<IAlertChannel> CreateUnityChannelAsync(ChannelConfig config, ILoggingService loggingService)
        {
            await UniTask.Yield();

            return config.ChannelType switch
            {
                AlertChannelType.UnityConsole => new UnityDebugAlertChannel(_messageBusService),
                AlertChannelType.UnityNotification => new UnityNotificationAlertChannel(_messageBusService),
                _ => throw new NotSupportedException($"Unity channel type '{config.ChannelType}' is not supported")
            };
        }

        private void LogInfo(string message, Guid correlationId)
        {
            _loggingService?.LogInfo($"[UnityAlertChannelFactory] {message}", correlationId.ToString(), "UnityAlertChannelFactory");
        }

        private void LogError(string message, Guid correlationId)
        {
            _loggingService?.LogError($"[UnityAlertChannelFactory] {message}", correlationId.ToString(), "UnityAlertChannelFactory");
        }

        #endregion
    }

    /// <summary>
    /// Unity-specific channel settings.
    /// </summary>
    public sealed class UnityChannelSettings
    {
        public bool EnableColors { get; set; } = true;
        public bool ShowStackTrace { get; set; } = false;

        public static UnityChannelSettings Default => new UnityChannelSettings();
    }
}