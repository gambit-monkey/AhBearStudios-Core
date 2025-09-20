# HealthCheck System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.HealthChecking`
**Role:** System health monitoring, protection, and graceful degradation
**Status:** ✅ Production Ready

The HealthCheck System provides comprehensive health monitoring capabilities for all systems, enabling proactive issue detection, automated health reporting, system protection through circuit breakers, and graceful degradation patterns. This production-ready system serves as the central point for maintaining system resilience and operational health in Unity game development environments.

## 🚀 Key Features

- **⚡ Real-Time Monitoring**: Continuous health assessment of all registered systems
- **🛡️ Circuit Breaker Protection**: Production-ready circuit breaker implementation with fault tolerance
- **🔧 Flexible Health Checks**: Custom health check implementations using IHealthCheck interface
- **📊 Health Aggregation**: Comprehensive health reports with status aggregation
- **🎯 Automated Scheduling**: Configurable health check intervals with automatic execution
- **📈 Health History**: Historical health data tracking and statistical analysis
- **🔄 Message Bus Integration**: Event-driven architecture using IMessageBusService following CLAUDE.md patterns
- **🏥 Graceful Degradation**: Configurable degradation levels based on health status
- **⚙️ Service-Based Architecture**: Modular services for operations, events, resilience, and registry management
- **🎮 Unity-Optimized**: 60+ FPS performance targets with zero-allocation patterns using ZLinq and UniTask

## 🏗️ Architecture

### Production Architecture

The HealthCheck System follows the **Builder → Config → Factory → Service** pattern as specified in CLAUDE.md:

