using System;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Provides context and utilities for message handlers.
    /// </summary>
    public interface IMessageHandlerContext : IDisposable
    {
        /// <summary>
        /// Gets the message bus instance.
        /// </summary>
        IMessageBusService MessageBusService { get; }
        
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        ILoggingService Logger { get; }
        
        /// <summary>
        /// Gets the profiler instance.
        /// </summary>
        IProfiler Profiler { get; }
        
        /// <summary>
        /// Executes an action with profiling enabled.
        /// </summary>
        /// <param name="sampleName">The name of the profiling sample.</param>
        /// <param name="action">The action to execute.</param>
        void ExecuteWithProfiling(string sampleName, Action action);
        
        /// <summary>
        /// Subscribes to keyed messages with a specific key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="key">The key to subscribe to.</param>
        /// <param name="handler">The message handler.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable SubscribeKeyed<TKey, TMessage>(TKey key, Action<TMessage> handler);
        
        /// <summary>
        /// Subscribes to keyed messages with all keys.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The message handler that receives both key and message.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable SubscribeKeyedAll<TKey, TMessage>(Action<TKey, TMessage> handler);
    }
}