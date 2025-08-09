using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Null object pattern channel that discards all alerts.
    /// </summary>
    internal sealed class NullAlertChannel : BaseAlertChannel
    {
        public override FixedString64Bytes Name => "NullChannel";

        public NullAlertChannel(IMessageBusService messageBusService) : base(messageBusService)
        {
            MinimumSeverity = AlertSeverity.Debug;
        }

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            return true; // Always succeeds by doing nothing
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            await UniTask.CompletedTask;
            return true; // Always succeeds by doing nothing
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            await UniTask.CompletedTask;
            var duration = DateTime.UtcNow - startTime;
            return ChannelHealthResult.Healthy("Null channel always healthy", duration);
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
                MessageFormat = "",
                EnableBatching = false,
                EnableHealthMonitoring = false,
                HealthCheckInterval = TimeSpan.FromHours(1),
                SendTimeout = TimeSpan.FromMilliseconds(1),
                Priority = 0
            };
        }
    }
}