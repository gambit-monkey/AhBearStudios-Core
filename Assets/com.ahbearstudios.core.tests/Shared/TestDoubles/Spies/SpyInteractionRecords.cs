using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Spies
{
    /// <summary>
    /// Records details about a publish call for test verification.
    /// </summary>
    public sealed class PublishCall
    {
        public Type MessageType { get; set; }
        public IMessage Message { get; set; }
        public bool IsAsync { get; set; }
        public bool IsBatch { get; set; }
        public int BatchSize { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Records details about a subscription call for test verification.
    /// </summary>
    public sealed class SubscriptionCall
    {
        public Type MessageType { get; set; }
        public bool IsAsync { get; set; }
        public bool HasFilter { get; set; }
        public bool HasPriority { get; set; }
        public MessagePriority MinPriority { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Records details about an unsubscription call for test verification.
    /// </summary>
    public sealed class UnsubscriptionCall
    {
        public Type MessageType { get; set; }
        public bool IsAsync { get; set; }
        public DateTime Timestamp { get; set; }
    }
}