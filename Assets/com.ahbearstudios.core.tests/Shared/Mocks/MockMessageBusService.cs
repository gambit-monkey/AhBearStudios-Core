using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockMessageBusService : IMessageBusService
    {
        private readonly List<IMessage> _publishedMessages = new List<IMessage>();
        private readonly Dictionary<Type, List<Action<IMessage>>> _subscriptions = new Dictionary<Type, List<Action<IMessage>>>();
        private readonly object _lockObject = new object();

        public IReadOnlyList<IMessage> PublishedMessages
        {
            get
            {
                lock (_lockObject)
                {
                    return _publishedMessages.AsValueEnumerable().ToList();
                }
            }
        }

        public IReadOnlyDictionary<Type, List<Action<IMessage>>> Subscriptions
        {
            get
            {
                lock (_lockObject)
                {
                    return new Dictionary<Type, List<Action<IMessage>>>(_subscriptions);
                }
            }
        }

        public bool IsEnabled { get; set; } = true;
        public int PublishCount { get; private set; }
        public int SubscriptionCount { get; private set; }
        public bool ShouldThrowOnPublish { get; set; }
        public bool ShouldDelayPublish { get; set; }
        public TimeSpan PublishDelay { get; set; } = TimeSpan.FromMilliseconds(1);

        public async UniTask PublishMessageAsync<T>(T message) where T : IMessage
        {
            if (ShouldThrowOnPublish)
                throw new InvalidOperationException("Mock publish error");

            if (ShouldDelayPublish)
                await UniTask.Delay(PublishDelay);

            lock (_lockObject)
            {
                _publishedMessages.Add(message);
                PublishCount++;

                // Trigger subscriptions if any exist
                if (_subscriptions.TryGetValue(typeof(T), out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler.Invoke(message);
                        }
                        catch
                        {
                            // Swallow exceptions in test scenarios
                        }
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> handler) where T : IMessage
        {
            lock (_lockObject)
            {
                if (!_subscriptions.ContainsKey(typeof(T)))
                    _subscriptions[typeof(T)] = new List<Action<IMessage>>();

                _subscriptions[typeof(T)].Add(msg => handler((T)msg));
                SubscriptionCount++;
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IMessage
        {
            lock (_lockObject)
            {
                if (_subscriptions.TryGetValue(typeof(T), out var handlers))
                {
                    handlers.RemoveAll(h => h.Target == handler.Target && h.Method == handler.Method);
                    if (handlers.Count == 0)
                        _subscriptions.Remove(typeof(T));
                }
            }
        }

        public bool HasMessageOfType<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.AsValueEnumerable().Any(m => m is T);
            }
        }

        public T GetLastMessage<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.AsValueEnumerable()
                    .Where(m => m is T)
                    .Cast<T>()
                    .LastOrDefault();
            }
        }

        public IEnumerable<T> GetAllMessages<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.AsValueEnumerable()
                    .Where(m => m is T)
                    .Cast<T>()
                    .ToList();
            }
        }

        public int GetMessageCount<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.AsValueEnumerable().Count(m => m is T);
            }
        }

        public bool HasSubscriptionFor<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _subscriptions.ContainsKey(typeof(T));
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _publishedMessages.Clear();
                _subscriptions.Clear();
                PublishCount = 0;
                SubscriptionCount = 0;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public MessageBusStatistics GetStatistics()
        {
            return MessageBusStatistics.Empty;
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockMessageBusService");
        }

        public async UniTask StartAsync()
        {
            IsEnabled = true;
            await UniTask.CompletedTask;
        }

        public async UniTask StopAsync()
        {
            IsEnabled = false;
            await UniTask.CompletedTask;
        }

        public async UniTask FlushAsync()
        {
            await UniTask.CompletedTask;
        }
    }
}