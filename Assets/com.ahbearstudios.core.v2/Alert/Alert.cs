using Unity.Collections;

namespace AhBearStudios.Core.Alerts
{
    public readonly struct Alert
    {
        public readonly FixedString128Bytes Message;
        public readonly AlertSeverity Severity;
        public readonly FixedString64Bytes Source;
        public readonly FixedString64Bytes Tag;
        public readonly FixedString32Bytes GroupId;
        public readonly FixedString32Bytes CorrelationId;
        public readonly long Timestamp;

        public Alert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag, FixedString32Bytes groupId, FixedString32Bytes correlationId, long timestamp)
        {
            Message = message;
            Severity = severity;
            Source = source;
            Tag = tag;
            GroupId = groupId;
            CorrelationId = correlationId;
            Timestamp = timestamp;
        }
    }
}