namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message bus with error handling
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IErrorHandlingMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the error handler for this message bus
        /// </summary>
        IMessageErrorHandler ErrorHandler { get; set; }
    
        /// <summary>
        /// Gets or sets a value indicating whether to continue delivery to other subscribers if an error occurs
        /// </summary>
        bool ContinueOnError { get; set; }
    
        /// <summary>
        /// Gets or sets a value indicating whether to rethrow exceptions after handling
        /// </summary>
        bool RethrowExceptions { get; set; }
    }
}