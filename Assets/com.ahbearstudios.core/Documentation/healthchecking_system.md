# HealthCheck System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.HealthCheck`  
**Role:** System health monitoring, protection, and graceful degradation  
**Status:** ✅ Core Infrastructure

The HealthCheck System provides comprehensive health monitoring capabilities for all systems, enabling proactive issue detection, automated health reporting, system protection through circuit breakers, and graceful degradation patterns. This system serves as the central point for maintaining system resilience and operational health.

## 🚀 Key Features

- **⚡ Real-Time Monitoring**: Continuous health assessment of all registered systems
- **🛡️ Circuit Breaker Protection**: Automatic failure isolation and system protection
- **🔧 Flexible Health Checks**: Custom health check implementations for any system
- **📊 Health Aggregation**: Hierarchical health status with dependency tracking
- **🎯 Automated Scheduling**: Configurable health check intervals and timing
- **📈 Health History**: Historical health data tracking and trend analysis
- **🔄 Alert Integration**: Automatic alert generation for health status changes
- **🏥 Graceful Degradation**: Automatic system degradation based on health status
- **🔁 Recovery Management**: Automatic recovery detection and circuit breaker reset

## 🏗️ Architecture

### Folder Structure

```
AhBearStudios.Core.HealthCheck/
├── IHealthCheckService.cs                # Enhanced primary service interface
├── HealthCheckService.cs                 # Health monitoring with circuit breakers
├── ICircuitBreaker.cs                    # Circuit breaker interface
├── CircuitBreaker.cs                     # Circuit breaker implementation
├── Configs/
│   ├── HealthCheckServiceConfig.cs       # Main service configuration
│   ├── HealthCheckConfig.cs              # Individual check configuration
│   ├── CircuitBreakerConfig.cs           # Circuit breaker settings
│   ├── DegradationConfig.cs              # Graceful degradation rules
│   └── SchedulingConfig.cs               # Check scheduling settings
├── Builders/
│   ├── IHealthCheckServiceConfigBuilder.cs # Configuration builder interface
│   └── HealthCheckServiceConfigBuilder.cs  # Builder implementation
├── Factories/
│   ├── IHealthCheckFactory.cs            # Health check creation interface
│   ├── HealthCheckFactory.cs             # Factory implementation
│   └── CircuitBreakerFactory.cs          # Circuit breaker factory
├── Services/
│   ├── HealthAggregationService.cs       # Health status aggregation
│   ├── HealthHistoryService.cs           # Historical tracking
│   ├── HealthSchedulingService.cs        # Check scheduling
│   ├── DegradationManagementService.cs   # Graceful degradation
│   └── CircuitBreakerManager.cs          # Circuit breaker management
├── Schedulers/
│   ├── IHealthCheckScheduler.cs          # Scheduler interface
│   ├── IntervalScheduler.cs              # Interval-based scheduling
│   ├── CronScheduler.cs                  # Cron-based scheduling
│   └── AdaptiveScheduler.cs              # Adaptive scheduling
├── Models/
│   ├── HealthCheckResult.cs              # Health check result
│   ├── HealthReport.cs                   # Comprehensive health report
│   ├── HealthStatus.cs                   # Status enumeration
│   ├── HealthCheckCategory.cs            # Category enumeration
│   ├── DegradationLevel.cs               # Degradation levels
│   └── CircuitBreakerState.cs            # Circuit breaker states
└── HealthChecks/
    ├── SystemHealthCheck.cs              # System-level checks
    ├── DatabaseHealthCheck.cs            # Database connectivity
    ├── NetworkHealthCheck.cs             # Network connectivity
    └── PerformanceHealthCheck.cs         # Performance monitoring

AhBearStudios.Unity.HealthCheck/
├── Installers/
│   └── HealthCheckInstaller.cs           # Reflex registration
├── Components/
│   ├── HealthMonitorComponent.cs         # Unity monitoring component
│   └── HealthDisplayComponent.cs         # Visual health display
└── ScriptableObjects/
    └── HealthCheckConfigAsset.cs         # Unity configuration
```

## 🔌 Key Interfaces

### IHealthCheckService

The primary interface for all health monitoring operations.

```csharp
public interface IHealthCheckService
{
    // Health check registration
    void RegisterHealthCheck(IHealthCheck healthCheck);
    void RegisterHealthCheck<T>() where T : class, IHealthCheck;
    void UnregisterHealthCheck(FixedString64Bytes name);
    
    // Health check execution
    Task<HealthCheckResult> CheckHealthAsync(FixedString64Bytes name, CancellationToken cancellationToken = default);
    Task<HealthReport> CheckAllHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthReport> CheckHealthByCategory(HealthCheckCategory category, CancellationToken cancellationToken = default);
    
    // Overall health status
    Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default);
    HealthStatus GetCachedOverallHealthStatus();
    
    // Automatic monitoring
    void StartAutomaticChecks();
    void StopAutomaticChecks();
    bool IsAutomaticChecksEnabled { get; }
    
    // Circuit breaker management
    void EnableCircuitBreaker(FixedString64Bytes healthCheckName, CircuitBreakerConfig config);
    void DisableCircuitBreaker(FixedString64Bytes healthCheckName);
    CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes healthCheckName);
    
    // Graceful degradation
    DegradationLevel GetCurrentDegradationLevel();
    void SetDegradationLevel(DegradationLevel level, string reason);
    bool IsFeatureEnabled(FixedString64Bytes featureName);
    
    // History and reporting
    IEnumerable<HealthCheckResult> GetHealthHistory(FixedString64Bytes name, TimeSpan period);
    HealthReport GetLastHealthReport();
    IEnumerable<HealthReport> GetHealthReportHistory(TimeSpan period);
    
    // Statistics
    HealthServiceStatistics GetStatistics();
    
    // Events
    event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
    event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
    event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;
    event EventHandler<HealthCheckExecutedEventArgs> HealthCheckExecuted;
}
```

### IHealthCheck

Interface for individual health check implementations.

```csharp
public interface IHealthCheck
{
    // Basic properties
    FixedString64Bytes Name { get; }
    string Description { get; }
    HealthCheckCategory Category { get; }
    TimeSpan Timeout { get; }
    
    // Execution
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    // Configuration
    HealthCheckConfiguration Configuration { get; }
    void Configure(HealthCheckConfiguration configuration);
    
    // Dependencies and metadata
    IEnumerable<FixedString64Bytes> Dependencies { get; }
    Dictionary<string, object> GetMetadata();
}

public enum HealthCheckCategory
{
    System,
    Database,
    Network,
    Performance,
    Security,
    CircuitBreaker,
    Custom
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public enum DegradationLevel
{
    None,      // Full functionality
    Minor,     // Some features disabled
    Moderate,  // Significant features disabled
    Severe,    // Only essential features
    Disabled   // Emergency mode only
}
```

