using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Unity.Common.Components;

namespace AhBearStudios.Unity.Common.HealthChecks;

/// <summary>
/// Health check implementation for MainThreadDispatcher.
/// Monitors queue depth, processing performance, and system health.
/// </summary>
public sealed class MainThreadDispatcherHealthCheck : IHealthCheck
{
    private readonly IMainThreadDispatcher _dispatcher;
    private HealthCheckConfiguration _configuration;
    private readonly float _queueWarningThreshold;
    private readonly float _queueCriticalThreshold;
    private readonly float _frameBudgetWarningMs;
    
    /// <summary>
    /// Unique identifier for this health check using FixedString64Bytes.
    /// </summary>
    public FixedString64Bytes Name { get; private set; } = "MainThreadDispatcher";
    
    /// <summary>
    /// Human-readable description of what this health check validates.
    /// </summary>
    public string Description { get; private set; } = "Monitors MainThreadDispatcher queue depth, processing performance, and system health";
    
    /// <summary>
    /// Health check category for grouping related checks.
    /// </summary>
    public HealthCheckCategory Category { get; private set; } = HealthCheckCategory.System;
    
    /// <summary>
    /// Maximum time this health check should be allowed to run before timing out.
    /// </summary>
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Current configuration for this health check instance.
    /// </summary>
    public HealthCheckConfiguration Configuration => _configuration;
    
    /// <summary>
    /// Names of other health checks that must be healthy for this check to execute.
    /// </summary>
    public IEnumerable<FixedString64Bytes> Dependencies { get; private set; } = Enumerable.Empty<FixedString64Bytes>();
    
    /// <summary>
    /// Initializes a new MainThreadDispatcherHealthCheck with configuration.
    /// </summary>
    /// <param name="dispatcher">The dispatcher instance to monitor</param>
    /// <param name="configuration">Health check configuration (optional, will use default if null)</param>
    public MainThreadDispatcherHealthCheck(
        IMainThreadDispatcher dispatcher,
        HealthCheckConfiguration configuration = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        
        // Use provided configuration or create default
        _configuration = configuration ?? CreateDefaultConfiguration();
        
        // Extract thresholds from configuration or use defaults
        _queueWarningThreshold = GetConfigValue<float>("QueueWarningThreshold", 0.7f);
        _queueCriticalThreshold = GetConfigValue<float>("QueueCriticalThreshold", 0.9f);
        _frameBudgetWarningMs = GetConfigValue<float>("FrameBudgetWarningMs", 12.0f);
        
        // Apply configuration to properties
        ApplyConfiguration(_configuration);
    }
    
    /// <summary>
    /// Initializes a new MainThreadDispatcherHealthCheck with legacy parameters.
    /// </summary>
    /// <param name="dispatcher">The dispatcher instance to monitor</param>
    /// <param name="queueWarningThreshold">Queue depth warning threshold (0.0-1.0)</param>
    /// <param name="queueCriticalThreshold">Queue depth critical threshold (0.0-1.0)</param>
    /// <param name="frameBudgetWarningMs">Frame budget warning threshold in milliseconds</param>
    public MainThreadDispatcherHealthCheck(
        IMainThreadDispatcher dispatcher,
        float queueWarningThreshold,
        float queueCriticalThreshold,
        float frameBudgetWarningMs)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _queueWarningThreshold = queueWarningThreshold;
        _queueCriticalThreshold = queueCriticalThreshold;
        _frameBudgetWarningMs = frameBudgetWarningMs;
        
