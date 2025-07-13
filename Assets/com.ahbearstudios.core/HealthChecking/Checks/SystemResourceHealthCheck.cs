using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Health check for monitoring system resource utilization including CPU, memory, and disk usage
    /// </summary>
    /// <remarks>
    /// Monitors critical system resources to ensure the application is operating within acceptable
    /// resource limits. Provides early warning of resource exhaustion and performance degradation.
    /// Integrates with pooling service for performance optimization and uses platform-specific
    /// resource monitoring APIs for accurate measurements.
    /// </remarks>
    public sealed class SystemResourceHealthCheck : IHealthCheck
    {
        private readonly ILoggingService _logger;
        private readonly IPoolingService _poolingService;
        private readonly SystemResourceThresholds _thresholds;
        private readonly PerformanceCounter _cpuCounter;
        private readonly Process _currentProcess;
        
        private HealthCheckConfiguration _configuration;
        private readonly object _configurationLock = new object();

        /// <inheritdoc />
        public FixedString64Bytes Name { get; } = new FixedString64Bytes("SystemResources");

        /// <inheritdoc />
        public string Description => "Monitors system resource utilization including CPU, memory, and performance metrics";

        /// <inheritdoc />
        public HealthCheckCategory Category => HealthCheckCategory.System;

        /// <inheritdoc />
        public TimeSpan Timeout => _configuration?.Timeout ?? TimeSpan.FromSeconds(10);

        /// <inheritdoc />
        public HealthCheckConfiguration Configuration 
        { 
            get 
            { 
                lock (_configurationLock) 
                { 
                    return _configuration; 
                } 
            } 
        }

        /// <inheritdoc />
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Initializes the system resource health check with dependencies and thresholds
        /// </summary>
        /// <param name="logger">Logging service for diagnostic information</param>
        /// <param name="poolingService">Optional pooling service for performance optimization</param>
        /// <param name="thresholds">Optional custom resource thresholds</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public SystemResourceHealthCheck(
            ILoggingService logger,
            IPoolingService poolingService = null,
            SystemResourceThresholds thresholds = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _poolingService = poolingService;
            _thresholds = thresholds ?? SystemResourceThresholds.CreateDefault();
            
            _currentProcess = Process.GetCurrentProcess();
            
            try
            {
                // Initialize CPU performance counter
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // Initialize counter (first call returns 0)
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to initialize CPU performance counter: {ex.Message}");
                _cpuCounter = null;
            }

            // Set default configuration
            _configuration = HealthCheckConfiguration.ForSystemMonitoring(
                "System Resources", 
                "Comprehensive system resource monitoring");

            _logger.LogInfo("SystemResourceHealthCheck initialized with resource monitoring");
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Collect system resource metrics
                var resourceMetrics = await CollectResourceMetricsAsync(cancellationToken);
                
                // Add metrics to result data
                foreach (var metric in resourceMetrics)
                {
                    data[metric.Key] = metric.Value;
                }

                // Analyze resource health
                var healthStatus = AnalyzeResourceHealth(resourceMetrics);
                var message = CreateStatusMessage(healthStatus, resourceMetrics);

                stopwatch.Stop();

                _logger.LogDebug($"System resource check completed: {healthStatus} in {stopwatch.Elapsed}");

                return CreateHealthCheckResult(healthStatus, message, stopwatch.Elapsed, data);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogDebug("System resource check was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, "System resource health check failed");
                
                data["Exception"] = ex.GetType().Name;
                data["ErrorMessage"] = ex.Message;

                return HealthCheckResult.Unhealthy(
                    $"System resource check failed: {ex.Message}",
                    stopwatch.Elapsed,
                    data,
                    ex);
            }
        }

        /// <inheritdoc />
        public void Configure(HealthCheckConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            lock (_configurationLock)
            {
                _configuration = configuration;
            }

            _logger.LogInfo("SystemResourceHealthCheck configuration updated");
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Type"] = nameof(SystemResourceHealthCheck),
                ["Category"] = Category.ToString(),
                ["Description"] = Description,
                ["MonitoredResources"] = new[] { "CPU", "Memory", "WorkingSet", "Threads", "Handles" },
                ["Platform"] = Environment.OSVersion.Platform.ToString(),
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["SupportsPerformanceCounters"] = _cpuCounter != null,
                ["Thresholds"] = new Dictionary<string, object>
                {
                    ["CpuWarning"] = _thresholds.CpuWarningThreshold,
                    ["CpuCritical"] = _thresholds.CpuCriticalThreshold,
                    ["MemoryWarning"] = _thresholds.MemoryWarningThreshold,
                    ["MemoryCritical"] = _thresholds.MemoryCriticalThreshold,
                    ["WorkingSetWarning"] = _thresholds.WorkingSetWarningThreshold,
                    ["WorkingSetCritical"] = _thresholds.WorkingSetCriticalThreshold
                },
                ["ConfigurationSupport"] = new[] { "Thresholds", "Intervals", "CircuitBreaker", "Alerts" },
                ["Dependencies"] = new string[0],
                ["Version"] = "1.0.0"
            };
        }

        #region Private Implementation

        private async Task<Dictionary<string, object>> CollectResourceMetricsAsync(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, object>();

            // Collect CPU usage
            await CollectCpuMetricsAsync(metrics, cancellationToken);

            // Collect memory metrics
            CollectMemoryMetrics(metrics);

            // Collect process metrics
            CollectProcessMetrics(metrics);

            // Collect system metrics
            CollectSystemMetrics(metrics);

            // Collect garbage collection metrics
            CollectGarbageCollectionMetrics(metrics);

            return metrics;
        }

        private async Task CollectCpuMetricsAsync(Dictionary<string, object> metrics, CancellationToken cancellationToken)
        {
            try
            {
                if (_cpuCounter != null)
                {
                    // Get system CPU usage
                    var systemCpuUsage = _cpuCounter.NextValue();
                    metrics["SystemCpuUsage"] = systemCpuUsage;
                    
                    // Wait briefly for accurate reading
                    await Task.Delay(100, cancellationToken);
                    systemCpuUsage = _cpuCounter.NextValue();
                    metrics["SystemCpuUsageAccurate"] = systemCpuUsage;
                }

                // Get process CPU usage
                var processCpuTime = _currentProcess.TotalProcessorTime;
                metrics["ProcessTotalCpuTime"] = processCpuTime.TotalMilliseconds;
                metrics["ProcessUserCpuTime"] = _currentProcess.UserProcessorTime.TotalMilliseconds;
                metrics["ProcessPrivilegedCpuTime"] = _currentProcess.PrivilegedProcessorTime.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect CPU metrics: {ex.Message}");
                metrics["CpuMetricsError"] = ex.Message;
            }
        }

        private void CollectMemoryMetrics(Dictionary<string, object> metrics)
        {
            try
            {
                // Process memory metrics
                _currentProcess.Refresh();
                metrics["WorkingSet"] = _currentProcess.WorkingSet64;
                metrics["PrivateMemorySize"] = _currentProcess.PrivateMemorySize64;
                metrics["VirtualMemorySize"] = _currentProcess.VirtualMemorySize64;
                metrics["PagedMemorySize"] = _currentProcess.PagedMemorySize64;
                metrics["PagedSystemMemorySize"] = _currentProcess.PagedSystemMemorySize64;
                metrics["NonpagedSystemMemorySize"] = _currentProcess.NonpagedSystemMemorySize64;

                // .NET memory metrics
                var totalMemory = GC.GetTotalMemory(false);
                var totalMemoryAfterCollection = GC.GetTotalMemory(true);
                
                metrics["ManagedMemory"] = totalMemory;
                metrics["ManagedMemoryAfterGC"] = totalMemoryAfterCollection;
                metrics["ManagedMemoryDifference"] = totalMemory - totalMemoryAfterCollection;

                // Memory pressure
                var memoryPressure = totalMemory > _thresholds.MemoryWarningThreshold ? "High" : "Normal";
                metrics["MemoryPressure"] = memoryPressure;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect memory metrics: {ex.Message}");
                metrics["MemoryMetricsError"] = ex.Message;
            }
        }

        private void CollectProcessMetrics(Dictionary<string, object> metrics)
        {
            try
            {
                _currentProcess.Refresh();
                
                metrics["ProcessId"] = _currentProcess.Id;
                metrics["ThreadCount"] = _currentProcess.Threads.Count;
                metrics["HandleCount"] = _currentProcess.HandleCount;
                metrics["ProcessStartTime"] = _currentProcess.StartTime;
                metrics["ProcessUptime"] = DateTime.Now - _currentProcess.StartTime;
                
                // Process priority and responsiveness
                metrics["ProcessPriorityClass"] = _currentProcess.PriorityClass.ToString();
                metrics["ProcessorAffinity"] = _currentProcess.ProcessorAffinity.ToInt64();
                metrics["ProcessName"] = _currentProcess.ProcessName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect process metrics: {ex.Message}");
                metrics["ProcessMetricsError"] = ex.Message;
            }
        }

        private void CollectSystemMetrics(Dictionary<string, object> metrics)
        {
            try
            {
                metrics["ProcessorCount"] = Environment.ProcessorCount;
                metrics["SystemPageSize"] = Environment.SystemPageSize;
                metrics["TickCount"] = Environment.TickCount;
                metrics["Is64BitProcess"] = Environment.Is64BitProcess;
                metrics["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem;
                metrics["MachineName"] = Environment.MachineName;
                metrics["OSVersion"] = Environment.OSVersion.ToString();
                metrics["CLRVersion"] = Environment.Version.ToString();
                
                // System directories
                metrics["SystemDirectory"] = Environment.SystemDirectory;
                metrics["CurrentDirectory"] = Environment.CurrentDirectory;
                metrics["WorkingDirectory"] = Environment.CurrentDirectory;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect system metrics: {ex.Message}");
                metrics["SystemMetricsError"] = ex.Message;
            }
        }

        private void CollectGarbageCollectionMetrics(Dictionary<string, object> metrics)
        {
            try
            {
                metrics["GC.MaxGeneration"] = GC.MaxGeneration;
                
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    metrics[$"GC.Gen{i}Collections"] = GC.CollectionCount(i);
                }
                
                // Memory allocation rate (approximate)
                var allocatedBytes = GC.GetTotalAllocatedBytes(false);
                metrics["TotalAllocatedBytes"] = allocatedBytes;
                
                // LOH size if available (.NET Core 3.0+)
                try
                {
                    var memoryInfo = GC.GetGCMemoryInfo();
                    metrics["HeapSizeBytes"] = memoryInfo.HeapSizeBytes;
                    metrics["MemoryLoadBytes"] = memoryInfo.MemoryLoadBytes;
                    metrics["TotalAvailableMemoryBytes"] = memoryInfo.TotalAvailableMemoryBytes;
                    metrics["HighMemoryLoadThresholdBytes"] = memoryInfo.HighMemoryLoadThresholdBytes;
                    metrics["FragmentedBytes"] = memoryInfo.FragmentedBytes;
                }
                catch (Exception)
                {
                    // GC.GetGCMemoryInfo() not available on all platforms
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect GC metrics: {ex.Message}");
                metrics["GCMetricsError"] = ex.Message;
            }
        }

        private HealthStatus AnalyzeResourceHealth(Dictionary<string, object> metrics)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            // Analyze CPU usage
            if (metrics.TryGetValue("SystemCpuUsageAccurate", out var cpuUsageObj) && cpuUsageObj is float cpuUsage)
            {
                if (cpuUsage >= _thresholds.CpuCriticalThreshold)
                {
                    issues.Add($"Critical CPU usage: {cpuUsage:F1}%");
                }
                else if (cpuUsage >= _thresholds.CpuWarningThreshold)
                {
                    warnings.Add($"High CPU usage: {cpuUsage:F1}%");
                }
            }

            // Analyze memory usage
            if (metrics.TryGetValue("WorkingSet", out var workingSetObj) && workingSetObj is long workingSet)
            {
                if (workingSet >= _thresholds.WorkingSetCriticalThreshold)
                {
                    issues.Add($"Critical working set size: {workingSet / (1024 * 1024)} MB");
                }
                else if (workingSet >= _thresholds.WorkingSetWarningThreshold)
                {
                    warnings.Add($"High working set size: {workingSet / (1024 * 1024)} MB");
                }
            }

            if (metrics.TryGetValue("ManagedMemory", out var managedMemoryObj) && managedMemoryObj is long managedMemory)
            {
                if (managedMemory >= _thresholds.MemoryCriticalThreshold)
                {
                    issues.Add($"Critical managed memory: {managedMemory / (1024 * 1024)} MB");
                }
                else if (managedMemory >= _thresholds.MemoryWarningThreshold)
                {
                    warnings.Add($"High managed memory: {managedMemory / (1024 * 1024)} MB");
                }
            }

            // Analyze thread count
            if (metrics.TryGetValue("ThreadCount", out var threadCountObj) && threadCountObj is int threadCount)
            {
                if (threadCount >= _thresholds.ThreadCountCriticalThreshold)
                {
                    issues.Add($"Critical thread count: {threadCount}");
                }
                else if (threadCount >= _thresholds.ThreadCountWarningThreshold)
                {
                    warnings.Add($"High thread count: {threadCount}");
                }
            }

            // Analyze handle count
            if (metrics.TryGetValue("HandleCount", out var handleCountObj) && handleCountObj is int handleCount)
            {
                if (handleCount >= _thresholds.HandleCountCriticalThreshold)
                {
                    issues.Add($"Critical handle count: {handleCount}");
                }
                else if (handleCount >= _thresholds.HandleCountWarningThreshold)
                {
                    warnings.Add($"High handle count: {handleCount}");
                }
            }

            // Determine overall health status
            if (issues.Count > 0)
            {
                return HealthStatus.Unhealthy;
            }
            if (warnings.Count > 0)
            {
                return HealthStatus.Degraded;
            }
            
            return HealthStatus.Healthy;
        }

        private string CreateStatusMessage(HealthStatus status, Dictionary<string, object> metrics)
        {
            var cpuUsage = metrics.TryGetValue("SystemCpuUsageAccurate", out var cpu) ? $"{cpu:F1}%" : "N/A";
            var workingSet = metrics.TryGetValue("WorkingSet", out var ws) && ws is long wsLong ? 
                $"{wsLong / (1024 * 1024)} MB" : "N/A";
            var managedMemory = metrics.TryGetValue("ManagedMemory", out var mm) && mm is long mmLong ? 
                $"{mmLong / (1024 * 1024)} MB" : "N/A";
            var threads = metrics.TryGetValue("ThreadCount", out var tc) ? tc.ToString() : "N/A";

            return status switch
            {
                HealthStatus.Healthy => $"System resources healthy - CPU: {cpuUsage}, Memory: {workingSet}/{managedMemory}, Threads: {threads}",
                HealthStatus.Degraded => $"System resources degraded - CPU: {cpuUsage}, Memory: {workingSet}/{managedMemory}, Threads: {threads}",
                HealthStatus.Unhealthy => $"System resources critical - CPU: {cpuUsage}, Memory: {workingSet}/{managedMemory}, Threads: {threads}",
                _ => $"System resources status unknown - CPU: {cpuUsage}, Memory: {workingSet}/{managedMemory}, Threads: {threads}"
            };
        }

        private HealthCheckResult CreateHealthCheckResult(
            HealthStatus status, 
            string message, 
            TimeSpan duration, 
            Dictionary<string, object> data)
        {
            return status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(message, duration, data),
                HealthStatus.Degraded => HealthCheckResult.Degraded(message, duration, data),
                HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(message, duration, data),
                _ => HealthCheckResult.Unhealthy("Unknown system resource status", duration, data)
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the health check
        /// </summary>
        public void Dispose()
        {
            try
            {
                _cpuCounter?.Dispose();
                _currentProcess?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error disposing SystemResourceHealthCheck");
            }
        }

        #endregion
    }
}