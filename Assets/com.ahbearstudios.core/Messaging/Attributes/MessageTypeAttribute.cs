namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Attribute for marking a class or struct as a message type
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class MessageTypeAttribute : System.Attribute
    {
        /// <summary>
        /// Gets the category of the message
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// Gets the description of the message
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Gets the version of the message
        /// </summary>
        public int Version { get; }
        
        /// <summary>
        /// Gets a value indicating whether this message is transient
        /// </summary>
        /// <remarks>
        /// Transient messages are typically not persisted or logged
        /// </remarks>
        public bool IsTransient { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageTypeAttribute class
        /// </summary>
        /// <param name="category">The category of the message</param>
        /// <param name="description">The description of the message</param>
        /// <param name="version">The version of the message</param>
        /// <param name="isTransient">Whether the message is transient</param>
        public MessageTypeAttribute(string category = "General", string description = null, int version = 1, bool isTransient = false)
        {
            Category = category;
            Description = description;
            Version = version;
            IsTransient = isTransient;
        }
    }
}