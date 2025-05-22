using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for managing message versioning
    /// </summary>
    public interface IMessageVersioning
    {
        /// <summary>
        /// Registers a converter between two message types
        /// </summary>
        /// <typeparam name="TSource">The source message type</typeparam>
        /// <typeparam name="TTarget">The target message type</typeparam>
        /// <param name="converter">The converter to register</param>
        void RegisterConverter<TSource, TTarget>(IMessageConverter<TSource, TTarget> converter) 
            where TSource : IMessage 
            where TTarget : IMessage;
    
        /// <summary>
        /// Converts a message from one type to another
        /// </summary>
        /// <typeparam name="TSource">The source message type</typeparam>
        /// <typeparam name="TTarget">The target message type</typeparam>
        /// <param name="source">The source message</param>
        /// <returns>The converted message</returns>
        TTarget Convert<TSource, TTarget>(TSource source) 
            where TSource : IMessage 
            where TTarget : IMessage;
    
        /// <summary>
        /// Gets all available conversions for a message type
        /// </summary>
        /// <typeparam name="TSource">The source message type</typeparam>
        /// <returns>The available target message types</returns>
        IEnumerable<Type> GetAvailableConversions<TSource>() where TSource : IMessage;
    }
}