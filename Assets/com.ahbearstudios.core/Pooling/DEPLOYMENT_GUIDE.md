# AhBearStudios Core Pooling System - Deployment Guide

## Pre-Deployment Preparation

### 1. Dependency Configuration

#### Required Dependencies (Existing)
- `ILoggingService` - Logging infrastructure
- `IMessageBusService` - Event publishing
- `IProfilerService` - Performance monitoring
- `IAlertService` - Alert notifications

#### Optional Dependencies (New)
- `IHealthCheckService` - Health monitoring integration
- `ICircuitBreakerFactory` - Circuit breaker protection
- `IPoolValidationService` - Advanced validation (has default implementation)

#### Dependency Injection Setup
```csharp
// Register required services (existing)
Container.Bind<ILoggingService>().To<LoggingService>().AsSingle();
Container.Bind<IMessageBusService>().To<MessageBusService>().AsSingle();
Container.Bind<IProfilerService>().To<ProfilerService>().AsSingle();
Container.Bind<IAlertService>().To<AlertService>().AsSingle();

// Register optional services (new - recommend adding)
Container.Bind<IHealthCheckService>().To<HealthCheckService>().AsSingle();
Container.Bind<ICircuitBreakerFactory>().To<CircuitBreakerFactory>().AsSingle();

// Register enhanced pooling service
Container.Bind<IPoolingService>().To<PoolingService>().AsSingle();
```

### 2. Performance Budget Configuration

#### Target Platform Configurations
```csharp
// High-end PC/Console (60+ FPS)
public static PoolConfiguration CreateHighPerformanceConfig()
{
    return new PoolConfiguration
    {
        InitialCapacity = 50,
        MaxCapacity = 500,
        MinCapacity = 10,
        StrategyType = PoolingStrategyType.HighPerformance,
        PerformanceBudget = PerformanceBudget.For60FPS(),
        EnablePerformanceMonitoring = true
    };
}

// Mobile/Lower-end targets (30 FPS)
public static PoolConfiguration CreateMobileConfig()
{
    return new PoolConfiguration
    {
        InitialCapacity = 25,
        MaxCapacity = 250,
        MinCapacity = 5,
        StrategyType = PoolingStrategyType.Dynamic,
        PerformanceBudget = PerformanceBudget.For30FPS(),
        EnablePerformanceMonitoring = true
    };
}

// Development/Debug builds
public static PoolConfiguration CreateDevelopmentConfig()
{
    return new PoolConfiguration
    {
        InitialCapacity = 10,
        MaxCapacity = 100,
        MinCapacity = 5,
        StrategyType = PoolingStrategyType.Default,
        PerformanceBudget = PerformanceBudget.ForDevelopment(),
        EnablePerformanceMonitoring = false // Reduced overhead
    };
}
```

## Deployment Steps

### Step 1: Update Constructor Calls

#### Before (Existing)
```csharp
var poolingService = new PoolingService(
    messageBusService,
    loggingService,
    profilerService,
    alertService
);
```

#### After (Enhanced)
```csharp
var poolingService = new PoolingService(
    messageBusService,
    loggingService,
    profilerService,
    alertService,
    healthCheckService,        // Optional - can be null
    circuitBreakerFactory,     // Optional - can be null
    strategySelector,          // Optional - has default
    poolTypeSelector,          // Optional - has default
    validationService,         // Optional - has default
    bufferFactory,             // Optional - has default
    configBuilder              // Optional - has default
);
```

### Step 2: Initialize Enhanced Features

