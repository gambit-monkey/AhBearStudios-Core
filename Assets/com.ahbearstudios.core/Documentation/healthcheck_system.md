    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration;
        if (configuration.Parameters.TryGetValue("Timeout", out var timeoutValue))
        {
            Timeout = TimeSpan.FromSeconds(Convert.ToDouble(timeoutValue));
        }
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Provider"] = _database.GetType().Name,
            ["ConnectionString"] = _database.GetConnectionString(masked: true),
            ["LastCheck"] = DateTime.UtcNow
        };
    }
}
```

### Composite Health Check

```csharp
public class ApplicationHealthCheck : IHealthCheck
{
    public string Name => "Application";
    public string Description => "Overall application health including all subsystems";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromMinutes(2);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<string> Dependencies => _dependentChecks;
    
    private readonly IHealthCheckService _healthCheckService;
    private readonly string[] _dependentChecks = { "Database", "Cache", "ExternalAPI", "FileSystem" };
    
    public ApplicationHealthCheck(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        var issues = new List<string>();
        
        try
        {
            // Execute all dependent health checks
            var results = new Dictionary<string, HealthCheckResult>();
            
            foreach (var checkName in _dependentChecks)
            {
                try
                {
                    var result = await _healthCheckService.ExecuteHealthCheckAsync(checkName, cancellationToken);
                    results[checkName] = result;
                    data[$"{checkName}_Status"] = result.Status.ToString();
                    data[$"{checkName}_Duration"] = result.Duration.TotalMilliseconds;
                    
                    if (result.Status == HealthStatus.Unhealthy)
                    {
                        issues.Add($"{checkName}: {result.Message}");
                    }
                    else if (result.Status == HealthStatus.Degraded)
                    {
                        issues.Add($"{checkName} (degraded): {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"{checkName}: Health check failed - {ex.Message}");
                    data[$"{checkName}_Status"] = "Failed";
                }
            }
            
            // Calculate overall health
            var healthyCount = results.Values.Count(r => r.Status == HealthStatus.Healthy);
            var degradedCount = results.Values.Count(r => r.Status == HealthStatus.Degraded);
            var unhealthyCount = results.Values.Count(r => r.Status == HealthStatus.Unhealthy);
            
            data["HealthyCount"] = healthyCount;
            data["DegradedCount"] = degradedCount;
            data["UnhealthyCount"] = unhealthyCount;
            data["TotalChecks"] = results.Count;
            
            // Determine overall status
            if (unhealthyCount > 0)
            {
                var message = unhealthyCount == 1 
                    ? $"1 system unhealthy: {string.Join(", ", issues)}"
                    : $"{unhealthyCount} systems unhealthy: {string.Join(", ", issues)}";
                    
                return HealthCheckResult.Unhealthy(message, stopwatch.Elapsed, data);
            }
            
            if (degradedCount > 0)
            {
                var message = degradedCount == 1
                    ? $"1 system degraded: {string.Join(", ", issues)}"
                    : $"{degradedCount} systems degraded: {string.Join(", ", issues)}";
                    
                return HealthCheckResult.Degraded(message, stopwatch.Elapsed, data);
            }
            
            return HealthCheckResult.Healthy(
                $"All {healthyCount} systems healthy",
                stopwatch.Elapsed,
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Application health check failed: {ex.Message}",
                stopwatch.Elapsed,
                data,
                ex);
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["DependentChecks"] = _dependentChecks,
            ["CheckCount"] = _dependentChecks.Length
        };
    }
}
```

### System Resource Health Check

```csharp
public class SystemResourceHealthCheck : IHealthCheck
{
    public string Name => "SystemResources";
    public string Description => "Monitors CPU, memory, and disk usage";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(10);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    
    public SystemResourceHealthCheck()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        var issues = new List<string>();
        
        try
        {
            // Get CPU usage
            var cpuUsage = _cpuCounter.NextValue();
            await Task.Delay(1000, cancellationToken); // Wait for accurate reading
            cpuUsage = _cpuCounter.NextValue();
            
            data["CpuUsage"] = Math.Round(cpuUsage, 2);
            
            // Get memory usage
            var availableMemoryMB = _memoryCounter.NextValue();
            var totalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            var memoryUsagePercent = ((totalMemoryMB - availableMemoryMB) / totalMemoryMB) * 100;
            
            data["AvailableMemoryMB"] = Math.Round(availableMemoryMB, 2);
            data["TotalMemoryMB"] = Math.Round(totalMemoryMB, 2);
            data["MemoryUsagePercent"] = Math.Round(memoryUsagePercent, 2);
            
            // Get disk usage
            var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
            var diskUsagePercent = ((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize) * 100;
            
            data["DiskUsagePercent"] = Math.Round(diskUsagePercent, 2);
            data["AvailableDiskSpaceGB"] = Math.Round(driveInfo.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2);
            
            // Evaluate thresholds
            if (cpuUsage > 90)
                issues.Add($"CPU usage critical: {cpuUsage:F1}%");
            else if (cpuUsage > 75)
                issues.Add($"CPU usage high: {cpuUsage:F1}%");
                
            if (memoryUsagePercent > 90)
                issues.Add($"Memory usage critical: {memoryUsagePercent:F1}%");
            else if (memoryUsagePercent > 80)
                issues.Add($"Memory usage high: {memoryUsagePercent:F1}%");
                
            if (diskUsagePercent > 95)
                issues.Add($"Disk usage critical: {diskUsagePercent:F1}%");
            else if (diskUsagePercent > 85)
                issues.Add($"Disk usage high: {diskUsagePercent:F1}%");
            
            // Determine status
            var criticalIssues = issues.Where(i => i.Contains("critical")).ToList();
            var warningIssues = issues.Where(i => i.Contains("high")).ToList();
            
            if (criticalIssues.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical resource issues: {string.Join(", ", criticalIssues)}",
                    stopwatch.Elapsed,
                    data);
            }
            
            if (warningIssues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Resource warnings: {string.Join(", ", warningIssues)}",
                    stopwatch.Elapsed,
                    data);
            }
            
            return HealthCheckResult.Healthy(
                $"System resources normal (CPU: {cpuUsage:F1}%, Memory: {memoryUsagePercent:F1}%, Disk: {diskUsagePercent:F1}%)",
                stopwatch.Elapsed,
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"System resource check failed: {ex.Message}",
                stopwatch.Elapsed,
                data,
                ex);
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Platform"] = Environment.OSVersion.Platform.ToString(),
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["MachineName"] = Environment.MachineName
        };
    }
    
    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
    }
}
```

### Health Check Service Usage

```csharp
public class GameHealthMonitor : MonoBehaviour
{
    private IHealthCheckService _healthCheckService;
    private IAlertService _alertService;
    private ILoggingService _logger;
    
