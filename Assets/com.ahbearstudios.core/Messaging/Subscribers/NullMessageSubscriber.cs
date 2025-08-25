using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Subscribers
{
    /// <summary>
    /// Null implementation of IMessageSubscriber for use in null service patterns.
    /// </summary>
    internal sealed class NullMessageSubscriber<TMessage> : IMessageSubscriber<TMessage> where TMessage : IMessage
    {
        public static readonly NullMessageSubscriber<TMessage> Instance = new NullMessageSubscriber<TMessage>();
        private NullMessageSubscriber() { }

        public IDisposable Subscribe(Action<TMessage> handler) => NullDisposable.Instance;
        
        public IDisposable SubscribeAsync(Func<TMessage, UniTask> handler) => NullDisposable.Instance;
        
        public IDisposable SubscribeWithFilter(Action<TMessage> handler, Func<TMessage, bool> filter = null, MessagePriority minPriority = MessagePriority.Debug) 
            => NullDisposable.Instance;
        
        public IDisposable SubscribeAsyncWithFilter(Func<TMessage, UniTask> handler, Func<TMessage, UniTask<bool>> filter = null, MessagePriority minPriority = MessagePriority.Debug) 
            => NullDisposable.Instance;
        
        public void UnsubscribeAll() { /* No-op */ }
        
        public int ActiveSubscriptions => 0;
        
        public bool IsOperational => false;
        
        public Type MessageType => typeof(TMessage);
        
        public SubscriberStatistics GetStatistics() => SubscriberStatistics.Empty;
        
        public void Dispose() { /* No-op */ }
    }
}