### IHealthCheckResult

Result of a health check execution.

```csharp
public interface IHealthCheckResult
{
    FixedString64Bytes Name { get; }
    HealthStatus Status { get; }
    string Message { get; }
    string Description { get; }
    TimeSpan Duration { get; }
    DateTime Timestamp { get; }
    Exception Exception { get; }
    Dictionary<string, object> Data { get; }
    
    // Helper methods
    bool IsHealthy { get; }
    bool IsDegraded { get; }
    bool IsUnhealthy { get; }
}
```

### IHealthReport

Comprehensive health report containing multiple check results.

```csharp
public interface IHealthReport
{
    HealthStatus OverallStatus { get; }
    TimeSpan TotalDuration { get; }
    DateTime Timestamp { get; }
    DegradationLevel CurrentDegradationLevel { get; }
    
    // Results
    IReadOnlyDictionary<FixedString64Bytes, HealthCheckResult> Results { get; }
    IEnumerable<HealthCheckResult> HealthyChecks { get; }
    IEnumerable<HealthCheckResult> DegradedChecks { get; }
    IEnumerable<HealthCheckResult> UnhealthyChecks { get; }
    
    // Statistics
    int TotalChecks { get; }
    int HealthyCount { get; }
    int DegradedCount { get; }
    int UnhealthyCount { get; }
    
    // Filtering
    IEnumerable<HealthCheckResult> GetChecksByCategory(HealthCheckCategory category);
    IEnumerable<HealthCheckResult> GetChecksByStatus(HealthStatus status);
    
    // Dependency analysis
    IEnumerable<FixedString64Bytes> GetFailedDependencies();
    bool HasCriticalFailures();
}
```

### ICircuitBreaker

Interface for circuit breaker functionality.

```csharp
public interface ICircuitBreaker
{
    FixedString64Bytes Name { get; }
    CircuitBreakerState State { get; }
    CircuitBreakerConfig Configuration { get; }
    
    // Execution
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    
    // State management
    void ForceOpen(string reason);
    void ForceClose();
    void Reset();
    
    // Statistics
    CircuitBreakerStatistics GetStatistics();
    
    // Events
    event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;
    event EventHandler<CircuitBreakerExecutionEventArgs> ExecutionCompleted;
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
```

## ⚙️ Configuration

### Basic Configuration

```csharp
var config = new HealthCheckServiceConfigBuilder()
    .WithDefaultHealthCheckInterval(TimeSpan.FromMinutes(1))
    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
    .WithAutomaticChecks(enabled: true)
    .WithHistoryRetention(TimeSpan.FromHours(24))
    .WithCircuitBreakers(enabled: true)
    .WithGracefulDegradation(enabled: true)
    .Build();
```

### Advanced Configuration with Circuit Breakers

```csharp
var config = new HealthCheckServiceConfigBuilder()
    .WithDefaultHealthCheckInterval(TimeSpan.FromMinutes(5))
    .WithDefaultTimeout(TimeSpan.FromSeconds(10))
    .WithScheduling(builder => builder
        .WithScheduler<IntervalScheduler>()
        .WithConcurrentChecks(maxConcurrency: 10)
        .WithFailureRetry(maxRetries: 3, backoff: TimeSpan.FromSeconds(5)))
    .WithCircuitBreakers(builder => builder
        .WithDefaultFailureThreshold(5)
        .WithDefaultOpenTimeout(TimeSpan.FromSeconds(30))
        .WithDefaultSuccessThreshold(2)
        .WithAutoHealthCheckIntegration(true))
    .WithGracefulDegradation(builder => builder
        .WithThresholds(minor: 0.10, moderate: 0.25, severe: 0.50, disable: 0.75)
        .WithAutoFeatureDisabling(true)
        .WithRecoveryHysteresis(TimeSpan.FromMinutes(5)))
    .WithHistory(builder => builder
        .WithRetention(TimeSpan.FromDays(7))
        .WithMaxEntries(10000)
        .WithCompression(enabled: true))
    .WithAlerting(builder => builder
        .WithAlertOnStatusChange(true)
        .WithAlertOnDegradation(true)
        .WithAlertThrottling(TimeSpan.FromMinutes(5)))
    .Build();
```

### Unity Integration

```csharp
[CreateAssetMenu(menuName = "AhBear/HealthCheck/Config")]
public class HealthCheckConfigAsset : ScriptableObject
{
    [Header("General")]
    public bool enableAutomaticChecks = true;
    public float defaultCheckIntervalSeconds = 60f;
    public float defaultTimeoutSeconds = 30f;
    
    [Header("Circuit Breakers")]
    public bool enableCircuitBreakers = true;
    public int defaultFailureThreshold = 5;
    public float defaultOpenTimeoutSeconds = 30f;
    public int defaultSuccessThreshold = 2;
    
    [Header("Graceful Degradation")]
    public bool enableGracefulDegradation = true;
    [Range(0f, 1f)] public float minorDegradationThreshold = 0.10f;
    [Range(0f, 1f)] public float moderateDegradationThreshold = 0.25f;
    [Range(0f, 1f)] public float severeDegradationThreshold = 0.50f;
    [Range(0f, 1f)] public float disableDegradationThreshold = 0.75f;
    
    [Header("History")]
    public bool enableHistory = true;
    public float historyRetentionHours = 24f;
    public int maxHistoryEntries = 1000;
    
    [Header("Performance")]
    public int maxConcurrentChecks = 5;
    public bool enableAsyncExecution = true;
    public bool enablePerformanceMonitoring = true;
}
```

## 📦 Installation

### 1. Package Installation

```csharp
// In Package Manager, add:
"com.ahbearstudios.core.healthcheck": "2.0.0"
```

### 2. Reflex Bootstrap Installation

