using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Simple test channel for unit testing scenarios.
    /// Stores alerts in memory for verification.
    /// </summary>
    internal sealed class TestAlertChannel : BaseAlertChannel
    {
        private readonly List<Alert> _sentAlerts = new List<Alert>();

        public IReadOnlyList<Alert> SentAlerts => _sentAlerts;
        public override FixedString64Bytes Name { get; } = "TestChannel";

        public TestAlertChannel(IMessageBusService messageBusService = null) : base(messageBusService)
        {
            MinimumSeverity = AlertSeverity.Debug;
        }

        protected override async UniTask<bool> SendAlertInternalAsync(Alert alert, Guid correlationId)
        {
            _sentAlerts.Add(alert);
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<HealthCheckResult> PerformHealthCheckAsync(Guid correlationId)
        {
            await UniTask.CompletedTask;
            return HealthCheckResult.Healthy("Test channel always healthy");
        }

        public override void ResetStatistics(Guid correlationId = default)
        {
            _sentAlerts.Clear();
        }
    }
}