using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Diagnostic handler for MessagePipe that integrates with the logging system.
    /// </summary>
    internal sealed class DiagnosticHandler : MessageHandlerFilter<object>
    {
        private readonly ILoggingService _logger;
        
        /// <summary>
        /// Initializes a new instance of the DiagnosticHandler class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public DiagnosticHandler(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public override void Handle(object message, Action<object> next)
        {
            var messageType = message.GetType();
            
            _logger.Log(LogLevel.Trace, $"Processing message of type {messageType.Name}", "MessageBusService");
            next(message);
            _logger.Log(LogLevel.Trace, $"Completed processing message of type {messageType.Name}", "MessageBusService");
        }
    }
}