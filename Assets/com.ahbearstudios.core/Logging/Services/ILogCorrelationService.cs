using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Interface for correlation ID management services.
    /// Provides standardized correlation tracking capabilities with support for distributed tracing and hierarchical correlation.
    /// </summary>
    public interface ILogCorrelationService : IDisposable
    {
        /// <summary>
        /// Gets the maximum age for correlation entries before cleanup.
        /// </summary>
        TimeSpan MaxCorrelationAge { get; }

        /// <summary>
        /// Gets the cleanup interval for expired correlations.
        /// </summary>
        TimeSpan CleanupInterval { get; }

        /// <summary>
        /// Gets the current correlation ID for the current thread.
        /// </summary>
        FixedString128Bytes CurrentCorrelationId { get; }

        /// <summary>
        /// Gets the current correlation context for the current thread.
        /// </summary>
        CorrelationContext CurrentContext { get; }

        /// <summary>
        /// Gets the number of active correlations being tracked.
        /// </summary>
        int ActiveCorrelationCount { get; }

        /// <summary>
        /// Gets correlation performance metrics.
        /// </summary>
        CorrelationMetrics Metrics { get; }

        /// <summary>
        /// Event raised when a new correlation is started.
        /// </summary>
        event EventHandler<CorrelationStartedEventArgs> CorrelationStarted;

        /// <summary>
        /// Event raised when a correlation is completed.
        /// </summary>
        event EventHandler<CorrelationCompletedEventArgs> CorrelationCompleted;

        /// <summary>
        /// Starts a new correlation for the current thread.
        /// </summary>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        ICorrelationScope StartCorrelation(
            string operationName,
            FixedString128Bytes parentCorrelationId = default,
            IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Starts a new correlation with a specific correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        ICorrelationScope StartCorrelation(
            FixedString128Bytes correlationId,
            string operationName,
            FixedString128Bytes parentCorrelationId = default,
            IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Continues an existing correlation from another thread or system.
        /// </summary>
        /// <param name="correlationId">The correlation ID to continue</param>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        ICorrelationScope ContinueCorrelation(
            FixedString128Bytes correlationId,
            string operationName,
            IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Completes the current correlation for the current thread.
        /// </summary>
        /// <param name="success">Whether the operation completed successfully</param>
        /// <param name="properties">Optional completion properties</param>
        void CompleteCorrelation(bool success = true, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Gets correlation information for a specific correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to look up</param>
        /// <returns>The correlation information, or null if not found</returns>
        CorrelationInfo? GetCorrelationInfo(FixedString128Bytes correlationId);

        /// <summary>
        /// Gets all active correlations.
        /// </summary>
        /// <returns>A dictionary of correlation IDs to correlation information</returns>
        IReadOnlyDictionary<string, CorrelationInfo> GetActiveCorrelations();

        /// <summary>
        /// Enriches a log entry with correlation information.
        /// </summary>
        /// <param name="logEntry">The log entry to enrich</param>
        /// <returns>A new log entry with correlation information applied</returns>
        LogEntry EnrichLogEntry(LogEntry logEntry);

        /// <summary>
        /// Gets the current correlation performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        CorrelationMetrics GetMetrics();

        /// <summary>
        /// Resets the correlation performance metrics.
        /// </summary>
        void ResetMetrics();
    }
}