using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    public interface IKeyedMessageBus<TKey, TMessage>
    {
        void Publish(TKey key, TMessage message);
        Task PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
        ISubscriptionToken Subscribe(TKey key, Action<TMessage> handler);
        void Unsubscribe(ISubscriptionToken token);
    }
}