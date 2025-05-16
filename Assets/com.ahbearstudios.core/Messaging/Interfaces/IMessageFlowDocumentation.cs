using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for documenting message flows
    /// </summary>
    public interface IMessageFlowDocumentation
    {
        /// <summary>
        /// Documents a message flow from a source to a target
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="source">The source of the message</param>
        /// <param name="target">The target of the message</param>
        /// <param name="description">A description of the flow</param>
        void DocumentFlow<TMessage>(string source, string target, string description) where TMessage : IMessage;
    
        /// <summary>
        /// Gets all flows from a source
        /// </summary>
        /// <param name="source">The source to filter by</param>
        /// <returns>The flows from the source</returns>
        IEnumerable<MessageFlow> GetFlowsFromSource(string source);
    
        /// <summary>
        /// Gets all flows to a target
        /// </summary>
        /// <param name="target">The target to filter by</param>
        /// <returns>The flows to the target</returns>
        IEnumerable<MessageFlow> GetFlowsToTarget(string target);
    
        /// <summary>
        /// Gets all flows for a message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The flows for the message type</returns>
        IEnumerable<MessageFlow> GetFlowsForMessageType<TMessage>() where TMessage : IMessage;
    }
}