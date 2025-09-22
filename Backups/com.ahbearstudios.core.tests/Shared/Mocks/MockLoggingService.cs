using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockLoggingService : ILoggingService
    {
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private readonly object _lockObject = new object();

        public IReadOnlyList<LogEntry> LogEntries
        {
            get
            {
                lock (_lockObject)
                {
                    return _logEntries.AsValueEnumerable().ToList();
                }
            }
        }

        public bool ThrowOnError { get; set; }
        public int CallCount { get; private set; }
        public bool IsEnabled { get; set; } = true;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        public void LogDebug(string message, string correlationId = null, string source = null)
        {
            LogMessage(LogLevel.Debug, message, correlationId, source);
        }

        public void LogInfo(string message, string correlationId = null, string source = null)
        {
            LogMessage(LogLevel.Info, message, correlationId, source);
        }

        public void LogWarning(string message, string correlationId = null, string source = null)
        {
            LogMessage(LogLevel.Warning, message, correlationId, source);
        }

        public void LogError(string message, string correlationId = null, string source = null)
        {
            LogMessage(LogLevel.Error, message, correlationId, source);
            if (ThrowOnError)
                throw new InvalidOperationException($"Mock error: {message}");
        }

        public void LogCritical(string message, string correlationId = null, string source = null)
        {
            LogMessage(LogLevel.Critical, message, correlationId, source);
            if (ThrowOnError)
                throw new InvalidOperationException($"Mock critical error: {message}");
        }

        public void LogException(Exception exception, string message = null, string correlationId = null, string source = null)
        {
            var logMessage = string.IsNullOrEmpty(message) ? exception.Message : $"{message}: {exception.Message}";
            LogMessage(LogLevel.Error, logMessage, correlationId, source);
            if (ThrowOnError)
                throw exception;
        }

        private void LogMessage(LogLevel level, string message, string correlationId, string source)
        {
            lock (_lockObject)
            {
                var entry = LogEntry.Create(
                    message,
                    level,
                    source ?? "MockTest",
                    correlationId != null ? Guid.Parse(correlationId) : Guid.NewGuid());

                _logEntries.Add(entry);
                CallCount++;
            }
        }

        public bool HasLogWithMessage(string message)
        {
            lock (_lockObject)
            {
                return _logEntries.AsValueEnumerable().Any(e => e.Message.ToString().Contains(message));
            }
        }

        public bool HasLogWithLevel(LogLevel level)
        {
            lock (_lockObject)
            {
                return _logEntries.AsValueEnumerable().Any(e => e.Level == level);
            }
        }

        public bool HasErrorLogs()
        {
            lock (_lockObject)
            {
                return _logEntries.AsValueEnumerable().Any(e => e.Level >= LogLevel.Error);
            }
        }

        public int GetLogCount(LogLevel level)
        {
            lock (_lockObject)
            {
                return _logEntries.AsValueEnumerable().Count(e => e.Level == level);
            }
        }

        public LogEntry GetLastLog()
        {
            lock (_lockObject)
            {
                return _logEntries.LastOrDefault();
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
                CallCount = 0;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public async UniTask FlushAsync()
        {
            await UniTask.CompletedTask;
        }

        public LogStatistics GetStatistics()
        {
            return LogStatistics.Empty;
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockLoggingService");
        }

        public void SetMinimumLevel(LogLevel level)
        {
            MinimumLevel = level;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }
    }
}