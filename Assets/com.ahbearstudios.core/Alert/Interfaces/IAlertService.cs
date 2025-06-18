using Unity.Collections;

namespace AhBearStudios.Core.Alerts.Interfaces
{
    public interface IAlertService
    {
        void RaiseAlert(in Alert alert);
        void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag);
        void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag, FixedString32Bytes groupId, FixedString32Bytes correlationId);
        void SetMinimumSeverity(AlertSeverity severity);
        AlertSeverity GetMinimumSeverity();
    }
}