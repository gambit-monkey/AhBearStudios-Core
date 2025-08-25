using System;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Null implementation of IMessageScope for use in null service patterns.
    /// Provides a new instance each time to maintain proper scope semantics.
    /// </summary>
    internal sealed class NullMessageScope : IMessageScope
    {
        private readonly Guid _id;
        
        public NullMessageScope()
        {
            _id = Guid.NewGuid();
        }

        public Guid Id => _id;
        
        public int ActiveSubscriptions => 0;
        
        public bool IsActive => true;

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage 
            => NullDisposable.Instance;
        
        public IDisposable SubscribeAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage 
            => NullDisposable.Instance;
        
        public void Dispose() { /* No-op */ }
    }
}