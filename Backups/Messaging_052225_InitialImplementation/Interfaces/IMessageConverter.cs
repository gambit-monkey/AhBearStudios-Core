namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for converting between message versions
    /// </summary>
    /// <typeparam name="TSource">The source message type</typeparam>
    /// <typeparam name="TTarget">The target message type</typeparam>
    public interface IMessageConverter<in TSource, out TTarget> 
        where TSource : IMessage 
        where TTarget : IMessage
    {
        /// <summary>
        /// Converts a message from one type to another
        /// </summary>
        /// <param name="source">The source message</param>
        /// <returns>The converted message</returns>
        TTarget Convert(TSource source);
    }
}