using AhBearStudios.Core.Alerts.Interfaces;
using AhBearStudios.Core.Alerts.Configuration;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Core.Alerts.Targets
{
    public sealed class LoggingAlertTarget : IAlertTarget
    {
        private readonly ILogService _log;
        private AlertSeverity _minimumSeverity;
        private FixedList512Bytes<FixedString64Bytes> _acceptedTags;

        public LoggingAlertTarget(ILogService log, AlertConfiguration config)
        {
            _log = log;
            _minimumSeverity = config.MinimumSeverity;
            _acceptedTags = config.AcceptedTags;
        }

        public AlertSeverity MinimumSeverity
        {
            get => _minimumSeverity;
            set => _minimumSeverity = value;
        }

        public bool AcceptsTag(FixedString64Bytes tag)
        {
            if (_acceptedTags.Length == 0) return true;
            for (int i = 0; i < _acceptedTags.Length; ++i)
                if (_acceptedTags[i].Equals(tag)) return true;
            return false;
        }

        public void HandleAlert(in Alert alert)
        {
            _log.Log($"[{alert.Severity}] {alert.Source.ToString()} ({alert.Tag.ToString()}, Group:{alert.GroupId.ToString()}, Corr:{alert.CorrelationId.ToString()}): {alert.Message.ToString()}");
        }

        public void Dispose() { }
    }
}