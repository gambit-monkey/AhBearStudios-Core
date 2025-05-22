using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for publishing messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish</typeparam>
    public interface IMessagePublisher<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Publishes a message to all subscribers
        /// </summary>
        /// <param name="message">The message to publish</param>
        void Publish(TMessage message);
    
        /// <summary>
        /// Publishes a message to all subscribers asynchronously
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}