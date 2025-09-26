using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for orchestrating alert processing flow between subsystems.
    /// Coordinates filtering, suppression, routing, and delivery operations.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertOrchestrationService : IDisposable
    {
        #region Service Status

        /// <summary>
        /// Gets whether the orchestration service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the number of alerts currently being processed.
        /// </summary>
        int AlertsInProgress { get; }

        #endregion

        #region Single Alert Processing

        /// <summary>
        /// Processes a single alert through the complete alert pipeline.
        /// Coordinates filtering, suppression, state management, and delivery.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Processing result indicating success/failure and any modifications</returns>
        AlertProcessingResult ProcessAlert(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Processes a single alert asynchronously through the complete alert pipeline.
        /// Useful for alerts that may require network operations or complex processing.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask with processing result</returns>
        UniTask<AlertProcessingResult> ProcessAlertAsync(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Validates an alert before processing.
        /// Checks severity levels, source validation, and other business rules.
        /// </summary>
        /// <param name="alert">Alert to validate</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Validation result with any issues found</returns>
        AlertValidationResult ValidateAlert(Alert alert, Guid correlationId = default);

        #endregion

        #region Bulk Alert Processing

        /// <summary>
        /// Processes multiple alerts in a single batch operation for performance.
        /// Optimizes processing by batching similar operations together.
        /// </summary>
        /// <param name="alerts">Collection of alerts to process</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask with batch processing results</returns>
        UniTask<BulkAlertProcessingResult> ProcessBulkAlertsAsync(IEnumerable<Alert> alerts, Guid correlationId = default);

        /// <summary>
        /// Processes alerts with priority-based ordering.
        /// Higher severity alerts are processed first, with load balancing.
        /// </summary>
        /// <param name="alerts">Collection of alerts to process with priority</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask with priority processing results</returns>
        UniTask<BulkAlertProcessingResult> ProcessPriorityAlertsAsync(IEnumerable<Alert> alerts, Guid correlationId = default);

        #endregion

        #region Processing Pipeline Control

        /// <summary>
        /// Pauses alert processing for maintenance or emergency situations.
        /// Alerts received during pause are queued for later processing.
        /// </summary>
        /// <param name="reason">Reason for pausing processing</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void PauseProcessing(string reason, Guid correlationId = default);

        /// <summary>
        /// Resumes alert processing after a pause.
        /// Processes any queued alerts that accumulated during pause.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>UniTask representing the resume operation</returns>
        UniTask ResumeProcessingAsync(Guid correlationId = default);

        /// <summary>
        /// Gets whether alert processing is currently paused.
        /// </summary>
        bool IsProcessingPaused { get; }

        /// <summary>
        /// Gets the reason for processing pause (if paused).
        /// </summary>
        string ProcessingPauseReason { get; }

        #endregion

        #region Performance and Metrics

        /// <summary>
        /// Gets performance metrics for alert processing operations.
        /// </summary>
        /// <returns>Processing performance metrics</returns>
        AlertProcessingMetrics GetProcessingMetrics();

        /// <summary>
        /// Gets statistics about alert processing throughput and success rates.
        /// </summary>
        /// <returns>Processing statistics</returns>
        AlertProcessingStatistics GetProcessingStatistics();

        /// <summary>
        /// Resets all processing metrics and statistics.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void ResetMetrics(Guid correlationId = default);

        #endregion

        #region Configuration and Tuning

        /// <summary>
        /// Updates processing configuration for performance tuning.
        /// Allows runtime adjustment of concurrency, timeouts, and batch sizes.
        /// </summary>
        /// <param name="configuration">New processing configuration</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>True if configuration was applied successfully</returns>
        bool UpdateProcessingConfiguration(AlertProcessingConfiguration configuration, Guid correlationId = default);

        /// <summary>
        /// Gets the current processing configuration.
        /// </summary>
        /// <returns>Current processing configuration</returns>
        AlertProcessingConfiguration GetProcessingConfiguration();

        #endregion

        #region Diagnostics and Maintenance

        /// <summary>
        /// Gets detailed diagnostic information about the processing pipeline.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Processing diagnostics</returns>
        AlertProcessingDiagnostics GetProcessingDiagnostics(Guid correlationId = default);

        /// <summary>
        /// Performs maintenance operations on the processing pipeline.
        /// Cleans up completed operations, optimizes queues, and updates metrics.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void PerformMaintenance(Guid correlationId = default);

        /// <summary>
        /// Validates the processing pipeline configuration and health.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Validation result with any issues found</returns>
        ValidationResult ValidateProcessingPipeline(Guid correlationId = default);

        #endregion

        #region Event Notifications

        /// <summary>
        /// Event triggered when alert processing completes successfully.
        /// Provides the processed alert and processing metrics.
        /// </summary>
        event EventHandler<AlertProcessedEventArgs> AlertProcessed;

        /// <summary>
        /// Event triggered when alert processing fails.
        /// Provides the original alert, error information, and retry options.
        /// </summary>
        event EventHandler<AlertProcessingFailedEventArgs> AlertProcessingFailed;

        /// <summary>
        /// Event triggered when processing pipeline status changes.
        /// Provides information about pause/resume and performance changes.
        /// </summary>
        event EventHandler<ProcessingStatusChangedEventArgs> ProcessingStatusChanged;

        #endregion
    }
}