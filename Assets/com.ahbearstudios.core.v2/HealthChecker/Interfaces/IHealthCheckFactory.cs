using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Interfaces;

/// <summary>
/// Factory interface for creating health check system components with proper dependency injection,
/// configuration validation, and enterprise-grade features. Provides centralized creation of
/// health checks, services, registries, and supporting infrastructure while ensuring consistent
/// integration with core systems (logging, messaging, dependency injection).
/// Follows the Builder → Config → Factory → Service architectural pattern.
/// </summary>
public interface IHealthCheckFactory : IDisposable
{
    #region Core Dependencies

    /// <summary>
    /// Gets the dependency provider used for resolving services and dependencies.
    /// </summary>
    IDependencyProvider DependencyProvider { get; }

    /// <summary>
    /// Gets the message bus service used for publishing health check events.
    /// </summary>
    IMessageBusService MessageBusService { get; }

    /// <summary>
    /// Gets the logging service used for factory operation logging.
    /// </summary>
    ILoggingService Logger { get; }

    /// <summary>
    /// Gets whether the factory has been properly initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets statistics about objects created by this factory.
    /// </summary>
    HealthCheckFactoryStatistics Statistics { get; }

    #endregion

    #region Health Check Creation

    /// <summary>
    /// Creates a health check instance of the specified type with dependency injection.
    /// Publishes a HealthCheckCreatedMessage on successful creation or 
    /// HealthCheckCreationFailedMessage on failure through the message bus.
    /// </summary>
    /// <typeparam name="THealthCheck">The type of health check to create.</typeparam>
    /// <param name="config">Optional configuration for the health check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check instance.</returns>
    /// <exception cref="ArgumentException">Thrown when THealthCheck does not implement IHealthCheck.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="DependencyResolutionException">Thrown when required dependencies cannot be resolved.</exception>
    Task<THealthCheck> CreateHealthCheckAsync<THealthCheck>(
        IHealthCheckConfig config = null, 
        CancellationToken cancellationToken = default) 
        where THealthCheck : class, IHealthCheck;

    /// <summary>
    /// Creates a health check instance by type with dependency injection.
    /// Publishes a HealthCheckCreatedMessage on successful creation or 
    /// HealthCheckCreationFailedMessage on failure through the message bus.
    /// </summary>
    /// <param name="healthCheckType">The type of health check to create.</param>
    /// <param name="config">Optional configuration for the health check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when healthCheckType does not implement IHealthCheck.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="DependencyResolutionException">Thrown when required dependencies cannot be resolved.</exception>
    Task<IHealthCheck> CreateHealthCheckAsync(
        Type healthCheckType, 
        IHealthCheckConfig config = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a health check instance using a factory function with dependency injection support.
    /// Publishes a HealthCheckCreatedMessage on successful creation or 
    /// HealthCheckCreationFailedMessage on failure through the message bus.
    /// </summary>
    /// <typeparam name="THealthCheck">The type of health check to create.</typeparam>
    /// <param name="factory">Factory function that creates the health check instance.</param>
    /// <param name="config">Optional configuration for the health check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<THealthCheck> CreateHealthCheckAsync<THealthCheck>(
        Func<IDependencyProvider, THealthCheck> factory,
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default)
        where THealthCheck : class, IHealthCheck;