```csharp
/// <summary>
/// Reflex installer for the HealthCheck System following AhBearStudios Core Development Guidelines.
/// Provides comprehensive health monitoring with circuit breaker protection and graceful degradation.
/// </summary>
public class HealthCheckInstaller : IBootstrapInstaller
{
    public string InstallerName => "HealthCheckInstaller";
    public int Priority => 300; // After Logging (100), Messaging (150), Alerts (200)
    public bool IsEnabled => true;
    public Type[] Dependencies => new[] { typeof(LoggingInstaller), typeof(MessagingInstaller), typeof(AlertsInstaller) };

    public bool ValidateInstaller()
    {
        // Validate required dependencies
        if (!Container.HasBinding<ILoggingService>())
        {
            Debug.LogError("HealthCheckInstaller: ILoggingService not registered");
            return false;
        }

        if (!Container.HasBinding<IMessageBusService>())
        {
            Debug.LogError("HealthCheckInstaller: IMessageBusService not registered");
            return false;
        }

        if (!Container.HasBinding<IAlertService>())
        {
            Debug.LogError("HealthCheckInstaller: IAlertService not registered");
            return false;
        }

        return true;
    }

    public void PreInstall()
    {
        Debug.Log("HealthCheckInstaller: Beginning pre-installation validation");
    }

    public void Install(ContainerBuilder builder)
    {
        // Configure health check service with builder pattern
        var config = new HealthCheckServiceConfigBuilder()
            .WithDefaultHealthCheckInterval(TimeSpan.FromMinutes(1))
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithAutomaticChecks(enabled: true)
            .WithHistoryRetention(TimeSpan.FromHours(24))
            .WithCircuitBreakers(enabled: true)
            .WithGracefulDegradation(enabled: true)
            .WithPerformanceMonitoring(enabled: true)
            .Build();

        // Bind configuration
        builder.Bind<HealthCheckServiceConfig>().FromInstance(config);
        
        // Bind core services using Reflex patterns
        builder.Bind<IHealthCheckService>().To<HealthCheckService>().AsSingle();
        builder.Bind<IHealthCheckFactory>().To<HealthCheckFactory>().AsSingle();
        builder.Bind<ICircuitBreakerFactory>().To<CircuitBreakerFactory>().AsSingle();
        
        // Bind specialized services
        builder.Bind<HealthAggregationService>().To<HealthAggregationService>().AsSingle();
        builder.Bind<HealthHistoryService>().To<HealthHistoryService>().AsSingle();
        builder.Bind<HealthSchedulingService>().To<HealthSchedulingService>().AsSingle();
        builder.Bind<DegradationManagementService>().To<DegradationManagementService>().AsSingle();
        builder.Bind<CircuitBreakerManager>().To<CircuitBreakerManager>().AsSingle();
        
        // Bind schedulers
        builder.Bind<IHealthCheckScheduler>().WithId("Interval").To<IntervalScheduler>().AsSingle();
        builder.Bind<IHealthCheckScheduler>().WithId("Cron").To<CronScheduler>().AsSingle();
        builder.Bind<IHealthCheckScheduler>().WithId("Adaptive").To<AdaptiveScheduler>().AsSingle();
        
        // Bind default health checks
        builder.Bind<SystemHealthCheck>().To<SystemHealthCheck>().AsSingle();
        builder.Bind<NetworkHealthCheck>().To<NetworkHealthCheck>().AsSingle();
        builder.Bind<PerformanceHealthCheck>().To<PerformanceHealthCheck>().AsSingle();
    }

    public void PostInstall()
    {
        try
        {
            var healthService = Container.Resolve<IHealthCheckService>();
            var logger = Container.Resolve<ILoggingService>();

            // Register default health checks
            var systemHealthCheck = Container.Resolve<SystemHealthCheck>();
            var networkHealthCheck = Container.Resolve<NetworkHealthCheck>();
            var performanceHealthCheck = Container.Resolve<PerformanceHealthCheck>();

            healthService.RegisterHealthCheck(systemHealthCheck);
            healthService.RegisterHealthCheck(networkHealthCheck);
            healthService.RegisterHealthCheck(performanceHealthCheck);

            // Configure circuit breakers for critical checks
            healthService.EnableCircuitBreaker(systemHealthCheck.Name, new CircuitBreakerConfig
            {
                FailureThreshold = 3,
                OpenTimeout = TimeSpan.FromMinutes(1),
                SuccessThreshold = 2
            });

            // Start automatic health checks
            healthService.StartAutomaticChecks();

            logger.LogInfo("HealthCheckInstaller: Post-installation completed successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"HealthCheckInstaller: Post-installation failed: {ex.Message}");
            throw;
        }
    }
}
```

## 🚀 Usage Examples

### Basic Health Check Implementation

