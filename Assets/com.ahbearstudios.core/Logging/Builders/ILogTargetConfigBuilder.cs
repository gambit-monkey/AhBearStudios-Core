using System.Collections.Generic;
using System.IO;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder interface for creating robust, production-ready log target configurations.
    /// Supports validation, Unity-specific features, and core system integration.
    /// </summary>
    public interface ILogTargetConfigBuilder
    {
        #region Core Configuration Methods

        /// <summary>
        /// Sets the unique name of the log target.
        /// </summary>
        /// <param name="name">The target name</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithName(string name);

        /// <summary>
        /// Sets the type of the log target.
        /// </summary>
        /// <param name="targetType">The target type (e.g., "Console", "File", "Serilog")</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithTargetType(string targetType);

        /// <summary>
        /// Sets the minimum log level for this target.
        /// </summary>
        /// <param name="level">The minimum log level</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithMinimumLevel(LogLevel level);

        /// <summary>
        /// Sets whether this target is enabled.
        /// </summary>
        /// <param name="isEnabled">True to enable the target</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithEnabled(bool isEnabled);

        /// <summary>
        /// Sets the buffer size for this target.
        /// </summary>
        /// <param name="bufferSize">The buffer size in messages</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithBufferSize(int bufferSize);

        /// <summary>
        /// Sets the flush interval for this target.
        /// </summary>
        /// <param name="flushInterval">The flush interval</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithFlushInterval(TimeSpan flushInterval);

        /// <summary>
        /// Sets whether this target should use asynchronous writing.
        /// </summary>
        /// <param name="useAsync">True to use async writing</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithAsyncWrite(bool useAsync);

        /// <summary>
        /// Sets the message format template for this target.
        /// </summary>
        /// <param name="messageFormat">The message format template</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithMessageFormat(string messageFormat);

        /// <summary>
        /// Adds a channel for this target to listen to.
        /// </summary>
        /// <param name="channel">The channel name</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithChannel(string channel);

        /// <summary>
        /// Adds multiple channels for this target to listen to.
        /// </summary>
        /// <param name="channels">The channel names</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithChannels(params string[] channels);

        /// <summary>
        /// Clears all channels for this target.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder ClearChannels();

        /// <summary>
        /// Sets whether to include stack traces in error messages.
        /// </summary>
        /// <param name="includeStackTrace">True to include stack traces</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithStackTrace(bool includeStackTrace);

        /// <summary>
        /// Sets whether to include correlation IDs in messages.
        /// </summary>
        /// <param name="includeCorrelationId">True to include correlation IDs</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithCorrelationId(bool includeCorrelationId);

        #endregion

        #region Unity-Specific Game Development Properties

        /// <summary>
        /// Sets the error rate threshold (0.0 to 1.0) that triggers alerts.
        /// </summary>
        /// <param name="threshold">Error rate threshold (default: 0.1 = 10%)</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithErrorRateThreshold(double threshold);

        /// <summary>
        /// Sets the frame budget threshold in milliseconds per write operation.
        /// </summary>
        /// <param name="thresholdMs">Frame budget threshold in milliseconds (default: 0.5ms)</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithFrameBudgetThreshold(double thresholdMs);

        /// <summary>
        /// Sets the alert suppression interval in minutes.
        /// </summary>
        /// <param name="intervalMinutes">Alert suppression interval (default: 5 minutes)</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithAlertSuppressionInterval(int intervalMinutes);

        /// <summary>
        /// Sets the maximum concurrent async operations for this target.
        /// </summary>
        /// <param name="maxOperations">Maximum concurrent operations (default: 10)</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithMaxConcurrentOperations(int maxOperations);

        /// <summary>
        /// Sets whether Unity Profiler integration is enabled.
        /// </summary>
        /// <param name="enabled">True to enable Unity Profiler integration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithUnityProfilerIntegration(bool enabled);

        /// <summary>
        /// Sets whether performance metrics should be tracked.
        /// </summary>
        /// <param name="enabled">True to enable performance metrics</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithPerformanceMetrics(bool enabled);

        /// <summary>
        /// Sets the health check interval in seconds.
        /// </summary>
        /// <param name="intervalSeconds">Health check interval (default: 30 seconds)</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithHealthCheckInterval(int intervalSeconds);

        /// <summary>
        /// Sets frame rate impact limit in milliseconds.
        /// </summary>
        /// <param name="maxFrameImpactMs">Maximum frame impact in milliseconds</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithFrameRateImpactLimit(double maxFrameImpactMs);

        /// <summary>
        /// Sets memory budget for this target.
        /// </summary>
        /// <param name="maxMemoryBytes">Maximum memory usage in bytes</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithMemoryBudget(long maxMemoryBytes);

        /// <summary>
        /// Sets GC pressure limit for allocations per second.
        /// </summary>
        /// <param name="maxAllocationsPerSecond">Maximum allocations per second</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithGCPressureLimit(int maxAllocationsPerSecond);

        /// <summary>
        /// Configures platform-specific settings.
        /// </summary>
        /// <param name="platform">Target platform</param>
        /// <param name="platformConfig">Platform-specific configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithPlatformSpecificSettings(RuntimePlatform platform, Action<ILogTargetConfigBuilder> platformConfig);

        /// <summary>
        /// Sets development mode specific settings.
        /// </summary>
        /// <param name="isDevelopment">True if in development mode</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithDevelopmentMode(bool isDevelopment);

        /// <summary>
        /// Sets build type specific settings.
        /// </summary>
        /// <param name="isDebugBuild">True if debug build</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithBuildType(bool isDebugBuild);

        #endregion

        #region Custom Property Management

        /// <summary>
        /// Adds a custom property with type safety and optional validation.
        /// </summary>
        /// <typeparam name="T">Property value type</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <param name="validator">Optional validator function</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithProperty<T>(string key, T value, Func<T, bool> validator = null);

        /// <summary>
        /// Adds a property from an environment variable.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="environmentVariable">Environment variable name</param>
        /// <param name="defaultValue">Default value if environment variable not found</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithPropertyFromEnvironment(string key, string environmentVariable, object defaultValue);

        /// <summary>
        /// Adds a secure property (for API keys, connection strings, etc.).
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="encryptedValue">Encrypted value</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithSecureProperty(string key, string encryptedValue);

        /// <summary>
        /// Adds multiple properties from a dictionary.
        /// </summary>
        /// <param name="properties">Properties to add</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithProperties(IDictionary<string, object> properties);

        /// <summary>
        /// Copies properties from another configuration.
        /// </summary>
        /// <param name="sourceConfig">Source configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithPropertiesFromConfig(ILogTargetConfig sourceConfig);

        /// <summary>
        /// Removes a custom property.
        /// </summary>
        /// <param name="key">Property key to remove</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder RemoveProperty(string key);

        /// <summary>
        /// Clears all custom properties.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder ClearProperties();

        #endregion

        #region Core Systems Integration

        /// <summary>
        /// Configures profiler service integration.
        /// </summary>
        /// <param name="profiler">Profiler service instance</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithProfilerIntegration(IProfilerService profiler);

        /// <summary>
        /// Configures alerting service integration.
        /// </summary>
        /// <param name="alertService">Alert service instance</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithAlertingIntegration(IAlertService alertService);

        /// <summary>
        /// Configures health check service integration.
        /// </summary>
        /// <param name="healthService">Health check service instance</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithHealthCheckIntegration(IHealthCheckService healthService);

        /// <summary>
        /// Adds a custom metric provider.
        /// </summary>
        /// <param name="metricName">Metric name</param>
        /// <param name="metricProvider">Metric provider function</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithCustomMetric(string metricName, Func<object> metricProvider);

        #endregion

        #region Conditional Configuration

        /// <summary>
        /// Applies configuration conditionally.
        /// </summary>
        /// <param name="condition">Condition to evaluate</param>
        /// <param name="configuration">Configuration to apply if condition is true</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder When(Func<bool> condition, Action<ILogTargetConfigBuilder> configuration);

        /// <summary>
        /// Applies configuration for specific platform.
        /// </summary>
        /// <param name="platform">Target platform</param>
        /// <param name="configuration">Platform-specific configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder ForPlatform(RuntimePlatform platform, Action<ILogTargetConfigBuilder> configuration);

        /// <summary>
        /// Applies configuration for specific build type.
        /// </summary>
        /// <param name="isDebugBuild">True if debug build</param>
        /// <param name="configuration">Build-specific configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder ForBuildType(bool isDebugBuild, Action<ILogTargetConfigBuilder> configuration);

        #endregion

        #region Configuration Inheritance & Composition

        /// <summary>
        /// Bases configuration on another configuration.
        /// </summary>
        /// <param name="baseConfig">Base configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder BasedOn(ILogTargetConfig baseConfig);

        /// <summary>
        /// Applies default configuration settings.
        /// </summary>
        /// <param name="defaults">Default settings</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithDefaults(LogTargetDefaults defaults);

        /// <summary>
        /// Overrides settings with another configuration.
        /// </summary>
        /// <param name="overrideConfig">Override configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder OverrideWith(ILogTargetConfig overrideConfig);

        #endregion

        #region Serialization (AhBearStudios.Core.Serialization Integration)

        /// <summary>
        /// Loads configuration from serialized byte array using AhBearStudios.Core.Serialization.
        /// </summary>
        /// <param name="serializedData">Serialized configuration data</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder FromSerialized(byte[] serializedData);

        /// <summary>
        /// Loads configuration from serialized stream using AhBearStudios.Core.Serialization.
        /// </summary>
        /// <param name="stream">Stream containing serialized configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder FromStream(Stream stream);

        /// <summary>
        /// Loads configuration from Unity ScriptableObject.
        /// </summary>
        /// <param name="configAsset">ScriptableObject containing configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder FromScriptableObject(ScriptableObject configAsset);

        /// <summary>
        /// Loads configuration from environment variables with specified prefix.
        /// </summary>
        /// <param name="prefix">Environment variable prefix</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder FromEnvironmentVariables(string prefix);

        #endregion

        #region Validation & Error Handling

        /// <summary>
        /// Adds a custom validator for the entire configuration.
        /// </summary>
        /// <param name="validator">Validator function</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithValidation(Func<ILogTargetConfig, IReadOnlyList<string>> validator);

        /// <summary>
        /// Adds validation for a specific property.
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="validator">Property validator</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>Builder instance for method chaining</returns>
        ILogTargetConfigBuilder WithPropertyValidation<T>(string key, Func<T, bool> validator, string errorMessage);

        /// <summary>
        /// Builds the configuration and throws exception if validation fails.
        /// </summary>
        /// <returns>Built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
        ILogTargetConfig Build();

        /// <summary>
        /// Attempts to build the configuration safely.
        /// </summary>
        /// <param name="config">Built configuration if successful</param>
        /// <param name="errors">Validation errors if build fails</param>
        /// <returns>True if build succeeded, false otherwise</returns>
        bool TryBuild(out ILogTargetConfig config, out IReadOnlyList<string> errors);

        #endregion
    }
}