    [Inject]
    public void Initialize(
        IHealthCheckService healthCheckService, 
        IAlertService alertService,
        ILoggingService logger)
    {
        _healthCheckService = healthCheckService;
        _alertService = alertService;
        _logger = logger;
        
        SetupHealthChecks();
        SetupEventHandlers();
    }
    
    private void SetupHealthChecks()
    {
        // Register built-in health checks
        _healthCheckService.RegisterHealthCheck(new SystemResourceHealthCheck());
        _healthCheckService.RegisterHealthCheck(new DatabaseHealthCheck(_databaseService, _logger));
        _healthCheckService.RegisterHealthCheck(new NetworkHealthCheck("https://api.game.com/health"));
        _healthCheckService.RegisterHealthCheck(new ApplicationHealthCheck(_healthCheckService));
        
        // Configure check intervals
        _healthCheckService.SetCheckInterval("SystemResources", TimeSpan.FromMinutes(1));
        _healthCheckService.SetCheckInterval("Database", TimeSpan.FromMinutes(2));
        _healthCheckService.SetCheckInterval("Network", TimeSpan.FromMinutes(5));
        _healthCheckService.SetCheckInterval("Application", TimeSpan.FromMinutes(10));
        
        // Start automatic monitoring
        _healthCheckService.StartAutomaticChecks();
    }
    
    private void SetupEventHandlers()
    {
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.HealthCheckCompleted += OnHealthCheckCompleted;
    }
    
    private void OnHealthStatusChanged(object sender, HealthStatusChangeEventArgs e)
    {
        _logger.LogInfo($"Health status changed for {e.HealthCheckName}: {e.PreviousStatus} -> {e.NewStatus}");
        
        // Raise alerts for status degradation
        if (e.NewStatus == HealthStatus.Unhealthy)
        {
            _alertService.RaiseAlert(
                $"System {e.HealthCheckName} is unhealthy: {e.Message}",
                AlertSeverity.Critical,
                "HealthCheckService",
                e.HealthCheckName);
        }
        else if (e.NewStatus == HealthStatus.Degraded && e.PreviousStatus == HealthStatus.Healthy)
        {
            _alertService.RaiseAlert(
                $"System {e.HealthCheckName} is degraded: {e.Message}",
                AlertSeverity.Warning,
                "HealthCheckService",
                e.HealthCheckName);
        }
        else if (e.NewStatus == HealthStatus.Healthy && e.PreviousStatus != HealthStatus.Healthy)
        {
            _alertService.RaiseAlert(
                $"System {e.HealthCheckName} recovered: {e.Message}",
                AlertSeverity.Info,
                "HealthCheckService",
                e.HealthCheckName);
        }
    }
    
    private void OnHealthCheckCompleted(object sender, HealthCheckEventArgs e)
    {
        // Log detailed results for unhealthy checks
        if (e.Result.Status == HealthStatus.Unhealthy)
        {
            _logger.LogError($"Health check failed: {e.Result.Name} - {e.Result.Message}");
            
            if (e.Result.Exception != null)
            {
                _logger.LogException(e.Result.Exception, $"Health check exception: {e.Result.Name}");
            }
        }
    }
    
    public async Task<HealthReport> GetCurrentHealthAsync()
    {
        return await _healthCheckService.ExecuteAllHealthChecksAsync();
    }
    
