using System.Collections.Generic;
using AhBearStudios.Core.Logging.Interfaces;
using Unity.Burst;
using AhBearStudios.Core.Logging.Messages;

namespace AhBearStudios.Core.Logging.Processors
{
    /// <summary>
    /// Dequeues Burst‐buffered <see cref="LogMessage"/>s and
    /// dispatches them to configured <see cref="ILogTarget"/>s.
    /// </summary>
    [BurstCompile]
    public sealed class LogBatchProcessor : System.IDisposable
    {
        private readonly IBurstLoggingService      _burstLogger;
        private readonly IReadOnlyList<ILogTarget> _targets;
        private readonly int                       _maxMessagesPerBatch;

        public LogBatchProcessor(
            IBurstLoggingService burstLogger,
            IReadOnlyList<ILogTarget> targets,
            int maxMessagesPerBatch = 100)
        {
            _burstLogger          = burstLogger;
            _targets              = targets;
            _maxMessagesPerBatch = maxMessagesPerBatch;
        }

        /// <summary>
        /// Processes up to <c>_maxMessagesPerBatch</c> entries from the Burst queue.
        /// </summary>
        [BurstCompile]
        public void Process()
        {
            int processed = 0;
            while (processed < _maxMessagesPerBatch
                   && _burstLogger.TryDequeue(out LogMessage entry))
            {
                for (int i = 0, n = _targets.Count; i < n; i++)
                {
                    var target = _targets[i];
                    if (target.IsEnabled
                        && target.IsLevelEnabled(entry.Level)
                        && target.ShouldProcessMessage(in entry))
                    {
                        target.Write(in entry);
                    }
                }
                processed++;
            }
        }

        public void Dispose()
        {
            // No-op: targets and burstLogger disposed elsewhere
        }
    }
}