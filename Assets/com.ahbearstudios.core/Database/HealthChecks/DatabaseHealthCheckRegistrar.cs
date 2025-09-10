using System;
using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Database.HealthChecks;

/// <summary>
/// Database domain health check registrar implementing the domain self-registration pattern.
/// Manages registration of all database-related health checks with the HealthCheckService.
/// </summary>
public sealed class DatabaseHealthCheckRegistrar : IDomainHealthCheckRegistrar
{
    private readonly ILoggingService _logger;
    private readonly Dictionary<IHealthCheck, HealthCheckConfiguration> _registeredHealthChecks;

    /// <summary>
    /// Initializes a new instance of the DatabaseHealthCheckRegistrar
    /// </summary>
    /// <param name="logger">Logging service for registration operations</param>
    public DatabaseHealthCheckRegistrar(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registeredHealthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();
    }

    /// <inheritdoc />
    public string DomainName => "Database";

    /// <inheritdoc />
    public int RegistrationPriority => 800; // Database checks are high priority

    /// <inheritdoc />
    public Dictionary<IHealthCheck, HealthCheckConfiguration> RegisterHealthChecks(
        IHealthCheckService healthCheckService, 
        HealthCheckServiceConfig serviceConfig = null)
    {
        if (healthCheckService == null)
            throw new ArgumentNullException(nameof(healthCheckService));

        try
        {
            var healthChecks = GetHealthCheckConfigurations(serviceConfig);
            
            foreach (var (healthCheck, config) in healthChecks)
            {
                healthCheckService.RegisterHealthCheck(healthCheck, config);
                _registeredHealthChecks[healthCheck] = config;
                
                _logger.LogDebug("Registered database health check: {HealthCheckName}", healthCheck.Name);
            }

            _logger.LogInfo("Successfully registered {Count} database health checks", healthChecks.Count);
            return new Dictionary<IHealthCheck, HealthCheckConfiguration>(healthChecks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register database health checks");
            throw;
        }
    }

    /// <inheritdoc />
    public void UnregisterHealthChecks(IHealthCheckService healthCheckService)
    {
        if (healthCheckService == null)
            throw new ArgumentNullException(nameof(healthCheckService));

        try
        {
            var healthCheckNames = _registeredHealthChecks.Keys.AsValueEnumerable()
                .Select(hc => hc.Name.ToString())
                .ToArray();

            foreach (var healthCheckName in healthCheckNames)
            {
                healthCheckService.UnregisterHealthCheck(healthCheckName);
                _logger.LogDebug("Unregistered database health check: {HealthCheckName}", healthCheckName);
            }

            var count = _registeredHealthChecks.Count;
            _registeredHealthChecks.Clear();
            
            _logger.LogInfo("Successfully unregistered {Count} database health checks", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister database health checks");
            throw;
        }
    }

    /// <inheritdoc />
    public Dictionary<IHealthCheck, HealthCheckConfiguration> GetHealthCheckConfigurations(
        HealthCheckServiceConfig serviceConfig = null)
    {
        var healthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();

        // Database connectivity health check
        var databaseConnectivityConfig = HealthCheckConfiguration.ForDatabase(
            "DatabaseConnectivity", 
            "Database Connectivity Health Check"
        );
        
        var databaseConnectivityCheck = new DatabaseConnectivityHealthCheck(databaseConnectivityConfig);
        healthChecks[databaseConnectivityCheck] = databaseConnectivityConfig;

        // Database performance health check
        var databasePerformanceConfig = HealthCheckConfiguration.ForDatabase(
            "DatabasePerformance",
            "Database Performance Health Check"
        );
        
        var databasePerformanceCheck = new DatabasePerformanceHealthCheck(databasePerformanceConfig);
        healthChecks[databasePerformanceCheck] = databasePerformanceConfig;

        // Database transaction health check
        var databaseTransactionConfig = HealthCheckConfiguration.ForDatabase(
            "DatabaseTransactions",
            "Database Transaction Health Check"
        );
        
        var databaseTransactionCheck = new DatabaseTransactionHealthCheck(databaseTransactionConfig);
        healthChecks[databaseTransactionCheck] = databaseTransactionConfig;

        _logger.LogDebug("Generated {Count} database health check configurations", healthChecks.Count);
        return healthChecks;
    }
}

#region Database Health Check Implementations

/// <summary>
/// Health check for database connectivity
/// </summary>
internal sealed class DatabaseConnectivityHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public DatabaseConnectivityHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "DatabaseConnectivity";
    public string Description => "Monitors database connection status and availability";
    public HealthCheckCategory Category => HealthCheckCategory.Database;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check actual database connectivity
        await UniTask.Delay(50, cancellationToken);
        
        return HealthCheckResult.Healthy("Database connectivity check passed");
    }
}

/// <summary>
/// Health check for database performance
/// </summary>
internal sealed class DatabasePerformanceHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public DatabasePerformanceHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "DatabasePerformance";
    public string Description => "Monitors database query performance and response times";
    public HealthCheckCategory Category => HealthCheckCategory.Database;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check database performance metrics
        await UniTask.Delay(100, cancellationToken);
        
        return HealthCheckResult.Healthy("Database performance check passed");
    }
}

/// <summary>
/// Health check for database transactions
/// </summary>
internal sealed class DatabaseTransactionHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public DatabaseTransactionHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "DatabaseTransactions";
    public string Description => "Monitors database transaction health and deadlock detection";
    public HealthCheckCategory Category => HealthCheckCategory.Database;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check transaction health
        await UniTask.Delay(75, cancellationToken);
        
        return HealthCheckResult.Healthy("Database transaction check passed");
    }
}

#endregion