using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Profiling.Internal
{
    /// <summary>
    /// Tracked ProfilerScope that notifies the parent service when disposed.
    /// This internal class is used by ProfilerService to track active scopes and manage lifecycle.
    /// </summary>
    internal sealed class TrackedProfilerScope : IDisposable
    {
        #region Private Fields

        private readonly ProfilerScope _innerScope;
        private readonly Action<Guid> _onDispose;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the unique identifier of the inner profiler scope.
        /// </summary>
        public Guid Id => _innerScope.Id;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the TrackedProfilerScope class.
        /// </summary>
        /// <param name="tag">Profiler tag for categorization</param>
        /// <param name="profilerService">Parent profiler service</param>
        /// <param name="messageBus">Message bus service for publishing messages</param>
        /// <param name="source">Source system or component</param>
        /// <param name="metadata">Additional metadata for context</param>
        /// <param name="thresholdMs">Performance threshold in milliseconds</param>
        /// <param name="enableThresholdMonitoring">Whether to monitor threshold violations</param>
        /// <param name="onDispose">Callback invoked when this scope is disposed</param>
        public TrackedProfilerScope(
            ProfilerTag tag,
            IProfilerService profilerService,
            IMessageBusService messageBus,
            FixedString64Bytes source,
            IReadOnlyDictionary<string, object> metadata,
            double thresholdMs,
            bool enableThresholdMonitoring,
            Action<Guid> onDispose)
        {
            _innerScope = new ProfilerScope(tag, profilerService, messageBus, source, default, metadata, thresholdMs, enableThresholdMonitoring);
            _onDispose = onDispose;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the inner scope and notifies the parent service.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _innerScope?.Dispose();
            }
            finally
            {
                _onDispose?.Invoke(Id);
            }
        }

        #endregion
    }
}