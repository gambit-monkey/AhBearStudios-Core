using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting
{
    /// <summary>
    /// Null implementation of IAlertService for use when alerting is disabled or unavailable.
    /// Provides no-op implementations of all alerting operations with minimal performance overhead.
    /// </summary>
    public sealed class NullAlertService : IAlertService
    {
        /// <summary>
        /// Shared instance of the null alert service to avoid unnecessary allocations.
        /// </summary>
        public static readonly NullAlertService Instance = new NullAlertService();

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
            FixedString32Bytes tag = default, Guid correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RaiseAlert(Alert alert)
        {
            // No-op
        }

        /// <inheritdoc />
        public UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask RaiseAlertAsync(string message, AlertSeverity severity, FixedString64Bytes source,
            FixedString32Bytes tag = default, Guid correlationId = default,
            CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void SetMinimumSeverity(AlertSeverity minimumSeverity)
        {
            // No-op
        }

        /// <inheritdoc />
        public void SetMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity)
        {
            // No-op
        }

        /// <inheritdoc />
        public AlertSeverity GetMinimumSeverity(FixedString64Bytes source = default)
        {
            return AlertSeverity.Critical;
        }

        /// <inheritdoc />
        public void RegisterChannel(IAlertChannel channel, FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public bool UnregisterChannel(FixedString64Bytes channelName, FixedString64Bytes correlationId = default)
        {
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IAlertChannel> GetRegisteredChannels()
        {
            return Array.Empty<IAlertChannel>();
        }

        /// <inheritdoc />
        public void AddFilter(IAlertFilter filter, FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public bool RemoveFilter(FixedString64Bytes filterName, FixedString64Bytes correlationId = default)
        {
            return false;
        }

        /// <inheritdoc />
        public void AddSuppressionRule(AlertRule rule, FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public bool RemoveSuppressionRule(FixedString64Bytes ruleName, FixedString64Bytes correlationId = default)
        {
            return false;
        }

        /// <inheritdoc />
        public IEnumerable<Alert> GetActiveAlerts()
        {
            return Array.Empty<Alert>();
        }

        /// <inheritdoc />
        public IEnumerable<Alert> GetAlertHistory(TimeSpan period)
        {
            return Array.Empty<Alert>();
        }

        /// <inheritdoc />
        public void AcknowledgeAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public void ResolveAlert(Guid alertId, FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public AlertStatistics GetStatistics()
        {
            return AlertStatistics.Empty;
        }

        /// <inheritdoc />
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            return ValidationResult.Success("NullAlertService");
        }

        /// <inheritdoc />
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            // No-op
        }

        /// <inheritdoc />
        public UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // No-op - singleton pattern, nothing to dispose
        }
    }
}