```csharp
/// <summary>
/// Database health check implementation demonstrating modern C# patterns and comprehensive monitoring.
/// Follows AhBearStudios Core Development Guidelines with proper error handling and correlation tracking.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseService _database;
    private readonly ILoggingService _logger;
    private readonly IProfilerService _profiler;
    private readonly FixedString64Bytes _correlationId;
    
    public FixedString64Bytes Name => "Database";
    public string Description => "Monitors database connectivity and performance";
    public HealthCheckCategory Category => HealthCheckCategory.Database;
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
    
    /// <summary>
    /// Initializes the database health check with required dependencies.
    /// </summary>
    /// <param name="database">Database service to monitor</param>
    /// <param name="logger">Logging service for health check operations</param>
    /// <param name="profiler">Optional profiler service for performance monitoring</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public DatabaseHealthCheck(
        IDatabaseService database, 
        ILoggingService logger, 
        IProfilerService profiler = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiler = profiler;
        _correlationId = $"DbHealthCheck_{Guid.NewGuid():N}"[..32];
        
        Configuration = new HealthCheckConfiguration
        {
            Timeout = Timeout,
            Interval = TimeSpan.FromMinutes(1),
            IsEnabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                BackoffStrategy = BackoffStrategy.Exponential,
                BaseDelay = TimeSpan.FromSeconds(1)
            }
        };
    }
    
    /// <summary>
    /// Performs comprehensive database health assessment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Health check result with detailed database status</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _profiler?.BeginScope("DatabaseHealthCheck.CheckHealthAsync");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInfo($"[{_correlationId}] Starting database health check");
            
            var data = new Dictionary<string, object>
            {
                ["CorrelationId"] = _correlationId.ToString(),
                ["CheckStartTime"] = DateTime.UtcNow
            };
            
            // Test basic connectivity with modern C# pattern matching
            var connectionResult = await TestConnectionAsync(cancellationToken);
            data["ConnectionTest"] = connectionResult.IsSuccess;
            data["ConnectionTime"] = connectionResult.Duration.TotalMilliseconds;
            
            if (!connectionResult.IsSuccess)
            {
                return CreateResult(HealthStatus.Unhealthy, 
                    $"Database connection failed: {connectionResult.Error}", 
                    stopwatch.Elapsed, data, connectionResult.Exception);
            }
            
            // Test query performance
            var queryResult = await TestQueryPerformanceAsync(cancellationToken);
            data["QueryTest"] = queryResult.IsSuccess;
            data["QueryTime"] = queryResult.Duration.TotalMilliseconds;
            data["RecordCount"] = queryResult.RecordCount;
            
            // Determine health status using switch expression
            var status = (connectionResult, queryResult) switch
            {
                ({ IsSuccess: true }, { IsSuccess: true, Duration.TotalMilliseconds: < 1000 }) => HealthStatus.Healthy,
                ({ IsSuccess: true }, { IsSuccess: true, Duration.TotalMilliseconds: < 5000 }) => HealthStatus.Degraded,
                ({ IsSuccess: true }, { IsSuccess: true }) => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };
            
            var message = status switch
            {
                HealthStatus.Healthy => "Database operating normally",
                HealthStatus.Degraded => $"Database responding slowly (query: {queryResult.Duration.TotalMilliseconds:F0}ms)",
                _ => "Database health check failed"
            };
            
            _logger.LogInfo($"[{_correlationId}] Database health check completed: {status}");
            return CreateResult(status, message, stopwatch.Elapsed, data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning($"[{_correlationId}] Database health check cancelled");
            return CreateResult(HealthStatus.Unknown, "Health check was cancelled", 
                stopwatch.Elapsed, new Dictionary<string, object> 
                { 
                    ["CorrelationId"] = _correlationId.ToString(),
                    ["Cancelled"] = true 
                });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Database health check failed");
            
            return CreateResult(HealthStatus.Unhealthy, 
                $"Database health check failed: {ex.Message}",
                stopwatch.Elapsed, 
                new Dictionary<string, object> 
                { 
                    ["CorrelationId"] = _correlationId.ToString(),
                    ["ExceptionType"] = ex.GetType().Name 
                }, 
                ex);
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["ServiceType"] = _database.GetType().Name,
            ["SupportedOperations"] = new[] { "Connect", "Query", "TestConnection" },
            ["CircuitBreakerEnabled"] = true,
            ["MonitoringCapabilities"] = new[] { "Connectivity", "Performance", "RecordCount" },
            ["Dependencies"] = Dependencies.ToArray()
        };
    }
    
    private async Task<TestResult> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var isConnected = await _database.TestConnectionAsync(cancellationToken);
            return new TestResult(isConnected, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            return new TestResult(false, stopwatch.Elapsed, ex.Message, ex);
        }
    }
    
    private async Task<QueryTestResult> TestQueryPerformanceAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var recordCount = await _database.GetRecordCountAsync("health_check_table", cancellationToken);
            return new QueryTestResult(true, stopwatch.Elapsed, recordCount);
        }
        catch (Exception ex)
        {
            return new QueryTestResult(false, stopwatch.Elapsed, 0, ex.Message, ex);
        }
    }
    
    private static HealthCheckResult CreateResult(
        HealthStatus status, 
        string message, 
        TimeSpan duration, 
        Dictionary<string, object> data, 
        Exception exception = null)
    {
        return new HealthCheckResult
        {
            Name = "Database",
            Status = status,
            Message = message,
            Description = "Monitors database connectivity and performance",
            Duration = duration,
            Timestamp = DateTime.UtcNow,
            Data = data,
            Exception = exception
        };
    }
}

/// <summary>
/// Result of a health check test operation.
/// </summary>
public sealed record TestResult(
    bool IsSuccess, 
    TimeSpan Duration, 
    string Error = null, 
    Exception Exception = null);

/// <summary>
/// Result of a database query test operation.
/// </summary>
public sealed record QueryTestResult(
    bool IsSuccess, 
    TimeSpan Duration, 
    int RecordCount, 
    string Error = null, 
    Exception Exception = null) : TestResult(IsSuccess, Duration, Error, Exception);
```

### Health Status Monitoring

