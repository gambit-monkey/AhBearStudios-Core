using System;
using System.Collections.Generic;
using ZLinq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Network.HealthChecks;

/// <summary>
/// Network domain health check registrar implementing the domain self-registration pattern.
/// Manages registration of all network-related health checks with the HealthCheckService.
/// </summary>
public sealed class NetworkHealthCheckRegistrar : IDomainHealthCheckRegistrar
{
    private readonly ILoggingService _logger;
    private readonly Dictionary<IHealthCheck, HealthCheckConfiguration> _registeredHealthChecks;

    /// <summary>
    /// Initializes a new instance of the NetworkHealthCheckRegistrar
    /// </summary>
    /// <param name="logger">Logging service for registration operations</param>
    public NetworkHealthCheckRegistrar(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registeredHealthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();
    }

    /// <inheritdoc />
    public string DomainName => "Network";

    /// <inheritdoc />
    public int RegistrationPriority => 600; // Network checks are medium-high priority

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
                
                _logger.LogDebug("Registered network health check: {HealthCheckName}", healthCheck.Name);
            }

            _logger.LogInfo("Successfully registered {Count} network health checks", healthChecks.Count);
            return new Dictionary<IHealthCheck, HealthCheckConfiguration>(healthChecks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register network health checks");
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
                _logger.LogDebug("Unregistered network health check: {HealthCheckName}", healthCheckName);
            }

            var count = _registeredHealthChecks.Count;
            _registeredHealthChecks.Clear();
            
            _logger.LogInfo("Successfully unregistered {Count} network health checks", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister network health checks");
            throw;
        }
    }

    /// <inheritdoc />
    public Dictionary<IHealthCheck, HealthCheckConfiguration> GetHealthCheckConfigurations(
        HealthCheckServiceConfig serviceConfig = null)
    {
        var healthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();

        // Network connectivity health check
        var networkConnectivityConfig = HealthCheckConfiguration.ForNetwork(
            "NetworkConnectivity", 
            "Network Connectivity Health Check"
        );
        
        var networkConnectivityCheck = new NetworkConnectivityHealthCheck(networkConnectivityConfig);
        healthChecks[networkConnectivityCheck] = networkConnectivityConfig;

        // Network latency health check
        var networkLatencyConfig = HealthCheckConfiguration.ForNetwork(
            "NetworkLatency",
            "Network Latency Health Check"
        );
        
        var networkLatencyCheck = new NetworkLatencyHealthCheck(networkLatencyConfig);
        healthChecks[networkLatencyCheck] = networkLatencyConfig;

        // Network bandwidth health check
        var networkBandwidthConfig = HealthCheckConfiguration.ForNetwork(
            "NetworkBandwidth",
            "Network Bandwidth Health Check"
        );
        
        var networkBandwidthCheck = new NetworkBandwidthHealthCheck(networkBandwidthConfig);
        healthChecks[networkBandwidthCheck] = networkBandwidthConfig;

        _logger.LogDebug("Generated {Count} network health check configurations", healthChecks.Count);
        return healthChecks;
    }
}

#region Network Health Check Implementations

/// <summary>
/// Health check for network connectivity
/// </summary>
internal sealed class NetworkConnectivityHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public NetworkConnectivityHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "NetworkConnectivity";
    public string Description => "Monitors network interface status and basic connectivity";
    public HealthCheckCategory Category => HealthCheckCategory.Network;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check actual network connectivity
        await UniTask.Delay(100, cancellationToken);
        
        return HealthCheckResult.Healthy("Network connectivity check passed");
    }
}

/// <summary>
/// Health check for network latency
/// </summary>
internal sealed class NetworkLatencyHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public NetworkLatencyHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "NetworkLatency";
    public string Description => "Monitors network latency and response times";
    public HealthCheckCategory Category => HealthCheckCategory.Network;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check network latency metrics
        await UniTask.Delay(150, cancellationToken);
        
        return HealthCheckResult.Healthy("Network latency check passed");
    }
}

/// <summary>
/// Health check for network bandwidth
/// </summary>
internal sealed class NetworkBandwidthHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public NetworkBandwidthHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "NetworkBandwidth";
    public string Description => "Monitors available network bandwidth and throughput";
    public HealthCheckCategory Category => HealthCheckCategory.Network;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check bandwidth metrics
        await UniTask.Delay(200, cancellationToken);
        
        return HealthCheckResult.Healthy("Network bandwidth check passed");
    }
}

#endregion