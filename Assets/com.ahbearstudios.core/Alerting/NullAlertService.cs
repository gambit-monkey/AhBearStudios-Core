using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

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
        public void RaiseAlert(string message, AlertSeverity severity, string source, string tag)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag)
        {
            // No-op
        }
    }
}