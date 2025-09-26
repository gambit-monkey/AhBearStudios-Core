using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs
{
    /// <summary>
    /// Lightweight stub implementation of ILoggingService for TDD testing.
    /// Records logging calls without implementing actual logging logic.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class StubLoggingService : ILoggingService
    {
        private readonly List<LogEntry> _logEntries = new();
        private readonly object _lockObject = new();

        #region Test Verification Properties

        /// <summary>
        /// Gets all recorded log entries for test verification.
        /// </summary>
        public IReadOnlyList<LogEntry> RecordedLogs
        {
            get
            {
                lock (_lockObject)
                {
                    return _logEntries.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the count of log entries with the specified level.
        /// </summary>
        public int GetLogCount(LogLevel level)
        {
            lock (_lockObject)
            {
                return _logEntries.Count(e => e.Level == level);
            }
        }

        /// <summary>
        /// Checks if any log entry contains the specified message.
        /// </summary>
        public bool HasLogWithMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            lock (_lockObject)
            {
                return _logEntries.Any(e => e.Message.ToString().Contains(message));
            }
        }

        /// <summary>
        /// Checks if there are any error logs recorded.
        /// </summary>
        public bool HasErrorLogs()
        {
            lock (_lockObject)
            {
                return _logEntries.Any(e => e.Level >= LogLevel.Error);
            }
        }

        /// <summary>
        /// Gets the last logged entry.
        /// </summary>
        public LogEntry? GetLastLog()
        {
            lock (_lockObject)
            {
                return _logEntries.LastOrDefault();
            }
        }

        /// <summary>
        /// Clears all recorded log entries.
        /// </summary>
        public void ClearLogs()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
            }
        }

        #endregion

        #region ILoggingService Implementation - Minimal Stub Behavior

        // Configuration properties with default values
        public LoggingConfig Configuration { get; private set; } = LoggingConfig.Default;
        public bool IsEnabled { get; set; } = true;

        // Internal property for stub behavior
        private LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        // Core logging methods - record calls only
        public void LogDebug(string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Debug, message, correlationId, sourceContext, null, properties);
        }

        public void LogInfo(string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Info, message, correlationId, sourceContext, null, properties);
        }

        public void LogWarning(string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Warning, message, correlationId, sourceContext, null, properties);
        }

        public void LogError(string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Error, message, correlationId, sourceContext, null, properties);
        }

        public void LogCritical(string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Critical, message, correlationId, sourceContext, null, properties);
        }

        // Guid overloads
        public void LogDebug(string message, Guid correlationId, string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            LogDebug(message, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        public void LogInfo(string message, Guid correlationId, string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            LogInfo(message, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        public void LogWarning(string message, Guid correlationId, string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            LogWarning(message, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        public void LogError(string message, Guid correlationId, string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            LogError(message, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        public void LogCritical(string message, Guid correlationId, string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            LogCritical(message, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        // Structured logging methods - minimal implementation
        public void LogDebug<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var properties = new Dictionary<string, object> { ["StructuredData"] = data };
            RecordLogEntry(LogLevel.Debug, message, correlationId, null, null, properties);
        }

        public void LogInfo<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var properties = new Dictionary<string, object> { ["StructuredData"] = data };
            RecordLogEntry(LogLevel.Info, message, correlationId, null, null, properties);
        }

        public void LogWarning<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var properties = new Dictionary<string, object> { ["StructuredData"] = data };
            RecordLogEntry(LogLevel.Warning, message, correlationId, null, null, properties);
        }

        public void LogError<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var properties = new Dictionary<string, object> { ["StructuredData"] = data };
            RecordLogEntry(LogLevel.Error, message, correlationId, null, null, properties);
        }

        public void LogCritical<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var properties = new Dictionary<string, object> { ["StructuredData"] = data };
            RecordLogEntry(LogLevel.Critical, message, correlationId, null, null, properties);
        }

        // Exception logging
        public void LogException(string message, Exception exception, FixedString64Bytes correlationId = default,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            RecordLogEntry(LogLevel.Error, message, correlationId, sourceContext, exception, properties);
        }

        public void LogException(string message, Exception exception, Guid correlationId,
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            LogException(message, exception, new FixedString64Bytes(correlationId.ToString("N")[..16]), sourceContext, properties);
        }

        // General logging method
        public void Log(LogLevel level, string message, FixedString64Bytes correlationId = default,
            string sourceContext = null, Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null, string channel = null)
        {
            RecordLogEntry(level, message, correlationId, sourceContext, exception, properties, channel);
        }

        // Scope management - returns stub scope
        public ILogScope BeginScope(string scopeName, FixedString64Bytes correlationId = default,
            string sourceContext = null)
        {
            return new StubLogScope(scopeName, correlationId, sourceContext);
        }

        // Target management - no-op implementations
        public void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default)
        {
            // No-op: stub doesn't manage targets
        }

        public bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default)
        {
            return true; // Always returns success
        }

        public IReadOnlyCollection<ILogTarget> GetTargets()
        {
            return new List<ILogTarget>(); // Always empty
        }

        // Configuration
        public void SetMinimumLevel(LogLevel level, FixedString64Bytes correlationId = default)
        {
            MinimumLevel = level;
        }

        // Filter management - no-op implementations
        public void AddFilter(ILogFilter filter, FixedString64Bytes correlationId = default)
        {
            // No-op: stub doesn't manage filters
        }

        public bool RemoveFilter(string filterName, FixedString64Bytes correlationId = default)
        {
            return true; // Always returns success
        }

        // Statistics - returns mock data
        public LoggingStatistics GetStatistics()
        {
            return LoggingStatistics.Create(
                messagesProcessed: _logEntries.Count,
                errorCount: _logEntries.Count(e => e.Level >= LogLevel.Error),
                activeTargets: 0,
                healthyTargets: 0,
                uptimeSeconds: 1.0,
                lastHealthCheck: DateTime.UtcNow,
                currentQueueSize: 0,
                averageProcessingTimeMs: 0.0,
                peakMemoryUsageBytes: 0);
        }

        // Unity Test Runner compatible async methods
        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            await UniTask.CompletedTask; // No-op for stub
        }

        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            return ValidationResult.Success("StubLoggingService");
        }

        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            // No-op: stub doesn't require maintenance
        }

        // Channel management - no-op implementations
        public void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default)
        {
            // No-op: stub doesn't manage channels
        }

        public bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default)
        {
            return true; // Always returns success
        }

        public IReadOnlyCollection<ILogChannel> GetChannels()
        {
            return new List<ILogChannel>(); // Always empty
        }

        public ILogChannel GetChannel(string channelName)
        {
            return null; // Stub doesn't have channels
        }

        public bool HasChannel(string channelName)
        {
            return false; // Stub doesn't have channels
        }

        public bool PerformHealthCheck()
        {
            return IsEnabled; // Simple health check based on enabled state
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            ClearLogs();
        }

        #endregion

        #region Private Helper Methods

        private void RecordLogEntry(LogLevel level, string message, FixedString64Bytes correlationId,
            string sourceContext, Exception exception, IReadOnlyDictionary<string, object> properties,
            string channel = null)
        {
            if (!IsEnabled || level < MinimumLevel)
                return;

            lock (_lockObject)
            {
                var correlationString = correlationId.IsEmpty ? string.Empty : correlationId.ToString();

                var entry = LogEntry.Create(
                    level: level,
                    channel: channel ?? "Default",
                    message: message,
                    correlationId: correlationString,
                    sourceContext: sourceContext ?? "StubTest",
                    source: "StubLoggingService",
                    exception: exception,
                    properties: properties);

                _logEntries.Add(entry);
            }
        }

        #endregion
    }

    /// <summary>
    /// Stub implementation of ILogScope for testing.
    /// Provides canned responses without implementing actual scope logic.
    /// </summary>
    internal sealed class StubLogScope : ILogScope
    {
        private readonly DateTime _startTime = DateTime.UtcNow;
        private readonly Dictionary<string, object> _properties = new();

        public FixedString64Bytes Name { get; }
        public FixedString64Bytes CorrelationId { get; }
        public string SourceContext { get; }
        public TimeSpan Elapsed => DateTime.UtcNow - _startTime;
        public bool IsActive { get; private set; } = true;
        public ILogScope Parent => null; // Stub doesn't track parent
        public IReadOnlyCollection<ILogScope> Children { get; } = new List<ILogScope>();

        public StubLogScope(string scopeName, FixedString64Bytes correlationId = default, string sourceContext = null)
        {
            Name = scopeName ?? "StubScope";
            CorrelationId = correlationId;
            SourceContext = sourceContext ?? "StubTest";
        }

        public ILogScope BeginChild(string childName, FixedString64Bytes correlationId = default)
        {
            return new StubLogScope(childName, correlationId, SourceContext);
        }

        public void SetProperty(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _properties[key] = value;
            }
        }

        public object GetProperty(string key)
        {
            return _properties.TryGetValue(key ?? string.Empty, out var value) ? value : null;
        }

        public IReadOnlyDictionary<string, object> GetAllProperties()
        {
            return _properties;
        }

        // Stub logging methods - no-op implementations
        public void LogDebug(string message) { }
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void LogCritical(string message) { }
        public void LogException(Exception exception, string message = null) { }

        public void Dispose()
        {
            IsActive = false;
        }
    }
}