```
AhBearStudios.Core.HealthChecking/
├── IHealthCheckService.cs                # Primary service interface
├── ICircuitBreaker.cs                    # Circuit breaker interface
├── CircuitBreaker.cs                     # Production circuit breaker implementation
├── CircuitBreakerOpenException.cs        # Circuit breaker exception handling
├── HealthCheckRegistrationManager.cs     # Health check registration management
├── IDomainHealthCheckRegistrar.cs        # Domain-specific registration interface
├── Builders/
│   ├── IHealthCheckServiceConfigBuilder.cs # Service configuration builder interface
│   ├── HealthCheckServiceConfigBuilder.cs  # Main configuration builder
│   ├── IHealthCheckConfigBuilder.cs      # Individual check config builder interface
│   ├── HealthCheckConfigBuilder.cs       # Individual check configuration builder
│   ├── ICircuitBreakerConfigBuilder.cs   # Circuit breaker config builder interface
│   └── CircuitBreakerConfigBuilder.cs    # Circuit breaker configuration builder
├── Configs/
│   ├── HealthCheckServiceConfig.cs       # Main service configuration
│   ├── IHealthCheckServiceConfig.cs      # Service configuration interface
│   ├── HealthCheckConfiguration.cs       # Individual check configuration
│   ├── CircuitBreakerConfig.cs           # Circuit breaker settings
│   ├── ICircuitBreakerConfig.cs          # Circuit breaker config interface
│   ├── DegradationConfig.cs              # Graceful degradation configuration
│   ├── RetryConfig.cs                    # Retry policy configuration
│   ├── IRetryConfig.cs                   # Retry configuration interface
│   ├── RateLimitConfig.cs                # Rate limiting configuration
│   ├── IRateLimitConfig.cs               # Rate limit config interface
│   ├── PerformanceConfig.cs              # Performance monitoring configuration
│   ├── ISlowCallConfig.cs                # Slow call detection interface
│   ├── IFailoverConfig.cs                # Failover configuration interface
│   └── IBulkheadConfig.cs                # Bulkhead pattern configuration interface
├── Factories/
│   ├── IHealthCheckServiceFactory.cs     # Service factory interface
│   ├── HealthCheckServiceFactory.cs      # Production service factory
│   ├── IHealthCheckFactory.cs            # Health check creation interface
│   ├── HealthCheckFactory.cs             # Health check factory implementation
│   └── ICircuitBreakerFactory.cs         # Circuit breaker factory interface
├── Services/
│   ├── IHealthCheckOperationService.cs   # Operations service interface
│   ├── HealthCheckOperationService.cs    # Health check execution and scheduling
│   ├── IHealthCheckEventService.cs       # Event service interface
│   ├── HealthCheckEventService.cs        # Event handling and message publishing
│   ├── IHealthCheckResilienceService.cs  # Resilience service interface
│   ├── HealthCheckResilienceService.cs   # Circuit breakers and degradation
│   ├── IHealthCheckRegistryService.cs    # Registry service interface
│   └── HealthCheckRegistryService.cs     # Health check registration and metadata
├── Checks/
│   └── IHealthCheck.cs                   # Enhanced health check interface
├── Models/
│   ├── HealthStatus.cs                   # Enhanced status enumeration (6 levels)
│   ├── HealthCheckCategory.cs            # Category enumeration
│   ├── HealthCheckResult.cs              # Health check result
│   ├── IHealthCheckResult.cs             # Health check result interface
│   ├── HealthReport.cs                   # Comprehensive health report
│   ├── OverallHealthStatus.cs            # Overall system health status
│   ├── DegradationLevel.cs               # Degradation levels
│   ├── CircuitBreakerState.cs            # Circuit breaker states
│   ├── CircuitBreakerStatistics.cs       # Circuit breaker performance metrics
│   ├── HealthStatistics.cs               # System health statistics
│   ├── IndividualHealthCheckStatistics.cs # Individual check statistics
│   ├── PerformanceMetrics.cs             # Performance monitoring data
│   ├── HealthThresholds.cs               # Health threshold configuration
│   ├── DatabaseTestResult.cs             # Database test result model
│   ├── ExecutionType.cs                  # Health check execution types
│   ├── HealthCheckExecution.cs           # Execution tracking model
│   └── IValidatable.cs                   # Validation interface for configurations
├── Messages/
│   ├── HealthCheckStatusChangedMessage.cs        # Status change notifications
│   ├── HealthCheckCircuitBreakerStateChangedMessage.cs # Circuit breaker events
│   ├── HealthCheckDegradationChangeMessage.cs    # Degradation level changes
│   ├── HealthCheckStartedMessage.cs              # Health check start events
│   ├── HealthCheckCompletedMessage.cs            # Health check completion events
│   ├── HealthCheckCompletedWithResultsMessage.cs # Health check results
│   ├── HealthCheckServiceCreatedMessage.cs       # Service creation events
│   ├── HealthCheckServiceCreationFailedMessage.cs # Service creation failures
│   ├── HealthCheckCreatedMessage.cs              # Health check creation events
│   ├── HealthCheckAlertMessage.cs                # Alert integration messages
│   ├── CircuitBreakerCreatedMessage.cs           # Circuit breaker creation
│   └── (Additional message types for comprehensive event coverage)
├── Extensions/
│   └── HealthCheckServiceExtensions.cs   # Service extension methods
└── HealthChecks/
    └── HealthCheckServiceHealthCheck.cs  # Self-monitoring health check

AhBearStudios.Unity.HealthChecking/
├── Installers/
│   └── HealthCheckInstaller.cs           # Reflex dependency injection setup
├── Components/
│   ├── HealthMonitorComponent.cs         # Unity monitoring component
│   └── HealthDisplayComponent.cs         # Visual health display
└── ScriptableObjects/
    └── HealthCheckConfigAsset.cs         # Unity configuration assets
```

## 🔌 Key Interfaces

### IHealthCheckService

The primary interface for all health monitoring operations. All health status changes are published via IMessageBusService following CLAUDE.md patterns.

```csharp
public interface IHealthCheckService
{
    // Health check registration
    void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null);
    void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks);
    bool UnregisterHealthCheck(FixedString64Bytes name);

    // Health check execution using UniTask for Unity optimization
    UniTask<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default);
    UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default);

    // Overall health status
    UniTask<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default);
    DegradationLevel GetCurrentDegradationLevel();

    // Circuit breaker management
    CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName);
    Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates();
    void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason);
    void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason);

    // Graceful degradation
    void SetDegradationLevel(DegradationLevel level, string reason);

    // History and reporting
    List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100);

    // Health check management
    List<FixedString64Bytes> GetRegisteredHealthCheckNames();
    Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name);
    bool IsHealthCheckEnabled(FixedString64Bytes name);
    void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled);

    // Automatic monitoring
    void StartAutomaticChecks();
    void StopAutomaticChecks();
    bool IsAutomaticChecksRunning();

    // Statistics
    HealthStatistics GetHealthStatistics();
}
```

### IHealthCheck

Enhanced interface for implementing health checks with comprehensive monitoring capabilities.

