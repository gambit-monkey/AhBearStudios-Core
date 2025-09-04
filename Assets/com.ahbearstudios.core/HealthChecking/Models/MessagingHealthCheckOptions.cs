using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Configuration options for messaging health checking operations.
    /// Provides comprehensive settings for message bus connectivity, performance, and operational testing.
    /// </summary>
    public sealed class MessagingHealthCheckOptions
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
        /// Default timeout for all messaging operations
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for message publishing operations
        /// </summary>
        public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Timeout for message subscription operations
        /// </summary>
        public TimeSpan SubscriptionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Performance threshold that triggers warning status
        /// </summary>
        public TimeSpan PerformanceWarningThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Performance threshold that triggers critical status
        /// </summary>
        public TimeSpan PerformanceCriticalThreshold { get; set; } = TimeSpan.FromSeconds(2);

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
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Test message types to use for connectivity validation
        /// </summary>
        public List<string> TestMessageTypes { get; set; } = new List<string> { "HealthCheckTest" };

        /// <summary>
        /// Maximum queue depth before triggering warning status
        /// </summary>
        public int QueueDepthWarningThreshold { get; set; } = 1000;

        /// <summary>
        /// Maximum queue depth before triggering critical status
        /// </summary>
        public int QueueDepthCriticalThreshold { get; set; } = 5000;

        /// <summary>
        /// Creates default messaging health check options
        /// </summary>
        /// <returns>MessagingHealthCheckOptions with default settings</returns>
        public static MessagingHealthCheckOptions CreateDefault()
        {
            return new MessagingHealthCheckOptions
            {
                Name = "MessagingHealth",
                Description = "Message bus connectivity, performance, and operational health monitoring"
            };
        }
    }
}