using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Schema
{
    /// <summary>
    /// A filter that validates messages against their schemas
    /// </summary>
    /// <typeparam name="TMessage">The type of message to validate</typeparam>
    public class SchemaValidationFilter<TMessage> : IMessageFilter<TMessage> where TMessage : IMessage
    {
        private readonly IMessageSchemaGenerator _schemaGenerator;
        private readonly IBurstLogger _logger;
        private readonly bool _failOnValidationError;
        
        public SchemaValidationFilter(IMessageSchemaGenerator schemaGenerator, IBurstLogger logger = null, bool failOnValidationError = false)
        {
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _logger = logger;
            _failOnValidationError = failOnValidationError;
        }
        
        public bool ShouldProcess(TMessage message)
        {
            if (message == null)
                return false;
                
            var validationResult = _schemaGenerator.ValidateMessage(message);
            
            if (!validationResult.IsValid)
            {
                // Log validation errors
                foreach (var error in validationResult.Errors)
                {
                    _logger?.Log(LogLevel.Error, $"Schema validation error for {typeof(TMessage).Name}.{error.PropertyName}: {error.ErrorMessage}", "Serialization");
                }
                
                // Return false to prevent processing if configured to fail on validation errors
                return !_failOnValidationError;
            }
            
            return true;
        }
    }
}