```csharp
public interface IHealthCheck
{
    // Basic properties using high-performance FixedString
    FixedString64Bytes Name { get; }
    string Description { get; }
    HealthCheckCategory Category { get; }
    TimeSpan Timeout { get; }

    // Execution using UniTask for Unity optimization
    UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    // Configuration with enhanced settings
    HealthCheckConfiguration Configuration { get; }
    void Configure(HealthCheckConfiguration configuration);

    // Dependencies and metadata for introspection
    IEnumerable<FixedString64Bytes> Dependencies { get; }
    Dictionary<string, object> GetMetadata();
}

public enum HealthCheckCategory
{
    System,           // System-level health checks (CPU, memory, disk)
    Database,         // Database connectivity and performance checks
    Development,      // Development Logging Service Health Check
    Testing,          // Testing Logging Service Health Check
    Network,          // Network connectivity and latency checks
    Performance,      // Performance and throughput checks
    Security,         // Security-related health checks
    CircuitBreaker,   // Circuit breaker health checks
    Custom            // Custom application-specific health checks
}

public enum HealthStatus
{
    Unknown = 0,      // The health status is unknown
    Healthy = 1,      // The component is healthy
    Degraded = 2,     // The component is degraded but still functional
    Unhealthy = 3,    // The component is unhealthy
    Warning = 4,      // The component has a warning status
    Critical = 5,     // The component is in a critical state
    Offline = 6       // The component is offline
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

### Production Configuration

The system uses the **Builder → Config → Factory → Service** pattern as specified in CLAUDE.md. Configuration builders provide fluent APIs for setup:

```csharp
// Basic configuration using the production builder
var config = new HealthCheckServiceConfigBuilder()
    .WithAutomaticCheckInterval(TimeSpan.FromMinutes(1))
    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
    .WithMaxConcurrentHealthChecks(5)
    .WithAutomaticChecks(enabled: true)
    .WithMaxHistorySize(100)
    .WithMaxHistoryAge(TimeSpan.FromHours(24))
    .WithCircuitBreaker(enabled: true)
    .WithGracefulDegradation(enabled: true)
    .Build();
```

### Advanced Configuration with Circuit Breakers

```csharp
// Advanced configuration with comprehensive resilience features
var config = new HealthCheckServiceConfigBuilder()
    .WithAutomaticCheckInterval(TimeSpan.FromMinutes(2))
    .WithDefaultTimeout(TimeSpan.FromSeconds(15))
    .WithMaxConcurrentHealthChecks(10)
    .WithAutomaticChecks(enabled: true)
    .WithMaxHistorySize(500)
    .WithMaxHistoryAge(TimeSpan.FromDays(7))
    .WithMaxRetries(3)
    .WithRetryDelay(TimeSpan.FromSeconds(5))
    .WithCircuitBreaker(enabled: true)
    .WithDefaultFailureThreshold(5)
    .WithDefaultCircuitBreakerTimeout(TimeSpan.FromMinutes(2))
    .WithCircuitBreakerAlerts(enabled: true)
    .WithGracefulDegradation(enabled: true)
    .WithDegradationThresholds(
        minorThreshold: 0.10,
        moderateThreshold: 0.25,
        severeThreshold: 0.50,
        disabledThreshold: 0.75)
    .WithDegradationAlerts(enabled: true)
    .WithPerformanceTracking(enabled: true)
    .WithEventPublishing(enabled: true)
    .WithLogging(enabled: true, logLevel: LogLevel.Information)
    .WithAlerting(enabled: true, alertLevel: AlertSeverity.Warning)
    .Build();
```

### Unity Integration

Unity-specific configuration uses ScriptableObject assets for designer-friendly workflow:

```csharp
[CreateAssetMenu(menuName = "AhBear/HealthCheck/Config")]
public class HealthCheckConfigAsset : ScriptableObject
{
    [Header("General Settings")]
    public bool enableAutomaticChecks = true;
    public float automaticCheckIntervalSeconds = 60f;
    public float defaultTimeoutSeconds = 30f;
    public int maxConcurrentHealthChecks = 5;

    [Header("History and Retention")]
    public int maxHistorySize = 100;
    public float maxHistoryAgeHours = 24f;
    public int maxRetries = 3;
    public float retryDelaySeconds = 5f;

    [Header("Circuit Breaker Settings")]
    public bool enableCircuitBreaker = true;
    public bool enableCircuitBreakerAlerts = true;
    public int defaultFailureThreshold = 5;
    public float defaultCircuitBreakerTimeoutSeconds = 120f;

