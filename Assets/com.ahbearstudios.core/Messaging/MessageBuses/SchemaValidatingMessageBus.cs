using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.Schema;

namespace AhBearStudios.Core.Messaging.MessageBuses
{
    /// <summary>
    /// A message bus that validates messages against their schemas
    /// </summary>
    /// <typeparam name="TMessage">The type of message to validate</typeparam>
    public class SchemaValidatingMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _innerBus;
        private readonly IMessageSchemaGenerator _schemaGenerator;
        private readonly IBurstLogger _logger;
        
        public SchemaValidatingMessageBus(IMessageBus<TMessage> innerBus, IMessageSchemaGenerator schemaGenerator, IBurstLogger logger = null)
        {
            _innerBus = innerBus ?? throw new ArgumentNullException(nameof(innerBus));
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _logger = logger;
        }
        
        public void Publish(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            // Validate the message
            var validationResult = _schemaGenerator.ValidateMessage(message);
            
            if (!validationResult.IsValid)
            {
                // Log validation errors
                foreach (var error in validationResult.Errors)
                {
                    _logger?.Log(LogLevel.Error, $"Schema validation error for {typeof(TMessage).Name}.{error.PropertyName}: {error.ErrorMessage}","Serialization");
                }
                
                throw new SchemaValidationException($"Message of type {typeof(TMessage).Name} failed schema validation", validationResult);
            }
            
            // Publish the message
            _innerBus.Publish(message);
        }
        
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            // Validate the message
            var validationResult = _schemaGenerator.ValidateMessage(message);
            
            if (!validationResult.IsValid)
            {
                // Log validation errors
                foreach (var error in validationResult.Errors)
                {
                    _logger?.Log(LogLevel.Error, $"Schema validation error for {typeof(TMessage).Name}.{error.PropertyName}: {error.ErrorMessage}", "Serialization");
                }
                
                throw new SchemaValidationException($"Message of type {typeof(TMessage).Name} failed schema validation", validationResult);
            }
            
            // Publish the message
            await _innerBus.PublishAsync(message, cancellationToken);
        }
        
        public ISubscriptionToken Subscribe(Action<TMessage> handler)
        {
            return _innerBus.Subscribe(handler);
        }

        public ISubscriptionToken SubscribeAsync(Func<TMessage, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
        
            // Simply delegate to the inner bus
            // No validation is needed for subscriptions as the validation happens during publishing
            return _innerBus.SubscribeAsync(handler);
        }
    }
    
    
}