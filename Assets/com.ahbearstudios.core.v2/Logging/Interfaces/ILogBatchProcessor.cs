using System;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Drains and forwards burst log messages to targets.
    /// </summary>
    public interface ILogBatchProcessor : IDisposable
    {
        /// <summary>
        /// Processes up to the configured number of messages in the burst buffer.
        /// </summary>
        void Process();
    }
}