```csharp
/// <summary>
/// Unity component for monitoring and displaying health status.
/// Demonstrates integration with the health check system and modern C# patterns.
/// </summary>
public class HealthStatusMonitor : MonoBehaviour
{
    private IHealthCheckService _healthCheckService;
    private ILoggingService _logger;
    private IAlertService _alertService;
    private readonly FixedString64Bytes _correlationId;
    
    [Header("Monitoring Settings")]
    [SerializeField] private bool _enableVisualIndicators = true;
    [SerializeField] private bool _enableAudioAlerts = false;
    [SerializeField] private HealthStatusDisplay _statusDisplay;
    
    [Header("Alert Thresholds")]
    [SerializeField] private float _degradedAlertThreshold = 0.3f; // 30% unhealthy
    [SerializeField] private float _criticalAlertThreshold = 0.5f; // 50% unhealthy
    
    public HealthStatusMonitor()
    {
        _correlationId = $"HealthMonitor_{Guid.NewGuid():N}"[..32];
    }
    
    private void Start()
    {
        // Resolve dependencies via Reflex
        _healthCheckService = Container.Resolve<IHealthCheckService>();
        _logger = Container.Resolve<ILoggingService>();
        _alertService = Container.Resolve<IAlertService>();
        
        // Subscribe to health events
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.CircuitBreakerStateChanged += OnCircuitBreakerStateChanged;
        _healthCheckService.DegradationStatusChanged += OnDegradationStatusChanged;
        _healthCheckService.HealthCheckExecuted += OnHealthCheckExecuted;
        
        // Start automatic monitoring
        _healthCheckService.StartAutomaticChecks();
        
        _logger.LogInfo($"[{_correlationId}] Health status monitor initialized");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_healthCheckService != null)
        {
            _healthCheckService.HealthStatusChanged -= OnHealthStatusChanged;
            _healthCheckService.CircuitBreakerStateChanged -= OnCircuitBreakerStateChanged;
            _healthCheckService.DegradationStatusChanged -= OnDegradationStatusChanged;
            _healthCheckService.HealthCheckExecuted -= OnHealthCheckExecuted;
        }
        
        _logger?.LogInfo($"[{_correlationId}] Health status monitor destroyed");
    }
    
    private void OnHealthStatusChanged(object sender, HealthStatusChangedEventArgs e)
    {
        _logger.LogInfo($"[{_correlationId}] Health status changed: {e.OldStatus} -> {e.NewStatus} ({e.Reason})");
        
        // Update visual indicators
        if (_enableVisualIndicators && _statusDisplay != null)
        {
            _statusDisplay.UpdateStatus(e.NewStatus, e.Reason);
        }
        
        // Trigger alerts based on status
        var alertSeverity = e.NewStatus switch
        {
            HealthStatus.Unhealthy => AlertSeverity.Critical,
            HealthStatus.Degraded => AlertSeverity.Warning,
            HealthStatus.Unknown => AlertSeverity.Warning,
            _ => (AlertSeverity?)null
        };
        
        if (alertSeverity.HasValue)
        {
            _alertService.RaiseAlert(
                $"System health status changed to {e.NewStatus}: {e.Reason}",
                alertSeverity.Value,
                "HealthStatusMonitor",
                "HealthStatusChange"
            );
        }
        
        // Check for system-wide health degradation
        CheckSystemDegradation();
    }
    
    private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
    {
        _logger.LogInfo($"[{_correlationId}] Circuit breaker '{e.Name}' state changed: {e.OldState} -> {e.NewState}");
        
        // Alert on circuit breaker state changes
        if (e.NewState == CircuitBreakerState.Open)
        {
            _alertService.RaiseAlert(
                $"Circuit breaker '{e.Name}' opened: {e.Reason}",
                AlertSeverity.Critical,
                "HealthStatusMonitor",
                "CircuitBreakerOpen"
            );
        }
        else if (e.NewState == CircuitBreakerState.Closed && e.OldState == CircuitBreakerState.Open)
        {
            _alertService.RaiseAlert(
                $"Circuit breaker '{e.Name}' closed: System recovered",
                AlertSeverity.Info,
                "HealthStatusMonitor",
                "CircuitBreakerClosed"
            );
        }
    }
    
    private void OnDegradationStatusChanged(object sender, DegradationStatusChangedEventArgs e)
    {
        _logger.LogInfo($"[{_correlationId}] Degradation level changed: {e.OldLevel} -> {e.NewLevel} ({e.Reason})");
        
        // Alert on degradation changes
        var alertSeverity = e.NewLevel switch
        {
            DegradationLevel.Severe or DegradationLevel.Disabled => AlertSeverity.Critical,
            DegradationLevel.Moderate => AlertSeverity.Warning,
            DegradationLevel.Minor => AlertSeverity.Info,
            _ => (AlertSeverity?)null
        };
        
        if (alertSeverity.HasValue)
        {
            _alertService.RaiseAlert(
                $"System degradation level changed to {e.NewLevel}: {e.Reason}",
                alertSeverity.Value,
                "HealthStatusMonitor",
                "DegradationChange"
            );
        }
        
        // Update UI based on degradation level
        if (_enableVisualIndicators && _statusDisplay != null)
        {
            _statusDisplay.UpdateDegradationLevel(e.NewLevel, e.Reason);
        }
    }
    
    private void OnHealthCheckExecuted(object sender, HealthCheckExecutedEventArgs e)
    {
        // Log detailed health check results for debugging
        _logger.LogInfo($"[{_correlationId}] Health check '{e.Result.Name}' completed: " +
                       $"{e.Result.Status} in {e.Result.Duration.TotalMilliseconds:F1}ms");
        
        // Track performance issues
        if (e.Result.Duration > TimeSpan.FromSeconds(10))
        {
            _alertService.RaiseAlert(
                $"Health check '{e.Result.Name}' is running slowly: {e.Result.Duration.TotalSeconds:F1}s",
                AlertSeverity.Warning,
                "HealthStatusMonitor",
                "SlowHealthCheck"
            );
        }
    }
    
    private async void CheckSystemDegradation()
    {
        try
        {
            var report = await _healthCheckService.CheckAllHealthAsync();
            var unhealthyRatio = (double)report.UnhealthyCount / report.TotalChecks;
            
            if (unhealthyRatio >= _criticalAlertThreshold)
            {
                _alertService.RaiseAlert(
                    $"Critical system degradation: {report.UnhealthyCount}/{report.TotalChecks} health checks failing",
                    AlertSeverity.Critical,
                    "HealthStatusMonitor",
                    "SystemDegradation"
                );
            }
            else if (unhealthyRatio >= _degradedAlertThreshold)
            {
                _alertService.RaiseAlert(
                    $"System degradation detected: {report.UnhealthyCount}/{report.TotalChecks} health checks failing",
                    AlertSeverity.Warning,
                    "HealthStatusMonitor",
                    "SystemDegradation"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Failed to check system degradation");
        }
    }
    
    /// <summary>
    /// Manually triggers a comprehensive health check for debugging purposes.
    /// </summary>
    [ContextMenu("Run Health Check")]
    public async void RunHealthCheckManually()
    {
        try
        {
            _logger.LogInfo($"[{_correlationId}] Running manual health check");
            var report = await _healthCheckService.CheckAllHealthAsync();
            
            var summary = $"Health Check Results:\n" +
                         $"Overall Status: {report.OverallStatus}\n" +
                         $"Total Checks: {report.TotalChecks}\n" +
                         $"Healthy: {report.HealthyCount}\n" +
                         $"Degraded: {report.DegradedCount}\n" +
                         $"Unhealthy: {report.UnhealthyCount}\n" +
                         $"Duration: {report.TotalDuration.TotalMilliseconds:F1}ms";
            
            _logger.LogInfo($"[{_correlationId}] {summary}");
            Debug.Log(summary);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Manual health check failed");
        }
    }
}
```

### Circuit Breaker Integration

```csharp
/// <summary>
/// Service demonstrating circuit breaker integration with health checks.
/// Provides automatic protection against cascading failures.
/// </summary>
public class ResilientDatabaseService : IDatabaseService
{
    private readonly IDatabaseService _innerService;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly FixedString64Bytes _correlationId;
    
    /// <summary>
    /// Initializes the resilient database service with circuit breaker protection.
    /// </summary>
    /// <param name="innerService">The underlying database service</param>
    /// <param name="circuitBreakerFactory">Factory for creating circuit breakers</param>
    /// <param name="logger">Logging service for operation tracking</param>
    /// <param name="alertService">Alert service for failure notifications</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public ResilientDatabaseService(
        IDatabaseService innerService,
        ICircuitBreakerFactory circuitBreakerFactory,
        ILoggingService logger,
        IAlertService alertService)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _correlationId = $"ResilientDbService_{Guid.NewGuid():N}"[..32];
        
        // Create circuit breaker with custom configuration
        var config = new CircuitBreakerConfig
        {
            Name = "DatabaseService",
            FailureThreshold = 5,
            OpenTimeout = TimeSpan.FromMinutes(2),
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _circuitBreaker = circuitBreakerFactory.CreateCircuitBreaker(config);
        _circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
    }
    
    /// <summary>
    /// Tests database connectivity with circuit breaker protection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if connection test succeeds, false otherwise</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                _logger.LogInfo($"[{_correlationId}] Testing database connection");
                var result = await _innerService.TestConnectionAsync(cancellationToken);
                _logger.LogInfo($"[{_correlationId}] Database connection test result: {result}");
                return result;
            }, cancellationToken);
        }
        catch (CircuitBreakerOpenException)
        {
            _logger.LogWarning($"[{_correlationId}] Database connection test blocked by circuit breaker");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Database connection test failed");
            return false;
        }
    }
    
    /// <summary>
    /// Gets record count with circuit breaker protection and fallback mechanism.
    /// </summary>
    /// <param name="tableName">Name of the table to query</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Record count or estimated value if circuit breaker is open</returns>
    public async Task<int> GetRecordCountAsync(string tableName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                _logger.LogInfo($"[{_correlationId}] Getting record count for table: {tableName}");
                var count = await _innerService.GetRecordCountAsync(tableName, cancellationToken);
                _logger.LogInfo($"[{_correlationId}] Record count for {tableName}: {count}");
                return count;
            }, cancellationToken);
        }
        catch (CircuitBreakerOpenException)
        {
            _logger.LogWarning($"[{_correlationId}] Record count query blocked by circuit breaker, returning fallback value");
            
            // Return cached or estimated value when circuit breaker is open
            return GetFallbackRecordCount(tableName);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Record count query failed");
            throw;
        }
    }
    
    private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
    {
        _logger.LogInfo($"[{_correlationId}] Circuit breaker state changed: {e.OldState} -> {e.NewState}");
        
        // Generate alerts for circuit breaker state changes
        var alertSeverity = e.NewState switch
        {
            CircuitBreakerState.Open => AlertSeverity.Critical,
            CircuitBreakerState.HalfOpen => AlertSeverity.Warning,
            CircuitBreakerState.Closed when e.OldState == CircuitBreakerState.Open => AlertSeverity.Info,
            _ => (AlertSeverity?)null
        };
        
        if (alertSeverity.HasValue)
        {
            _alertService.RaiseAlert(
                $"Database circuit breaker {e.NewState}: {e.Reason}",
                alertSeverity.Value,
                "ResilientDatabaseService",
                "CircuitBreakerStateChange"
            );
        }
    }
    
    private int GetFallbackRecordCount(string tableName)
    {
        // Return cached or estimated values based on table name
        return tableName switch
        {
            "users" => 1000,
            "products" => 500,
            "orders" => 2000,
            _ => 100 // Default fallback
        };
    }
    
    public void Dispose()
    {
        _circuitBreaker?.Dispose();
        _innerService?.Dispose();
    }
}
```

