using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerts.Interfaces
{
    public interface IAlertTarget : IDisposable
    {
        void HandleAlert(in Alert alert);
        AlertSeverity MinimumSeverity { get; set; }
        bool AcceptsTag(FixedString64Bytes tag);
    }
}