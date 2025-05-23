using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Base class for messages that require reliable delivery.
    /// </summary>
    [MemoryPackable]
    public partial class ReliableMessageBase : MessageBase, IReliableMessage
    {
        /// <inheritdoc />
        [MemoryPackInclude]
        public Guid DeliveryId { get; set; } = Guid.NewGuid();
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public int DeliveryAttempts { get; set; } = 0;
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public int MaxDeliveryAttempts { get; set; } = 3;
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public long NextAttemptTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        /// <summary>
        /// Gets the time of the next delivery attempt.
        /// </summary>
        public DateTime NextAttempt => new DateTime(NextAttemptTicks, DateTimeKind.Utc);
        
        /// <summary>
        /// Sets the next delivery attempt time based on an exponential backoff strategy.
        /// </summary>
        public void ScheduleNextAttempt()
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, etc.
            var delaySeconds = Math.Pow(2, DeliveryAttempts);
            var nextAttempt = DateTime.UtcNow.AddSeconds(delaySeconds);
            NextAttemptTicks = nextAttempt.Ticks;
        }
    }
}