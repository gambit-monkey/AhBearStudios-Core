using Unity.Collections;

namespace AhBearStudios.Core.Alerts.Configuration
{
    public struct AlertConfiguration
    {
        public AlertSeverity MinimumSeverity;
        public FixedString64Bytes DefaultTag;
        public FixedList512Bytes<FixedString64Bytes> AcceptedTags;
        public long CooldownMilliseconds;
    }
}