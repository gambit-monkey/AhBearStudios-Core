using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for handling messages asynchronously
    /// </summary>
    /// <typeparam name="TMessage">The type of message to handle</typeparam>
    public interface IAsyncMessageHandler<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Handles a message asynchronously
        /// </summary>
        /// <param name="message">The message to handle</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}