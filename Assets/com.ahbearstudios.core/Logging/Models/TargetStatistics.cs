using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Performance and operational statistics for a specific log target.
    /// Provides detailed metrics for monitoring target health and performance.
    /// </summary>
    public sealed record TargetStatistics
    {
        /// <summary>
        /// Gets the target name this statistics record belongs to.
        /// </summary>
        public string TargetName { get; init; }

        /// <summary>
        /// Gets the target type identifier.
        /// </summary>
        public string TargetType { get; init; }

        /// <summary>
        /// Gets whether the target is currently enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets whether the target is currently healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the total number of messages processed by this target.
        /// </summary>
        public long MessagesProcessed { get; init; }

        /// <summary>
        /// Gets the total number of messages written successfully.
        /// </summary>
        public long MessagesWritten { get; init; }

        /// <summary>
        /// Gets the total number of messages that failed to write.
        /// </summary>
        public long MessagesFailed { get; init; }

        /// <summary>
        /// Gets the total number of messages dropped (e.g., due to queue overflow).
        /// </summary>
        public long MessagesDropped { get; init; }

        /// <summary>
        /// Gets the total number of retry attempts made.
        /// </summary>
        public long RetryAttempts { get; init; }

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double SuccessRate { get; init; }

        /// <summary>
        /// Gets the error rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double ErrorRate { get; init; }

        /// <summary>
        /// Gets the average message processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the minimum message processing time in milliseconds.
        /// </summary>
        public double MinProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the maximum message processing time in milliseconds.
        /// </summary>
        public double MaxProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the 95th percentile processing time in milliseconds.
        /// </summary>
        public double P95ProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the 99th percentile processing time in milliseconds.
        /// </summary>
        public double P99ProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the current queue size (for buffered targets).
        /// </summary>
        public int CurrentQueueSize { get; init; }

        /// <summary>
        /// Gets the maximum queue size reached.
        /// </summary>
        public int MaxQueueSize { get; init; }

        /// <summary>
        /// Gets the configured queue capacity.
        /// </summary>
        public int QueueCapacity { get; init; }

        /// <summary>
        /// Gets the queue utilization as a percentage (0.0 to 1.0).
        /// </summary>
        public double QueueUtilization { get; init; }

        /// <summary>
        /// Gets the total number of bytes written by this target.
        /// </summary>
        public long BytesWritten { get; init; }

        /// <summary>
        /// Gets the total number of flush operations performed.
        /// </summary>
        public long FlushOperations { get; init; }

        /// <summary>
        /// Gets the average flush time in milliseconds.
        /// </summary>
        public double AverageFlushTimeMs { get; init; }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        public long CurrentMemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets the timestamp when the target was started.
        /// </summary>
        public DateTime StartedAt { get; init; }

        /// <summary>
        /// Gets the timestamp of the last successful write.
        /// </summary>
        public DateTime LastSuccessfulWrite { get; init; }

        /// <summary>
        /// Gets the timestamp of the last error.
        /// </summary>
        public DateTime LastError { get; init; }

        /// <summary>
        /// Gets the timestamp when these statistics were collected.
        /// </summary>
        public DateTime CollectedAt { get; init; }

        /// <summary>
        /// Gets the target uptime.
        /// </summary>
        public TimeSpan Uptime { get; init; }

        /// <summary>
        /// Gets the time since the last successful write.
        /// </summary>
        public TimeSpan TimeSinceLastWrite { get; init; }

        /// <summary>
        /// Gets the time since the last error.
        /// </summary>
        public TimeSpan TimeSinceLastError { get; init; }

        /// <summary>
        /// Gets the messages per second rate.
        /// </summary>
        public double MessagesPerSecond { get; init; }

        /// <summary>
        /// Gets the bytes per second rate.
        /// </summary>
        public double BytesPerSecond { get; init; }

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        public string LastErrorMessage { get; init; }

        /// <summary>
        /// Gets the last error exception type, if any.
        /// </summary>
        public string LastErrorType { get; init; }

        /// <summary>
        /// Gets additional target-specific metrics.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomMetrics { get; init; }

        /// <summary>
        /// Gets the configuration snapshot at the time of collection.
        /// </summary>
        public IReadOnlyDictionary<string, object> ConfigurationSnapshot { get; init; }

        /// <summary>
        /// Initializes a new instance of the TargetStatistics record.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="targetType">The target type</param>
        /// <param name="isEnabled">Whether the target is enabled</param>
        /// <param name="isHealthy">Whether the target is healthy</param>
        /// <param name="messagesProcessed">Total messages processed</param>
        /// <param name="messagesWritten">Total messages written successfully</param>
        /// <param name="messagesFailed">Total messages failed</param>
        /// <param name="messagesDropped">Total messages dropped</param>
        /// <param name="retryAttempts">Total retry attempts</param>
        /// <param name="successRate">Success rate percentage</param>
        /// <param name="errorRate">Error rate percentage</param>
        /// <param name="averageProcessingTimeMs">Average processing time</param>
        /// <param name="minProcessingTimeMs">Minimum processing time</param>
        /// <param name="maxProcessingTimeMs">Maximum processing time</param>
        /// <param name="p95ProcessingTimeMs">95th percentile processing time</param>
        /// <param name="p99ProcessingTimeMs">99th percentile processing time</param>
        /// <param name="currentQueueSize">Current queue size</param>
        /// <param name="maxQueueSize">Maximum queue size reached</param>
        /// <param name="queueCapacity">Queue capacity</param>
        /// <param name="queueUtilization">Queue utilization percentage</param>
        /// <param name="bytesWritten">Total bytes written</param>
        /// <param name="flushOperations">Total flush operations</param>
        /// <param name="averageFlushTimeMs">Average flush time</param>
        /// <param name="currentMemoryUsageBytes">Current memory usage</param>
        /// <param name="peakMemoryUsageBytes">Peak memory usage</param>
        /// <param name="startedAt">Start timestamp</param>
        /// <param name="lastSuccessfulWrite">Last successful write timestamp</param>
        /// <param name="lastError">Last error timestamp</param>
        /// <param name="collectedAt">Collection timestamp</param>
        /// <param name="uptime">Target uptime</param>
        /// <param name="timeSinceLastWrite">Time since last write</param>
        /// <param name="timeSinceLastError">Time since last error</param>
        /// <param name="messagesPerSecond">Messages per second rate</param>
        /// <param name="bytesPerSecond">Bytes per second rate</param>
        /// <param name="lastErrorMessage">Last error message</param>
        /// <param name="lastErrorType">Last error type</param>
        /// <param name="customMetrics">Custom metrics</param>
        /// <param name="configurationSnapshot">Configuration snapshot</param>
        public TargetStatistics(
            string targetName,
            string targetType,
            bool isEnabled = true,
            bool isHealthy = true,
            long messagesProcessed = 0,
            long messagesWritten = 0,
            long messagesFailed = 0,
            long messagesDropped = 0,
            long retryAttempts = 0,
            double successRate = 1.0,
            double errorRate = 0.0,
            double averageProcessingTimeMs = 0.0,
            double minProcessingTimeMs = 0.0,
            double maxProcessingTimeMs = 0.0,
            double p95ProcessingTimeMs = 0.0,
            double p99ProcessingTimeMs = 0.0,
            int currentQueueSize = 0,
            int maxQueueSize = 0,
            int queueCapacity = 0,
            double queueUtilization = 0.0,
            long bytesWritten = 0,
            long flushOperations = 0,
            double averageFlushTimeMs = 0.0,
            long currentMemoryUsageBytes = 0,
            long peakMemoryUsageBytes = 0,
            DateTime startedAt = default,
            DateTime lastSuccessfulWrite = default,
            DateTime lastError = default,
            DateTime collectedAt = default,
            TimeSpan uptime = default,
            TimeSpan timeSinceLastWrite = default,
            TimeSpan timeSinceLastError = default,
            double messagesPerSecond = 0.0,
            double bytesPerSecond = 0.0,
            string lastErrorMessage = null,
            string lastErrorType = null,
            IReadOnlyDictionary<string, object> customMetrics = null,
            IReadOnlyDictionary<string, object> configurationSnapshot = null)
        {
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            TargetType = targetType ?? "Unknown";
            IsEnabled = isEnabled;
            IsHealthy = isHealthy;
            MessagesProcessed = messagesProcessed;
            MessagesWritten = messagesWritten;
            MessagesFailed = messagesFailed;
            MessagesDropped = messagesDropped;
            RetryAttempts = retryAttempts;
            SuccessRate = Math.Max(0.0, Math.Min(1.0, successRate));
            ErrorRate = Math.Max(0.0, Math.Min(1.0, errorRate));
            AverageProcessingTimeMs = averageProcessingTimeMs;
            MinProcessingTimeMs = minProcessingTimeMs;
            MaxProcessingTimeMs = maxProcessingTimeMs;
            P95ProcessingTimeMs = p95ProcessingTimeMs;
            P99ProcessingTimeMs = p99ProcessingTimeMs;
            CurrentQueueSize = currentQueueSize;
            MaxQueueSize = maxQueueSize;
            QueueCapacity = queueCapacity;
            QueueUtilization = Math.Max(0.0, Math.Min(1.0, queueUtilization));
            BytesWritten = bytesWritten;
            FlushOperations = flushOperations;
            AverageFlushTimeMs = averageFlushTimeMs;
            CurrentMemoryUsageBytes = currentMemoryUsageBytes;
            PeakMemoryUsageBytes = peakMemoryUsageBytes;
            StartedAt = startedAt == default ? DateTime.UtcNow : startedAt;
            LastSuccessfulWrite = lastSuccessfulWrite;
            LastError = lastError;
            CollectedAt = collectedAt == default ? DateTime.UtcNow : collectedAt;
            Uptime = uptime == default ? CollectedAt - StartedAt : uptime;
            TimeSinceLastWrite = timeSinceLastWrite;
            TimeSinceLastError = timeSinceLastError;
            MessagesPerSecond = messagesPerSecond;
            BytesPerSecond = bytesPerSecond;
            LastErrorMessage = lastErrorMessage;
            LastErrorType = lastErrorType;
            CustomMetrics = customMetrics ?? new Dictionary<string, object>();
            ConfigurationSnapshot = configurationSnapshot ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates an empty statistics record for a target.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="targetType">The target type</param>
        /// <returns>An empty TargetStatistics record</returns>
        public static TargetStatistics Empty(string targetName, string targetType = "Unknown")
        {
            return new TargetStatistics(targetName, targetType);
        }

        /// <summary>
        /// Creates a statistics record indicating a healthy target.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="targetType">The target type</param>
        /// <param name="messagesProcessed">Number of messages processed</param>
        /// <param name="averageProcessingTimeMs">Average processing time</param>
        /// <returns>A healthy TargetStatistics record</returns>
        public static TargetStatistics Healthy(
            string targetName,
            string targetType,
            long messagesProcessed,
            double averageProcessingTimeMs)
        {
            return new TargetStatistics(
                targetName: targetName,
                targetType: targetType,
                isHealthy: true,
                messagesProcessed: messagesProcessed,
                messagesWritten: messagesProcessed,
                successRate: 1.0,
                errorRate: 0.0,
                averageProcessingTimeMs: averageProcessingTimeMs,
                lastSuccessfulWrite: DateTime.UtcNow);
        }

        /// <summary>
        /// Creates a statistics record indicating an unhealthy target.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="targetType">The target type</param>
        /// <param name="lastErrorMessage">The last error message</param>
        /// <param name="lastErrorType">The last error type</param>
        /// <returns>An unhealthy TargetStatistics record</returns>
        public static TargetStatistics Unhealthy(
            string targetName,
            string targetType,
            string lastErrorMessage,
            string lastErrorType = null)
        {
            return new TargetStatistics(
                targetName: targetName,
                targetType: targetType,
                isHealthy: false,
                errorRate: 1.0,
                successRate: 0.0,
                lastError: DateTime.UtcNow,
                lastErrorMessage: lastErrorMessage,
                lastErrorType: lastErrorType);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a specific target.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="targetType">The target type</param>
        /// <param name="messagesProcessed">Number of messages processed</param>
        /// <param name="messagesWritten">Number of messages written successfully</param>
        /// <param name="messagesFailed">Number of messages that failed</param>
        /// <returns>A new TargetStatistics instance</returns>
        public static TargetStatistics Create(
            string targetName,
            string targetType,
            long messagesProcessed = 0,
            long messagesWritten = 0,
            long messagesFailed = 0)
        {
            var successRate = messagesProcessed > 0 ? (double)messagesWritten / messagesProcessed : 1.0;
            var errorRate = messagesProcessed > 0 ? (double)messagesFailed / messagesProcessed : 0.0;

            return new TargetStatistics(
                targetName: targetName,
                targetType: targetType,
                messagesProcessed: messagesProcessed,
                messagesWritten: messagesWritten,
                messagesFailed: messagesFailed,
                successRate: successRate,
                errorRate: errorRate);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a file target.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="bytesWritten">Number of bytes written</param>
        /// <param name="flushOperations">Number of flush operations</param>
        /// <returns>A new TargetStatistics instance optimized for file targets</returns>
        public static TargetStatistics ForFile(
            string fileName,
            long bytesWritten = 0,
            long flushOperations = 0)
        {
            var fileProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "File",
                ["FileName"] = fileName,
                ["FileSize"] = bytesWritten
            };

            return new TargetStatistics(
                targetName: fileName,
                targetType: "File",
                bytesWritten: bytesWritten,
                flushOperations: flushOperations,
                customMetrics: fileProperties);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a memory target.
        /// </summary>
        /// <param name="capacity">The memory capacity</param>
        /// <param name="currentMemoryUsage">Current memory usage</param>
        /// <param name="peakMemoryUsage">Peak memory usage</param>
        /// <returns>A new TargetStatistics instance optimized for memory targets</returns>
        public static TargetStatistics ForMemory(
            int capacity,
            long currentMemoryUsage = 0,
            long peakMemoryUsage = 0)
        {
            var memoryProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "Memory",
                ["Capacity"] = capacity,
                ["MemoryPressure"] = currentMemoryUsage / (double)capacity
            };

            return new TargetStatistics(
                targetName: "Memory",
                targetType: "Memory",
                queueCapacity: capacity,
                currentMemoryUsageBytes: currentMemoryUsage,
                peakMemoryUsageBytes: peakMemoryUsage,
                queueUtilization: capacity > 0 ? currentMemoryUsage / (double)capacity : 0.0,
                customMetrics: memoryProperties);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a console target.
        /// </summary>
        /// <param name="messagesWritten">Number of messages written to console</param>
        /// <param name="averageProcessingTime">Average processing time in milliseconds</param>
        /// <returns>A new TargetStatistics instance optimized for console targets</returns>
        public static TargetStatistics ForConsole(
            long messagesWritten = 0,
            double averageProcessingTime = 0.0)
        {
            var consoleProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "Console",
                ["ConsoleOutput"] = "StandardOut",
                ["Buffered"] = false
            };

            return new TargetStatistics(
                targetName: "Console",
                targetType: "Console",
                messagesProcessed: messagesWritten,
                messagesWritten: messagesWritten,
                averageProcessingTimeMs: averageProcessingTime,
                successRate: 1.0,
                errorRate: 0.0,
                customMetrics: consoleProperties);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a null target.
        /// </summary>
        /// <param name="messagesProcessed">Number of messages processed</param>
        /// <returns>A new TargetStatistics instance optimized for null targets</returns>
        public static TargetStatistics ForNull(long messagesProcessed = 0)
        {
            var nullProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "Null",
                ["Discarded"] = true,
                ["NoIO"] = true
            };

            return new TargetStatistics(
                targetName: "Null",
                targetType: "Null",
                messagesProcessed: messagesProcessed,
                messagesWritten: messagesProcessed,
                averageProcessingTimeMs: 0.0,
                successRate: 1.0,
                errorRate: 0.0,
                customMetrics: nullProperties);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a network target.
        /// </summary>
        /// <param name="endpoint">The network endpoint</param>
        /// <param name="bytesWritten">Number of bytes sent</param>
        /// <param name="retryAttempts">Number of retry attempts</param>
        /// <param name="averageProcessingTime">Average processing time in milliseconds</param>
        /// <returns>A new TargetStatistics instance optimized for network targets</returns>
        public static TargetStatistics ForNetwork(
            string endpoint,
            long bytesWritten = 0,
            long retryAttempts = 0,
            double averageProcessingTime = 0.0)
        {
            var networkProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "Network",
                ["Endpoint"] = endpoint,
                ["Protocol"] = "TCP",
                ["Retries"] = retryAttempts
            };

            return new TargetStatistics(
                targetName: endpoint,
                targetType: "Network",
                bytesWritten: bytesWritten,
                retryAttempts: retryAttempts,
                averageProcessingTimeMs: averageProcessingTime,
                customMetrics: networkProperties);
        }

        /// <summary>
        /// Creates a new TargetStatistics instance for a database target.
        /// </summary>
        /// <param name="connectionString">The database connection string (masked)</param>
        /// <param name="messagesWritten">Number of messages written to database</param>
        /// <param name="averageProcessingTime">Average processing time in milliseconds</param>
        /// <param name="retryAttempts">Number of retry attempts</param>
        /// <returns>A new TargetStatistics instance optimized for database targets</returns>
        public static TargetStatistics ForDatabase(
            string connectionString,
            long messagesWritten = 0,
            double averageProcessingTime = 0.0,
            long retryAttempts = 0)
        {
            var databaseProperties = new Dictionary<string, object>
            {
                ["TargetType"] = "Database",
                ["ConnectionString"] = connectionString,
                ["Transactional"] = true,
                ["Retries"] = retryAttempts
            };

            return new TargetStatistics(
                targetName: "Database",
                targetType: "Database",
                messagesProcessed: messagesWritten,
                messagesWritten: messagesWritten,
                averageProcessingTimeMs: averageProcessingTime,
                retryAttempts: retryAttempts,
                customMetrics: databaseProperties);
        }

        /// <summary>
        /// Gets the queue utilization percentage as a formatted string.
        /// </summary>
        /// <returns>Queue utilization as a percentage string</returns>
        public string GetQueueUtilizationPercent()
        {
            return $"{QueueUtilization:P1}";
        }

        /// <summary>
        /// Gets the success rate as a formatted string.
        /// </summary>
        /// <returns>Success rate as a percentage string</returns>
        public string GetSuccessRatePercent()
        {
            return $"{SuccessRate:P2}";
        }

        /// <summary>
        /// Gets the error rate as a formatted string.
        /// </summary>
        /// <returns>Error rate as a percentage string</returns>
        public string GetErrorRatePercent()
        {
            return $"{ErrorRate:P2}";
        }

        /// <summary>
        /// Gets a summary description of the target status.
        /// </summary>
        /// <returns>A status summary string</returns>
        public string GetStatusSummary()
        {
            if (!IsEnabled)
                return "Disabled";

            if (!IsHealthy)
                return $"Unhealthy - {LastErrorMessage ?? "Unknown error"}";

            if (ErrorRate > 0.1)
                return $"Degraded - {GetErrorRatePercent()} error rate";

            if (QueueUtilization > 0.8)
                return $"Under pressure - {GetQueueUtilizationPercent()} queue utilization";

            return "Healthy";
        }

        /// <summary>
        /// Gets performance metrics as a dictionary.
        /// </summary>
        /// <returns>A dictionary of performance metrics</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>
            {
                ["MessagesPerSecond"] = MessagesPerSecond,
                ["BytesPerSecond"] = BytesPerSecond,
                ["AverageProcessingTimeMs"] = AverageProcessingTimeMs,
                ["P95ProcessingTimeMs"] = P95ProcessingTimeMs,
                ["P99ProcessingTimeMs"] = P99ProcessingTimeMs,
                ["SuccessRate"] = SuccessRate,
                ["ErrorRate"] = ErrorRate,
                ["QueueUtilization"] = QueueUtilization,
                ["MemoryUsageMB"] = CurrentMemoryUsageBytes / (1024.0 * 1024.0),
                ["PeakMemoryUsageMB"] = PeakMemoryUsageBytes / (1024.0 * 1024.0)
            };
        }

        /// <summary>
        /// Gets operational metrics as a dictionary.
        /// </summary>
        /// <returns>A dictionary of operational metrics</returns>
        public Dictionary<string, object> GetOperationalMetrics()
        {
            return new Dictionary<string, object>
            {
                ["MessagesProcessed"] = MessagesProcessed,
                ["MessagesWritten"] = MessagesWritten,
                ["MessagesFailed"] = MessagesFailed,
                ["MessagesDropped"] = MessagesDropped,
                ["RetryAttempts"] = RetryAttempts,
                ["FlushOperations"] = FlushOperations,
                ["BytesWritten"] = BytesWritten,
                ["UptimeHours"] = Uptime.TotalHours,
                ["CurrentQueueSize"] = CurrentQueueSize,
                ["MaxQueueSize"] = MaxQueueSize
            };
        }

        /// <summary>
        /// Determines if the target is performing well based on key metrics.
        /// </summary>
        /// <returns>True if the target is performing well</returns>
        public bool IsPerformingWell()
        {
            return IsHealthy && 
                   ErrorRate < 0.05 && 
                   QueueUtilization < 0.8 && 
                   AverageProcessingTimeMs < 100;
        }

        /// <summary>
        /// Determines if the target needs attention based on key metrics.
        /// </summary>
        /// <returns>True if the target needs attention</returns>
        public bool NeedsAttention()
        {
            return !IsHealthy || 
                   ErrorRate > 0.1 || 
                   QueueUtilization > 0.9 || 
                   AverageProcessingTimeMs > 1000 ||
                   (DateTime.UtcNow - LastSuccessfulWrite).TotalMinutes > 5;
        }

        /// <summary>
        /// Gets a detailed string representation of the statistics.
        /// </summary>
        /// <returns>A detailed string representation</returns>
        public string GetDetailedString()
        {
            return $"Target Statistics: {TargetName} ({TargetType})\n" +
                   $"Status: {GetStatusSummary()}\n" +
                   $"Messages: {MessagesProcessed:N0} processed, {MessagesWritten:N0} written, {MessagesFailed:N0} failed\n" +
                   $"Performance: {AverageProcessingTimeMs:F2}ms avg, {MessagesPerSecond:F1} msg/s, {BytesPerSecond:F1} B/s\n" +
                   $"Queue: {CurrentQueueSize}/{QueueCapacity} ({GetQueueUtilizationPercent()} utilized)\n" +
                   $"Rates: {GetSuccessRatePercent()} success, {GetErrorRatePercent()} error\n" +
                   $"Memory: {CurrentMemoryUsageBytes / (1024.0 * 1024.0):F1} MB current, {PeakMemoryUsageBytes / (1024.0 * 1024.0):F1} MB peak\n" +
                   $"Uptime: {Uptime.TotalHours:F1} hours\n" +
                   $"Last Write: {(LastSuccessfulWrite == default ? "Never" : $"{(DateTime.UtcNow - LastSuccessfulWrite).TotalMinutes:F1} min ago")}\n" +
                   $"Last Error: {(LastError == default ? "Never" : $"{(DateTime.UtcNow - LastError).TotalMinutes:F1} min ago")}";
        }
    }
}