## 🏥 Health Check Examples

### System Performance Health Check

```csharp
/// <summary>
/// Performance health check monitoring system resources and frame rates.
/// Integrates with profiler service for comprehensive performance monitoring.
/// </summary>
public class PerformanceHealthCheck : IHealthCheck
{
    private readonly IProfilerService _profiler;
    private readonly ILoggingService _logger;
    private readonly FixedString64Bytes _correlationId;
    
    public FixedString64Bytes Name => "Performance";
    public string Description => "Monitors system performance metrics and frame rates";
    public HealthCheckCategory Category => HealthCheckCategory.Performance;
    public TimeSpan Timeout => TimeSpan.FromSeconds(15);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
    
    public PerformanceHealthCheck(IProfilerService profiler, ILoggingService logger)
    {
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationId = $"PerfHealthCheck_{Guid.NewGuid():N}"[..32];
        
        Configuration = new HealthCheckConfiguration
        {
            Timeout = Timeout,
            Interval = TimeSpan.FromMinutes(2),
            IsEnabled = true
        };
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _profiler.BeginScope("PerformanceHealthCheck.CheckHealthAsync");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInfo($"[{_correlationId}] Starting performance health check");
            
            // Gather performance metrics
            var metrics = await GatherPerformanceMetricsAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["CorrelationId"] = _correlationId.ToString(),
                ["CpuUsage"] = metrics.CpuUsage,
                ["MemoryUsage"] = metrics.MemoryUsage,
                ["FrameRate"] = metrics.FrameRate,
                ["GcCollections"] = metrics.GcCollections,
                ["ManagedMemory"] = metrics.ManagedMemory,
                ["UnmanagedMemory"] = metrics.UnmanagedMemory
            };
            
            // Evaluate performance status using modern C# patterns
            var status = EvaluatePerformanceStatus(metrics);
            var message = GenerateStatusMessage(status, metrics);
            
            _logger.LogInfo($"[{_correlationId}] Performance health check completed: {status}");
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = status,
                Message = message,
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Performance health check failed");
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Message = $"Performance check failed: {ex.Message}",
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Exception = ex
            };
        }
    }
    
    private async Task<PerformanceMetrics> GatherPerformanceMetricsAsync(CancellationToken cancellationToken)
    {
        // Simulate gathering performance metrics
        await Task.Delay(100, cancellationToken);
        
        return new PerformanceMetrics
        {
            CpuUsage = GetCpuUsage(),
            MemoryUsage = GetMemoryUsage(),
            FrameRate = Application.targetFrameRate > 0 ? Time.frameCount / Time.time : 60f,
            GcCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            ManagedMemory = GC.GetTotalMemory(false),
            UnmanagedMemory = Profiler.GetMonoUsedSize()
        };
    }
    
    private static HealthStatus EvaluatePerformanceStatus(PerformanceMetrics metrics)
    {
        // Use pattern matching for complex status evaluation
        return (metrics.CpuUsage, metrics.MemoryUsage, metrics.FrameRate) switch
        {
            // Critical thresholds
            (> 95, _, _) or (_, > 95, _) or (_, _, < 15) => HealthStatus.Unhealthy,
            
            // Warning thresholds
            (> 80, _, _) or (_, > 80, _) or (_, _, < 30) => HealthStatus.Degraded,
            
            // Healthy range
            _ => HealthStatus.Healthy
        };
    }
    
    private static string GenerateStatusMessage(HealthStatus status, PerformanceMetrics metrics)
    {
        return status switch
        {
            HealthStatus.Healthy => "Performance within normal parameters",
            HealthStatus.Degraded => $"Performance degraded: CPU {metrics.CpuUsage:F1}%, Memory {metrics.MemoryUsage:F1}%, FPS {metrics.FrameRate:F1}",
            HealthStatus.Unhealthy => $"Critical performance issues: CPU {metrics.CpuUsage:F1}%, Memory {metrics.MemoryUsage:F1}%, FPS {metrics.FrameRate:F1}",
            _ => "Performance status unknown"
        };
    }
    
    private static float GetCpuUsage()
    {
        // Platform-specific CPU usage implementation
        #if UNITY_EDITOR
        return UnityEngine.Random.Range(10f, 30f); // Mock data for editor
        #else
        // Real implementation would use platform-specific APIs
        return 25f;
        #endif
    }
    
    private static float GetMemoryUsage()
    {
        var totalMemory = SystemInfo.systemMemorySize * 1024L * 1024L; // Convert MB to bytes
        var usedMemory = GC.GetTotalMemory(false) + Profiler.GetMonoUsedSize();
        return (float)(usedMemory / (double)totalMemory * 100);
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["MonitoredMetrics"] = new[] { "CPU", "Memory", "FrameRate", "GC", "ManagedMemory" },
            ["Platform"] = Application.platform.ToString(),
            ["GraphicsMemory"] = SystemInfo.graphicsMemorySize,
            ["ProcessorCount"] = SystemInfo.processorCount,
            ["SystemMemory"] = SystemInfo.systemMemorySize
        };
    }
}

/// <summary>
/// Container for performance metrics data.
/// </summary>
public sealed record PerformanceMetrics
{
    public float CpuUsage { get; init; }
    public float MemoryUsage { get; init; }
    public float FrameRate { get; init; }
    public int GcCollections { get; init; }
    public long ManagedMemory { get; init; }
    public long UnmanagedMemory { get; init; }
}
```

