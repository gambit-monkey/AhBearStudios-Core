using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Record struct for messages that require reliable delivery.
    /// Combines the benefits of value semantics with reliable delivery tracking.
    /// </summary>
    [MemoryPackable]
    public readonly partial record struct ReliableMessageBase : IReliableMessage
    {
        /// <summary>
        /// The base message data.
        /// </summary>
        [MemoryPackInclude]
        public MessageBase BaseMessage { get; init; }
        
        /// <inheritdoc />
        public Guid Id => BaseMessage.Id;
        
        /// <inheritdoc />
        public long TimestampTicks => BaseMessage.TimestampTicks;
        
        /// <inheritdoc />
        public ushort TypeCode => BaseMessage.TypeCode;
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public Guid DeliveryId { get; init; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public int DeliveryAttempts { get; init; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public int MaxDeliveryAttempts { get; init; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public long NextAttemptTicks { get; init; }
        
        /// <summary>
        /// Gets the time of the next delivery attempt.
        /// </summary>
        public DateTime NextAttempt => new(NextAttemptTicks, DateTimeKind.Utc);
        
        /// <summary>
        /// Initializes a new instance of the ReliableMessageBase record struct.
        /// </summary>
        /// <param name="typeCode">The type code that identifies this message type.</param>
        /// <param name="maxDeliveryAttempts">Maximum number of delivery attempts.</param>
        public ReliableMessageBase(ushort typeCode, int maxDeliveryAttempts = 3)
        {
            BaseMessage = new MessageBase(typeCode);
            DeliveryId = Guid.NewGuid();
            DeliveryAttempts = 0;
            MaxDeliveryAttempts = maxDeliveryAttempts;
            NextAttemptTicks = DateTime.UtcNow.Ticks;
        }
        
        /// <summary>
        /// Constructor for MemoryPack serialization.
        /// </summary>
        [MemoryPackConstructor]
        public ReliableMessageBase(MessageBase baseMessage, Guid deliveryId, int deliveryAttempts, 
            int maxDeliveryAttempts, long nextAttemptTicks)
        {
            BaseMessage = baseMessage;
            DeliveryId = deliveryId;
            DeliveryAttempts = deliveryAttempts;
            MaxDeliveryAttempts = maxDeliveryAttempts;
            NextAttemptTicks = nextAttemptTicks;
        }
        
        /// <summary>
        /// Creates a new instance with the next delivery attempt scheduled.
        /// </summary>
        /// <returns>A new ReliableMessageBase with updated delivery attempt information.</returns>
        public ReliableMessageBase WithNextAttempt()
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, etc.
            var delaySeconds = Math.Pow(2, DeliveryAttempts);
            var nextAttempt = DateTime.UtcNow.AddSeconds(delaySeconds);
            
            return this with 
            { 
                DeliveryAttempts = DeliveryAttempts + 1,
                NextAttemptTicks = nextAttempt.Ticks
            };
        }
        
        /// <summary>
        /// Schedules the next delivery attempt for the message.
        /// Note: This method exists for interface compatibility but should prefer WithNextAttempt().
        /// </summary>
        void IReliableMessage.ScheduleNextAttempt()
        {
            // This method cannot modify the immutable struct, so it's effectively a no-op
            // Consumers should use WithNextAttempt() instead for proper immutable handling
        }
        
        // Explicit interface implementation for mutable properties
        Guid IReliableMessage.DeliveryId 
        { 
            get => DeliveryId; 
            set => throw new NotSupportedException("Use record struct 'with' expressions for modifications"); 
        }
        
        int IReliableMessage.DeliveryAttempts 
        { 
            get => DeliveryAttempts; 
            set => throw new NotSupportedException("Use record struct 'with' expressions for modifications"); 
        }
        
        int IReliableMessage.MaxDeliveryAttempts 
        { 
            get => MaxDeliveryAttempts; 
            set => throw new NotSupportedException("Use record struct 'with' expressions for modifications"); 
        }
        
        long IReliableMessage.NextAttemptTicks 
        { 
            get => NextAttemptTicks; 
            set => throw new NotSupportedException("Use record struct 'with' expressions for modifications"); 
        }
    }
}