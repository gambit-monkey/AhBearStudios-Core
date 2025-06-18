using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using AhBearStudios.Core.Alerts.Interfaces;

namespace AhBearStudios.Core.Alerts
{
    public sealed class AlertService : IAlertService, IDisposable
    {
        private readonly UnsafeList<IAlertTarget> _targets;
        private readonly AlertThrottleCache _throttle;
        private AlertSeverity _minimumSeverity;
        private readonly long _getTimestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public AlertService(Allocator allocator, AlertSeverity initialSeverity = AlertSeverity.Info, long throttleCooldownMillis = 1000)
        {
            _targets = new UnsafeList<IAlertTarget>(4, allocator);
            _minimumSeverity = initialSeverity;
            _throttle = new AlertThrottleCache(throttleCooldownMillis, allocator);
        }

        public void RegisterTarget(IAlertTarget target)
        {
            if (!_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        public void RaiseAlert(in Alert alert)
        {
            if (alert.Severity < _minimumSeverity || _throttle.ShouldSuppress(in alert))
                return;

            for (int i = 0; i < _targets.Length; ++i)
            {
                var target = _targets[i];
                if (alert.Severity >= target.MinimumSeverity && target.AcceptsTag(alert.Tag))
                {
                    target.HandleAlert(in alert);
                }
            }
        }

        public void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag)
        {
            var correlationId = CorrelationContext.IsSet ? CorrelationContext.Current : CorrelationContext.GenerateId();
            var alert = new Alert(message, severity, source, tag, default, correlationId, _getTimestamp);
            RaiseAlert(in alert);
        }

        public void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag, FixedString32Bytes groupId, FixedString32Bytes correlationId)
        {
            var alert = new Alert(message, severity, source, tag, groupId, correlationId, _getTimestamp);
            RaiseAlert(in alert);
        }

        public void SetMinimumSeverity(AlertSeverity severity) => _minimumSeverity = severity;

        public AlertSeverity GetMinimumSeverity() => _minimumSeverity;

        public void Dispose()
        {
            for (int i = 0; i < _targets.Length; ++i)
            {
                _targets[i]?.Dispose();
            }
            _targets.Dispose();
            _throttle.Dispose();
        }
    }
}
