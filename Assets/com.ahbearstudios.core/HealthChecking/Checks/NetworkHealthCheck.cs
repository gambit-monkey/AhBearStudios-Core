using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Health check for monitoring network connectivity, DNS resolution, and external service availability
    /// </summary>
    /// <remarks>
    /// Provides comprehensive network health monitoring including:
    /// - Network interface status and availability
    /// - DNS resolution performance and accuracy
    /// - External endpoint connectivity testing
    /// - HTTP/HTTPS service health verification
    /// - Network latency and performance measurement
    /// - Circuit breaker integration for fault tolerance
    /// </remarks>
    public sealed class NetworkHealthCheck : IHealthCheck
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILoggingService _logger;
        private readonly NetworkHealthCheckOptions _options;
        private readonly HttpClient _httpClient;
        
        private HealthCheckConfiguration _configuration;
        private readonly object _configurationLock = new object();

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public HealthCheckCategory Category => HealthCheckCategory.Network;

        /// <inheritdoc />
        public TimeSpan Timeout => _configuration?.Timeout ?? _options.DefaultTimeout;

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
        public IEnumerable<FixedString64Bytes> Dependencies => _options.Dependencies ?? Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Name of the health check
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what this health check monitors
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default timeout for all network operations
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for HTTP requests
        /// </summary>
        public TimeSpan HttpRequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Timeout for ping operations
        /// </summary>
        public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Timeout for port connectivity tests
        /// </summary>
        public TimeSpan PortConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Ping latency threshold that triggers warning status
        /// </summary>
        public TimeSpan PingWarningThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Ping latency threshold that triggers critical status
        /// </summary>
        public TimeSpan PingCriticalThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// HTTP response time threshold that triggers warning status
        /// </summary>
        public TimeSpan HttpWarningThreshold { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// HTTP response time threshold that triggers critical status
        /// </summary>
        public TimeSpan HttpCriticalThreshold { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// DNS resolution time threshold that triggers warning status
        /// </summary>
        public TimeSpan DnsWarningThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// DNS resolution time threshold that triggers critical status
        /// </summary>
        public TimeSpan DnsCriticalThreshold { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Whether to test network interface availability
        /// </summary>
        public bool EnableNetworkInterfaceTest { get; set; } = true;

        /// <summary>
        /// Whether to test DNS resolution
        /// </summary>
        public bool EnableDnsTest { get; set; } = true;

        /// <summary>
        /// Whether to test ping connectivity
        /// </summary>
        public bool EnablePingTest { get; set; } = true;

        /// <summary>
        /// Whether to test HTTP/HTTPS endpoints
        /// </summary>
        public bool EnableHttpTest { get; set; } = true;

        /// <summary>
        /// Whether to test port connectivity
        /// </summary>
        public bool EnablePortTest { get; set; } = false;

        /// <summary>
        /// HTTP/HTTPS endpoints to test
        /// </summary>
        public string[] TestEndpoints { get; set; }

        /// <summary>
        /// Hosts to test with ping
        /// </summary>
        public string[] PingTestHosts { get; set; }

        /// <summary>
        /// Hosts to test DNS resolution
        /// </summary>
        public string[] DnsTestHosts { get; set; }

        /// <summary>
        /// DNS servers to use for resolution testing
        /// </summary>
        public string[] DnsServers { get; set; }

        /// <summary>
        /// Port connectivity tests to perform
        /// </summary>
        public PortTest[] PortTests { get; set; }

        /// <summary>
        /// Whether to use circuit breaker pattern for fault tolerance
        /// </summary>
        public bool UseCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public FixedString64Bytes[] Dependencies { get; set; }

        /// <summary>
        /// Creates default network health check options
        /// </summary>
        /// <returns>Default configuration</returns>
        public static NetworkHealthCheckOptions CreateDefault()
        {
            return new NetworkHealthCheckOptions
            {
                TestEndpoints = new[] 
                { 
                    "https://www.google.com", 
                    "https://www.microsoft.com" 
                },
                PingTestHosts = new[] 
                { 
                    "8.8.8.8",      // Google DNS
                    "1.1.1.1",     // Cloudflare DNS
                    "208.67.222.222" // OpenDNS
                },
                DnsTestHosts = new[] 
                { 
                    "google.com", 
                    "microsoft.com", 
                    "cloudflare.com" 
                }
            };
        }

        /// <summary>
        /// Creates options optimized for minimal network testing
        /// </summary>
        /// <returns>Minimal testing configuration</returns>
        public static NetworkHealthCheckOptions CreateMinimal()
        {
            return new NetworkHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(15),
                HttpRequestTimeout = TimeSpan.FromSeconds(10),
                PingTimeout = TimeSpan.FromSeconds(3),
                EnableNetworkInterfaceTest = true,
                EnableDnsTest = false,
                EnablePingTest = true,
                EnableHttpTest = false,
                EnablePortTest = false,
                PingTestHosts = new[] { "8.8.8.8" },
                UseCircuitBreaker = true
            };
        }

        /// <summary>
        /// Creates options optimized for comprehensive network testing
        /// </summary>
        /// <returns>Comprehensive testing configuration</returns>
        public static NetworkHealthCheckOptions CreateComprehensive()
        {
            return new NetworkHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(2),
                HttpRequestTimeout = TimeSpan.FromSeconds(30),
                PingTimeout = TimeSpan.FromSeconds(10),
                EnableNetworkInterfaceTest = true,
                EnableDnsTest = true,
                EnablePingTest = true,
                EnableHttpTest = true,
                EnablePortTest = true,
                TestEndpoints = new[] 
                { 
                    "https://www.google.com",
                    "https://www.microsoft.com",
                    "https://httpbin.org/status/200",
                    "https://www.github.com"
                },
                PingTestHosts = new[] 
                { 
                    "8.8.8.8", "8.8.4.4",          // Google DNS
                    "1.1.1.1", "1.0.0.1",          // Cloudflare DNS
                    "208.67.222.222", "208.67.220.220" // OpenDNS
                },
                DnsTestHosts = new[] 
                { 
                    "google.com", "microsoft.com", "github.com",
                    "stackoverflow.com", "cloudflare.com"
                },
                PortTests = new[]
                {
                    new PortTest { Host = "google.com", Port = 80, Protocol = "HTTP" },
                    new PortTest { Host = "google.com", Port = 443, Protocol = "HTTPS" },
                    new PortTest { Host = "8.8.8.8", Port = 53, Protocol = "DNS" },
                    new PortTest { Host = "smtp.gmail.com", Port = 587, Protocol = "SMTP" }
                },
                UseCircuitBreaker = true
            };
        }

        /// <summary>
        /// Creates options optimized for enterprise environments
        /// </summary>
        /// <returns>Enterprise configuration</returns>
        public static NetworkHealthCheckOptions CreateEnterprise()
        {
            return new NetworkHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(45),
                HttpRequestTimeout = TimeSpan.FromSeconds(20),
                PingTimeout = TimeSpan.FromSeconds(5),
                PingWarningThreshold = TimeSpan.FromMilliseconds(50),
                PingCriticalThreshold = TimeSpan.FromMilliseconds(200),
                HttpWarningThreshold = TimeSpan.FromSeconds(1),
                HttpCriticalThreshold = TimeSpan.FromSeconds(5),
                DnsWarningThreshold = TimeSpan.FromMilliseconds(200),
                DnsCriticalThreshold = TimeSpan.FromMilliseconds(1000),
                EnableNetworkInterfaceTest = true,
                EnableDnsTest = true,
                EnablePingTest = true,
                EnableHttpTest = true,
                EnablePortTest = true,
                TestEndpoints = new[] 
                { 
                    "https://www.google.com",
                    "https://www.microsoft.com",
                    "https://azure.microsoft.com/en-us/status/",
                    "https://status.aws.amazon.com/"
                },
                PingTestHosts = new[] 
                { 
                    "8.8.8.8", "8.8.4.4",          // Google DNS
                    "1.1.1.1", "1.0.0.1",          // Cloudflare DNS
                    "208.67.222.222", "208.67.220.220", // OpenDNS
                    "9.9.9.9", "149.112.112.112"   // Quad9 DNS
                },
                DnsTestHosts = new[] 
                { 
                    "google.com", "microsoft.com", "amazon.com",
                    "github.com", "stackoverflow.com", "cloudflare.com"
                },
                PortTests = new[]
                {
                    new PortTest { Host = "google.com", Port = 80, Protocol = "HTTP" },
                    new PortTest { Host = "google.com", Port = 443, Protocol = "HTTPS" },
                    new PortTest { Host = "8.8.8.8", Port = 53, Protocol = "DNS" },
                    new PortTest { Host = "outlook.office365.com", Port = 993, Protocol = "IMAP" },
                    new PortTest { Host = "smtp.office365.com", Port = 587, Protocol = "SMTP" }
                },
                UseCircuitBreaker = true
            };
        }

        /// <summary>
        /// Creates options optimized for high-speed testing
        /// </summary>
        /// <returns>High-speed testing configuration</returns>
        public static NetworkHealthCheckOptions CreateHighSpeed()
        {
            return new NetworkHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(10),
                HttpRequestTimeout = TimeSpan.FromSeconds(5),
                PingTimeout = TimeSpan.FromSeconds(2),
                PortConnectTimeout = TimeSpan.FromSeconds(3),
                PingWarningThreshold = TimeSpan.FromMilliseconds(50),
                PingCriticalThreshold = TimeSpan.FromMilliseconds(200),
                HttpWarningThreshold = TimeSpan.FromSeconds(1),
                HttpCriticalThreshold = TimeSpan.FromSeconds(3),
                DnsWarningThreshold = TimeSpan.FromMilliseconds(200),
                DnsCriticalThreshold = TimeSpan.FromMilliseconds(800),
                EnableNetworkInterfaceTest = true,
                EnableDnsTest = true,
                EnablePingTest = true,
                EnableHttpTest = false, // Skip HTTP for speed
                EnablePortTest = false, // Skip port tests for speed
                PingTestHosts = new[] { "8.8.8.8", "1.1.1.1" }, // Only test 2 hosts
                DnsTestHosts = new[] { "google.com", "cloudflare.com" }, // Only test 2 hosts
                UseCircuitBreaker = true
            };
        }
    }

    /// <summary>
    /// Configuration for testing port connectivity
    /// </summary>
    public sealed class PortTest
    {
        /// <summary>
        /// Host to connect to
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port number to test
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Protocol description for logging
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Optional timeout for this specific port test
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Returns a string representation of this port test
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"{Host}:{Port} ({Protocol})";
        }
    }

    /// <summary>
    /// Result of an individual network test operation
    /// </summary>
    internal sealed class NetworkTestResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string Details { get; set; }
        public double MetricValue { get; set; }
        public List<string> AdditionalData { get; set; }
        public Exception Exception { get; set; }
    }

    #endregion
    
        /// Initializes the network health check with required dependencies and configuration
        /// </summary>
        /// <param name="healthCheckService">Health check service for circuit breaker integration</param>
        /// <param name="logger">Logging service for diagnostic information</param>
        /// <param name="options">Optional configuration for network health checking</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public NetworkHealthCheck(
            IHealthCheckService healthCheckService,
            ILoggingService logger,
            NetworkHealthCheckOptions options = null)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? NetworkHealthCheckOptions.CreateDefault();

            Name = new FixedString64Bytes(_options.Name ?? "NetworkHealth");
            Description = _options.Description ?? "Network connectivity, DNS resolution, and external service health monitoring";

            // Initialize HTTP client with appropriate timeout
            _httpClient = new HttpClient
            {
                Timeout = _options.HttpRequestTimeout
            };

            // Set default configuration
            _configuration = HealthCheckConfiguration.ForNetworkService(
                Name.ToString(), 
                Description);

            _logger.LogInfo($"NetworkHealthCheck '{Name}' initialized with comprehensive network monitoring");
        }

        /// <inheritdoc />
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug($"Starting network health check '{Name}'");

                // Execute health checks with circuit breaker protection
                if (_options.UseCircuitBreaker && _healthCheckService != null)
                {
                    return await _healthCheckService.ExecuteWithProtectionAsync(
                        $"Network.{Name}",
                        () => ExecuteHealthCheckInternal(data, cancellationToken),
                        () => CreateCircuitBreakerFallbackResult(stopwatch.Elapsed, data),
                        cancellationToken);
                }
                else
                {
                    return await ExecuteHealthCheckInternal(data, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                data["CancellationRequested"] = true;
                _logger.LogDebug($"Network health check '{Name}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Network health check '{Name}' failed with unexpected error");
                
                data["Exception"] = ex.GetType().Name;
                data["ErrorMessage"] = ex.Message;
                data["StackTrace"] = ex.StackTrace;

                return HealthCheckResult.Unhealthy(
                    $"Network health check failed: {ex.Message}",
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

            _logger.LogInfo($"NetworkHealthCheck '{Name}' configuration updated");
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Type"] = nameof(NetworkHealthCheck),
                ["Category"] = Category.ToString(),
                ["Description"] = Description,
                ["SupportedOperations"] = new[] 
                { 
                    "NetworkInterfaceCheck", 
                    "DNSResolution", 
                    "PingTest", 
                    "HTTPConnectivity", 
                    "PortConnectivity" 
                },
                ["CircuitBreakerEnabled"] = _options.UseCircuitBreaker,
                ["TestConfiguration"] = new Dictionary<string, object>
                {
                    ["NetworkInterfaceTestEnabled"] = _options.EnableNetworkInterfaceTest,
                    ["DNSTestEnabled"] = _options.EnableDnsTest,
                    ["PingTestEnabled"] = _options.EnablePingTest,
                    ["HTTPTestEnabled"] = _options.EnableHttpTest,
                    ["PortTestEnabled"] = _options.EnablePortTest,
                    ["TestEndpoints"] = _options.TestEndpoints?.Length ?? 0,
                    ["DNSServers"] = _options.DnsServers?.Length ?? 0
                },
                ["PerformanceThresholds"] = new Dictionary<string, object>
                {
                    ["PingWarningThreshold"] = _options.PingWarningThreshold.TotalMilliseconds,
                    ["PingCriticalThreshold"] = _options.PingCriticalThreshold.TotalMilliseconds,
                    ["HttpWarningThreshold"] = _options.HttpWarningThreshold.TotalMilliseconds,
                    ["HttpCriticalThreshold"] = _options.HttpCriticalThreshold.TotalMilliseconds,
                    ["DnsWarningThreshold"] = _options.DnsWarningThreshold.TotalMilliseconds,
                    ["DnsCriticalThreshold"] = _options.DnsCriticalThreshold.TotalMilliseconds
                },
                ["Dependencies"] = Dependencies,
                ["Version"] = "1.0.0",
                ["HttpClientTimeout"] = _options.HttpRequestTimeout.TotalMilliseconds,
                ["PingTimeout"] = _options.PingTimeout.TotalMilliseconds
            };
        }

        #region Private Implementation

        private async UniTask<HealthCheckResult> ExecuteHealthCheckInternal(
            Dictionary<string, object> data, 
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var healthChecks = new List<(string Name, bool Success, TimeSpan Duration, string Details)>();

            try
            {
                // Test 1: Network interface availability
                if (_options.EnableNetworkInterfaceTest)
                {
                    var interfaceResult = await TestNetworkInterfaces(cancellationToken);
                    healthChecks.Add(("NetworkInterface", interfaceResult.Success, interfaceResult.Duration, interfaceResult.Details));
                    data["NetworkInterfaceTest"] = interfaceResult;
                }

                // Test 2: DNS resolution
                if (_options.EnableDnsTest)
                {
                    var dnsResult = await TestDnsResolution(cancellationToken);
                    healthChecks.Add(("DNSResolution", dnsResult.Success, dnsResult.Duration, dnsResult.Details));
                    data["DNSTest"] = dnsResult;
                }

                // Test 3: Ping connectivity
                if (_options.EnablePingTest)
                {
                    var pingResult = await TestPingConnectivity(cancellationToken);
                    healthChecks.Add(("PingConnectivity", pingResult.Success, pingResult.Duration, pingResult.Details));
                    data["PingTest"] = pingResult;
                }

                // Test 4: HTTP/HTTPS endpoint testing
                if (_options.EnableHttpTest)
                {
                    var httpResult = await TestHttpEndpoints(cancellationToken);
                    healthChecks.Add(("HTTPConnectivity", httpResult.Success, httpResult.Duration, httpResult.Details));
                    data["HTTPTest"] = httpResult;
                }

                // Test 5: Port connectivity testing
                if (_options.EnablePortTest)
                {
                    var portResult = await TestPortConnectivity(cancellationToken);
                    healthChecks.Add(("PortConnectivity", portResult.Success, portResult.Duration, portResult.Details));
                    data["PortTest"] = portResult;
                }

                // Test 6: Network performance metrics
                var metricsResult = await CollectNetworkMetrics(cancellationToken);
                data["NetworkMetrics"] = metricsResult;

                stopwatch.Stop();

                // Analyze overall health based on all tests
                var overallHealth = AnalyzeOverallHealth(healthChecks, data);
                var statusMessage = CreateStatusMessage(overallHealth, healthChecks, stopwatch.Elapsed);

                _logger.LogDebug($"Network health check '{Name}' completed: {overallHealth} in {stopwatch.Elapsed}");

                return CreateHealthResult(overallHealth, statusMessage, stopwatch.Elapsed, data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Network health check '{Name}' execution failed");
                throw;
            }
        }

        private async UniTask<NetworkTestResult> TestNetworkInterfaces(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var activeInterfaces = 0;
                var totalInterfaces = networkInterfaces.Length;
                var interfaceDetails = new List<string>();

                foreach (var netInterface in networkInterfaces)
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up && 
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        activeInterfaces++;
                        interfaceDetails.Add($"{netInterface.Name}: {netInterface.NetworkInterfaceType}");
                    }
                }

                stopwatch.Stop();

                var success = activeInterfaces > 0;
                var details = success 
                    ? $"{activeInterfaces}/{totalInterfaces} network interfaces active: {string.Join(", ", interfaceDetails)}"
                    : "No active network interfaces found";

                return new NetworkTestResult
                {
                    Success = success,
                    Duration = stopwatch.Elapsed,
                    Details = details,
                    MetricValue = activeInterfaces
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new NetworkTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Network interface test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async UniTask<NetworkTestResult> TestDnsResolution(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var dnsTestHosts = _options.DnsTestHosts ?? new[] { "google.com", "microsoft.com", "cloudflare.com" };
                var successfulResolutions = 0;
                var resolutionTimes = new List<TimeSpan>();
                var resolutionDetails = new List<string>();

                foreach (var host in dnsTestHosts)
                {
                    try
                    {
                        var resolutionStopwatch = Stopwatch.StartNew();
                        var addresses = await Dns.GetHostAddressesAsync(host);
                        resolutionStopwatch.Stop();

                        if (addresses.Length > 0)
                        {
                            successfulResolutions++;
                            resolutionTimes.Add(resolutionStopwatch.Elapsed);
                            resolutionDetails.Add($"{host}: {addresses.Length} addresses in {resolutionStopwatch.ElapsedMilliseconds}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        resolutionDetails.Add($"{host}: Failed - {ex.Message}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                stopwatch.Stop();

                var averageResolutionTime = resolutionTimes.Count > 0 
                    ? TimeSpan.FromTicks(resolutionTimes.Sum(t => t.Ticks) / resolutionTimes.Count)
                    : TimeSpan.Zero;

                var success = successfulResolutions > 0;
                var performanceStatus = AnalyzeDnsPerformance(averageResolutionTime);

                return new NetworkTestResult
                {
                    Success = success,
                    Duration = stopwatch.Elapsed,
                    Details = $"DNS Resolution: {successfulResolutions}/{dnsTestHosts.Length} hosts resolved, avg: {averageResolutionTime.TotalMilliseconds:F0}ms - {performanceStatus}",
                    MetricValue = averageResolutionTime.TotalMilliseconds,
                    AdditionalData = resolutionDetails
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new NetworkTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"DNS resolution test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async UniTask<NetworkTestResult> TestPingConnectivity(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var pingHosts = _options.PingTestHosts ?? new[] { "8.8.8.8", "1.1.1.1", "208.67.222.222" };
                var successfulPings = 0;
                var pingTimes = new List<long>();
                var pingDetails = new List<string>();

                using var ping = new Ping();

                foreach (var host in pingHosts)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(host, (int)_options.PingTimeout.TotalMilliseconds);
                        
                        if (reply.Status == IPStatus.Success)
                        {
                            successfulPings++;
                            pingTimes.Add(reply.RoundtripTime);
                            pingDetails.Add($"{host}: {reply.RoundtripTime}ms");
                        }
                        else
                        {
                            pingDetails.Add($"{host}: {reply.Status}");
                        }
                    }
                    catch (Exception ex)
                    {
                        pingDetails.Add($"{host}: Failed - {ex.Message}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                stopwatch.Stop();

                var averagePingTime = pingTimes.Count > 0 ? pingTimes.Average() : 0;
                var avgPingTimeSpan = TimeSpan.FromMilliseconds(averagePingTime);
                var success = successfulPings > 0;
                var performanceStatus = AnalyzePingPerformance(avgPingTimeSpan);

                return new NetworkTestResult
                {
                    Success = success,
                    Duration = stopwatch.Elapsed,
                    Details = $"Ping Connectivity: {successfulPings}/{pingHosts.Length} hosts reachable, avg: {averagePingTime:F0}ms - {performanceStatus}",
                    MetricValue = averagePingTime,
                    AdditionalData = pingDetails
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new NetworkTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Ping connectivity test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async UniTask<NetworkTestResult> TestHttpEndpoints(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var endpoints = _options.TestEndpoints ?? new[] 
                { 
                    "https://www.google.com", 
                    "https://www.microsoft.com", 
                    "https://httpbin.org/status/200" 
                };

                var successfulRequests = 0;
                var responseTimes = new List<TimeSpan>();
                var endpointDetails = new List<string>();

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var requestStopwatch = Stopwatch.StartNew();
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(_options.HttpRequestTimeout);

                        using var response = await _httpClient.GetAsync(endpoint, timeoutCts.Token);
                        requestStopwatch.Stop();

                        if (response.IsSuccessStatusCode)
                        {
                            successfulRequests++;
                            responseTimes.Add(requestStopwatch.Elapsed);
                            endpointDetails.Add($"{endpoint}: {(int)response.StatusCode} in {requestStopwatch.ElapsedMilliseconds}ms");
                        }
                        else
                        {
                            endpointDetails.Add($"{endpoint}: {(int)response.StatusCode} {response.ReasonPhrase}");
                        }
                    }
                    catch (Exception ex)
                    {
                        endpointDetails.Add($"{endpoint}: Failed - {ex.Message}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                stopwatch.Stop();

                var averageResponseTime = responseTimes.Count > 0 
                    ? TimeSpan.FromTicks(responseTimes.Sum(t => t.Ticks) / responseTimes.Count)
                    : TimeSpan.Zero;

                var success = successfulRequests > 0;
                var performanceStatus = AnalyzeHttpPerformance(averageResponseTime);

                return new NetworkTestResult
                {
                    Success = success,
                    Duration = stopwatch.Elapsed,
                    Details = $"HTTP Connectivity: {successfulRequests}/{endpoints.Length} endpoints reachable, avg: {averageResponseTime.TotalMilliseconds:F0}ms - {performanceStatus}",
                    MetricValue = averageResponseTime.TotalMilliseconds,
                    AdditionalData = endpointDetails
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new NetworkTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"HTTP endpoint test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async UniTask<NetworkTestResult> TestPortConnectivity(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var portTests = _options.PortTests ?? new[]
                {
                    new PortTest { Host = "google.com", Port = 80, Protocol = "HTTP" },
                    new PortTest { Host = "google.com", Port = 443, Protocol = "HTTPS" },
                    new PortTest { Host = "8.8.8.8", Port = 53, Protocol = "DNS" }
                };

                var successfulConnections = 0;
                var connectionTimes = new List<TimeSpan>();
                var portDetails = new List<string>();

                foreach (var portTest in portTests)
                {
                    try
                    {
                        var connectionStopwatch = Stopwatch.StartNew();
                        using var tcpClient = new System.Net.Sockets.TcpClient();
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(_options.PortConnectTimeout);

                        await tcpClient.ConnectAsync(portTest.Host, portTest.Port);
                        connectionStopwatch.Stop();

                        if (tcpClient.Connected)
                        {
                            successfulConnections++;
                            connectionTimes.Add(connectionStopwatch.Elapsed);
                            portDetails.Add($"{portTest.Host}:{portTest.Port} ({portTest.Protocol}): Connected in {connectionStopwatch.ElapsedMilliseconds}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        portDetails.Add($"{portTest.Host}:{portTest.Port} ({portTest.Protocol}): Failed - {ex.Message}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                stopwatch.Stop();

                var averageConnectionTime = connectionTimes.Count > 0 
                    ? TimeSpan.FromTicks(connectionTimes.Sum(t => t.Ticks) / connectionTimes.Count)
                    : TimeSpan.Zero;

                var success = successfulConnections > 0;

                return new NetworkTestResult
                {
                    Success = success,
                    Duration = stopwatch.Elapsed,
                    Details = $"Port Connectivity: {successfulConnections}/{portTests.Length} ports reachable, avg: {averageConnectionTime.TotalMilliseconds:F0}ms",
                    MetricValue = averageConnectionTime.TotalMilliseconds,
                    AdditionalData = portDetails
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new NetworkTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Port connectivity test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async UniTask<Dictionary<string, object>> CollectNetworkMetrics(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, object>();
            
            try
            {
                // Collect network interface statistics
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var totalBytesReceived = 0L;
                var totalBytesSent = 0L;
                var activeInterfaces = 0;

                foreach (var netInterface in networkInterfaces)
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        activeInterfaces++;
                        var stats = netInterface.GetIPv4Statistics();
                        totalBytesReceived += stats.BytesReceived;
                        totalBytesSent += stats.BytesSent;
                    }
                }

                metrics["TotalBytesReceived"] = totalBytesReceived;
                metrics["TotalBytesSent"] = totalBytesSent;
                metrics["ActiveNetworkInterfaces"] = activeInterfaces;
                metrics["TotalNetworkInterfaces"] = networkInterfaces.Length;

                // Collect DNS server information
                if (_options.DnsServers?.Length > 0)
                {
                    metrics["ConfiguredDNSServers"] = _options.DnsServers.Length;
                }

                // Add test configuration metrics
                metrics["TestConfiguration"] = new Dictionary<string, object>
                {
                    ["NetworkInterfaceTestEnabled"] = _options.EnableNetworkInterfaceTest,
                    ["DNSTestEnabled"] = _options.EnableDnsTest,
                    ["PingTestEnabled"] = _options.EnablePingTest,
                    ["HTTPTestEnabled"] = _options.EnableHttpTest,
                    ["PortTestEnabled"] = _options.EnablePortTest
                };

                // Performance thresholds
                metrics["PerformanceThresholds"] = new Dictionary<string, object>
                {
                    ["PingWarning"] = _options.PingWarningThreshold.TotalMilliseconds,
                    ["PingCritical"] = _options.PingCriticalThreshold.TotalMilliseconds,
                    ["HttpWarning"] = _options.HttpWarningThreshold.TotalMilliseconds,
                    ["HttpCritical"] = _options.HttpCriticalThreshold.TotalMilliseconds,
                    ["DnsWarning"] = _options.DnsWarningThreshold.TotalMilliseconds,
                    ["DnsCritical"] = _options.DnsCriticalThreshold.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect network metrics: {ex.Message}");
                metrics["MetricsCollectionError"] = ex.Message;
            }

            return metrics;
        }

        private string AnalyzePingPerformance(TimeSpan averagePingTime)
        {
            if (averagePingTime >= _options.PingCriticalThreshold)
                return "Critical Latency";
            if (averagePingTime >= _options.PingWarningThreshold)
                return "High Latency";
            return "Good Latency";
        }

        private string AnalyzeHttpPerformance(TimeSpan averageResponseTime)
        {
            if (averageResponseTime >= _options.HttpCriticalThreshold)
                return "Critical Performance";
            if (averageResponseTime >= _options.HttpWarningThreshold)
                return "Degraded Performance";
            return "Good Performance";
        }

        private string AnalyzeDnsPerformance(TimeSpan averageResolutionTime)
        {
            if (averageResolutionTime >= _options.DnsCriticalThreshold)
                return "Critical DNS Performance";
            if (averageResolutionTime >= _options.DnsWarningThreshold)
                return "Degraded DNS Performance";
            return "Good DNS Performance";
        }

        private HealthStatus AnalyzeOverallHealth(
            List<(string Name, bool Success, TimeSpan Duration, string Details)> healthChecks,
            Dictionary<string, object> data)
        {
            var failedChecks = 0;
            var degradedChecks = 0;
            var totalChecks = healthChecks.Count;

            foreach (var check in healthChecks)
            {
                if (!check.Success)
                {
                    failedChecks++;
                }
                else
                {
                    // Check for performance degradation based on check type
                    var isDegraded = check.Name switch
                    {
                        "PingConnectivity" => check.Duration >= _options.PingWarningThreshold,
                        "HTTPConnectivity" => check.Duration >= _options.HttpWarningThreshold,
                        "DNSResolution" => check.Duration >= _options.DnsWarningThreshold,
                        _ => false
                    };

                    if (isDegraded)
                    {
                        degradedChecks++;
                    }
                }
            }

            // Determine health status based on failures and performance
            if (failedChecks > 0)
            {
                return failedChecks >= totalChecks * 0.5 ? HealthStatus.Unhealthy : HealthStatus.Degraded;
            }

            if (degradedChecks >= totalChecks * 0.5)
            {
                return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }

        private string CreateStatusMessage(
            HealthStatus status,
            List<(string Name, bool Success, TimeSpan Duration, string Details)> healthChecks,
            TimeSpan totalDuration)
        {
            var successfulChecks = healthChecks.FindAll(c => c.Success).Count;
            var totalChecks = healthChecks.Count;
            var avgDuration = totalChecks > 0 
                ? TimeSpan.FromTicks(healthChecks.ConvertAll(c => c.Duration.Ticks).Sum() / totalChecks)
                : TimeSpan.Zero;

            var statusDescription = status switch
            {
                HealthStatus.Healthy => "Network connectivity is healthy and performing well",
                HealthStatus.Degraded => "Network connectivity is operational but showing performance issues",
                HealthStatus.Unhealthy => "Network connectivity has critical issues affecting functionality",
                _ => "Network connectivity status is unknown"
            };

            return $"{statusDescription} - {successfulChecks}/{totalChecks} checks passed, " +
                   $"avg response: {avgDuration.TotalMilliseconds:F0}ms, total: {totalDuration.TotalMilliseconds:F0}ms";
        }

        private HealthCheckResult CreateHealthResult(
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
                _ => HealthCheckResult.Unhealthy("Unknown network health status", duration, data)
            };
        }

        private HealthCheckResult CreateCircuitBreakerFallbackResult(TimeSpan duration, Dictionary<string, object> data)
        {
            data["CircuitBreakerTriggered"] = true;
            return HealthCheckResult.Unhealthy(
                "Network health check failed - circuit breaker is open",
                duration,
                data);
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
                _httpClient?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error disposing NetworkHealthCheck");
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Configuration options for network health checking
    /// </summary>
    public sealed class NetworkHealthCheckOptions
    {
        /// <summary>