    /// <summary>
    /// Creates multiple health check instances in parallel with dependency injection.
    /// Publishes HealthCheckCreatedMessage for each successful creation and 
    /// HealthCheckCreationFailedMessage for each failure through the message bus.
    /// </summary>
    /// <param name="healthCheckTypes">The types of health checks to create.</param>
    /// <param name="defaultConfig">Default configuration to apply to all health checks.</param>
    /// <param name="configOverrides">Optional configuration overrides per health check type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckTypes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IReadOnlyList<IHealthCheck>> CreateHealthChecksAsync(
        IEnumerable<Type> healthCheckTypes,
        IHealthCheckConfig defaultConfig = null,
        IReadOnlyDictionary<Type, IHealthCheckConfig> configOverrides = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a health check with automatic discovery and registration.
    /// Publishes a HealthCheckCreatedMessage on successful creation or 
    /// HealthCheckCreationFailedMessage on failure through the message bus.
    /// Additionally publishes HealthCheckRegisteredMessage if auto-registration is enabled.
    /// </summary>
    /// <typeparam name="THealthCheck">The type of health check to create and register.</typeparam>
    /// <param name="config">Optional configuration for the health check.</param>
    /// <param name="autoRegister">Whether to automatically register the health check with the service.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created and optionally registered health check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<HealthCheckCreationResult<THealthCheck>> CreateAndRegisterHealthCheckAsync<THealthCheck>(
        IHealthCheckConfig config = null,
        bool autoRegister = true,
        CancellationToken cancellationToken = default)
        where THealthCheck : class, IHealthCheck;

    #endregion

    #region Service Creation

    /// <summary>
    /// Creates a health check service instance with all required dependencies.
    /// Publishes a HealthCheckServiceCreatedMessage on successful creation or 
    /// HealthCheckServiceCreationFailedMessage on failure through the message bus.
    /// </summary>
    /// <param name="systemConfig">Configuration for the health check system.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check service.</returns>
    /// <exception cref="ArgumentNullException">Thrown when systemConfig is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="DependencyResolutionException">Thrown when required dependencies cannot be resolved.</exception>
    Task<IHealthCheckService> CreateHealthCheckServiceAsync(
        IHealthCheckSystemConfig systemConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a health check registry with the specified configuration.
    /// </summary>
    /// <param name="allocator">The Unity allocator to use for native collections.</param>
    /// <param name="initialCapacity">Initial capacity for the registry.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check registry.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when initialCapacity is negative.</exception>
    Task<IHealthCheckRegistry> CreateHealthCheckRegistryAsync(
        Allocator allocator = Allocator.Persistent,
        int initialCapacity = 64,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a health check reporter with default targets.
    /// </summary>
    /// <param name="includeDefaultTargets">Whether to include default reporting targets.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check reporter.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheckReporter> CreateHealthCheckReporterAsync(
        bool includeDefaultTargets = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a health check scheduler with the specified configuration.
    /// </summary>
    /// <param name="registry">The health check registry to schedule.</param>
    /// <param name="reporter">The reporter to send results to.</param>
    /// <param name="intervalSeconds">The scheduling interval in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created health check scheduler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry or reporter is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when intervalSeconds is negative or zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheckScheduler> CreateHealthCheckSchedulerAsync(
        IHealthCheckRegistry registry,
        IHealthCheckReporter reporter,
        double intervalSeconds = 30.0,
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration Creation

    /// <summary>
    /// Creates a default health check configuration with standard settings.
    /// </summary>
    /// <param name="configId">Optional configuration identifier.</param>
    /// <returns>A default health check configuration instance.</returns>
    IHealthCheckConfig CreateDefaultConfig(string configId = null);

    /// <summary>
    /// Creates a health check configuration builder for fluent configuration.
    /// </summary>
    /// <typeparam name="TConfig">The type of configuration to build.</typeparam>
    /// <typeparam name="TBuilder">The type of builder to create.</typeparam>
    /// <returns>A health check configuration builder instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="DependencyResolutionException">Thrown when the builder type cannot be resolved.</exception>
    TBuilder CreateConfigBuilder<TConfig, TBuilder>()
        where TConfig : class, IHealthCheckConfig
        where TBuilder : class, IHealthCheckConfigBuilder<TConfig, TBuilder>;

    /// <summary>
    /// Creates a health check system configuration with default values.
    /// </summary>
    /// <param name="configId">Optional configuration identifier.</param>
    /// <returns>A health check system configuration instance.</returns>
    IHealthCheckSystemConfig CreateSystemConfig(string configId = null);

    /// <summary>
    /// Creates a health check system configuration builder for fluent configuration.
    /// </summary>
    /// <returns>A health check system configuration builder instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    /// <exception cref="DependencyResolutionException">Thrown when the builder type cannot be resolved.</exception>
    IHealthCheckSystemConfigBuilder CreateSystemConfigBuilder();

    #endregion

    #region Specialized Health Checks

    /// <summary>
    /// Creates a memory health check that monitors system memory usage.
    /// </summary>
    /// <param name="config">Optional configuration for the memory check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created memory health check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheck> CreateMemoryHealthCheckAsync(
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a network connectivity health check.
    /// </summary>
    /// <param name="targetHost">The host to check connectivity to.</param>
    /// <param name="config">Optional configuration for the network check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created network health check.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetHost is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheck> CreateNetworkHealthCheckAsync(
        string targetHost,
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a database health check for the specified connection.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="config">Optional configuration for the database check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created database health check.</returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheck> CreateDatabaseHealthCheckAsync(
        string connectionString,
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a custom health check using a delegate function.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="category">The category of the health check.</param>
    /// <param name="checkFunction">The function that performs the health check.</param>
    /// <param name="config">Optional configuration for the health check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created custom health check.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name, category, or checkFunction is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheck> CreateCustomHealthCheckAsync(
        FixedString64Bytes name,
        FixedString64Bytes category,
        Func<double, HealthCheckResult> checkFunction,
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a composite health check that aggregates multiple child health checks.
    /// </summary>
    /// <param name="name">The name of the composite health check.</param>
    /// <param name="childChecks">The child health checks to aggregate.</param>
    /// <param name="aggregationStrategy">The strategy for aggregating child results.</param>
    /// <param name="config">Optional configuration for the composite check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the created composite health check.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name or childChecks is null.</exception>
    /// <exception cref="ArgumentException">Thrown when childChecks is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IHealthCheck> CreateCompositeHealthCheckAsync(
        FixedString64Bytes name,
        IReadOnlyList<IHealthCheck> childChecks,
        HealthCheckAggregationStrategy aggregationStrategy = HealthCheckAggregationStrategy.WorstCase,
        IHealthCheckConfig config = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Discovery and Registration

    /// <summary>
    /// Discovers health checks in the specified assemblies using reflection.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for health checks.</param>
    /// <param name="filter">Optional filter to apply during discovery.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the discovered health check types.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblies is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IReadOnlyList<Type>> DiscoverHealthChecksAsync(
        IEnumerable<System.Reflection.Assembly> assemblies,
        Func<Type, bool> filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and creates health checks marked with auto-registration attributes.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for auto-registering health checks.</param>
    /// <param name="defaultConfig">Default configuration to apply to discovered health checks.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the discovered and created health checks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblies is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<IReadOnlyList<IHealthCheck>> DiscoverAndCreateHealthChecksAsync(
        IEnumerable<System.Reflection.Assembly> assemblies,
        IHealthCheckConfig defaultConfig = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers health check types for future creation without creating instances.
    /// </summary>
    /// <param name="healthCheckTypes">The health check types to register.</param>
    /// <param name="defaultConfigs">Optional default configurations per type.</param>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckTypes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    void RegisterHealthCheckTypes(
        IEnumerable<Type> healthCheckTypes,
        IReadOnlyDictionary<Type, IHealthCheckConfig> defaultConfigs = null);

    /// <summary>
    /// Gets all registered health check types.
    /// </summary>
    /// <returns>A collection of all registered health check types.</returns>
    IReadOnlyCollection<Type> GetRegisteredHealthCheckTypes();

    /// <summary>
    /// Checks if a health check type is registered.
    /// </summary>
    /// <param name="healthCheckType">The health check type to check.</param>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    bool IsHealthCheckTypeRegistered(Type healthCheckType);

    #endregion

    #region Validation and Diagnostics

    /// <summary>
    /// Validates that all required dependencies are available for health check creation.
    /// </summary>
    /// <param name="healthCheckType">The health check type to validate dependencies for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is not initialized.</exception>
    Task<HealthCheckDependencyValidationResult> ValidateDependenciesAsync(
        Type healthCheckType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the factory's configuration and dependencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the factory validation result.</returns>
    Task<HealthCheckFactoryValidationResult> ValidateFactoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a diagnostic check of the factory's health and configuration.
    /// </summary>
    /// <param name="includeDetailedDiagnostics">Whether to include detailed diagnostic information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the diagnostic result.</returns>
    Task<HealthCheckFactoryDiagnosticResult> PerformDiagnosticAsync(
        bool includeDetailedDiagnostics = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets creation statistics for monitoring factory performance.
    /// </summary>
    /// <returns>Statistics about objects created by this factory.</returns>
    HealthCheckFactoryStatistics GetCreationStatistics();

    /// <summary>
    /// Resets the factory's creation statistics.
    /// </summary>
    void ResetStatistics();

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Initializes the factory with the specified dependencies.
    /// </summary>
    /// <param name="dependencyProvider">The dependency provider for resolving services.</param>
    /// <param name="messageBusService">The message bus service for events.</param>
    /// <param name="logger">The logging service for factory operations.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when initialization is finished.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the factory is already initialized.</exception>
    Task InitializeAsync(
        IDependencyProvider dependencyProvider,
        IMessageBusService messageBusService,
        ILoggingService logger,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached instances and resets the factory state.
    /// Publishes a HealthCheckFactoryClearedMessage through the message bus when complete.
    /// </summary>
    /// <param name="disposeInstances">Whether to dispose created instances that implement IDisposable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when cleanup is finished.</returns>
    Task ClearCacheAsync(bool disposeInstances = true, CancellationToken cancellationToken = default);

    #endregion
}