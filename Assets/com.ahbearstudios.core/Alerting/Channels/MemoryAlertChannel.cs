using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Memory-based alert channel for testing and debugging.
    /// </summary>
    internal sealed class MemoryAlertChannel : BaseAlertChannel
    {
        private readonly Queue<Alert> _storedAlerts;
        private readonly int _maxStoredAlerts;

        public override FixedString64Bytes Name => "MemoryChannel";
        public IReadOnlyCollection<Alert> StoredAlerts => _storedAlerts.ToList();

        public MemoryAlertChannel(IMessageBusService messageBusService, int maxStoredAlerts = 1000) : base(messageBusService)
        {
            _maxStoredAlerts = maxStoredAlerts;
            _storedAlerts = new Queue<Alert>(maxStoredAlerts);
            MinimumSeverity = AlertSeverity.Debug;
        }

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            lock (_storedAlerts)
            {
                while (_storedAlerts.Count >= _maxStoredAlerts)
                    _storedAlerts.Dequeue();
                
                _storedAlerts.Enqueue(alert);
            }
            
            return true;
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            lock (_storedAlerts)
            {
                while (_storedAlerts.Count >= _maxStoredAlerts)
                    _storedAlerts.Dequeue();
                
                _storedAlerts.Enqueue(alert);
            }
            
            await UniTask.CompletedTask;
            return true;
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            await UniTask.CompletedTask;
            var duration = DateTime.UtcNow - startTime;
            return ChannelHealthResult.Healthy($"Memory channel healthy: {_storedAlerts.Count}/{_maxStoredAlerts} stored", duration);
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
                Priority = 50
            };
        }

        public override void ResetStatistics(Guid correlationId = default)
        {
            base.ResetStatistics(correlationId);
            lock (_storedAlerts)
            {
                _storedAlerts.Clear();
            }
        }
    }
}