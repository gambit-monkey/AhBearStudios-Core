using System;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Default, high-performance implementation of <see cref="IBurstMessageBusService"/>.
    /// </summary>
    public sealed class BurstMessageBusService : IBurstMessageBusService, IDisposable
    {
        private readonly IMessageBusConfig _config;
        private readonly IMessageRegistry _registry;
        private readonly IMessageSerializer _serializer;
        private readonly IMessageDeliveryService _deliveryService;

        private bool _isRunning;

        /// <summary>
        /// Constructs the service, wiring in all dependencies.
        /// </summary>
        public BurstMessageBusService(
            IMessageBusConfig config,
            IMessageRegistry registry,
            IMessageSerializer serializer,
            IMessageDeliveryService deliveryService)
        {
            _config = config as MessageBusConfig 
                      ?? throw new ArgumentException("Expected MessageBusConfig", nameof(config));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_isRunning) return;
            _deliveryService.Initialize(_config.DefaultDelivery, _config.BatchDelivery);
            _isRunning = true;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isRunning) return;
            _deliveryService.Shutdown();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public void Publish<T>(in T message) where T : struct
        {
            if (!_isRunning) throw new InvalidOperationException("Bus not started");
            // Zero-GC: serializes directly to a NativeArray
            using var payload = _serializer.SerializeBurstCompatible(message, Allocator.Temp);
            _deliveryService.Enqueue(payload);
        }

        /// <inheritdoc/>
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _registry.Register(handler);
        }

        /// <inheritdoc/>
        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _registry.Unregister(handler);
        }

        /// <inheritdoc/>
        public void Update()
        {
            if (!_isRunning) return;
            // Process up to MaxMessagesPerFrame or until time slice expires
            var deadline = DateTime.UtcNow.AddMilliseconds(_config.MessageProcessingTimeSliceMs);
            int processed = 0;

            while (processed < _config.MaxMessagesPerFrame && DateTime.UtcNow < deadline)
            {
                if (!_deliveryService.TryDequeue(out var serialized))
                    break;

                // Burst-safe deserialization + dispatch
                _serializer.DeserializeBurstCompatible(serialized, out var msgType, out var msgData);
                _registry.Invoke(msgType, msgData);
                processed++;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isRunning) Stop();
            _deliveryService.Dispose();
        }
    }
}
