using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a thread-safe message bus
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IThreadSafeMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets a value indicating whether this message bus is thread-safe
        /// </summary>
        bool IsThreadSafe { get; }
    
        /// <summary>
        /// Gets the synchronization object used for thread safety
        /// </summary>
        object SyncRoot { get; }
    
        /// <summary>
        /// Executes an action with the message bus lock
        /// </summary>
        /// <param name="action">The action to execute</param>
        void WithLock(Action action);
    
        /// <summary>
        /// Executes a function with the message bus lock
        /// </summary>
        /// <typeparam name="TResult">The return type of the function</typeparam>
        /// <param name="func">The function to execute</param>
        /// <returns>The result of the function</returns>
        TResult WithLock<TResult>(Func<TResult> func);
    }
}