using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for filtering messages based on content or metadata.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to filter.</typeparam>
    public interface IMessageFilter<TMessage> : IDisposable where TMessage : IMessage
    {
        /// <summary>
        /// Gets a value indicating whether this filter is inverted (true becomes false and vice versa).
        /// </summary>
        bool IsInverted { get; }

        /// <summary>
        /// Gets a description of this filter.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Determines whether a message passes this filter.
        /// </summary>
        /// <param name="message">The message to check.</param>
        /// <returns>True if the message passes the filter, false otherwise.</returns>
        bool PassesFilter(TMessage message);

        /// <summary>
        /// Inverts this filter (true becomes false and vice versa).
        /// </summary>
        /// <returns>This filter, for chaining.</returns>
        IMessageFilter<TMessage> Invert();

        /// <summary>
        /// Combines this filter with another using logical AND.
        /// </summary>
        /// <param name="other">The other filter to combine with.</param>
        /// <returns>A new filter that passes only if both filters pass.</returns>
        IMessageFilter<TMessage> And(IMessageFilter<TMessage> other);

        /// <summary>
        /// Combines this filter with another using logical OR.
        /// </summary>
        /// <param name="other">The other filter to combine with.</param>
        /// <returns>A new filter that passes if either filter passes.</returns>
        IMessageFilter<TMessage> Or(IMessageFilter<TMessage> other);
    }
}