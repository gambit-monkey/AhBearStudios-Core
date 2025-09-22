using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Unity.Alerting.Channels
{
    /// <summary>
    /// Unity Debug.Log-based alert channel.
    /// </summary>
    internal sealed class UnityDebugAlertChannel : BaseAlertChannel
    {
        public override FixedString64Bytes Name => "UnityDebugChannel";

        public UnityDebugAlertChannel(IMessageBusService messageBusService) : base(messageBusService)
        {
            MinimumSeverity = AlertSeverity.Info;
        }

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            var message = $"[ALERT] [{alert.Source}] {alert.Message}";
            
            switch (alert.Severity)
            {
                case AlertSeverity.Critical:
                case AlertSeverity.Emergency:
                    Debug.LogError(message);
                    break;
                case AlertSeverity.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
            
            return true;
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            var message = $"[ALERT] [{alert.Source}] {alert.Message}";
            
            switch (alert.Severity)
            {
                case AlertSeverity.Critical:
                case AlertSeverity.Emergency:
                    Debug.LogError(message);
                    break;
                case AlertSeverity.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
            
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            await UniTask.CompletedTask;
            var duration = DateTime.UtcNow - startTime;
            return ChannelHealthResult.Healthy("Unity Debug channel always healthy", duration);
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
                ChannelType = AlertChannelType.UnityConsole,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[ALERT] [{Source}] {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(5),
                SendTimeout = TimeSpan.FromSeconds(1),
                Priority = 150
            };
        }
    }
}