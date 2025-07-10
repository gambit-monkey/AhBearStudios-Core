using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.MessageBuses
{
    /// <summary>
    /// Null implementation of IMessageBusService for scenarios where messaging is not needed.
    /// This prevents the systems from failing when no message bus is configured.
    /// </summary>
    public class NullMessageBusService : IMessageBusService
    {
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() => new NullPublisher<TMessage>();
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() => new NullSubscriber<TMessage>();

        public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>() =>
            new NullKeyedPublisher<TKey, TMessage>();

        public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>() =>
            new NullKeyedSubscriber<TKey, TMessage>();

        public void ClearCaches()
        {
        }

        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
        }

        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage =>
            new NullDisposable();

        public IDisposable SubscribeToAllMessages(Action<IMessage> handler) => new NullDisposable();
        public IMessageRegistry GetMessageRegistry() => new NullMessageRegistry();

        private class NullPublisher<TMessage> : IMessagePublisher<TMessage>
        {
            public void Publish(TMessage message)
            {
            }

            public IDisposable PublishAsync(TMessage message) => new NullDisposable();
        }

        private class NullSubscriber<TMessage> : IMessageSubscriber<TMessage>
        {
            public IDisposable Subscribe(Action<TMessage> handler) => new NullDisposable();
            public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter) => new NullDisposable();
        }

        private class NullKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>
        {
            public void Publish(TKey key, TMessage message)
            {
            }

            public IDisposable PublishAsync(TKey key, TMessage message) => new NullDisposable();
        }

        private class NullKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>
        {
            public IDisposable Subscribe(TKey key, Action<TMessage> handler) => new NullDisposable();
            public IDisposable Subscribe(Action<TKey, TMessage> handler) => new NullDisposable();

            public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter) =>
                new NullDisposable();
        }

        private class NullMessageRegistry : IMessageRegistry
        {
            public void DiscoverMessages()
            {
            }

            public void RegisterMessageType(Type messageType)
            {
            }

            public void RegisterMessageType(Type messageType, ushort typeCode)
            {
            }

            public IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes() => new Dictionary<Type, IMessageInfo>();
            public IReadOnlyList<string> GetCategories() => new List<string>();
            public IReadOnlyList<Type> GetMessageTypesByCategory(string category) => new List<Type>();
            public IMessageInfo GetMessageInfo(Type messageType) => null;
            public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage => null;
            public bool IsRegistered(Type messageType) => false;
            public bool IsRegistered<TMessage>() where TMessage : IMessage => false;
            public ushort GetTypeCode(Type messageType) => 0;
            public ushort GetTypeCode<TMessage>() where TMessage : IMessage => 0;
            public Type GetMessageType(ushort typeCode) => null;
            public IReadOnlyDictionary<ushort, Type> GetAllTypeCodes() => new Dictionary<ushort, Type>();

            public void Clear()
            {
            }
        }

        private class NullDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}