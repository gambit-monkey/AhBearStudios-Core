using System;

namespace AhBearStudios.Core.Messaging.Data
{
    public readonly struct PropagationInfo : IEquatable<PropagationInfo>
    {
        public readonly Guid MessageId;
        public readonly DateTime Timestamp;

        public PropagationInfo(Guid messageId)
        {
            MessageId = messageId;
            Timestamp = DateTime.UtcNow;
        }

        public bool Equals(PropagationInfo other) => MessageId.Equals(other.MessageId);
        public override bool Equals(object obj) => obj is PropagationInfo other && Equals(other);
        public override int GetHashCode() => MessageId.GetHashCode();
    }
}