    public void DisplayHealthStatus()
    {
        var overallHealth = _healthCheckService.GetOverallHealth();
        var lastResults = _healthCheckService.GetLastResults().ToList();
        
        Debug.Log($"Overall Health: {overallHealth}");
        
        foreach (var result in lastResults)
        {
            var statusIcon = result.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };
            
            Debug.Log($"{statusIcon} {result.Name}: {result.Message} ({result.Duration.TotalMilliseconds:F0}ms)");
        }
    }
}
```

## 🎯 Advanced Features

### Health Check Scheduling

```csharp
public class HealthCheckScheduler : IHealthCheckScheduler
{
    private readonly Dictionary<string, ScheduledCheck> _scheduledChecks = new();
    private readonly Timer _schedulerTimer;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILoggingService _logger;
    
    public HealthCheckScheduler(IHealthCheckService healthCheckService, ILoggingService logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _schedulerTimer = new Timer(ProcessScheduledChecks, null, 
                                  TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    public void ScheduleCheck(string checkName, TimeSpan interval, TimeSpan? initialDelay = null)
    {
        var nextRun = DateTime.UtcNow + (initialDelay ?? TimeSpan.Zero);
        
        _scheduledChecks[checkName] = new ScheduledCheck
        {
            CheckName = checkName,
            Interval = interval,
            NextRun = nextRun,
            LastRun = null,
            IsEnabled = true
        };
        
        _logger.LogDebug($"Scheduled health check '{checkName}' to run every {interval} starting at {nextRun}");
    }
    
    public void ScheduleCheck(string checkName, string cronExpression)
    {
        var cron = CronExpression.Parse(cronExpression);
        var nextRun = cron.GetNextOccurrence(DateTime.UtcNow) ?? DateTime.UtcNow.AddHours(1);
        
        _scheduledChecks[checkName] = new ScheduledCheck
        {
            CheckName = checkName,
            CronExpression = cronExpression,
            NextRun = nextRun,
            LastRun = null,
            IsEnabled = true
        };
    }
    
    private async void ProcessScheduledChecks(object state)
    {
        var now = DateTime.UtcNow;
        var checksToRun = _scheduledChecks.Values
            .Where(sc => sc.IsEnabled && sc.NextRun <= now)
            .ToList();
            
        foreach (var scheduledCheck in checksToRun)
        {
            try
            {
                await _healthCheckService.ExecuteHealthCheckAsync(scheduledCheck.CheckName);
                
                scheduledCheck.LastRun = now;
                
                // Calculate next run time
                if (!string.IsNullOrEmpty(scheduledCheck.CronExpression))
                {
                    var cron = CronExpression.Parse(scheduledCheck.CronExpression);
                    scheduledCheck.NextRun = cron.GetNextOccurrence(now) ?? now.AddHours(1);
                }
                else
                {
                    scheduledCheck.NextRun = now + scheduledCheck.Interval;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute scheduled health check '{scheduledCheck.CheckName}': {ex.Message}");
                
                // Schedule retry with backoff
                scheduledCheck.NextRun = now + TimeSpan.FromMinutes(5);
            }
        }
    }
    
    public void EnableCheck(string checkName) => SetCheckEnabled(checkName, true);
    public void DisableCheck(string checkName) => SetCheckEnabled(checkName, false);
    
    private void SetCheckEnabled(string checkName, bool enabled)
    {
        if (_scheduledChecks.TryGetValue(checkName, out var check))
        {
            check.IsEnabled = enabled;
            _logger.LogInfo($"Health check '{checkName}' {(enabled ? "enabled" : "disabled")}");
        }
    }
}

public class ScheduledCheck
{
    public string CheckName { get; set; }
    public TimeSpan Interval { get; set; }
    public string CronExpression { get; set; }
    public DateTime NextRun { get; set; }
    public DateTime? LastRun { get; set; }
    public bool IsEnabled { get; set; }
}
```

### Health Report Generation

```csharp
public class HealthReportingService
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ISerializer _serializer;
    private readonly ILoggingService _logger;
    
    public async Task<HealthReport> GenerateDetailedReportAsync()
    {
        var report = await _healthCheckService.ExecuteAllHealthChecksAsync();
        
        // Enhance report with historical data
        var enhancedReport = new DetailedHealthReport(report)
        {
            SystemInfo = GetSystemInfo(),
            HistoricalTrends = GetHistoricalTrends(),
            Recommendations = GenerateRecommendations(report)
        };
        
        return enhancedReport;
    }
    
    public async Task ExportReportAsync(HealthReport report, string filePath, ExportFormat format)
    {
        switch (format)
        {
            case ExportFormat.Json:
                await ExportAsJsonAsync(report, filePath);
                break;
            case ExportFormat.Csv:
                await ExportAsCsvAsync(report, filePath);
                break;
            case ExportFormat.Html:
                await ExportAsHtmlAsync(report, filePath);
                break;
            case ExportFormat.Pdf:
                await ExportAsPdfAsync(report, filePath);
                break;
            default:
                throw new ArgumentException($"Unsupported export format: {format}");
        }
    }
    
    private async Task ExportAsJsonAsync(HealthReport report, string filePath)
    {
        var reportData = new
        {
            timestamp = report.Timestamp,
            overallStatus = report.OverallStatus.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            summary = new
            {
                totalChecks = report.TotalChecks,
                healthyCount = report.HealthyCount,
                degradedCount = report.DegradedCount,
                unhealthyCount = report.UnhealthyCount
            },
            checks = report.Results.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                message = kvp.Value.Message,
                duration = kvp.Value.Duration.TotalMilliseconds,
                data = kvp.Value.Data
            })
        };
        
        var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
    
    private async Task ExportAsHtmlAsync(HealthReport report, string filePath)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Health Report - {report.Timestamp:yyyy-MM-dd HH:mm:ss}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .summary {{ margin: 20px 0; }}
        .check {{ margin: 10px 0; padding: 10px; border-left: 4px solid #ccc; }}
        .healthy {{ border-left-color: #4CAF50; }}
        .degraded {{ border-left-color: #FF9800; }}
        .unhealthy {{ border-left-color: #F44336; }}
        .status {{ font-weight: bold; }}
        .duration {{ color: #666; font-size: 0.9em; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>System Health Report</h1>
        <p>Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p>
        <p>Overall Status: <span class=""status"">{report.OverallStatus}</span></p>
    </div>
    
    <div class=""summary"">
        <h2>Summary</h2>
        <p>Total Checks: {report.TotalChecks}</p>
        <p>Healthy: {report.HealthyCount} | Degraded: {report.DegradedCount} | Unhealthy: {report.UnhealthyCount}</p>
        <p>Total Duration: {report.TotalDuration.TotalMilliseconds:F0}ms</p>
    </div>
    
    <div class=""checks"">
        <h2>Check Results</h2>";
        
        foreach (var check in report.Results.Values.OrderBy(r => r.Name))
        {
            var statusClass = check.Status.ToString().ToLower();
            html += $@"
        <div class=""check {statusClass}"">
            <h3>{check.Name}</h3>
            <p class=""status"">Status: {check.Status}</p>
            <p>{check.Message}</p>
            <p class=""duration"">Duration: {check.Duration.TotalMilliseconds:F0}ms</p>
        </div>";
        }
        
        html += @"
    </div>
</body>
</html>";
        
        await File.WriteAllTextAsync(filePath, html);
    }
    
    private SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            TotalMemory = GC.GetTotalMemory(false),
            RuntimeVersion = Environment.Version.ToString(),
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
        };
    }
    
    private List<HealthRecommendation> GenerateRecommendations(HealthReport report)
    {
        var recommendations = new List<HealthRecommendation>();
        
        foreach (var result in report.Results.Values)
        {
            if (result.Status == HealthStatus.Unhealthy)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Priority = RecommendationPriority.High,
                    Title = $"Address {result.Name} Issues",
                    Description = $"System {result.Name} is unhealthy: {result.Message}",
                    ActionRequired = "Investigate and resolve the underlying issue immediately"
                });
            }
            else if (result.Status == HealthStatus.Degraded)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Priority = RecommendationPriority.Medium,
                    Title = $"Monitor {result.Name} Performance",
                    Description = $"System {result.Name} is showing degraded performance: {result.Message}",
                    ActionRequired = "Monitor closely and consider optimization"
                });
            }
        }
        
        return recommendations;
    }
}

public enum ExportFormat
{
    Json,
    Csv,
    Html,
    Pdf
}
```

## 📊 Performance Characteristics

### Health Check Performance

| Check Type | Typical Duration | Memory Usage | Frequency |
|------------|------------------|--------------|-----------|
| System Resources | 1-2 seconds | 1KB | Every 1-5 minutes |
| Database Connectivity | 50-500ms | 500 bytes | Every 2-10 minutes |
| Network Endpoint | 100ms-5s | 200 bytes | Every 5-30 minutes |
| Application Health | 2-10 seconds | 5KB | Every 10-60 minutes |
| Composite Check | 5-30 seconds | 10-50KB | Every 30-300 minutes |

### Scheduling Overhead

- **Timer Resolution**: 10-second intervals for check scheduling
- **Memory per Check**: ~200 bytes for scheduling metadata
- **CPU Impact**: <0.1% for typical health check workloads
- **Concurrent Checks**: Configurable, recommended max 10-20 concurrent

## 🏥 Health Monitoring

### Self-Monitoring Health Check

```csharp
public class HealthCheckServiceHealthCheck : IHealthCheck
{
    public string Name => "HealthCheckService";
    public string Description => "Monitors the health check service itself";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(10);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    
    private readonly IHealthCheckService _healthCheckService;
    
    public HealthCheckServiceHealthCheck(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        
        try
        {
            var metrics = _healthCheckService.GetHealthMetrics();
            
            data["TotalChecks"] = metrics.TotalRegisteredChecks;
            data["ActiveChecks"] = metrics.ActiveChecks;
            data["FailedChecks"] = metrics.FailedChecks;
            data["AverageCheckDuration"] = metrics.AverageCheckDuration.TotalMilliseconds;
            data["LastCheckTime"] = metrics.LastCheckTime;
            data["ChecksInLast24Hours"] = metrics.ChecksInLast24Hours;
            
            // Validate service health
            if (metrics.FailedChecks > metrics.TotalRegisteredChecks * 0.5) // 50% failure rate
            {
                return HealthCheckResult.Unhealthy(
                    $"High failure rate: {metrics.FailedChecks}/{metrics.TotalRegisteredChecks} checks failing",
                    stopwatch.Elapsed,
                    data);
            }
            
            if (metrics.AverageCheckDuration > TimeSpan.FromMinutes(1))
            {
                return HealthCheckResult.Degraded(
                    $"Slow check performance: average {metrics.AverageCheckDuration.TotalSeconds:F1}s",
                    stopwatch.Elapsed,
                    data);
            }
            
            if (DateTime.UtcNow - metrics.LastCheckTime > TimeSpan.FromMinutes(10))
            {
                return HealthCheckResult.Degraded(
                    $"No recent checks: last check {metrics.LastCheckTime:HH:mm:ss}",
                    stopwatch.Elapsed,
                    data);
            }
            
            return HealthCheckResult.Healthy(
                $"Health check service operational: {metrics.ActiveChecks} active checks",
                stopwatch.Elapsed,
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to check health service status: {ex.Message}",
                stopwatch.Elapsed,
                data,
                ex);
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["ServiceType"] = _healthCheckService.GetType().Name,
            ["SelfMonitoring"] = true
        };
    }
}
```

## 🧪 Testing

### Unit Testing

```csharp
[Test]
public async Task HealthCheck_DatabaseConnected_ReturnsHealthy()
{
    // Arrange
    var healthCheckService = new HealthCheckService(_mockLogger.Object, _mockAlerts.Object);
    
    var healthyCheck = new Mock<IHealthCheck>();
    healthyCheck.Setup(hc => hc.Name).Returns("HealthyCheck");
    healthyCheck.Setup(hc => hc.CheckHealthAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(HealthCheckResult.Healthy("All good"));
    
    var degradedCheck = new Mock<IHealthCheck>();
    degradedCheck.Setup(hc => hc.Name).Returns("DegradedCheck");
    degradedCheck.Setup(hc => hc.CheckHealthAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(HealthCheckResult.Degraded("Some issues"));
    
    healthCheckService.RegisterHealthCheck(healthyCheck.Object);
    healthCheckService.RegisterHealthCheck(degradedCheck.Object);
    
    // Act
    var report = await healthCheckService.ExecuteAllHealthChecksAsync();
    
    // Assert
    Assert.That(report.TotalChecks, Is.EqualTo(2));
    Assert.That(report.HealthyCount, Is.EqualTo(1));
    Assert.That(report.DegradedCount, Is.EqualTo(1));
    Assert.That(report.UnhealthyCount, Is.EqualTo(0));
    Assert.That(report.OverallStatus, Is.EqualTo(HealthStatus.Degraded));
}
```

### Performance Testing

```csharp
[Benchmark]
public async Task HealthCheck_SimpleCheck()
{
    await _simpleHealthCheck.CheckHealthAsync();
}

[Benchmark]
public async Task HealthCheck_DatabaseCheck()
{
    await _databaseHealthCheck.CheckHealthAsync();
}

[Benchmark]
public async Task HealthCheckService_ExecuteAllChecks()
{
    await _healthCheckService.ExecuteAllHealthChecksAsync();
}
```

### Integration Testing

```csharp
[Test]
public async Task HealthCheckService_WithRealDependencies_WorksCorrectly()
{
    // Arrange
    var container = CreateTestContainer();
    var healthCheckService = container.Resolve<IHealthCheckService>();
    var databaseService = container.Resolve<IDatabaseService>();
    
    // Register real health checks
    healthCheckService.RegisterHealthCheck(new DatabaseHealthCheck(databaseService, _logger));
    healthCheckService.RegisterHealthCheck(new SystemResourceHealthCheck());
    
    // Act
    var report = await healthCheckService.ExecuteAllHealthChecksAsync();
    
    // Assert
    Assert.That(report.TotalChecks, Is.GreaterThan(0));
    Assert.That(report.OverallStatus, Is.Not.EqualTo(HealthStatus.Unknown));
    
    foreach (var result in report.Results.Values)
    {
        Assert.That(result.Duration, Is.LessThan(TimeSpan.FromMinutes(1)));
        Assert.That(string.IsNullOrEmpty(result.Message), Is.False);
    }
}
```

## 🚀 Getting Started

### 1. Installation

```csharp
// In Package Manager, add:
"com.ahbearstudios.core.healthcheck": "2.0.0"
```

### 2. Basic Setup

```csharp
public class HealthCheckInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Configure health checks
        var config = new HealthCheckConfigBuilder()
            .WithDefaultInterval(TimeSpan.FromMinutes(5))
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithAutomaticChecks(enabled: true)
            .WithAlerting(enabled: true)
            .Build();
            
        Container.Bind<HealthCheckConfig>().FromInstance(config);
        Container.Bind<IHealthCheckService>().To<HealthCheckService>().AsSingle();
        Container.Bind<IHealthCheckScheduler>().To<HealthCheckScheduler>().AsSingle();
    }
}
```

### 3. Register Health Checks

```csharp
public class GameInitializer : MonoBehaviour
{
    private IHealthCheckService _healthCheckService;
    
    [Inject]
    public void Initialize(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
        
        // Register built-in checks
        _healthCheckService.RegisterHealthCheck(new SystemResourceHealthCheck());
        
        // Register custom checks
        _healthCheckService.RegisterHealthCheck("Database", 
            new DatabaseHealthCheck(_databaseService, _logger));
        _healthCheckService.RegisterHealthCheck("GameLogic", 
            new GameLogicHealthCheck(_gameManager));
        
        // Start monitoring
        _healthCheckService.StartAutomaticChecks();
    }
}
```

### 4. Custom Health Check

```csharp
public class GameLogicHealthCheck : IHealthCheck
{
    public string Name => "GameLogic";
    public string Description => "Checks core game systems";
    public HealthCheckCategory Category => HealthCheckCategory.Custom;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(15);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    
    private readonly GameManager _gameManager;
    
    public GameLogicHealthCheck(GameManager gameManager)
    {
        _gameManager = gameManager;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        
        try
        {
            // Check if game manager is initialized
            if (!_gameManager.IsInitialized)
            {
                return HealthCheckResult.Unhealthy(
                    "Game manager not initialized",
                    stopwatch.Elapsed,
                    data);
            }
            
            // Check player count
            var playerCount = _gameManager.GetActivePlayerCount();
            data["ActivePlayers"] = playerCount;
            
            // Check game state
            var gameState = _gameManager.GetCurrentState();
            data["GameState"] = gameState.ToString();
            
            // Check for errors
            var errorCount = _gameManager.GetErrorCount();
            data["ErrorCount"] = errorCount;
            
            if (errorCount > 10)
            {
                return HealthCheckResult.Degraded(
                    $"High error count: {errorCount}",
                    stopwatch.Elapsed,
                    data);
            }
            
            return HealthCheckResult.Healthy(
                $"Game logic healthy ({playerCount} players)",
                stopwatch.Elapsed,
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Game logic check failed: {ex.Message}",
                stopwatch.Elapsed,
                data,
                ex);
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["GameManagerType"] = _gameManager.GetType().Name,
            ["Version"] = "1.0.0"
        };
    }
}
```

## 📚 Additional Resources

- [Health Check Best Practices](HEALTHCHECK_BEST_PRACTICES.md)
- [Custom Health Check Development](HEALTHCHECK_CUSTOM_CHECKS.md)
- [Health Monitoring Strategies](HEALTHCHECK_MONITORING.md)
- [Alert Integration Guide](HEALTHCHECK_ALERTS.md)
- [Troubleshooting Guide](HEALTHCHECK_TROUBLESHOOTING.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the HealthCheck System.

## 📄 Dependencies

- **Direct**: Logging, Alerts
- **Dependents**: Bootstrap (for system health monitoring)

---

*The HealthCheck System provides comprehensive health monitoring and status reporting across all AhBearStudios Core systems.*
    var mockDatabase = new Mock<IDatabaseService>();
    mockDatabase.Setup(db => db.TestConnectionAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
    mockDatabase.Setup(db => db.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);
    
    var healthCheck = new DatabaseHealthCheck(mockDatabase.Object, _mockLogger.Object);
    
    // Act
    var result = await healthCheck.CheckHealthAsync();
    
    // Assert
    Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    Assert.That(result.Data["IsConnected"], Is.EqualTo(true));
    Assert.That(result.Data["QueryResult"], Is.EqualTo(1));
}

[Test]
public async Task HealthCheckService_ExecuteAllChecks_ReturnsReport()
{
    // Arrange# HealthCheck System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.HealthCheck`  
**Role:** System health monitoring and status reporting  
**Status:** 🔄 In Progress

The HealthCheck System provides comprehensive health monitoring capabilities for all systems, enabling proactive issue detection, automated health reporting, and integration with alerting systems to maintain optimal system performance and reliability.

## 🚀 Key Features

- **⚡ Real-Time Monitoring**: Continuous health assessment of all registered systems
- **🔧 Flexible Health Checks**: Custom health check implementations for any system
- **📊 Health Aggregation**: Hierarchical health status with dependency tracking
- **🎯 Automated Scheduling**: Configurable health check intervals and timing
- **📈 Health History**: Historical health data tracking and trend analysis
- **🔄 Alert Integration**: Automatic alert generation for health status changes

## 🏗️ Architecture

### Folder Structure

```
AhBearStudios.Core.HealthCheck/
├── IHealthCheckService.cs                # Primary service interface
├── HealthCheckService.cs                 # Health monitoring implementation
├── Configs/
│   ├── HealthCheckConfig.cs              # Health check configuration
│   ├── CheckScheduleConfig.cs            # Scheduling configuration
│   └── ReportingConfig.cs                # Reporting settings
├── Builders/
│   ├── IHealthCheckConfigBuilder.cs      # Configuration builder interface
│   └── HealthCheckConfigBuilder.cs       # Builder implementation
├── Factories/
│   ├── IHealthCheckFactory.cs            # Health check creation interface
│   └── HealthCheckFactory.cs             # Factory implementation
├── Services/
│   ├── HealthAggregationService.cs       # Status aggregation
│   ├── HealthHistoryService.cs           # Historical tracking
│   ├── HealthSchedulingService.cs        # Check scheduling
│   └── HealthReportingService.cs         # Report generation
├── Checks/
│   ├── IHealthCheck.cs                   # Health check interface
│   ├── SystemResourceHealthCheck.cs      # Resource monitoring
│   ├── DatabaseHealthCheck.cs            # Database connectivity
│   ├── NetworkHealthCheck.cs             # Network connectivity
│   └── CompositeHealthCheck.cs           # Multi-check aggregation
├── Schedulers/
│   ├── IHealthCheckScheduler.cs          # Scheduler interface
│   ├── IntervalScheduler.cs              # Time-based scheduling
│   └── CronScheduler.cs                  # Cron-based scheduling
├── Models/
│   ├── HealthCheckResult.cs              # Check result
│   ├── HealthStatus.cs                   # Status enumeration
│   ├── HealthReport.cs                   # Comprehensive report
│   └── HealthMetrics.cs                  # Health metrics data
└── HealthChecks/
    └── HealthCheckServiceHealthCheck.cs  # Self-monitoring

AhBearStudios.Unity.HealthCheck/
├── Installers/
│   └── HealthCheckInstaller.cs           # Reflex registration
├── Components/
│   ├── HealthCheckDisplayComponent.cs    # Visual health display
│   └── HealthCheckDebugComponent.cs      # Debug information
├── Checks/
│   ├── UnitySystemHealthCheck.cs         # Unity-specific checks
│   └── PerformanceHealthCheck.cs         # Performance monitoring
└── ScriptableObjects/
    └── HealthCheckConfigAsset.cs         # Unity configuration
```

## 🔌 Key Interfaces

### IHealthCheckService

The primary interface for health monitoring operations.

```csharp
public interface IHealthCheckService
{
    // Health check registration
    void RegisterHealthCheck(IHealthCheck healthCheck);
    void RegisterHealthCheck(string name, IHealthCheck healthCheck);
    void UnregisterHealthCheck(string name);
    
    // Individual health checks
    Task<HealthCheckResult> ExecuteHealthCheckAsync(string name, 
        CancellationToken cancellationToken = default);
    HealthCheckResult ExecuteHealthCheck(string name);
    
    // Batch health checks
    Task<HealthReport> ExecuteAllHealthChecksAsync(
        CancellationToken cancellationToken = default);
    Task<HealthReport> ExecuteHealthChecksAsync(IEnumerable<string> names, 
        CancellationToken cancellationToken = default);
    
    // Health status queries
    HealthStatus GetOverallHealth();
    HealthStatus GetSystemHealth(string systemName);
    IEnumerable<HealthCheckResult> GetLastResults();
    
    // Scheduling and automation
    void StartAutomaticChecks();
    void StopAutomaticChecks();
    void SetCheckInterval(string name, TimeSpan interval);
    
    // History and reporting
    IEnumerable<HealthCheckResult> GetHealthHistory(string name, TimeSpan period);
    HealthReport GenerateHealthReport();
    HealthMetrics GetHealthMetrics();
    
    // Events
    event EventHandler<HealthCheckEventArgs> HealthCheckCompleted;
    event EventHandler<HealthStatusChangeEventArgs> HealthStatusChanged;
}
```

### IHealthCheck

Core interface for individual health checks.

```csharp
public interface IHealthCheck
{
    string Name { get; }
    string Description { get; }
    HealthCheckCategory Category { get; }
    TimeSpan Timeout { get; }
    
    // Health check execution
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    // Configuration
    HealthCheckConfiguration Configuration { get; }
    void Configure(HealthCheckConfiguration configuration);
    
    // Dependencies
    IEnumerable<string> Dependencies { get; }
    
    // Metadata
    Dictionary<string, object> GetMetadata();
}

public enum HealthCheckCategory
{
    System,
    Database,
    Network,
    Performance,
    Security,
    Custom
}
```

### IHealthCheckResult

Result of a health check execution.

```csharp
public interface IHealthCheckResult
{
    string Name { get; }
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

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
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
    
    // Results
    IReadOnlyDictionary<string, HealthCheckResult> Results { get; }
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
}
```

## ⚙️ Configuration

### Basic Configuration

```csharp
var config = new HealthCheckConfigBuilder()
    .WithDefaultInterval(TimeSpan.FromMinutes(1))
    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
    .WithAutomaticChecks(enabled: true)
    .WithHistoryRetention(TimeSpan.FromHours(24))
    .WithAlerting(enabled: true)
    .Build();
```

### Advanced Configuration

```csharp
var config = new HealthCheckConfigBuilder()
    .WithDefaultInterval(TimeSpan.FromMinutes(5))
    .WithDefaultTimeout(TimeSpan.FromSeconds(10))
    .WithScheduling(builder => builder
        .WithScheduler<IntervalScheduler>()
        .WithConcurrentChecks(maxConcurrency: 10)
        .WithFailureRetry(maxRetries: 3, backoff: TimeSpan.FromSeconds(5)))
    .WithAlerting(builder => builder
        .EnableAlerts(true)
        .WithAlertThreshold(consecutiveFailures: 3)
        .WithAlertCooldown(TimeSpan.FromMinutes(15))
        .WithEscalation(enabled: true, escalationTime: TimeSpan.FromHours(1)))
    .WithReporting(builder => builder
        .EnableHistoricalTracking(true)
        .WithHistoryRetention(TimeSpan.FromDays(7))
        .WithMetricsCollection(enabled: true)
        .WithReportGeneration(interval: TimeSpan.FromHours(6)))
    .WithChecks(builder => builder
        .AddSystemResourceCheck(interval: TimeSpan.FromMinutes(1))
        .AddDatabaseCheck("MainDatabase", connectionString, TimeSpan.FromMinutes(2))
        .AddNetworkCheck("ExternalAPI", "https://api.example.com/health")
        .AddCustomCheck<ApplicationHealthCheck>(TimeSpan.FromMinutes(5)))
    .Build();
```

### Unity Integration

```csharp
[CreateAssetMenu(menuName = "AhBear/HealthCheck/Config")]
public class HealthCheckConfigAsset : ScriptableObject
{
    [Header("General")]
    public bool enableAutomaticChecks = true;
    public float defaultIntervalMinutes = 5f;
    public float defaultTimeoutSeconds = 30f;
    
    [Header("Scheduling")]
    public int maxConcurrentChecks = 10;
    public int maxRetries = 3;
    public float retryBackoffSeconds = 5f;
    
    [Header("Alerting")]
    public bool enableAlerting = true;
    public int alertThreshold = 3;
    public float alertCooldownMinutes = 15f;
    public bool enableEscalation = false;
    
    [Header("History")]
    public bool enableHistoricalTracking = true;
    public float historyRetentionHours = 24f;
    public int maxHistoryEntries = 10000;
    
    [Header("Built-in Checks")]
    public bool enableSystemResourceCheck = true;
    public bool enablePerformanceCheck = true;
    public bool enableUnitySystemCheck = true;
    public HealthCheckConfig[] customChecks = Array.Empty<HealthCheckConfig>();
}

[Serializable]
public class HealthCheckConfig
{
    public string name;
    public string typeName;
    public float intervalMinutes;
    public float timeoutSeconds;
    public bool isEnabled = true;
    public Dictionary<string, string> parameters;
}
```

## 🚀 Usage Examples

### Basic Health Check Implementation

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    public string Name => "Database";
    public string Description => "Checks database connectivity and responsiveness";
    public HealthCheckCategory Category => HealthCheckCategory.Database;
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(30);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    
    private readonly IDatabaseService _database;
    private readonly ILoggingService _logger;
    
    public DatabaseHealthCheck(IDatabaseService database, ILoggingService logger)
    {
        _database = database;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        
        try
        {
            // Test basic connectivity
            var isConnected = await _database.TestConnectionAsync(cancellationToken);
            data["IsConnected"] = isConnected;
            
            if (!isConnected)
            {
                return HealthCheckResult.Unhealthy(
                    "Database connection failed", 
                    stopwatch.Elapsed, 
                    data);
            }
            
            // Test query performance
            var queryStart = Stopwatch.StartNew();
            var result = await _database.ExecuteScalarAsync<int>("SELECT 1", cancellationToken);
            queryStart.Stop();
            
            data["QueryTime"] = queryStart.ElapsedMilliseconds;
            data["QueryResult"] = result;
            
            // Evaluate performance
            if (queryStart.ElapsedMilliseconds > 5000) // 5 seconds
            {
                return HealthCheckResult.Unhealthy(
                    $"Database query too slow: {queryStart.ElapsedMilliseconds}ms",
                    stopwatch.Elapsed,
                    data);
            }
            
            if (queryStart.ElapsedMilliseconds > 1000) // 1 second
            {
                return HealthCheckResult.Degraded(
                    $"Database query slow: {queryStart.ElapsedMilliseconds}ms",
                    stopwatch.Elapsed,
                    data);
            }
            
            // Get additional metrics
            var metrics = await _database.GetMetricsAsync(cancellationToken);
            data["ActiveConnections"] = metrics.ActiveConnections;
            data["TotalQueries"] = metrics.TotalQueries;
            data["AverageQueryTime"] = metrics.AverageQueryTime;
            
            return HealthCheckResult.Healthy(
                $"Database responsive ({queryStart.ElapsedMilliseconds}ms)",
                stopwatch.Elapsed,
                data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "Database health check timed out",
                stopwatch.Elapsed,
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Database health check failed: {ex.Message}");
            
            return HealthCheckResult.Unhealthy(
                $"Database health check failed: {ex.Message}",
                stopwatch.Elapsed,
                data,
                ex);
        }
    }
    