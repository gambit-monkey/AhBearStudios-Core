using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Publishers
{
    /// <summary>
    /// Null implementation of IMessagePublisher for use in null service patterns.
    /// </summary>
    internal sealed class NullMessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : IMessage
    {
        public static readonly NullMessagePublisher<TMessage> Instance = new NullMessagePublisher<TMessage>();
        private NullMessagePublisher() { }

        public void Publish(TMessage message) { /* No-op */ }
        
        public UniTask PublishAsync(TMessage message, CancellationToken cancellationToken = default) 
            => UniTask.CompletedTask;
        
        public void PublishBatch(IEnumerable<TMessage> messages) { /* No-op */ }
        
        public UniTask PublishBatchAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default) 
            => UniTask.CompletedTask;
        
        public bool PublishIf(TMessage message, Func<bool> condition) 
            => false;
        
        public UniTask<bool> PublishIfAsync(TMessage message, Func<UniTask<bool>> condition, CancellationToken cancellationToken = default) 
            => UniTask.FromResult(false);
        
        public UniTask PublishDelayedAsync(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default) 
            => UniTask.CompletedTask;
        
        public PublisherStatistics GetStatistics() 
            => PublisherStatistics.Empty;
        
        public bool IsOperational => false;
        
        public Type MessageType => typeof(TMessage);
        
        public event EventHandler<MessagePublishedEventArgs> MessagePublished
        {
            add { /* No-op */ }
            remove { /* No-op */ }
        }
        
        public event EventHandler<MessagePublishFailedEventArgs> MessagePublishFailed
        {
            add { /* No-op */ }
            remove { /* No-op */ }
        }
        
        public void Dispose() { /* No-op */ }
    }
}