using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute used to mark and categorize message classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageAttribute : Attribute
    {
        /// <summary>
        /// Gets the category of the message.
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// Gets the description of the message.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Gets the priority of the message (lower values are higher priority).
        /// </summary>
        public int Priority { get; }
        
        /// <summary>
        /// Gets whether this message should be logged when published.
        /// </summary>
        public bool LogOnPublish { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageAttribute class.
        /// </summary>
        /// <param name="category">The category of the message.</param>
        public MessageAttribute(string category) : this(category, "", 0, false)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MessageAttribute class with the specified parameters.
        /// </summary>
        /// <param name="category">The category of the message.</param>
        /// <param name="description">The description of the message.</param>
        /// <param name="priority">The priority of the message (lower values are higher priority).</param>
        /// <param name="logOnPublish">Whether this message should be logged when published.</param>
        public MessageAttribute(string category, string description, int priority = 0, bool logOnPublish = false)
        {
            Category = category;
            Description = description;
            Priority = priority;
            LogOnPublish = logOnPublish;
        }
    }
}