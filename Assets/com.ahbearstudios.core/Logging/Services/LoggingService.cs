using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Processors;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Provides high-level logging capabilities: routing, batching, flushing,
    /// dynamic target management, and publication of <see cref="LogEntryMessage"/>s.
    /// </summary>
    public sealed class LoggingService : ILoggingService, IDisposable
    {
        private readonly IBurstLoggingService _burstLogger;
        private readonly List<ILogTarget> _targets;
        private readonly LogBatchProcessor _batchProcessor;
        private readonly ILogLevelManager _levelManager;
        private readonly IMessageBusService _messageBusService;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="LoggingService"/>.
        /// </summary>
        public LoggingService(
            IBurstLoggingService burstLogger,
            ILogLevelManager levelManager,
            IMessageBusService messageBusService,
            IEnumerable<ILogTarget> initialTargets,
            int maxBatchSize = 100)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            _levelManager = levelManager ?? throw new ArgumentNullException(nameof(levelManager));
            _messageBusService = messageBusService;

            _targets = initialTargets != null
                ? new List<ILogTarget>(initialTargets)
                : new List<ILogTarget>();

            _batchProcessor = new LogBatchProcessor(_burstLogger, _targets, maxBatchSize);
        }

        public void Log(LogLevel level, string message)
            => LogInternal(Tagging.LogTag.Default, level, message);

        public void Log(LogLevel level, string format, params object[] args)
            => LogInternal(Tagging.LogTag.Default, level,
                args == null || args.Length == 0 ? format : string.Format(format, args));

        public void LogException(Exception exception, string contextMessage = null)
        {
            if (exception == null) return;

            var message = contextMessage != null 
                ? $"{contextMessage}: {exception}" 
                : exception.ToString();
    
            LogInternal(Tagging.LogTag.Default, LogLevel.Error, message);
        }

        public void Log(Tagging.LogTag tag, LogLevel level, string message)
            => LogInternal(tag, level, message);

        public void Log(Tagging.LogTag tag, LogLevel level, string format, params object[] args)
            => LogInternal(tag, level,
                args == null || args.Length == 0 ? format : string.Format(format, args));

        public void LogDebug(string message)
            => Log(LogLevel.Debug, message);

        public void LogInfo(string message)
            => Log(LogLevel.Info, message);

        public void LogWarning(string message)
            => Log(LogLevel.Warning, message);
        
        public void Warn(string message)
            => Log(LogLevel.Warning, message);

        public void LogError(string message)
            => Log(LogLevel.Error, message);

        public void LogCritical(string message)
            => Log(LogLevel.Critical, message);

        public void Flush()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(LoggingService));
            _batchProcessor.Process();
        }

        public void SetLogLevel(string category, LogLevel level)
            => _levelManager.SetCategoryLevelOverride(category, level);

        public void RegisterTarget(ILogTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!_targets.Contains(target))
                _targets.Add(target);
        }

        public bool UnregisterTarget(ILogTarget target)
            => target != null && _targets.Remove(target);

        public IReadOnlyList<ILogTarget> GetRegisteredTargets()
            => _targets.AsReadOnly();

        private void LogInternal(Tagging.LogTag tag, LogLevel level, string message)
        {
            if (_disposed) return;

            if (!_levelManager.ShouldLog(level, tag, null))
                return;

            var fsMsg = new FixedString512Bytes(message);
            var props = new LogProperties();

            // Create the struct with stamped timestamp
            var logMsg = new LogMessage(fsMsg, level, tag, props)
            {
                TimestampTicks = DateTime.UtcNow.Ticks
            };

            _burstLogger.Log(logMsg.Tag, logMsg.Message, logMsg.Level, logMsg.Properties);
            _messageBusService?.Publish(new LogEntryMessage(logMsg));
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                Flush();
            }
            catch
            {
            }

            _batchProcessor.Dispose();
            (_burstLogger as IDisposable)?.Dispose();
            _disposed = true;
        }
    }
}