        // Create configuration from legacy parameters
        _configuration = CreateConfigurationFromLegacyParameters(
            queueWarningThreshold, queueCriticalThreshold, frameBudgetWarningMs);
        ApplyConfiguration(_configuration);
    }
    
    /// <summary>
    /// Executes the health check asynchronously and returns a comprehensive result.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation requests</param>
    /// <returns>Task representing the asynchronous health check operation</returns>
    public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = await PerformHealthCheckInternalAsync(cancellationToken);
            
            stopwatch.Stop();
            
            // Update result with timing information
            return result with
            {
                Duration = stopwatch.Elapsed,
                Timestamp = startTime,
                Name = Name.ToString()
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                Name.ToString(),
                "Health check was cancelled",
                stopwatch.Elapsed,
                CreateDiagnosticData(HealthStatus.Unhealthy, "Cancelled")
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                Name.ToString(),
                $"MainThreadDispatcher health check failed: {ex.Message}",
                stopwatch.Elapsed,
                CreateDiagnosticData(HealthStatus.Unhealthy, ex.Message),
                ex
            );
        }
    }
    
    /// <summary>
    /// Performs the internal health check logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    private async UniTask<HealthCheckResult> PerformHealthCheckInternalAsync(CancellationToken cancellationToken)
    {
        // Check if dispatcher is initialized
        if (!_dispatcher.IsInitialized)
        {
            return HealthCheckResult.Unhealthy(
                Name.ToString(),
                "MainThreadDispatcher is not initialized",
                data: CreateDiagnosticData(HealthStatus.Unhealthy, "Not initialized")
            );
        }
        
        // Allow for async operations with small delay to simulate real async health check
        await UniTask.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        
        var currentQueue = _dispatcher.PendingActionCount;
        var maxCapacity = _dispatcher.MaxQueueCapacity;
        var frameBudget = _dispatcher.FrameBudgetMs;
        
        // Calculate queue utilization
        var queueUtilization = maxCapacity > 0 ? (float)currentQueue / maxCapacity : 0f;
        
        // Determine health status based on queue utilization
        var status = DetermineHealthStatus(queueUtilization, frameBudget);
        var message = CreateHealthMessage(status, currentQueue, maxCapacity, queueUtilization, frameBudget);
        var diagnosticData = CreateDiagnosticData(status, message, currentQueue, maxCapacity, queueUtilization, frameBudget);
        
        return new HealthCheckResult
        {
            Name = Name.ToString(),
            Status = status,
            Message = message,
            Description = Description,
            Data = diagnosticData,
            Category = Category
        };
    }
    
    /// <summary>
    /// Determines the health status based on current metrics.
    /// </summary>
    private HealthStatus DetermineHealthStatus(float queueUtilization, float frameBudget)
    {
        // Critical conditions
        if (queueUtilization >= _queueCriticalThreshold)
        {
            return HealthStatus.Unhealthy;
        }
        
        if (frameBudget >= _frameBudgetWarningMs)
        {
            return HealthStatus.Unhealthy;
        }
        
        // Warning conditions
        if (queueUtilization >= _queueWarningThreshold)
        {
            return HealthStatus.Degraded;
        }
        
        if (frameBudget >= _frameBudgetWarningMs * 0.8f) // 80% of warning threshold
        {
            return HealthStatus.Degraded;
        }
        
        return HealthStatus.Healthy;
    }
    
    /// <summary>
    /// Creates a human-readable health message.
    /// </summary>
    private string CreateHealthMessage(HealthStatus status, int currentQueue, int maxCapacity, 
                                     float queueUtilization, float frameBudget)
    {
        return status switch
        {
            HealthStatus.Healthy => $"MainThreadDispatcher healthy - Queue: {currentQueue}/{maxCapacity} ({queueUtilization:P1}), Budget: {frameBudget:F1}ms",
            HealthStatus.Degraded => $"MainThreadDispatcher degraded - Queue: {currentQueue}/{maxCapacity} ({queueUtilization:P1}), Budget: {frameBudget:F1}ms",
            HealthStatus.Unhealthy => $"MainThreadDispatcher unhealthy - Queue: {currentQueue}/{maxCapacity} ({queueUtilization:P1}), Budget: {frameBudget:F1}ms",
            _ => $"MainThreadDispatcher status unknown - Queue: {currentQueue}/{maxCapacity} ({queueUtilization:P1}), Budget: {frameBudget:F1}ms"
        };
    }
    
    /// <summary>
    /// Creates diagnostic data for the health check result.
    /// </summary>
    private Dictionary<string, object> CreateDiagnosticData(HealthStatus status, string message, 
                                                           int currentQueue = 0, int maxCapacity = 0, 
                                                           float queueUtilization = 0f, float frameBudget = 0f)
    {
        var data = new Dictionary<string, object>
        {
            ["Status"] = status.ToString(),
            ["Message"] = message,
            ["Timestamp"] = DateTime.UtcNow,
            ["IsInitialized"] = _dispatcher.IsInitialized
        };
        
        if (_dispatcher.IsInitialized)
        {
            data["CurrentQueueCount"] = currentQueue;
            data["MaxQueueCapacity"] = maxCapacity;
            data["QueueUtilization"] = queueUtilization;
            data["QueueUtilizationPercentage"] = $"{queueUtilization:P1}";
            data["FrameBudgetMs"] = frameBudget;
            data["QueueWarningThreshold"] = _queueWarningThreshold;
            data["QueueCriticalThreshold"] = _queueCriticalThreshold;
            data["FrameBudgetWarningMs"] = _frameBudgetWarningMs;
            data["IsOnMainThread"] = _dispatcher.IsMainThread;
        }
        
        return data;
    }
    
    /// <summary>
    /// Configures the health check with new configuration settings.
    /// </summary>
    /// <param name="configuration">Configuration to apply to this health check</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
    public void Configure(HealthCheckConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        _configuration = configuration;
        ApplyConfiguration(configuration);
    }
    
    /// <summary>
    /// Gets comprehensive metadata about this health check for introspection and monitoring.
    /// </summary>
    /// <returns>Dictionary containing metadata about the health check</returns>
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Name"] = Name.ToString(),
            ["Description"] = Description,
            ["Category"] = Category.ToString(),
            ["Timeout"] = Timeout,
            ["Version"] = "1.0.0",
            ["Implementation"] = GetType().FullName,
            ["Dependencies"] = Dependencies.Select(d => d.ToString()).ToArray(),
            ["DispatcherType"] = _dispatcher.GetType().FullName,
            ["QueueWarningThreshold"] = _queueWarningThreshold,
            ["QueueCriticalThreshold"] = _queueCriticalThreshold,
            ["FrameBudgetWarningMs"] = _frameBudgetWarningMs,
            ["IsDispatcherInitialized"] = _dispatcher.IsInitialized,
            ["MaxQueueCapacity"] = _dispatcher.MaxQueueCapacity,
            ["FrameBudgetMs"] = _dispatcher.FrameBudgetMs,
            ["SupportsAsync"] = true,
            ["ConfigurationId"] = _configuration?.Id.ToString() ?? "None",
            ["LastConfigured"] = _configuration?.ModifiedAt ?? DateTime.MinValue,
            ["CreatedAt"] = DateTime.UtcNow,
            ["Platform"] = "Unity",
            ["Runtime"] = "MainThread"
        };
    }
    
    /// <summary>
    /// Creates a default configuration for the health check.
    /// </summary>
    /// <returns>Default health check configuration</returns>
    private static HealthCheckConfiguration CreateDefaultConfiguration()
    {
        return HealthCheckConfiguration.ForCriticalSystem(
            "MainThreadDispatcher",
            "Unity Main Thread Dispatcher",
            "Monitors MainThreadDispatcher queue depth, processing performance, and system health"
        ).WithMetadata("QueueWarningThreshold", 0.7f)
         .WithMetadata("QueueCriticalThreshold", 0.9f)
         .WithMetadata("FrameBudgetWarningMs", 12.0f);
    }
    
    /// <summary>
    /// Creates configuration from legacy constructor parameters.
    /// </summary>
    /// <param name="queueWarningThreshold">Queue warning threshold</param>
    /// <param name="queueCriticalThreshold">Queue critical threshold</param>
    /// <param name="frameBudgetWarningMs">Frame budget warning threshold</param>
    /// <returns>Health check configuration</returns>
    private static HealthCheckConfiguration CreateConfigurationFromLegacyParameters(
        float queueWarningThreshold, float queueCriticalThreshold, float frameBudgetWarningMs)
    {
        return CreateDefaultConfiguration()
            .WithMetadata("QueueWarningThreshold", queueWarningThreshold)
            .WithMetadata("QueueCriticalThreshold", queueCriticalThreshold)
            .WithMetadata("FrameBudgetWarningMs", frameBudgetWarningMs);
    }
    
    /// <summary>
    /// Applies configuration settings to the health check properties.
    /// </summary>
    /// <param name="configuration">Configuration to apply</param>
    private void ApplyConfiguration(HealthCheckConfiguration configuration)
    {
        if (configuration == null) return;
        
        Name = configuration.Name;
        Description = configuration.Description;
        Category = configuration.Category;
        Timeout = configuration.Timeout;
        Dependencies = configuration.Dependencies;
    }
    
    /// <summary>
    /// Gets a configuration value with fallback to default.
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Configuration value or default</returns>
    private T GetConfigValue<T>(string key, T defaultValue)
    {
        if (_configuration?.Metadata?.TryGetValue(key, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}