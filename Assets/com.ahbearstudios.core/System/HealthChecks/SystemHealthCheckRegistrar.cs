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

namespace AhBearStudios.Core.Infrastructure.HealthChecks;

/// <summary>
/// System domain health check registrar implementing the domain self-registration pattern.
/// Manages registration of all system resource-related health checks with the HealthCheckService.
/// </summary>
public sealed class SystemHealthCheckRegistrar : IDomainHealthCheckRegistrar
{
    private readonly ILoggingService _logger;
    private readonly Dictionary<IHealthCheck, HealthCheckConfiguration> _registeredHealthChecks;

    /// <summary>
    /// Initializes a new instance of the SystemHealthCheckRegistrar
    /// </summary>
    /// <param name="logger">Logging service for registration operations</param>
    public SystemHealthCheckRegistrar(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registeredHealthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();
    }

    /// <inheritdoc />
    public string DomainName => "System";

    /// <inheritdoc />
    public int RegistrationPriority => 1000; // System checks are highest priority

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
                
                _logger.LogDebug("Registered system health check: {HealthCheckName}", healthCheck.Name);
            }

            _logger.LogInfo("Successfully registered {Count} system health checks", healthChecks.Count);
            return new Dictionary<IHealthCheck, HealthCheckConfiguration>(healthChecks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register system health checks");
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
                _logger.LogDebug("Unregistered system health check: {HealthCheckName}", healthCheckName);
            }

            var count = _registeredHealthChecks.Count;
            _registeredHealthChecks.Clear();
            
            _logger.LogInfo("Successfully unregistered {Count} system health checks", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister system health checks");
            throw;
        }
    }

    /// <inheritdoc />
    public Dictionary<IHealthCheck, HealthCheckConfiguration> GetHealthCheckConfigurations(
        HealthCheckServiceConfig serviceConfig = null)
    {
        var healthChecks = new Dictionary<IHealthCheck, HealthCheckConfiguration>();

        // Memory usage health check
        var memoryConfig = HealthCheckConfiguration.ForCriticalSystem(
            "SystemMemory", 
            "System Memory Usage Health Check"
        );
        
        var memoryCheck = new SystemMemoryHealthCheck(memoryConfig);
        healthChecks[memoryCheck] = memoryConfig;

        // CPU usage health check
        var cpuConfig = HealthCheckConfiguration.ForCriticalSystem(
            "SystemCPU",
            "System CPU Usage Health Check"
        );
        
        var cpuCheck = new SystemCPUHealthCheck(cpuConfig);
        healthChecks[cpuCheck] = cpuConfig;

        // Disk usage health check
        var diskConfig = HealthCheckConfiguration.ForCriticalSystem(
            "SystemDisk",
            "System Disk Usage Health Check"
        );
        
        var diskCheck = new SystemDiskHealthCheck(diskConfig);
        healthChecks[diskCheck] = diskConfig;

        // Frame rate health check (Unity-specific)
        var frameRateConfig = HealthCheckConfiguration.ForCriticalSystem(
            "SystemFrameRate",
            "System Frame Rate Health Check"
        );
        
        var frameRateCheck = new SystemFrameRateHealthCheck(frameRateConfig);
        healthChecks[frameRateCheck] = frameRateConfig;

        _logger.LogDebug("Generated {Count} system health check configurations", healthChecks.Count);
        return healthChecks;
    }
}

#region System Health Check Implementations

/// <summary>
/// Health check for system memory usage
/// </summary>
internal sealed class SystemMemoryHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public SystemMemoryHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "SystemMemory";
    public string Description => "Monitors system memory usage and garbage collection performance";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check actual memory usage
        await UniTask.Delay(25, cancellationToken);
        
        var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024); // MB
        
        if (memoryUsage > 1000) // > 1GB
        {
            return HealthCheckResult.Degraded($"High memory usage: {memoryUsage}MB");
        }
        
        return HealthCheckResult.Healthy($"Memory usage normal: {memoryUsage}MB");
    }
}

/// <summary>
/// Health check for system CPU usage
/// </summary>
internal sealed class SystemCPUHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public SystemCPUHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "SystemCPU";
    public string Description => "Monitors system CPU usage and performance metrics";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check actual CPU usage
        await UniTask.Delay(50, cancellationToken);
        
        return HealthCheckResult.Healthy("CPU usage normal");
    }
}

/// <summary>
/// Health check for system disk usage
/// </summary>
internal sealed class SystemDiskHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public SystemDiskHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "SystemDisk";
    public string Description => "Monitors system disk space and I/O performance";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check actual disk usage
        await UniTask.Delay(75, cancellationToken);
        
        return HealthCheckResult.Healthy("Disk usage normal");
    }
}

/// <summary>
/// Health check for Unity frame rate performance
/// </summary>
internal sealed class SystemFrameRateHealthCheck : IHealthCheck
{
    private readonly HealthCheckConfiguration _configuration;

    public SystemFrameRateHealthCheck(HealthCheckConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public FixedString64Bytes Name => "SystemFrameRate";
    public string Description => "Monitors Unity application frame rate and performance";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout => _configuration.Timeout;
    public HealthCheckConfiguration Configuration => _configuration;

    public async UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would check Unity frame rate
        await UniTask.Delay(10, cancellationToken);
        
        var fps = UnityEngine.Application.targetFrameRate > 0 
            ? UnityEngine.Application.targetFrameRate 
            : 60;
        
        if (fps < 30)
        {
            return HealthCheckResult.Degraded($"Low frame rate: {fps} FPS");
        }
        
        return HealthCheckResult.Healthy($"Frame rate normal: {fps} FPS");
    }
}

#endregion