### Network Connectivity Health Check

```csharp
/// <summary>
/// Network health check monitoring connectivity to critical services.
/// Supports multiple endpoints and provides detailed connectivity diagnostics.
/// </summary>
public class NetworkHealthCheck : IHealthCheck
{
    private readonly ILoggingService _logger;
    private readonly HttpClient _httpClient;
    private readonly FixedString64Bytes _correlationId;
    private readonly List<NetworkEndpoint> _endpoints;
    
    public FixedString64Bytes Name => "Network";
    public string Description => "Monitors network connectivity to critical services";
    public HealthCheckCategory Category => HealthCheckCategory.Network;
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
    
    public NetworkHealthCheck(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationId = $"NetworkHealthCheck_{Guid.NewGuid():N}"[..32];
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        // Configure critical endpoints to monitor
        _endpoints = new List<NetworkEndpoint>
        {
            new("API Gateway", "https://api.ahbearstudios.com/health", NetworkEndpointType.Critical),
            new("CDN", "https://cdn.ahbearstudios.com/ping", NetworkEndpointType.Important),
            new("Analytics", "https://analytics.ahbearstudios.com/status", NetworkEndpointType.Optional)
        };
        
        Configuration = new HealthCheckConfiguration
        {
            Timeout = Timeout,
            Interval = TimeSpan.FromMinutes(3),
            IsEnabled = true
        };
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInfo($"[{_correlationId}] Starting network health check");
            
            var results = new List<EndpointResult>();
            var tasks = _endpoints.Select(endpoint => 
                CheckEndpointAsync(endpoint, cancellationToken)).ToArray();
            
            var endpointResults = await Task.WhenAll(tasks);
            results.AddRange(endpointResults);
            
            var data = new Dictionary<string, object>
            {
                ["CorrelationId"] = _correlationId.ToString(),
                ["EndpointCount"] = _endpoints.Count,
                ["SuccessfulEndpoints"] = results.Count(r => r.IsSuccessful),
                ["FailedEndpoints"] = results.Count(r => !r.IsSuccessful),
                ["AverageLatency"] = results.Where(r => r.IsSuccessful).Average(r => r.Latency.TotalMilliseconds),
                ["EndpointDetails"] = results.ToDictionary(r => r.Name, r => new 
                {
                    r.IsSuccessful,
                    Latency = r.Latency.TotalMilliseconds,
                    r.StatusCode,
                    r.Error
                })
            };
            
            // Evaluate network health based on endpoint types and results
            var status = EvaluateNetworkStatus(results);
            var message = GenerateNetworkStatusMessage(status, results);
            
            _logger.LogInfo($"[{_correlationId}] Network health check completed: {status}");
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = status,
                Message = message,
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Network health check failed");
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Message = $"Network check failed: {ex.Message}",
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Exception = ex
            };
        }
    }
    
    private async Task<EndpointResult> CheckEndpointAsync(NetworkEndpoint endpoint, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var response = await _httpClient.GetAsync(endpoint.Url, cancellationToken);
            
            return new EndpointResult
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Type = endpoint.Type,
                IsSuccessful = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Latency = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new EndpointResult
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Type = endpoint.Type,
                IsSuccessful = false,
                Latency = stopwatch.Elapsed,
                Error = ex.Message
            };
        }
    }
    
    private static HealthStatus EvaluateNetworkStatus(List<EndpointResult> results)
    {
        var criticalEndpoints = results.Where(r => r.Type == NetworkEndpointType.Critical).ToList();
        var importantEndpoints = results.Where(r => r.Type == NetworkEndpointType.Important).ToList();
        
        // Check critical endpoints first
        if (criticalEndpoints.Any() && criticalEndpoints.All(r => !r.IsSuccessful))
        {
            return HealthStatus.Unhealthy;
        }
        
        var failedCritical = criticalEndpoints.Count(r => !r.IsSuccessful);
        var failedImportant = importantEndpoints.Count(r => !r.IsSuccessful);
        
        return (failedCritical, failedImportant) switch
        {
            (> 0, _) => HealthStatus.Degraded,
            (0, > 1) => HealthStatus.Degraded,
            _ => HealthStatus.Healthy
        };
    }
    
    private static string GenerateNetworkStatusMessage(HealthStatus status, List<EndpointResult> results)
    {
        var successful = results.Count(r => r.IsSuccessful);
        var total = results.Count;
        var avgLatency = results.Where(r => r.IsSuccessful).DefaultIfEmpty().Average(r => r?.Latency.TotalMilliseconds ?? 0);
        
        return status switch
        {
            HealthStatus.Healthy => $"All network endpoints accessible ({successful}/{total}) - avg latency: {avgLatency:F0}ms",
            HealthStatus.Degraded => $"Some network issues detected ({successful}/{total}) - avg latency: {avgLatency:F0}ms",
            HealthStatus.Unhealthy => $"Critical network failures ({successful}/{total})",
            _ => "Network status unknown"
        };
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["EndpointCount"] = _endpoints.Count,
            ["CriticalEndpoints"] = _endpoints.Where(e => e.Type == NetworkEndpointType.Critical).Select(e => e.Name).ToArray(),
            ["ImportantEndpoints"] = _endpoints.Where(e => e.Type == NetworkEndpointType.Important).Select(e => e.Name).ToArray(),
            ["OptionalEndpoints"] = _endpoints.Where(e => e.Type == NetworkEndpointType.Optional).Select(e => e.Name).ToArray(),
            ["HttpClientTimeout"] = _httpClient.Timeout.TotalSeconds
        };
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Configuration for a network endpoint to monitor.
/// </summary>
public sealed record NetworkEndpoint(
    string Name, 
    string Url, 
    NetworkEndpointType Type);

/// <summary>
/// Result of checking a network endpoint.
/// </summary>
public sealed record EndpointResult
{
    public string Name { get; init; }
    public string Url { get; init; }
    public NetworkEndpointType Type { get; init; }
    public bool IsSuccessful { get; init; }
    public int StatusCode { get; init; }
    public TimeSpan Latency { get; init; }
    public string Error { get; init; }
}

/// <summary>
/// Importance level of network endpoints.
/// </summary>
public enum NetworkEndpointType
{
    Critical,   // System cannot function without these
    Important,  // System degraded without these
    Optional    // Nice to have, minimal impact
}
```

