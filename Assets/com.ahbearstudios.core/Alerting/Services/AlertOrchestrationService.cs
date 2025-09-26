using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Service responsible for orchestrating the complete alert processing flow.
    /// Coordinates between state management and health monitoring services to ensure
    /// reliable alert delivery and proper system health maintenance.
    /// </summary>
    public sealed class AlertOrchestrationService : IAlertOrchestrationService, IDisposable
    {
        #region Dependencies

        private readonly IAlertStateManagementService _stateManagementService;
        private readonly IAlertHealthMonitoringService _healthMonitoringService;
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IProfilerService _profilerService;

        #endregion

        #region State

        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertOrchestrationService.
        /// </summary>
        /// <param name="stateManagementService">Service for managing alert state</param>
        /// <param name="healthMonitoringService">Service for health monitoring operations</param>
        /// <param name="loggingService">Service for logging operations</param>
        /// <param name="messageBusService">Service for message publishing</param>
        /// <param name="profilerService">Service for performance profiling</param>
        public AlertOrchestrationService(
            IAlertStateManagementService stateManagementService,
            IAlertHealthMonitoringService healthMonitoringService,
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IProfilerService profilerService = null)
        {
            _stateManagementService = stateManagementService ?? throw new ArgumentNullException(nameof(stateManagementService));
            _healthMonitoringService = healthMonitoringService ?? throw new ArgumentNullException(nameof(healthMonitoringService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _profilerService = profilerService;
        }

        #endregion

        #region IAlertOrchestrationService Implementation

        /// <summary>
        /// Processes a single alert through the complete orchestration flow.
        /// </summary>
        /// <param name="alert">The alert to process</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>AlertProcessingResult indicating success or failure</returns>
        public async UniTask<AlertProcessingResult> ProcessAlertAsync(
            Alert alert,
            Guid correlationId = default,
            CancellationToken cancellationToken = default)
        {
            using (_profilerService?.BeginScope("AlertOrchestration.ProcessAlert"))
            {
                var finalCorrelationId = EnsureCorrelationId(correlationId, "ProcessAlert", alert.Id.ToString());

                try
                {
                    // Validate alert before processing
                    var validationResult = ValidateAlert(alert, finalCorrelationId);
                    if (!validationResult.IsValid)
                    {
                        _loggingService.LogWarning(
                            "Alert validation failed: {ValidationErrors}",
                            string.Join(", ", validationResult.ValidationErrors),
                            correlationId: finalCorrelationId);

                        return AlertProcessingResult.CreateFailure(
                            alert.Id,
                            "Validation failed",
                            validationResult.ValidationErrors,
                            finalCorrelationId);
                    }

                    // Store alert in state management
                    await _stateManagementService.StoreAlertAsync(alert, finalCorrelationId, cancellationToken);

                    _loggingService.LogInfo(
                        "Alert {AlertId} processed successfully through orchestration",
                        alert.Id,
                        correlationId: finalCorrelationId);

                    return AlertProcessingResult.CreateSuccess(alert.Id, finalCorrelationId);
                }
                catch (OperationCanceledException)
                {
                    _loggingService.LogWarning(
                        "Alert processing cancelled for alert {AlertId}",
                        alert.Id,
                        correlationId: finalCorrelationId);
                    throw;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(
                        ex,
                        "Failed to process alert {AlertId}: {ErrorMessage}",
                        alert.Id,
                        ex.Message,
                        correlationId: finalCorrelationId);

                    return AlertProcessingResult.CreateFailure(
                        alert.Id,
                        ex.Message,
                        new[] { ex.Message },
                        finalCorrelationId);
                }
            }
        }

        /// <summary>
        /// Processes multiple alerts in bulk with optimized performance.
        /// </summary>
        /// <param name="alerts">Collection of alerts to process</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of processing results</returns>
        public async UniTask<IReadOnlyList<AlertProcessingResult>> ProcessBulkAlertsAsync(
            IReadOnlyList<Alert> alerts,
            Guid correlationId = default,
            CancellationToken cancellationToken = default)
        {
            using (_profilerService?.BeginScope("AlertOrchestration.ProcessBulkAlerts"))
            {
                var finalCorrelationId = EnsureCorrelationId(correlationId, "ProcessBulkAlerts", alerts.Count.ToString());

                if (alerts == null || alerts.Count == 0)
                {
                    _loggingService.LogWarning(
                        "No alerts provided for bulk processing",
                        correlationId: finalCorrelationId);
                    return Array.Empty<AlertProcessingResult>();
                }

                var results = new List<AlertProcessingResult>(alerts.Count);

                try
                {
                    _loggingService.LogInfo(
                        "Starting bulk processing of {AlertCount} alerts",
                        alerts.Count,
                        correlationId: finalCorrelationId);

                    // Process alerts in batches to avoid overwhelming the system
                    const int batchSize = 50;
                    for (int i = 0; i < alerts.Count; i += batchSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batchEnd = Math.Min(i + batchSize, alerts.Count);
                        var batch = alerts.AsValueEnumerable().Skip(i).Take(batchEnd - i).ToList();

                        var batchCorrelationId = DeterministicIdGenerator.GenerateCorrelationId(
                            "AlertOrchestration.BulkProcessBatch",
                            $"{finalCorrelationId}_{i}");

                        var batchResults = await ProcessAlertBatchAsync(batch, batchCorrelationId, cancellationToken);
                        results.AddRange(batchResults);
                    }

                    _loggingService.LogInfo(
                        "Completed bulk processing of {AlertCount} alerts with {SuccessCount} successes",
                        alerts.Count,
                        results.AsValueEnumerable().Count(r => r.IsSuccess),
                        correlationId: finalCorrelationId);

                    return results;
                }
                catch (OperationCanceledException)
                {
                    _loggingService.LogWarning(
                        "Bulk alert processing cancelled after processing {ProcessedCount} of {TotalCount} alerts",
                        results.Count,
                        alerts.Count,
                        correlationId: finalCorrelationId);
                    throw;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(
                        ex,
                        "Failed during bulk alert processing: {ErrorMessage}",
                        ex.Message,
                        correlationId: finalCorrelationId);
                    throw;
                }
            }
        }

        /// <summary>
        /// Validates an alert to ensure it meets processing requirements.
        /// </summary>
        /// <param name="alert">The alert to validate</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Validation result with success status and any errors</returns>
        public AlertValidationResult ValidateAlert(Alert alert, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertOrchestration.ValidateAlert"))
            {
                var finalCorrelationId = EnsureCorrelationId(correlationId, "ValidateAlert", alert?.Id.ToString() ?? "null");
                var validationErrors = new List<string>();

                try
                {
                    if (alert == null)
                    {
                        validationErrors.Add("Alert cannot be null");
                        return AlertValidationResult.CreateFailure(validationErrors, finalCorrelationId);
                    }

                    if (alert.Id == default)
                    {
                        validationErrors.Add("Alert ID cannot be empty");
                    }

                    if (string.IsNullOrWhiteSpace(alert.Message))
                    {
                        validationErrors.Add("Alert message cannot be null or empty");
                    }

                    if (alert.Message?.Length > 1024)
                    {
                        validationErrors.Add("Alert message exceeds maximum length of 1024 characters");
                    }

                    if (alert.Source.IsEmpty)
                    {
                        validationErrors.Add("Alert source cannot be empty");
                    }

                    if (alert.Timestamp == default || alert.Timestamp > DateTime.UtcNow.AddMinutes(5))
                    {
                        validationErrors.Add("Alert timestamp is invalid or too far in the future");
                    }

                    if (validationErrors.Count > 0)
                    {
                        _loggingService.LogWarning(
                            "Alert validation failed for alert {AlertId}: {ValidationErrors}",
                            alert.Id,
                            string.Join(", ", validationErrors),
                            correlationId: finalCorrelationId);

                        return AlertValidationResult.CreateFailure(validationErrors, finalCorrelationId);
                    }

                    return AlertValidationResult.CreateSuccess(finalCorrelationId);
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(
                        ex,
                        "Exception during alert validation for alert {AlertId}: {ErrorMessage}",
                        alert?.Id ?? Guid.Empty,
                        ex.Message,
                        correlationId: finalCorrelationId);

                    validationErrors.Add($"Validation exception: {ex.Message}");
                    return AlertValidationResult.CreateFailure(validationErrors, finalCorrelationId);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures a correlation ID is provided, generating one if necessary.
        /// </summary>
        private Guid EnsureCorrelationId(Guid correlationId, string operation, string context)
        {
            return correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId($"AlertOrchestration.{operation}", context)
                : correlationId;
        }

        /// <summary>
        /// Processes a batch of alerts with proper error handling.
        /// </summary>
        private async UniTask<List<AlertProcessingResult>> ProcessAlertBatchAsync(
            IReadOnlyList<Alert> batch,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            using (_profilerService?.BeginScope("AlertOrchestration.ProcessAlertBatch"))
            {
                var results = new List<AlertProcessingResult>(batch.Count);

                foreach (var alert in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var alertCorrelationId = DeterministicIdGenerator.GenerateCorrelationId(
                        "AlertOrchestration.BatchAlert",
                        $"{correlationId}_{alert.Id}");

                    var result = await ProcessAlertAsync(alert, alertCorrelationId, cancellationToken);
                    results.Add(result);
                }

                return results;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _loggingService?.LogInfo("AlertOrchestrationService disposing");
            }
            catch
            {
                // Ignore logging errors during disposal
            }

            _isDisposed = true;
        }

        #endregion
    }
}