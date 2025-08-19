using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Services;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages.SystemMessages
{
    /// <summary>
    /// System startup message that provides comprehensive information about system initialization.
    /// Contains startup metrics, configuration details, and system health information.
    /// Follows AhBearStudios Core Development Guidelines with immutable design and performance optimization.
    /// </summary>
    [MessageTypeCode(1001)]
    [MessageCategory("System")]
    [MessageDescription("Indicates that a system or service has completed startup initialization")]
    [MessagePriority(MessagePriority.High)]
    [MessageSerializable(true)]
    public sealed record SystemStartupMessage : BaseMessage
    {
        #region Core Properties

        /// <inheritdoc />
        public override ushort TypeCode => 1001;

        /// <summary>
        /// Gets the name of the system or service that started up.
        /// </summary>
        public FixedString64Bytes SystemName { get; }

        /// <summary>
        /// Gets the version of the system or service.
        /// </summary>
        public FixedString32Bytes Version { get; }

        /// <summary>
        /// Gets the environment where the system is running (Development, Staging, Production).
        /// </summary>
        public FixedString32Bytes Environment { get; }

        /// <summary>
        /// Gets the unique instance identifier for this system instance.
        /// </summary>
        public Guid InstanceId { get; }

        /// <summary>
        /// Gets the process ID of the started system.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the machine name where the system is running.
        /// </summary>
        public FixedString64Bytes MachineName { get; }

        #endregion

        #region Startup Metrics

        /// <summary>
        /// Gets the total startup duration in milliseconds.
        /// </summary>
        public double StartupDurationMs { get; }

        /// <summary>
        /// Gets the timestamp when startup began (UTC ticks).
        /// </summary>
        public long StartupBeganTicks { get; }

        /// <summary>
        /// Gets the timestamp when startup completed (UTC ticks).
        /// </summary>
        public long StartupCompletedTicks { get; }

        /// <summary>
        /// Gets the number of components that were initialized during startup.
        /// </summary>
        public int ComponentsInitialized { get; }

        /// <summary>
        /// Gets the number of dependencies that were resolved during startup.
        /// </summary>
        public int DependenciesResolved { get; }

        /// <summary>
        /// Gets the number of services that were registered during startup.
        /// </summary>
        public int ServicesRegistered { get; }

        /// <summary>
        /// Gets the peak memory usage during startup in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; }

        /// <summary>
        /// Gets the current memory usage after startup in bytes.
        /// </summary>
        public long CurrentMemoryUsageBytes { get; }

        #endregion

        #region System Information

        /// <summary>
        /// Gets the operating system information.
        /// </summary>
        public FixedString128Bytes OperatingSystem { get; }

        /// <summary>
        /// Gets the .NET runtime version.
        /// </summary>
        public FixedString32Bytes RuntimeVersion { get; }

        /// <summary>
        /// Gets the application framework version (Unity, etc.).
        /// </summary>
        public FixedString32Bytes FrameworkVersion { get; }

        /// <summary>
        /// Gets the number of CPU cores available to the system.
        /// </summary>
        public int CpuCores { get; }

        /// <summary>
        /// Gets the total available memory in bytes.
        /// </summary>
        public long TotalMemoryBytes { get; }

        /// <summary>
        /// Gets whether the system is running in debug mode.
        /// </summary>
        public bool IsDebugMode { get; }

        /// <summary>
        /// Gets whether the system supports hot reloading.
        /// </summary>
        public bool SupportsHotReload { get; }

        #endregion

        #region Configuration Information

        /// <summary>
        /// Gets the configuration profiles that are active.
        /// </summary>
        public IReadOnlyList<FixedString32Bytes> ActiveProfiles { get; }

        /// <summary>
        /// Gets the feature flags that are enabled.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> EnabledFeatures { get; }

        /// <summary>
        /// Gets the external services that were connected during startup.
        /// </summary>
        public IReadOnlyList<ServiceConnectionInfo> ConnectedServices { get; }

        /// <summary>
        /// Gets the startup configuration parameters.
        /// </summary>
        public IReadOnlyDictionary<string, object> ConfigurationParameters { get; }

        #endregion

        #region Health Information

        /// <summary>
        /// Gets the overall health status of the system after startup.
        /// </summary>
        public SystemHealthStatus HealthStatus { get; }

        /// <summary>
        /// Gets the health check results performed during startup.
        /// </summary>
        public IReadOnlyList<StartupHealthCheck> HealthCheckResults { get; }

        /// <summary>
        /// Gets any warnings generated during startup.
        /// </summary>
        public IReadOnlyList<StartupWarning> StartupWarnings { get; }

        /// <summary>
        /// Gets any errors that occurred during startup but didn't prevent completion.
        /// </summary>
        public IReadOnlyList<StartupError> StartupErrors { get; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of when startup began.
        /// </summary>
        public DateTime StartupBegan => new DateTime(StartupBeganTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of when startup completed.
        /// </summary>
        public DateTime StartupCompleted => new DateTime(StartupCompletedTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the startup duration as a TimeSpan.
        /// </summary>
        public TimeSpan StartupDuration => TimeSpan.FromMilliseconds(StartupDurationMs);

        /// <summary>
        /// Gets whether the startup was successful (no critical errors).
        /// </summary>
        public bool IsStartupSuccessful => HealthStatus == SystemHealthStatus.Healthy || HealthStatus == SystemHealthStatus.Warning;

        /// <summary>
        /// Gets whether there were any issues during startup.
        /// </summary>
        public bool HasStartupIssues => StartupWarnings.Count > 0 || StartupErrors.Count > 0;

        /// <summary>
        /// Gets the memory utilization percentage during startup.
        /// </summary>
        public double MemoryUtilizationPercentage => TotalMemoryBytes > 0 
            ? (double)CurrentMemoryUsageBytes / TotalMemoryBytes * 100.0 
            : 0.0;

        /// <summary>
        /// Gets whether this is a production environment.
        /// </summary>
        public bool IsProductionEnvironment => Environment.ToString().Equals("Production", StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SystemStartupMessage record.
        /// </summary>
        /// <param name="systemName">The name of the system that started</param>
        /// <param name="version">The system version</param>
        /// <param name="environment">The environment name</param>
        /// <param name="instanceId">The unique instance identifier</param>
        /// <param name="processId">The process ID</param>
        /// <param name="machineName">The machine name</param>
        /// <param name="startupDurationMs">The startup duration in milliseconds</param>
        /// <param name="startupBeganTicks">The startup begin timestamp</param>
        /// <param name="startupCompletedTicks">The startup completion timestamp</param>
        /// <param name="componentsInitialized">Number of components initialized</param>
        /// <param name="dependenciesResolved">Number of dependencies resolved</param>
        /// <param name="servicesRegistered">Number of services registered</param>
        /// <param name="peakMemoryUsageBytes">Peak memory usage during startup</param>
        /// <param name="currentMemoryUsageBytes">Current memory usage</param>
        /// <param name="operatingSystem">Operating system information</param>
        /// <param name="runtimeVersion">.NET runtime version</param>
        /// <param name="frameworkVersion">Framework version</param>
        /// <param name="cpuCores">Number of CPU cores</param>
        /// <param name="totalMemoryBytes">Total available memory</param>
        /// <param name="isDebugMode">Whether running in debug mode</param>
        /// <param name="supportsHotReload">Whether hot reload is supported</param>
        /// <param name="activeProfiles">Active configuration profiles</param>
        /// <param name="enabledFeatures">Enabled feature flags</param>
        /// <param name="connectedServices">Connected external services</param>
        /// <param name="configurationParameters">Configuration parameters</param>
        /// <param name="healthStatus">Overall health status</param>
        /// <param name="healthCheckResults">Health check results</param>
        /// <param name="startupWarnings">Startup warnings</param>
        /// <param name="startupErrors">Startup errors</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        public SystemStartupMessage(
            FixedString64Bytes systemName,
            FixedString32Bytes version,
            FixedString32Bytes environment,
            Guid instanceId = default,
            int processId = 0,
            FixedString64Bytes machineName = default,
            double startupDurationMs = 0,
            long startupBeganTicks = 0,
            long startupCompletedTicks = 0,
            int componentsInitialized = 0,
            int dependenciesResolved = 0,
            int servicesRegistered = 0,
            long peakMemoryUsageBytes = 0,
            long currentMemoryUsageBytes = 0,
            FixedString128Bytes operatingSystem = default,
            FixedString32Bytes runtimeVersion = default,
            FixedString32Bytes frameworkVersion = default,
            int cpuCores = 0,
            long totalMemoryBytes = 0,
            bool isDebugMode = false,
            bool supportsHotReload = false,
            IReadOnlyList<FixedString32Bytes> activeProfiles = null,
            IReadOnlyList<FixedString64Bytes> enabledFeatures = null,
            IReadOnlyList<ServiceConnectionInfo> connectedServices = null,
            IReadOnlyDictionary<string, object> configurationParameters = null,
            SystemHealthStatus healthStatus = SystemHealthStatus.Healthy,
            IReadOnlyList<StartupHealthCheck> healthCheckResults = null,
            IReadOnlyList<StartupWarning> startupWarnings = null,
            IReadOnlyList<StartupError> startupErrors = null)
            : base("System", MessagePriority.High)
        {
            // Validate required parameters
            if (systemName.Length == 0)
                throw new ArgumentException("System name cannot be empty", nameof(systemName));

            if (startupDurationMs < 0)
                throw new ArgumentException("Startup duration cannot be negative", nameof(startupDurationMs));

            if (componentsInitialized < 0)
                throw new ArgumentException("Components initialized cannot be negative", nameof(componentsInitialized));

            if (dependenciesResolved < 0)
                throw new ArgumentException("Dependencies resolved cannot be negative", nameof(dependenciesResolved));

            if (servicesRegistered < 0)
                throw new ArgumentException("Services registered cannot be negative", nameof(servicesRegistered));

            // Set properties
            SystemName = systemName;
            Version = version;
            Environment = environment;
            InstanceId = instanceId == default ? Guid.NewGuid() : instanceId;
            ProcessId = processId == 0 ? System.Diagnostics.Process.GetCurrentProcess().Id : processId;
            MachineName = machineName.Length == 0 ? System.Environment.MachineName : machineName;

            // Set startup metrics
            StartupDurationMs = startupDurationMs;
            StartupBeganTicks = startupBeganTicks == 0 ? DateTime.UtcNow.Ticks - TimeSpan.FromMilliseconds(startupDurationMs).Ticks : startupBeganTicks;
            StartupCompletedTicks = startupCompletedTicks == 0 ? DateTime.UtcNow.Ticks : startupCompletedTicks;
            ComponentsInitialized = componentsInitialized;
            DependenciesResolved = dependenciesResolved;
            ServicesRegistered = servicesRegistered;
            PeakMemoryUsageBytes = peakMemoryUsageBytes == 0 ? GC.GetTotalMemory(false) : peakMemoryUsageBytes;
            CurrentMemoryUsageBytes = currentMemoryUsageBytes == 0 ? GC.GetTotalMemory(false) : currentMemoryUsageBytes;

            // Set system information
            OperatingSystem = operatingSystem.Length == 0 ? System.Environment.OSVersion.ToString() : operatingSystem;
            RuntimeVersion = runtimeVersion.Length == 0 ? System.Environment.Version.ToString() : runtimeVersion;
            FrameworkVersion = frameworkVersion.Length == 0 ? "Unity" : frameworkVersion;
            CpuCores = cpuCores == 0 ? System.Environment.ProcessorCount : cpuCores;
            TotalMemoryBytes = totalMemoryBytes;
            IsDebugMode = isDebugMode;
            SupportsHotReload = supportsHotReload;

            // Set configuration information
            ActiveProfiles = activeProfiles ?? Array.Empty<FixedString32Bytes>();
            EnabledFeatures = enabledFeatures ?? Array.Empty<FixedString64Bytes>();
            ConnectedServices = connectedServices ?? Array.Empty<ServiceConnectionInfo>();
            ConfigurationParameters = configurationParameters ?? new Dictionary<string, object>();

            // Set health information
            HealthStatus = healthStatus;
            HealthCheckResults = healthCheckResults ?? Array.Empty<StartupHealthCheck>();
            StartupWarnings = startupWarnings ?? Array.Empty<StartupWarning>();
            StartupErrors = startupErrors ?? Array.Empty<StartupError>();
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a basic startup message with minimal information.
        /// </summary>
        /// <param name="systemName">The system name</param>
        /// <param name="version">The system version</param>
        /// <param name="environment">The environment</param>
        /// <returns>Basic startup message</returns>
        public static SystemStartupMessage Basic(
            FixedString64Bytes systemName,
            FixedString32Bytes version,
            FixedString32Bytes environment) =>
            new(systemName, version, environment);

        /// <summary>
        /// Creates a comprehensive startup message with full metrics.
        /// </summary>
        /// <param name="systemName">The system name</param>
        /// <param name="version">The system version</param>
        /// <param name="environment">The environment</param>
        /// <param name="startupStopwatch">Stopwatch used to measure startup time</param>
        /// <param name="componentsInitialized">Number of components initialized</param>
        /// <param name="dependenciesResolved">Number of dependencies resolved</param>
        /// <param name="servicesRegistered">Number of services registered</param>
        /// <returns>Comprehensive startup message</returns>
        public static SystemStartupMessage Comprehensive(
            FixedString64Bytes systemName,
            FixedString32Bytes version,
            FixedString32Bytes environment,
            Stopwatch startupStopwatch,
            int componentsInitialized,
            int dependenciesResolved,
            int servicesRegistered)
        {
            var now = DateTime.UtcNow;
            var startupDuration = startupStopwatch?.Elapsed ?? TimeSpan.Zero;
            var startupBegan = now - startupDuration;

            return new SystemStartupMessage(
                systemName,
                version,
                environment,
                startupDurationMs: startupDuration.TotalMilliseconds,
                startupBeganTicks: startupBegan.Ticks,
                startupCompletedTicks: now.Ticks,
                componentsInitialized: componentsInitialized,
                dependenciesResolved: dependenciesResolved,
                servicesRegistered: servicesRegistered);
        }

        /// <summary>
        /// Creates a startup message with health check results.
        /// </summary>
        /// <param name="systemName">The system name</param>
        /// <param name="version">The system version</param>
        /// <param name="environment">The environment</param>
        /// <param name="healthStatus">Overall health status</param>
        /// <param name="healthCheckResults">Health check results</param>
        /// <param name="warnings">Startup warnings</param>
        /// <param name="errors">Startup errors</param>
        /// <returns>Startup message with health information</returns>
        public static SystemStartupMessage WithHealth(
            FixedString64Bytes systemName,
            FixedString32Bytes version,
            FixedString32Bytes environment,
            SystemHealthStatus healthStatus,
            IReadOnlyList<StartupHealthCheck> healthCheckResults = null,
            IReadOnlyList<StartupWarning> warnings = null,
            IReadOnlyList<StartupError> errors = null) =>
            new(systemName, version, environment,
                healthStatus: healthStatus,
                healthCheckResults: healthCheckResults,
                startupWarnings: warnings,
                startupErrors: errors);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets a summary of the startup information for logging.
        /// </summary>
        /// <returns>Formatted startup summary</returns>
        public string GetStartupSummary() =>
            $"System '{SystemName}' v{Version} started successfully in {Environment} environment. " +
            $"Startup took {StartupDurationMs:F2}ms with {ComponentsInitialized} components, " +
            $"{DependenciesResolved} dependencies, and {ServicesRegistered} services. " +
            $"Health: {HealthStatus}, Memory: {CurrentMemoryUsageBytes / 1024 / 1024:F1}MB";

        /// <summary>
        /// Gets detailed startup metrics for monitoring systems.
        /// </summary>
        /// <returns>Dictionary of startup metrics</returns>
        public IReadOnlyDictionary<string, object> GetStartupMetrics() =>
            new Dictionary<string, object>
            {
                ["SystemName"] = SystemName.ToString(),
                ["Version"] = Version.ToString(),
                ["Environment"] = Environment.ToString(),
                ["InstanceId"] = InstanceId.ToString(),
                ["StartupDurationMs"] = StartupDurationMs,
                ["ComponentsInitialized"] = ComponentsInitialized,
                ["DependenciesResolved"] = DependenciesResolved,
                ["ServicesRegistered"] = ServicesRegistered,
                ["PeakMemoryUsageMB"] = PeakMemoryUsageBytes / 1024.0 / 1024.0,
                ["CurrentMemoryUsageMB"] = CurrentMemoryUsageBytes / 1024.0 / 1024.0,
                ["MemoryUtilizationPercent"] = MemoryUtilizationPercentage,
                ["HealthStatus"] = HealthStatus.ToString(),
                ["WarningCount"] = StartupWarnings.Count,
                ["ErrorCount"] = StartupErrors.Count,
                ["IsProductionEnvironment"] = IsProductionEnvironment,
                ["IsDebugMode"] = IsDebugMode
            };

        /// <summary>
        /// Checks if a specific feature is enabled.
        /// </summary>
        /// <param name="featureName">The feature name to check</param>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        public bool IsFeatureEnabled(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
                return false;

            FixedString64Bytes fixedFeatureName = featureName;
            return EnabledFeatures.Contains(fixedFeatureName);
        }

        /// <summary>
        /// Checks if a specific profile is active.
        /// </summary>
        /// <param name="profileName">The profile name to check</param>
        /// <returns>True if the profile is active, false otherwise</returns>
        public bool IsProfileActive(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;

            FixedString32Bytes fixedProfileName = profileName;
            return ActiveProfiles.Contains(fixedProfileName);
        }

        /// <summary>
        /// Gets a configuration parameter value.
        /// </summary>
        /// <typeparam name="T">The expected parameter type</typeparam>
        /// <param name="parameterName">The parameter name</param>
        /// <returns>The parameter value, or default if not found</returns>
        public T GetConfigurationParameter<T>(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName) || !ConfigurationParameters.TryGetValue(parameterName, out var value))
                return default(T);

            try
            {
                return (T)value;
            }
            catch
            {
                return default(T);
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Information about a service connection established during startup.
    /// </summary>
    public sealed record ServiceConnectionInfo
    {
        /// <summary>
        /// Gets the name of the connected service.
        /// </summary>
        public FixedString64Bytes ServiceName { get; }

        /// <summary>
        /// Gets the endpoint or connection string of the service.
        /// </summary>
        public FixedString128Bytes Endpoint { get; }

        /// <summary>
        /// Gets the connection status.
        /// </summary>
        public ServiceConnectionStatus Status { get; }

        /// <summary>
        /// Gets the time taken to establish the connection in milliseconds.
        /// </summary>
        public double ConnectionTimeMs { get; }

        /// <summary>
        /// Gets additional connection metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceConnectionInfo record.
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="endpoint">The service endpoint</param>
        /// <param name="status">The connection status</param>
        /// <param name="connectionTimeMs">The connection time</param>
        /// <param name="metadata">Additional metadata</param>
        public ServiceConnectionInfo(
            FixedString64Bytes serviceName,
            FixedString128Bytes endpoint,
            ServiceConnectionStatus status,
            double connectionTimeMs = 0,
            IReadOnlyDictionary<string, object> metadata = null)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
            Status = status;
            ConnectionTimeMs = connectionTimeMs;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Result of a health check performed during startup.
    /// </summary>
    public sealed record StartupHealthCheck
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes CheckName { get; }

        /// <summary>
        /// Gets the health check status.
        /// </summary>
        public HealthCheckStatus Status { get; }

        /// <summary>
        /// Gets the duration of the health check in milliseconds.
        /// </summary>
        public double DurationMs { get; }

        /// <summary>
        /// Gets the health check message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets additional health check data.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data { get; }

        /// <summary>
        /// Initializes a new instance of the StartupHealthCheck record.
        /// </summary>
        /// <param name="checkName">The check name</param>
        /// <param name="status">The check status</param>
        /// <param name="durationMs">The check duration</param>
        /// <param name="message">The check message</param>
        /// <param name="data">Additional check data</param>
        public StartupHealthCheck(
            FixedString64Bytes checkName,
            HealthCheckStatus status,
            double durationMs,
            string message = null,
            IReadOnlyDictionary<string, object> data = null)
        {
            CheckName = checkName;
            Status = status;
            DurationMs = durationMs;
            Message = message ?? string.Empty;
            Data = data ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Warning generated during system startup.
    /// </summary>
    public sealed record StartupWarning
    {
        /// <summary>
        /// Gets the warning code.
        /// </summary>
        public FixedString32Bytes Code { get; }

        /// <summary>
        /// Gets the warning message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the component that generated the warning.
        /// </summary>
        public FixedString64Bytes Component { get; }

        /// <summary>
        /// Gets when the warning occurred.
        /// </summary>
        public DateTime OccurredAt { get; }

        /// <summary>
        /// Initializes a new instance of the StartupWarning record.
        /// </summary>
        /// <param name="code">The warning code</param>
        /// <param name="message">The warning message</param>
        /// <param name="component">The component that generated the warning</param>
        /// <param name="occurredAt">When the warning occurred</param>
        public StartupWarning(
            FixedString32Bytes code,
            string message,
            FixedString64Bytes component,
            DateTime occurredAt = default)
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Component = component;
            OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt;
        }
    }

    /// <summary>
    /// Error that occurred during system startup but didn't prevent completion.
    /// </summary>
    public sealed record StartupError
    {
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public FixedString32Bytes Code { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the component that generated the error.
        /// </summary>
        public FixedString64Bytes Component { get; }

        /// <summary>
        /// Gets the exception details if available.
        /// </summary>
        public string ExceptionDetails { get; }

        /// <summary>
        /// Gets when the error occurred.
        /// </summary>
        public DateTime OccurredAt { get; }

        /// <summary>
        /// Gets whether the error was recovered from.
        /// </summary>
        public bool WasRecovered { get; }

        /// <summary>
        /// Initializes a new instance of the StartupError record.
        /// </summary>
        /// <param name="code">The error code</param>
        /// <param name="message">The error message</param>
        /// <param name="component">The component that generated the error</param>
        /// <param name="exceptionDetails">Exception details</param>
        /// <param name="occurredAt">When the error occurred</param>
        /// <param name="wasRecovered">Whether the error was recovered from</param>
        public StartupError(
            FixedString32Bytes code,
            string message,
            FixedString64Bytes component,
            string exceptionDetails = null,
            DateTime occurredAt = default,
            bool wasRecovered = false)
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Component = component;
            ExceptionDetails = exceptionDetails ?? string.Empty;
            OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt;
            WasRecovered = wasRecovered;
        }
    }

    #endregion

    #region Enumerations

    /// <summary>
    /// Overall system health status.
    /// </summary>
    public enum SystemHealthStatus : byte
    {
        /// <summary>
        /// System is healthy and operating normally.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// System has warnings but is still operational.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// System has degraded performance but is functional.
        /// </summary>
        Degraded = 2,

        /// <summary>
        /// System is unhealthy and may not function properly.
        /// </summary>
        Unhealthy = 3
    }

    /// <summary>
    /// Status of a service connection.
    /// </summary>
    public enum ServiceConnectionStatus : byte
    {
        /// <summary>
        /// Connection was established successfully.
        /// </summary>
        Connected = 0,

        /// <summary>
        /// Connection failed but system can continue.
        /// </summary>
        Failed = 1,

        /// <summary>
        /// Connection timed out.
        /// </summary>
        Timeout = 2,

        /// <summary>
        /// Connection was skipped or not attempted.
        /// </summary>
        Skipped = 3
    }

    /// <summary>
    /// Status of a health check.
    /// </summary>
    public enum HealthCheckStatus : byte
    {
        /// <summary>
        /// Health check passed.
        /// </summary>
        Passed = 0,

        /// <summary>
        /// Health check failed.
        /// </summary>
        Failed = 1,

        /// <summary>
        /// Health check was skipped.
        /// </summary>
        Skipped = 2,

        /// <summary>
        /// Health check timed out.
        /// </summary>
        Timeout = 3
    }

    #endregion
}