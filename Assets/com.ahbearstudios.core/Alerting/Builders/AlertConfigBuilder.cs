using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Complete implementation of IAlertConfigBuilder that provides a fluent interface for building alert configurations.
    /// Supports all available alert channels, suppression rules, and provides comprehensive scenario-specific presets.
    /// Follows the Builder pattern as specified in the AhBearStudios Core Architecture.
    /// Integrates with health checking, logging, and performance monitoring systems for production reliability.
    /// </summary>
    public sealed class AlertConfigBuilder : IAlertConfigBuilder
    {
        #region Private Fields

        private AlertSeverity _minimumSeverity = AlertSeverity.Warning;
        private bool _enableSuppression = true;
        private TimeSpan _suppressionWindow = TimeSpan.FromMinutes(5);
        private bool _enableAsyncProcessing = true;
        private int _maxConcurrentAlerts = 100;
        private TimeSpan _processingTimeout = TimeSpan.FromSeconds(30);
        private bool _enableHistory = true;
        private TimeSpan _historyRetention = TimeSpan.FromHours(24);
        private int _maxHistoryEntries = 10000;
        private bool _enableAggregation = true;
        private TimeSpan _aggregationWindow = TimeSpan.FromMinutes(2);
        private int _maxAggregationSize = 50;
        private bool _enableCorrelationTracking = true;
        private int _alertBufferSize = 1000;
        private bool _enableUnityIntegration = true;
        private bool _enableMetrics = true;
        private bool _enableCircuitBreakerIntegration = true;

        private readonly List<ChannelConfig> _channels = new();
        private readonly List<SuppressionConfig> _suppressionRules = new();
        private readonly Dictionary<FixedString64Bytes, AlertSeverity> _sourceSeverityOverrides = new();
        private EmergencyEscalationConfig _emergencyEscalation = EmergencyEscalationConfig.Default;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AlertConfigBuilder with sensible defaults.
        /// Sets up default suppression rules to prevent common alerting issues.
        /// </summary>
        public AlertConfigBuilder()
        {
            // Initialize with basic default channels
            _channels.Add(ChannelConfig.CreateLogChannel());
            _channels.Add(ChannelConfig.CreateConsoleChannel());

            // Initialize with basic suppression rules
            _suppressionRules.Add(SuppressionConfig.CreateDefaultDuplicateFilter());
            _suppressionRules.Add(SuppressionConfig.CreateDefaultRateLimit());
        }

        #endregion

        #region Core Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithMinimumSeverity(AlertSeverity severity)
        {
            _minimumSeverity = severity;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppression(bool enabled)
        {
            _enableSuppression = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppressionWindow(TimeSpan windowSize)
        {
            if (windowSize <= TimeSpan.Zero)
                throw new ArgumentException("Suppression window must be greater than zero.", nameof(windowSize));

            _suppressionWindow = windowSize;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppression(bool enabled, TimeSpan windowSize)
        {
            return WithSuppression(enabled).WithSuppressionWindow(windowSize);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithAsyncProcessing(bool enabled)
        {
            _enableAsyncProcessing = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithMaxConcurrentAlerts(int maxConcurrentAlerts)
        {
            if (maxConcurrentAlerts <= 0)
                throw new ArgumentException("Max concurrent alerts must be greater than zero.", nameof(maxConcurrentAlerts));

            _maxConcurrentAlerts = maxConcurrentAlerts;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithProcessingTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Processing timeout must be greater than zero.", nameof(timeout));

            _processingTimeout = timeout;
            return this;
        }

        #endregion

        #region History Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithHistory(bool enabled)
        {
            _enableHistory = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithHistory(bool enabled, TimeSpan retention)
        {
            if (retention <= TimeSpan.Zero)
                throw new ArgumentException("History retention must be greater than zero.", nameof(retention));

            _enableHistory = enabled;
            _historyRetention = retention;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithHistoryLimit(int maxEntries)
        {
            if (maxEntries <= 0)
                throw new ArgumentException("Max history entries must be greater than zero.", nameof(maxEntries));

            _maxHistoryEntries = maxEntries;
            return this;
        }

        #endregion

        #region Aggregation Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithAggregation(bool enabled)
        {
            _enableAggregation = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithAggregation(bool enabled, TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
                throw new ArgumentException("Aggregation window must be greater than zero.", nameof(window));

            _enableAggregation = enabled;
            _aggregationWindow = window;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithAggregationLimit(int maxSize)
        {
            if (maxSize <= 1)
                throw new ArgumentException("Max aggregation size must be greater than one.", nameof(maxSize));

            _maxAggregationSize = maxSize;
            return this;
        }

        #endregion

        #region System Integration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithCorrelationTracking(bool enabled)
        {
            _enableCorrelationTracking = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithBufferSize(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException("Buffer size must be greater than zero.", nameof(bufferSize));

            _alertBufferSize = bufferSize;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithUnityIntegration(bool enabled)
        {
            _enableUnityIntegration = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithMetrics(bool enabled)
        {
            _enableMetrics = enabled;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithCircuitBreakerIntegration(bool enabled)
        {
            _enableCircuitBreakerIntegration = enabled;
            return this;
        }

        #endregion

        #region Channel Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithChannel(ChannelConfig channelConfig)
        {
            if (channelConfig == null)
                throw new ArgumentNullException(nameof(channelConfig));

            channelConfig.Validate();
            
            // Remove existing channel with the same name
            _channels.RemoveAll(c => c.Name.Equals(channelConfig.Name));
            _channels.Add(channelConfig);
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithChannels(params ChannelConfig[] channelConfigs)
        {
            if (channelConfigs == null)
                throw new ArgumentNullException(nameof(channelConfigs));

            foreach (var config in channelConfigs)
            {
                WithChannel(config);
            }
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithChannels(IEnumerable<ChannelConfig> channelConfigs)
        {
            if (channelConfigs == null)
                throw new ArgumentNullException(nameof(channelConfigs));

            foreach (var config in channelConfigs)
            {
                WithChannel(config);
            }
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithChannels(Action<IChannelConfigBuilder> channelBuilder)
        {
            if (channelBuilder == null)
                throw new ArgumentNullException(nameof(channelBuilder));

            var builder = new ChannelConfigBuilder(_channels);
            channelBuilder(builder);
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithLogChannel(string name = "Log", AlertSeverity minimumSeverity = AlertSeverity.Info, bool enabled = true)
        {
            var config = ChannelConfig.CreateLogChannel(name) with
            {
                MinimumSeverity = minimumSeverity,
                IsEnabled = enabled
            };
            return WithChannel(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithConsoleChannel(string name = "Console", AlertSeverity minimumSeverity = AlertSeverity.Warning, bool enabled = true)
        {
            var config = ChannelConfig.CreateConsoleChannel(name) with
            {
                MinimumSeverity = minimumSeverity,
                IsEnabled = enabled
            };
            return WithChannel(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithNetworkChannel(string name, string endpoint, AlertSeverity minimumSeverity = AlertSeverity.Critical, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be null or whitespace.", nameof(endpoint));

            var config = ChannelConfig.CreateNetworkChannel(name, endpoint, minimumSeverity) with
            {
                IsEnabled = enabled
            };
            return WithChannel(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithEmailChannel(string name, string smtpServer, string fromEmail, string[] toEmails, AlertSeverity minimumSeverity = AlertSeverity.Critical, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(smtpServer))
                throw new ArgumentException("SMTP server cannot be null or whitespace.", nameof(smtpServer));
            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new ArgumentException("From email cannot be null or whitespace.", nameof(fromEmail));
            if (toEmails == null || toEmails.Length == 0)
                throw new ArgumentException("At least one recipient email must be provided.", nameof(toEmails));

            var config = ChannelConfig.CreateEmailChannel(name, smtpServer, fromEmail, toEmails) with
            {
                MinimumSeverity = minimumSeverity,
                IsEnabled = enabled
            };
            return WithChannel(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithUnityConsoleChannel(string name = "UnityConsole", AlertSeverity minimumSeverity = AlertSeverity.Warning, bool enabled = true)
        {
            var config = ChannelConfig.CreateUnityConsoleChannel(name) with
            {
                MinimumSeverity = minimumSeverity,
                IsEnabled = enabled
            };
            return WithChannel(config);
        }

        #endregion

        #region Suppression Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppressionRule(SuppressionConfig suppressionConfig)
        {
            if (suppressionConfig == null)
                throw new ArgumentNullException(nameof(suppressionConfig));

            suppressionConfig.Validate();
            
            // Remove existing rule with the same name
            _suppressionRules.RemoveAll(r => r.RuleName.Equals(suppressionConfig.RuleName));
            _suppressionRules.Add(suppressionConfig);
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppressionRules(params SuppressionConfig[] suppressionConfigs)
        {
            if (suppressionConfigs == null)
                throw new ArgumentNullException(nameof(suppressionConfigs));

            foreach (var config in suppressionConfigs)
            {
                WithSuppressionRule(config);
            }
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppressionRules(IEnumerable<SuppressionConfig> suppressionConfigs)
        {
            if (suppressionConfigs == null)
                throw new ArgumentNullException(nameof(suppressionConfigs));

            foreach (var config in suppressionConfigs)
            {
                WithSuppressionRule(config);
            }
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSuppressionRules(Action<ISuppressionConfigBuilder> suppressionBuilder)
        {
            if (suppressionBuilder == null)
                throw new ArgumentNullException(nameof(suppressionBuilder));

            var builder = new SuppressionConfigBuilder(_suppressionRules);
            suppressionBuilder(builder);
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithDuplicateFilter(string name = "DuplicateFilter", TimeSpan? window = null, bool enabled = true)
        {
            var config = SuppressionConfig.CreateDefaultDuplicateFilter(name) with
            {
                IsEnabled = enabled,
                SuppressionWindow = window ?? TimeSpan.FromMinutes(5)
            };
            return WithSuppressionRule(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithRateLimit(string name = "RateLimit", int maxAlerts = 10, TimeSpan? window = null, bool enabled = true)
        {
            if (maxAlerts <= 0)
                throw new ArgumentException("Max alerts must be greater than zero.", nameof(maxAlerts));

            var config = SuppressionConfig.CreateDefaultRateLimit(name) with
            {
                IsEnabled = enabled,
                MaxAlertsInWindow = maxAlerts,
                SuppressionWindow = window ?? TimeSpan.FromMinutes(1)
            };
            return WithSuppressionRule(config);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithBusinessHoursFilter(string name = "BusinessHours", TimeZoneInfo timeZone = null, bool enabled = true)
        {
            var config = SuppressionConfig.CreateBusinessHoursFilter(name, timeZone) with
            {
                IsEnabled = enabled
            };
            return WithSuppressionRule(config);
        }

        #endregion

        #region Source Override Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithSourceSeverityOverride(FixedString64Bytes source, AlertSeverity severity)
        {
            if (source.IsEmpty)
                throw new ArgumentException("Source cannot be empty.", nameof(source));

            _sourceSeverityOverrides[source] = severity;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithSourceSeverityOverrides(IDictionary<FixedString64Bytes, AlertSeverity> overrides)
        {
            if (overrides == null)
                throw new ArgumentNullException(nameof(overrides));

            foreach (var kvp in overrides)
            {
                WithSourceSeverityOverride(kvp.Key, kvp.Value);
            }
            return this;
        }

        #endregion

        #region Emergency Escalation Methods

        /// <inheritdoc />
        public IAlertConfigBuilder WithEmergencyEscalation(EmergencyEscalationConfig escalationConfig)
        {
            if (escalationConfig == null)
                throw new ArgumentNullException(nameof(escalationConfig));

            escalationConfig.Validate();
            _emergencyEscalation = escalationConfig;
            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder WithEmergencyEscalation(bool enabled, double failureThreshold = 0.8, string fallbackChannel = "Console")
        {
            if (failureThreshold <= 0.0 || failureThreshold > 1.0)
                throw new ArgumentException("Failure threshold must be between 0.0 and 1.0.", nameof(failureThreshold));

            var config = new EmergencyEscalationConfig
            {
                IsEnabled = enabled,
                FailureThreshold = failureThreshold,
                FallbackChannel = fallbackChannel,
                EvaluationWindow = TimeSpan.FromMinutes(5),
                EscalationCooldown = TimeSpan.FromMinutes(1)
            };
            return WithEmergencyEscalation(config);
        }

        #endregion

        #region Scenario Configuration Methods

        /// <inheritdoc />
        public IAlertConfigBuilder ForProduction()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Warning)
                .WithSuppression(true, TimeSpan.FromMinutes(5))
                .WithAsyncProcessing(true)
                .WithMaxConcurrentAlerts(200)
                .WithProcessingTimeout(TimeSpan.FromSeconds(30))
                .WithHistory(true, TimeSpan.FromHours(48))
                .WithHistoryLimit(20000)
                .WithAggregation(true, TimeSpan.FromMinutes(2))
                .WithAggregationLimit(100)
                .WithCorrelationTracking(true)
                .WithBufferSize(2000)
                .WithUnityIntegration(false)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(true)
                .WithLogChannel("ProductionLog", AlertSeverity.Warning)
                .WithConsoleChannel("ProductionConsole", AlertSeverity.Critical)
                .WithDuplicateFilter("ProductionDuplicateFilter", TimeSpan.FromMinutes(10))
                .WithRateLimit("ProductionRateLimit", 20, TimeSpan.FromMinutes(1))
                .WithBusinessHoursFilter("ProductionBusinessHours")
                .WithEmergencyEscalation(true, 0.8, "ProductionConsole");
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForDevelopment()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Debug)
                .WithSuppression(true, TimeSpan.FromMinutes(2))
                .WithAsyncProcessing(false)
                .WithMaxConcurrentAlerts(50)
                .WithProcessingTimeout(TimeSpan.FromSeconds(10))
                .WithHistory(true, TimeSpan.FromHours(8))
                .WithHistoryLimit(5000)
                .WithAggregation(false)
                .WithCorrelationTracking(true)
                .WithBufferSize(500)
                .WithUnityIntegration(true)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(false)
                .WithLogChannel("DevelopmentLog", AlertSeverity.Debug)
                .WithConsoleChannel("DevelopmentConsole", AlertSeverity.Info)
                .WithUnityConsoleChannel("UnityDevelopmentConsole", AlertSeverity.Warning)
                .WithDuplicateFilter("DevelopmentDuplicateFilter", TimeSpan.FromMinutes(1))
                .WithRateLimit("DevelopmentRateLimit", 50, TimeSpan.FromMinutes(1))
                .WithEmergencyEscalation(false);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForTesting()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Debug)
                .WithSuppression(false)
                .WithAsyncProcessing(false)
                .WithMaxConcurrentAlerts(1000)
                .WithProcessingTimeout(TimeSpan.FromSeconds(5))
                .WithHistory(true, TimeSpan.FromHours(1))
                .WithHistoryLimit(10000)
                .WithAggregation(false)
                .WithCorrelationTracking(true)
                .WithBufferSize(5000)
                .WithUnityIntegration(false)
                .WithMetrics(false)
                .WithCircuitBreakerIntegration(false)
                .WithLogChannel("TestingLog", AlertSeverity.Debug)
                .WithEmergencyEscalation(false);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForStaging()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Info)
                .WithSuppression(true, TimeSpan.FromMinutes(3))
                .WithAsyncProcessing(true)
                .WithMaxConcurrentAlerts(100)
                .WithProcessingTimeout(TimeSpan.FromSeconds(20))
                .WithHistory(true, TimeSpan.FromHours(24))
                .WithHistoryLimit(10000)
                .WithAggregation(true, TimeSpan.FromMinutes(2))
                .WithAggregationLimit(50)
                .WithCorrelationTracking(true)
                .WithBufferSize(1000)
                .WithUnityIntegration(true)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(true)
                .WithLogChannel("StagingLog", AlertSeverity.Info)
                .WithConsoleChannel("StagingConsole", AlertSeverity.Warning)
                .WithUnityConsoleChannel("UnityStagingConsole", AlertSeverity.Critical)
                .WithDuplicateFilter("StagingDuplicateFilter", TimeSpan.FromMinutes(5))
                .WithRateLimit("StagingRateLimit", 15, TimeSpan.FromMinutes(1))
                .WithBusinessHoursFilter("StagingBusinessHours")
                .WithEmergencyEscalation(true, 0.7, "StagingConsole");
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForPerformanceTesting()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Critical)
                .WithSuppression(true, TimeSpan.FromSeconds(30))
                .WithAsyncProcessing(true)
                .WithMaxConcurrentAlerts(500)
                .WithProcessingTimeout(TimeSpan.FromSeconds(5))
                .WithHistory(false)
                .WithAggregation(false)
                .WithCorrelationTracking(false)
                .WithBufferSize(100)
                .WithUnityIntegration(false)
                .WithMetrics(false)
                .WithCircuitBreakerIntegration(false)
                .WithLogChannel("PerformanceTestingLog", AlertSeverity.Critical)
                .WithRateLimit("PerformanceRateLimit", 5, TimeSpan.FromMinutes(1))
                .WithEmergencyEscalation(false);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForHighAvailability()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Info)
                .WithSuppression(true, TimeSpan.FromMinutes(10))
                .WithAsyncProcessing(true)
                .WithMaxConcurrentAlerts(500)
                .WithProcessingTimeout(TimeSpan.FromMinutes(1))
                .WithHistory(true, TimeSpan.FromDays(7))
                .WithHistoryLimit(50000)
                .WithAggregation(true, TimeSpan.FromMinutes(5))
                .WithAggregationLimit(200)
                .WithCorrelationTracking(true)
                .WithBufferSize(5000)
                .WithUnityIntegration(false)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(true)
                .WithLogChannel("HALog", AlertSeverity.Info)
                .WithConsoleChannel("HAConsole", AlertSeverity.Critical)
                .WithDuplicateFilter("HADuplicateFilter", TimeSpan.FromMinutes(15))
                .WithRateLimit("HARateLimit", 50, TimeSpan.FromMinutes(1))
                .WithBusinessHoursFilter("HABusinessHours")
                .WithEmergencyEscalation(true, 0.9, "HAConsole");
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForCloudDeployment()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Warning)
                .WithSuppression(true, TimeSpan.FromMinutes(5))
                .WithAsyncProcessing(true)
                .WithMaxConcurrentAlerts(300)
                .WithProcessingTimeout(TimeSpan.FromSeconds(45))
                .WithHistory(true, TimeSpan.FromHours(72))
                .WithHistoryLimit(25000)
                .WithAggregation(true, TimeSpan.FromMinutes(3))
                .WithAggregationLimit(75)
                .WithCorrelationTracking(true)
                .WithBufferSize(2000)
                .WithUnityIntegration(false)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(true)
                .WithLogChannel("CloudLog", AlertSeverity.Warning)
                .WithConsoleChannel("CloudConsole", AlertSeverity.Critical)
                .WithDuplicateFilter("CloudDuplicateFilter", TimeSpan.FromMinutes(8))
                .WithRateLimit("CloudRateLimit", 30, TimeSpan.FromMinutes(1))
                .WithBusinessHoursFilter("CloudBusinessHours")
                .WithEmergencyEscalation(true, 0.8, "CloudConsole");
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForMobile()
        {
            Reset();
            
            return WithMinimumSeverity(AlertSeverity.Critical)
                .WithSuppression(true, TimeSpan.FromMinutes(2))
                .WithAsyncProcessing(false)
                .WithMaxConcurrentAlerts(20)
                .WithProcessingTimeout(TimeSpan.FromSeconds(5))
                .WithHistory(true, TimeSpan.FromHours(2))
                .WithHistoryLimit(500)
                .WithAggregation(true, TimeSpan.FromMinutes(1))
                .WithAggregationLimit(10)
                .WithCorrelationTracking(false)
                .WithBufferSize(100)
                .WithUnityIntegration(false)
                .WithMetrics(false)
                .WithCircuitBreakerIntegration(false)
                .WithLogChannel("MobileLog", AlertSeverity.Critical)
                .WithRateLimit("MobileRateLimit", 3, TimeSpan.FromMinutes(1))
                .WithEmergencyEscalation(false);
        }

        /// <inheritdoc />
        public IAlertConfigBuilder ForDebugging(string debugSource = null)
        {
            Reset();
            
            var builder = WithMinimumSeverity(AlertSeverity.Debug)
                .WithSuppression(false)
                .WithAsyncProcessing(false)
                .WithMaxConcurrentAlerts(1000)
                .WithProcessingTimeout(TimeSpan.FromSeconds(30))
                .WithHistory(true, TimeSpan.FromHours(4))
                .WithHistoryLimit(20000)
                .WithAggregation(false)
                .WithCorrelationTracking(true)
                .WithBufferSize(5000)
                .WithUnityIntegration(true)
                .WithMetrics(true)
                .WithCircuitBreakerIntegration(false)
                .WithLogChannel("DebugLog", AlertSeverity.Debug)
                .WithConsoleChannel("DebugConsole", AlertSeverity.Debug)
                .WithUnityConsoleChannel("UnityDebugConsole", AlertSeverity.Debug)
                .WithEmergencyEscalation(false);

            if (!string.IsNullOrWhiteSpace(debugSource))
            {
                builder.WithSourceSeverityOverride(debugSource, AlertSeverity.Debug);
            }

            return builder;
        }

        #endregion

        #region Validation and Build Methods

        /// <inheritdoc />
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            try
            {
                // Validate channels
                if (_channels.Count == 0)
                {
                    errors.Add("At least one alert channel must be configured.");
                }

                foreach (var channel in _channels)
                {
                    try
                    {
                        channel.Validate();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Channel '{channel.Name}' validation failed: {ex.Message}");
                    }
                }

                // Validate suppression rules
                foreach (var rule in _suppressionRules)
                {
                    try
                    {
                        rule.Validate();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Suppression rule '{rule.RuleName}' validation failed: {ex.Message}");
                    }
                }

                // Validate emergency escalation
                try
                {
                    _emergencyEscalation.Validate();
                }
                catch (Exception ex)
                {
                    errors.Add($"Emergency escalation validation failed: {ex.Message}");
                }

                // Validate timespan values
                if (_suppressionWindow <= TimeSpan.Zero)
                    errors.Add("Suppression window must be greater than zero.");

                if (_processingTimeout <= TimeSpan.Zero)
                    errors.Add("Processing timeout must be greater than zero.");

                if (_enableHistory && _historyRetention <= TimeSpan.Zero)
                    errors.Add("History retention must be greater than zero when history is enabled.");

                if (_enableAggregation && _aggregationWindow <= TimeSpan.Zero)
                    errors.Add("Aggregation window must be greater than zero when aggregation is enabled.");

                // Validate numeric values
                if (_maxConcurrentAlerts <= 0)
                    errors.Add("Max concurrent alerts must be greater than zero.");

                if (_maxHistoryEntries <= 0)
                    errors.Add("Max history entries must be greater than zero.");

                if (_maxAggregationSize <= 1)
                    errors.Add("Max aggregation size must be greater than one.");

                if (_alertBufferSize <= 0)
                    errors.Add("Alert buffer size must be greater than zero.");
            }
            catch (Exception ex)
            {
                errors.Add($"Validation exception: {ex.Message}");
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public AlertConfig Build()
        {
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            var config = new AlertConfig
            {
                MinimumSeverity = _minimumSeverity,
                EnableSuppression = _enableSuppression,
                SuppressionWindow = _suppressionWindow,
                EnableAsyncProcessing = _enableAsyncProcessing,
                MaxConcurrentAlerts = _maxConcurrentAlerts,
                ProcessingTimeout = _processingTimeout,
                EnableHistory = _enableHistory,
                HistoryRetention = _historyRetention,
                MaxHistoryEntries = _maxHistoryEntries,
                EnableAggregation = _enableAggregation,
                AggregationWindow = _aggregationWindow,
                MaxAggregationSize = _maxAggregationSize,
                EnableCorrelationTracking = _enableCorrelationTracking,
                AlertBufferSize = _alertBufferSize,
                EnableUnityIntegration = _enableUnityIntegration,
                EnableMetrics = _enableMetrics,
                EnableCircuitBreakerIntegration = _enableCircuitBreakerIntegration,
                Channels = _channels.ToList().AsReadOnly(),
                SuppressionRules = _suppressionRules.ToList().AsReadOnly(),
                SourceSeverityOverrides = _sourceSeverityOverrides.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                EmergencyEscalation = _emergencyEscalation
            };

            // Final validation of the built configSo
            config.Validate();
            
            return config;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder Reset()
        {
            _minimumSeverity = AlertSeverity.Warning;
            _enableSuppression = true;
            _suppressionWindow = TimeSpan.FromMinutes(5);
            _enableAsyncProcessing = true;
            _maxConcurrentAlerts = 100;
            _processingTimeout = TimeSpan.FromSeconds(30);
            _enableHistory = true;
            _historyRetention = TimeSpan.FromHours(24);
            _maxHistoryEntries = 10000;
            _enableAggregation = true;
            _aggregationWindow = TimeSpan.FromMinutes(2);
            _maxAggregationSize = 50;
            _enableCorrelationTracking = true;
            _alertBufferSize = 1000;
            _enableUnityIntegration = true;
            _enableMetrics = true;
            _enableCircuitBreakerIntegration = true;
            _emergencyEscalation = EmergencyEscalationConfig.Default;

            _channels.Clear();
            _suppressionRules.Clear();
            _sourceSeverityOverrides.Clear();

            return this;
        }

        /// <inheritdoc />
        public IAlertConfigBuilder Clone()
        {
            var clone = new AlertConfigBuilder
            {
                _minimumSeverity = _minimumSeverity,
                _enableSuppression = _enableSuppression,
                _suppressionWindow = _suppressionWindow,
                _enableAsyncProcessing = _enableAsyncProcessing,
                _maxConcurrentAlerts = _maxConcurrentAlerts,
                _processingTimeout = _processingTimeout,
                _enableHistory = _enableHistory,
                _historyRetention = _historyRetention,
                _maxHistoryEntries = _maxHistoryEntries,
                _enableAggregation = _enableAggregation,
                _aggregationWindow = _aggregationWindow,
                _maxAggregationSize = _maxAggregationSize,
                _enableCorrelationTracking = _enableCorrelationTracking,
                _alertBufferSize = _alertBufferSize,
                _enableUnityIntegration = _enableUnityIntegration,
                _enableMetrics = _enableMetrics,
                _enableCircuitBreakerIntegration = _enableCircuitBreakerIntegration,
                _emergencyEscalation = _emergencyEscalation
            };

            clone._channels.AddRange(_channels);
            clone._suppressionRules.AddRange(_suppressionRules);
            foreach (var kvp in _sourceSeverityOverrides)
            {
                clone._sourceSeverityOverrides[kvp.Key] = kvp.Value;
            }

            return clone;
        }

        #endregion
    }

    /// <summary>
    /// Specialized builder implementation for configuring alert channels with fluent syntax.
    /// Provides advanced channel configuration capabilities and validation.
    /// </summary>
    internal sealed class ChannelConfigBuilder : IChannelConfigBuilder
    {
        private readonly List<ChannelConfig> _channels;

        /// <summary>
        /// Initializes a new instance of the ChannelConfigBuilder.
        /// </summary>
        /// <param name="channels">The channel list to modify</param>
        public ChannelConfigBuilder(List<ChannelConfig> channels)
        {
            _channels = channels ?? throw new ArgumentNullException(nameof(channels));
        }

        /// <inheritdoc />
        public IChannelConfigBuilder AddChannel<TChannel>(Action<TChannel> configAction) where TChannel : ChannelConfig, new()
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var channel = new TChannel();
            configAction(channel);
            channel.Validate();

            // Remove existing channel with the same name
            _channels.RemoveAll(c => c.Name.Equals(channel.Name));
            _channels.Add(channel);
            return this;
        }

        /// <inheritdoc />
        public IChannelConfigBuilder AddChannel(ChannelConfig channelConfig)
        {
            if (channelConfig == null)
                throw new ArgumentNullException(nameof(channelConfig));

            channelConfig.Validate();

            // Remove existing channel with the same name
            _channels.RemoveAll(c => c.Name.Equals(channelConfig.Name));
            _channels.Add(channelConfig);
            return this;
        }

        /// <inheritdoc />
        public IChannelConfigBuilder RemoveChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name cannot be null or whitespace.", nameof(channelName));

            _channels.RemoveAll(c => c.Name.ToString().Equals(channelName, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        /// <inheritdoc />
        public IChannelConfigBuilder ClearChannels()
        {
            _channels.Clear();
            return this;
        }
    }

    /// <summary>
    /// Specialized builder implementation for configuring suppression rules with fluent syntax.
    /// Provides advanced suppression rule configuration capabilities and validation.
    /// </summary>
    internal sealed class SuppressionConfigBuilder : ISuppressionConfigBuilder
    {
        private readonly List<SuppressionConfig> _suppressionRules;

        /// <summary>
        /// Initializes a new instance of the SuppressionConfigBuilder.
        /// </summary>
        /// <param name="suppressionRules">The suppression rules list to modify</param>
        public SuppressionConfigBuilder(List<SuppressionConfig> suppressionRules)
        {
            _suppressionRules = suppressionRules ?? throw new ArgumentNullException(nameof(suppressionRules));
        }

        /// <inheritdoc />
        public ISuppressionConfigBuilder AddRule<TRule>(Action<TRule> configAction) where TRule : SuppressionConfig, new()
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var rule = new TRule();
            configAction(rule);
            rule.Validate();

            // Remove existing rule with the same name
            _suppressionRules.RemoveAll(r => r.RuleName.Equals(rule.RuleName));
            _suppressionRules.Add(rule);
            return this;
        }

        /// <inheritdoc />
        public ISuppressionConfigBuilder AddRule(SuppressionConfig suppressionConfig)
        {
            if (suppressionConfig == null)
                throw new ArgumentNullException(nameof(suppressionConfig));

            suppressionConfig.Validate();

            // Remove existing rule with the same name
            _suppressionRules.RemoveAll(r => r.RuleName.Equals(suppressionConfig.RuleName));
            _suppressionRules.Add(suppressionConfig);
            return this;
        }

        /// <inheritdoc />
        public ISuppressionConfigBuilder RemoveRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
                throw new ArgumentException("Rule name cannot be null or whitespace.", nameof(ruleName));

            _suppressionRules.RemoveAll(r => r.RuleName.ToString().Equals(ruleName, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        /// <inheritdoc />
        public ISuppressionConfigBuilder ClearRules()
        {
            _suppressionRules.Clear();
            return this;
        }
    }
}