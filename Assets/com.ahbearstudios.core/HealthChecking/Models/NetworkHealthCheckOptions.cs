using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Configuration options for network health checking operations.
    /// Provides comprehensive settings for network connectivity, DNS resolution, and external service testing.
    /// </summary>
    public sealed class NetworkHealthCheckOptions
    {
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
        public TimeSpan HttpRequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Performance threshold that triggers warning status
        /// </summary>
        public TimeSpan PerformanceWarningThreshold { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Performance threshold that triggers critical status
        /// </summary>
        public TimeSpan PerformanceCriticalThreshold { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// List of external endpoints to test connectivity
        /// </summary>
        public List<string> TestEndpoints { get; set; } = new List<string>();

        /// <summary>
        /// DNS servers to test for resolution
        /// </summary>
        public List<string> DnsServers { get; set; } = new List<string> { "8.8.8.8", "1.1.1.1" };

        /// <summary>
        /// Test domains for DNS resolution validation
        /// </summary>
        public List<string> TestDomains { get; set; } = new List<string> { "google.com", "cloudflare.com" };

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public List<FixedString64Bytes> Dependencies { get; set; } = new List<FixedString64Bytes>();

        /// <summary>
        /// Maximum number of retry attempts for failed operations
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Creates default network health check options
        /// </summary>
        /// <returns>NetworkHealthCheckOptions with default settings</returns>
        public static NetworkHealthCheckOptions CreateDefault()
        {
            return new NetworkHealthCheckOptions
            {
                Name = "NetworkHealth",
                Description = "Network connectivity, DNS resolution, and external service health monitoring",
                TestEndpoints = new List<string> 
                { 
                    "https://www.google.com",
                    "https://www.cloudflare.com" 
                }
            };
        }
    }
}