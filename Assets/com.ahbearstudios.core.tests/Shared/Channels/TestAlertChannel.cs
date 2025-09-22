using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Tests.Shared.Channels
{
    /// <summary>
    /// Simple test channel for unit testing scenarios.
    /// Stores alerts in memory for verification.
    /// </summary>
    public sealed class TestAlertChannel : BaseAlertChannel
    {
        private readonly List<Alert> _sentAlerts = new List<Alert>();
        private bool _isHealthy = true;

        public IReadOnlyList<Alert> SentAlerts => _sentAlerts;
        public override FixedString64Bytes Name { get; }

        public TestAlertChannel(string name = "TestChannel", IMessageBusService messageBusService = null) : base(messageBusService)
        {
            Name = name ?? "TestChannel";
            MinimumSeverity = AlertSeverity.Debug;
        }

        /// <summary>
        /// Sets the health status of this test channel for testing purposes.
        /// </summary>
        /// <param name="healthy">True if the channel should report as healthy, false otherwise</param>
        public void SetHealthy(bool healthy)
        {
            _isHealthy = healthy;
        }

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            if (!_isHealthy)
            {
                throw new InvalidOperationException("Test channel is not healthy");
            }

            _sentAlerts.Add(alert);
            return true;
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            if (!_isHealthy)
            {
                throw new InvalidOperationException("Test channel is not healthy");
            }

            _sentAlerts.Add(alert);
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            await UniTask.CompletedTask;
            var duration = DateTime.UtcNow - startTime;

            if (_isHealthy)
            {
                return ChannelHealthResult.Healthy("Test channel is healthy", duration);
            }
            else
            {
                return ChannelHealthResult.Unhealthy("Test channel is set to unhealthy", duration);
            }
        }

        protected override async UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId)
        {
            await UniTask.CompletedTask;
            return true;
        }

        protected override ChannelConfig CreateDefaultConfiguration()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = AlertChannelType.Log,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Debug,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] [{Source}] {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                SendTimeout = TimeSpan.FromSeconds(1),
                Priority = 10
            };
        }

        public override void ResetStatistics(Guid correlationId = default)
        {
            _sentAlerts.Clear();
        }
    }
}