using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Combined interface for publishing and subscribing to messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IMessageBus<TMessage> : IMessagePublisher<TMessage>, IMessageSubscriber<TMessage> 
        where TMessage : IMessage
    {
    }
}