    [Header("Graceful Degradation")]
    public bool enableGracefulDegradation = true;
    public bool enableDegradationAlerts = true;
    [Range(0f, 1f)] public float minorDegradationThreshold = 0.10f;
    [Range(0f, 1f)] public float moderateDegradationThreshold = 0.25f;
    [Range(0f, 1f)] public float severeDegradationThreshold = 0.50f;
    [Range(0f, 1f)] public float disabledDegradationThreshold = 0.75f;

    [Header("Integration Settings")]
    public bool enablePerformanceTracking = true;
    public bool enableEventPublishing = true;
    public bool enableLogging = true;
    public LogLevel logLevel = LogLevel.Information;
    public bool enableAlerting = true;
    public AlertSeverity alertLevel = AlertSeverity.Warning;
}
```

## 📦 Installation

### 1. Package Installation

The HealthCheck System is part of the core framework:

```json
// Already included in com.ahbearstudios.core package
"com.ahbearstudios.core": "latest"
"com.ahbearstudios.unity": "latest"  // For Unity-specific components
```

### 2. Factory-Based Service Creation

The HealthCheck System uses the **Factory pattern** for service creation following CLAUDE.md guidelines:

```csharp
/// <summary>
/// Production service creation using HealthCheckServiceFactory.
/// Demonstrates proper Builder → Config → Factory → Service pattern.
/// </summary>
public async UniTask<IHealthCheckService> CreateHealthCheckServiceAsync()
{
    // Step 1: Build configuration using builder pattern
    var config = new HealthCheckServiceConfigBuilder()
        .WithAutomaticCheckInterval(TimeSpan.FromMinutes(1))
        .WithDefaultTimeout(TimeSpan.FromSeconds(30))
        .WithMaxConcurrentHealthChecks(5)
        .WithAutomaticChecks(enabled: true)
        .WithCircuitBreaker(enabled: true)
        .WithGracefulDegradation(enabled: true)
        .WithPerformanceTracking(enabled: true)
        .WithEventPublishing(enabled: true)
        .Build();

    // Step 2: Use factory to create service instance
    var healthCheckService = await _healthCheckServiceFactory.CreateServiceAsync(config);

    // Step 3: Register built-in health checks
    var selfHealthCheck = _healthCheckFactory.CreateHealthCheckServiceHealthCheck(healthCheckService);
    healthCheckService.RegisterHealthCheck(selfHealthCheck);

    // Step 4: Start automatic monitoring
    healthCheckService.StartAutomaticChecks();

    return healthCheckService;
}
```

### 3. Dependency Injection Integration

```csharp
/// <summary>
/// Reflex installer for the HealthCheck System dependencies.
/// Registers factories and core dependencies following CLAUDE.md patterns.
/// </summary>
public class HealthCheckInstaller : IBootstrapInstaller
{
    public string InstallerName => "HealthCheckInstaller";
    public int Priority => 300; // After Logging (100), Messaging (150), Alerts (200)
    public bool IsEnabled => true;
    public Type[] Dependencies => new[]
    {
        typeof(LoggingInstaller),
        typeof(MessagingInstaller),
        typeof(AlertsInstaller),
        typeof(PoolingInstaller),
        typeof(ProfilerInstaller),
        typeof(SerializationInstaller)
    };

    public void Install(ContainerBuilder builder)
    {
        // Bind factories (creation only, no lifecycle management)
        builder.Bind<IHealthCheckServiceFactory>().To<HealthCheckServiceFactory>().AsSingle();
        builder.Bind<IHealthCheckFactory>().To<HealthCheckFactory>().AsSingle();

        // Service instances are created via factory, not directly bound
        // This follows CLAUDE.md pattern: Factories create, services manage lifecycle
    }
}
```

## 🚀 Usage Examples

### Basic Health Check Implementation

```csharp
/// <summary>
/// Production health check implementation demonstrating CLAUDE.md patterns.
/// Uses UniTask, ZLinq, and integrates with all core systems for comprehensive monitoring.
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
    /// Performs comprehensive database health assessment using UniTask for Unity optimization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Health check result with detailed database status</returns>
    public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
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

### Production Health Statistics

```csharp
/// <summary>
/// Production health statistics providing comprehensive metrics for monitoring and optimization.
/// Integrates with profiling and alerting systems for operational visibility.
/// </summary>
public sealed record HealthStatistics
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

## 🔄 Message Bus Integration

The HealthCheck System publishes events via IMessageBusService following CLAUDE.md patterns. All messages use static factory methods with DeterministicIdGenerator:

### Key Message Types

```csharp
// Health status changes
var statusMessage = HealthCheckStatusChangedMessage.Create(
    healthCheckName: "DatabaseCheck",
    oldStatus: HealthStatus.Healthy,
    newStatus: HealthStatus.Degraded,
    reason: "High latency detected",
    source: "HealthCheckService");