## 📊 Statistics and Monitoring

### Health Service Statistics

```csharp
/// <summary>
/// Comprehensive statistics for health check service performance and status.
/// Provides detailed metrics for system monitoring and optimization.
/// </summary>
public sealed record HealthServiceStatistics
{
    /// <summary>
    /// Gets the total number of health checks registered with the service.
    /// </summary>
    public int RegisteredHealthChecks { get; init; }
    
    /// <summary>
    /// Gets the total number of health checks executed since last reset.
    /// </summary>
    public long TotalHealthChecksExecuted { get; init; }
    
    /// <summary>
    /// Gets the total number of health checks that failed execution.
    /// </summary>
    public long TotalHealthChecksFailed { get; init; }
    
    /// <summary>
    /// Gets the total number of health checks that timed out.
    /// </summary>
    public long TotalHealthChecksTimedOut { get; init; }
    
    /// <summary>
    /// Gets the current overall health status of the system.
    /// </summary>
    public HealthStatus CurrentOverallStatus { get; init; }
    
    /// <summary>
    /// Gets the current system degradation level.
    /// </summary>
    public DegradationLevel CurrentDegradationLevel { get; init; }
    
    /// <summary>
    /// Gets the average health check execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; init; }
    
    /// <summary>
    /// Gets the timestamp when statistics were last reset.
    /// </summary>
    public DateTime LastStatsReset { get; init; }
    
    /// <summary>
    /// Gets the number of currently active circuit breakers.
    /// </summary>
    public int ActiveCircuitBreakers { get; init; }
    
    /// <summary>
    /// Gets the number of circuit breakers currently in open state.
    /// </summary>
    public int OpenCircuitBreakers { get; init; }
    
    /// <summary>
    /// Gets the current error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate => TotalHealthChecksExecuted > 0 ? 
        (double)TotalHealthChecksFailed / TotalHealthChecksExecuted : 0;
    
    /// <summary>
    /// Gets the current timeout rate (0.0 to 1.0).
    /// </summary>
    public double TimeoutRate => TotalHealthChecksExecuted > 0 ?
        (double)TotalHealthChecksTimedOut / TotalHealthChecksExecuted : 0;
    
    /// <summary>
    /// Gets statistics per health check category.
    /// </summary>
    public Dictionary<HealthCheckCategory, CategoryStatistics> CategoryStatistics { get; init; } = new();
    
    /// <summary>
    /// Gets statistics per individual health check.
    /// </summary>
    public Dictionary<FixedString64Bytes, IndividualHealthCheckStatistics> HealthCheckStatistics { get; init; } = new();
    
    /// <summary>
    /// Gets circuit breaker statistics.
    /// </summary>
    public Dictionary<FixedString64Bytes, CircuitBreakerStatistics> CircuitBreakerStatistics { get; init; } = new();
    
    /// <summary>
    /// Gets degradation event history.
    /// </summary>
    public List<DegradationEvent> DegradationHistory { get; init; } = new();
}

/// <summary>
/// Statistics for a specific health check category.
/// </summary>
public sealed record CategoryStatistics
{
    public HealthCheckCategory Category { get; init; }
    public int HealthCheckCount { get; init; }
    public long TotalExecutions { get; init; }
    public long TotalFailures { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public HealthStatus CurrentStatus { get; init; }
    public DateTime LastExecution { get; init; }
}

/// <summary>
/// Statistics for an individual health check.
/// </summary>
public sealed record IndividualHealthCheckStatistics
{
    public FixedString64Bytes Name { get; init; }
    public HealthCheckCategory Category { get; init; }
    public long TotalExecutions { get; init; }
    public long TotalFailures { get; init; }
    public long TotalTimeouts { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public TimeSpan LastExecutionTime { get; init; }
    public HealthStatus CurrentStatus { get; init; }
    public DateTime LastExecution { get; init; }
    public DateTime LastFailure { get; init; }
    public string LastFailureMessage { get; init; }
    public bool IsEnabled { get; init; }
    public bool HasCircuitBreaker { get; init; }
}

/// <summary>
/// Statistics for circuit breaker performance.
/// </summary>
public sealed record CircuitBreakerStatistics
{
    public FixedString64Bytes Name { get; init; }
    public CircuitBreakerState CurrentState { get; init; }
    public long TotalExecutions { get; init; }
    public long TotalFailures { get; init; }
    public long TotalSuccesses { get; init; }
    public long TotalTimeouts { get; init; }
    public int StateTransitions { get; init; }
    public DateTime LastStateChange { get; init; }
    public TimeSpan TotalOpenTime { get; init; }
    public TimeSpan CurrentStateDuration { get; init; }
    
    public double FailureRate => TotalExecutions > 0 ? 
        (double)TotalFailures / TotalExecutions : 0;
        
    public double SuccessRate => TotalExecutions > 0 ?
        (double)TotalSuccesses / TotalExecutions : 0;
}

/// <summary>
/// Event representing a system degradation occurrence.
/// </summary>
public sealed record DegradationEvent
{
    public DateTime Timestamp { get; init; }
    public DegradationLevel OldLevel { get; init; }
    public DegradationLevel NewLevel { get; init; }
    public string Reason { get; init; }
    public TimeSpan Duration { get; init; }
    public List<FixedString64Bytes> AffectedHealthChecks { get; init; } = new();
    public Dictionary<string, object> Context { get; init; } = new();
}
```

## 📚 Additional Resources

- [Health Check Design Patterns](HEALTHCHECK_PATTERNS.md)
- [Circuit Breaker Implementation Guide](HEALTHCHECK_CIRCUIT_BREAKERS.md)
- [Graceful Degradation Strategies](HEALTHCHECK_DEGRADATION.md)
- [Custom Health Check Development](HEALTHCHECK_CUSTOM.md)
- [Performance Optimization Guide](HEALTHCHECK_PERFORMANCE.md)
- [Integration Guide](HEALTHCHECK_INTEGRATION.md)
- [Troubleshooting Guide](HEALTHCHECK_TROUBLESHOOTING.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the HealthCheck System.

## 📄 Dependencies

- **Direct**: Logging, Messaging, Alerts
- **Dependents**: All systems requiring health monitoring
- **Optional**: Profiling (for performance monitoring), Pooling (for high-throughput scenarios)

---

*The HealthCheck System provides comprehensive health monitoring and system resilience capabilities across all AhBearStudios Core systems.*
    