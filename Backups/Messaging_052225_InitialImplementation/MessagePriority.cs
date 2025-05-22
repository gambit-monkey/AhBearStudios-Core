namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Enum defining message priority levels
    /// </summary>
    public enum MessagePriority
    {
        /// <summary>
        /// Low priority messages (processed last)
        /// </summary>
        Low = 0,
    
        /// <summary>
        /// Normal priority messages (default)
        /// </summary>
        Normal = 1,
    
        /// <summary>
        /// High priority messages (processed before normal)
        /// </summary>
        High = 2,
    
        /// <summary>
        /// Critical priority messages (processed first)
        /// </summary>
        Critical = 3
    }
}