// Circuit breaker state changes
var circuitMessage = HealthCheckCircuitBreakerStateChangedMessage.Create(
    operationName: "DatabaseOperations",
    oldState: CircuitBreakerState.Closed,
    newState: CircuitBreakerState.Open,
    reason: "Failure threshold exceeded",
    source: "HealthCheckResilienceService");

// Degradation level changes
var degradationMessage = HealthCheckDegradationChangeMessage.Create(
    oldLevel: DegradationLevel.None,
    newLevel: DegradationLevel.Minor,
    reason: "Multiple health checks failing",
    affectedFeatures: new[] { "BackgroundSync", "Analytics" },
    source: "HealthCheckService");
```

### Message Type Codes

Following CLAUDE.md system ranges (1200-1299 for HealthCheck System):

```csharp
public static class MessageTypeCodes
{
    // Health Check System: 1200-1299
    public const ushort HealthCheckStatusChangedMessage = 1200;
    public const ushort HealthCheckCircuitBreakerStateChangedMessage = 1201;
    public const ushort HealthCheckDegradationChangeMessage = 1202;
    public const ushort HealthCheckStartedMessage = 1203;
    public const ushort HealthCheckCompletedMessage = 1204;
    public const ushort HealthCheckCompletedWithResultsMessage = 1205;
    public const ushort HealthCheckServiceCreatedMessage = 1206;
    public const ushort HealthCheckCreatedMessage = 1207;
    public const ushort HealthCheckAlertMessage = 1208;
    // ... additional message types within range
}
```

## 🎮 Unity Performance Optimization

### Frame Budget Compliance

The HealthCheck System is optimized for Unity's 60+ FPS performance targets:

- **Zero-allocation patterns**: Uses ZLinq instead of LINQ
- **UniTask optimization**: Replaces Task for Unity-specific async operations
- **Profiler integration**: Built-in performance tracking with IProfilerService
- **Efficient data structures**: FixedString64Bytes for identifiers, pooled objects for temporary allocations

### Performance Best Practices

```csharp
// ✅ CORRECT: Zero-allocation health check execution
public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
{
    using var scope = _profilerService.BeginScope("HealthCheck.Execution");

    // Use ZLinq for zero-allocation operations
    var results = healthChecks.AsValueEnumerable()
        .Where(check => check.IsEnabled)
        .Select(check => ExecuteCheckAsync(check, cancellationToken))
        .ToList();

    // Profiler integration for performance monitoring
    _profilerService.RecordMetric("healthchecks.executed", results.Count);

    return await UniTask.WhenAll(results);
}
```

## 📚 Additional Resources

- [CLAUDE.md Development Guidelines](../../CLAUDE.md)
- [Message Bus Integration Patterns](../messaging_system.md)
- [Circuit Breaker Implementation Guide](../resilience_patterns.md)
- [Profiling and Performance Monitoring](../profiling_system.md)
- [Alerting System Integration](../alerting_system.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the HealthCheck System.

## 📄 Dependencies

### Core Dependencies (Required)
- **ILoggingService**: System logging and diagnostic output
- **IMessageBusService**: Event publishing for health status changes
- **IAlertService**: Critical notification and alerting integration
- **IPoolingService**: Object pooling for performance optimization
- **IProfilerService**: Performance monitoring and metrics collection
- **ISerializationService**: Data serialization using MemoryPack backend

### Unity Dependencies
- **UniTask**: Unity-optimized async/await operations
- **Unity.Collections**: High-performance data structures (FixedString64Bytes)
- **ZLinq**: Zero-allocation LINQ operations

### Integration Points
- **Circuit Breaker Integration**: Automatic failure isolation and recovery
- **Degradation Management**: Graceful system degradation based on health status
- **Event-Driven Architecture**: Full integration with message bus for system-wide notifications

### Dependent Systems
All systems requiring health monitoring can integrate with the HealthCheck System through:
- Custom IHealthCheck implementations
- Event subscription via IMessageBusService
- Circuit breaker protection for critical operations
- Graceful degradation policies

---

**Status: ✅ Production Ready**

*The HealthCheck System provides enterprise-grade health monitoring and system resilience capabilities optimized for Unity game development at 60+ FPS performance targets.*
    