```csharp
public class PoolingServiceInitializer : MonoBehaviour
{
    [Inject] private IPoolingService _poolingService;
    
    void Start()
    {
        // Register health checks if health service is available
        _poolingService.RegisterHealthChecks();
        
        // Start auto-scaling with appropriate interval
        var scalingInterval = Application.isEditor 
            ? TimeSpan.FromSeconds(5)   // Faster in editor for testing
            : TimeSpan.FromSeconds(30); // Production interval
            
        _poolingService.StartAutoScaling(scalingInterval);
        
        // Register common pools with platform-appropriate configurations
        RegisterCommonPools();
    }
    
    void OnDestroy()
    {
        _poolingService?.StopAutoScaling();
    }
    
    private void RegisterCommonPools()
    {
        var config = GetPlatformConfig();
        
        // Register network buffer pools
        _poolingService.RegisterPool<PooledNetworkBuffer>(config);
        
        // Register logging pools
        _poolingService.RegisterPool<ManagedLogData>(config);
        
        // Register application-specific pools
        _poolingService.RegisterPool<GameEntity>(config);
        _poolingService.RegisterPool<UIElement>(config);
    }
    
    private PoolConfiguration GetPlatformConfig()
    {
        #if UNITY_EDITOR
            return CreateDevelopmentConfig();
        #elif UNITY_MOBILE
            return CreateMobileConfig();
        #else
            return CreateHighPerformanceConfig();
        #endif
    }
}
```

### Step 3: Update Existing Pool Registration

#### Before
```csharp
_poolingService.RegisterPool<MyObject>();
```

#### After (Enhanced)
```csharp
var config = new PoolConfiguration
{
    InitialCapacity = 20,
    MaxCapacity = 200,
    MinCapacity = 5,
    StrategyType = PoolingStrategyType.HighPerformance,
    PerformanceBudget = PerformanceBudget.For60FPS()
};

_poolingService.RegisterPool<MyObject>(config);
```

### Step 4: Migrate to Async Operations (Recommended)

#### Before
```csharp
var obj = _poolingService.Get<MyObject>();
_poolingService.Return(obj);
```

#### After (Async with Cancellation)
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

var obj = await _poolingService.GetAsync<MyObject>(cts.Token);
await _poolingService.ReturnAsync(obj, cts.Token);
```

#### Bulk Operations
```csharp
// Get multiple objects efficiently
var objects = await _poolingService.GetMultipleAsync<MyObject>(
    count: 10, 
    timeout: TimeSpan.FromSeconds(1), 
    cancellationToken);

// Return all objects
await _poolingService.ReturnMultipleAsync(objects, cancellationToken);
```

## Production Configuration

### 1. Performance Monitoring Setup

```csharp
public class PoolingMonitor : MonoBehaviour
{
    [Inject] private IPoolingService _poolingService;
    
    void Start()
    {
        // Start periodic monitoring
        InvokeRepeating(nameof(CheckPoolHealth), 10f, 10f);
    }
    
    private void CheckPoolHealth()
    {
        // Check overall performance
        if (!_poolingService.IsPerformanceAcceptable())
        {
            Debug.LogWarning("Pool performance is degraded");
            LogPerformanceStatistics();
        }
        
        // Check recovery system
        if (!_poolingService.IsRecoverySystemHealthy())
        {
            Debug.LogError("Pool recovery system is unhealthy");
            LogRecoveryStatistics();
        }
        
        // Check overall health
        if (!_poolingService.IsHealthy())
        {
            Debug.LogError("Pool system is unhealthy");
            LogComprehensiveStats();
        }
    }
    
    private void LogPerformanceStatistics()
    {
        var stats = _poolingService.GetPerformanceStatistics();
        foreach (var kvp in stats)
        {
            Debug.Log($"Pool {kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");
        }
    }
    
    private void LogRecoveryStatistics()
    {
        var stats = _poolingService.GetErrorRecoveryStatistics();
        foreach (var kvp in stats)
        {
            Debug.Log($"Recovery {kvp.Key}: {JsonUtility.ToJson(kvp.Value)}");
        }
    }
    
    private void LogComprehensiveStats()
    {
        var perfStats = _poolingService.GetPerformanceStatistics();
        var circuitStats = _poolingService.GetCircuitBreakerStatistics();
        var scalingStats = _poolingService.GetAutoScalingStatistics();
        var recoveryStats = _poolingService.GetErrorRecoveryStatistics();
        
        Debug.Log($"Performance: {JsonUtility.ToJson(perfStats)}");
        Debug.Log($"Circuit Breakers: {JsonUtility.ToJson(circuitStats)}");
        Debug.Log($"Auto Scaling: {JsonUtility.ToJson(scalingStats)}");
        Debug.Log($"Recovery: {JsonUtility.ToJson(recoveryStats)}");
    }
}
```

### 2. Alert Configuration

```csharp
public class PoolingAlertConfiguration
{
    public static void ConfigureAlerts(IAlertService alertService, IPoolingService poolingService)
    {
        // Subscribe to critical pooling messages
        var messageBus = Container.Resolve<IMessageBusService>();
        
        messageBus.Subscribe<PoolCircuitBreakerStateChangedMessage>(msg =>
        {
            if (msg.NewState == "Open")
            {
                alertService.RaiseAlert(
                    AlertSeverity.Critical,
                    $"Circuit breaker opened for pool: {msg.CircuitBreakerName}",
                    "PoolingService.CircuitBreaker"
                );
            }
        });
        
        messageBus.Subscribe<PoolExpansionMessage>(msg =>
        {
            if (msg.NewCapacity > msg.PreviousCapacity * 2) // 100% growth
            {
                alertService.RaiseAlert(
                    AlertSeverity.Warning,
                    $"Rapid pool expansion: {msg.PoolName} grew from {msg.PreviousCapacity} to {msg.NewCapacity}",
                    "PoolingService.Scaling"
                );
            }
        });
    }
}
```

### 3. Platform-Specific Optimizations

#### Mobile Optimizations
```csharp
#if UNITY_MOBILE
public static class MobilePoolingOptimizations
{
    public static void OptimizeForMobile(IPoolingService poolingService)
    {
        // More conservative scaling intervals
        poolingService.StartAutoScaling(TimeSpan.FromMinutes(1));
        
        // Use more conservative performance budgets
        var mobileConfig = new PoolConfiguration
        {
            InitialCapacity = 15,
            MaxCapacity = 150,
            MinCapacity = 5,
            StrategyType = PoolingStrategyType.Dynamic,
            PerformanceBudget = PerformanceBudget.For30FPS()
        };
        
        // Register mobile-optimized pools
        poolingService.RegisterPool<MobileOptimizedObject>(mobileConfig);
    }
}
#endif
```

#### Console/PC Optimizations
```csharp
#if UNITY_STANDALONE || UNITY_CONSOLE
public static class HighPerformanceOptimizations
{
    public static void OptimizeForHighPerformance(IPoolingService poolingService)
    {
        // Aggressive scaling for high-performance targets
        poolingService.StartAutoScaling(TimeSpan.FromSeconds(15));
        
        var highPerfConfig = new PoolConfiguration
        {
            InitialCapacity = 100,
            MaxCapacity = 1000,
            MinCapacity = 20,
            StrategyType = PoolingStrategyType.HighPerformance,
            PerformanceBudget = PerformanceBudget.For60FPS()
        };
        
        // Pre-register large pools for performance
        poolingService.RegisterPool<HighPerformanceObject>(highPerfConfig);
    }
}
#endif
```

## Monitoring and Observability

### 1. Dashboard Metrics

#### Key Performance Indicators (KPIs)
```csharp
public class PoolingDashboard
{
    public struct PoolingMetrics
    {
        public int TotalPools;
        public bool OverallHealth;
        public double AveragePerformanceViolationRate;
        public int ActiveCircuitBreakers;
        public int TotalScalingEvents;
        public int RecoveryAttempts;
        public double RecoverySuccessRate;
    }
    
    public static PoolingMetrics GetDashboardMetrics(IPoolingService poolingService)
    {
        var perfStats = poolingService.GetPerformanceStatistics();
        var circuitStats = poolingService.GetCircuitBreakerStatistics();
        var scalingStats = poolingService.GetAutoScalingStatistics();
        var recoveryStats = poolingService.GetErrorRecoveryStatistics();
        
        return new PoolingMetrics
        {
            TotalPools = perfStats.Count,
            OverallHealth = poolingService.IsHealthy(),
            AveragePerformanceViolationRate = CalculateAverageViolationRate(perfStats),
            ActiveCircuitBreakers = CountActiveCircuitBreakers(circuitStats),
            TotalScalingEvents = CountScalingEvents(scalingStats),
            RecoveryAttempts = CountRecoveryAttempts(recoveryStats),
            RecoverySuccessRate = CalculateRecoverySuccessRate(recoveryStats)
        };
    }
}
```

### 2. Log Aggregation

```csharp
public class PoolingLogAggregator
{
    public static void SetupLogAggregation(ILoggingService loggingService)
    {
        // Filter and aggregate pooling-related logs
        loggingService.AddLogFilter("PoolingService", LogLevel.Info);
        loggingService.AddLogFilter("CircuitBreaker", LogLevel.Warning);
        loggingService.AddLogFilter("AutoScaling", LogLevel.Info);
        loggingService.AddLogFilter("Recovery", LogLevel.Warning);
    }
}
```

## Troubleshooting Guide

### Common Issues and Solutions

#### 1. High Performance Budget Violations
```csharp
// Problem: Performance violations > 10%
// Solution: Check and adjust performance budget
var perfStats = poolingService.GetPerformanceStatistics();
foreach (var stat in perfStats)
{
    if (((dynamic)stat.Value).ViolationRatePercentage > 10)
    {
        Debug.LogWarning($"Pool {stat.Key} has high violation rate");
        // Consider adjusting performance budget or optimizing operations
    }
}
```

#### 2. Circuit Breaker Frequently Opens
```csharp
// Problem: Circuit breakers opening frequently
// Solution: Investigate underlying issues and consider recovery
var circuitStats = poolingService.GetCircuitBreakerStatistics();
foreach (var stat in circuitStats)
{
    if (((dynamic)stat.Value).State == "Open")
    {
        Debug.LogError($"Circuit breaker {stat.Key} is open");
        // Force recovery if needed
        await poolingService.ForcePoolRecovery(stat.Key);
    }
}
```

#### 3. Memory Usage Growing
```csharp
// Problem: Pool memory usage continuously growing
// Solution: Check scaling behavior and enable trimming
var scalingStats = poolingService.GetAutoScalingStatistics();
foreach (var stat in scalingStats)
{
    var avgUtilization = ((dynamic)stat.Value).AverageUtilization;
    if (avgUtilization < 0.3) // Low utilization but high capacity
    {
        Debug.LogWarning($"Pool {stat.Key} may be over-scaled");
        // Consider adjusting scaling thresholds
    }
}
```

### Emergency Procedures

#### System-Wide Recovery
```csharp
public static async UniTask EmergencyPoolingRecovery(IPoolingService poolingService)
{
    Debug.LogError("Performing emergency pooling recovery");
    
    // Stop auto-scaling
    poolingService.StopAutoScaling();
    
    // Force recovery on all pools
    var recoveryStats = poolingService.GetErrorRecoveryStatistics();
    foreach (var poolType in recoveryStats.Keys)
    {
        try
        {
            await poolingService.ForcePoolRecovery(poolType);
            Debug.Log($"Recovered pool: {poolType}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to recover pool {poolType}: {ex.Message}");
        }
    }
    
    // Restart auto-scaling
    poolingService.StartAutoScaling(TimeSpan.FromMinutes(1));
    
    Debug.Log("Emergency recovery completed");
}
```

## Rollback Plan

### Gradual Rollback Strategy
```csharp
public class PoolingRollback
{
    // Phase 1: Disable new features, keep compatibility
    public static void DisableEnhancedFeatures(IPoolingService poolingService)
    {
        poolingService.StopAutoScaling();
        // Continue using existing sync operations
        // Disable performance monitoring alerts
    }
    
    // Phase 2: Revert to previous implementation if needed
    // Keep old PoolingService implementation available as fallback
    // Switch DI registration back to old implementation
}
```

## Performance Baselines

### Expected Performance Metrics
- **Get/Return Operations**: < 0.1ms average
- **Memory Allocation**: < 1KB per 1000 operations
- **Frame Rate Impact**: < 0.1ms per frame
- **Auto-scaling Response**: < 5 seconds
- **Recovery Success Rate**: > 95%

### Monitoring Thresholds
- **Performance Violations**: Alert if > 10%
- **Circuit Breaker Opens**: Alert immediately
- **Recovery Failures**: Alert if success rate < 80%
- **Memory Growth**: Alert if > 20% increase per hour

This deployment guide ensures a smooth transition to the enhanced pooling system while maintaining production stability and performance requirements.